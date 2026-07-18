# IQC Nexus AI Coding Agent Rulebook

## 1. Mục đích và phạm vi áp dụng

Tài liệu này quy định cách mọi AI coding agent phải làm việc với repository IQC Nexus. Mục tiêu là giới hạn phạm vi thay đổi, giữ đúng kiến trúc mục tiêu, tránh code/mô hình dữ liệu trùng lặp, bảo vệ dữ liệu và không làm tăng nợ kỹ thuật trong quá trình migration.

Rulebook áp dụng cho mọi hoạt động đọc, phân tích, tạo kế hoạch, sửa mã nguồn, schema, migration, cấu hình, dependency, test, script, tài liệu và deployment artifact trong repository.

### Tài liệu chuẩn bắt buộc tham chiếu

Trước khi thực hiện task, agent phải đọc phiên bản hiện tại của các tài liệu liên quan:

1. [EXECUTION_ORDER.md](./EXECUTION_ORDER.md) — nguồn chuẩn về thứ tự, điều kiện bắt đầu/hoàn thành, file dự kiến ảnh hưởng và estimate.
2. [PRODUCT_BACKLOG.md](./PRODUCT_BACKLOG.md) — nguồn chuẩn về Epic, Feature, Task, giá trị, dependency và Definition of Done.
3. [MIGRATION_PLAN.md](./MIGRATION_PLAN.md) — chiến lược chuyển đổi, phase gates, timeline, rollback và risk register.
4. [ARCHITECTURE_TARGET.md](./ARCHITECTURE_TARGET.md) — kiến trúc đích và các quality attributes.
5. [DATA_HUB_ARCHITECTURE.md](./DATA_HUB_ARCHITECTURE.md) — contract bắt buộc cho mọi luồng ingest.
6. [DESIGN_SYSTEM_PLAN.md](./DESIGN_SYSTEM_PLAN.md) — chuẩn frontend, accessibility, theme và i18n.

Khi tài liệu mâu thuẫn, agent không tự chọn cách diễn giải có tác động lớn. Thứ tự ưu tiên là: yêu cầu rõ ràng mới nhất của người dùng → `EXECUTION_ORDER.md` → `PRODUCT_BACKLOG.md` → kiến trúc chuyên biệt → migration plan. Mâu thuẫn vẫn ảnh hưởng đến nghiệp vụ, bảo mật hoặc dữ liệu phải được báo cáo và chờ quyết định.

## 2. Nguyên tắc chung

1. **Mỗi lần chỉ xử lý một task** trong `EXECUTION_ORDER.md`, trừ khi người dùng chỉ yêu cầu phân tích/tài liệu hoặc phê duyệt rõ một nhóm task.
2. Luôn ghi rõ Task ID theo cả hai dạng, ví dụ `Task 018 / E02-T06`.
3. Không tự động thực hiện task tiếp theo sau khi task hiện tại hoàn thành.
4. Không tự ý mở rộng phạm vi để “tiện sửa luôn”. Phát hiện ngoài phạm vi được ghi thành đề xuất hoặc issue riêng.
5. Không refactor code không liên quan, kể cả khi code đó chưa đẹp.
6. Không thay đổi nghiệp vụ, role, status, permission, workflow hoặc data contract khi chưa có yêu cầu và acceptance criteria rõ ràng.
7. Ưu tiên thay đổi tối thiểu nhưng phải phù hợp kiến trúc đích; “ít dòng” không phải lý do để tiếp tục vi phạm ranh giới kiến trúc.
8. Giữ nguyên thay đổi chưa commit của người dùng. Không reset, overwrite hoặc format lại chúng.
9. Không suy đoán task đã hoàn thành chỉ vì code compile. Definition of Done và điều kiện hoàn thành mới là chuẩn.
10. Không coi tài liệu roadmap là quyền tự động sửa mọi file được liệt kê. Mỗi lần triển khai vẫn cần yêu cầu của người dùng cho task cụ thể.

## 3. Quy trình bắt buộc trước khi sửa code

### 3.1 Xác định task

Agent phải nêu rõ:

- Task number và backlog ID.
- Mục tiêu và giá trị của task.
- Điều kiện bắt đầu đã đạt/chưa đạt.
- Dependency nội bộ và dependency bên ngoài.
- Definition of Done và các ngưỡng đo lường.
- Những phần cố ý nằm ngoài phạm vi.

Nếu task dùng các từ chưa định lượng như “critical”, “target”, “ổn định”, “đạt” hoặc “tương đương”, agent phải tìm tiêu chí trong tài liệu/test hiện có. Nếu không tìm được và cách hiểu có thể làm thay đổi kết quả, phải hỏi trước khi sửa.

### 3.2 Đọc và kiểm tra repository

Trước khi chỉnh sửa, agent phải:

1. Đọc các tài liệu kiến trúc liên quan.
2. Đọc instruction files áp dụng cho đường dẫn như `AGENTS.md`.
3. Kiểm tra `git status` và phân biệt thay đổi của người dùng với phạm vi task.
4. Tìm implementation hiện có bằng search trước khi tạo class, component, service, DTO, hook, helper hoặc model mới.
5. Lần theo call path và data flow đủ để xác định nguyên nhân/phạm vi thật.
6. Kiểm tra migration/schema/API contract/test liên quan.

### 3.3 Kế hoạch thay đổi

Trước khi sửa code, agent phải cung cấp hoặc ghi nhận:

- Danh sách file dự kiến bị ảnh hưởng.
- Lý do từng file cần thay đổi.
- Rủi ro về nghiệp vụ, dữ liệu, bảo mật, compatibility và deployment.
- Cách kiểm thử.
- Kế hoạch rollback.
- Số file dự kiến thay đổi.

Danh sách file trong `EXECUTION_ORDER.md` là định hướng, không phải allowlist tuyệt đối. Nếu implementation cần file ngoài danh sách, agent phải giải thích mối quan hệ trực tiếp với task.

### 3.4 Khi nào phải chờ xác nhận

Agent phải dừng và xin xác nhận trước khi sửa nếu xảy ra một trong các điều kiện:

- Phạm vi vượt Task ID hiện tại.
- Dự kiến sửa quá 10 file.
- Cần thay đổi API contract, role/permission, status/workflow hoặc business key chưa được task phê duyệt.
- Cần migration phá hủy, xóa/đổi tên cột/bảng hoặc backfill khó rollback.
- Cần dependency mới, major upgrade hoặc dịch vụ/hạ tầng mới.
- Gặp thay đổi chưa commit chồng lên vùng phải sửa mà không thể giữ an toàn.
- Dependency/Definition of Ready chưa đạt và tiếp tục sẽ tạo workaround hoặc rework đáng kể.

## 4. Quy tắc kiến trúc

1. Duy trì hướng **modular monolith** trong `ARCHITECTURE_TARGET.md`.
2. Không tạo microservice, message broker hoặc distributed boundary nếu chưa có ADR và phê duyệt rõ ràng.
3. Module giao tiếp qua application interfaces/contracts hoặc outbox đã được phê duyệt; không tạo HTTP nội bộ trong cùng deployment.
4. Controller/API host chỉ làm transport concerns: binding, authentication/authorization, gọi use case và map response.
5. Không truy cập database trực tiếp từ controller.
6. Không đặt nghiệp vụ trong React component, page, hook trình bày hoặc API client.
7. Domain/Application không phụ thuộc UI, controller hoặc EF-specific implementation.
8. Không tạo mô hình dữ liệu song song cho cùng nghiệp vụ. Trước khi thêm entity/DTO/model phải kiểm tra canonical model và legacy adapter hiện có.
9. Không tạo “temporary architecture” không có owner, flag, expiry và removal task.
10. Mọi luồng import phải đi qua Data Hub. Module nghiệp vụ không được tự xây parser, staging hoặc commit pipeline riêng.

## 5. Quy tắc frontend

### 5.1 API và state

- Không hard-code API URL, hostname hoặc port trong component/page.
- Chỉ sử dụng API client thống nhất được sinh/bao bởi contract chung.
- Không tạo thêm wrapper `fetch`/`axios` riêng nếu client hiện tại có thể mở rộng đúng cách.
- Không sao chép server state dài hạn vào Context như nguồn sự thật thứ hai.
- Không dùng mock nếu API thật đã tồn tại và task không yêu cầu test fixture/demo rõ ràng.
- Mock/test fixture phải synthetic, được cô lập khỏi production bundle và không chứa PII.

### 5.2 Component và UI scope

- Search component/pattern hiện có trước khi tạo component mới.
- Không tạo component trùng chức năng chỉ khác tên, màu hoặc vị trí.
- Mở rộng variant của component chuẩn khi phù hợp; không fork component design-system vào feature.
- Không sửa UI ngoài phạm vi task, không “đồng bộ phong cách” toàn trang khi task chỉ sửa hành vi nhỏ.
- Tôn trọng semantic tokens, layout patterns và component contracts trong `DESIGN_SYSTEM_PLAN.md`.
- Không hard-code chuỗi giao diện mới nếu i18n framework đã tồn tại.
- Mọi UI mới phải hỗ trợ keyboard, focus, error/loading/empty state và reduced motion khi liên quan.

### 5.3 TypeScript và chất lượng

- Giữ TypeScript strict và ESLint xanh.
- Không dùng `any`, `as unknown as`, `@ts-ignore`, `@ts-expect-error` hoặc disable rule để né thiết kế type. Ngoại lệ phải có lý do, phạm vi nhỏ, test và removal plan.
- Không import internal/private path của dependency nếu public API tồn tại.
- Không thêm state/effect chỉ để che lỗi render hoặc hydration; phải xử lý đúng nguồn dữ liệu/lifecycle.
- Generated API types là nguồn chuẩn; không viết lại type response bằng tay nếu đã được sinh.

## 6. Quy tắc backend

### 6.1 API và application boundary

- Không trả trực tiếp EF entity nếu đã có hoặc cần DTO contract.
- Controller không chứa nghiệp vụ, query orchestration phức tạp hoặc transaction logic.
- Không trả raw exception message, stack trace, SQL, filesystem path hoặc internal identifier nhạy cảm cho client.
- Lỗi API dùng Problem Details/error code ổn định và có `traceId`.
- Request/response mới phải có validation và OpenAPI/contract test phù hợp.
- Collection endpoint phải có giới hạn/pagination khi dữ liệu có thể tăng.

### 6.2 Authorization và audit

- Mọi endpoint nghiệp vụ phải có authorization policy phù hợp; deny-by-default.
- Không tin user ID, sender ID, role hoặc scope do client tự truyền khi có thể lấy từ claims/context.
- Mọi thay đổi dữ liệu quan trọng phải ghi audit với actor, action, resource, timestamp, reason/changes và traceId theo contract.
- Không dùng diagnostic log thay cho business audit.
- Không vô hiệu hóa `[Authorize]`, policy hoặc validation để test cho nhanh.

### 6.3 EF Core và hiệu năng

- Tránh N+1 query, `ToList` toàn bảng và `SaveChangesAsync` trong vòng lặp.
- Dùng projection, bounded query, batching/bulk strategy và cancellation token phù hợp.
- Transaction boundary phải bao phủ invariant nghiệp vụ; side effects ngoài DB cần outbox/compensation được phê duyệt.
- Thay đổi đồng thời quan trọng phải dùng unique constraint, rowversion/optimistic concurrency hoặc idempotency phù hợp.
- Không che `DbUpdateConcurrencyException` hoặc constraint violation bằng catch chung.

## 7. Quy tắc Data Hub

### 7.1 Pipeline bất biến

Mọi ingestion phải giữ luồng:

```text
source → archive/raw receipt → staging → validation → review → commit → audit/lineage
```

- Không bỏ qua raw archive, staging hoặc validation để ghi thẳng core table.
- Không commit dữ liệu chưa qua review khi còn Error, Blocked, ReviewRequired hoặc ambiguity chưa giải quyết.
- Resolution phải tạo quyết định có actor/reason và chạy revalidation; không chỉ đổi status thành ready.
- Commit phải idempotent, kiểm soát concurrency và ghi lineage/audit cùng transaction hoặc cơ chế tin cậy tương đương.

### 7.2 Header detection, mapping và AI

- Header detection ưu tiên deterministic rules, source profile và versioned dictionary.
- AI/ML chỉ được dùng để gợi ý ngoại lệ hoặc xếp hạng candidate khi có phê duyệt; không tự động thay rule/dictionary hoặc commit mapping.
- Gợi ý của AI phải kèm confidence/evidence và được con người xác nhận khi dưới ngưỡng hoặc ảnh hưởng key/status/identity.
- Mọi mapping được người dùng xác nhận phải có khả năng tái sử dụng, version, scope, effective time, audit và rollback.
- Không “học” âm thầm từ override của người dùng.

### 7.3 Tính toàn vẹn và dữ liệu đa ngôn ngữ

- Luôn xử lý duplicate, idempotency, concurrency, replay và partial failure.
- Canonicalization phải nêu rõ Unicode normalization, locale, timezone, date/number parsing và encoding.
- Không dựa vào locale/timezone mặc định của server.
- Dữ liệu Việt/Hàn và ký tự Unicode phải có round-trip/regression tests.
- Raw input là immutable; reprocess tạo pipeline run mới, không overwrite evidence cũ.

### 7.4 File tạm và dữ liệu giải mã

- File giải mã, extract, preview hoặc dữ liệu tạm chỉ được tạo trong thư mục được quản lý với tên server-generated.
- Phải canonicalize path, kiểm tra target nằm trong allowlisted root và không tin filename từ client.
- Áp dụng extension, MIME/magic-byte, size và malware policy phù hợp.
- Không thực thi macro/formula từ Excel.
- Temporary data phải có owner, retention/TTL và cleanup trong `finally` hoặc recovery job an toàn.
- Không xóa raw archive/audit evidence theo cleanup tạm.
- Cleanup phải chống symlink/path traversal và ghi metric/log không chứa dữ liệu nhạy cảm.

## 8. Bảo mật

1. Không commit secret, password, token, private key, connection string thật hoặc dữ liệu cá nhân.
2. Không in secret/PII ra console, build log, test snapshot, telemetry hoặc error response.
3. Không mở rộng quyền filesystem, database, network, CORS hoặc service account nếu task không yêu cầu.
4. Không xin/quy định Internet access chỉ để né thiếu dependency hoặc tài liệu local.
5. Không vô hiệu hóa authentication/authorization, certificate validation, TLS hoặc malware checks để chức năng chạy.
6. Mọi đường dẫn file đầu vào phải canonicalize và kiểm tra allowlisted root/type/size.
7. Query/filter/sort từ client phải parameterized và allowlisted.
8. Dependency mới cần kiểm tra license, maintenance, vulnerability, bundle/runtime impact và lý do không dùng dependency hiện có.
9. Security finding Critical/High không được suppress nếu chưa có acceptance từ security owner và remediation task/date.
10. Dữ liệu test phải synthetic hoặc được mask theo policy.

## 9. Database và migration

- Không sửa migration cũ đã được áp dụng ở bất kỳ môi trường dùng chung nào.
- Mọi thay đổi schema tạo migration mới có tên/mục đích rõ.
- Trước migration phải đánh giá data type, collation/Unicode, timezone/precision, FK, unique index, index hiệu năng, default/nullability và concurrency.
- Migration phải có forward verification và rollback/compensation plan.
- Không xóa/đổi tên bảng hoặc cột khi chưa có kế hoạch migrate/backfill/verify dữ liệu và stability window.
- Ưu tiên expand → migrate/backfill → switch reads/writes → contract ở release sau.
- Không chạy migration production ngầm trong application startup.
- Không dùng `EnsureCreated`, sửa tay database hoặc script one-off không version-control để né migration.
- Data migration phải idempotent và có reconciliation theo count, business key/checksum và exception disposition.
- Destructive cleanup chỉ thực hiện sau backup/restore verification, telemetry consumer bằng 0 và phê duyệt rõ.

## 10. Build, test và kiểm tra chất lượng

### 10.1 Nguyên tắc

- Sau mỗi task phải chạy build, type-check, lint và test liên quan trong phạm vi hợp lý.
- Không đánh dấu hoàn thành nếu build/type-check/lint bắt buộc chưa xanh.
- Bug fix quan trọng phải có regression test khi khả thi. Nếu không thể, phải giải thích lý do và nêu kiểm tra thay thế.
- Test không được phụ thuộc secret, PII hoặc trạng thái máy cá nhân.
- Không cập nhật snapshot/golden file hàng loạt nếu chưa xác nhận thay đổi là đúng.

### 10.2 Ma trận kiểm tra tối thiểu

| Vùng thay đổi | Kiểm tra tối thiểu |
|---|---|
| Frontend | TypeScript no-emit, ESLint, unit/component tests và route/interaction smoke liên quan |
| Backend | Restore/build, unit tests, integration/API tests liên quan |
| API contract | OpenAPI generation/diff, request/response và consumer tests |
| Database | Migration up trên DB rỗng + snapshot; constraint/reconciliation; rollback hoặc compensation rehearsal |
| Data Hub | Parser/detection/rule tests, idempotency, duplicate, concurrency, replay và failure-path tests |
| Authorization | 401/403, permission/scope matrix và privilege-escalation negative tests |
| UI/design system | Interaction, axe, keyboard, theme/locale và visual regression trong phạm vi |
| Windows Agent | Restart, network loss, duplicate event, disk limit, certificate và signed update tests phù hợp |

### 10.3 Báo cáo kiểm tra

Agent phải báo cáo rõ:

- Lệnh đã chạy, working directory và kết quả pass/fail.
- Test nào không chạy được và lý do cụ thể.
- Phần nào chỉ kiểm tra tĩnh/thủ công.
- Warning hoặc known failure có trước task.
- Không được nói “all tests pass” nếu chỉ chạy một tập con.

## 11. Giới hạn thay đổi

1. Mặc định không sửa quá **10 file** trong một task, tính cả generated contract/migration files nhưng không tính build artifacts không được commit.
2. Nếu cần vượt giới hạn, phải giải thích trước khi sửa: vì sao không thể chia task, danh sách file, risk, test và rollback.
3. Không đổi tên hàng loạt, chuyển folder diện rộng, format toàn repository hoặc chạy autofix trên toàn repo ngoài phạm vi.
4. Không nâng dependency, runtime, framework hoặc lockfile nếu task không yêu cầu.
5. Không gom cleanup/refactor “tiện tay” vào bug fix hoặc feature task.
6. Không sửa tài liệu khác chỉ để làm chúng phù hợp với implementation ngoài phạm vi; báo drift và đề xuất task riêng.
7. Generated file chỉ cập nhật bằng source/tool chính thức và phải kiểm tra diff; không chỉnh tay nếu sẽ bị generator ghi đè.
8. Không tạo file placeholder, class rỗng, endpoint `NotImplementedException` hoặc TODO giả để đạt số lượng deliverable.

## 12. Quản lý phạm vi và thay đổi phát sinh

Khi phát hiện vấn đề ngoài task:

1. Không sửa ngay.
2. Ghi bằng chứng: file/line, tác động và mức độ rủi ro.
3. Xác định nó có chặn DoD hiện tại hay không.
4. Nếu không chặn, đề xuất Task ID/issue tiếp theo.
5. Nếu chặn, nêu phương án tối thiểu, phạm vi tăng thêm và xin xác nhận.

Agent không được dùng lý do “để code sạch hơn”, “best practice” hoặc “sau này sẽ cần” làm quyền mở rộng task.

## 13. Rollback

Mỗi task có thay đổi state/schema/contract phải có rollback thực tế:

- Code/UI: revert artifact/commit hoặc feature flag có expiry.
- API: compatibility adapter/version cũ trong deprecation window.
- Database: expand-compatible rollback hoặc compensation/restore đã mô tả; không hứa down migration phá hủy nếu không an toàn.
- Data Hub: replay từ immutable raw bằng pinned pipeline version.
- Windows Agent: signed previous package và spool không mất dữ liệu.
- Config/security: rotation/revoke và last-known-good config.

Rollback plan phải nêu trigger, người quyết định, dữ liệu có thể mất và cách verify sau rollback.

## 14. Báo cáo sau khi hoàn thành

Mọi báo cáo hoàn thành phải chứa:

1. **Task ID:** Task number và backlog ID.
2. **Tóm tắt thay đổi:** outcome, không chỉ liệt kê thao tác.
3. **Danh sách file đã sửa:** file mới/sửa/xóa, nêu lý do ngắn.
4. **Build/test đã chạy:** lệnh, thư mục, kết quả và phần chưa kiểm tra.
5. **Definition of Done:** từng tiêu chí đạt/chưa đạt và bằng chứng.
6. **Rủi ro còn lại:** severity, owner hoặc follow-up đề xuất.
7. **Hướng rollback:** cách quay lại và lưu ý dữ liệu.
8. **Đề xuất task tiếp theo:** chỉ đề xuất theo `EXECUTION_ORDER.md`, không tự động thực hiện.

Không tuyên bố task hoàn thành nếu còn DoD bắt buộc chưa đạt. Trạng thái đúng khi đó là “chưa hoàn thành” hoặc “blocked”, kèm điều kiện để tiếp tục.

## 15. Các hành vi bị cấm

- Tự ý viết lại toàn bộ module hoặc thay kiến trúc bằng microservices.
- Tự động thực hiện task kế tiếp khi chưa được yêu cầu.
- Tạo code placeholder/TODO/mock rồi coi là hoàn thành.
- Tắt TypeScript strict, compiler warning, ESLint rule, test hoặc security check để build xanh.
- Dùng `any`, suppress warning, catch rỗng hoặc catch-all nuốt lỗi mà không có lý do/test rõ ràng.
- Trả success/fallback giả khi dependency thực tế thất bại.
- Tự ý thay đổi role, permission, status, business key, API contract hoặc database model.
- Tạo entity/table/context/component/service thứ hai cho nghiệp vụ đã có thay vì hợp nhất hoặc dùng adapter.
- Bỏ qua Data Hub staging/validation/review để ghi core nhanh hơn.
- Sửa migration đã áp dụng hoặc sửa trực tiếp database để né migration.
- Commit secret/PII, log dữ liệu nhạy cảm hoặc vô hiệu hóa authorization/TLS.
- Chạy destructive command, reset thay đổi người dùng hoặc cleanup ngoài target đã xác minh.
- Format, rename hoặc nâng dependency toàn repository ngoài phạm vi task.
- Khẳng định build/test xanh khi chưa chạy hoặc khi lệnh thất bại.

## 16. Checklist thực thi nhanh

### Trước khi sửa

- [ ] Đã ghi Task number/backlog ID.
- [ ] Đã đọc tài liệu kiến trúc và instructions liên quan.
- [ ] Dependency và điều kiện bắt đầu đã đạt.
- [ ] DoD có ngưỡng đo lường rõ.
- [ ] Đã search implementation để tránh code/model trùng.
- [ ] Đã kiểm tra `git status`.
- [ ] Đã liệt kê file, risk, test và rollback.
- [ ] Phạm vi ≤10 file hoặc đã được xác nhận mở rộng.

### Trong khi sửa

- [ ] Chỉ thay đổi file trực tiếp phục vụ task.
- [ ] Không thay nghiệp vụ/contract/quyền ngoài acceptance criteria.
- [ ] Giữ đúng module/API/Data Hub/design-system boundaries.
- [ ] Thêm hoặc cập nhật test cùng thay đổi.
- [ ] Không tạo secret, PII, mock hoặc suppression mới trái quy định.

### Trước khi bàn giao

- [ ] Review diff và xác nhận không có thay đổi ngoài phạm vi.
- [ ] Build/type-check/lint/tests liên quan đã chạy.
- [ ] DoD được đối chiếu bằng evidence.
- [ ] Migration/security/audit/rollback đã kiểm tra khi áp dụng.
- [ ] Báo cáo đủ file, lệnh, kết quả, risk và rollback.
- [ ] Chỉ đề xuất, không tự thực hiện task tiếp theo.

## 17. Session Closing Protocol

Cuối mỗi task hoặc trước khi bàn giao sang phiên khác, agent phải thực hiện đầy đủ:

1. Cập nhật `PROJECT_STATUS.md` với phase/sprint, số lượng task theo trạng thái, milestone, risk, blocker và ngày cập nhật.
2. Cập nhật `CURRENT_SPRINT.md` với task active, dependency, DoD đạt/chưa đạt, lệnh kiểm tra và phạm vi còn lại. Không tự thêm task kế tiếp vào sprint.
3. Ghi một entry trong `CHANGELOG_INTERNAL.md` theo đúng Task ID, file thực tế, migration, lệnh build/test, kết quả, rủi ro và rollback. Công việc quản trị ngoài roadmap dùng ID `ADMIN-*` và không làm thay đổi thống kê Task 001–053.
4. Cập nhật `NEXT_SESSION.md` đủ để agent mới tiếp tục mà không cần lịch sử trò chuyện: vị trí dự án, task vừa hoàn thành, task active, build/test gần nhất, thay đổi chưa commit, blocker, bước tiếp theo và điều cấm.
5. Không đánh dấu task `Completed` trong bất kỳ tài liệu nào nếu build/test hoặc tiêu chí bắt buộc trong Definition of Done chưa đạt. Khi chưa đủ bằng chứng, dùng `In Progress` hoặc `Blocked` và ghi rõ điều kiện còn thiếu.
6. Không tự động bắt đầu, sửa file hoặc cập nhật trạng thái task tiếp theo. Chỉ đề xuất task kế tiếp theo `EXECUTION_ORDER.md` và chờ yêu cầu rõ ràng.
7. Chạy và báo cáo `git status --short`; phân biệt file modified, untracked và staged. Liệt kê mọi thay đổi chưa commit, kể cả thay đổi có trước phiên, và không nhận là thay đổi của agent nếu không có bằng chứng.
8. Ghi chính xác lệnh build/test đã chạy, working directory và exit result; ghi rõ lệnh chưa chạy hoặc phần chưa kiểm chứng. Không dùng kết quả của phiên cũ làm bằng chứng mới nếu working tree hoặc dependency liên quan đã đổi.
9. Đối chiếu trạng thái giữa bốn tài liệu trước khi kết thúc: Task ID, trạng thái, blocker, build/test và bước tiếp theo phải nhất quán.

Session Closing Protocol là bước quản trị bắt buộc nhưng không mở rộng phạm vi mã nguồn của task. Cập nhật trạng thái không được dùng để sửa roadmap, dependency hoặc quyết định kiến trúc.

## 18. Ngoại lệ

Ngoại lệ chỉ hợp lệ khi người dùng hoặc owner có thẩm quyền phê duyệt rõ phạm vi, lý do, thời hạn và kế hoạch loại bỏ. Agent phải ghi ngoại lệ trong báo cáo task và không được biến ngoại lệ tạm thời thành precedent cho task khác.

Sự cố bảo mật hoặc nguy cơ mất dữ liệu khẩn cấp cho phép agent dừng công việc, thực hiện kiểm tra read-only và đề xuất containment. Nó không mặc nhiên cho phép agent thay đổi production, xóa dữ liệu hoặc mở rộng quyền nếu chưa được ủy quyền.
