# Import Preview Attestation Contract Specification

**Module:** Data Platform Import Engine  
**Version:** `1.0`  
**Security Standard:** HMAC-SHA256 Tamper-Evident Attestation  

---

## 1. Purpose

The Import Preview Attestation Contract establishes a server-generated, tamper-evident cryptographic fingerprint for normalized, mapped, and validated workbook data prior to future commit execution.

---

## 2. Attestation Schema

```json
{
  "jobId": "job-001",
  "ownerUserId": "alex.engineer",
  "contentFingerprint": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
  "mappingProfileVersion": "1.0",
  "validationProfileVersion": "1.0",
  "generatedAt": "2026-07-24T00:15:00Z",
  "expiresAt": "2026-07-24T02:15:00Z",
  "signature": "a1b2c3d4..."
}
```

---

## 3. Fingerprint & Signature Generation

1. **Fingerprint Source String**:
   `{JobId}:{OwnerUserId}:{MappingProfileId}:{MappingProfileVersion}:{ValidationProfileId}:{ValidationProfileVersion}:{MappedRecordCount}:{WarningCount}:{ErrorCount}:{BlockingErrorCount}`
2. **Fingerprint Hash**: `SHA256(FingerprintSourceString)`
3. **Signature Source String**:
   `{JobId}:{OwnerUserId}:{ContentFingerprint}:{GeneratedAtUnixSeconds}:{ExpiresAtUnixSeconds}`
4. **Signature Hash**: `HMACSHA256(SignatureSourceString, SecretKey)`

---

## 4. Verification Rules

* Verification succeeds if and only if:
  1. `GeneratedAt` <= `UtcNow` <= `ExpiresAt`.
  2. The computed HMAC signature matches `signature` exactly.
  3. The request actor matches `ownerUserId` (or possesses `import.admin` policy).

---

## 5. Security & Confidentiality Safeguards

* No raw cell values, source paths, or confidential customer identifiers are present in the fingerprint string or attestation token.
* All preview records sampled use sanitized DTO representations.
