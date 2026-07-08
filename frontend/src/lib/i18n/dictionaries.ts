export const dictionaries = {
  vi: {
    nav: {
      workforce: "Nhân Sự",
      organization: "Tổ chức",
      training: "Đào tạo",
      certificate: "Chứng chỉ",
      test: "Kiểm tra",
      inspections: "Kiểm tra",
      newModels: "Sản Phẩm Mới",
      compliance: "Tuân Thủ",
      tasks: "Nhiệm vụ",
      support: "Hỗ Trợ",
      faq: "Hỏi Đáp",
      qa: "Q&A",
      chatWithAdmin: "Chat với Admin",
      userManagement: "Quản lý Người Dùng",
      auditLogs: "Nhật ký Hệ thống",
    },
    header: {
      searchPlaceholder: "Tìm kiếm Nexus...",
      toggleTheme: "Đổi giao diện",
      lens: "Góc nhìn",
      switchPerspective: "Chuyển góc nhìn",
      changePassword: "Đổi mật khẩu",
      signOut: "Đăng xuất",
      devOverride: "Ghi đè định danh (Dev)",
    },
    task: {
      kanbanTitle: "Bảng Kanban",
      kanbanSubtitle: "Quy trình thực thi kéo và thả.",
      addTask: "Thêm",
      addCard: "Thêm thẻ",
      columns: {
        todo: "Cần Làm",
        inProgress: "Đang Làm",
        review: "Chờ Duyệt",
        done: "Hoàn Thành"
      },
      priority: {
        low: "Thấp",
        medium: "Trung bình",
        high: "Cao",
        critical: "Khẩn cấp"
      }
    }
  },
  en: {
    nav: {
      workforce: "Workforce",
      organization: "Organization",
      training: "Training",
      certificate: "Certificate",
      test: "Test",
      inspections: "Inspections",
      newModels: "New Models",
      compliance: "Compliance",
      tasks: "Tasks",
      support: "Support",
      faq: "FAQ",
      qa: "Q&A",
      chatWithAdmin: "Chat with Admin",
      userManagement: "User Management",
      auditLogs: "Audit Logs",
    },
    header: {
      searchPlaceholder: "Search Nexus...",
      toggleTheme: "Toggle Theme",
      lens: "Lens",
      switchPerspective: "Switch Perspective",
      changePassword: "Change Password",
      signOut: "Sign out",
      devOverride: "Dev Identity Override",
    },
    task: {
      kanbanTitle: "Kanban Board",
      kanbanSubtitle: "Drag and drop execution workflow.",
      addTask: "Task",
      addCard: "Add Card",
      columns: {
        todo: "To Do",
        inProgress: "In Progress",
        review: "Waiting Approval",
        done: "Completed"
      },
      priority: {
        low: "Low",
        medium: "Medium",
        high: "High",
        critical: "Critical"
      }
    }
  }
};

export type Locale = "vi" | "en";
export type Dictionary = typeof dictionaries.vi;
