# IQC Nexus Client Agent — Input Path Security & Boundary Enforcement

## Path Validation Rules

1. **Absolute Path Verification**: All input paths must be fully qualified absolute paths. Relative paths are rejected (`RelativePathNotAllowed`).
2. **Device Namespace Rejection**: Paths starting with `\\.\` or `\\?\` are rejected (`DeviceNamespaceNotAllowed`).
3. **Alternate Data Stream Rejection**: File paths containing colons after root drive letter (e.g. `C:\file.xlsx:stream`) are rejected (`AlternateDataStreamNotAllowed`).
4. **Boundary Resolution & Reparse Point Handling**:
   - `AllowedInputPathValidator` uses `IFileSystemResolver` to resolve symbolic links and junctions component-by-component.
   - Target path must resolve inside one of the configured `AllowedInputRoots`.
   - Traversal escaping `AllowedInputRoots` is rejected (`OutsideAllowedRoots`).
   - Reparse depth exceeding 10 link hops is rejected (`ReparseDepthExceeded`).
   - Broken links or missing target directories are rejected (`BrokenLinkOrJunction`).
5. **Processing-Time Re-validation**: Candidate paths are re-validated at execution time before data provider invocation. If path target changes after enqueue, job is rejected cleanly.
