# Quality Gate: Import Mapping Engine

**Target Subsystem:** `IqcQms.Application.DataPlatform.WorkbookMappingService`

---

## Mandatory Criteria

* [x] **Provider-Neutrality**: Operates on `NormalizedWorkbook` only. No CSV/XLSX provider types in signature.
* [x] **Source Coordinate Preservation**: Every `MappedField` contains `SourceCoordinate` (`WorksheetIndex`, `WorksheetName`, `RowNumber`, `ColumnNumber`).
* [x] **Original Value Retention**: `OriginalNormalizedValue` preserved alongside `MappedValue`.
* [x] **Duplicate Target Validation**: Rejects mapping multiple source columns to the same non-repeatable target field with stable diagnostic code `MAPPING_DUPLICATE_TARGET_FIELD`.
* [x] **Safe Transformations**: Pure, deterministic parsing for text, integers, decimals, booleans, dates, and dictionary lookups with explicit culture.
* [x] **No Silent Coercion**: Rejects silent conversions between numeric and date types without explicit format configuration.
* [x] **Cancellation Support**: Propagates `CancellationToken` throughout execution loop.
