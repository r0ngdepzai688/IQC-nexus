# IQC Nexus Client Agent — Operations & Recovery Runbook

## Routine Operations

- **Monitoring**: Check Portal UI at `/agent-devices` to view device online/offline states and last seen timestamps.
- **Service Restart**: Run `sc.exe stop IqcQmsClientAgent` followed by `sc.exe start IqcQmsClientAgent`.
- **Identity Reset**: Delete `%LocalAppData%\IqcQmsAgent\device_identity.dpapi` and re-pair using a new pairing code generated from Portal.
- **Device Revocation**: Admin clicks "Revoke" on Portal UI `/agent-devices`. Device credentials are immediately invalidated.
