# IQC Nexus - Backlog & Pending Features

Tài liệu này lưu trữ danh sách các tính năng và logic cần được phát triển trong tương lai (khi có Backend và Database thực tế).

## 1. Authentication & Role-Based Access Control (RBAC)
- [ ] **Login System**: Tích hợp luồng Đăng nhập (SSO hoặc Local).
- [ ] **Lenses Implementation**: 
  - [ ] **Data Lens**: Chặn dữ liệu trả về từ API theo Role (Công nhân chỉ thấy task của mình).
  - [ ] **Action Lens**: Ẩn/Hiện các nút bấm thao tác (Thêm, Xóa, Sửa, Duyệt) dựa trên Role và Trạng thái Task.
  - [ ] **Simplified Mode**: Tự động chuyển đổi giao diện sang chế độ Tối giản (chữ to, thuần Việt, ẩn biểu đồ) nếu User Role là Công nhân.

## 2. Tasks & Workflow Engine
- [ ] **Real Database Integration**: Chuyển đổi dữ liệu Mock thành dữ liệu thật (PostgreSQL/SQL Server).
- [ ] **File Upload API**: Xây dựng API `/api/upload` nhận file bằng chứng từ `TaskDetailModal`, lưu trữ (S3/Local) và trả về URL bảo mật.
- [ ] **State Machine Validation**: Backend cần chặn các request chuyển trạng thái không hợp lệ (Ví dụ: Công nhân không được chuyển task từ `Review` sang `Done`).
- [ ] **Audit Trail**: Mọi thao tác (Kéo thả cột, Comment, Upload file) đều phải gọi API log lại `empId` và `timestamp` để truy xuất trách nhiệm.

## 3. Real-time & Communications
- [ ] **WebSockets**: Cập nhật trạng thái Task theo thời gian thực để mọi người trong team nhìn thấy ngay lập tức mà không cần F5.
- [ ] **Live Chat**: Thay thế hiệu ứng mô phỏng mở chat bằng tính năng Chat thật (kết nối Socket, lưu tin nhắn, thông báo).
- [ ] **Notification System**: Đẩy thông báo (Push Notification/Email) khi có Task mới, Task bị từ chối, hoặc sắp đến Deadline.

## 4. New Models Module
- [ ] **BOM Parser**: Xây dựng thuật toán Backend đọc file Excel BOM, tự động bóc tách và lọc ra những linh kiện nhà máy thực tế mua.
- [ ] **Base Model Inheritance**: Tính năng cho phép copy tự động các Risk Factor từ Model tiền nhiệm sang Model mới.
- [ ] **HQ Integration (Optional)**: Tích hợp API (nếu có) để tự động lấy trạng thái Approval Sheet từ hệ thống của HQ, thay vì để PIC tự tick thủ công.
