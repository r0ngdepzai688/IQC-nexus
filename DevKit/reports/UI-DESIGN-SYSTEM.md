# IQC Nexus UI Design System Report

## Design Philosophy
IQC Nexus is designed as a modern enterprise quality intelligence platform. The design system prioritizes a minimal, executive, calm, and intelligent visual atmosphere.

## Core Design Tokens (`globals.css`)
- **Canvas & Surface**: `#f3f4f2` (Light canvas), `#ffffff` (Card surface), `#050505` (Dark canvas).
- **Typography**: Inter / system sans-serif stack. Clean weight hierarchy over heavy bolding.
- **Accent Color**: Deep industrial teal `#16645a` (Primary action & active navigation indicator).
- **Status Indicators**:
  - Success: `#2e6b43` (Green badge/dot)
  - Warning: `#8a5a14` (Amber badge/dot)
  - Danger: `#a33a32` (Red badge/dot)
- **Radii**: `var(--nexus-radius)` (6px) for inputs, buttons, and cards.
- **Borders & Shadows**: Subtle 1px borders (`#d5d9d4`) with minimal elevation shadows.

## Usability & Accessibility Standards
- **Personnel & PIC Handling**: Interactive `UserBadge` component used for all personnel fields.
- **Keyboard Access**: Focus rings visible on all interactive elements (`button:focus-visible`, `a:focus-visible`, `input:focus-visible`).
- **Motion Controls**: Reduced motion preferences (`prefers-reduced-motion: reduce`) respected globally.
- **Bilingual Context**: Built-in support for English and Vietnamese interface text.
