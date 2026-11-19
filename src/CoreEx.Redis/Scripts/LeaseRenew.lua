-- Extends a synchronization lease only while the caller still owns it.
-- KEYS[1]: lease key (string value is the owner token, random per acquisition).
-- ARGV[1]: owner token; ARGV[2]: lease duration (ms).
-- Returns 1 when renewed; 0 when absent or owned by another caller.
-- Arguments are validated before any write: Redis does not roll back a script that errors midway, and PEXPIRE <= 0 deletes the key.
local function posint(v)
  local n = tonumber(v)
  if not n or n <= 0 or n % 1 ~= 0 then return nil end
  return n
end
local lease = posint(ARGV[2])
if not lease then return redis.error_reply('Invalid lease duration') end
if redis.call('GET', KEYS[1]) == ARGV[1] then
  return redis.call('PEXPIRE', KEYS[1], lease)
end
return 0
