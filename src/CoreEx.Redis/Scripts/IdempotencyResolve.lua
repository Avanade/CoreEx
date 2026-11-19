-- Reconciles an indeterminate (or expired pending) idempotency record for an explicitly expected revision (see IdempotencyBegin.lua for the hash fields).
-- The caller must already be certain the original work can no longer commit.
-- KEYS[1]: idempotency record key.
-- ARGV[1]: expected revision (owner token); ARGV[2]: retry, C or U; ARGV[3]: response bytes (non-empty for C, empty for U, ignored for retry); ARGV[4]: retention (ms).
-- Returns 1 when resolved; 0 when the revision differs or the record is still actively pending or already terminal.
-- Arguments are validated before any write: Redis does not roll back a script that errors midway, and PEXPIRE <= 0 deletes the key.
local function posint(v)
  local n = tonumber(v)
  if not n or n <= 0 or n % 1 ~= 0 then return nil end
  return n
end
local mode = ARGV[2]
if mode ~= 'retry' and mode ~= 'C' and mode ~= 'U' then return redis.error_reply('Invalid resolution') end
local retention = posint(ARGV[4])
if mode ~= 'retry' and not retention then return redis.error_reply('Invalid retention duration') end
if mode == 'C' and ARGV[3] == '' then return redis.error_reply('C needs response') end
if mode == 'U' and ARGV[3] ~= '' then return redis.error_reply('U takes no response') end
local o, s, d = unpack(redis.call('HMGET', KEYS[1], 'o', 's', 'd'))
if o ~= ARGV[1] then return 0 end
local t = redis.call('TIME')
local now = t[1] * 1000 + math.floor(t[2] / 1000)
if s ~= 'I' and (s ~= 'P' or tonumber(d) > now) then return 0 end
if mode == 'retry' then
  redis.call('DEL', KEYS[1])
else
  redis.call('HSET', KEYS[1], 's', mode, 'r', ARGV[3])
  redis.call('PEXPIRE', KEYS[1], retention)
end
return 1
