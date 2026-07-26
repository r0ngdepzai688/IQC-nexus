# Provider-Neutral Mapping Contract

## Architecture Overview

The Mapping layer translates a `NormalizedWorkbook` into a structured, typed `MappingResult`.

```
NormalizedWorkbook -> WorkbookMappingService -> MappingResult
```

## Key Entities & Value Objects

* **`MappingProfile`**: Identifier, version, target worksheet, header row number, case-sensitivity flag, mapping rules, and ignored source columns.
* **`MappingRule`**: `SourceColumnIdentifier`, `TargetField`, `IsRequired`, `TransformationType`, `DefaultValue`, `CultureName`.
* **`MappedField`**: `TargetField`, `SourceColumnName`, `Coordinate` (`WorksheetIndex`, `WorksheetName`, `RowNumber`, `ColumnNumber`), `OriginalNormalizedValue`, `MappedValue`, `MappedType`, `HasTransformationError`.
* **`MappingResult`**: Contains `MappedRecord` list, `MappingDiagnostic` list, unmapped source columns, and ignored source columns.

## Rules & Invariants

1. **Pure Input**: Accepts `NormalizedWorkbook` only.
2. **Coordinate Preservation**: Every cell mapped retains its exact row and column coordinate.
3. **No Silent Coercion**: Date values are not silently cast to numbers; numeric values are not silently cast to dates.
4. **Duplicate Target Rejection**: Mapping multiple headers to the same non-repeatable target field returns error diagnostic `MAPPING_DUPLICATE_TARGET_FIELD`.
