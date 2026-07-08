# 03. User Roles

The New Models module supports a strict role-based access control (RBAC) system.

## Roles

### Admin
- **Description:** System administrator with full access.
- **Permissions:** Can modify workflows, reassign owners, override statuses, and manage system configuration.

### Team Leader
- **Description:** Head of the IQC department.
- **Permissions:** Views executive dashboards, approves critical stage gates (e.g., PR to SR), reviews escalated risks.

### Group Leader
- **Description:** Manages multiple part groups or projects.
- **Permissions:** Reviews overall project progress, approves Vendor Capability and Golden Samples, manages resource allocation.

### Part Leader
- **Description:** Lead engineer for a specific commodity or material category.
- **Permissions:** Analyzes BOM, evaluates Grade A materials, conducts vendor capability reviews, generates IQC material lists.

### Cell Leader
- **Description:** Line supervisor managing daily IQC operations.
- **Permissions:** Tracks incoming lots, records IQC results, manages daily tasks, monitors dashboard.

### Staff (Engineer/Inspector)
- **Description:** Executes daily tasks.
- **Permissions:** Uploads DFx, inputs Golden Sample locations, creates ISS, logs issues.

---

## Permission Matrix

| Feature | Admin | Team Leader | Group Leader | Part Leader | Cell Leader | Staff |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Visible Widgets** | All | Executive, KPI | Project, KPI | Project, Vendor | Action Center, Timeline | Action Center |
| **Editable Data** | All | None | High-level KPI | BOM, Vendor Data | IQC Results, Issues | Tasks, Comments |
| **Approval Rights** | Override | Phase Gates | Vendor/Sample | ISS/Drawings | Daily Tasks | None |
| **Notification Rights** | System | Critical Esc | High Esc | Medium Esc | Task Updates | Task Assignment |
