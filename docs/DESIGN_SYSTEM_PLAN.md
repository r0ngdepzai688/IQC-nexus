# IQC Nexus Design System Plan

## 1. Mục tiêu

Design system tạo ngôn ngữ giao diện thống nhất, accessible và có thể mở rộng cho IQC Nexus. Hệ thống không chỉ là bộ component; nó gồm tokens, layout, navigation, content, interaction, accessibility, i18n, governance và công cụ kiểm thử.

## 2. Nguyên tắc thiết kế

- Rõ ràng trước trang trí; thông tin chất lượng và trạng thái phải đọc được nhanh.
- Nhất quán về semantics, không chỉ giống màu sắc.
- Keyboard, screen reader và contrast là yêu cầu mặc định.
- Motion giải thích thay đổi trạng thái, không gây nhiễu.
- Responsive theo nội dung và tác vụ, không chỉ breakpoint thiết bị.
- Text có thể dài hơn 30–50% khi dịch mà layout vẫn chịu được.
- Theme không thay đổi ý nghĩa trạng thái.
- Component API nhỏ, typed và composable.

## 3. Kiến trúc package

```text
src/design-system/
  tokens/           semantic + primitive tokens
  foundations/      typography, icons, focus, motion
  components/       reusable UI primitives/composites
  patterns/         form, table, review queue, status, empty/error
  layouts/          app shell, page, panel, split view
  accessibility/    helpers and test contracts
  i18n/             formatting and content conventions
  docs/              stories, usage, do/don't
```

Ban đầu giữ trong monorepo để thay đổi nhanh. Chỉ tách package/version độc lập khi có consumer thứ hai.

## 4. Design Tokens

### Tầng token

1. **Primitive:** palette, scale spacing, radius, font size, duration.
2. **Semantic:** surface, text, border, action, focus, status-success/warning/error/info.
3. **Component:** chỉ dùng khi semantic token không đủ, ví dụ `table-row-hover`.

### Nhóm token

- Color: canvas, surface, elevated, text, muted, border, interactive, data visualization.
- Typography: family, size, weight, line-height, letter spacing.
- Spacing/sizing: scale 4px hoặc scale được phê duyệt.
- Shape: radius, border width.
- Elevation: shadow và overlay.
- Motion: duration, easing, distance.
- Layout: sidebar/header/panel widths, content max-width, breakpoints.
- Z-index: nền, sticky, dropdown, modal, toast, command palette.

Tokens xuất qua CSS custom properties; TypeScript chỉ tham chiếu tên semantic. Không hard-code hex/spacing mới trong feature code. Có light, dark và high-contrast token sets.

## 5. Component Library

### Foundation components

- Button, IconButton, Link, Input, Textarea, Select, Checkbox, Radio, Switch.
- Label, HelpText, ErrorText, Badge, Avatar, Tooltip, Separator, Spinner.
- Dialog, Drawer, Popover, Dropdown, Toast, Tabs, Accordion.
- Table/DataGrid, Pagination, FilterBar, Date/Time controls.

### Domain-neutral patterns

- PageHeader, Breadcrumbs, ActionBar, Search/Command palette.
- StatusBadge, Progress, Timeline, ActivityFeed.
- EmptyState, ErrorState, PermissionDenied, Loading/Skeleton.
- FormSection, Confirmation, UnsavedChanges guard.
- ReviewQueue, DiffViewer, AuditTrail, FileUpload/ImportSummary.

### Contract mỗi component

- Typed props và controlled/uncontrolled behavior rõ.
- Keyboard interaction, focus management và ARIA documented.
- Loading, disabled, error, empty và overflow states.
- Responsive behavior và localization stress story.
- Unit/interaction/a11y test; visual regression cho variant trọng yếu.

Storybook hoặc công cụ tương đương là catalog và review surface; không coi screenshot Figma là implementation contract duy nhất.

## 6. Layout System

- `AppShell`: header, sidebar, utility region, content và overlays.
- `PageLayout`: title, description, breadcrumbs, primary/secondary actions.
- `DashboardLayout`: responsive grid có priority, không cố định số card.
- `MasterDetailLayout`: list/detail hoặc review queue.
- `WorkspaceLayout`: tabs/rail/context panel cho dự án.
- `FormLayout`: single/two-column theo độ dài và dependency field.

Grid dựa trên CSS Grid/Flex, container queries khi hợp lý. Breakpoint được xác định từ điểm vỡ nội dung. Page không tự tạo app shell riêng như Tasks hiện tại; variation được cấu hình qua layout primitives.

## 7. Navigation

- Information architecture theo module và nhiệm vụ, không theo cấu trúc code.
- Sidebar cấp một; section có thể collapse nhưng route active luôn rõ.
- Breadcrumb cho độ sâu > 1.
- Command palette hỗ trợ search/navigate/action có quyền.
- Mobile dùng drawer/bottom affordance phù hợp, giữ cùng route model.
- Navigation item được lọc theo capability từ server nhưng route vẫn được backend bảo vệ.
- Deep link, refresh và back/forward phải ổn định.

## 8. Motion

- Functional motion: state transition, hierarchy, feedback và continuity.
- Duration gợi ý: instant 0–100ms, fast 120–180ms, standard 180–280ms, emphasized 280–400ms.
- Chỉ animate transform/opacity nếu có thể; tránh layout thrash.
- Không dùng animation vô hạn trang trí trong màn hình tác vụ trọng yếu.
- Tuân thủ `prefers-reduced-motion`; mọi thông tin vẫn hiểu được khi motion tắt.
- Loading animation không che độ trễ; tác vụ dài có progress/state thật.

## 9. Theme

- Light, dark và high-contrast dùng chung semantic tokens.
- Theme preference theo system, user profile và local fallback; server render tránh flash.
- Status không chỉ dựa màu; luôn có text/icon/shape phụ trợ.
- Data visualization có palette color-blind-safe và kiểm tra contrast.
- Asset/icon phải hoạt động trên mọi surface.

## 10. Accessibility

Mục tiêu WCAG 2.1 AA cho luồng production.

- Tất cả tác vụ dùng được bằng keyboard; focus visible và thứ tự logic.
- Dialog/popover quản lý focus đúng; escape/return focus nhất quán.
- Form có label, instruction, error association và summary.
- Live region cho async status có ý nghĩa.
- Table có header, caption/context và alternative trên màn hình nhỏ.
- Contrast text/UI đạt chuẩn; zoom 200% và reflow 400% được kiểm tra.
- Touch target tối thiểu hợp lý; không phụ thuộc hover.
- Automated axe + manual keyboard/screen reader test cho critical flows.

## 11. Đa ngôn ngữ

- Locale ban đầu: `vi-VN`, `en-US`; chuẩn bị `ko-KR` nếu nghiệp vụ yêu cầu.
- Không hard-code chuỗi mới trong component; dùng message key có namespace theo feature.
- ICU message format cho plural/select; không nối chuỗi dịch.
- Intl API cho date/time/number; lưu UTC và format theo user locale/timezone.
- Thuật ngữ nghiệp vụ có glossary do Product/Data Owner duyệt.
- Pipeline kiểm tra missing/unused keys và pseudo-localization.
- UTF-8 end-to-end; có test chống mojibake.
- Layout chịu text expansion, wrapping và không ép width theo tiếng Anh.

## 12. Content và trạng thái

- Tên action dùng động từ cụ thể: Commit batch, Resolve review, Activate project.
- Error message gồm vấn đề, tác động và cách xử lý; technical detail đặt trong support ID.
- Status catalogue dùng cùng thuật ngữ ở UI, API và audit.
- Confirmation chỉ cho destructive/irreversible/high-impact action.
- Empty state phân biệt chưa có dữ liệu, không có quyền, filter không khớp và lỗi tải.

## 13. Data-dense UI

- DataGrid hỗ trợ column visibility, sort/filter, sticky header, pagination/virtualization khi cần.
- Không dùng màu nền dày đặc làm tín hiệu duy nhất.
- Review queue ưu tiên severity, age, owner và SLA.
- Diff viewer hiển thị raw → normalized → proposed/core, kèm lý do rule/mapping.
- Dashboard card có định nghĩa metric và timestamp; tránh số mock trong production.

## 14. Governance

- Design System Owner cùng hội đồng nhỏ Product Design + Frontend + Accessibility.
- Contribution qua RFC nhỏ, story, tests và changelog.
- Component có trạng thái Proposed, Experimental, Stable, Deprecated.
- Semantic versioning khi có package; deprecation tối thiểu hai release.
- Không fork component ở feature nếu thiếu variant; đề xuất extension hoặc pattern.
- Theo dõi adoption, duplicate components, accessibility defects và breaking changes.

## 15. Công cụ và quality gates

- Storybook/catalog, interaction tests và axe.
- Visual regression trên themes, viewport và locale chính.
- ESLint rule/codemod để hạn chế raw colors và imports ngoài public API.
- Bundle budget và tree-shaking.
- Browser matrix theo môi trường công ty; test keyboard/screen reader thủ công trước release lớn.

## 16. Lộ trình

### Wave 1 — Foundation

- Inventory UI hiện tại; định nghĩa tokens, typography, focus và status catalogue.
- Chuẩn hóa Button/Input/Dialog/Badge/Table cơ bản.
- Thiết lập catalog và automated a11y.

### Wave 2 — Shell và critical flows

- Hợp nhất dashboard/tasks app shell và navigation.
- Migrate Login, Data Hub import/review, New Models master plan.
- Thêm error/loading/empty patterns.

### Wave 3 — Data-dense và i18n

- DataGrid, diff, audit, timeline.
- Chuyển toàn bộ text luồng chính sang message catalogue; pseudo-localization.

### Wave 4 — Adoption và governance

- Migrate module còn lại theo opportunistic rule: chạm màn hình nào thì chuyển màn hình đó.
- Deprecate component trùng; đo adoption và bundle/performance.

## 17. Tiêu chí thành công

- ≥ 80% critical UI dùng tokens/component chuẩn sau Wave 3.
- Không có axe serious/critical trên critical journeys.
- 100% component stable có docs, keyboard contract và tests.
- Không còn raw color mới ngoài token layer.
- Không mojibake/missing key trong build production.
- Thời gian tạo màn hình CRUD/review mới giảm ít nhất 30% so với baseline.
- Người dùng hoàn thành các tác vụ chính với tỷ lệ lỗi và thời gian được đo, không chỉ đánh giá thẩm mỹ.

