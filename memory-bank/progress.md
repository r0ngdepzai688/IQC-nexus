# Project Progress

## Đã hoàn thành (Completed)
1. **Thiết lập Base System:**
   - Hoàn thiện khung Layout chính (Sidebar, Header, Next-themes).
2. **Branding & Naming:**
   - Tên chính thức: `IQC Nexus` (Enterprise Quality Platform).
   - Đã thiết kế Logo mới dạng Hình học (Box Lucide) tối giản và thanh lịch.
3. **Quản trị người dùng & Phân quyền (Role Architecture):**
   - Tách biệt hoàn toàn `Business Role` (Team/Group/Part/Cell Leader) và `System Role` (Administrator/User).
   - Cập nhật Data Seeder từ Excel và Backend API để phản ánh kiến trúc quyền này.
   - Sửa lỗi State Management của AuthContext để đọc đúng user từ localStorage.
4. **Header - Enterprise Command Center:**
   - Đại tu toàn bộ Header sang giao diện tràn viền chuẩn doanh nghiệp cao cấp.
   - Tích hợp Universal Search (⌘K), Micro-interactions mượt mà, gạch chân linh hoạt cho Menu.
   - Ẩn hoàn toàn "Dev Simulation Mode" vào trong Menu Dropdown của Avatar để tối ưu không gian.
5. **Workforce - Organization Health Center:**
   - Xây dựng giao diện Digital Twin quản lý sức khỏe tổ chức thay thế cho sơ đồ tổ chức tĩnh truyền thống.
   - Tính toán và gán đúng các Leader thực tế lấy từ dữ liệu gốc vào từng node nhánh (IQC G, 1P, 2P, 3P).
   - Chia tách menu con cho Workforce thành 4 mục: Organization, Training, Certificate, Test (kèm Boilerplate routing).
6. **New Models NPI (Giao diện):**
   - Thiết kế thành công màn hình `Command Center` chuẩn Enterprise.
   - Cá nhân hóa câu chào lấy chính xác tên `user.name` của tài khoản đăng nhập.
   - Cập nhật đúng các dự án chiến lược: Galaxy Z Fold 7, Galaxy S26 Ultra, Galaxy Buds 4 Pro, Galaxy Watch 7.

## Đang thực hiện / Cần làm tiếp (Next Steps)
1. **Backend Integration cho New Models:** Bổ sung Entity Framework Models (như `NewModelProject`, `NpiStage`, v.v.) vào `AppDbContext`.
2. **Module Compliance (Standards Library):** Xây dựng trang thư viện quản lý tiêu chuẩn kỹ thuật (`InspectionStandard`, `InspectionItem`).
3. **Module Overview (Dashboard):** Lên thiết kế các Biểu đồ phân tích dữ liệu chuyên sâu bằng Recharts, đồng thời làm Dashboard biến đổi thông minh theo Role của người dùng (từ User DB).
4. **Hoàn thiện UI cho Workforce:** Dựng nội dung thực tế cho các trang Training, Certificate và Test.
