# IQC Nexus Design System

## Character

IQC Nexus is an internal precision-manufacturing quality system. Its interface prioritizes traceability, operational density, and explicit state over decoration. The dark graphite navigation rail recalls controlled equipment surfaces; neutral work areas keep dense tables readable; the single teal accent identifies actions and focus.

## Tokens

Tokens live in `frontend/src/app/globals.css` under the `--nexus-*` namespace.

- Canvas, surface, subtle surface, ink, muted text, and border are neutral semantic colors.
- Accent is reserved for primary action, active navigation, focus, and structural emphasis.
- Success, warning, and danger always appear with text or icons; color is never the only signal.
- Radius is 6px. Operational panels use thin borders and at most a one-pixel shadow.
- Spacing uses a 4px base scale.
- Motion is brief and functional. `prefers-reduced-motion` reduces all transitions.

## Layout

The authenticated shell consists of a collapsible primary navigation rail, compact header, environment/version marker, page context, notification entry, and profile menu. Mobile and tablet layouts use an explicit navigation drawer rather than hover.

Pages use:

1. eyebrow, title, description, and actions;
2. an optional clearly labeled development-fixture notice;
3. metrics or filters;
4. bordered operational panels and data tables.

## Data and state

Tables use sticky headers when reviewing normalized rows. Source coordinates remain visible. Lifecycle and severity use consistent text labels. Every API-backed screen must implement loading, empty, error, forbidden, and ready states before release.

Development data must include the words `Development fixture` and use synthetic identifiers. It must never resemble live company evidence without that label.

## Accessibility

Interactive controls use native elements, visible focus rings, associated labels, and meaningful names. Dialogs must use the shared accessible dialog primitive. Navigation supports keyboard and touch. Personnel use the clickable `UserBadge` component. Critical flows require keyboard and screen-reader review at desktop and tablet widths.

## Prohibited patterns

Do not introduce decorative gradients, blobs, glass panels, sparkle icons, fabricated intelligence, fake confidence, unsupported SSO buttons, oversized marketing sections, or arbitrary hardcoded feature colors.
