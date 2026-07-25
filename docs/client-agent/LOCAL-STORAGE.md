# IQC Nexus Client Agent — Local Storage Architecture (Phase 3A.5)

## Storage Components

1. **Queue Store**: `queue.db` (`SqliteLocalJobQueueStore`)
2. **Replay Tombstone Store**: `replay_tombstones.db` (`SqliteReplayTombstoneStore`)
3. **Execution State Store**: `nasca_state.db` (`SqliteNascaExecutionStateStore`)
4. **Work Directories**: `%LocalAppData%\IqcQmsAgent\NascaWork\` managed by `INascaWorkDirectoryManager` (`NascaWorkDirectoryManager.cs`).
   - Opaque directory naming (`work_<correlationId>`).
   - Atomic manifest updates (`manifest.json.tmp` -> `manifest.json`).
   - Atomic input staging (`staging_<guid>.tmp` -> `input.dat`).
   - Quarantine isolation for corrupt or mismatched directories.
