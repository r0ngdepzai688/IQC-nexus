# IQC Nexus New Model Master Plan MVP Execution Plan

## 1. Quyết định MVP

MVP nhỏ nhất tạo giá trị thực tế là một **controlled internal UAT release** cho đúng một lát dọc:

```text
Đăng nhập → phân quyền → upload Excel thủ công qua Data Hub
→ raw archive → staging → validation → review
→ commit có audit/lineage → hiển thị Master Plan
```

MVP không phải bản production diện rộng. Nó phục vụ nhóm người dùng nội bộ được chỉ định trong UAT, trên một nguồn dữ liệu Master Plan và một contract Excel đã phê duyệt. MVP phải dùng dữ liệu thật đã kiểm soát hoặc dữ liệu UAT được mask; không dùng mock để chứng minh hoàn thành.

### Giá trị người dùng

- Data Steward có thể đưa Master Plan Excel vào hệ thống thay vì xử lý thủ công rời rạc.
- Lỗi và ambiguity được nhìn thấy, phân công/xử lý trước khi ghi dữ liệu core.
- Người có quyền có thể commit dữ liệu hợp lệ; hành động được audit và truy vết về file/dòng nguồn.
- Người dùng được phân quyền có thể xem danh sách Master Plan đã commit.

### Tiêu chí thành công cấp MVP

1. Người dùng không xác thực không truy cập được dữ liệu hoặc Data Hub.
2. Data Steward upload được file Excel trong contract và nhận batch receipt.
3. File được archive immutable; các dòng đi qua staging và validation.
4. Dòng lỗi/ambiguous không thể commit trước khi review/revalidation hợp lệ.
5. Commit retry không tạo duplicate; mọi record có audit và lineage.
6. Master Plan UI chỉ hiển thị dữ liệu core đã commit qua API thật.
7. Lát dọc vượt build, type-check, lint, unit/integration/E2E và UAT theo role.

## 2. Giới hạn phạm vi

### Trong MVP

- Login thật bằng OIDC/BFF theo kiến trúc mục tiêu.
- Permission tối thiểu: Master Plan Viewer, Data Steward/Reviewer, Data Committer và Administrator.
- Manual Excel upload qua web; một source/profile và một data contract Master Plan.
- Raw archive, staging, deterministic header detection, mapping, validation, review, commit, audit/lineage.
- SQL Server UAT và migration dữ liệu cần thiết từ SQLite.
- Master Plan list/detail tối thiểu, pagination không bắt buộc nếu dữ liệu UAT nằm trong giới hạn đã chốt.
- CI, môi trường UAT, telemetry tối thiểu và feature flag/rollback cho MVP.

### Ngoài MVP

- Project activation/workspace, Tasks, Chat, Standards, HR và notification workflow tổng quát.
- Windows Agent và watched folder.
- Connector SDK tổng quát, REST connector thứ hai hoặc future connectors.
- Full replay/dead-letter operator platform; MVP chỉ yêu cầu retry/idempotency/rollback ở commit và lưu raw để có thể phục hồi thủ công có kiểm soát.
- Design-system migration, app-shell redesign, i18n framework tổng quát; chỉ sửa UI cần thiết cho luồng MVP.
- Production-wide pilot/hypercare, SLO dashboard tổng quát, DR exercise toàn hệ thống và legacy retirement.

## 3. Task bắt buộc và task có thể hoãn

### 3.1 Task bắt buộc

MVP gồm **32/53 task**, tổng estimate danh nghĩa khoảng **276 person-days** chưa tính thời gian chờ IdP/DBA/UAT và maintenance window.

| Nhóm | Task bắt buộc |
|---|---|
| Baseline | 001, 002, 003, 004, 005, 006 |
| Security/API/environment | 007, 008, 009, 010, 014, 015, 016, 017, 018, 019, 020, 024 |
| Canonical model/SQL/audit | 022, 025, 026, 027, 028, 029 |
| Data Hub MVP | 030, 031, 032, 035, 036, 037, 038 |
| Acceptance | 050 |

### 3.2 Task hoãn

| Task | Lý do hoãn khỏi MVP |
|---|---|
| 011, 012, 013, 023, 049 | Design tokens, i18n framework, component catalog, app shell và UI migration không cần để chứng minh luồng nghiệp vụ; UI MVP chỉ sửa tối thiểu, vẫn phải type/lint/a11y cơ bản. |
| 021 | Pagination tổng quát hoãn nếu dataset UAT nằm dưới giới hạn đã ký; API MVP vẫn phải có hard maximum để tránh trả vô hạn. |
| 033 | Connector SDK/reference API connectors tổng quát không cần cho manual web upload một nguồn. |
| 034, 040, 041 | Windows Agent và fleet management nằm ngoài manual-upload MVP. |
| 039 | Replay/dead-letter/quarantine operator platform đầy đủ hoãn; raw immutable và commit idempotency vẫn bắt buộc trong Task 031/038. |
| 042, 043, 044 | Health/runbook tổng quát, SLO platform và DR exercise toàn hệ thống hoãn tới production readiness; MVP vẫn có logs/traces, UAT backup/cutover rollback từ Task 009/029. |
| 045 | Project activation nằm sau bước hiển thị Master Plan, ngoài lát MVP. |
| 046, 047, 048 | Tasks, Chat và notification/approval workflow tổng quát ngoài phạm vi. |
| 051 | Production pilot/hypercare bắt đầu sau khi MVP UAT được chấp nhận. |
| 052, 053 | Xóa mock/legacy cần stability window; không phải điều kiện tạo MVP UAT. MVP không được gọi mock trong lát Master Plan nhưng chưa xóa toàn repository. |

Không được tự động kéo task hoãn vào MVP. Nếu một task hoãn trở thành blocker thật, phải cập nhật scope/estimate và xin xác nhận trước.

## 4. Dependency và thứ tự triển khai

```text
001 → 002 → 004 ─┐
001 → 003 ───────┼→ 005 → 006 → 007
                 └→ 008 → 009 → 010

007 → 014 → 015 → 016 → 017 → 018
008 + 014 → 019
008 → 020 → 022
009 + 010 → 024

022 → 025 → 027 → 028 → 029
017 + 025 → 026

008 + 025 + 029 → 030 → 031
030 → 032 → 035 → 036 → 037 → 038
018 + 031 hỗ trợ ranh giới upload
026 + 038 bảo đảm audit/lineage

Tất cả task trên + deploy UAT → 050
```

Thứ tự đánh số dưới đây là thứ tự ưu tiên dependency. Frontend, backend và platform có thể chạy song song khi dependency đã đạt, nhưng mỗi agent vẫn chỉ xử lý một task theo `AI_RULEBOOK.md`.

## 5. Kế hoạch thực hiện từng task MVP

### MVP Task 01 — Task 001 / E01-T01: Đưa source Data Hub vào version control

- **Mục tiêu:** clone sạch chứa đủ source Data Hub để build và triển khai.
- **Lý do bắt buộc:** pipeline MVP hiện phụ thuộc file local đang bị ignore; thiếu task này thì CI/UAT không tái lập được.
- **Dependency:** không.
- **Tiêu chí hoàn thành có thể kiểm chứng:** clone mới có toàn bộ entities/interfaces/services Data Hub; `git check-ignore` chỉ match runtime/raw data; backend build xanh từ clone sạch.
- **Lệnh build/test cần chạy:** `dotnet restore backend/IqcQms.sln`; `dotnet build backend/IqcQms.sln --no-restore`; `git check-ignore -v <các đường dẫn DataHub kiểm tra>`.
- **Kế hoạch rollback:** revert thay đổi `.gitignore` và file tracking; không xóa bản local/raw cho tới khi clone verification và backup hoàn tất.

### MVP Task 02 — Task 002 / E01-T02: Phân loại và loại dữ liệu/binary khỏi repository

- **Mục tiêu:** không đưa PII, DB, Excel hoặc export thật vào artifact/test MVP.
- **Lý do bắt buộc:** MVP xử lý danh sách nhân viên và file Master Plan; rò PII làm UAT không thể chấp nhận.
- **Dependency:** MVP Task 01.
- **Tiêu chí hoàn thành có thể kiểm chứng:** inventory có owner/classification/retention; fixtures synthetic thay dữ liệu thật; secret/PII scan không phát hiện dữ liệu hoạt động trong artifact.
- **Lệnh build/test cần chạy:** `git status --short`; CI secret/PII scan đã cấu hình; `dotnet test backend/IqcQms.sln`; `npm test --if-present` trong `frontend`.
- **Kế hoạch rollback:** khôi phục từ backup bảo mật ngoài Git; revert fixture/ignore changes nếu test mất coverage, tuyệt đối không recommit PII.

### MVP Task 03 — Task 003 / E01-T04: Làm xanh TypeScript và ESLint

- **Mục tiêu:** frontend có baseline compile/lint xanh trước khi nối auth và API thật.
- **Lý do bắt buộc:** không thể xác nhận Master Plan UI nếu type/lint đang lỗi sẵn.
- **Dependency:** MVP Task 01.
- **Tiêu chí hoàn thành có thể kiểm chứng:** TypeScript no-emit và ESLint exit code 0; các route login, Data Hub, Master Plan render trong smoke test.
- **Lệnh build/test cần chạy:** trong `frontend`: `npx tsc --noEmit --incremental false`; `npm run lint`; `npm run build`; `npm test --if-present`.
- **Kế hoạch rollback:** revert theo cụm lỗi; không tắt strict/rule; giữ patch nhỏ để xác định regression.

### MVP Task 04 — Task 004 / E01-T03: Chuẩn hóa UTF-8 và mojibake

- **Mục tiêu:** tên người dùng, header Excel và giá trị Việt/Hàn round-trip đúng.
- **Lý do bắt buộc:** encoding sai làm header/mapping/identity validation sai ngay trong luồng MVP.
- **Dependency:** MVP Task 02.
- **Tiêu chí hoàn thành có thể kiểm chứng:** corpus Việt/Hàn parse và render đúng; source MVP UTF-8; không mojibake trong login/import/review/Master Plan.
- **Lệnh build/test cần chạy:** `dotnet test backend/IqcQms.sln`; frontend `npx tsc --noEmit --incremental false`; tests parser/Unicode được thêm và chạy trong suite.
- **Kế hoạch rollback:** revert từng file/corpus; giữ original bytes của fixtures trong backup; không chuyển encoding hàng loạt ngoài phạm vi.

### MVP Task 05 — Task 005 / E01-T05: Test pyramid tối thiểu

- **Mục tiêu:** tạo safety net cho auth và Data Hub end-to-end.
- **Lý do bắt buộc:** các task sau thay security, schema và commit; không có tests sẽ không phân biệt regression với lỗi cũ.
- **Dependency:** MVP Task 02, 03.
- **Tiêu chí hoàn thành có thể kiểm chứng:** CI-ready unit/integration/E2E harness; có test login denial, upload→staging, validation/review gate, commit và Master Plan display.
- **Lệnh build/test cần chạy:** `dotnet test backend/IqcQms.sln`; trong `frontend`: `npm test --if-present`; lệnh E2E được project thiết lập và ghi trong package scripts/README.
- **Kế hoạch rollback:** revert test infrastructure theo layer; giữ synthetic fixtures; không xóa test chỉ vì implementation chưa pass.

### MVP Task 06 — Task 006 / E01-T06: CI build, scan và artifact pipeline

- **Mục tiêu:** mọi MVP build từ main có evidence và cùng artifact được promote.
- **Lý do bắt buộc:** UAT phải tái lập, không deploy từ máy cá nhân.
- **Dependency:** MVP Task 03, 05.
- **Tiêu chí hoàn thành có thể kiểm chứng:** PR gates chạy restore/build/type/lint/test/scan; main tạo versioned artifact và SBOM; gate fail chặn promote.
- **Lệnh build/test cần chạy:** chạy local tương đương pipeline: `dotnet build backend/IqcQms.sln`; `dotnet test backend/IqcQms.sln`; frontend `npm ci`, `npx tsc --noEmit --incremental false`, `npm run lint`, `npm run build`.
- **Kế hoạch rollback:** promote last-known-good artifact; revert pipeline change; không bypass required checks để release.

### MVP Task 07 — Task 007 / E02-T01: Secret management

- **Mục tiêu:** xoay JWT key lộ và quản lý IdP/DB/storage secrets ngoài Git.
- **Lý do bắt buộc:** login/phân quyền không có giá trị nếu token hoặc connection secret có thể giả mạo.
- **Dependency:** MVP Task 06.
- **Tiêu chí hoàn thành có thể kiểm chứng:** secret cũ bị revoke; tracked configs chỉ chứa placeholder/reference; rotation test thành công; scan không thấy secret hoạt động.
- **Lệnh build/test cần chạy:** `dotnet build backend/IqcQms.sln`; `dotnet test backend/IqcQms.sln`; CI secret scan; smoke startup bằng UAT secret provider.
- **Kế hoạch rollback:** kích hoạt last-known-good secret version trong vault và rollback config artifact; revoke credential bị nghi ngờ sau ổn định.

### MVP Task 08 — Task 008 / E03-T01: API v1 và Problem Details

- **Mục tiêu:** contract ổn định cho auth, Data Hub và Master Plan UI.
- **Lý do bắt buộc:** tránh frontend viết thêm fetch/DTO song song và bảo đảm lỗi review/commit có thể xử lý.
- **Dependency:** MVP Task 05.
- **Tiêu chí hoàn thành có thể kiểm chứng:** API MVP có version, DTO, validation, Problem Details/error code/traceId; OpenAPI và contract tests xanh.
- **Lệnh build/test cần chạy:** `dotnet build backend/IqcQms.sln`; `dotnet test backend/IqcQms.sln`; generate OpenAPI và chạy OpenAPI diff command được CI định nghĩa.
- **Kế hoạch rollback:** giữ compatibility route/adapter cũ trong UAT; rollback API artifact, không đổi contract âm thầm.

### MVP Task 09 — Task 009 / E09-T01: Logging và OpenTelemetry tối thiểu

- **Mục tiêu:** theo dõi request từ UI qua upload/validation/commit bằng traceId mà không log PII.
- **Lý do bắt buộc:** feature flags, UAT và xử lý lỗi batch cần evidence; Task 024 phụ thuộc task này.
- **Dependency:** MVP Task 08; observability endpoint UAT.
- **Tiêu chí hoàn thành có thể kiểm chứng:** trace web/API và upload→commit xem được; redaction test xanh; log có operation/outcome/duration, không chứa token/file content.
- **Lệnh build/test cần chạy:** `dotnet test backend/IqcQms.sln`; frontend tests liên quan correlation; smoke request và xác nhận trace qua collector UAT.
- **Kế hoạch rollback:** tắt exporter bằng config và giữ structured local logs; rollback instrumentation nếu gây latency, không tắt audit.

### MVP Task 10 — Task 010 / E10-T01: Môi trường DEV/TEST/UAT

- **Mục tiêu:** có UAT TLS/config/data-mask để người dùng thật kiểm thử MVP.
- **Lý do bắt buộc:** “giá trị sử dụng thực tế” cần môi trường dùng được ngoài máy developer.
- **Dependency:** MVP Task 06, 07.
- **Tiêu chí hoàn thành có thể kiểm chứng:** cùng artifact deploy DEV/TEST/UAT; TLS/config validation xanh; UAT không dùng PII thô; smoke health/login thực hiện được.
- **Lệnh build/test cần chạy:** pipeline deploy dry-run; automated UAT smoke command; `dotnet test backend/IqcQms.sln`; frontend production build.
- **Kế hoạch rollback:** redeploy artifact/config UAT trước đó; DNS/proxy rollback; database chưa cutover ở task này.

### MVP Task 11 — Task 014 / E02-T02: OIDC/BFF login

- **Mục tiêu:** login/logout/session thật qua identity provider doanh nghiệp.
- **Lý do bắt buộc:** đăng nhập là bước đầu của lát MVP và token giả/localStorage không thể dùng cho UAT thật.
- **Dependency:** MVP Task 07; IdP sandbox, redirect URI và claims sample.
- **Tiêu chí hoàn thành có thể kiểm chứng:** login/logout, idle/absolute expiry, revoke và callback hoạt động; cookie HttpOnly/Secure/SameSite; CSRF tests xanh; không access token trong localStorage.
- **Lệnh build/test cần chạy:** `dotnet test backend/IqcQms.sln`; frontend type/lint/build; E2E auth login/logout/expired-session; security-header/cookie assertions.
- **Kế hoạch rollback:** feature/config switch về last-known-good UAT auth adapter; revoke UAT sessions; không phục hồi fake-token path.

### MVP Task 12 — Task 015 / E02-T03: Loại mock login/dev identity khỏi production path

- **Mục tiêu:** chỉ authenticated session thật truy cập luồng MVP.
- **Lý do bắt buộc:** nếu fake token còn hiệu lực thì phân quyền UAT không thể kiểm chứng.
- **Dependency:** MVP Task 11.
- **Tiêu chí hoàn thành có thể kiểm chứng:** fake token bị từ chối; anonymous redirect đúng; dev override không có trong UAT/production bundle; E2E auth xanh.
- **Lệnh build/test cần chạy:** frontend `npx tsc --noEmit --incremental false`, `npm run lint`, `npm run build`; E2E negative tests với fake/missing/expired session.
- **Kế hoạch rollback:** rollback UI/session adapter artifact; giữ OIDC server enforcement, không khôi phục bypass.

### MVP Task 13 — Task 016 / E02-T04: Permission catalogue MVP

- **Mục tiêu:** định nghĩa Viewer, Data Steward/Reviewer, Committer và Administrator cùng scope Master Plan.
- **Lý do bắt buộc:** upload, review, commit và view là các quyền khác nhau; UI visibility không đủ bảo mật.
- **Dependency:** MVP Task 11; Product/Security sign-off.
- **Tiêu chí hoàn thành có thể kiểm chứng:** permission catalogue/version/owner; role→permission mapping ký duyệt; test fixtures cho mọi persona; không còn tên role mâu thuẫn trong lát MVP.
- **Lệnh build/test cần chạy:** `dotnet test backend/IqcQms.sln` với permission matrix tests; frontend type-check cho capability types.
- **Kế hoạch rollback:** rollback mapping/config version trước; không xóa audit role changes; session/claims refresh sau rollback.

### MVP Task 14 — Task 017 / E02-T05: Authorization deny-by-default

- **Mục tiêu:** khóa mọi endpoint/hub MVP bằng authenticated permission và resource scope.
- **Lý do bắt buộc:** ngăn bypass UI để upload/review/commit hoặc đọc Master Plan.
- **Dependency:** MVP Task 13.
- **Tiêu chí hoàn thành có thể kiểm chứng:** 100% endpoint MVP có policy hoặc explicit anonymous; ma trận 401/403/allowed tests cho bốn persona xanh; IDOR/scope negative tests xanh.
- **Lệnh build/test cần chạy:** `dotnet test backend/IqcQms.sln`; API integration tests cho login, upload, staging, review, commit và records; frontend E2E role visibility.
- **Kế hoạch rollback:** rollback policy artifact/mapping version nhưng không bỏ deny-by-default; nếu mapping lỗi, khóa commit và cho phép read-only có kiểm soát.

### MVP Task 15 — Task 018 / E02-T06: Bảo vệ upload và abuse paths

- **Mục tiêu:** chặn path traversal, oversize, spoofing và file không hợp lệ.
- **Lý do bắt buộc:** MVP nhận file từ người dùng và chạm filesystem/raw storage.
- **Dependency:** MVP Task 14; file type/size/origin policy được duyệt.
- **Tiêu chí hoàn thành có thể kiểm chứng:** canonical path/allowlist; extension+magic-byte+size validation; rate limit; traversal/oversize/spoof tests xanh; High/Critical findings bằng 0.
- **Lệnh build/test cần chạy:** `dotnet test backend/IqcQms.sln`; upload integration/security tests với valid/invalid/traversal/oversize files; UAT CORS smoke.
- **Kế hoạch rollback:** disable upload endpoint bằng feature flag và giữ read-only Master Plan; rollback validator config theo version, không mở rộng allowlist tạm.

### MVP Task 16 — Task 019 / E03-T02: Generated frontend API client

- **Mục tiêu:** login/Data Hub/Master Plan dùng một client typed, không hard-code URL.
- **Lý do bắt buộc:** loại drift contract và các fetch riêng hiện có trong đúng lát MVP.
- **Dependency:** MVP Task 08, 11.
- **Tiêu chí hoàn thành có thể kiểm chứng:** 100% request trong các trang MVP đi qua client chung; không localhost hard-code; auth/correlation/error mapping tự động; OpenAPI diff gate xanh.
- **Lệnh build/test cần chạy:** frontend `npx tsc --noEmit --incremental false`, `npm run lint`, `npm run build`, component/E2E tests; OpenAPI client generation/diff command.
- **Kế hoạch rollback:** giữ adapter compatibility sau interface chung; rollback generated client/package cùng frontend artifact, không tái tạo fetch rời.

### MVP Task 17 — Task 020 / E03-T04: Application/module boundaries cho lát MVP

- **Mục tiêu:** Auth, Data Hub, New Models controllers mỏng và use cases nằm trong Application/Domain.
- **Lý do bắt buộc:** các task MVP sẽ thay transaction/schema; tiếp tục đặt logic trong controller tạo nợ kỹ thuật ngay trong lõi mới.
- **Dependency:** MVP Task 08; behavior tests baseline.
- **Tiêu chí hoàn thành có thể kiểm chứng:** controllers MVP không truy cập DbContext trực tiếp; architecture tests chặn dependency sai; behavior tests trước/sau tương đương.
- **Lệnh build/test cần chạy:** `dotnet build backend/IqcQms.sln`; `dotnet test backend/IqcQms.sln`; architecture test suite.
- **Kế hoạch rollback:** revert use-case wiring theo vertical slice; giữ API contract; rollback một module/lát thay vì toàn backend.

### MVP Task 18 — Task 022 / E03-T05: Canonical Master Plan model

- **Mục tiêu:** chọn một canonical MasterPlan để commit và display; khóa legacy path trong lát MVP.
- **Lý do bắt buộc:** hai model `MasterPlanRecord`/`MasterPlan` có thể làm commit và UI đọc hai nguồn khác nhau.
- **Dependency:** MVP Task 17; Product ký business key và merge rules.
- **Tiêu chí hoàn thành có thể kiểm chứng:** một canonical aggregate/business key; data reconciliation; upload/commit/display dùng cùng ID/model; legacy endpoint không được UI MVP gọi.
- **Lệnh build/test cần chạy:** `dotnet test backend/IqcQms.sln`; API contract/integration tests; frontend Master Plan E2E; migration/data reconciliation tests.
- **Kế hoạch rollback:** compatibility adapter và legacy table giữ read-only; rollback app artifact; không xóa legacy schema trong MVP.

### MVP Task 19 — Task 024 / E10-T02: Feature flag và rollback MVP

- **Mục tiêu:** bật MVP theo UAT cohort và có thể tắt upload/commit độc lập.
- **Lý do bắt buộc:** MVP thay auth, DB và commit; cần giới hạn blast radius và rollback không mất read access.
- **Dependency:** MVP Task 09, 10.
- **Tiêu chí hoàn thành có thể kiểm chứng:** flags có owner/expiry/audit; tách read/upload/review/commit; canary smoke và rollback rehearsal thành công.
- **Lệnh build/test cần chạy:** backend/frontend tests cho flag states; deployment smoke với flags on/off; UAT rollback rehearsal.
- **Kế hoạch rollback:** tắt write flags trước, giữ Master Plan read-only; redeploy last-known-good artifact nếu cần.

### MVP Task 20 — Task 025 / E04-T01: SQL Server schema và constraints

- **Mục tiêu:** schema SQL Server cho IAM, Data Hub, audit và canonical Master Plan có integrity/concurrency.
- **Lý do bắt buộc:** Data Hub tasks có dependency schema; UAT nhiều người dùng cần FK, unique key, rowversion và transaction đáng tin cậy.
- **Dependency:** MVP Task 18; SQL instance/DBA.
- **Tiêu chí hoàn thành có thể kiểm chứng:** ERD/DDL review; FK/unique index/business key/rowversion đúng; migration chạy trên DB rỗng và snapshot; Unicode/date types được test.
- **Lệnh build/test cần chạy:** `dotnet build backend/IqcQms.sln`; `dotnet test backend/IqcQms.sln`; `dotnet ef migrations script --idempotent --project backend/src/IqcQms.Infrastructure --startup-project backend/src/IqcQms.Api`.
- **Kế hoạch rollback:** dùng expand-compatible schema; rollback app trước; schema destructive không nằm trong MVP; restore UAT snapshot nếu migration thất bại.

### MVP Task 21 — Task 026 / E09-T02: Business audit MVP

- **Mục tiêu:** audit login, permission/config, review, commit và Master Plan changes.
- **Lý do bắt buộc:** review/override/commit dữ liệu chất lượng phải có trách nhiệm và truy vết.
- **Dependency:** MVP Task 14, 20; event catalogue/retention được duyệt.
- **Tiêu chí hoàn thành có thể kiểm chứng:** critical-event coverage 100% cho lát MVP; actor/action/before-after/reason/traceId; runtime không update/delete audit; Auditor query được bảo vệ.
- **Lệnh build/test cần chạy:** `dotnet test backend/IqcQms.sln` với audit coverage/immutability/authorization tests; API integration tests cho review/commit.
- **Kế hoạch rollback:** giữ audit table append-only; rollback viewer/writer adapter, không xóa audit records; disable write operations nếu audit unavailable.

### MVP Task 22 — Task 027 / E04-T02: Tách DB migration khỏi startup

- **Mục tiêu:** UAT schema được deploy bằng pipeline có approval, không từ app startup.
- **Lý do bắt buộc:** MVP chuyển SQL Server và không được chạy trên schema nửa vời.
- **Dependency:** MVP Task 06, 20.
- **Tiêu chí hoàn thành có thể kiểm chứng:** production/UAT app không gọi auto-migrate; pipeline backup→migrate→verify; runtime account không có DDL; migration fail chặn rollout.
- **Lệnh build/test cần chạy:** generate/apply idempotent script trên DB test; `dotnet test backend/IqcQms.sln`; deployment dry-run.
- **Kế hoạch rollback:** restore pre-migration UAT backup hoặc apply approved compensation; redeploy compatible app artifact.

### MVP Task 23 — Task 028 / E04-T03: SQLite → SQL Server migration/reconciliation

- **Mục tiêu:** chuyển users, required Data Hub history và canonical Master Plan có đối soát.
- **Lý do bắt buộc:** login/reference validation và dữ liệu Master Plan UAT cần nguồn SQL nhất quán.
- **Dependency:** MVP Task 02, 20.
- **Tiêu chí hoàn thành có thể kiểm chứng:** migration rerun không duplicate; count/checksum/business-key/Unicode-date reconciliation; 100% row hoặc exception có disposition.
- **Lệnh build/test cần chạy:** migration tool dry-run và apply trên snapshot; `dotnet test backend/IqcQms.sln`; reconciliation command/report; SQL constraint tests.
- **Kế hoạch rollback:** target SQL UAT được rebuild từ verified backup/snapshot; SQLite nguồn giữ read-only; không sửa nguồn trong migration.

### MVP Task 24 — Task 029 / E04-T04: SQL Server rehearsal và UAT cutover

- **Mục tiêu:** chuyển runtime MVP sang SQL Server có freeze/rollback và restore đã thử.
- **Lý do bắt buộc:** schema/tool chưa tạo giá trị nếu ứng dụng MVP vẫn chạy SQLite.
- **Dependency:** MVP Task 19, 22, 23; Operations/business freeze sign-off.
- **Tiêu chí hoàn thành có thể kiểm chứng:** hai rehearsal; reconciliation ký; RPO ≤15 phút/RTO ≤4 giờ; rollback test; SQLite cũ read-only.
- **Lệnh build/test cần chạy:** full CI suite; cutover smoke login/upload/read; backup restore verification; reconciliation report; frontend E2E MVP read path.
- **Kế hoạch rollback:** tắt write flags; chuyển connection/config về SQLite read-only/last-known-good theo rehearsal; restore SQL backup; reconcile writes trước mở lại.

### MVP Task 25 — Task 030 / E05-T01: Source Registry và ingestion envelope

- **Mục tiêu:** đăng ký đúng một Master Plan source/profile/contract và tạo receipt versioned.
- **Lý do bắt buộc:** upload phải có owner, source identity, schema/profile version và SLA thay vì metadata hard-code.
- **Dependency:** MVP Task 08, 20, 24; Data Steward được chỉ định.
- **Tiêu chí hoàn thành có thể kiểm chứng:** source active/inactive policy; Excel upload tạo envelope/receipt; batch pin source/profile/contract version; config changes được audit.
- **Lệnh build/test cần chạy:** `dotnet test backend/IqcQms.sln`; API integration tests source registration/authorization/upload receipt; OpenAPI contract tests.
- **Kế hoạch rollback:** deactivate source/profile version mới; giữ receipts/audit; chuyển UI upload sang read-only, không xóa batches.

### MVP Task 26 — Task 031 / E05-T02: Immutable raw archive

- **Mục tiêu:** stream file gốc vào managed storage với checksum và idempotent receipt.
- **Lý do bắt buộc:** staging/review/commit phải truy về nguồn và có thể phục hồi sau lỗi.
- **Dependency:** MVP Task 25; storage ADR/quota/retention/encryption.
- **Tiêu chí hoàn thành có thể kiểm chứng:** raw immutable; server-generated path; duplicate content/idempotency outcome xác định; restore/read checksum test; cleanup không xóa evidence.
- **Lệnh build/test cần chạy:** `dotnet test backend/IqcQms.sln`; upload integration tests valid/duplicate/path attack/storage failure; checksum verification.
- **Kế hoạch rollback:** tắt upload flag; giữ raw/receipt; rollback storage adapter/config; không xóa raw object đã nhận.

### MVP Task 27 — Task 032 / E05-T04: Header Detection Engine MVP

- **Mục tiêu:** deterministic detection cho contract Master Plan với confidence/ambiguity evidence.
- **Lý do bắt buộc:** header không cố định hàng 1; phát hiện sai sẽ map sai toàn batch.
- **Dependency:** MVP Task 25; corpus và threshold được Product/Data Steward ký.
- **Tiêu chí hoàn thành có thể kiểm chứng:** benchmark corpus đạt precision/recall threshold đã ghi; missing/ambiguous header không auto-commit; engine/profile version được lưu.
- **Lệnh build/test cần chạy:** `dotnet test backend/IqcQms.sln` với parser/detection corpus vi/en/ko, shifted headers, duplicate aliases và invalid files; performance test theo giới hạn MVP.
- **Kế hoạch rollback:** pin engine/profile version trước; ambiguous files chuyển review/manual rejection; không fallback silent mapping.

### MVP Task 28 — Task 035 / E05-T05: Mapping Dictionary MVP

- **Mục tiêu:** mapping header/value/identity đã xác nhận có thể tái sử dụng và versioned.
- **Lý do bắt buộc:** review sẽ lặp vô hạn nếu quyết định người dùng không được lưu dùng lại.
- **Dependency:** MVP Task 13, 27; mapping governance/effective-time rules.
- **Tiêu chí hoàn thành có thể kiểm chứng:** draft/review/publish/retire; critical mapping có four-eyes; batch pin mapping version; conflict/rollback/audit tests xanh.
- **Lệnh build/test cần chạy:** `dotnet test backend/IqcQms.sln`; mapping API/authorization/version/conflict tests; frontend review mapping interaction tests.
- **Kế hoạch rollback:** retire version lỗi và republish version trước; revalidate affected uncommitted batches; committed records xử lý bằng audited correction, không sửa âm thầm.

### MVP Task 29 — Task 036 / E05-T06: Validation Engine MVP

- **Mục tiêu:** version hóa rules required/type/date/duplicate/reference/core cho Master Plan.
- **Lý do bắt buộc:** staging chỉ tạo giá trị khi dữ liệu không hợp lệ bị chặn nhất quán trước commit.
- **Dependency:** MVP Task 20, 28; rows/file/time/memory target được chốt.
- **Tiêu chí hoàn thành có thể kiểm chứng:** toàn bộ Master Plan rules nằm trong catalogue; immutable results có rule/version/evidence; revalidation tạo run mới; benchmark đạt target.
- **Lệnh build/test cần chạy:** `dotnet test backend/IqcQms.sln` với schema/row/cross-row/reference/core rules; duplicate/concurrency/Unicode/date boundary tests; batch performance test.
- **Kế hoạch rollback:** pin previous rule-set version; tắt commit cho batch chạy rule lỗi; revalidate từ staging/raw, không overwrite result cũ.

### MVP Task 30 — Task 037 / E05-T07: Review Workflow MVP

- **Mục tiêu:** reviewer xử lý lỗi/ambiguity có owner, reason, approval và revalidation.
- **Lý do bắt buộc:** review là bước được yêu cầu rõ trong lát MVP; đổi status trực tiếp không đủ an toàn.
- **Dependency:** MVP Task 14, 29; reviewer roles và SLA matrix.
- **Tiêu chí hoàn thành có thể kiểm chứng:** queue/assignment; resolution actions có semantics; override có reason/four-eyes khi critical; không row nào thành Ready nếu chưa revalidate; E2E review xanh.
- **Lệnh build/test cần chạy:** `dotnet test backend/IqcQms.sln`; API workflow/authorization/state-transition tests; frontend type/lint/build và E2E review cases.
- **Kế hoạch rollback:** tắt review/commit write flags; giữ cases/decisions append-only; rollback workflow version và revalidate pending batches.

### MVP Task 31 — Task 038 / E05-T08: Commit pipeline và lineage

- **Mục tiêu:** commit validated/reviewed rows atomic, idempotent và hiển thị qua canonical Master Plan API.
- **Lý do bắt buộc:** đây là bước biến dữ liệu staged thành giá trị sử dụng; thiếu idempotency/lineage sẽ gây duplicate hoặc mất nguồn gốc.
- **Dependency:** MVP Task 20, 21, 29, 30.
- **Tiêu chí hoàn thành có thể kiểm chứng:** retry/concurrent commit không duplicate; validation/review gate không thể bypass; rollback không partial effect; 100% committed rows có audit/lineage; UI hiển thị đúng core records.
- **Lệnh build/test cần chạy:** `dotnet test backend/IqcQms.sln` với transaction/idempotency/concurrency/constraint/audit tests; frontend type/lint/build; E2E upload→review→commit→display; SQL reconciliation.
- **Kế hoạch rollback:** tắt commit flag và giữ read-only; rollback app artifact; transaction rollback tự động; correction dùng audited compensating action, không xóa history.

### MVP Task 32 — Task 050 / E10-T03: UAT theo vai trò

- **Mục tiêu:** xác nhận lát MVP hoạt động cho Viewer, Steward/Reviewer, Committer và Admin trên dữ liệu kiểm soát.
- **Lý do bắt buộc:** build xanh chưa chứng minh workflow đáp ứng nhu cầu thực tế.
- **Dependency:** tất cả MVP Task 01–31; UAT scenarios, data và users sẵn sàng.
- **Tiêu chí hoàn thành có thể kiểm chứng:** 100% critical MVP scenarios pass; Severity 1/2 bằng 0; permission negative cases pass; reconciliation đúng; Product/Security/Data Owner ký; known issues có owner/date/workaround.
- **Lệnh build/test cần chạy:** full CI suite; UAT E2E suite cho bốn personas; manual exploratory checklist; SQL/raw/audit reconciliation; security smoke cho upload/authorization.
- **Kế hoạch rollback:** tắt upload/review/commit flags; giữ Master Plan read-only hoặc rollback toàn artifact/DB theo Task 024; preserve raw/audit/UAT evidence.

## 6. Timeline và nguồn lực MVP

### Estimate

- Tổng ideal effort: khoảng **276 PD**.
- Với 2–3 developers, QA chuyên trách và DevOps/DBA/Security bán thời gian: **10–12 sprint phát triển**.
- Tính external approvals, IdP/SQL provisioning, hai rehearsal và UAT: baseline **12–14 sprint**, khoảng **6–7 tháng**.

### Các lane chạy song song

- **Lane A — Foundation/Platform:** Task 001–010, 019–024.
- **Lane B — Identity/API:** Task 011–18.
- **Lane C — Data Hub:** Task 025–31 sau khi SQL cutover sẵn sàng.
- **Lane D — QA/UAT:** chuẩn bị test/data từ Task 005; thực thi Task 032 sau feature complete.

Không rút timeline bằng cách bỏ security, staging, review, audit, idempotency hoặc SQL reconciliation. Nếu cần giảm thêm phạm vi, giảm số source/file variants và UI presentation, không giảm quality gates của pipeline.

## 7. MVP Definition of Done tổng hợp

MVP chỉ hoàn thành khi:

- [ ] 32 task bắt buộc đều đạt DoD; không task dependency nào bị đánh dấu giả.
- [ ] Full build/type/lint/test pipeline xanh từ clone sạch.
- [ ] OIDC login và permission matrix hoạt động ở UAT.
- [ ] Một file hợp lệ đi xuyên suốt đến Master Plan UI.
- [ ] File lỗi và ambiguity bị chặn, review, revalidate rồi mới commit.
- [ ] Duplicate/retry/concurrent commit không tạo duplicate effect.
- [ ] Raw, staging, validation result, review decision, audit và lineage truy vết được.
- [ ] SQL migration/cutover/reconciliation và rollback đã rehearsal.
- [ ] UAT critical scenarios pass, không còn Severity 1/2.
- [ ] Các task hoãn vẫn nằm ngoài implementation; không có feature phụ được “tiện tay” thêm vào.

## 8. Quy tắc rollback cấp MVP

1. Khi có sự cố dữ liệu hoặc authorization, tắt `commit` rồi `review/upload` flags; giữ read-only nếu an toàn.
2. Không xóa raw archive, staging, review decision hoặc audit khi rollback.
3. Rollback application bằng last-known-good artifact; rollback config/secrets qua version đã kiểm thử.
4. Database dùng backup/restore hoặc compensation đã rehearsal; không sửa tay production/UAT để che lỗi.
5. Reconciliation bắt buộc trước khi mở lại writes.
6. Mọi rollback phải ghi incident/traceId, actor, thời điểm, dữ liệu bị ảnh hưởng và tiêu chí mở lại.

