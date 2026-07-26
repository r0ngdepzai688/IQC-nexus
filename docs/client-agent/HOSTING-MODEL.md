# IQC Nexus Client Agent — Interactive Per-User Hosting Model

## Architecture Correction & Strategy

The production Client Agent MUST run inside the logged-in interactive Windows user's session.

### Why Windows Service Mode is NOT the Primary NASCA Execution Model
Future NASCA extraction and Excel COM automation depend on:
- User's interactive Windows Desktop Session (Session 1+)
- User's DPAPI `CurrentUser` encryption key scope
- User's mapped network drives and Windows user credentials
- User's Excel installation, COM registry registration, and Office license activation context

Executing as a Windows Service under Session 0, `LocalSystem`, `NetworkService`, or a service account is **incompatible** with NASCA/Excel extraction and is disabled by default.

## Hosting Implementation

1. **Interactive Background Process**: The default agent executable runs under the logged-in user account.
2. **Single-Instance Enforcement**: Named Mutex (`Local\IqcQmsClientAgent_Profile_<Hash>`) ensures only one instance per profile runs per user session.
3. **No Inbound Listeners**: Outbound HTTPS calls only.
4. **No Privilege Elevation**: Runs without administrative privileges.
5. **Startup Registration**: Per-user HKCU Run key (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`) with safely quoted executable paths and profile arguments.
