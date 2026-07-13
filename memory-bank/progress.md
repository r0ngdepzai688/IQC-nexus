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
7. **Collaboration Hub (Advanced Features):**
   - Nâng cấp Sidebar Chat Floating với hệ thống Layout 2 Tab (Chats/Contacts).
   - Tổ chức Contact List: Gom nhóm (Group by Department) với chức năng Thu gọn/Mở rộng tối ưu không gian, hiển thị 20 người đầu tiên mỗi bộ phận.
   - Quản lý Thread nâng cao (Chat Settings): Bảng Info panel trượt từ phải ra cho phép đổi tên nhóm, xóa thành viên, thêm thành viên, xóa toàn bộ lịch sử trò chuyện, xóa nhóm.
   - Bổ sung bộ lọc mở rộng: All, Unread, Mentions, Favorite.
   - Thêm tính năng Emoji Picker bằng animation mượt mà.
   - Thêm tính năng Tag tên (`@mention`): Hiển thị menu gợi ý thành viên khi gõ `@` theo thời gian thực.
8. **Data Hub & Master Plan Ingestion Pipeline (Phase 1 & 2):**
   - Hoàn thành DataHub API với khả năng Upload, Phân tích (Parse), Staging, Validate (Hard Columns, Business Logic), Duplicate Checks, và Ghi Log.
   - Hoàn thành Commit Transaction chuyển đổi từ `Staging_MasterPlan` sang `MasterPlans` (Core) và `ProjectMilestones` an toàn.
   - Xử lý thành công PVR Target Logic (`Urgent`, `Ready`, `Future`, `Created`) cho giao diện Master Plan.
   - Tích hợp thành công dữ liệu Core API thật (`GET /api/masterplan/records`) vào UI Master Plan Dashboard.
9. **UI/UX Refinements:**
   - Sửa lỗi điều hướng menu chính: Ngăn chặn tự động chuyển trang khi click vào menu chính nếu menu đó có danh sách menu phụ.
   - Tối ưu hóa UI/UX các nút chức năng trong Chat (nút Favorite dạng Star, Xóa Chat nhanh trên danh sách hover).

## Đang thực hiện / Cần làm tiếp (Next Steps - Phase 3)
1. **Validation Errors & Review Queue UI:** Triển khai màn hình xử lý lỗi validation và hàng chờ duyệt dữ liệu cho Data Hub.
2. **Mapping Dictionary UI:** Xây dựng giao diện quản lý từ điển ánh xạ (alias mapping).
3. **Batch Detail & Report Export:** Xây dựng giao diện xem chi tiết Batch, audit log và export file Excel/CSV báo cáo lỗi.
4. **Module Compliance (Standards Library):** Xây dựng trang thư viện quản lý tiêu chuẩn kỹ thuật (`InspectionStandard`, `InspectionItem`).
5. **Hoàn thiện UI cho Workforce:** Dựng nội dung thực tế cho các trang Training, Certificate và Test.
