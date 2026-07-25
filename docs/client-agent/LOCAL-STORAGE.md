# IQC Nexus Client Agent — Local Storage Architecture (Phase 3A.4)

## Local Databases

1. **Queue Store**: `queue.db` (`SqliteLocalJobQueueStore`)
2. **Replay Tombstone Store**: `replay_tombstones.db` (`SqliteReplayTombstoneStore`)
3. **Execution State Store**: `nasca_state.db` (`SqliteNascaExecutionStateStore`)
   - Tracks durable state transitions (`NascaExecutionState`) for NASCA queue items.
   - Enforces atomic compare-and-set updates.
   - Stores NO secrets, NO raw access tokens, and NO workbook cell contents.
