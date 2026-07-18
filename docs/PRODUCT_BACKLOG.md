# IQC Nexus Product Backlog

## 1. Quy ước

Backlog này bao phủ chương trình chuyển đổi IQC Nexus. Thứ tự thực hiện thực tế do Product Owner quyết định tại planning nhưng không được vượt qua các quality/security gates trong `MIGRATION_PLAN.md`.

### Priority

- **P0:** blocker bảo mật, dữ liệu hoặc khả năng build/deploy.
- **P1:** bắt buộc cho production/MVP.
- **P2:** nâng độ ổn định, năng suất hoặc module tiếp theo.
- **P3:** tối ưu sau khi hệ thống ổn định.

### Estimate

- Dùng ideal person-days (PD), bao gồm code, review, test và tài liệu kỹ thuật.
- `Spike` là time-box để ra quyết định/ADR, không phải implementation hoàn chỉnh.
- Estimate cần refinement lại sau discovery và không phải cam kết lịch cứng.

### Definition of Done chung

Mọi task ngoài DoD riêng phải: được review; test phù hợp chạy xanh; không tạo lỗi security/accessibility nghiêm trọng; tài liệu/ADR/OpenAPI/runbook liên quan được cập nhật; telemetry và feature flag được thêm nếu cần; không chứa secret/PII; có evidence trong CI/UAT.

---

## EPIC E01 — Repository, Quality Baseline và Delivery Foundation

**Kết quả:** clone sạch có thể build, test và tạo artifact đáng tin cậy.

### Feature E01-F01 — Repository integrity

#### E01-T01 — Đưa toàn bộ source Data Hub vào version control

- **Mô tả:** kiểm kê entities, interfaces, services và docs đang bị rule `DataHub/` ignore; sửa scope ignore để chỉ bỏ dữ liệu runtime.
- **Giá trị:** loại rủi ro clone/deploy thiếu module cốt lõi.
- **Tác động kỹ thuật:** `.gitignore`, repository inventory, clone verification; không đổi behavior.
- **Ưu tiên / Ước lượng:** P0 / 2 PD.
- **Phụ thuộc:** không.
- **Definition of Done:** clone mới có đủ source; `git check-ignore` chỉ bỏ raw/runtime data; backend build từ clone sạch; không commit file dữ liệu.

#### E01-T02 — Phân loại và loại dữ liệu/binary khỏi repository

- **Mô tả:** lập inventory DB, Excel, JSON users, zip/export; xác định owner, sensitivity, retention và nơi lưu thay thế.
- **Giá trị:** giảm rò rỉ PII, kích thước repo và sai lệch môi trường.
- **Tác động kỹ thuật:** ignore policy, data handling guide, sanitized fixtures.
- **Ưu tiên / Ước lượng:** P0 / 3 PD.
- **Phụ thuộc:** E01-T01.
- **Definition of Done:** không có PII fixture trong test; synthetic sample thay thế; Security/Data Owner ký inventory; history cleanup được quyết định bằng ADR/runbook.

#### E01-T03 — Chuẩn hóa UTF-8 và sửa mojibake baseline

- **Mô tả:** xác định encoding policy cho C#, TS, JSON, Markdown và nguồn Excel; tạo kiểm tra tự động.
- **Giá trị:** tên người dùng và thuật ngữ Việt/Hàn hiển thị đúng, giảm lỗi mapping.
- **Tác động kỹ thuật:** editor config, lint/test encoding, dữ liệu fixture chuẩn.
- **Ưu tiên / Ước lượng:** P1 / 4 PD.
- **Phụ thuộc:** E01-T02.
- **Definition of Done:** source mới UTF-8; test round-trip Việt/Hàn xanh; danh sách file legacy cần sửa có owner; không còn mojibake trong critical flows.

### Feature E01-F02 — CI và quality gates

#### E01-T04 — Làm xanh TypeScript và ESLint

- **Mô tả:** sửa import/type mismatch, missing hook, role union, React hook và lint errors hiện hữu mà không đổi nghiệp vụ ngoài ý muốn.
- **Giá trị:** frontend có thể release và refactor an toàn.
- **Tác động kỹ thuật:** type contracts, component state/effects, lint rules.
- **Ưu tiên / Ước lượng:** P0 / 8 PD.
- **Phụ thuộc:** E01-T01.
- **Definition of Done:** `tsc --noEmit` và ESLint không lỗi; warning có baseline/owner; critical pages smoke test xanh.

#### E01-T05 — Tạo test projects và test pyramid tối thiểu

- **Mô tả:** thiết lập unit test backend/frontend, API integration test và E2E smoke framework.
- **Giá trị:** ngăn regression trong quá trình migration.
- **Tác động kỹ thuật:** test harness, isolated DB/container strategy, fixtures synthetic.
- **Ưu tiên / Ước lượng:** P0 / 7 PD.
- **Phụ thuộc:** E01-T02, E01-T04.
- **Definition of Done:** CI chạy unit/integration/E2E smoke; có test login denial, Data Hub parse/commit và route render; flaky test policy được ghi rõ.

#### E01-T06 — CI build, scan và artifact pipeline

- **Mô tả:** pipeline restore/build/test/lint/OpenAPI diff/dependency scan/SBOM và tạo artifact ký phiên bản.
- **Giá trị:** mọi release tái lập và có bằng chứng chất lượng.
- **Tác động kỹ thuật:** CI runner, artifact registry, versioning.
- **Ưu tiên / Ước lượng:** P0 / 6 PD.
- **Phụ thuộc:** E01-T04, E01-T05.
- **Definition of Done:** PR bắt buộc checks; main tạo artifact một lần; failed gate không deploy; SBOM lưu cùng release.

---

## EPIC E02 — Identity, Security và Authorization

**Kết quả:** danh tính tin cậy, deny-by-default và quyền dữ liệu được thực thi tại backend.

### Feature E02-F01 — Identity and session

#### E02-T01 — Xoay secrets và thiết lập secret management

- **Mô tả:** thu hồi JWT key hiện tại; chuyển secrets/connection strings/cert references khỏi file tracked.
- **Giá trị:** giảm nguy cơ giả mạo token và lộ thông tin kết nối.
- **Tác động kỹ thuật:** configuration providers, deployment variables/vault, key rotation runbook.
- **Ưu tiên / Ước lượng:** P0 / 3 PD.
- **Phụ thuộc:** E01-T06 cho secret scan tự động.
- **Definition of Done:** secret cũ không còn hiệu lực; production config không nằm trong Git; rotation được thử nghiệm; scan không phát hiện secret hoạt động.

#### E02-T02 — Quyết định và tích hợp OIDC/BFF

- **Mô tả:** spike identity provider, chọn BFF HttpOnly cookie hoặc phương án được phê duyệt; tích hợp login/logout/session.
- **Giá trị:** SSO doanh nghiệp, loại token giả/localStorage.
- **Tác động kỹ thuật:** Next.js auth boundary, ASP.NET auth, reverse proxy callbacks, session protection.
- **Ưu tiên / Ước lượng:** P0 / 3 PD spike + 10 PD implementation.
- **Phụ thuộc:** OIDC/AD access, E02-T01.
- **Definition of Done:** ADR được ký; login/logout/expiry/revoke hoạt động ở UAT; token không xuất hiện trong localStorage; callback/CSRF tests xanh.

#### E02-T03 — Loại mock login và dev identity khỏi production

- **Mô tả:** nối trang login/session thật, khóa dev override bằng build/environment guard và xóa fake token path.
- **Giá trị:** không thể vào hệ thống bằng chuỗi token bất kỳ.
- **Tác động kỹ thuật:** AuthContext/session API, route guards, production build flags.
- **Ưu tiên / Ước lượng:** P0 / 5 PD.
- **Phụ thuộc:** E02-T02.
- **Definition of Done:** anonymous bị chuyển tới login; fake token không truy cập được; dev override không có trong production bundle; E2E auth xanh.

### Feature E02-F02 — Authorization

#### E02-T04 — Xây permission catalogue và role mapping

- **Mô tả:** hợp nhất System Role, Business Role, permission và scope; workshop với Product/Security.
- **Giá trị:** quyền nhất quán và audit được.
- **Tác động kỹ thuật:** policy model, seed/config, migration từ chuỗi role cũ.
- **Ưu tiên / Ước lượng:** P0 / 5 PD.
- **Phụ thuộc:** business owners, E02-T02.
- **Definition of Done:** catalogue có owner/version; mapping Team/Group/Part/Cell/Staff được phê duyệt; không còn thuật ngữ mâu thuẫn chưa giải quyết.

#### E02-T05 — Áp dụng policy-based authorization deny-by-default

- **Mô tả:** controller/hub yêu cầu authenticated user mặc định; policy kiểm tra permission và resource scope.
- **Giá trị:** bảo vệ dữ liệu dù UI bị bypass.
- **Tác động kỹ thuật:** ASP.NET policies/handlers, resource loaders, endpoint metadata.
- **Ưu tiên / Ước lượng:** P0 / 10 PD.
- **Phụ thuộc:** E02-T04.
- **Definition of Done:** 100% endpoint có policy hoặc explicit anonymous; ma trận 401/403/scope tests xanh; Users/Data Hub/Chat không còn public.

#### E02-T06 — Bảo vệ upload, SignalR và abuse controls

- **Mô tả:** size/MIME/signature checks, path canonicalization, rate limit, group membership và security headers/CORS.
- **Giá trị:** giảm path traversal, upload độc hại và giả mạo sender/chat room.
- **Tác động kỹ thuật:** middleware, upload service, SignalR authorization, proxy config.
- **Ưu tiên / Ước lượng:** P0 / 8 PD.
- **Phụ thuộc:** E02-T05.
- **Definition of Done:** security tests cho traversal/oversize/spoofing xanh; CORS theo environment; SignalR sender lấy từ claims; findings High/Critical bằng 0.

---

## EPIC E03 — API Contract và Modular Monolith

### Feature E03-F01 — API standards

#### E03-T01 — Định nghĩa API v1 và Problem Details

- **Mô tả:** chuẩn route, status code, error code, validation, pagination, filtering, correlation và idempotency.
- **Giá trị:** frontend/agent tích hợp ổn định, lỗi hỗ trợ dễ hơn.
- **Tác động kỹ thuật:** middleware, DTO conventions, OpenAPI filters.
- **Ưu tiên / Ước lượng:** P1 / 6 PD.
- **Phụ thuộc:** E01-T05.
- **Definition of Done:** API guide và mẫu endpoint; RFC 7807 nhất quán; traceId xuyên request; contract tests mẫu xanh.

#### E03-T02 — Sinh TypeScript API client từ OpenAPI

- **Mô tả:** thay axios/fetch hard-code bằng generated client và adapter chung.
- **Giá trị:** loại drift `rawStatus/importedStatus`, giảm code lặp.
- **Tác động kỹ thuật:** codegen, API client package, environment config, error mapping.
- **Ưu tiên / Ước lượng:** P1 / 6 PD.
- **Phụ thuộc:** E03-T01, E02-T02.
- **Definition of Done:** critical pages dùng một client; không hard-code localhost; OpenAPI diff chạy CI; auth/correlation tự động.

#### E03-T03 — Thêm pagination/query safeguards

- **Mô tả:** áp dụng page/cursor, max limit, allowlisted sort/filter và cancellation cho Users, batches, staging, master plans.
- **Giá trị:** tránh tải toàn bảng và cải thiện UX dữ liệu lớn.
- **Tác động kỹ thuật:** query DTO, EF projections/index needs, frontend paging.
- **Ưu tiên / Ước lượng:** P1 / 7 PD.
- **Phụ thuộc:** E03-T01, E03-T02.
- **Definition of Done:** endpoint không trả collection vô hạn; query invalid trả Problem Details; load test ở dataset mục tiêu đạt P95.

### Feature E03-F02 — Module boundaries và legacy retirement

#### E03-T04 — Tạo module/application use-case boundaries

- **Mô tả:** di chuyển EF logic khỏi controllers; định nghĩa commands/queries/ports theo module và architecture tests.
- **Giá trị:** thay đổi từng module mà không tạo coupling lan rộng.
- **Tác động kỹ thuật:** project/folder structure, DI, application services, tests.
- **Ưu tiên / Ước lượng:** P1 / 12 PD.
- **Phụ thuộc:** E03-T01.
- **Definition of Done:** Auth/Users/New Models/Data Hub controllers mỏng; architecture tests chặn dependency sai; behavior regression tests xanh.

#### E03-T05 — Hợp nhất mô hình Master Plan legacy/core

- **Mô tả:** chọn aggregate chính, map `MasterPlanRecord`/`MasterPlan`, sửa activation ID và lên kế hoạch xóa legacy.
- **Giá trị:** activation hoạt động đúng, giảm hai nguồn sự thật.
- **Tác động kỹ thuật:** schema/data migration, adapter, endpoint compatibility.
- **Ưu tiên / Ước lượng:** P0 / 8 PD.
- **Phụ thuộc:** E03-T04; Product xác nhận business key.
- **Definition of Done:** một canonical aggregate; dữ liệu đối soát; activate dùng đúng ID/rowversion; legacy endpoint có deprecation test/date.

---

## EPIC E04 — SQL Server và Data Integrity

### Feature E04-F01 — Schema target

#### E04-T01 — Thiết kế schema SQL Server và constraints

- **Mô tả:** schema theo module, FK, unique indexes, rowversion, lengths/types, UTC và retention.
- **Giá trị:** database tự bảo vệ integrity và concurrency.
- **Tác động kỹ thuật:** EF configurations/migrations, naming conventions, DBA review.
- **Ưu tiên / Ước lượng:** P1 / 10 PD.
- **Phụ thuộc:** E03-T05, DBA/SQL instance.
- **Definition of Done:** ERD/DDL review; keys cho User, Batch, MasterPlan, Milestone, mapping; migration test trên DB rỗng và snapshot.

#### E04-T02 — Tách database migration khỏi app startup

- **Mô tả:** tạo idempotent migration scripts/pipeline với approval và runtime account least privilege.
- **Giá trị:** deploy có kiểm soát, app không chạy trên schema nửa vời.
- **Tác động kỹ thuật:** CI/CD stages, accounts, readiness checks.
- **Ưu tiên / Ước lượng:** P1 / 5 PD.
- **Phụ thuộc:** E01-T06, E04-T01.
- **Definition of Done:** production app không gọi `Migrate()`; pipeline backup→migrate→verify; failed migration chặn rollout.

### Feature E04-F02 — Data migration và cutover

#### E04-T03 — Xây migration/reconciliation tool SQLite → SQL Server

- **Mô tả:** extract-transform-load idempotent, mapping legacy IDs và báo cáo count/checksum/business key.
- **Giá trị:** chuyển dữ liệu có thể kiểm chứng và lặp lại.
- **Tác động kỹ thuật:** migration utility, data quality reports, secure config.
- **Ưu tiên / Ước lượng:** P1 / 12 PD.
- **Phụ thuộc:** E04-T01, E01-T02.
- **Definition of Done:** chạy lại không duplicate; discrepancy report; 100% record hoặc exception có disposition; secrets không nằm trong log.

#### E04-T04 — Rehearsal, backup/restore và production cutover

- **Mô tả:** hai lần rehearsal UAT, load test, freeze/cutover/rollback và hypercare.
- **Giá trị:** giảm downtime và nguy cơ mất dữ liệu.
- **Tác động kỹ thuật:** deployment, DNS/config flags, backup strategy, operational runbook.
- **Ưu tiên / Ước lượng:** P0 / 8 PD + cửa sổ vận hành.
- **Phụ thuộc:** E04-T02, E04-T03, Operations sign-off.
- **Definition of Done:** RPO/RTO rehearsal đạt; restore test thành công; reconciliation ký duyệt; rollback được thử; SQLite cũ read-only theo retention.

---

## EPIC E05 — Data Hub Platform

### Feature E05-F01 — Ingestion core và connectors

#### E05-T01 — Xây Source Registry và versioned ingestion envelope

- **Mô tả:** quản lý source/profile/owner/contract/SLA và receipt chuẩn cho mọi connector.
- **Giá trị:** onboarding nguồn mới mà không fork pipeline.
- **Tác động kỹ thuật:** tables, API, permission, manifest/version model.
- **Ưu tiên / Ước lượng:** P1 / 8 PD.
- **Phụ thuộc:** E04-T01, E03-T01.
- **Definition of Done:** Excel upload đi qua envelope; source inactive bị chặn; batch pin profile/version; audit config changes.

#### E05-T02 — Immutable raw store và idempotent receipt

- **Mô tả:** streaming raw payload, SHA-256, managed path/URI, idempotency và retention.
- **Giá trị:** không mất nguồn gốc, replay an toàn.
- **Tác động kỹ thuật:** storage abstraction, DB metadata, cleanup jobs.
- **Ưu tiên / Ước lượng:** P1 / 10 PD.
- **Phụ thuộc:** E05-T01; quyết định storage ADR.
- **Definition of Done:** duplicate content/key có outcome xác định; file không dùng user path trực tiếp; raw immutable; restore/replay test xanh.

#### E05-T03 — Connector API SDK/contract

- **Mô tả:** capability contract cho push/poll/checkpoint/ack/health; triển khai Excel/API reference connectors.
- **Giá trị:** mở rộng connector có chuẩn và security nhất quán.
- **Tác động kỹ thuật:** interfaces, API auth, version compatibility tests.
- **Ưu tiên / Ước lượng:** P1 / 9 PD.
- **Phụ thuộc:** E05-T01, E05-T02, E02-T06.
- **Definition of Done:** hai connector reference qua cùng gateway; compatibility matrix; idempotency/rate-limit tests; onboarding guide.

### Feature E05-F02 — Detection, mapping và validation

#### E05-T04 — Header Detection Engine có confidence

- **Mô tả:** tách detection khỏi parser; scoring, ambiguity, profile và evidence.
- **Giá trị:** giảm lỗi khi Excel thay đổi layout và cho phép review có thông tin.
- **Tác động kỹ thuật:** engine service, normalized aliases, result model, tests đa locale.
- **Ưu tiên / Ước lượng:** P1 / 10 PD.
- **Phụ thuộc:** E05-T01, fixture synthetic.
- **Definition of Done:** benchmark corpus có precision/recall threshold được duyệt; ambiguous file vào review; engine version được lưu.

#### E05-T05 — Mapping Dictionary lifecycle

- **Mô tả:** draft/review/publish/retire, effective version, scope và impact preview.
- **Giá trị:** steward sửa mapping không cần deploy code, vẫn tái lập được batch cũ.
- **Tác động kỹ thuật:** schema/API/UI, authorization, conflict detection.
- **Ưu tiên / Ước lượng:** P1 / 12 PD.
- **Phụ thuộc:** E05-T04, E02-T04.
- **Definition of Done:** four-eyes cho mapping critical; batch pin version; rollback version; conflict và audit tests xanh.

#### E05-T06 — Validation Engine và rule catalogue

- **Mô tả:** rule metadata/version/severity, schema/row/cross-row/reference/core checks và immutable results.
- **Giá trị:** chất lượng dữ liệu minh bạch, rule tái sử dụng giữa nguồn.
- **Tác động kỹ thuật:** rule framework, execution limits, result schema, test corpus.
- **Ưu tiên / Ước lượng:** P1 / 15 PD.
- **Phụ thuộc:** E05-T05, E04-T01.
- **Definition of Done:** Master Plan rules chuyển sang catalogue; revalidation tạo run mới; rule/version/evidence lưu đầy đủ; performance đạt batch target.

### Feature E05-F03 — Review, commit và recovery

#### E05-T07 — Review Workflow có assignment và SLA

- **Mô tả:** queue, ownership, resolution action, reason, approval, escalation và revalidation.
- **Giá trị:** ngoại lệ có trách nhiệm, không bypass audit.
- **Tác động kỹ thuật:** workflow state machine, notifications, UI, policies.
- **Ưu tiên / Ước lượng:** P1 / 15 PD.
- **Phụ thuộc:** E05-T06, E02-T05.
- **Definition of Done:** resolution không trực tiếp set ready; revalidate bắt buộc; SLA dashboard; four-eyes critical path; E2E review xanh.

#### E05-T08 — Commit pipeline idempotent và lineage

- **Mô tả:** lease, rowversion, bulk upsert, field ownership, audit/lineage/outbox cùng transaction.
- **Giá trị:** tránh duplicate/lost update và truy vết toàn bộ dữ liệu core.
- **Tác động kỹ thuật:** SQL transaction, unique keys, outbox worker, APIs.
- **Ưu tiên / Ước lượng:** P0 / 15 PD.
- **Phụ thuộc:** E05-T06, E05-T07, E04-T01.
- **Definition of Done:** concurrent/retry tests không duplicate; rollback không để partial effect; 100% committed rows có lineage; outbox eventual delivery test xanh.

#### E05-T09 — Retry, dead-letter, quarantine và replay

- **Mô tả:** stage retry policy, dead-letter reason, operator recovery, reproduce/reprocess modes.
- **Giá trị:** phục hồi không upload lại hoặc sửa DB thủ công.
- **Tác động kỹ thuật:** orchestration state, jobs, admin APIs/UI, runbooks.
- **Ưu tiên / Ước lượng:** P1 / 10 PD.
- **Phụ thuộc:** E05-T08.
- **Definition of Done:** failure injection cho parse/DB/worker/network; batch resume đúng stage; replay comparison report; operator drill thành công.

---

## EPIC E06 — Windows Agent

### Feature E06-F01 — Secure edge ingestion

#### E06-T01 — Agent architecture spike và threat model

- **Mô tả:** quyết định Windows Service runtime, authentication, spool, update và folder permissions.
- **Giá trị:** giảm rework và rủi ro cài agent trên máy nội bộ.
- **Tác động kỹ thuật:** ADR, threat model, prototype handshake.
- **Ưu tiên / Ước lượng:** P1 / 5 PD.
- **Phụ thuộc:** Security, Infrastructure, E05-T03.
- **Definition of Done:** ADR/threat model ký duyệt; certificate/update/spool choices; PoC authenticate và upload receipt.

#### E06-T02 — Watched-folder Agent với durable spool

- **Mô tả:** file stability, checksum, manifest, queued upload, retry và acknowledgement.
- **Giá trị:** tự động hóa nguồn Excel mà không mất file khi mạng lỗi.
- **Tác động kỹ thuật:** Windows Service, local embedded queue, connector client.
- **Ưu tiên / Ước lượng:** P1 / 15 PD.
- **Phụ thuộc:** E06-T01, E05-T02/E05-T03.
- **Definition of Done:** reboot/network-loss/disk-limit tests; chỉ archive sau ack; duplicate-safe; least-privilege install guide.

#### E06-T03 — Agent management, telemetry và signed updates

- **Mô tả:** registration, heartbeat, config, certificate rotation, ring deployment, signed upgrade/rollback.
- **Giá trị:** vận hành đội agent an toàn và quan sát được.
- **Tác động kỹ thuật:** management API/UI, code signing, deployment packaging, metrics.
- **Ưu tiên / Ước lượng:** P1 / 12 PD.
- **Phụ thuộc:** E06-T02, code-sign certificate.
- **Definition of Done:** central inventory/version/health; unsigned package bị từ chối; staged rollout và rollback thử thành công; expiry alerts hoạt động.

---

## EPIC E07 — Operational Modules

### Feature E07-F01 — New Models và Tasks

#### E07-T01 — Hoàn thiện New Models activation end-to-end

- **Mô tả:** API/use case activation từ canonical MasterPlan, owner/scope, concurrency và audit.
- **Giá trị:** chuyển kế hoạch đã duyệt thành workspace thật.
- **Tác động kỹ thuật:** aggregate, endpoint, UI adapter, task/event integration.
- **Ưu tiên / Ước lượng:** P1 / 10 PD.
- **Phụ thuộc:** E03-T05, E02-T05, E04-T01.
- **Definition of Done:** create/view/idempotent activation; duplicate/concurrency denied đúng; audit/permission/E2E xanh; không dùng `current_user`.

#### E07-T02 — Task API và persistence

- **Mô tả:** CRUD, assignment, status, checklist, comment, attachment, dependency và optimistic concurrency.
- **Giá trị:** task không mất sau reload và có nguồn sự thật chung.
- **Tác động kỹ thuật:** Tasks module use cases/API/schema, generated client.
- **Ưu tiên / Ước lượng:** P1 / 18 PD.
- **Phụ thuộc:** E03-T04, E04-T01, E02-T05.
- **Definition of Done:** TaskContext chỉ còn UI adapter/cache; permissions/audit; dependency cycle prevention; API/E2E critical flows xanh.

#### E07-T03 — Task approval và notification workflow

- **Mô tả:** approval policy, escalation, due reminders và links từ New Models/Data Hub.
- **Giá trị:** công việc chất lượng có trách nhiệm và deadline rõ.
- **Tác động kỹ thuật:** state machine, outbox notifications, templates, UI.
- **Ưu tiên / Ước lượng:** P2 / 12 PD.
- **Phụ thuộc:** E07-T02, E05-T08.
- **Definition of Done:** approval theo permission/scope; notification idempotent; SLA/escalation tests; audit complete.

### Feature E07-F02 — Chat và collaboration

#### E07-T04 — Chat persistence và authorized SignalR

- **Mô tả:** conversation membership, persisted message, read receipt, reconnect và server-derived sender.
- **Giá trị:** chat thật, không mất lịch sử và không spoof sender.
- **Tác động kỹ thuật:** Chat APIs, SignalR hub/service, SQL indexes, frontend adapter.
- **Ưu tiên / Ước lượng:** P2 / 15 PD.
- **Phụ thuộc:** E02-T06, E04-T01.
- **Definition of Done:** only members join/read/send; message survives restart; reconnect/catch-up; audit/moderation policy được xác nhận.

#### E07-T05 — Retire static users/mock chat/tasks

- **Mô tả:** feature flags chuyển read/write sang server; xóa static JSON/mock paths sau stability window.
- **Giá trị:** một nguồn sự thật và giảm bundle/technical debt.
- **Tác động kỹ thuật:** data adapters, migration flags, code deletion sau verification.
- **Ưu tiên / Ước lượng:** P1 / 7 PD.
- **Phụ thuộc:** E07-T02, E07-T04, hai release ổn định.
- **Definition of Done:** production metrics không gọi mock; rollback flag đã thử; static PII removed; dead code scan sạch.

---

## EPIC E08 — Design System, Accessibility và i18n

### Feature E08-F01 — Foundations

#### E08-T01 — UI inventory và semantic design tokens

- **Mô tả:** audit colors/spacing/type/motion/z-index; định nghĩa primitive/semantic tokens cho light/dark/high contrast.
- **Giá trị:** giao diện nhất quán, theme và rebrand rẻ hơn.
- **Tác động kỹ thuật:** CSS variables, token files, migration lint.
- **Ưu tiên / Ước lượng:** P1 / 8 PD.
- **Phụ thuộc:** Design/Product sign-off.
- **Definition of Done:** token catalogue; contrast verified; critical screens proof; rule không cho raw color mới ngoài token layer.

#### E08-T02 — Component catalog và accessibility contract

- **Mô tả:** Storybook/catalog cho Button, Form, Dialog, Navigation, Table, Status và error/loading patterns.
- **Giá trị:** tăng tốc UI và giảm lỗi tương tác.
- **Tác động kỹ thuật:** component APIs, stories, interaction/axe/visual tests.
- **Ưu tiên / Ước lượng:** P1 / 15 PD.
- **Phụ thuộc:** E08-T01, E01-T05.
- **Definition of Done:** stable components có docs/keyboard/a11y tests; axe serious/critical bằng 0; themes/locales stories.

#### E08-T03 — Hợp nhất App Shell và navigation

- **Mô tả:** một layout system cho dashboard/tasks, responsive navigation, breadcrumbs và command palette.
- **Giá trị:** trải nghiệm nhất quán, giảm duplicate Header/Chat/AuthProvider.
- **Tác động kỹ thuật:** Next layouts, design system primitives, route metadata.
- **Ưu tiên / Ước lượng:** P1 / 10 PD.
- **Phụ thuộc:** E08-T02, E02-T03.
- **Definition of Done:** dashboard/tasks dùng cùng shell; deep link/back/refresh/mobile/keyboard tests; capability visibility đúng.

### Feature E08-F02 — Localization

#### E08-T04 — Kiến trúc i18n và glossary

- **Mô tả:** message namespaces, ICU, locale/timezone formatting, glossary vi/en và pseudo-localization.
- **Giá trị:** loại text hard-code/mojibake, chuẩn bị tiếng Hàn.
- **Tác động kỹ thuật:** i18n runtime, server/client loading, CI key checks.
- **Ưu tiên / Ước lượng:** P1 / 8 PD.
- **Phụ thuộc:** E01-T03, Product translators.
- **Definition of Done:** critical flow vi/en; no missing keys; dates/numbers locale-aware; pseudo-locale không vỡ layout.

#### E08-T05 — Migrate critical flows sang design system

- **Mô tả:** Login, Data Hub import/review, Master Plan, Tasks theo component/tokens mới.
- **Giá trị:** critical UX accessible trước go-live.
- **Tác động kỹ thuật:** component decomposition, responsive/data-dense patterns, visual regression.
- **Ưu tiên / Ước lượng:** P1 / 20 PD.
- **Phụ thuộc:** E08-T02–T04 và API tương ứng ổn định.
- **Definition of Done:** WCAG AA critical journeys; keyboard/screen reader UAT; no raw tokens/hard-coded strings mới; adoption metrics ≥ 80% critical UI.

---

## EPIC E09 — Observability, Audit và Operations

### Feature E09-F01 — Telemetry and audit

#### E09-T01 — Structured logging và OpenTelemetry

- **Mô tả:** chuẩn log schema, trace/correlation xuyên web/API/worker/Agent, PII redaction.
- **Giá trị:** chẩn đoán nhanh mà không lộ dữ liệu nhạy cảm.
- **Tác động kỹ thuật:** instrumentation, collectors/exporters, log policy.
- **Ưu tiên / Ước lượng:** P1 / 10 PD.
- **Phụ thuộc:** observability platform, E03-T01.
- **Definition of Done:** trace upload→commit và web→API xem được; redaction tests; dashboards service cơ bản; sampling/retention documented.

#### E09-T02 — Business audit service và viewer

- **Mô tả:** append-only audit cho login, role/config, review, commit, activation, approval và export.
- **Giá trị:** accountability, điều tra và compliance.
- **Tác động kỹ thuật:** audit schema/service, query API/UI, retention/access policy.
- **Ưu tiên / Ước lượng:** P1 / 12 PD.
- **Phụ thuộc:** E02-T05, E04-T01.
- **Definition of Done:** critical events coverage test; before/after/reason/traceId; Auditor permission; audit không sửa/xóa qua app runtime.

#### E09-T03 — SLO dashboards và alerts

- **Mô tả:** availability/latency/error, Data Hub stages, review SLA và Agent health; alert routing/runbook links.
- **Giá trị:** phát hiện vấn đề trước người dùng và đo readiness.
- **Tác động kỹ thuật:** metrics, dashboards, alert rules, synthetic checks.
- **Ưu tiên / Ước lượng:** P1 / 8 PD.
- **Phụ thuộc:** E09-T01, E05-T09, E06-T03.
- **Definition of Done:** SLO được Product/Ops ký; alert test đến đúng owner; không alert thiếu runbook; monthly report có error budget.

### Feature E09-F02 — Production operations

#### E09-T04 — Health/readiness và operational runbooks

- **Mô tả:** health kiểm tra DB/storage/identity dependency; runbook cho stuck batch, outage, storage full, cert expiry.
- **Giá trị:** giảm MTTR và tránh health “xanh giả”.
- **Tác động kỹ thuật:** health endpoints, probes, docs, permission-safe diagnostics.
- **Ưu tiên / Ước lượng:** P1 / 6 PD.
- **Phụ thuộc:** E09-T01, target infrastructure.
- **Definition of Done:** dependency failure làm readiness fail đúng; runbook drill; support ID/log lookup hoạt động.

#### E09-T05 — Backup, restore và disaster recovery exercise

- **Mô tả:** SQL/raw/config backup, restore automation, RPO/RTO và diễn tập định kỳ.
- **Giá trị:** phục hồi được dữ liệu thay vì chỉ tin backup job.
- **Tác động kỹ thuật:** SQL/storage jobs, access controls, DR environment/runbook.
- **Ưu tiên / Ước lượng:** P0 / 8 PD.
- **Phụ thuộc:** E04-T04, E05-T02, Operations.
- **Definition of Done:** restore thành công với reconciliation; RPO/RTO đạt; findings có owner/date; lịch drill hai lần/năm.

---

## EPIC E10 — Deployment, Rollout và Adoption

### Feature E10-F01 — Environment và release

#### E10-T01 — Chuẩn hóa DEV/TEST/UAT/PROD và config

- **Mô tả:** topology, DNS/TLS, config/secrets, synthetic/masked data và environment parity.
- **Giá trị:** giảm “works on my machine” và lỗi cấu hình production.
- **Tác động kỹ thuật:** infrastructure definitions, reverse proxy, environment config matrix.
- **Ưu tiên / Ước lượng:** P1 / 10 PD.
- **Phụ thuộc:** Infrastructure, E01-T06, E02-T01.
- **Definition of Done:** mỗi env deploy từ cùng artifact; TLS/config validation; UAT không dùng PII thô; environment checklist ký duyệt.

#### E10-T02 — Feature flags, canary và rollback automation

- **Mô tả:** flags theo module/connector/cohort, staged rollout, smoke và rollback gate.
- **Giá trị:** chuyển từng lát dọc với blast radius nhỏ.
- **Tác động kỹ thuật:** flag service/config, deployment pipeline, telemetry linkage.
- **Ưu tiên / Ước lượng:** P1 / 8 PD.
- **Phụ thuộc:** E10-T01, E09-T01.
- **Definition of Done:** flag changes audited; pilot cohort; automated post-deploy smoke; rollback rehearsal thành công.

### Feature E10-F02 — UAT và go-live

#### E10-T03 — UAT theo vai trò và dữ liệu thực tế đã kiểm soát

- **Mô tả:** kịch bản Admin/Data Steward/Leader/Staff/Agent operator, acceptance criteria và defect triage.
- **Giá trị:** xác nhận hệ thống hỗ trợ quy trình thật, không chỉ pass technical test.
- **Tác động kỹ thuật:** test data, UAT environment, traceable acceptance evidence.
- **Ưu tiên / Ước lượng:** P0 / 10 PD mỗi wave.
- **Phụ thuộc:** feature wave hoàn chỉnh, business users.
- **Definition of Done:** critical scenarios pass; Severity 1/2 bằng 0; known issues ký chấp nhận; sign-off theo vai trò.

#### E10-T04 — Training, pilot và hypercare

- **Mô tả:** đào tạo theo persona, pilot một nhóm, support model và hypercare 2–4 tuần.
- **Giá trị:** tăng adoption và phát hiện vấn đề với phạm vi nhỏ.
- **Tác động kỹ thuật:** support dashboards, feedback/incident intake, release communication.
- **Ưu tiên / Ước lượng:** P1 / 8 PD + hypercare.
- **Phụ thuộc:** E10-T02, E10-T03, E09-T03/T04.
- **Definition of Done:** training completion; pilot SLO đạt hai tuần; feedback có disposition; on-call và escalation active.

#### E10-T05 — Retire legacy và đóng chương trình

- **Mô tả:** xóa mock/legacy route/schema chỉ sau stability window; lưu archive và ADR cuối.
- **Giá trị:** giảm chi phí bảo trì và ambiguity nguồn sự thật.
- **Tác động kỹ thuật:** code/schema contract migration, deprecation communication, cleanup.
- **Ưu tiên / Ước lượng:** P2 / 10 PD theo module.
- **Phụ thuộc:** hai release ổn định, telemetry không còn consumer, backup.
- **Definition of Done:** consumer usage bằng 0; rollback window hết; archive/backup xác nhận; contract/schema cleanup scripts review; docs cập nhật.

---

## 2. Backlog sequencing đề xuất

### Release Foundation

E01 toàn bộ → E02-T01–T05 → E03-T01/T02/T05 → E09-T01.

### Release Secure UAT

E02-T06 → E03-T03/T04 → E04-T01–T03 → E10-T01.

### Release SQL/Data Hub

E04-T04 → E05-T01–T09 → E09-T02/T03 → E09-T05.

### Release Operational Modules

E07-T01–T05, E08-T01–T05, E10-T02/T03 theo từng feature flag wave.

### Release Agent và Production

E06-T01–T03 → E09-T04 → E10-T04; E10-T05 thực hiện dần sau stability window.

## 3. Backlog readiness checklist

Một task chỉ vào sprint khi có owner, acceptance examples, dependency sẵn sàng, test/data/environment plan, security/privacy classification và estimate đã được team xác nhận. Task liên quan migration/destructive cleanup phải có rollback và backup plan trước khi bắt đầu.

