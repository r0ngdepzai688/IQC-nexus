# Task 002B Synthetic Edge-Case Matrix

| Case | Coverage | Location / expected behavior |
|---|---|---|
| Canonical synthetic ID | `SYN-0001` through `SYN-0030` | `fixtures/personnel.synthetic.json`; accept `^SYN-[A-Z0-9-]+$`, reject non-canonical case. |
| Reserved non-deliverable email domain | `example.invalid` | Canonical fixture and `AuthContext.tsx`; reject other fixture domains. |
| Vietnamese Unicode | `Nguyễn An Nhiên`, `Trần Minh Khoa`, `Lê Gia Hân` | Preserve NFC UTF-8 without mojibake. |
| Korean Unicode | `김하늘`, `박서준` | Preserve NFC UTF-8 without mojibake. |
| Optional field present | `preferredName` | `SYN-0026`, `SYN-0030`; preserve in frontend. |
| Optional field absent/null | Most personnel records and optional backend fields | Canonical null is valid; adapters convert only at compatibility boundaries. |
| Active state | Multiple records | Require `accountStatus: Active` and `isActive: true`. |
| Inactive state | `SYN-0007`, `SYN-0013`, `SYN-0019`, `SYN-0025`, `SYN-0030` | Require `isActive: false`; frontend maps to `Inactive`. |
| Pending state | `SYN-0028` | Require `isActive: false`; frontend maps to `Inactive`. |
| Locked state | `SYN-0029` | Require `isActive: false`; frontend maps to `Inactive`. |
| Duplicate-key rejection | Unique employee ID and non-null Knox ID checks | Reject the complete fixture before mutation. |
| Invalid legacy ID | Rejected by absence checks for `EMP####` | Repository grep and validator case/pattern checks. |
| Legacy personnel names | Rejected by repository grep | Verification commands in consumer inventory. |
| Invalid input | Lowercase ID, status contradiction and deliverable email negative tests | Reject with record/field diagnostics; do not partially seed. |
| Missing credential | Startup safety check | Validate fixture, then permit migration but skip personnel mutation with a non-secret warning. |

Duplicate IDs and malformed records are not placed in the runtime fixture because doing so would make runtime data invalid. They are represented as validation conditions and focused negative tests instead.
