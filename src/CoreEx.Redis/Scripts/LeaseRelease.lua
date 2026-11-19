-- Deletes a synchronization lease only while the caller still owns it, so a stale owner can never release a successor.
-- KEYS[1]: lease key (string value is the owner token).
-- ARGV[1]: owner token.
-- Returns 1 when released; 0 when absent or owned by another caller.
if redis.call('GET', KEYS[1]) == ARGV[1] then
  return redis.call('DEL', KEYS[1])
end
return 0
