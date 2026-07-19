# IQC Nexus Current Sprint

## 1. Sprint

- **Tên:** Sprint 0 — Repository Safety Baseline.
- **Mục tiêu:** thiết lập repository safety baseline: source bắt buộc được track, root personnel artifacts được containment an toàn và frontend quality baseline được thiết lập.
- **Nguyên tắc:** mỗi lần chỉ thực hiện một execution slice; không tự động vượt dependency trong `EXECUTION_ORDER.md`.

## 2. Danh sách task thuộc sprint

| Task | Mục tiêu | Trạng thái |
|---|---|---|
| Task 001 / E01-T01 — Đưa source Data Hub vào version control | Clone sạch chứa đủ entities, interfaces, services và tài liệu contract Data Hub để backend build được. | **Completed** — commit `d2b1de162cba8a844bfeca3412817ee1dcf13f4d`; chưa push. |
| Task 002 / E01-T02 — 002A Contain tracked root data | Remove tracking bốn root personnel artifacts, giữ file local và ghi inventory metadata-only. | **002A Completed** — Task 002 tổng thể vẫn in progress; 002B/002C chưa bắt đầu. |
| Task 003 / E01-T04 — Làm xanh TypeScript và ESLint | Thiết lập frontend compile/lint/build baseline trước các refactor tiếp theo. | **Completed** — PR #2 merged tại `d19fee785744dbb275359449f398e3f2efc40fb5`. |

Task 004 **chưa được phép bắt đầu** vì `EXECUTION_ORDER.md` yêu cầu Task 002 hoàn thành trước.

Không tự động bắt đầu 002B, 002C hoặc task khác khi chưa có yêu cầu mới và Definition of Ready tương ứng.

## 3. Task đang active

Hiện **không có implementation task active**.

### Task vừa hoàn thành — Task 003 / E01-T04

- ESLint đạt **0 errors, 0 warnings**.
- `npx tsc --noEmit --incremental false` thành công.
- `npm run build` thành công.
- `npm test --if-present` không báo lỗi.
- PR #2 đã merge vào `main`.
- Merge commit: `d19fee785744dbb275359449f398e3f2efc40fb5`.

### Task 002 / E01-T02 — kết quả slice 002A

- Bốn root personnel artifacts không còn được Git track tại HEAD nhưng vẫn tồn tại local và không đổi nội dung.
- `.gitignore` dùng rule path-specific cho ba JSON root; rule Excel và `/DataHub/` được giữ nguyên.
- `docs/security/data-inventory.md` chỉ chứa metadata và release gates.
- Không sửa source, runtime seeding, frontend fixture hoặc backend seeder.
- Không rewrite Git history, không push và không bắt đầu 002B.
- Data Owner và Security approval vẫn là release gate.
- Backend restore thành công; build thành công với 6 nullable warnings trong Data Hub source không bị sửa bởi 002A và 0 errors.

### Task 001 / E01-T01 — kết quả hoàn thành

- **Kết quả khảo sát:** 15 file source/tài liệu bị rule `.gitignore` `DataHub/` loại bỏ vì rule khớp mọi thư mục cùng tên ở mọi cấp; trên Windows, `docs/datahub` cũng bị ảnh hưởng.
- **Thay đổi đã thực hiện:** thay `DataHub/` bằng `/DataHub/`; giữ nguyên các rule chặn database, Excel, CSV, PDF, ZIP/export và `.env*.local`; đưa đúng 15 file Data Hub vào tracking mà không đổi nội dung.
- **Commit:** `d2b1de162cba8a844bfeca3412817ee1dcf13f4d` — `fix(repo): track Data Hub source safely`; chưa push.
- **Build gần nhất:** restore thành công; build thành công; 0 warnings, 0 errors.

## 4. Bước tiếp theo được phép thực hiện

Bước tiếp theo hợp lệ là **Definition of Ready cho Task 002B**, không phải Task 004.

Definition of Ready tối thiểu:

- Inventory consumer dữ liệu personnel trong frontend, backend, seeders, fixtures và script liên quan.
- Xác định canonical synthetic personnel fixture.
- Chốt schema/field contract mà không thay đổi API contract hoặc database schema.
- Xác nhận toàn bộ dữ liệu đều synthetic, không suy diễn từ PII thật và không chứa identifier thật.
- Chốt acceptance criteria cho frontend và backend consumers.
- Chốt owner cho secure store, retention và Git history disposition.
- Dừng trước implementation nếu schema fixture, owner hoặc security constraints chưa được xác nhận.

## 5. Dependency và điều kiện bắt đầu

- **Dependency roadmap:** Task 004 phụ thuộc Task 002 hoàn thành.
- **Điều kiện bắt đầu:** quyền đọc repository và inventory file local — đã đạt.
- **Điều kiện hoàn thành:** người dùng xác nhận đạt Definition of Done.
- **Dependency bên ngoài:** không cần Internet, dịch vụ mới, database migration hoặc thay đổi nghiệp vụ.
- **Release gate còn lại:** Data Owner, Security approval, secure store, least-privilege access, encryption, retention và Git-history disposition.

## 6. Definition of Done

### Task 001

Task 001 chỉ hoàn thành khi tất cả điều kiện sau có bằng chứng:

- [x] `.gitignore` không còn ignore các thư mục source/tài liệu Data Hub lồng trong `backend/src` và `docs`.
- [x] Đúng 15 file Data Hub đã kiểm kê được Git track, không sửa behavior của chúng.
- [x] `git check-ignore -v` xác nhận source Data Hub không bị ignore.
- [x] Runtime/raw Data Hub, `*.db`, `*.db-shm`, `*.db-wal`, Excel, CSV, PDF, ZIP/export và secret local vẫn bị ignore.
- [x] Staged diff không chứa raw data, database, PII, export hoặc secret.
- [x] Backend restore và build thành công với 0 warnings, 0 errors.
- [x] Source Data Hub có mặt trong commit và backend build được; người dùng đã xác nhận Task 001 đạt DoD.
- [x] Git status và các thay đổi chưa commit được báo cáo đầy đủ.

### Task 003

- [x] `npm run lint` đạt 0 errors và 0 warnings.
- [x] `npx tsc --noEmit --incremental false` thành công.
- [x] `npm run build` thành công.
- [x] `npm test --if-present` không báo lỗi.
- [x] Không sử dụng blanket suppression để che lỗi.
- [x] PR #2 đã merge.

### Task 002 tổng thể

- [x] 002A: bốn root personnel artifacts không còn được track tại HEAD.
- [x] 002A: file local được giữ nguyên và inventory metadata-only được ghi nhận.
- [ ] Canonical synthetic personnel fixture được xác định.
- [ ] Frontend/backend test-dev consumers không còn phụ thuộc PII thật.
- [ ] Secure store, access, encryption và retention owner được chốt.
- [ ] Git-history disposition được phê duyệt.
- [ ] Data Owner và Security sign-off hoàn tất.

Không đánh dấu Task 002 hoàn thành nếu bất kỳ điều kiện chưa hoàn tất nào ở trên chưa có bằng chứng.

## 7. Lệnh build và test bắt buộc

### Backend

Chạy từ repository root:

```powershell
git check-ignore -v -- backend/src/IqcQms.Application/Interfaces/DataHub/DataHubPathConfig.cs
git check-ignore -v -- backend/src/IqcQms.Domain/Entities/DataHub/ImportBatch.cs
git check-ignore -v -- backend/src/IqcQms.Infrastructure/Services/DataHub/DataHubIngestionService.cs
git check-ignore -v -- docs/datahub/MasterPlan_DataContract.md
git check-ignore -v -- sample.xlsx sample.db sample.zip .env.local DataHub/runtime.json

dotnet restore backend/IqcQms.sln
dotnet build backend/IqcQms.sln --no-restore
