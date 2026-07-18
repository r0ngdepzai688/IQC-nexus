# IQC Nexus Decision Log

Tài liệu này lưu các quyết định kiến trúc và quản trị bền vững. Nó không thay thế `EXECUTION_ORDER.md`, không tự cấp quyền triển khai và không được dùng để thay đổi scope task.

## DEC-001 — Kiến trúc modular monolith

- **Ngày quyết định:** 2026-07-18.
- **Bối cảnh:** IQC Nexus có nhiều module nghiệp vụ dùng chung identity, database, audit và Data Hub nhưng cần ranh giới rõ để giảm coupling.
- **Các phương án đã cân nhắc:** tiếp tục monolith không ranh giới; modular monolith; chuyển ngay sang microservices.
- **Quyết định cuối cùng:** phát triển theo modular monolith với module boundaries rõ trong cùng deployment topology ban đầu.
- **Lý do:** giảm rủi ro vận hành và migration, vẫn cho phép cô lập domain/application contracts và tiến hóa từng phần.
- **Tác động:** controller không chứa nghiệp vụ; module không truy cập chéo tùy ý; microservice mới cần ADR và phê duyệt.
- **Điều kiện xem xét lại:** có bằng chứng về nhu cầu scale/deployment độc lập, ownership độc lập và chi phí distributed system đã được chấp nhận.

## DEC-002 — Không viết lại toàn bộ hệ thống

- **Ngày quyết định:** 2026-07-18.
- **Bối cảnh:** hệ thống hiện tại có giá trị nghiệp vụ và dữ liệu đang dùng nhưng tồn tại nợ kỹ thuật và ranh giới chưa ổn định.
- **Các phương án đã cân nhắc:** big-bang rewrite; duy trì nguyên trạng; migration tăng dần theo strangler/expand-and-contract.
- **Quyết định cuối cùng:** chuyển đổi theo từng lát nhỏ, giữ hệ thống hoạt động và không viết lại toàn bộ.
- **Lý do:** giảm rủi ro mất nghiệp vụ, rút ngắn thời gian tạo giá trị và cho phép rollback theo từng task.
- **Tác động:** mọi task phải tương thích với roadmap, có DoD và rollback; legacy chỉ được loại sau stability window.
- **Điều kiện xem xét lại:** chỉ khi có bằng chứng định lượng rằng một module cô lập không thể cứu vãn và phương án thay thế có migration/cutover được phê duyệt; không áp dụng thành rewrite toàn hệ thống.

## DEC-003 — Data Hub là nền tảng ingest dùng chung

- **Ngày quyết định:** 2026-07-18.
- **Bối cảnh:** Excel, API, Windows Agent và connector tương lai cần cùng cơ chế archive, staging, validation, review, commit, audit và recovery.
- **Các phương án đã cân nhắc:** import riêng theo từng module; thư viện parser dùng chung một phần; Data Hub trung tâm.
- **Quyết định cuối cùng:** Data Hub là nền tảng trung tâm tiếp nhận và chuẩn hóa dữ liệu.
- **Lý do:** bảo đảm lineage, idempotency, versioning và quality gate nhất quán.
- **Tác động:** mọi ingestion tuân theo `source → archive/raw → staging → validation → review → commit → audit/lineage`.
- **Điều kiện xem xét lại:** thay đổi yêu cầu compliance/topology hoặc bottleneck đã được đo và có ADR thay thế vẫn giữ toàn vẹn dữ liệu.

## DEC-004 — Module nghiệp vụ không xây pipeline import riêng

- **Ngày quyết định:** 2026-07-18.
- **Bối cảnh:** pipeline riêng tạo mapping/rule/model song song, audit không đồng nhất và duplicate effect.
- **Các phương án đã cân nhắc:** cho phép từng module tự import; adapter module gọi thẳng core; module đăng ký contract/rules với Data Hub.
- **Quyết định cuối cùng:** module nghiệp vụ không tự xây parser, staging, validation hoặc commit pipeline riêng; module cung cấp canonical contract/rules qua boundary đã định.
- **Lý do:** tránh code trùng, giảm nợ kỹ thuật và duy trì một quality gate.
- **Tác động:** import hiện hữu ngoài Data Hub phải được migrate theo roadmap, không được mở rộng thêm.
- **Điều kiện xem xét lại:** connector có yêu cầu pháp lý/cách ly đặc biệt và ADR chứng minh không thể đáp ứng bằng extension point của Data Hub.

## DEC-005 — AI agent thực hiện từng task theo rulebook

- **Ngày quyết định:** 2026-07-18.
- **Bối cảnh:** chuỗi migration dài dễ phát sinh sửa lan phạm vi, refactor ngoài yêu cầu và trạng thái không thể bàn giao.
- **Các phương án đã cân nhắc:** agent tự tối ưu theo phiên; xử lý nhiều task cùng lúc; một task mỗi lần theo governance bắt buộc.
- **Quyết định cuối cùng:** mọi AI coding agent phải tuân thủ `AI_RULEBOOK.md` và chỉ thực hiện task đang được phê duyệt.
- **Lý do:** kiểm soát scope, tạo bằng chứng DoD, bảo vệ thay đổi người dùng và cho phép rollback.
- **Tác động:** không tự bắt đầu task kế tiếp; cuối task phải cập nhật bộ nhớ dự án và báo Git status.
- **Điều kiện xem xét lại:** quy trình delivery chính thức thay đổi và owner phê duyệt cập nhật rulebook; không được agent tự miễn trừ.

## DEC-006 — MVP tập trung New Model Master Plan

- **Ngày quyết định:** 2026-07-18.
- **Bối cảnh:** roadmap toàn hệ thống quá lớn để chứng minh giá trị sớm; cần một lát dọc thực tế, có data quality và authorization.
- **Các phương án đã cân nhắc:** MVP nhiều module; UI demo dùng mock; lát dọc New Model Master Plan qua Data Hub.
- **Quyết định cuối cùng:** MVP trước mắt chỉ tập trung luồng đăng nhập, phân quyền, upload Data Hub, staging, validation, review, commit và hiển thị New Model Master Plan.
- **Lý do:** phạm vi đủ nhỏ nhưng tạo giá trị nội bộ và kiểm chứng các rủi ro kiến trúc cốt lõi.
- **Tác động:** 32 task bắt buộc và 21 task hoãn được xác định trong `MVP_EXECUTION_PLAN.md`; không kéo feature ngoài MVP vào implementation.
- **Điều kiện xem xét lại:** MVP bị chặn bởi bằng chứng kỹ thuật/nghiệp vụ mới hoặc Product Owner thay đổi outcome; mọi thay đổi phải cập nhật kế hoạch và dependency có phê duyệt.

