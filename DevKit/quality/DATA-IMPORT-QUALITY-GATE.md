# Data Import Quality Gate

Release is blocked unless all mandatory checks pass with synthetic data.

## Normalization

- [ ] CSV BOM/no-BOM, quoting, escaped quotes, embedded newlines, empty cells,
  duplicate headers, and delimiter selection are tested.
- [ ] XLSX multi-sheet, hidden-sheet metadata, merged ranges, empty rows,
  formulas/cached values when supported, formats, and limits are tested.
- [ ] Numeric values remain numeric; providers do not interpret dates.
- [ ] Original row and column coordinates are preserved.
- [ ] Protocol versions and unsupported providers fail with stable codes.
- [ ] Providers never log cell values.

## Lifecycle

- [ ] Inspection precedes mapping confirmation.
- [ ] Mapping confirmation precedes validation.
- [ ] Preview is recorded and required before commit.
- [ ] Blocking validation errors prevent commit.
- [ ] Commit is transactional and mixed-operation rollback is verified.
- [ ] Repeated commit is idempotent and cannot duplicate data.
- [ ] Cancellation propagates and returns a stable cancelled result.
- [ ] Jobs are user-bound; explicit `import.admin` is the only override.
- [ ] Important transitions emit sanitized audit events.

## Boundary

- [ ] CSV and ordinary Excel share the normalized pipeline.
- [ ] Business validation contains no provider-specific branching.
- [ ] NASCA and Client Agent providers accept normalized payload contracts only.
- [ ] The server accepts no raw NASCA upload and contains no Office COM.

