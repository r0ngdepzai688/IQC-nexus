# Task 002B Definition of Ready

## Task
- Roadmap: Task 002 / E01-T02
- Slice: 002B — Replace PII-dependent fixtures with synthetic fixtures
- Status: NOT READY

## Objective
Thay mọi fixture/test data phụ thuộc PII thật bằng dữ liệu synthetic, không đổi behavior production.

## Entry conditions
- [x] Task 001 hoàn thành.
- [x] 002A hoàn thành containment.
- [x] Root personnel artifacts không còn tracked.
- [ ] Hoàn tất consumer inventory.
- [ ] Chốt canonical synthetic schema.
- [ ] Chốt edge-case matrix.
- [ ] Xác nhận production không phụ thuộc root artifacts.
- [ ] Có Data Owner hoặc Security approval.

## Discovery scope
- frontend/src/data/**
- frontend/src/**/*fixture*
- frontend/src/**/*mock*
- frontend/src/**/*.test.*
- backend/**/Seeders/**
- backend/**/*Fixture*
- backend/**/*Mock*
- backend/**/*Test*
- mọi tham chiếu tới personnel, employee, staff hoặc users.json

## Synthetic fixture requirements
- Không chứa PII thật hoặc dữ liệu biến đổi từ dữ liệu thật.
- Dùng prefix SYN- và domain example.invalid.
- Giữ đúng kiểu dữ liệu và constraint hiện tại.
- Bao phủ Unicode Việt, Unicode Hàn, optional fields, duplicate key, active/inactive và invalid input trong test chuyên biệt.
- Không được bundle vào production nếu chỉ dành cho test/development.

## Out of scope
- Git history rewrite
- Xóa file local duy nhất
- Database migration
- API contract change
- Auth/RBAC change
- CI pipeline mới
- Bắt đầu 002C hoặc Task 003

## Definition of Done
- [ ] Hoàn tất consumer inventory.
- [ ] Không còn fixture phụ thuộc PII thật.
- [ ] Synthetic fixture đạt yêu cầu.
- [ ] Không có PII trong staged diff.
- [ ] Production behavior không đổi.
- [ ] Backend build xanh.
- [ ] Frontend checks xanh nếu frontend bị sửa.
- [ ] Data inventory được cập nhật.
- [ ] Data Owner hoặc Security approver xác nhận.
- [ ] 002C chưa bắt đầu.

## Readiness decision
NOT READY: còn thiếu inventory, schema, edge-case matrix và approval.
