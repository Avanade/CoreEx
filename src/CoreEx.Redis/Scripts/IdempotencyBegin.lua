-- Atomically claims an idempotency key or reports its existing decision.
-- Record hash fields: o = owner/revision token; f = request fingerprint; s = state (P pending, C completed, U completed without response, I indeterminate); d = pending deadline (Redis server ms); r = retained response.
-- KEYS[1]: idempotency record key.
-- ARGV[1]: new owner token (random per attempt); ARGV[2]: request fingerprint; ARGV[3]: lease duration (ms); ARGV[4]: retention (ms).
-- Returns {outcome, revision, response} where outcome matches IdempotencyOutcome: 0 Acquired, 1 InProgress, 2 Completed, 3 ResponseUnavailable, 4 DifferentRequest, 5 Indeterminate.
-- Time comes from the Redis server clock so clients never compare skewed clocks.
-- Arguments are validated before any write: Redis does not roll back a script that errors midway, and PEXPIRE <= 0 deletes the key.
local function posint(v)
  local n = tonumber(v)
  if not n or n <= 0 or n % 1 ~= 0 then return nil end
  return n
end
local lease, retention = posint(ARGV[3]), posint(ARGV[4])
if not lease or not retention then return redis.error_reply('Invalid lease or retention duration') end
local t = redis.call('TIME')
local now = t[1] * 1000 + math.floor(t[2] / 1000)
if redis.call('EXISTS', KEYS[1]) == 0 then
  redis.call('HSET', KEYS[1], 'o', ARGV[1], 'f', ARGV[2], 's', 'P', 'd', now + lease)
  -- Retention after the deadline also bounds how long an abandoned (indeterminate) claim blocks re-execution.
  redis.call('PEXPIRE', KEYS[1], lease + retention)
  return {0, ARGV[1], ''}
end
local o, f, s, d = unpack(redis.call('HMGET', KEYS[1], 'o', 'f', 's', 'd'))
if f ~= ARGV[2] then return {4, o, ''} end
-- The response is read only when replaying, as it can be large; a corrupt C record without r returns nil and the caller fails closed.
if s == 'C' then return {2, o, redis.call('HGET', KEYS[1], 'r')} end
if s == 'U' then return {3, o, ''} end
if s == 'I' then return {5, o, ''} end
if s ~= 'P' then return redis.error_reply('Invalid idempotency state') end
-- An expired pending claim is never taken over automatically; the original work may still be running or may have committed effects.
if tonumber(d) <= now then return {5, o, ''} end
return {1, o, ''}
