# System Architecture: IQC Quality Management Cloud

## Tech Stack
- **Frontend:** Next.js 16, React 19, Tailwind CSS v4, Framer Motion, Lucide React.
- **Backend:** .NET (C#), Entity Framework Core, SignalR (Chat).
- **Database:** (Chưa cấu hình cụ thể, có thể là SQL Server/PostgreSQL thông qua EF Core).

## Module Structure

### 1. Chat & Collaboration (`/components/FloatingChat.tsx`)
- Tích hợp SignalR kết nối `chathub` real-time.
- Hỗ trợ Chat One-to-One, Group, Tag user `@`.
- Chứa logic Action-oriented: Slash commands (`/report`, `/task`) và Suggestion Chips.
- Global Event Listener: Bắt các sự kiện từ ngoài (vd: `open-admin-chat` từ Header).

### 2. New Models NPI (`/app/new-model`)
- **Backend Models:** (Kế hoạch) `NewModelProject`, `NpiStage`, `BomItem`, `JigRequirement`.
- **Frontend UI:** Giao diện Kanban động, Side Drawer chứa 4 Tabs chi tiết:
  - Stages & Approvals (Tiến độ Approval)
  - BOM Performance (Đối chiếu Level 1 Material)
  - Jig Control (Kiểm soát tiến độ nhận hàng)
  - Incoming QC Lots (Tiến độ nguyên vật liệu về kho)

### 3. Standards / Compliance (Planned)
- Bảng `InspectionStandard` và `InspectionItem`.
- Quản lý các Tiêu chí, Mức dung sai, Công cụ đo.

### 4. Layout & Theming
- Hỗ trợ Dark/Light mode toàn hệ thống qua `next-themes`.
- Header có Glassmorphism, Mini Search/Command (Ctrl+K).
