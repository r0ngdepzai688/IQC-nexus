# IQC Nexus Client Agent — Local Queue & Transport Recovery

## Overview

The IQC Nexus Client Agent operates a local SQLite queue (`SqliteLocalAgentQueue`) for offline resilience and crash recovery.

## Durability & Recovery Guarantees

1. **Local Queue Durability**: Queue job items (`LocalJobItem`) persist `ServerJobId`, `PayloadSubmissionId`, `Nonce`, and payload path across process restarts and SQLite file re-openings.
2. **Lease Management**: Worker threads acquire short-lived leases. Stale leases from crashed worker processes are automatically reclaimed after lease expiration.
3. **Lost Response Recovery**: If an upload HTTP response is lost in transit after server commitment, retrying the submission with identical `(PayloadSubmissionId, Nonce, CanonicalPayloadHash)` returns the original committed `UploadId` and `ServerImportJobId` without creating duplicate submissions or downstream jobs.
4. **Concurrent Worker Safety**: Multiple local worker threads processing duplicate responses safely converge on completing the single local queue job.
