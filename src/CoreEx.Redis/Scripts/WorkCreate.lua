-- Atomically creates a work record only where it does not already exist (insert-if-absent).
-- Record hash fields: s = serialized work state (JSON); i = incarnation; v = version; d = optional result data.
-- KEYS[1]: work record key.
-- ARGV[1]: serialized work state; ARGV[2]: time-to-live (ms); ARGV[3]: unique incarnation.
-- Returns 1 when created; 0 when the record already exists.
-- Arguments are validated before any write: Redis does not roll back a script that errors midway, and PEXPIRE <= 0 deletes the key.
local function posint(v)
  local n = tonumber(v)
  if not n or n <= 0 or n % 1 ~= 0 then return nil end
  return n
end
local ttl = posint(ARGV[2])
if not ttl then return redis.error_reply('Invalid time-to-live') end
if not ARGV[1] or ARGV[1] == '' then return redis.error_reply('Work state is required') end
if not ARGV[3] or ARGV[3] == '' then return redis.error_reply('Work incarnation is required') end
if redis.call('EXISTS', KEYS[1]) == 1 then return 0 end
redis.call('HSET', KEYS[1], 's', ARGV[1], 'i', ARGV[3], 'v', 1)
redis.call('PEXPIRE', KEYS[1], ttl)
return 1
