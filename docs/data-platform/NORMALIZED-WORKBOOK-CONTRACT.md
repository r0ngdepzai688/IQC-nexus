# Normalized Workbook Contract

`NormalizedWorkbook` protocol `1.0` is the provider-neutral boundary between source extraction and IQC business mapping. Unsupported versions must fail with `IMPORT_PROTOCOL_UNSUPPORTED`; consumers must not guess compatibility.

The envelope contains `protocolVersion`, `sourceKind`, a safe `sourceDisplayName`, worksheets, diagnostics, and allowlisted metadata. A worksheet retains its zero-based index, name, visibility, dimensions, merged A1 ranges, and rows. Rows and cells retain one-based source coordinates. Cells retain raw type, mutually appropriate string/numeric/boolean values, cached value, number format, merge address, and anchor status.

Extraction rules:

- Providers extract structure; they do not select headers or attach business meaning.
- Numeric Excel values remain numeric. A date-formatted serial is still numeric; mapping or validation interprets it.
- Empty rows and cells are retained inside the reader-reported used range.
- Worksheet visibility and merged ranges are retained.
- Formula text is populated only when the safe reader supports it. Cached values may be retained with a diagnostic when formula text is unavailable.
- Cell values must never appear in diagnostic logs. Coordinates and stable codes are sufficient.
- Payload, worksheet, row, column, and total-cell limits are enforced during normalization.

The contract never contains original file bytes. In particular, NASCA input is decrypted and extracted only by a future Windows Client Agent; the server accepts only this normalized payload after a paired, user-bound session.
