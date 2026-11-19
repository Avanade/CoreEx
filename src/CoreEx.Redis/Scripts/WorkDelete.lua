-- Deletes a work record only where the caller's version (ETag) matches; an absent record is considered deleted.
-- Record hash fields: s = serialized work state (JSON); i = incarnation; v = version; d = optional result data.
-- KEYS[1]: work record key.
-- ARGV[1]: expected ETag (incarnation:version).
-- Returns 1 when deleted or absent; 0 when the version does not match.
if redis.call('EXISTS', KEYS[1]) == 0 then return 1 end
local fields = redis.call('HMGET', KEYS[1], 'i', 'v')
if not fields[1] or not fields[2] or fields[1] .. ':' .. fields[2] ~= ARGV[1] then return 0 end
redis.call('DEL', KEYS[1])
return 1
