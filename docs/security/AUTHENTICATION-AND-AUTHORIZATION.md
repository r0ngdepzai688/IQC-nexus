# Authentication and Authorization

IQC Nexus uses the existing ASP.NET Core JWT bearer and EF Core user model. It
does not introduce a parallel identity store.

## Authentication

- `POST /api/auth/login` is anonymous and rate limited to five requests per
  minute per limiter partition. Invalid, unknown, malformed-hash, inactive and
  disabled accounts receive the same `401` response.
- Passwords are verified with BCrypt. Passwords and bearer tokens must never be
  logged or written to audit values.
- `GET /api/auth/me` returns the currently authenticated, active user.
- `POST /api/auth/logout` records the event and tells the client to discard its
  bearer token. Tokens are stateless and remain cryptographically valid until
  their short expiry; this is not server-side revocation.
- `POST /api/auth/change-password` is bound to the authenticated token subject,
  requires the current password, and enforces a 12-character minimum.
- Every validated token is checked against current user active/account status,
  so disabling an account takes effect on its next request.
- Login success, login failure, logout and password change are audited without
  credential or token material.

JWT signing secrets must come from environment variables or user-secrets outside
Development and Testing. Synthetic user seeding is restricted to Development or
Testing and requires an externally supplied `IQC_SYNTHETIC_USER_SEED_PASSWORD`.
There is no production default credential.

## Authorization

Authenticated access is the fallback policy. Public endpoints must opt out with
`[AllowAnonymous]`; currently this is limited to login and health.

Permission policies use the existing `Role.Permissions` value. JSON string arrays
are preferred; comma/semicolon-delimited legacy values remain supported.
`Administrator` is the explicit all-permissions system role. Disabled users
cannot satisfy a permission.

Permissions:

- `dashboard.view`
- `import.view`, `import.create`, `import.review`, `import.commit`, `import.admin`
- `download.view`, `download.manage`
- `user.manage`, `role.manage`
- `audit.view`

Controllers and services must enforce permissions on the backend even when the
UI hides an action. Resource ownership is an additional rule: import jobs are
bound to the immutable JWT `sub` user ID. Administrative override requires
`import.admin`. A missing/invalid token produces `401`; an authenticated user
without permission produces `403`.

## Operational limitations

The initial logout endpoint cannot revoke an individual stateless JWT. Keep token
lifetimes short and add a persisted token-version or revocation store before
supporting long-lived sessions. Role permission edits take effect immediately
because authorization reads current persistence rather than trusting permission
claims embedded in the token.
