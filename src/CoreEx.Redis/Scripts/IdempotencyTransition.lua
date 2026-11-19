-- Applies an owner-checked transition to an idempotency record (see IdempotencyBegin.lua for the hash fields).
-- KEYS[1]: idempotency record key.
-- ARGV[1]: owner token; ARGV[2]: operation (renew, retain or complete); ARGV[3]: lease duration (ms); ARGV[4]: retention (ms); ARGV[5]: terminal state (C or U); ARGV[6]: response bytes (non-empty for C, empty for U).
-- Returns 1 when applied (or already applied identically; for retain, when the owner matched); otherwise 0.
-- Arguments are validated before any write: Redis does not roll back a script that errors midway, and PEXPIRE <= 0 deletes the key.
local function posint(v)
  local n = tonumber(v)
  if not n or n <= 0 or n % 1 ~= 0 then return nil end
  return n
end
local op = ARGV[2]
local lease, retention = posint(ARGV[3]), posint(ARGV[4])
if op == 'renew' then
  if not lease or not retention then return redis.error_reply('Invalid lease or retention duration') end
elseif op == 'complete' then
  if not retention then return redis.error_reply('Invalid retention duration') end
  if ARGV[5] ~= 'C' and ARGV[5] ~= 'U' then return redis.error_reply('Invalid terminal state') end
  if ARGV[5] == 'C' and ARGV[6] == '' then return redis.error_reply('C needs response') end
  if ARGV[5] == 'U' and ARGV[6] ~= '' then return redis.error_reply('U takes no response') end
elseif op ~= 'retain' then
  return redis.error_reply('Unknown transition')
end
local o, s, d = unpack(redis.call('HMGET', KEYS[1], 'o', 's', 'd'))
if o ~= ARGV[1] then return 0 end
-- Retain marks an abandoned pending claim indeterminate; terminal states are left untouched.
if op == 'retain' then
  if s == 'P' then redis.call('HSET', KEYS[1], 's', 'I') end
  return 1
end
-- A retried completion after a lost acknowledgement succeeds only when it matches the committed outcome exactly.
if op == 'complete' and (s == 'C' or s == 'U') then
  if s == ARGV[5] and (redis.call('HGET', KEYS[1], 'r') or '') == ARGV[6] then return 1 end
  return 0
end
local t = redis.call('TIME')
local now = t[1] * 1000 + math.floor(t[2] / 1000)
-- A late owner cannot complete after its deadline: readers already report the claim as indeterminate, so the outcome must not change underneath them.
if s ~= 'P' or tonumber(d) <= now then return 0 end
if op == 'renew' then
  redis.call('HSET', KEYS[1], 'd', now + lease)
  redis.call('PEXPIRE', KEYS[1], lease + retention)
else
  -- Retention restarts at completion, so long-running work does not consume the promised replay window.
  redis.call('HSET', KEYS[1], 's', ARGV[5], 'r', ARGV[6])
  redis.call('PEXPIRE', KEYS[1], retention)
end
return 1
