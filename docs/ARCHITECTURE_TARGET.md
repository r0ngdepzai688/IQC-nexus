# IQC Nexus Target Architecture

## 1. Tóm tắt kiến trúc đích

IQC Nexus hướng tới một **modular monolith triển khai on-premises**, gồm Next.js web application, ASP.NET Core API/BFF, background workers, SQL Server và một Windows Agent ở biên. Data Hub là module nền tảng dùng chung; giao tiếp nội bộ ưu tiên application interfaces và transactional outbox, không dùng distributed messaging nếu chưa có nhu cầu thực tế.

```text
Users / Browser
      |
  HTTPS + OIDC
      v
Reverse Proxy / IIS
      |
      +--> Next.js Web / BFF
      |          |
      |          +--> ASP.NET Core API v1
      |                       |
      |                       +--> Application Modules
      |                       |     Auth, Users, New Models,
      |                       |     Tasks, Chat, Standards, Data Hub
      |                       |
      |                       +--> Background Workers / SignalR
      |                       +--> SQL Server
      |                       +--> Managed File/Object Storage
      |
Windows Agent(s) --mTLS/HTTPS--> Data Hub ingestion endpoints

All components --> OpenTelemetry --> Logs / Metrics / Traces / Alerts
```

## 2. Kiến trúc frontend

### Cấu trúc

- Next.js App Router, TypeScript strict.
- Feature folders: `features/auth`, `features/data-hub`, `features/new-models`, `features/tasks`, `features/chat`.
- `app/` chỉ chịu trách nhiệm routing, composition, metadata và error/loading boundaries.
- Shared layers: `design-system`, `api-client`, `auth`, `i18n`, `observability`, `config`.
- Server Components là mặc định cho read-heavy page; Client Components chỉ cho interaction/state cục bộ.
- Một generated API client từ OpenAPI, có correlation ID, Problem Details và cancellation.
- Server state dùng query cache có quy ước; không lưu bản sao dữ liệu nghiệp vụ dài hạn trong Context.

### State

- Authentication/session: server/BFF session, không tin dữ liệu role từ browser storage.
- Server state: query cache và invalidation theo resource.
- UI state ngắn hạn: local component hoặc scoped context.
- Workflow draft dài hạn: API draft resource hoặc autosave, không chỉ memory.

### Bảo vệ frontend

- CSP nghiêm ngặt, security headers, output encoding và dependency scanning.
- Route guard chỉ để UX; API vẫn là nơi quyết định quyền.
- Không dùng token trong localStorage.
- Feature flags lấy từ server và có audit cho thay đổi production.

## 3. Kiến trúc backend

### Thành phần

- ASP.NET Core 8 LTS hoặc phiên bản LTS được phê duyệt tại thời điểm triển khai.
- API host, worker host và Windows Agent dùng chung application contracts nhưng triển khai độc lập.
- Các module: Identity Access, Workforce, New Models, Tasks, Chat, Standards, Data Hub, Audit, Notifications.

### Ranh giới layer

```text
API/Worker Host
  -> Application (commands, queries, policies, DTOs)
      -> Domain (entities, value objects, invariants, events)
      -> Ports (repositories, clock, file store, identity)
          <- Infrastructure (EF Core, SQL Server, filesystem, OIDC, telemetry)
```

- Controller mỏng: binding, authorization, gọi use case, map response.
- Domain không phụ thuộc EF/API.
- Không trả EF entities trực tiếp.
- Mỗi module sở hữu schema/table prefix và application service của nó.
- Architecture tests ngăn tham chiếu ngược hoặc truy cập chéo DbSet tùy tiện.

### Giao tiếp module

- Đồng bộ: application interface/query contract rõ ràng.
- Bất đồng bộ tin cậy: domain event → transactional outbox → background dispatcher.
- Không gọi HTTP nội bộ trong cùng deployment.
- Event schema được version, consumer idempotent.

## 4. SQL Server

### Thiết kế

- Một database ban đầu, schema logic theo module: `iam`, `newmodels`, `tasks`, `chat`, `datahub`, `audit`.
- Business key có unique constraint; FK được khai báo thay vì chỉ lưu ID/string.
- `rowversion` cho aggregate có cập nhật đồng thời.
- UTC cho timestamp; timezone chỉ áp dụng ở presentation.
- Soft delete chỉ dùng khi có yêu cầu phục hồi; không thay audit.
- Temporal tables hoặc audit history dành cho dữ liệu cần truy vết lịch sử, sau đánh giá chi phí.

### Vận hành

- EF migrations tạo script idempotent và được DBA review.
- App không tự migrate database khi startup production.
- Least-privilege accounts tách migration/runtime/read-only.
- Encryption in transit; TDE/backup encryption theo chính sách công ty.
- Full/differential/log backup phù hợp RPO; restore test định kỳ.

## 5. Xác thực

- Ưu tiên OIDC với Entra ID/AD FS/identity provider doanh nghiệp.
- Authorization Code + PKCE; frontend sử dụng BFF session cookie HttpOnly, Secure, SameSite.
- Service-to-service/Agent dùng client credential hoặc certificate-bound identity.
- Session có absolute/idle expiry, revoke và audit.
- Local account chỉ dành cho break-glass, quản lý trong vault, MFA và cảnh báo khi sử dụng.

## 6. Phân quyền

Mô hình kết hợp RBAC và data-scope ABAC:

- **System roles:** Administrator, Security Administrator, Auditor, Data Steward, Standard User.
- **Business roles:** Team Leader, Group Leader, Part Leader, Cell Leader, Staff.
- **Permissions:** hành động nguyên tử như `datahub.batch.commit`, `task.approve`, `user.read`.
- **Scope attributes:** organization, part, cell, project, module.

Policy được đánh giá tại backend trên authenticated subject + permission + resource scope. Role chỉ là tập permission; không hard-code chuỗi role rải rác. Permission catalogue có owner và version.

## 7. Data Hub

- Connector framework nhận envelope chuẩn từ upload, API và Windows Agent.
- Immutable raw store + metadata manifest + checksum.
- Pipeline versioned: detect → map → parse → validate → review → commit.
- Staging tách khỏi core; commit idempotent và có lineage.
- Review workflow hỗ trợ assignment, SLA, approval và reprocess.
- Thiết kế chi tiết tại `DATA_HUB_ARCHITECTURE.md`.

## 8. Windows Agent

### Vai trò

- Theo dõi thư mục/file share được cấu hình.
- Chờ file ổn định, tính checksum, tạo manifest và upload an toàn.
- Spool cục bộ khi mất mạng; retry có exponential backoff.
- Không parse hay áp dụng nghiệp vụ lõi để tránh lệch phiên bản giữa agents.

### Bảo mật và vận hành

- Windows Service chạy bằng dedicated service account, least privilege.
- mTLS hoặc OAuth client credential; certificate rotate được.
- Package được code-sign; auto-update theo ring và có rollback.
- Config từ central management, local secret được bảo vệ bằng DPAPI/certificate store.
- Heartbeat, version, queue depth, disk usage và last-success được gửi về server.

## 9. API

- REST JSON `/api/v1`; OpenAPI là contract chính.
- Resource-oriented routes; command phức tạp có action endpoint rõ nhưng nhất quán.
- RFC 7807 Problem Details với `traceId`, code ổn định và field errors.
- Pagination cursor hoặc page/size có limit; filter/sort allowlist.
- Idempotency-Key cho upload/commit/retry-sensitive commands.
- ETag/rowversion cho optimistic concurrency.
- Correlation ID xuyên frontend, API, worker và Agent.
- API deprecation policy tối thiểu hai release; contract tests bảo vệ compatibility.
- Upload qua streaming, size limit, MIME/signature validation và malware scanning hook.

## 10. Logging, monitoring và tracing

### Logging

- Structured JSON logs; không log password, token, file content hoặc PII không cần thiết.
- Trường chuẩn: timestamp, level, service, environment, traceId, user pseudonymous ID, module, operation, outcome, duration.
- Security event và audit business tách khỏi diagnostic log.

### Observability

- OpenTelemetry cho ASP.NET Core, HTTP, EF Core, worker và Agent.
- Metrics: request rate/error/latency, DB pool/query, SignalR connections, batch throughput, queue age, review SLA, agent heartbeat.
- Distributed traces cho upload → validation → commit.
- Dashboard theo service và business workflow; alert dựa trên SLO, không chỉ CPU.

### SLO ban đầu

- Web/API availability: 99,5% tháng.
- P95 read API: < 500 ms ở tải mục tiêu.
- Batch tiếp nhận được xác nhận: < 60 giây sau upload với file trong giới hạn.
- Agent heartbeat freshness: < 5 phút.
- Không mất raw file/batch đã nhận receipt.

## 11. Audit và compliance

- Audit record append-only: actor, action, resource, before/after hoặc change set, reason, timestamp, source IP/device, traceId.
- Tất cả login, role change, data override, review resolution, commit, export và config change được audit.
- Audit query chỉ dành cho Auditor/Admin có scope.
- Retention và legal hold theo chính sách; hash-chain/WORM được cân nhắc nếu yêu cầu chống sửa đổi cao.
- PII masking trong UI/log; export có quyền riêng và watermark khi cần.

## 12. Deployment topology

### Môi trường

- DEV: dữ liệu synthetic, developer self-service.
- TEST/UAT: topology gần production, dữ liệu đã mask.
- PROD: HA theo nhu cầu, backup và monitoring đầy đủ.

### Triển khai on-premises đề xuất

- IIS/reverse proxy terminate TLS.
- Next.js Node process hoặc standalone deployment phía sau proxy.
- ASP.NET Core API và Worker chạy Windows Service/IIS phù hợp.
- SQL Server do DBA quản trị.
- File/object store nằm trên managed storage, không phải local disk của app instance.
- Agent triển khai qua software distribution/Group Policy theo deployment ring.

### CI/CD

- Build một lần, promote cùng artifact qua môi trường.
- SBOM, signature, unit/integration/security scans.
- Database migration là stage riêng có backup và approval.
- Blue-green/canary hoặc rolling với feature flags.
- Automated smoke test và rollback gate sau deploy.

## 13. Availability và disaster recovery

- API stateless; session store dùng shared protected storage nếu nhiều instance.
- Worker dùng lease/queue semantics để tránh xử lý trùng.
- Raw store và SQL backup có retention/replication phù hợp.
- Agent spool bảo vệ khi server outage.
- Runbook cho DB unavailable, storage full, identity outage, stuck batch và certificate expiry.
- DR exercise tối thiểu hai lần/năm.

## 14. Các quyết định cần ADR

1. OIDC provider và BFF session topology.
2. SQL Server edition/HA và schema-per-module.
3. Raw storage: managed file share hay object-compatible storage.
4. Background execution: hosted worker hay enterprise scheduler/queue.
5. Audit immutability và retention.
6. Agent authentication, update distribution và code signing.
7. Query cache/state library frontend.
8. Feature flag platform.

## 15. Quality attributes và fitness functions

| Thuộc tính | Fitness function |
|---|---|
| Security | Endpoint coverage test; secret scan; dependency scan |
| Modularity | Architecture tests không cho dependency trái chiều |
| Reliability | Idempotency/replay tests; chaos test Agent/network |
| Data integrity | FK/unique constraints; reconciliation report |
| Performance | Automated API/import benchmark theo release |
| Operability | Health/readiness, dashboards, alert test và runbook drill |
| Accessibility | axe + keyboard E2E trên critical flows |
| Compatibility | OpenAPI diff và consumer contract tests |

