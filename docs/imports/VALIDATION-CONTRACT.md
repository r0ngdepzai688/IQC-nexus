# Provider-Neutral Validation Contract

## Overview

The Validation Engine evaluates configured business and structural rules against mapped records.

```
MappingResult -> ImportValidationEngine -> ValidationResult
```

## Validation Severities

* **`Information`**: Informational diagnostics (no action required).
* **`Warning`**: Non-blocking recommendation.
* **`Error`**: Record-level or field-level validation failure.
* **`BlockingError`**: Critical failure preventing review progression.

## Built-In Rule Kinds

* **`RequiredValue`**: Checks non-null and non-empty string.
* **`TextLength`**: Checks `MinLength` and `MaxLength`.
* **`NumericRange`**: Checks `MinNumeric` and `MaxNumeric`.
* **`DateRange`**: Checks `MinDate` and `MaxDate`.
* **`AllowedValues`**: Enforces strict value whitelist.
* **`RegexPattern`**: Enforces regular expression with safe `RegexTimeoutMs` (default 100ms).
* **`UniqueValue`**: Pre-calculates field values to detect duplicate occurrences across the import payload.
* **`CrossFieldComparison`**: Compares two fields in the same record (`<=`, `>=`, `<`, `>`, `==`, `!=`).

## Safety Invariants

1. **Non-Mutating**: `MappingResult` input is never altered.
2. **Diagnostic Cap**: Suppresses output after `MaximumDiagnostics` limit.
3. **Deterministic Rerun**: Execution is pure and reproducible.
