# Quality Gate: Import Review & Preview Engine

**Target Subsystem:** `IqcQms.Application.DataPlatform.ImportPreviewEngine`

---

## Mandatory Criteria

* [x] **Mandatory Pre-Commit Contract**: Preview generation required before `ReadyForReview` state transition.
* [x] **Tamper-Evident Attestation**: SHA-256 fingerprint & HMAC-SHA256 signature generated on preview creation.
* [x] **Scoping & Security**: Scoped to `jobId`, `ownerUserId`, mapping profile version, and validation profile version.
* [x] **No Raw Confidential Data**: Attestation token and fingerprint string contain zero confidential workbook content.
* [x] **Representative Record Sampling**: Truncated sampling (default 50 records) displaying original normalized value vs mapped value.
* [x] **Explicit Deferred Commit**: Production DB persistence explicitly deferred without misrepresentation.
