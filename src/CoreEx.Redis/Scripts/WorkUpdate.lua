-- Atomically replaces a work record's state (and optionally its result data) only where the caller's version (ETag) matches (compare-and-set).
-- Record hash fields: s = serialized work state (JSON); v = version (ETag), incremented on every update; d = optional result data.
-- KEYS[1]: work record key.
-- ARGV[1]: expected version; ARGV[2]: serialized work state; ARGV[3]: time-to-live (ms); ARGV[4]: data mode ('keep' leaves data unchanged, 'set' replaces it); ARGV[5]: result data (used when 'set').
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
local mode = ARGV[4]
if mode ~= 'keep' and mode ~= 'set' then return redis.error_reply('Invalid data mode') end
if mode == 'set' and not ARGV[5] then return redis.error_reply('Result data is required') end
if redis.call('HGET', KEYS[1], 'v') ~= ARGV[1] then return 0 end
local version = redis.call('HINCRBY', KEYS[1], 'v', 1)
redis.call('HSET', KEYS[1], 's', ARGV[2])
if mode == 'set' then redis.call('HSET', KEYS[1], 'd', ARGV[5]) end
redis.call('PEXPIRE', KEYS[1], ttl)
return version
