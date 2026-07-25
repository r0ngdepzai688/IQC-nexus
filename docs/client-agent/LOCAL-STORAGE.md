# IQC Nexus Client Agent — Local Storage & DPAPI Security

## Storage Locations

- **Data Directory**: `%LocalAppData%\IqcQmsAgent\` per logged-in Windows user.
- **Identity File**: `%LocalAppData%\IqcQmsAgent\device_identity.dpapi` protected by Windows DPAPI (`DataProtectionScope.CurrentUser`).
- **Local Queue Database**: `%LocalAppData%\IqcQmsAgent\local_queue.db` (SQLite database storing queue items).

## Single Instance Lock & HKCU Startup

- **Single Instance Lock**: Win32 Mutex `Global\IqcQmsClientAgent_<Profile>_<UserSID>` enforces a single active Client Agent process per logged-in user session.
- **Startup Registration**: Windows HKCU Run key `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` registers `IqcQms.ClientAgent.exe` for automatic user session startup.
