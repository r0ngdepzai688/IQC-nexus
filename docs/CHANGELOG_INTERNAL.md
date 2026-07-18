# IQC Nexus Internal Changelog

Changelog này ghi bằng chứng theo task. Chỉ ghi **Completed** khi Definition of Done đã đạt. Các công việc quản trị ngoài Task 001–053 phải dùng ID `ADMIN-*` và không thay đổi trạng thái roadmap.

## ADMIN-DOC-001 — Thiết lập bộ nhớ và tiến độ dài hạn

- **Ngày thực hiện:** 2026-07-18.
- **Trạng thái:** Completed (docs-only, ngoài roadmap Task 001–053).
- **Tóm tắt thay đổi:** tạo nguồn trạng thái dự án, sprint hiện tại, decision log, changelog, handoff phiên và bổ sung session-closing protocol cho AI agent.
- **File đã thay đổi:**
  - Tạo `docs/PROJECT_STATUS.md`.
  - Tạo `docs/CURRENT_SPRINT.md`.
  - Tạo `docs/DECISION_LOG.md`.
  - Tạo `docs/CHANGELOG_INTERNAL.md`.
  - Tạo `docs/NEXT_SESSION.md`.
  - Cập nhật `docs/AI_RULEBOOK.md`.
- **Migration được tạo:** không.
- **Build/test đã chạy:** không chạy build ứng dụng vì chỉ thay đổi Markdown; kiểm tra cấu trúc tài liệu, liên kết nội bộ, UTF-8, `git diff --check` và `git status --short` được thực hiện khi đóng phiên.
- **Kết quả:** bộ tài liệu quản trị được tạo; Task 001 sau đó đã hoàn thành tại commit `d2b1de162cba8a844bfeca3412817ee1dcf13f4d`.
- **Rủi ro còn lại:** tài liệu hiện chưa được Git track; trạng thái có thể drift nếu Session Closing Protocol không được thực hiện sau mỗi task.
- **Rollback procedure:** xóa năm tài liệu mới và revert riêng phần “Session Closing Protocol” trong `AI_RULEBOOK.md`; không tác động source, database hay migration.

## Task 001 / E01-T01 — Đưa source Data Hub vào version control

- **Ngày thực hiện:** 2026-07-18.
- **Trạng thái:** Completed.
- **Commit:** `d2b1de162cba8a844bfeca3412817ee1dcf13f4d` — `fix(repo): track Data Hub source safely`; chưa push.
- **Tóm tắt thay đổi:** thay rule `.gitignore` từ `DataHub/` thành `/DataHub/`; đưa 15 file source/tài liệu Data Hub vào tracking mà không thay đổi nội dung của chúng.
- **File trong commit (16):**
  - `.gitignore`.
  - `backend/src/IqcQms.Application/Interfaces/DataHub/DataHubPathConfig.cs`.
  - `backend/src/IqcQms.Application/Interfaces/DataHub/IDataHubIngestionService.cs`.
  - `backend/src/IqcQms.Application/Interfaces/DataHub/IMasterPlanContractParser.cs`.
  - `backend/src/IqcQms.Domain/Entities/DataHub/BusinessReviewQueue.cs`.
  - `backend/src/IqcQms.Domain/Entities/DataHub/DataHubAuditLog.cs`.
  - `backend/src/IqcQms.Domain/Entities/DataHub/DataSource.cs`.
  - `backend/src/IqcQms.Domain/Entities/DataHub/ImportBatch.cs`.
  - `backend/src/IqcQms.Domain/Entities/DataHub/ImportLog.cs`.
  - `backend/src/IqcQms.Domain/Entities/DataHub/MappingDictionary.cs`.
  - `backend/src/IqcQms.Domain/Entities/DataHub/RawFile.cs`.
  - `backend/src/IqcQms.Domain/Entities/DataHub/StagingMasterPlan.cs`.
  - `backend/src/IqcQms.Domain/Entities/DataHub/ValidationError.cs`.
  - `backend/src/IqcQms.Infrastructure/Services/DataHub/DataHubIngestionService.cs`.
  - `backend/src/IqcQms.Infrastructure/Services/DataHub/MasterPlanContractParser.cs`.
  - `docs/datahub/MasterPlan_DataContract.md`.
- **Migration được tạo:** không.
- **Build/test đã chạy:** `dotnet restore backend/IqcQms.sln` thành công; `dotnet build backend/IqcQms.sln --no-restore` thành công với 0 warnings và 0 errors.
- **Kiểm tra repository:** source Data Hub và `docs/datahub/MasterPlan_DataContract.md` không còn bị ignore; probe `.xlsx`, `.db` và file dưới `/DataHub/` vẫn bị ignore; staged set đúng 16 file; binary, sensitive filename và secret assignment scan đều có 0 kết quả; SHA-256 xác nhận 15/15 file Data Hub không đổi nội dung.
- **Kết quả:** người dùng xác nhận Task 001 đạt Definition of Done.
- **Rủi ro còn lại:** các file Data Hub có trailing whitespace tồn tại từ trước; commit mới tồn tại local và chưa push.
- **Rollback procedure:** revert commit `d2b1de162cba8a844bfeca3412817ee1dcf13f4d` bằng một commit đảo riêng khi được phê duyệt; không amend/reset commit đã tạo và không xóa bản local của Data Hub.

## Task 002 / E01-T02 — Slice 002A: Contain tracked root data

- **Ngày thực hiện:** 2026-07-18.
- **Trạng thái:** 002A Completed; Task 002 tổng thể In Progress; 002B/002C Not Started.
- **Tóm tắt thay đổi:** remove tracking bốn root personnel artifacts bằng `git rm --cached`, giữ nguyên file local; thêm ignore rules path-specific và inventory metadata-only.
- **File thay đổi:** `.gitignore`; bốn root artifacts đổi tracking state; `docs/security/data-inventory.md`; bốn tài liệu Session Closing Protocol.
- **Migration được tạo:** không.
- **Build/test:** `dotnet restore backend/IqcQms.sln` thành công; `dotnet build backend/IqcQms.sln --no-restore` thành công với 6 nullable warnings trong Data Hub source không bị sửa bởi 002A và 0 errors; repository/ignore/staged-set scans đạt.
- **Kết quả:** bốn artifacts không còn tracked; file local vẫn tồn tại; Data Hub source/docs không bị ignore; không sửa code.
- **Release gate còn lại:** secure store, Data Owner, Security approver, encryption/access/retention và history reassessment đều pending organizational approval.
- **Rủi ro còn lại:** PII vẫn tồn tại local và trong Git history; history không được rewrite trong 002A.
- **Rollback procedure:** không recommit PII, không xóa local và không reset/rewrite history; dùng forward-fix hoặc secure-store recovery khi được owner phê duyệt.
- **Bước tiếp theo:** chỉ chuẩn bị Definition of Ready cho 002B khi có yêu cầu; không tự động triển khai.
