# Test Matrix

| Area | Unit | Integration | UI/E2E |
| --- | --- | --- | --- |
| CSV normalization | quoting, BOM, delimiter, empty/duplicate cells, limits | upload and normalize | provider selection and progress |
| XLSX normalization | sheets, visibility, merge, formats, numeric, limits | upload and normalize | file selection and capability copy |
| Lifecycle | transition table and preconditions | owner, preview, commit, replay, rollback, cancellation | mapping, filters, commit confirmation |
| Authentication | permission evaluation | login, disabled, me, logout, 401/403, expiry, log secrecy | keyboard/errors/route guard |
| Audit | sanitized event shape | auth and import transitions | activity states and filters |
| Downloads | permission model | safe metadata endpoint when added | loading/empty/error/list |
| Security boundary | protocol and provider rejection | no raw NASCA route | Client Agent-only messaging |
| Accessibility | component semantics | — | keyboard, focus, responsive navigation |

All data files used by tests must be generated synthetically within isolated
temporary directories. Tests must not read repository-local business artifacts
or normal runtime import storage.

