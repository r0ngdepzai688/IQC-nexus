# Portal Screen Inventory

| Route | Purpose | Current source |
|---|---|---|
| `/login` | Secure username/password access | Authentication API |
| `/overview` | Operational dashboard | Explicit synthetic fixture pending dashboard API |
| `/imports` | Search and filter import jobs | Explicit synthetic fixture pending import-jobs API |
| `/imports/new` | Provider selection, mapping, validation, preview | Interactive foundation fixture |
| `/imports/[id]` | Import context and lifecycle | Explicit synthetic fixture |
| `/downloads` | Controlled artifact catalogue | Explicit synthetic fixture pending storage API |
| `/audit` | Security and import activity | Explicit synthetic fixture pending audit API |
| `/profile` | Identity and interface preferences | Authenticated user context |
| `/unauthorized` | Authenticated permission denial | Static |
| unmatched route | Not found | Static |

Legacy New Models, HR, Tasks, and Data Hub routes remain during staged migration.
