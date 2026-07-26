# Review & Preview Attestation Contract

## Overview

Before any commit action can occur, an import job must generate a mandatory Preview Attestation.

```
(MappingResult + ValidationResult) -> ImportPreviewEngine -> ImportPreviewDetail (with Attestation)
```

## Attestation Fingerprint Architecture

The attestation token proves that the reviewed content matches the exact mapped and validated state:

* **Fingerprint Source**: `{JobId}:{OwnerUserId}:{MappingProfileId}:{MappingProfileVersion}:{ValidationProfileId}:{ValidationProfileVersion}:{MappedRecordCount}:{WarningCount}:{ErrorCount}:{BlockingErrorCount}`
* **Content Fingerprint**: SHA-256 hash of Fingerprint Source.
* **Signature**: HMAC-SHA256 signature using application secret key.

## Invariants

1. **Mandatory Requirement**: Preview must be generated before `ReadyForReview` state transition.
2. **Owner Scoping**: Attestation is strictly bound to `JobId` and `OwnerUserId`.
3. **No Confidential Data Leakage**: Hashes contain no confidential business payload strings.
4. **Deferred Commit**: Commitment to production database is deferred in this milestone.
