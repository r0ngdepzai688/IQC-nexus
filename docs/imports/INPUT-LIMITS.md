# Import Platform Input & Security Limits Specification

## 1. Physical & Structural Limits

| Dimension | Default Limit | Error Code | Safe Rejection Behavior |
| :--- | :--- | :--- | :--- |
| Payload Size | 50 MB | `FILE_TOO_LARGE` | HTTP 400 Bad Request before parsing |
| Worksheets per Workbook | 10 | `WORKSHEET_LIMIT_EXCEEDED` | Normalization aborted safely |
| Rows per Worksheet | 100,000 | `ROW_LIMIT_EXCEEDED` | Normalization aborted safely |
| Columns per Worksheet | 500 | `COLUMN_LIMIT_EXCEEDED` | Normalization aborted safely |
| Total Cells per Workbook | 500,000 | `CELL_LIMIT_EXCEEDED` | Normalization aborted safely |
| Line Length | 64,000 chars | `NORMALIZATION_FAILED` | Safe UTF-8 parse error |

---

## 2. Parser Abuse Rejection Rules

1. **Macro Workbooks**: Rejects `.xlsm`, `.xltm`, `.xlam`, or workbooks with `xl/vbaProject.bin`.
2. **CSV Formula Injection**: Values starting with `=`, `+`, `-`, or `@` are automatically sanitized by prefixing a single quote `'`.
3. **NASCA Workbooks**: Rejects `.nasca` extensions and NASCA keywords.
4. **UTF-8 Encoding**: Rejects malformed byte sequences (`DecoderFallbackException`).
