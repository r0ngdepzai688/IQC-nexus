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
- **Task vừa hoàn thành:** Task 003 / E01-T04 — Làm xanh TypeScript và ESLint; merged qua PR #2 tại merge commit `d19fee785744dbb275359449f398e3f2efc40fb5`.
- **Trạng thái thực thi:** Task 001 và Task 003 đã hoàn thành. Task 002 tổng thể vẫn in progress vì mới hoàn thành slice 002A; 002B/002C chưa hoàn thành. Hiện không có implementation task active.

## 3. Tổng quan task

Nguồn chuẩn là 53 task trong `EXECUTION_ORDER.md`.

| Trạng thái | Số lượng | Task |
|---|---:|---|
| Hoàn thành | 2 | Task 001 / E01-T01; Task 003 / E01-T04 |
| Đang thực hiện, không bị chặn | 1 | Task 002 / E01-T02 — 002A hoàn thành, 002B/002C chưa hoàn thành |
| Bị chặn | 0 | — |
| Chưa bắt đầu | 50 | Task 004–053 |
| **Tổng** | **53** | Task 001–053 |

Việc tạo bộ tài liệu quản trị là yêu cầu docs-only được phê duyệt riêng và không làm thay đổi trạng thái của Task 002–053.

## 4. Milestone quan trọng

| Milestone | Phạm vi | Trạng thái |
|---|---|---|
| M0 — Repository an toàn và build tái lập | Task 001–006 | Đang thực hiện; Task 001 và Task 003 hoàn thành, Task 002 còn in progress |
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
| R-005 | Data governance | Bốn artifact PII không còn được track tại HEAD nhưng bản local và Git history vẫn tồn tại; secure-store/Data Owner/Security approval chưa hoàn tất. | Release không được phép coi containment là data disposition hoàn chỉnh. | Giữ local, không rewrite history; hoàn tất owner, backup, encryption, retention và history decision trước release. |

## 6. Cập nhật gần nhất

- **Ngày cập nhật:** 2026-07-19 (Asia/Bangkok).
- **Nguồn cập nhật:** PR #2 `fix(task-003): resolve TypeScript and ESLint issues`, merge commit `d19fee785744dbb275359449f398e3f2efc40fb5`; ESLint 0 errors/0 warnings; TypeScript, production build và `npm test --if-present` đều không báo lỗi.
