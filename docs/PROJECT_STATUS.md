# IQC Nexus Project Status

## 1. Mục tiêu hiện tại của sản phẩm

Đưa IQC Nexus tiến dần đến kiến trúc modular monolith có thể vận hành an toàn mà không viết lại toàn bộ hệ thống. Giá trị gần nhất cần chứng minh là MVP New Model Master Plan theo lát dọc:

```text
đăng nhập → phân quyền → upload qua Data Hub → staging → validation
→ review → commit → hiển thị Master Plan
```

MVP là controlled internal UAT release, không phải phát hành production diện rộng. Phạm vi chuẩn nằm trong `MVP_EXECUTION_PLAN.md`; roadmap đầy đủ và thứ tự dependency nằm trong `EXECUTION_ORDER.md`.

## 2. Giai đoạn và sprint hiện tại

- **Giai đoạn:** Phase 0 — Stabilize and Govern / Repository safety baseline.
- **Sprint:** Sprint 0.
- **Task vừa hoàn thành:** Task 001 / E01-T01 — Đưa source Data Hub vào version control, còn được gọi là Task A; commit `d2b1de162cba8a844bfeca3412817ee1dcf13f4d`.
- **Trạng thái thực thi:** hiện không có task active. Task 002 chưa bắt đầu. Commit Task 001 chưa được push.

## 3. Tổng quan task

Nguồn chuẩn là 53 task trong `EXECUTION_ORDER.md`.

| Trạng thái | Số lượng | Task |
|---|---:|---|
| Hoàn thành | 1 | Task 001 / E01-T01 |
| Đang thực hiện, không bị chặn | 0 | — |
| Bị chặn | 0 | — |
| Chưa bắt đầu | 52 | Task 002–053 |
| **Tổng** | **53** | Task 001–053 |

Việc tạo bộ tài liệu quản trị là yêu cầu docs-only được phê duyệt riêng và không làm thay đổi trạng thái của Task 002–053.

## 4. Milestone quan trọng

| Milestone | Phạm vi | Trạng thái |
|---|---|---|
| M0 — Repository an toàn và build tái lập | Task 001–006 | Đang thực hiện; Task 001 hoàn thành, Task 002 chưa bắt đầu |
| M1 — Identity, security và API foundation | Các task MVP liên quan trong Task 007–024 | Chưa bắt đầu |
| M2 — SQL Server readiness và cutover UAT | Task 025–029 | Chưa bắt đầu |
| M3 — Data Hub Master Plan end-to-end | Task 030–038 trong phạm vi MVP | Chưa bắt đầu |
| M4 — New Model Master Plan MVP UAT | Task 050 sau khi dependency MVP hoàn tất | Chưa bắt đầu |
| M5 — Production pilot và hypercare | Task 051 | Hoãn ngoài MVP hiện tại |
| M6 — Legacy retirement | Task 052–053 sau stability window | Hoãn |

## 5. Rủi ro và blocker hiện tại

| ID | Loại | Mô tả | Tác động | Hành động/điều kiện gỡ |
|---|---|---|---|---|
| R-001 | Code quality | Một số file Data Hub vừa được track có trailing whitespace tồn tại từ trước. | `git diff --check` báo lỗi khi nhìn toàn bộ file mới. | Không sửa trong Task 001; xử lý trong task chất lượng được phê duyệt sau này. |
| R-002 | Delivery | Commit Task 001 mới tồn tại local và chưa được push. | Remote/clone khác chưa nhận thay đổi. | Chỉ push khi có yêu cầu rõ ràng. |
| R-003 | Git hygiene | Bộ tài liệu baseline đang được đưa vào Git bằng task quản trị độc lập. | Handoff chưa bền vững cho tới khi commit tài liệu hoàn tất. | Stage đúng allowlist 13 tài liệu, scan và commit riêng; không push nếu chưa được yêu cầu. |
| R-004 | Local path | Data Hub hiện chứa đường dẫn mặc định phụ thuộc máy local. | Build có thể xanh nhưng runtime không portable. | Ghi nhận cho task cấu hình phù hợp sau này; không sửa trong Task 001. |

## 6. Cập nhật gần nhất

- **Ngày cập nhật:** 2026-07-18 (Asia/Bangkok).
- **Nguồn cập nhật:** commit Task 001 `d2b1de162cba8a844bfeca3412817ee1dcf13f4d`; backend restore/build thành công với 0 warning và 0 error.
