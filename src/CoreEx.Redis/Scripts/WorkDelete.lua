-- Deletes a work record only where the caller's version (ETag) matches; an absent record is considered deleted.
-- Record hash fields: s = serialized work state (JSON); v = version (ETag); d = optional result data.
-- KEYS[1]: work record key.
-- ARGV[1]: expected version.
-- Returns 1 when deleted or absent; 0 when the version does not match.
local v = redis.call('HGET', KEYS[1], 'v')
if not v then return 1 end
if v ~= ARGV[1] then return 0 end
redis.call('DEL', KEYS[1])
return 1
