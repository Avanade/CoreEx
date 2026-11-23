-- DatabaseOperationStoreBase supplies the explicit transaction. Finalize new-row timestamps after any conflicting insert has resolved.
INSERT INTO {{Table}} (key_hash, operation_id, domain_name, tenant_id, operation_name, instance_id, status, initiator_id, attempt, data, created_on, updated_on, abandon_after)
VALUES (@KeyHash, @OperationId, @DomainName, @TenantId, @OperationName, @InstanceId, 1, @InitiatorId, 0, @Data, statement_timestamp(), statement_timestamp(), statement_timestamp() + (@InactivityMs * interval '1 millisecond'))
ON CONFLICT (key_hash) DO NOTHING;

UPDATE {{Table}} SET created_on = statement_timestamp(), updated_on = statement_timestamp(), abandon_after = statement_timestamp() + (@InactivityMs * interval '1 millisecond')
WHERE key_hash = @KeyHash AND operation_id = @OperationId;

SELECT {{Columns}}, CASE WHEN operation_id = @OperationId THEN 1 ELSE 0 END AS created FROM {{Table}} WHERE key_hash = @KeyHash;
