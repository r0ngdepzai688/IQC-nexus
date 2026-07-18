# IQC Nexus Next Session Handoff

Tài liệu này là điểm bắt đầu cho AI agent ở phiên tiếp theo. Nội dung phản ánh trạng thái tại cuối phiên 2026-07-18 và phải được cập nhật lại theo Session Closing Protocol.

## 1. Dự án đang ở đâu

- IQC Nexus đang ở **Phase 0 / Sprint 0 — Repository Safety Baseline**.
- Roadmap có 53 task; hiện 1 hoàn thành, 1 đang thực hiện, 0 bị chặn và 51 chưa bắt đầu. Task 002 đang in progress vì chỉ 002A hoàn thành.
- MVP mục tiêu là controlled internal UAT cho New Model Master Plan qua toàn bộ Data Hub flow.
- Không có quyền tự động triển khai task tiếp theo hoặc thay roadmap.

## 2. Task vừa hoàn thành

- Task 001 / E01-T01 đã hoàn thành tại commit `d2b1de162cba8a844bfeca3412817ee1dcf13f4d` với message `fix(repo): track Data Hub source safely`.
- Task 002 / E01-T02 slice 002A đã hoàn thành development containment: bốn root artifacts không còn tracked, file local được giữ, history không rewrite.
- Công việc ngoài roadmap `ADMIN-DOC-001` đã tạo cơ chế bộ nhớ/progress docs và cập nhật rulebook; việc này không thay đổi tiến độ roadmap.

## 3. Task đang active

- Hiện không có task active.
- Task 002 / E01-T02 tổng thể đang in progress; 002B và 002C chưa bắt đầu.
- Bước tiếp theo khi được yêu cầu là Definition of Ready cho 002B, không phải implementation tự động.
- Commit Task 001 chưa được push.

## 4. Kết quả build/test gần nhất

- `dotnet restore backend/IqcQms.sln`: thành công; mọi project up-to-date.
- `dotnet build backend/IqcQms.sln --no-restore`: thành công.
- Kết quả: 0 warnings, 0 errors.
- 002A: backend restore thành công; build thành công với 6 nullable warnings trong Data Hub source không bị sửa bởi 002A và 0 errors. Không chạy frontend build vì 002A không sửa code/fixture.

## 5. File đã thay đổi nhưng chưa commit

- Sau khi commit baseline tài liệu hiện tại thành công, không còn file thay đổi hoặc untracked.
- Không có source ứng dụng nào được thay đổi trong task quản trị tài liệu.

## 6. Blocker và quyết định đang chờ

- Không có development blocker active.
- Release gate còn pending: secure store, Data Owner, Security approver, least-privilege access, encryption, retention và Git-history reassessment.
- Commit Task 001 chưa được push.
- Không có phê duyệt cho Task 002 hoặc bất kỳ task tiếp theo nào.

## 7. Bước tiếp theo chính xác

1. Đọc các tài liệu bắt buộc ở mục 9 bên dưới.
2. Chạy `git status --short` và xác nhận trạng thái chưa đổi.
3. Xác nhận 002A hoàn thành, Task 002 tổng thể chưa hoàn thành và không có task active.
4. Dừng và chờ yêu cầu chuẩn bị Definition of Ready cho 002B. Không triển khai 002B hoặc 002C tự động.

## 8. Hành động tuyệt đối không được tự ý thực hiện

- Không dùng `git add .`, wildcard rộng, reset, clean hoặc xóa dữ liệu local.
- Không stage/commit/push nếu người dùng chưa yêu cầu đúng hành động đó.
- Không amend commit Task 001.
- Không track database, Excel, CSV, PDF, ZIP/export, raw/archive/staging, `.env.local`, secret hoặc PII.
- Không bắt đầu 002B hoặc 002C nếu chưa có yêu cầu mới.
- Không thay đổi roadmap, Task ID, dependency hoặc MVP scope.

## 9. Tài liệu phải đọc trước khi tiếp tục

Theo thứ tự:

1. `docs/AI_RULEBOOK.md`
2. `docs/NEXT_SESSION.md`
3. `docs/PROJECT_STATUS.md`
4. `docs/CURRENT_SPRINT.md`
5. `docs/EXECUTION_ORDER.md` — đặc biệt Task 001
6. `docs/MVP_EXECUTION_PLAN.md` — đặc biệt MVP Task 01
7. `docs/PRODUCT_BACKLOG.md` — E01-T01
8. `docs/DECISION_LOG.md`
9. `docs/ARCHITECTURE_TARGET.md`
10. `docs/DATA_HUB_ARCHITECTURE.md`
11. `.agents/AGENTS.md` và instruction file áp dụng cho vùng sẽ sửa
