# IQC Nexus Client Agent — Payload Replay Protection & Idempotent Submission

## Overview

The IQC Nexus Client Agent guarantees strict atomic payload acceptance, idempotency, and replay protection for all workbook submissions.

## Core Submission Identifiers

- **`PayloadSubmissionId`**: High-entropy non-secret UUID (`agt_sub_<guid>`) generated ONCE per logical queued submission by the Client Agent before the first HTTP request. Persisted in the local SQLite queue and preserved across network retries and Agent restarts.
- **`Nonce`**: Cryptographically random value (`agt_nonce_<guid>`) generated once for the submission.
- **`SourceFingerprint`**: Deterministic SHA-256 hash calculated over normalized workbook content.

## Server Canonical Payload Digest

The server recomputes a deterministic SHA-256 digest (`CanonicalPayloadHash`) over an invariant UTF-8 JSON representation binding:
- `CanonicalSchemaVersion`
- `DeviceId`
- `PayloadSubmissionId`
- `Nonce`
- `ServerImportJobId`
- `SourceFingerprint`
- `NormalizedWorkbook` content.

## Idempotency & Replay Semantics

### 1. First Valid Submission
The server creates a record in `AgentPayloadSubmission` within an atomic database transaction. Returns `UploadId` with `IsDuplicateRetry = false`.

### 2. Safe Duplicate Retry
When a retry arrives with the **SAME `PayloadSubmissionId`**, **SAME `Nonce`**, and **SAME `CanonicalPayloadHash`**:
- The server recovers the committed `UploadId` and `ReceivedAtUtc`.
- Returns `IsDuplicateRetry = true`.
- No duplicate database record or downstream import action is triggered.

### 3. Submission Mismatch or Replay Attack
If a submission arrives with an existing `PayloadSubmissionId` or `Nonce` but a **DIFFERENT `CanonicalPayloadHash`**:
- The server rejects the submission with `InvalidOperationException` ("mismatch detected").
- Prevents payload tampering or cross-device replay.
