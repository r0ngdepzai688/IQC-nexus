# Hướng Dẫn Triển Khai (Deployment Guide) - Mạng Nội Bộ Công Ty

Tài liệu này hướng dẫn cách triển khai hệ thống Portal lên máy chủ nội bộ (On-Premises) phục vụ mục đích kiểm thử và sử dụng thực tế.

## Yêu Cầu Hệ Thống (Prerequisites)

1. **Hệ điều hành:** Windows Server / Linux (Ubuntu/CentOS).
2. **Backend:**
   - .NET 8.0 SDK / Runtime.
3. **Frontend:**
   - Node.js (Phiên bản v18.17.0 trở lên).
   - NPM hoặc Yarn.
4. **Cơ Sở Dữ Liệu:**
   - SQLite (tích hợp sẵn, file `.db` sẽ tự động tạo).
   - File Excel Danh sách người dùng gốc: Đặt tại `backend/src/IqcQms.Infrastructure/Data/Seeders/Danh sách NV .xlsx` (hoặc cấu hình đường dẫn tương ứng).

---

## Bước 1: Khởi Chạy Backend API (.NET 8)

1. Mở Terminal / PowerShell.
2. Di chuyển vào thư mục Backend API:
   ```bash
   cd Portal/backend/src/IqcQms.Api
   ```
3. Khôi phục các thư viện cần thiết:
   ```bash
   dotnet restore
   ```
4. Xây dựng (Build) project:
   ```bash
   dotnet build -c Release
   ```
5. Khởi chạy Backend:
   ```bash
   dotnet run --urls="http://0.0.0.0:5000"
   ```
   > **Lưu ý:** Chạy với `0.0.0.0` để các máy tính khác trong cùng mạng LAN có thể gọi API thông qua IP của máy chủ (Ví dụ: `http://192.168.1.100:5000`).

---

## Bước 2: Triển Khai Frontend (Next.js)

### Môi trường Phát triển (Dùng cho Kiểm thử ngày mai)
1. Mở một Terminal / PowerShell mới.
2. Di chuyển vào thư mục Frontend:
   ```bash
   cd Portal/frontend
   ```
3. Cài đặt các gói thư viện (Dependencies):
   ```bash
   npm install
   ```
   > **Lưu ý:** Đảm bảo máy tính có kết nối Internet để tải các thư viện từ npm.
4. Chạy Frontend:
   ```bash
   npm run dev -- -H 0.0.0.0 -p 3000
   ```
   *Người dùng trong mạng nội bộ truy cập qua: `http://<IP_MAY_CHU>:3000`*

### Môi trường Sản xuất (Production)
Khi muốn chạy ổn định, hãy Build Frontend:
1. Sửa file `.env` (nếu có) hoặc code gọi API để trỏ tới `http://<IP_MAY_CHU>:5000/api` thay vì `localhost`.
2. Chạy lệnh:
   ```bash
   npm run build
   npm run start -- -H 0.0.0.0 -p 3000
   ```

---

## Bước 3: Đăng Nhập & Cấp Quyền

1. Mở trình duyệt và truy cập `http://<IP_MAY_CHU>:3000`.
2. Tài khoản mặc định trong hệ thống đang được Seed (khởi tạo) từ File Excel.
3. Đối với các User chưa đổi mật khẩu, mật khẩu mặc định được mã hóa là: `Welcome@123`.
4. Để test tính năng **Lens (Switch Perspective)** hoặc kiểm tra **Quản trị hệ thống**:
   - Sử dụng tính năng **Dev Identity Override** ở góc phải (Avatar) để thiết lập quyền `Administrator` tạm thời (Nút màu vàng).
   - F5 lại trang và kiểm tra đầy đủ các tính năng quyền hạn.

---

## Xử lý Sự cố Thường gặp

- **Lỗi không kết nối được Backend:** Đảm bảo Firewall (Tường lửa) của máy chủ đã mở Port `5000` (Backend) và `3000` (Frontend) cho kết nối Inbound.
- **Lỗi đăng nhập:** Kiểm tra file SQLite `iqcqms.db` trong thư mục Backend xem đã được tự động cấp quyền Đọc/Ghi chưa.
- **Trắng trang Frontend:** Xóa thư mục `.next` và chạy lại `npm run build`.

*Chúc bạn có buổi demo/test thành công tại công ty ngày mai!*
