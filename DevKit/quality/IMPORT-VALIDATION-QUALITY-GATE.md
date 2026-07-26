# Quality Gate: Import Validation Engine

**Target Subsystem:** `IqcQms.Application.DataPlatform.ImportValidationEngine`

---

## Mandatory Criteria

* [x] **Provider-Neutrality**: Operates on `MappingResult` domain structures.
* [x] **Severities Enforced**: Information, Warning, Error, BlockingError accurately categorized.
* [x] **Safe Regex Execution**: Timeouts enforced (`RegexTimeoutMs`, default 100ms) with `VAL_REGEX_TIMEOUT` handling.
* [x] **Unique Constraints**: Pre-calculates field value frequencies across records and flags duplicates.
* [x] **Non-Mutating Execution**: Input `MappingResult` is strictly immutable during validation.
* [x] **Diagnostic Cap**: Suppresses further diagnostics beyond `MaximumDiagnostics` to guarantee memory safety.
* [x] **Deterministic Output**: Rerunning identical input and profile yields identical summaries and diagnostic order.
