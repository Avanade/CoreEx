-- Reads a work record and, unless a test clock override is configured, Redis server time in one primary-side operation.
-- Record hash fields: s = serialized work state; i = incarnation; v = version.
-- KEYS[1]: work record key.
-- ARGV[1]: 1 to return placeholders instead of Redis server time, otherwise 0.
-- Returns {state, incarnation, version, seconds, microseconds}; absent hash fields are returned as false.
local fields = redis.call('HMGET', KEYS[1], 's', 'i', 'v')
if ARGV[1] == '1' then return {fields[1] or false, fields[2] or false, fields[3] or false, 0, 0} end
local t = redis.call('TIME')
return {fields[1] or false, fields[2] or false, fields[3] or false, t[1], t[2]}
