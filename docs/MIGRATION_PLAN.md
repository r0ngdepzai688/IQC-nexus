# IQC Nexus Migration Plan

## 1. Mục đích và quyết định kiến trúc

Tài liệu này là kế hoạch chuyển IQC Nexus từ prototype cục bộ sang nền tảng quản lý chất lượng nội bộ có thể vận hành ổn định, bảo mật và mở rộng. Chiến lược là **cải tiến gia tăng theo mô hình strangler**, giữ lại UI, nghiệp vụ và pipeline Data Hub có giá trị; không viết lại toàn bộ và không tách microservice sớm.

Kiến trúc chuyển tiếp và kiến trúc đích là **modular monolith** gồm frontend Next.js, backend ASP.NET Core, SQL Server, Data Hub worker và Windows Agent. Ranh giới module được thiết lập trong code, API và database trước; chỉ tách service khi có bằng chứng về nhu cầu scale, ownership hoặc isolation.

## 2. Mục tiêu chương trình

1. Loại bỏ các lỗ hổng xác thực, phân quyền và quản lý bí mật hiện tại.
2. Tạo baseline build/test/deploy lặp lại được từ một bản clone sạch.
3. Chuẩn hóa API contract và hợp nhất các mô hình dữ liệu legacy/mới.
4. Chuyển SQLite sang SQL Server với kiểm soát toàn vẹn, backup và phục hồi.
5. Nâng Data Hub thành nền tảng ingest dùng chung, có connector, versioning, review và recovery.
6. Chuyển từng module mock sang dữ liệu thật mà không làm gián đoạn người dùng.
7. Chuẩn hóa giao diện, accessibility và đa ngôn ngữ.
8. Thiết lập quan sát hệ thống, audit, SLO và quy trình vận hành.

## 3. Nguyên tắc thực hiện

- Security và data integrity là điều kiện mở cổng, không phải hạng mục hoàn thiện cuối.
- Mỗi giai đoạn phải có sản phẩm chạy được và có rollback.
- Không xóa đường cũ trước khi đường mới đạt kiểm thử tương đương và có telemetry.
- Migrations phải backward-compatible trong cửa sổ triển khai; ưu tiên expand–migrate–contract.
- API và event có version; schema mapping có hiệu lực theo phiên bản.
- Không để UI quyết định authorization.
- Không deploy migration database ngầm trong startup production.
- Dữ liệu cá nhân, bí mật và file nguồn không nằm trong Git.
- Mỗi module mới phải có owner, SLO, runbook và test tối thiểu.

## 4. Phạm vi và giả định

### Trong phạm vi

- Frontend, backend, database, authentication/authorization, Data Hub, Windows Agent.
- Tasks, Chat, New Models, Users, Audit và các module Standards/HR theo từng đợt.
- CI/CD, môi trường DEV/UAT/PROD, logging, monitoring, backup và disaster recovery.
- Design system và i18n.

### Ngoài phạm vi ban đầu

- Microservices hoàn chỉnh, Kubernetes và multi-region.
- Thay toàn bộ UI hoặc domain model trong một lần.
- Data lake/warehouse phân tích độc lập; Data Hub trước hết phục vụ operational data.
- Mobile native app.

### Giả định lập kế hoạch

- Một squad 5–7 người: Product Owner/BA, Tech Lead, 2–3 developers, QA, DevOps/DBA bán thời gian, Security hỗ trợ theo cổng.
- Sprint hai tuần; timeline mục tiêu 22 sprint, khoảng 10–11 tháng.
- Hạ tầng on-premises Windows/SQL Server là lựa chọn chính.
- Identity doanh nghiệp có thể cung cấp OIDC/Entra ID hoặc AD FS; nếu chưa có, dùng ASP.NET Core Identity tạm thời nhưng giữ abstraction OIDC.

## 5. Lộ trình tổng thể

| Giai đoạn | Thời lượng | Kết quả chính | Điều kiện vào | Cổng hoàn tất |
|---|---:|---|---|---|
| 0. Baseline & containment | Sprint 1–2 | Repo đầy đủ, build xanh, secrets được xử lý | Quyền truy cập repo/hạ tầng | Clone sạch build và kiểm tra được |
| 1. Security foundation | Sprint 3–5 | Login thật, RBAC/ABAC backend, API được khóa | Baseline xanh | Pen-test nội bộ không còn lỗi Critical/High |
| 2. Contract & modularization | Sprint 6–8 | API v1, Problem Details, module boundary, legacy map | Auth ổn định | Contract tests và architecture tests xanh |
| 3. SQL Server migration | Sprint 9–11 | SQL Server UAT/PROD, dữ liệu đối soát | Schema chuẩn hóa | Cutover rehearsal đạt RTO/RPO |
| 4. Data Hub platform | Sprint 12–15 | Connector framework, versioned pipeline, review/recovery | SQL Server sẵn sàng | Import replay/idempotency và SLA đạt |
| 5. Operational modules | Sprint 16–19 | Tasks, Chat, New Models dùng API thật | Contract/Data Hub ổn định | Mock bị tắt theo feature flag |
| 6. UX & design system | Sprint 17–20, song song | Tokens, component library, a11y, i18n | API ổn định theo module | WCAG 2.1 AA cho luồng chính |
| 7. Production readiness | Sprint 20–22 | Observability, DR, runbook, rollout | UAT hoàn tất | Go-live checklist và sign-off |

## 6. Kế hoạch theo giai đoạn

### Giai đoạn 0 — Baseline và containment

**Mục tiêu:** biến trạng thái máy phát triển thành một sản phẩm có thể tái tạo từ Git, đồng thời cô lập rủi ro tức thời.

**Công việc trọng tâm**

- Sửa quy tắc ignore để source Data Hub được version-control; loại dữ liệu, DB, file Excel và export khỏi source control.
- Thu hồi/xoay JWT secret; đưa cấu hình nhạy cảm vào secret store/environment.
- Chuẩn hóa encoding UTF-8 và quét dữ liệu cá nhân.
- Làm xanh TypeScript, ESLint và backend build.
- Tạo test project, CI quality gates và dependency scan.
- Chụp baseline OpenAPI, schema SQLite, dữ liệu đối soát và performance import.
- Gắn nhãn rõ mock/legacy; đóng băng feature mới ở vùng bảo mật và Data Hub cho tới khi cổng đạt.

**Tiêu chí thành công**

- Clone sạch có thể restore/build/test không cần file local bị bỏ quên.
- Không có secret thật trong Git history hiện hành; secret production được xoay.
- CI bắt buộc trên pull request.
- Có inventory dữ liệu và kế hoạch retention.

### Giai đoạn 1 — Security foundation

**Mục tiêu:** danh tính đáng tin cậy và authorization được thực thi tại server.

**Công việc trọng tâm**

- Tích hợp OIDC doanh nghiệp; phương án dự phòng là ASP.NET Core Identity.
- Dùng BFF/HttpOnly Secure SameSite cookie nếu topology cho phép; không lưu access token trong localStorage.
- Policy-based authorization với permission catalogue và data scope theo part/scope/project.
- Khóa toàn bộ controller/hub theo deny-by-default; chỉ login/health công khai.
- Loại mock login và Dev Identity Override khỏi build production.
- Rate limiting, lockout, password reset, MFA theo chính sách doanh nghiệp.
- Security audit cho upload, path traversal, CORS, error leakage và SignalR group membership.

**Tiêu chí thành công**

- 100% endpoint có policy hoặc khai báo anonymous có chủ đích.
- Unauthorized/forbidden được kiểm thử tự động.
- Không thể sửa role ở client để tăng quyền.
- Không còn lỗ hổng Critical/High trong security review.

### Giai đoạn 2 — API contract và modularization

**Mục tiêu:** ổn định contract trước khi chuyển database và module.

**Công việc trọng tâm**

- Định nghĩa `/api/v1`, DTO riêng, validation, pagination/filter/sort và Problem Details.
- Sinh TypeScript client từ OpenAPI; một API client duy nhất.
- Chuẩn hóa từ điển Role, Status, Module, Action và naming.
- Hợp nhất `MasterPlanRecord` legacy với `MasterPlan` core bằng adapter và migration map.
- Tách application use case khỏi controller/EF; thêm architecture tests.
- Áp dụng outbox cho sự kiện nội bộ cần độ tin cậy.

**Tiêu chí thành công**

- Không trả EF entity trực tiếp qua API mới.
- Contract tests chạy trong CI.
- Các route legacy có telemetry, deprecation date và adapter.
- Không còn mismatch activate ID giữa hai mô hình Master Plan.

### Giai đoạn 3 — SQL Server

**Mục tiêu:** chuyển dữ liệu có đối soát, không làm gián đoạn nghiệp vụ.

**Công việc trọng tâm**

- Thiết kế schema SQL Server: khóa, FK, unique index, filtered index, rowversion và temporal/audit phù hợp.
- Tách migration execution khỏi app startup; pipeline deploy database có approval.
- Viết công cụ migrate idempotent SQLite → SQL Server và báo cáo reconciliation.
- Chạy rehearsal DEV/UAT, performance test, backup/restore và rollback.
- Cutover bằng maintenance window ngắn hoặc dual-read có feature flag; không dual-write dài hạn.

**Tiêu chí thành công**

- Đối soát số lượng, checksum và business key đạt 100%; sai lệch được giải trình.
- RPO mục tiêu ≤ 15 phút, RTO ≤ 4 giờ trong giai đoạn đầu.
- P95 API read chính < 500 ms; import mục tiêu được xác lập từ baseline.
- Có restore test thành công, không chỉ có backup job.

### Giai đoạn 4 — Data Hub platform

**Mục tiêu:** biến pipeline Master Plan thành nền tảng ingest đa nguồn có kiểm soát.

**Công việc trọng tâm**

- Connector SDK/contract cho Excel upload, watched folder qua Windows Agent, REST API và connector tương lai.
- Header Detection Engine có confidence, profile theo nguồn và UI xác nhận mapping.
- Mapping Dictionary versioned theo tenant/module/source/schema.
- Validation rule catalogue, severity, rule version và kết quả tái lập được.
- Review workflow có assignment, SLA, four-eyes approval và resolution semantics.
- Commit pipeline idempotent, optimistic concurrency, outbox và lineage.
- Replay/retry/dead-letter, quarantine và recovery runbook.
- Dashboard vận hành và alert theo SLO.

**Tiêu chí thành công**

- Cùng một batch/version replay cho cùng kết quả hoặc phát hiện conflict có kiểm soát.
- 100% core record truy ngược được source file, row, mapping/rule version và approver.
- Không mất batch khi agent/network/server gián đoạn.
- Review backlog và ingestion failure có owner/SLA.

### Giai đoạn 5 — Chuyển module nghiệp vụ

**Mục tiêu:** loại mock từng lát dọc mà không thay toàn bộ UI.

**Thứ tự:** New Models activation → Tasks → Chat → Audit/Users → Standards/HR.

Mỗi module đi qua: contract → read API → write API → migration dữ liệu → feature flag → UAT → tắt mock → xóa legacy sau một release ổn định.

**Tiêu chí thành công**

- Không mất dữ liệu sau reload.
- Luồng create/update/approve có audit và authorization.
- Tỷ lệ lỗi theo module nằm trong SLO hai release liên tiếp trước khi xóa legacy.

### Giai đoạn 6 — Design system và trải nghiệm

**Mục tiêu:** giảm chi phí phát triển UI và tạo trải nghiệm nhất quán.

- Token hóa màu, typography, spacing, elevation, motion và z-index.
- Component library có Storybook, tests và accessibility contract.
- App shell/layout/navigation responsive dùng chung.
- Theme light/dark/high contrast; i18n tiếng Việt/Anh và chuẩn bị cho tiếng Hàn.
- Giảm component lớn bằng container/presenter và feature folders.

**Tiêu chí thành công**

- ≥ 80% UI luồng chính dùng component/tokens chuẩn.
- Không lỗi axe mức serious/critical trên luồng chính.
- Không còn mojibake; UI không hard-code text nghiệp vụ mới.

### Giai đoạn 7 — Production readiness và rollout

- OpenTelemetry, structured logs, dashboards, alerts, synthetic checks.
- Runbook, incident process, audit retention, DR exercise và capacity test.
- Pilot theo nhóm nhỏ, canary/feature flags, hypercare 2–4 tuần.
- Đào tạo admin, data steward, reviewer và support.

**Cổng go-live**

- Security, QA, DBA, Product Owner và Operations cùng ký duyệt.
- Không còn blocker Severity 1/2; known issues có owner/date/workaround.
- Rollback đã rehearsal; monitoring và on-call hoạt động.

## 7. Phụ thuộc chính

| Phụ thuộc | Chủ sở hữu đề xuất | Cần trước | Rủi ro nếu chậm |
|---|---|---|---|
| OIDC/AD, nhóm và claims doanh nghiệp | IT Identity | Giai đoạn 1 | Phải dùng identity tạm, tăng rework |
| SQL Server license/instance, DBA | Infrastructure/DBA | Giai đoạn 3 | Không thể rehearsal/cutover |
| Windows code-signing certificate | Security/IT | Giai đoạn 4 | Agent khó triển khai an toàn |
| File share và service accounts | Infrastructure | Giai đoạn 4 | Connector folder không ổn định |
| Danh mục role/permission chính thức | Product/Security | Giai đoạn 1–2 | Authorization không nhất quán |
| Data owner/steward cho từng nguồn | Business Owner | Giai đoạn 4 | Review backlog không có trách nhiệm |
| DEV/UAT/PROD và CI runners | DevOps | Giai đoạn 0 | Không tạo release pipeline |

## 8. Rủi ro chương trình và kiểm soát

| Rủi ro | Xác suất/Tác động | Kiểm soát | Trigger/Phương án |
|---|---|---|---|
| Source local không nằm trong Git | Cao/Rất cao | Inventory, clone test | Dừng release nếu clone thiếu file |
| Lộ secret/dữ liệu cá nhân | Cao/Rất cao | Rotate, scan, secret store | Incident response và thu hồi ngay |
| Sai lệch dữ liệu khi chuyển SQL Server | Trung/Rất cao | Reconciliation, rehearsal, backup | Rollback và mở lại SQLite read-only |
| Role nghiệp vụ chưa thống nhất | Cao/Cao | Permission workshop, catalogue | Không mở policy production khi chưa sign-off |
| Scope phình do thay UI toàn bộ | Cao/Cao | Strangler, feature flags, WIP limit | Product steering cắt scope theo cổng |
| Data Hub import đồng thời gây duplicate | Trung/Cao | DB unique key, idempotency | Quarantine batch và replay |
| Windows Agent offline/mất file | Trung/Cao | Local spool, ack, checksum | Alert, retry, operator recovery |
| Hiệu năng SQL/ingest không đạt | Trung/Cao | Baseline, load test, indexes | Tối ưu query/batch trước scale-out |
| Thiếu test gây regression | Cao/Cao | Test pyramid và quality gate | Không cho merge nếu vùng thay đổi thiếu test |
| Người dùng không chấp nhận workflow review | Trung/Cao | Pilot, SLA, training | Điều chỉnh workflow, không bypass audit |

## 9. Governance và nhịp điều hành

- Steering committee hàng tháng: scope, risk, budget, go/no-go.
- Architecture review mỗi sprint cho ADR và exception.
- Security/Privacy review tại giai đoạn 1, 3, 4 và trước go-live.
- Data governance forum hai tuần/lần trong giai đoạn Data Hub.
- Demo và UAT theo lát dọc, không chờ cuối chương trình.
- Mỗi cổng có evidence pack: test, scan, metrics, rollback và sign-off.

## 10. Chỉ số thành công cấp chương trình

- Build success trên main ≥ 95%; không release từ build đỏ.
- Lead time thay đổi giảm ≥ 30% sau khi CI/design system ổn định.
- 100% API protected có authorization test.
- 100% core record từ Data Hub có lineage đầy đủ.
- Import success ≥ 99% với file đúng contract; batch thất bại không bị mất.
- P95 API < 500 ms cho luồng tương tác chính ở tải mục tiêu.
- Uptime mục tiêu ban đầu 99,5%, nâng lên 99,9% sau khi ổn định.
- MTTR sự cố Severity 2 < 4 giờ.
- Không còn mock trong module đã tuyên bố production.
- Accessibility WCAG 2.1 AA cho các luồng trọng yếu.

## 11. Chiến lược rollback

- Feature flag ở cấp module và connector.
- Database dùng expand–migrate–contract; rollback app không yêu cầu rollback schema phá hủy.
- Trước cutover: full backup, verify restore và snapshot reconciliation.
- SQLite cũ được đóng băng read-only trong thời gian hypercare, có retention được phê duyệt.
- Batch Data Hub có thể replay từ immutable raw object/file và manifest.
- Windows Agent giữ local spool cho tới khi server xác nhận checksum và receipt.

