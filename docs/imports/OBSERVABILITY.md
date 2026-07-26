# Import Observability Specification: Structured Logging, Metrics & Health Checks

## 1. Structured Metrics (`ImportMetrics`)

* **Meter Name**: `IqcQms.ImportPlatform`
* **Counters**:
  * `imports_created_total`
  * `imports_completed_total`
  * `imports_failed_total`
  * `commit_requests_total`
  * `commit_success_total`
  * `commit_failure_total`
  * `commit_replay_total`
  * `commit_rollback_total`
  * `concurrency_conflicts_total`
  * `background_retries_total`
  * `poison_work_items_total`
  * `outbox_dispatch_total`
  * `outbox_failure_total`
* **Histograms**:
  * `mapping_duration_ms`
  * `validation_duration_ms`
  * `preview_duration_ms`
  * `commit_duration_ms`
  * `imported_record_count`

---

## 2. Metric Label Policy

To prevent high-cardinality label explosion, the following are **strictly excluded** from metric labels:
* Job IDs (`jobId`)
* User IDs (`userId` / `actor`)
* Filenames (`fileName`)
* Raw cell values or error trace messages

Allowed labels: `provider` (Csv/Excel), `state`, `code` (bounded enum/stable error string).
