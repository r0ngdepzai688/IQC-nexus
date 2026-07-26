# IQC Nexus Client Agent — Operations & Recovery Runbook

## Routine Operations

- **Monitoring**: Check Portal UI at `/agent-devices` to view device online/offline states and last seen timestamps.
- **Service Restart**: Run `sc.exe stop IqcQmsClientAgent` followed by `sc.exe start IqcQmsClientAgent`.
- **Identity Reset**: Delete `%LocalAppData%\IqcQmsAgent\device_identity.dpapi` and re-pair using a new pairing code generated from Portal.
- **Device Revocation**: Admin clicks "Revoke" on Portal UI `/agent-devices`. Device credentials are immediately invalidated.

## Retention & Cleanup Operations

- **Payload Retention Options**: Configured via `AgentPayloadRetentionOptions` in appsettings:
  - `FullResultRetentionDays`: Default 90 days.
  - `ReplayTombstoneRetentionDays`: Default 365 days.
  - `CleanupBatchSize`: Default 100 records per batch.
  - `CleanupInterval`: Default 1 hour.
- **Retention Maintenance**: `AgentPayloadRetentionService` runs scheduled cleanup of expired submission records into replay tombstones, and purges tombstones past replay retention expiry.
- **Operational Risk Note**: Beyond `ReplayTombstoneRetentionDays`, replay protection terminates upon tombstone deletion.
