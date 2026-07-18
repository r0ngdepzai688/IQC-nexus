# IQC Nexus Data Hub Architecture

## 1. Sứ mệnh

Data Hub là nền tảng dữ liệu vận hành trung tâm của IQC Nexus: tiếp nhận dữ liệu từ nhiều nguồn, bảo toàn bản gốc, chuẩn hóa về canonical model, kiểm tra chất lượng, đưa ngoại lệ qua workflow con người và commit có kiểm soát vào core modules. Data Hub không phải data warehouse; nó chịu trách nhiệm ingestion, quality gate, lineage và operational synchronization.

## 2. Nguyên tắc

- Raw input là immutable và luôn truy xuất được theo checksum.
- Source-specific logic dừng ở connector/profile; business rules nằm tại server.
- Mọi transformation, mapping và rule đều có version.
- Không commit record chưa qua policy tương ứng.
- At-least-once delivery ở transport, exactly-once effect bằng idempotency tại commit.
- Human override phải có quyền, lý do và audit.
- Replay từ raw không làm mất lineage hoặc ghi trùng.
- Quarantine thay vì bỏ qua âm thầm.

## 3. Kiến trúc logic

```text
Sources
  Excel Upload | REST API | Windows Agent | Future Connectors
       |
Connector Gateway / Ingestion API
       |
Receipt + Immutable Raw Store + Source Manifest
       |
Pipeline Orchestrator
       +--> Header Detection Engine
       +--> Parser & Canonical Mapper
       +--> Mapping Dictionary
       +--> Validation Engine
       +--> Staging + Quality Results
                         |
                Review Workflow
                         |
                  Commit Pipeline
                         |
              Core Modules + Outbox

All stages --> Lineage/Audit --> Metrics/Logs/Alerts
Failures --> Retry / Dead Letter / Quarantine / Replay
```

## 4. Canonical ingestion envelope

Mọi connector gửi cùng một envelope:

| Trường | Ý nghĩa |
|---|---|
| `ingestionId` | UUID do gateway cấp hoặc chấp nhận idempotency key |
| `sourceId` | Nguồn đã đăng ký |
| `connectorType/version` | Loại và phiên bản connector |
| `module/dataContract` | Module và contract mục tiêu |
| `schemaVersion` | Phiên bản schema nguồn/canonical mong muốn |
| `occurredAt/receivedAt` | Thời điểm nguồn và server |
| `fileName/contentType/size` | Metadata payload |
| `checksum` | SHA-256 nội dung |
| `agentId` | Nếu đến từ Agent |
| `correlationId` | Theo dõi xuyên pipeline |
| `attributes` | Metadata allowlist theo nguồn |

Payload lớn được stream vào raw store; database chỉ lưu metadata và URI quản lý.

## 5. Source Connectors

### Excel/manual upload

- Hỗ trợ `.xlsx`, `.xlsm`, `.xls` theo allowlist; xác thực magic bytes và giới hạn kích thước.
- Quét malware nếu hạ tầng cho phép.
- Lưu nguyên file trước khi parse; không tin tên file làm path.
- Profile xác định sheet selection, scan window, locale, timezone và contract.

### REST API connector

- OAuth2 client credential/mTLS.
- JSON schema/OpenAPI validation tại biên.
- Idempotency key bắt buộc cho command tạo batch.
- Rate limit theo source; payload invalid đi quarantine với error code ổn định.

### Windows Agent connector

- Theo dõi folder/file share, phát hiện file ổn định bằng size/mtime + exclusive/open check theo policy.
- Tính checksum, tạo manifest, upload chunk/stream và chờ receipt.
- Local durable spool; chỉ archive/remove nguồn sau server acknowledgement.
- Heartbeat, health, version, backlog và disk watermark.
- Agent không chứa mapping/validation business rules.

### Connector tương lai

- SFTP, database CDC/polling, message bus, SharePoint/Drive hoặc vendor APIs.
- Mỗi connector implement capability contract: discover, validate config, poll/push, checkpoint, fetch, acknowledge, health.
- Connector chạy sandboxed/isolated khi có code bên thứ ba; secrets tham chiếu từ vault.

## 6. Source Registry và Connector Profile

`SourceDefinition` lưu owner, module, contract, sensitivity, timezone, SLA, active state. `ConnectorProfile` lưu cấu hình không nhạy cảm, lịch chạy, retry và detection profile. Secret chỉ lưu reference đến vault.

Mọi thay đổi profile có version, approval và effective time. Batch luôn ghi lại profile version đã dùng.

## 7. Header Detection Engine

### Pipeline phát hiện

1. Đọc preview giới hạn số sheet/row/cell.
2. Normalize Unicode, whitespace, punctuation và locale.
3. So khớp exact alias, normalized alias, token similarity và profile-specific patterns.
4. Chấm điểm từng candidate row theo số field nhận diện, required-field coverage, uniqueness và type plausibility ở các dòng sau.
5. Chọn candidate nếu vượt threshold và margin; nếu không, đưa review.

### Kết quả

- `detectedHeaderRow`, mapping column→canonical field.
- Confidence tổng và từng field.
- Missing required/ambiguous/duplicate mappings.
- Engine version, alias dictionary version và evidence preview đã mask.

Không tự động học alias mới từ override. Alias được steward phê duyệt rồi phát hành thành dictionary version mới.

## 8. Mapping Dictionary

### Phạm vi khóa

`module + dataContract + source/profile + dictionaryType + rawValueNormalized + effectiveVersion`.

### Khả năng

- Header aliases, value mappings, user identity aliases, unit/status/category mappings.
- Draft → Review → Published → Retired.
- Effective from/to; rollback phiên bản.
- Exact, normalized và regex mapping theo allowlist; regex phải có timeout/test để tránh ReDoS.
- Conflict detection khi hai mapping cùng hiệu lực.
- Usage metrics và impact preview trước publish.

### Governance

- Data Steward soạn; Approver phát hành với four-eyes cho mapping ảnh hưởng core key/status.
- Batch pin dictionary version; replay mặc định dùng version cũ, reprocess có chủ đích mới dùng version mới.

## 9. Canonical parsing và staging

- Parser adapter theo `dataContract + schemaVersion`.
- Chuyển kiểu dùng invariant rules và locale được khai báo, không dựa locale của server.
- Canonical row lưu raw row reference, parsed values, normalized values và parse diagnostics.
- Staging có batch partition/key và retention; không trở thành nguồn dữ liệu nghiệp vụ lâu dài.
- PII fields có classification/masking.

## 10. Validation Engine

### Loại rule

- Schema: required column, data type, length, enum.
- Row: required value, range, format, date ordering.
- Cross-row: duplicate, aggregate consistency.
- Reference: employee, category, part, supplier.
- Core-state: existing project/version/status/concurrency.
- Policy: quyền nguồn được phép cập nhật field nào.

### Mô hình kết quả

- `ruleId`, `ruleVersion`, severity, status, field, message code, parameters, suggested action.
- Severity: Info, Warning, ReviewRequired, Error, Blocked.
- Kết quả immutable theo pipeline run; revalidate tạo run mới.

### Thực thi

- Rule catalogue được test và version-control.
- Rule thuần khi có thể; reference snapshot/version được ghi lại.
- Giới hạn CPU/time/memory cho batch.
- Tổng hợp quality score chỉ để ưu tiên, không thay thế rule gate.

## 11. Review Workflow

### Trạng thái

`Pending → Assigned → InReview → Resolved/Rejected/Escalated → Revalidated → Ready/Blocked`.

### Chức năng

- Assignment theo module/source/conflict/scope; có queue và owner.
- SLA, reminder, escalation và workload visibility.
- Resolution action có semantics cụ thể: map value, correct row, accept exception, skip row, reject batch, request source correction.
- Reason bắt buộc cho override; attachment/comment tùy trường hợp.
- Four-eyes approval cho business key, identity mapping hoặc blocked rule override.
- Sau resolution phải revalidate; không trực tiếp đổi `ReviewRequired` thành `ReadyToInsert` nếu chưa chạy rule lại.

## 12. Commit Pipeline

### Điều kiện

- Batch/pipeline run ở trạng thái committable.
- Tất cả required rules đạt hoặc override hợp lệ.
- Contract, mapping, rule và reference versions được pin.
- Actor có permission và scope.

### Cơ chế

1. Nhận idempotency key và lock/lease batch.
2. Đọc ready rows theo snapshot.
3. Resolve business key và kiểm tra rowversion core.
4. Bulk insert/update theo allowlist field ownership.
5. Ghi lineage, audit và outbox cùng transaction SQL.
6. Commit transaction; phát event từ outbox.
7. Đánh dấu receipt và archive policy.

Unique constraints bảo đảm effect không trùng. Partial commit chỉ được phép nếu contract tuyên bố row-independent; mặc định batch atomic cho dữ liệu liên kết chặt.

## 13. Audit và lineage

Mỗi core value cần truy về:

- Source, connector/agent, original payload/file checksum.
- Batch, raw object, sheet/row/cell nếu áp dụng.
- Parser, header engine, mapping dictionary và validation rule versions.
- Review decisions, actor, reason và approval.
- Commit run, before/after, timestamp và correlation trace.

Audit business là append-only. Diagnostic logs không được dùng thay audit.

## 14. Versioning

- Data contract: semantic version; breaking change tạo major version.
- Connector/profile, parser, header engine, mapping dictionary, validation rule set và commit mapping đều có version riêng.
- `PipelineDefinitionVersion` pin toàn bộ thành phần cho một run.
- Raw immutable; staging/result versioned theo `pipelineRunId`.
- Replay có hai chế độ:
  - **Reproduce:** dùng toàn bộ version cũ.
  - **Reprocess:** dùng version mục tiêu mới, tạo comparison report và không overwrite run cũ.

## 15. Monitoring và SLO

### Metrics

- Received/processed/committed/rejected batches và rows.
- Queue age, stage duration, throughput, retry count.
- Header confidence, mapping miss rate, validation error rate.
- Review backlog, age và SLA breaches.
- Commit conflict/rollback, replay count.
- Agent health, spool depth, disk capacity, last receipt.

### Alert

- Không nhận dữ liệu theo lịch nguồn.
- Batch stuck vượt ngưỡng stage.
- Mapping miss/error tăng bất thường.
- Review SLA breach.
- Commit failure hoặc duplicate conflict.
- Agent offline/certificate sắp hết hạn/disk gần đầy.

## 16. Error recovery

| Failure | Trạng thái bảo toàn | Recovery |
|---|---|---|
| Upload gián đoạn | Agent spool/raw temp có checksum | Resume/retry với idempotency key |
| Parse lỗi | Raw immutable + diagnostics | Sửa parser/profile, reprocess |
| Thiếu header | Batch quarantined | Steward xác nhận mapping/profile |
| Validation service lỗi | Pipeline run failed, staging giữ nguyên | Retry stage, không upload lại |
| Review sai | Decision append-only | Reopen/reverse bằng quyết định mới |
| Commit conflict | Transaction rollback | Refresh core snapshot, revalidate |
| SQL outage | Raw và queue giữ nguyên | Backoff, circuit breaker, resume |
| Worker chết giữa commit | DB transaction/outbox bảo vệ | Lease timeout và idempotent retry |
| File độc hại | Quarantine cô lập | Security workflow, không parse |

## 17. Mô hình dữ liệu khái niệm

- `SourceDefinition`, `ConnectorProfile`, `AgentRegistration`
- `IngestionReceipt`, `RawObject`, `Batch`, `PipelineRun`
- `HeaderDetectionResult`, `CanonicalRecord`, `FieldValue`
- `MappingSet`, `MappingEntry`, `RuleSet`, `ValidationResult`
- `ReviewCase`, `ReviewDecision`, `Approval`
- `CommitRun`, `CommitRecord`, `DataLineage`, `OutboxMessage`
- `AgentHeartbeat`, `PipelineMetric`, `OperationalAlert`

## 18. Security

- Source/Agent identity riêng, least privilege theo source/module.
- Encryption in transit và at rest; checksum không thay encryption.
- Path canonicalization, extension/signature/size limits và malware hook.
- Formula/macro không được thực thi khi đọc Excel.
- CSV/Excel export chống formula injection.
- PII classification, masking và retention.
- Review/commit/export là privileged operations có step-up hoặc approval nếu cần.

## 19. Capacity và retention

- Xác lập baseline: file size, rows/batch, batches/day, peak concurrency.
- Streaming và bounded preview; không tải toàn bộ file/core table vào memory.
- Raw retention theo compliance; staging ngắn hơn; audit dài hơn.
- Partition/archive theo received date/source nếu volume yêu cầu.
- Backpressure tại gateway và agent; không nhận vô hạn khi downstream quá tải.

## 20. Lộ trình triển khai Data Hub

1. Củng cố pipeline Master Plan hiện có và source registry.
2. Tách raw store, receipt và versioned pipeline run.
3. Xây mapping/rule catalogue và review workflow thật.
4. Thêm commit idempotency, lineage, outbox và monitoring.
5. Phát hành Windows Agent pilot.
6. Mở connector SDK/API và onboarding nguồn thứ hai.
7. Tối ưu scale/retention dựa trên metrics thực tế.

## 21. Tiêu chí nghiệm thu nền tảng

- Replay không tạo duplicate effect.
- Tắt mạng Agent/server không làm mất file đã phát hiện.
- Mọi core record truy ngược được đến raw row và decision.
- Mapping/rule change không làm mất khả năng tái lập batch cũ.
- Review resolution luôn revalidate trước commit.
- Có dashboard, alert, runbook và recovery exercise cho các failure mode chính.

