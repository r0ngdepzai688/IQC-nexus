# Data Source Provider Model

Providers implement `IDataSourceProvider`; selection is centralized in `IDataSourceProviderRegistry`. `IWorkbookNormalizer` is the application-facing facade. Provider kinds are `Csv`, `Excel`, `ClientAgentExcel`, `NascaExcel`, `Api`, and `Clipboard`.

Only CSV and ordinary XLSX are local server providers in the foundation. CSV supports strict UTF-8 with or without BOM, RFC-style quoting, embedded newlines, escaped quotes, empty cells, duplicate headers, and safe delimiter detection. XLSX uses ExcelDataReader without Office automation and retains multiple worksheets, visibility, merged ranges, values, coordinates, and number formats.

`ClientAgentExcel` and `NascaExcel` are contract identifiers, not server file handlers. They deliberately have no registered provider. Raw NASCA bytes, local client paths, permanent Agent tokens, Excel COM, and Office automation are outside the server trust boundary.

Capabilities are explicit flags so UI/API composition can advertise facts without inferring them from extensions. Providers receive bounded streams and propagate cancellation. Failures cross the boundary as `ImportPlatformException` with stable public codes and sanitized messages.
