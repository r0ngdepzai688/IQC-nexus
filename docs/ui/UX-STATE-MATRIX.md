# UX State Matrix

| Screen | Loading | Empty | Error | Forbidden | Success |
|---|---|---|---|---|---|
| Login | Disabled submit with status | N/A | Invalid, disabled, service unavailable | N/A | Redirect to dashboard |
| Dashboard | Skeleton required with API | Zero-value metrics | Retry panel required | Unauthorized route | Operational snapshot |
| Import Center | Table loading required with API | Filter-aware empty state | Retry panel required | Unauthorized route | Searchable job list |
| New Import | Inspection/upload progress | No file selected | File, mapping, validation errors | Unauthorized route | Commit confirmation/result |
| Import Detail | Lifecycle loading required | Unknown job → not found | Stable error code | Unauthorized route | Job timeline |
| Downloads | Catalogue loading required | No published artifacts | Retry panel required | Permission-aware action | Download begins |
| Audit | Activity loading required | No matching events | Retry panel required | Unauthorized route | Filtered activity |

Blocking validation errors always disable commit. Commit requires an explicit confirmation dialog once the server workflow is connected. NASCA never exposes a browser upload control and always explains that the Client Agent submits normalized payloads only.
