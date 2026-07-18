# IQC Nexus Data Inventory

## 1. Phạm vi

Inventory này ghi metadata containment cho execution slice `002A` của Task `E01-T02`. Không chứa record, tên người, email, mã nhân viên, credential, đường dẫn máy cá nhân hoặc giả định về vị trí secure store.

Technical Owner cho phép development containment. Data Owner và Security sign-off vẫn là release gate.

## 2. Root artifact inventory

| Logical filename | Classification | Tracked trước 002A | Tracked sau 002A | Kích thước (bytes) | SHA-256 | Known reference | Disposition | Local retention status | Secure-store status | Data Owner | Security approver | Git-history decision |
|---|---|---:|---:|---:|---|---|---|---|---|---|---|---|
| `User_DB.xlsx` | Confidential — personnel master data; PII | Yes | No | 34,456 | `594228279417170EA8204BDEC5FF7A85432FBF7AD16BBA3DE7ECF276A6962F59` | API startup lookup; local extraction utility | Remove tracking; retain local pending organizational disposition | RETAINED LOCALLY; NOT DELETED DURING 002A | PENDING ORGANIZATIONAL APPROVAL | TBD before release | TBD before release | NO REWRITE DURING 002A; reassess before release |
| `users.json` | Confidential — personnel export; PII | Yes | No | 85,387 | `295993B8EC20F853B8EDC50939E6A8D40BA2EF316D99916DD74CE44D278C2ECC` | No direct runtime reference to root file identified | Remove tracking; retain local pending organizational disposition | RETAINED LOCALLY; NOT DELETED DURING 002A | PENDING ORGANIZATIONAL APPROVAL | TBD before release | TBD before release | NO REWRITE DURING 002A; reassess before release |
| `user_preview.json` | Confidential — personnel preview; PII | Yes | No | 2,352 | `8D0BB2230D6081B3681D51AE2990AA91854245CFBDB4C688EA527620D7752CD2` | No runtime reference identified | Remove tracking; retain local pending organizational disposition | RETAINED LOCALLY; NOT DELETED DURING 002A | PENDING ORGANIZATIONAL APPROVAL | TBD before release | TBD before release | NO REWRITE DURING 002A; reassess before release |
| `user_preview_utf8.json` | Confidential — personnel preview; PII | Yes | No | 1,173 | `04E0A5927E3BAC0A4EA06F02182BDA90E55C9CE1F096C748BAF48B06D4578588` | No runtime reference identified | Remove tracking; retain local pending organizational disposition | RETAINED LOCALLY; NOT DELETED DURING 002A | PENDING ORGANIZATIONAL APPROVAL | TBD before release | TBD before release | NO REWRITE DURING 002A; reassess before release |

## 3. Controls và release gates

- Root artifacts được remove khỏi Git index bằng `git rm --cached`; file local không bị xóa hoặc sửa nội dung.
- Ignore policy dùng rule path-specific cho ba JSON root và rule Excel hiện hữu; không dùng `*.json`.
- Source Data Hub và `docs/datahub/**` phải tiếp tục được track.
- Không rewrite Git history trong 002A.
- Trước release, tổ chức phải phê duyệt secure store, owner, least-privilege access, encryption, backup checksum verification và retention.
- Trước release, Data Owner phải xác nhận repository copy không phải bản duy nhất hoặc backup ngoài Git đã được kiểm chứng.
- Trước release, Security phải quyết định có cần history remediation bổ sung hay không.

## 4. Trạng thái 002A

- Development containment: completed after repository verification and commit.
- Local file deletion: not authorized and not performed.
- Secure backup/move: not performed by 002A.
- Organizational approval: pending release gate.
- Follow-up: Definition of Ready cho 002B; 002B chưa bắt đầu.
