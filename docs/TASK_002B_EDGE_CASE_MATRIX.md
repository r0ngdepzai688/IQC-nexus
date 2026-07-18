# Task 002B Synthetic Edge-Case Matrix

| Case | Coverage | Location |
|---|---|---|
| Canonical synthetic ID | `SYN-0001` through `SYN-0030` | `personnelMock.ts` |
| Reserved non-deliverable email domain | `example.invalid` | `personnelMock.ts`, `AuthContext.tsx` |
| Vietnamese Unicode | `Nguyá»…n An NhiÃªn`, `Tráº§n Minh Khoa`, `LÃª Gia HÃ¢n` | `personnelMock.ts` |
| Korean Unicode | `ê¹€í•˜ëŠ˜`, `ë°•ì„œì¤€` | `personnelMock.ts` |
| Optional field present | `preferredName` | `SYN-0026`, `SYN-0030` |
| Optional field absent | Most personnel records | `personnelMock.ts` |
| Active state | Multiple records | `status: Active` |
| Inactive state | `SYN-0007`, `SYN-0013`, `SYN-0019`, `SYN-0025`, `SYN-0030` | `personnelMock.ts` |
| Duplicate-key rejection | Verified by validation script using unique-ID check | Auto-fix validation |
| Invalid legacy ID | Rejected by absence checks for `EMP####` | Auto-fix validation |
| Legacy personnel names | Rejected by repository grep | Auto-fix validation |

Duplicate IDs and malformed records are not placed in the runtime fixture because doing so would make runtime data invalid. They are represented as validation conditions instead.