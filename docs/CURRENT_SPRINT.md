# IQC Nexus Current Sprint

## 1. Sprint

- **Tên:** Sprint 0 — Repository Safety Baseline.
- **Mục tiêu:** khôi phục repository về trạng thái an toàn và tái lập bằng cách đưa đúng source Data Hub vào version control mà không track dữ liệu runtime hoặc dữ liệu nhạy cảm.
- **Nguyên tắc:** mỗi lần chỉ thực hiện một task; Sprint 0 hiện chỉ có Task 001 / E01-T01.

## 2. Danh sách task thuộc sprint

| Task | Mục tiêu | Trạng thái |
|---|---|---|
| Task 001 / E01-T01 — Đưa source Data Hub vào version control | Clone sạch chứa đủ entities, interfaces, services và tài liệu contract Data Hub để backend build được. | **Completed** — commit `d2b1de162cba8a844bfeca3412817ee1dcf13f4d`; chưa push. |

Không đưa Task 002 hoặc task khác vào sprint cho tới khi Task 001 đạt DoD và người dùng yêu cầu bắt đầu task tiếp theo.

## 3. Task đang active

Hiện không có task active. Task 002 chưa bắt đầu.

### Task 001 / E01-T01 — kết quả hoàn thành

- **Kết quả khảo sát:** 15 file source/tài liệu bị rule `.gitignore` `DataHub/` loại bỏ vì rule khớp mọi thư mục cùng tên ở mọi cấp; trên Windows, `docs/datahub` cũng bị ảnh hưởng.
- **Thay đổi đã thực hiện:** thay `DataHub/` bằng `/DataHub/`; giữ nguyên các rule chặn database, Excel, CSV, PDF, ZIP/export và `.env*.local`; đưa đúng 15 file Data Hub vào tracking mà không đổi nội dung.
- **Commit:** `d2b1de162cba8a844bfeca3412817ee1dcf13f4d` — `fix(repo): track Data Hub source safely`; chưa push.
- **Build gần nhất:** restore thành công; build thành công; 0 warnings, 0 errors.

## 4. Dependency và điều kiện bắt đầu

- **Dependency roadmap:** không có.
- **Điều kiện bắt đầu:** quyền đọc repository và inventory file local — đã đạt.
- **Điều kiện hoàn thành:** đã được người dùng xác nhận đạt Definition of Done.
- **Dependency bên ngoài:** không cần Internet, dịch vụ mới, database migration hoặc thay đổi nghiệp vụ.

## 5. Definition of Done

Task 001 chỉ hoàn thành khi tất cả điều kiện sau có bằng chứng:

- [x] `.gitignore` không còn ignore các thư mục source/tài liệu Data Hub lồng trong `backend/src` và `docs`.
- [x] Đúng 15 file Data Hub đã kiểm kê được Git track, không sửa behavior của chúng.
- [x] `git check-ignore -v` xác nhận source Data Hub không bị ignore.
- [x] Runtime/raw Data Hub, `*.db`, `*.db-shm`, `*.db-wal`, Excel, CSV, PDF, ZIP/export và secret local vẫn bị ignore.
- [x] Staged diff không chứa raw data, database, PII, export hoặc secret.
- [x] Backend restore và build thành công với 0 warnings, 0 errors.
- [x] Source Data Hub có mặt trong commit và backend build được; người dùng đã xác nhận Task 001 đạt DoD.
- [x] Git status và các thay đổi chưa commit được báo cáo đầy đủ.

Không đánh dấu hoàn thành nếu bất kỳ ô nào chưa đạt.

## 6. Lệnh build và test bắt buộc

Chạy từ repository root:

```powershell
git check-ignore -v -- backend/src/IqcQms.Application/Interfaces/DataHub/DataHubPathConfig.cs
git check-ignore -v -- backend/src/IqcQms.Domain/Entities/DataHub/ImportBatch.cs
git check-ignore -v -- backend/src/IqcQms.Infrastructure/Services/DataHub/DataHubIngestionService.cs
git check-ignore -v -- docs/datahub/MasterPlan_DataContract.md
git check-ignore -v -- sample.xlsx sample.db sample.zip .env.local DataHub/runtime.json
dotnet restore backend/IqcQms.sln
dotnet build backend/IqcQms.sln --no-restore
git diff --check
git diff --cached --name-status
git status --short
```

Kỳ vọng: bốn đường dẫn source đầu không bị ignore; các đường dẫn dữ liệu giả lập vẫn bị ignore. Không tạo file dữ liệu thật chỉ để kiểm thử ignore. Clean-clone-equivalent verification phải dùng snapshot/index an toàn hoặc clone sau khi thay đổi đã được lưu hợp lệ; không xóa working tree hiện tại.

## 7. Ngoài phạm vi sprint

- Task 002–053.
- Phân loại/xóa dữ liệu root hoặc làm sạch Git history; đây là Task 002.
- Refactor `DataHubIngestionService`, parser, entities hoặc đường dẫn cấu hình.
- Thay đổi pipeline import, nghiệp vụ, API, database schema hoặc migration.
- Tạo CI pipeline/clone-check framework mới nếu không cần thiết để chứng minh DoD tối thiểu.
- Sửa frontend, authentication, authorization, design system hoặc Windows Agent.
- Stage/commit/push các tài liệu hoặc thay đổi khác ngoài allowlist được phê duyệt.
