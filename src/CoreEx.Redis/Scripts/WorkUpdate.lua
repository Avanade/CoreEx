-- Atomically replaces a work record's state only where the caller's version (ETag) matches (compare-and-set).
-- Record hash fields: s = serialized work state (JSON); i = incarnation; v = version, incremented on every update.
-- KEYS[1]: work record key.
-- ARGV[1]: expected ETag (incarnation:version); ARGV[2]: serialized work state; ARGV[3]: time-to-live (ms).
-- Returns the new version when updated; 0 when the record is absent or the version does not match.
-- Arguments are validated before any write: Redis does not roll back a script that errors midway, and PEXPIRE <= 0 deletes the key.
local function posint(v)
  local n = tonumber(v)
  if not n or n <= 0 or n % 1 ~= 0 then return nil end
  return n
end
local ttl = posint(ARGV[3])
if not ttl then return redis.error_reply('Invalid time-to-live') end
if not ARGV[2] or ARGV[2] == '' then return redis.error_reply('Work state is required') end
local fields = redis.call('HMGET', KEYS[1], 'i', 'v')
if not fields[1] or not fields[2] or fields[1] .. ':' .. fields[2] ~= ARGV[1] then return 0 end
local version = redis.call('HINCRBY', KEYS[1], 'v', 1)
redis.call('HSET', KEYS[1], 's', ARGV[2])
redis.call('PEXPIRE', KEYS[1], ttl)
return version
