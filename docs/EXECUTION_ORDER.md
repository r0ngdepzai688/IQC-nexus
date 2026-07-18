# IQC Nexus Execution Order

## 1. Kết luận kiểm tra chéo

Năm tài liệu hiện tại thống nhất về hướng kiến trúc: modular monolith, security-first, SQL Server, Data Hub versioned, Windows Agent ở edge, API contract-first và migration theo strangler. Backlog có 53 task, tổng estimate danh nghĩa khoảng **497 person-days**, chưa bao gồm đầy đủ thời gian chờ phê duyệt, procurement, maintenance window, UAT lặp theo wave, hypercare và cleanup theo từng module.

**Đánh giá:** kế hoạch có thể thực thi nhưng cần điều chỉnh thứ tự và dự phòng timeline. Mốc 22 sprint chỉ hợp lý nếu squad duy trì khoảng 30 person-days hiệu dụng mỗi sprint, external dependencies sẵn sàng đúng hạn và các lane backend/frontend/platform chạy song song. Baseline quản trị nên là **24–26 sprint (11–13 tháng)**, với target 22 sprint và contingency 2–4 sprint.

### Phạm vi kiểm tra

- Nguồn: `MIGRATION_PLAN.md`, `PRODUCT_BACKLOG.md`, `ARCHITECTURE_TARGET.md`, `DATA_HUB_ARCHITECTURE.md`, `DESIGN_SYSTEM_PLAN.md`.
- Kiểm tra: trùng lặp, dependency, timeline, tính đo lường của DoD và thiếu sót risk register.
- File ảnh hưởng dưới đây là **phạm vi dự kiến khi triển khai**, không phải thay đổi được thực hiện trong lần lập tài liệu này.

## 2. Task giao nhau nhưng không trùng hoàn toàn

Không có task nào có cùng mục tiêu và cùng output đến mức phải xóa. Các cụm sau cần một owner/deliverable chung để tránh làm lặp:

| Cụm | Task liên quan | Ranh giới cần giữ |
|---|---|---|
| Secrets và dữ liệu nhạy cảm | E01-T02, E02-T01 | E01 phân loại/loại dữ liệu; E02 xoay và vận hành secrets |
| Encoding và i18n | E01-T03, E08-T04 | E01 sửa baseline/UTF-8; E08 quản lý message/locale/glossary |
| CI và environments | E01-T06, E10-T01 | E01 tạo artifact; E10 promote cùng artifact qua môi trường |
| Correlation và telemetry | E03-T01, E09-T01 | E03 định nghĩa contract `traceId`; E09 thu thập/export trace |
| Data lineage và audit | E05-T08, E09-T02 | E05 ghi lineage/commit audit; E09 cung cấp audit platform/viewer |
| Monitoring | E05-T09, E06-T03, E09-T03 | Module phát metrics; E09 tổng hợp SLO/dashboard/alert |
| Backup và DR | E04-T04, E09-T05 | E04 bảo vệ cutover; E09 vận hành backup/DR định kỳ |
| Rollback/retirement | E04-T04, E10-T02, E10-T05 | DB rollback, release rollback và legacy retirement là ba cấp khác nhau |

## 3. Dependency và timeline đã hiệu chỉnh

### Điều chỉnh dependency

1. E09-T01 được đưa lên sớm sau API standards để các wave sau có telemetry ngay từ đầu.
2. E10-T01 và E10-T02 phải hoàn tất trước migration/canary, không để đến giai đoạn go-live.
3. Windows Agent E06-T01 bắt đầu ngay sau connector contract; E06-T02/T03 chạy cùng cuối Data Hub, không chờ xong Operational Modules.
4. E09-T02 phải có audit service contract trước khi Data Hub commit và module mới phụ thuộc sâu vào audit.
5. Design-system foundations chạy song song từ sau baseline; migration critical UI chỉ bắt đầu khi API của từng flow ổn định.
6. E10-T03 là activity lặp theo wave. Trong danh sách đánh số nó là một task quản trị, nhưng kế hoạch sprint phải tạo execution instance cho SQL/Data Hub, Modules và Production.
7. E07-T05 và E10-T05 bị chặn bởi stability window; không được kéo sớm chỉ để “đóng sprint”.

### Timeline khuyến nghị

| Khoảng sprint | Lane chính | Task tiêu biểu |
|---|---|---|
| 1–2 | Baseline | Task 001–006 |
| 3–5 | Security, API foundation, design foundation | Task 007–018, một phần 011–013 chạy song song |
| 6–8 | Contract, module boundary, release controls | Task 019–024 |
| 9–12 | SQL Server và cutover | Task 025–029 |
| 13–17 | Data Hub và Agent | Task 030–044; Agent chạy song song pipeline |
| 18–21 | Operational modules và critical UX | Task 045–049 |
| 22–24 | UAT, pilot, hypercare | Task 050–051 |
| 25–26 | Stability window và retirement | Task 052–053; có thể là calendar time hơn là full sprint capacity |

## 4. Kiểm tra Definition of Done

Toàn bộ 53 task đều có DoD. Phần lớn có output/test/sign-off kiểm chứng được. Tuy nhiên các task sau phải điền baseline số cụ thể tại refinement trước khi đạt Definition of Ready:

- E01-T03/E01-T04: danh sách “critical flows/pages”.
- E03-T02: tỷ lệ critical pages phải chuyển sang generated client.
- E03-T03: kích thước dataset, concurrency và P95 mục tiêu.
- E05-T04: corpus, precision/recall và confidence threshold.
- E05-T06: rows/batch, file size và time/memory target.
- E06-T02: spool capacity, disk watermark, retry duration và file-size limit.
- E07-T02: danh sách critical Task journeys.
- E08-T01: contrast standard và danh sách proof screens.
- E08-T04/E08-T05: critical journeys/locales/browser matrix.
- E09-T02: danh mục “critical events” và retention.
- E09-T03: SLO window, burn-rate và alert thresholds.
- E10-T01: tiêu chí environment parity.
- E10-T03: critical UAT scenarios và pass-rate.
- E10-T04: training completion target, pilot cohort và success threshold.

Những DoD này không sai, nhưng chưa hoàn toàn đo lường được cho đến khi các ngưỡng được ghi vào acceptance criteria của sprint.

## 5. Rủi ro còn thiếu cần bổ sung vào risk register

| ID | Rủi ro thiếu | Xác suất/Tác động | Kiểm soát đề xuất |
|---|---|---|---|
| R11 | OIDC/claims hoặc nhóm AD không sẵn sàng đúng hạn | Trung/Rất cao | Spike sớm, identity sandbox, deadline go/no-go và adapter tạm có sunset |
| R12 | Procurement SQL Server, certificate, observability hoặc malware scanner chậm | Trung/Cao | Procurement plan ở Sprint 1, owner/date và fallback được ADR phê duyệt |
| R13 | Thiếu capacity DBA/DevOps/Security, phụ thuộc “bán thời gian” trở thành critical path | Cao/Cao | Reservation theo sprint, RACI và escalation khi SLA hỗ trợ bị lỡ |
| R14 | SQL collation, Unicode, timezone hoặc precision làm đổi dữ liệu | Trung/Rất cao | Contract tests đa locale, migration rehearsal và field-level reconciliation |
| R15 | Retention/PII/legal hold chưa được phê duyệt | Trung/Cao | Data classification workshop, retention matrix trước raw/audit storage |
| R16 | Audit/lineage tăng nhanh làm phình DB và giảm hiệu năng | Trung/Cao | Volume model, partition/archive, index/performance test và retention job |
| R17 | Agent update/certificate supply-chain bị xâm phạm | Thấp/Rất cao | Code signing, pinned trust, rotation, revocation và staged rollout |
| R18 | File share/service account permission drift | Cao/Trung | Automated preflight, health check, least privilege và periodic access review |
| R19 | Cutover trùng chu kỳ nghiệp vụ, không được data freeze | Trung/Cao | Business calendar, rehearsal, freeze owner và abort criteria |
| R20 | Feature flags tồn tại quá lâu tạo nhiều nhánh behavior | Cao/Trung | Expiry/owner bắt buộc, stale-flag dashboard và cleanup gate |
| R21 | Notification/email infrastructure chưa có nhưng workflow phụ thuộc | Trung/Trung | Notification ADR/adapter, fallback in-app queue và delivery monitoring |
| R22 | Single-site outage ảnh hưởng cả SQL, raw storage và identity | Thấp/Rất cao | Failure-domain review, offsite backup và DR environment/exercise |
| R23 | Load/volume thật chưa biết làm estimate Data Hub sai | Cao/Cao | Capacity baseline ở Sprint 1–2, synthetic scale corpus và quarterly review |
| R24 | Browser/Windows fleet không đồng nhất | Trung/Trung | Supported matrix, Agent OS inventory và pilot cohort đại diện |
| R25 | Dependency/framework EOL hoặc breaking upgrade trong chương trình dài | Trung/Trung | Dependency policy, LTS pinning, upgrade calendar và SBOM alerts |

## 6. Thứ tự thực hiện chi tiết

### Task 001 — E01-T01: Đưa source Data Hub vào version control

- **Mục tiêu:** bảo đảm clone sạch chứa toàn bộ source bắt buộc để build Data Hub.
- **File sẽ bị ảnh hưởng:** `.gitignore`; `backend/src/**/DataHub/**`; `docs/datahub/**`; CI clone-check script.
- **Rủi ro:** vô tình commit raw files, database hoặc PII; thay rule ignore quá rộng.
- **Điều kiện bắt đầu:** quyền đọc repo và inventory file local hiện tại.
- **Điều kiện hoàn thành:** clone mới build backend; `git check-ignore` đúng; không có runtime data được track.
- **Ước lượng thời gian:** 2 PD.

### Task 002 — E01-T02: Phân loại và loại dữ liệu/binary khỏi repository

- **Mục tiêu:** tách PII, DB, Excel, ZIP/export khỏi source và thay bằng fixtures synthetic.
- **File sẽ bị ảnh hưởng:** `.gitignore`; root data files; `frontend/src/data/**`; `backend/**/Seeders/**`; `docs/security/data-inventory.md` dự kiến.
- **Rủi ro:** xóa nhầm dữ liệu duy nhất; fixture synthetic không đại diện; Git history vẫn chứa PII.
- **Điều kiện bắt đầu:** Task 001 hoàn tất; Data Owner/Security tham gia.
- **Điều kiện hoàn thành:** inventory ký duyệt; test không dùng PII; nơi lưu/retention/history-cleanup có quyết định.
- **Ước lượng thời gian:** 3 PD.

### Task 003 — E01-T04: Làm xanh TypeScript và ESLint

- **Mục tiêu:** tạo frontend baseline compile/lint xanh trước refactor.
- **File sẽ bị ảnh hưởng:** `frontend/src/**`; `frontend/eslint.config.mjs`; `frontend/tsconfig.json`; test smoke frontend.
- **Rủi ro:** sửa type làm đổi behavior; suppress lint thay vì xử lý nguyên nhân.
- **Điều kiện bắt đầu:** Task 001 hoàn tất; danh sách critical pages được chốt.
- **Điều kiện hoàn thành:** TypeScript và ESLint không lỗi; warning có owner; critical-page smoke test xanh.
- **Ước lượng thời gian:** 8 PD.

### Task 004 — E01-T03: Chuẩn hóa UTF-8 và sửa mojibake

- **Mục tiêu:** bảo đảm source và dữ liệu Việt/Hàn round-trip đúng.
- **File sẽ bị ảnh hưởng:** `.editorconfig` dự kiến; C#/TS/JSON/Markdown có mojibake; parser fixtures; CI encoding check.
- **Rủi ro:** thay nội dung nghiệp vụ khi sửa encoding; Excel legacy dùng code page khác.
- **Điều kiện bắt đầu:** Task 002 hoàn tất; glossary/sample strings được xác nhận.
- **Điều kiện hoàn thành:** policy UTF-8; automated round-trip tests; không mojibake trong danh sách critical flows đã chốt.
- **Ước lượng thời gian:** 4 PD.

### Task 005 — E01-T05: Thiết lập test pyramid tối thiểu

- **Mục tiêu:** tạo unit, integration và E2E smoke làm safety net.
- **File sẽ bị ảnh hưởng:** `backend/tests/**` dự kiến; `frontend/src/**/*.test.*`; E2E config/tests; synthetic fixtures.
- **Rủi ro:** test phụ thuộc máy local; flaky E2E; fixture không phản ánh production.
- **Điều kiện bắt đầu:** Task 002, 003 hoàn tất.
- **Điều kiện hoàn thành:** test login denial, Data Hub parse/commit, route render chạy ổn định trong môi trường CI tương đương.
- **Ước lượng thời gian:** 7 PD.

### Task 006 — E01-T06: CI build, scan và artifact pipeline

- **Mục tiêu:** kiểm tra PR và tạo artifact/SBOM tái lập từ main.
- **File sẽ bị ảnh hưởng:** CI pipeline files; build scripts; package/project configs; artifact/version metadata.
- **Rủi ro:** runner thiếu network/runtime; scan false positive; artifact khác giữa môi trường.
- **Điều kiện bắt đầu:** Task 003, 005 hoàn tất; CI runner/artifact registry sẵn sàng.
- **Điều kiện hoàn thành:** required checks hoạt động; build đỏ không deploy; một artifact được ký/version và lưu cùng SBOM.
- **Ước lượng thời gian:** 6 PD.

### Task 007 — E02-T01: Xoay secrets và thiết lập secret management

- **Mục tiêu:** thu hồi key lộ và chuyển secrets sang nguồn cấu hình được quản lý.
- **File sẽ bị ảnh hưởng:** `backend/src/IqcQms.Api/config.json`; appsettings templates; deployment secret references; rotation runbook.
- **Rủi ro:** outage khi rotate; secret cũ còn hiệu lực; secret bị log.
- **Điều kiện bắt đầu:** Task 006; secret store/environment owner được xác định.
- **Điều kiện hoàn thành:** key cũ bị revoke; production không đọc secret tracked; rotation test và secret scan xanh.
- **Ước lượng thời gian:** 3 PD.

### Task 008 — E03-T01: Định nghĩa API v1 và Problem Details

- **Mục tiêu:** tạo chuẩn API chung trước khi mở rộng auth/module.
- **File sẽ bị ảnh hưởng:** API conventions/middleware; controllers/DTO samples; OpenAPI config; `docs/api/**` dự kiến.
- **Rủi ro:** versioning gây duplicate route; breaking existing UI; error contract thiếu ổn định.
- **Điều kiện bắt đầu:** Task 005; Tech Lead/Product thống nhất compatibility policy.
- **Điều kiện hoàn thành:** `/api/v1` mẫu, RFC 7807, traceId, validation và contract tests được phê duyệt.
- **Ước lượng thời gian:** 6 PD.

### Task 009 — E09-T01: Structured logging và OpenTelemetry

- **Mục tiêu:** có trace/log chuẩn trước khi migration tạo thêm complexity.
- **File sẽ bị ảnh hưởng:** API/worker/frontend instrumentation; logging config; collector/deployment config; redaction tests.
- **Rủi ro:** log PII; chi phí/volume cao; trace bị đứt qua async jobs.
- **Điều kiện bắt đầu:** Task 008; observability platform/export endpoint được phê duyệt.
- **Điều kiện hoàn thành:** web→API và upload→worker trace xem được; redaction test xanh; retention/sampling có owner.
- **Ước lượng thời gian:** 10 PD.

### Task 010 — E10-T01: Chuẩn hóa DEV/TEST/UAT/PROD

- **Mục tiêu:** promote cùng artifact qua môi trường có TLS, config và dữ liệu phù hợp.
- **File sẽ bị ảnh hưởng:** deployment/IIS configs; environment templates; DNS/TLS definitions; environment matrix docs.
- **Rủi ro:** configuration drift; UAT chứa PII; hạ tầng cấp chậm.
- **Điều kiện bắt đầu:** Task 006, 007; Infrastructure capacity và DNS/TLS owner sẵn sàng.
- **Điều kiện hoàn thành:** deploy cùng artifact ở mọi môi trường; checklist parity định lượng; UAT dùng dữ liệu mask/synthetic.
- **Ước lượng thời gian:** 10 PD.

### Task 011 — E08-T01: UI inventory và semantic design tokens

- **Mục tiêu:** chốt foundation giao diện mà không chờ xong backend.
- **File sẽ bị ảnh hưởng:** `frontend/src/app/globals.css`; `frontend/src/design-system/tokens/**` dự kiến; UI inventory/docs.
- **Rủi ro:** token hóa quá sớm theo màn hình cũ; contrast chưa đạt; scope thành redesign.
- **Điều kiện bắt đầu:** Design/Product sign-off; Task 003 baseline xanh.
- **Điều kiện hoàn thành:** catalogue tokens light/dark/high-contrast; WCAG contrast test; lint rule cho raw color mới.
- **Ước lượng thời gian:** 8 PD.

### Task 012 — E08-T04: Kiến trúc i18n và glossary

- **Mục tiêu:** tạo message/locale framework và thuật ngữ vi/en nhất quán.
- **File sẽ bị ảnh hưởng:** `frontend/src/lib/i18n/**`; message catalogues; locale middleware/config; glossary docs.
- **Rủi ro:** key churn; dịch sai thuật ngữ; hydration mismatch; layout vỡ khi text dài.
- **Điều kiện bắt đầu:** Task 004; Product translators/glossary owner sẵn sàng.
- **Điều kiện hoàn thành:** critical-flow list vi/en; missing-key CI; locale date/number tests; pseudo-locale không vỡ layout.
- **Ước lượng thời gian:** 8 PD.

### Task 013 — E08-T02: Component catalog và accessibility contract

- **Mục tiêu:** cung cấp primitives/patterns có test cho các wave UI.
- **File sẽ bị ảnh hưởng:** `frontend/src/design-system/components/**`; Storybook/catalog config; interaction/axe/visual tests.
- **Rủi ro:** trùng component hiện hữu; bundle tăng; visual regression không ổn định.
- **Điều kiện bắt đầu:** Task 005, 011; browser/a11y matrix được chốt.
- **Điều kiện hoàn thành:** component stable có docs, keyboard contract, themes/locales stories; axe serious/critical bằng 0.
- **Ước lượng thời gian:** 15 PD.

### Task 014 — E02-T02: Quyết định và tích hợp OIDC/BFF

- **Mục tiêu:** triển khai SSO/session đáng tin cậy, không dùng token localStorage.
- **File sẽ bị ảnh hưởng:** frontend auth/session boundaries; backend authentication config; reverse-proxy callback config; ADR/tests.
- **Rủi ro:** IdP chậm; callback/CSRF/session lỗi; HA session chưa rõ.
- **Điều kiện bắt đầu:** Task 007; OIDC sandbox, redirect URIs và claims sample sẵn sàng.
- **Điều kiện hoàn thành:** ADR ký; login/logout/expiry/revoke UAT; CSRF/callback tests; browser không giữ access token.
- **Ước lượng thời gian:** 13 PD gồm spike 3 PD.

### Task 015 — E02-T03: Loại mock login và dev identity khỏi production

- **Mục tiêu:** chỉ session thật mới truy cập dashboard production.
- **File sẽ bị ảnh hưởng:** login page; `AuthContext`; dashboard/tasks layouts; Header dev override; production flags/E2E.
- **Rủi ro:** khóa nhầm developer; redirect loop; dev path lọt production bundle.
- **Điều kiện bắt đầu:** Task 014.
- **Điều kiện hoàn thành:** fake token bị từ chối; anonymous redirect đúng; override không nằm production bundle; auth E2E xanh.
- **Ước lượng thời gian:** 5 PD.

### Task 016 — E02-T04: Permission catalogue và role mapping

- **Mục tiêu:** thống nhất system/business roles, permissions và data scopes.
- **File sẽ bị ảnh hưởng:** permission models/config/seed; policy docs; user/role migration mapping; frontend capability types.
- **Rủi ro:** business chưa đồng thuận; role explosion; scope mapping sai.
- **Điều kiện bắt đầu:** Task 014; Product/Security/business owners tham gia workshop.
- **Điều kiện hoàn thành:** catalogue có version/owner; role map ký duyệt; không còn tên role mâu thuẫn.
- **Ước lượng thời gian:** 5 PD.

### Task 017 — E02-T05: Authorization deny-by-default

- **Mục tiêu:** mọi endpoint/hub được bảo vệ bằng policy và resource scope.
- **File sẽ bị ảnh hưởng:** Program/auth policies; controllers; SignalR hubs; resource handlers; authorization tests.
- **Rủi ro:** khóa luồng hợp lệ; IDOR do resource loader sai; endpoint mới thiếu policy.
- **Điều kiện bắt đầu:** Task 016; endpoint inventory hoàn chỉnh.
- **Điều kiện hoàn thành:** coverage 100%; explicit anonymous allowlist; 401/403/scope matrix xanh.
- **Ước lượng thời gian:** 10 PD.

### Task 018 — E02-T06: Bảo vệ upload, SignalR và abuse controls

- **Mục tiêu:** đóng path traversal, spoofing, oversize và abuse paths.
- **File sẽ bị ảnh hưởng:** upload services/controllers; SignalR hub; rate-limit/security middleware; CORS/proxy config; security tests.
- **Rủi ro:** chặn file hợp lệ; malware scanner chưa có; CORS làm gián đoạn Agent/UI.
- **Điều kiện bắt đầu:** Task 017; file limits/allowed types và origin matrix được duyệt.
- **Điều kiện hoàn thành:** traversal/oversize/spoof tests xanh; sender từ claims; High/Critical findings bằng 0.
- **Ước lượng thời gian:** 8 PD.

### Task 019 — E03-T02: Sinh TypeScript client từ OpenAPI

- **Mục tiêu:** một client typed thay fetch/axios hard-code.
- **File sẽ bị ảnh hưởng:** OpenAPI codegen config; `frontend/src/lib/api/**`; generated types/client; consuming pages.
- **Rủi ro:** generated diff lớn; custom error/auth bị mất; bundle tăng.
- **Điều kiện bắt đầu:** Task 008, 014; critical-page list và API v1 mẫu ổn định.
- **Điều kiện hoàn thành:** tỷ lệ critical pages đã chốt dùng generated client; không localhost hard-code; contract diff CI hoạt động.
- **Ước lượng thời gian:** 6 PD.

### Task 020 — E03-T04: Tạo module/application use-case boundaries

- **Mục tiêu:** tách EF khỏi controller và thiết lập ranh giới modular monolith.
- **File sẽ bị ảnh hưởng:** backend API/Application/Domain/Infrastructure structure; DI; controllers/services; architecture tests.
- **Rủi ro:** refactor rộng gây regression; abstraction thừa; transaction boundary bị thay đổi.
- **Điều kiện bắt đầu:** Task 008; behavior tests baseline tồn tại.
- **Điều kiện hoàn thành:** controller Auth/Users/New Models/Data Hub mỏng; dependency rules được test; regression xanh.
- **Ước lượng thời gian:** 12 PD.

### Task 021 — E03-T03: Pagination và query safeguards

- **Mục tiêu:** chặn collection vô hạn và query tùy ý.
- **File sẽ bị ảnh hưởng:** API query DTOs/controllers/services; EF projections; generated client; data grids/pages.
- **Rủi ro:** breaking UI; sort/filter thiếu index; count query đắt.
- **Điều kiện bắt đầu:** Task 008, 019; dataset/concurrency/P95 acceptance được chốt.
- **Điều kiện hoàn thành:** limits/allowlists/cancellation áp dụng; invalid query trả Problem Details; load test đạt ngưỡng.
- **Ước lượng thời gian:** 7 PD.

### Task 022 — E03-T05: Hợp nhất Master Plan legacy/core

- **Mục tiêu:** một aggregate/business key và activation ID đúng.
- **File sẽ bị ảnh hưởng:** MasterPlan domain/entities/services/controllers; EF mappings/migrations; frontend master-plan APIs/pages; compatibility tests.
- **Rủi ro:** map sai record; phá history; business key chưa thống nhất.
- **Điều kiện bắt đầu:** Task 020; Product ký business key và merge rules.
- **Điều kiện hoàn thành:** canonical aggregate; data reconciliation; activation dùng rowversion/đúng ID; deprecation date/test.
- **Ước lượng thời gian:** 8 PD.

### Task 023 — E08-T03: Hợp nhất App Shell và navigation

- **Mục tiêu:** loại layout/provider trùng giữa dashboard và Tasks.
- **File sẽ bị ảnh hưởng:** Next layouts; Header/Sidebar/FloatingChat composition; design-system layouts; route metadata/tests.
- **Rủi ro:** route regression; focus/navigation mobile lỗi; capability flicker.
- **Điều kiện bắt đầu:** Task 013, 015; route/capability map được duyệt.
- **Điều kiện hoàn thành:** một shell; deep-link/refresh/back/mobile/keyboard tests; visibility theo capability đúng.
- **Ước lượng thời gian:** 10 PD.

### Task 024 — E10-T02: Feature flags, canary và rollback automation

- **Mục tiêu:** triển khai từng cohort/module với blast radius nhỏ.
- **File sẽ bị ảnh hưởng:** feature-flag config/service; frontend/backend flag adapters; deployment pipeline; audit/telemetry docs.
- **Rủi ro:** stale flags; behavior matrix quá lớn; flag bị dùng thay authorization.
- **Điều kiện bắt đầu:** Task 009, 010; platform/owner/expiry policy được chốt.
- **Điều kiện hoàn thành:** flag changes audited; expiry bắt buộc; canary smoke và rollback rehearsal thành công.
- **Ước lượng thời gian:** 8 PD.

### Task 025 — E04-T01: Thiết kế schema SQL Server và constraints

- **Mục tiêu:** schema module hóa có FK, unique keys, rowversion và data types đúng.
- **File sẽ bị ảnh hưởng:** EF configurations/entities; SQL Server migrations/DDL; ERD; database conventions docs.
- **Rủi ro:** collation/precision/timezone sai; lock/index overhead; migration legacy phức tạp.
- **Điều kiện bắt đầu:** Task 022; SQL instance/DBA và data classification sẵn sàng.
- **Điều kiện hoàn thành:** ERD/DDL DBA review; empty/snapshot migration tests; integrity keys đầy đủ.
- **Ước lượng thời gian:** 10 PD.

### Task 026 — E09-T02: Business audit service và viewer

- **Mục tiêu:** một audit platform append-only cho mọi module.
- **File sẽ bị ảnh hưởng:** audit schema/domain/application/API; audit UI; permission/retention configs; coverage tests.
- **Rủi ro:** log PII; volume lớn; audit có thể bị runtime user sửa; event catalogue thiếu.
- **Điều kiện bắt đầu:** Task 017, 025; critical-event catalogue và retention được duyệt.
- **Điều kiện hoàn thành:** event coverage test; before/after/reason/traceId; Auditor access; runtime không update/delete audit.
- **Ước lượng thời gian:** 12 PD.

### Task 027 — E04-T02: Tách DB migration khỏi startup

- **Mục tiêu:** database deployment có script, approval và least privilege riêng.
- **File sẽ bị ảnh hưởng:** `Program.cs`; EF migration scripts; CI/CD database stage; DB accounts/readiness checks.
- **Rủi ro:** app/schema mismatch; migration quyền quá rộng; failed script khó rollback.
- **Điều kiện bắt đầu:** Task 006, 025; DBA approval workflow.
- **Điều kiện hoàn thành:** production app không tự migrate; backup→migrate→verify pipeline; failure chặn rollout.
- **Ước lượng thời gian:** 5 PD.

### Task 028 — E04-T03: Migration/reconciliation SQLite → SQL Server

- **Mục tiêu:** công cụ ETL idempotent và báo cáo đối soát.
- **File sẽ bị ảnh hưởng:** `tools/migration/**` dự kiến; SQL/EF mappings; reconciliation reports/tests; secure config templates.
- **Rủi ro:** duplicate/lost rows; ID remap sai; Unicode/date precision drift.
- **Điều kiện bắt đầu:** Task 002, 025; snapshot SQLite và target SQL test sẵn sàng.
- **Điều kiện hoàn thành:** rerun không duplicate; count/checksum/business-key reconciliation; mọi exception có disposition.
- **Ước lượng thời gian:** 12 PD.

### Task 029 — E04-T04: Rehearsal và SQL Server cutover

- **Mục tiêu:** chuyển production có rollback, RPO/RTO và đối soát.
- **File sẽ bị ảnh hưởng:** deployment/config flags; cutover scripts; backup/restore jobs; runbook và UAT evidence.
- **Rủi ro:** data freeze thất bại; downtime vượt cửa sổ; rollback mất write mới.
- **Điều kiện bắt đầu:** Task 024, 027, 028; Operations/business freeze sign-off; hai rehearsal slots.
- **Điều kiện hoàn thành:** hai rehearsal; RPO ≤15 phút/RTO ≤4 giờ; reconciliation ký; rollback thử; SQLite read-only.
- **Ước lượng thời gian:** 8 PD cộng maintenance window.

### Task 030 — E05-T01: Source Registry và ingestion envelope

- **Mục tiêu:** chuẩn hóa source/profile/contract/receipt cho mọi connector.
- **File sẽ bị ảnh hưởng:** Data Hub domain/schema/API/UI; source/profile permissions; manifest/version contracts.
- **Rủi ro:** envelope quá chung hoặc thiếu metadata; source ownership không rõ.
- **Điều kiện bắt đầu:** Task 008, 025, 029; Data Steward/source owner được chỉ định.
- **Điều kiện hoàn thành:** Excel đi qua envelope; inactive source bị chặn; version/owner/SLA/audit được lưu.
- **Ước lượng thời gian:** 8 PD.

### Task 031 — E05-T02: Immutable raw store và idempotent receipt

- **Mục tiêu:** lưu payload nguyên bản an toàn và nhận lại không gây duplicate.
- **File sẽ bị ảnh hưởng:** storage abstraction; RawObject/Receipt schema; upload services; retention/cleanup jobs; storage config.
- **Rủi ro:** storage đầy; retention chưa duyệt; URI/path leak; checksum collision policy thiếu.
- **Điều kiện bắt đầu:** Task 030; storage ADR, quota, retention và encryption được phê duyệt.
- **Điều kiện hoàn thành:** streaming/immutable raw; outcome duplicate xác định; restore/replay test; path do server quản lý.
- **Ước lượng thời gian:** 10 PD.

### Task 032 — E05-T04: Header Detection Engine

- **Mục tiêu:** phát hiện header có confidence và đưa ambiguity vào review.
- **File sẽ bị ảnh hưởng:** Data Hub detection engine; parser adapter; detection-result schema; alias profiles; benchmark fixtures/tests.
- **Rủi ro:** corpus thiên lệch; false mapping ghi sai dữ liệu; performance preview kém.
- **Điều kiện bắt đầu:** Task 030; synthetic/approved corpus và precision/recall thresholds được chốt.
- **Điều kiện hoàn thành:** benchmark đạt threshold; ambiguous file không auto-commit; engine/evidence version được lưu.
- **Ước lượng thời gian:** 10 PD.

### Task 033 — E05-T03: Connector API SDK/contract

- **Mục tiêu:** capability contract chung cho Excel, API và Agent.
- **File sẽ bị ảnh hưởng:** application connector interfaces; ingestion endpoints; reference connectors; SDK docs/compatibility tests.
- **Rủi ro:** contract khóa chặt connector tương lai; auth/rate-limit không nhất quán.
- **Điều kiện bắt đầu:** Task 018, 030, 031.
- **Điều kiện hoàn thành:** Excel/API reference connector qua cùng gateway; compatibility/idempotency/rate-limit tests; onboarding guide.
- **Ước lượng thời gian:** 9 PD.

### Task 034 — E06-T01: Agent architecture spike và threat model

- **Mục tiêu:** chốt runtime, auth, spool, signing/update và folder permissions trước khi xây Agent.
- **File sẽ bị ảnh hưởng:** ADR/threat model; Agent PoC project; certificate/auth configuration samples.
- **Rủi ro:** certificate/procurement chậm; Windows fleet khác nhau; supply-chain threat bị đánh giá thiếu.
- **Điều kiện bắt đầu:** Task 033; Security/Infrastructure và representative Windows host sẵn sàng.
- **Điều kiện hoàn thành:** ADR/threat model ký; PoC authenticate/upload receipt; update/spool/certificate choices có owner.
- **Ước lượng thời gian:** 5 PD.

### Task 035 — E05-T05: Mapping Dictionary lifecycle

- **Mục tiêu:** steward quản lý mapping versioned với approval và rollback.
- **File sẽ bị ảnh hưởng:** mapping domain/schema/API/UI; authorization/audit; impact/conflict tests.
- **Rủi ro:** mapping conflict; regex abuse; thay mapping làm batch cũ không tái lập.
- **Điều kiện bắt đầu:** Task 016, 032; governance/effective-time rules được duyệt.
- **Điều kiện hoàn thành:** draft→publish→retire; four-eyes critical mapping; batch pin version; rollback/conflict tests.
- **Ước lượng thời gian:** 12 PD.

### Task 036 — E05-T06: Validation Engine và rule catalogue

- **Mục tiêu:** chuyển Master Plan rules thành engine/result versioned.
- **File sẽ bị ảnh hưởng:** validation framework; rule catalogue; result schema; parser/ingestion orchestration; benchmark tests.
- **Rủi ro:** rule order/side effects; reference snapshot drift; batch quá tốn memory/time.
- **Điều kiện bắt đầu:** Task 025, 035; file-size/rows/concurrency/time targets được chốt.
- **Điều kiện hoàn thành:** rules versioned; revalidation tạo run mới; evidence đầy đủ; benchmark đạt target.
- **Ước lượng thời gian:** 15 PD.

### Task 037 — E05-T07: Review Workflow có assignment và SLA

- **Mục tiêu:** ngoại lệ có owner, resolution semantics, approval và revalidation.
- **File sẽ bị ảnh hưởng:** ReviewCase workflow/domain/schema/API/UI; notifications adapter; policies/audit/E2E.
- **Rủi ro:** backlog nghẽn; override sai; notification dependency chưa sẵn sàng.
- **Điều kiện bắt đầu:** Task 017, 036; reviewer roles/SLA/escalation matrix được ký.
- **Điều kiện hoàn thành:** resolution không bypass validation; four-eyes critical path; SLA dashboard; E2E review xanh.
- **Ước lượng thời gian:** 15 PD.

### Task 038 — E05-T08: Commit pipeline idempotent và lineage

- **Mục tiêu:** commit atomic/idempotent với rowversion, audit, lineage và outbox.
- **File sẽ bị ảnh hưởng:** commit services; SQL constraints/schema; lineage/outbox tables/worker; core module adapters; tests.
- **Rủi ro:** deadlock; partial effect; field ownership sai; audit/lineage volume cao.
- **Điều kiện bắt đầu:** Task 025, 026, 036, 037.
- **Điều kiện hoàn thành:** concurrent/retry không duplicate; rollback sạch; 100% row có lineage; outbox delivery test xanh.
- **Ước lượng thời gian:** 15 PD.

### Task 039 — E05-T09: Retry, dead-letter, quarantine và replay

- **Mục tiêu:** operator phục hồi từng stage mà không sửa DB thủ công.
- **File sẽ bị ảnh hưởng:** pipeline orchestration/state; background jobs; quarantine/replay API/UI; runbooks/failure tests.
- **Rủi ro:** retry storm; replay sai version; quarantine chứa PII/malware.
- **Điều kiện bắt đầu:** Task 038; retry limits, retention và operator permission được duyệt.
- **Điều kiện hoàn thành:** failure injection các stage; resume đúng; reproduce/reprocess comparison; operator drill thành công.
- **Ước lượng thời gian:** 10 PD.

### Task 040 — E06-T02: Watched-folder Agent với durable spool

- **Mục tiêu:** phát hiện/upload file an toàn qua mất mạng và reboot.
- **File sẽ bị ảnh hưởng:** Windows Agent service project; spool database/files; connector client; installer/config/tests.
- **Rủi ro:** disk đầy; duplicate watcher events; file đang ghi; quyền service account sai.
- **Điều kiện bắt đầu:** Task 031, 033, 034; capacity/watermark/retry/file limits được chốt.
- **Điều kiện hoàn thành:** reboot/network/disk-limit tests; archive sau ack; duplicate-safe; install least privilege.
- **Ước lượng thời gian:** 15 PD.

### Task 041 — E06-T03: Agent management, telemetry và signed updates

- **Mục tiêu:** quản lý fleet, certificate và update/rollback có chữ ký.
- **File sẽ bị ảnh hưởng:** Agent management API/UI/schema; Agent heartbeat/update client; installer/signing pipeline; alerts.
- **Rủi ro:** signing key compromise; cert expiry hàng loạt; update brick Agent.
- **Điều kiện bắt đầu:** Task 040; code-sign certificate và deployment rings sẵn sàng.
- **Điều kiện hoàn thành:** inventory/health/version trung tâm; unsigned update bị chặn; staged update/rollback và expiry alert được thử.
- **Ước lượng thời gian:** 12 PD.

### Task 042 — E09-T04: Health/readiness và runbooks

- **Mục tiêu:** probes phản ánh dependency thật và operator có hướng xử lý.
- **File sẽ bị ảnh hưởng:** API/worker/Agent health checks; deployment probes; runbooks cho DB/storage/identity/batch/cert.
- **Rủi ro:** probe gây tải; dependency chậm làm restart loop; lộ diagnostic info.
- **Điều kiện bắt đầu:** Task 009, target infrastructure, Task 031/041 cho storage/Agent checks.
- **Điều kiện hoàn thành:** failure làm readiness đổi đúng; diagnostics được bảo vệ; runbook drill/support-ID lookup thành công.
- **Ước lượng thời gian:** 6 PD.

### Task 043 — E09-T03: SLO dashboards và alerts

- **Mục tiêu:** đo availability, Data Hub, review và Agent theo SLO.
- **File sẽ bị ảnh hưởng:** metrics instrumentation; dashboards/alert rules; synthetic checks; on-call/runbook links.
- **Rủi ro:** alert fatigue; threshold thiếu baseline; missing-data bị hiểu là healthy.
- **Điều kiện bắt đầu:** Task 009, 039, 041, 042; SLO/burn-rate/window được ký.
- **Điều kiện hoàn thành:** alert test đến đúng owner; mỗi alert có runbook; error-budget report theo tháng.
- **Ước lượng thời gian:** 8 PD.

### Task 044 — E09-T05: Backup, restore và DR exercise

- **Mục tiêu:** phục hồi SQL/raw/config theo RPO/RTO, kể cả failure-domain lớn.
- **File sẽ bị ảnh hưởng:** SQL/raw/config backup jobs; DR automation/environment; access policies; DR runbook/reconciliation.
- **Rủi ro:** backup không decrypt/restore; cùng failure domain; PII trong backup không đúng retention.
- **Điều kiện bắt đầu:** Task 029, 031, 042; Operations và offsite/DR storage sẵn sàng.
- **Điều kiện hoàn thành:** restore+reconciliation thành công; RPO/RTO đạt; findings có owner/date; lịch drill hai lần/năm.
- **Ước lượng thời gian:** 8 PD.

### Task 045 — E07-T01: New Models activation end-to-end

- **Mục tiêu:** kích hoạt canonical MasterPlan thành workspace có quyền/audit/concurrency.
- **File sẽ bị ảnh hưởng:** New Models domain/application/API; SQL schema; frontend master-plan/workspace pages; events/tasks integration.
- **Rủi ro:** duplicate workspace; owner/scope sai; event tạo task hai lần.
- **Điều kiện bắt đầu:** Task 017, 022, 025, 029; activation acceptance examples.
- **Điều kiện hoàn thành:** create/view/idempotency/concurrency tests; permission/audit/E2E; không hard-code user.
- **Ước lượng thời gian:** 10 PD.

### Task 046 — E07-T02: Task API và persistence

- **Mục tiêu:** thay TaskContext in-memory bằng module persistent có concurrency.
- **File sẽ bị ảnh hưởng:** Tasks domain/application/API/schema; generated client; `TaskContext`/task pages; attachments/tests.
- **Rủi ro:** dependency cycle; attachment security; lost update; scope assignment sai.
- **Điều kiện bắt đầu:** Task 017, 020, 025, 029; critical Task journeys được định nghĩa.
- **Điều kiện hoàn thành:** CRUD/checklist/comment/dependency persistent; permissions/audit; cycle/concurrency/API/E2E tests.
- **Ước lượng thời gian:** 18 PD.

### Task 047 — E07-T04: Chat persistence và authorized SignalR

- **Mục tiêu:** chat có membership, history và sender đáng tin cậy.
- **File sẽ bị ảnh hưởng:** Chat domain/application/API/schema; `ChatHub`; frontend `FloatingChat`; indexes/reconnect tests.
- **Rủi ro:** data retention/PII; unauthorized group; message order/reconnect; volume.
- **Điều kiện bắt đầu:** Task 018, 025, 029; moderation/retention policy.
- **Điều kiện hoàn thành:** chỉ member đọc/gửi; sender từ claims; restart/reconnect/catch-up tests; audit policy ký.
- **Ước lượng thời gian:** 15 PD.

### Task 048 — E07-T03: Task approval và notification workflow

- **Mục tiêu:** approval, escalation, reminders và liên kết Tasks với Data Hub/New Models.
- **File sẽ bị ảnh hưởng:** Task workflow/state machine; notification/outbox adapters/templates; UI; policies/audit/tests.
- **Rủi ro:** duplicate notification; spam; provider email chưa sẵn sàng; approval deadlock.
- **Điều kiện bắt đầu:** Task 038, 046; notification ADR và escalation matrix.
- **Điều kiện hoàn thành:** approval theo scope; notification idempotent; SLA/escalation tests; audit đầy đủ.
- **Ước lượng thời gian:** 12 PD.

### Task 049 — E08-T05: Migrate critical flows sang design system

- **Mục tiêu:** Login, Data Hub, Master Plan và Tasks đạt UI/a11y/i18n chuẩn.
- **File sẽ bị ảnh hưởng:** critical pages/components; design-system patterns; message catalogues; visual/a11y/E2E tests.
- **Rủi ro:** redesign gây scope creep; visual regression; API thay đổi đồng thời.
- **Điều kiện bắt đầu:** Task 012, 013, 023; API của Task 037/045/046 ổn định; journeys/browser matrix được chốt.
- **Điều kiện hoàn thành:** WCAG AA; keyboard/screen-reader UAT; ≥80% critical UI dùng design system; không hard-code locale mới.
- **Ước lượng thời gian:** 20 PD.

### Task 050 — E10-T03: UAT theo vai trò và theo wave

- **Mục tiêu:** xác nhận SQL/Data Hub/Modules hỗ trợ quy trình thật cho từng persona.
- **File sẽ bị ảnh hưởng:** UAT test plans/data/evidence; defect backlog; environment seed/masking scripts; sign-off records.
- **Rủi ro:** business users không rảnh; dữ liệu UAT không đại diện; acceptance thay đổi muộn.
- **Điều kiện bắt đầu:** mỗi feature wave deploy UAT; scenarios/pass-rate/personas được chốt.
- **Điều kiện hoàn thành:** critical scenarios đạt pass-rate đã ký; Severity 1/2 bằng 0; known issues có acceptance/sign-off.
- **Ước lượng thời gian:** 10 PD cho mỗi wave; tối thiểu ba wave cần kế hoạch capacity riêng.

### Task 051 — E10-T04: Training, pilot và hypercare

- **Mục tiêu:** rollout cohort nhỏ, đào tạo và ổn định vận hành trước mở rộng.
- **File sẽ bị ảnh hưởng:** training/runbook/support docs; cohort/feature-flag config; support dashboards; incident/feedback records.
- **Rủi ro:** cohort không đại diện; support quá tải; SLO đạt do tải thấp giả tạo.
- **Điều kiện bắt đầu:** Task 024, 042, 043, 050; pilot cohort/training target/on-call roster được ký.
- **Điều kiện hoàn thành:** training đạt target; pilot SLO hai tuần; feedback có disposition; escalation/on-call hoạt động.
- **Ước lượng thời gian:** 8 PD chuẩn bị cộng 2–4 tuần hypercare theo lịch.

### Task 052 — E07-T05: Retire static users/mock chat/tasks

- **Mục tiêu:** loại nguồn dữ liệu mock sau khi server paths ổn định.
- **File sẽ bị ảnh hưởng:** static JSON/mock data; TaskContext/chat adapters; feature flags; frontend bundle/tests; PII inventory.
- **Rủi ro:** consumer ẩn còn dùng mock; rollback không còn; xóa fixture test cần thiết.
- **Điều kiện bắt đầu:** Task 046, 047; hai release ổn định; telemetry chứng minh không còn production consumer.
- **Điều kiện hoàn thành:** mock usage bằng 0; rollback flag đã thử trước cleanup; static PII removed; dead-code scan sạch.
- **Ước lượng thời gian:** 7 PD cộng stability window hai release.

### Task 053 — E10-T05: Retire legacy và đóng chương trình

- **Mục tiêu:** xóa route/schema/flags legacy theo module và bàn giao vận hành.
- **File sẽ bị ảnh hưởng:** legacy API/code/schema migrations; flags; compatibility tests; ADR/architecture/operations docs; archives.
- **Rủi ro:** consumer chưa quan sát được; cleanup schema phá rollback; archive không phục hồi được.
- **Điều kiện bắt đầu:** Task 044, 051, 052; hai release ổn định; usage bằng 0; backup/restore xác nhận.
- **Điều kiện hoàn thành:** rollback window hết; contract/schema cleanup được review/rehearsal; docs/runbooks/ownership cập nhật; closure sign-off.
- **Ước lượng thời gian:** 10 PD cho mỗi module; thực hiện theo nhiều đợt, không coi 10 PD là tổng toàn chương trình.

## 7. Quy tắc sử dụng execution order

- Số Task 001–053 là thứ tự dependency ưu tiên, không cấm các task không phụ thuộc chạy song song.
- Không bắt đầu task nếu “Điều kiện bắt đầu” hoặc Definition of Ready trong backlog chưa đạt.
- Estimate là ideal effort, không phải elapsed time; external approval, UAT, stability window và hypercare phải nằm trong lịch riêng.
- Mỗi task khi đưa vào sprint phải thay các từ “critical”, “target”, “ổn định”, “đạt” bằng danh sách/ngưỡng cụ thể.
- Nếu ADR thay đổi topology OIDC, raw storage, Agent auth hoặc feature-flag platform, cập nhật dependency graph trước khi tiếp tục.

