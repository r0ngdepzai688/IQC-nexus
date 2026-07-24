# IQC Nexus Client Agent — Device Pairing Protocol

## Overview

Device pairing establishes a trusted relationship between an installed Windows Client Agent and an IQC Nexus user account without requiring static credentials or passwords stored on the client machine.

## Conceptual Protocol Flow

1. **Code Creation**: An authorized Portal user generates a short-lived 6-digit pairing code via `POST /api/agent-pairing-requests`.
2. **Display**: Portal displays code ONCE with a 10-minute expiry countdown.
3. **Storage**: Server stores SHA256/BCrypt hash of the pairing code (`HashedCode`).
4. **Agent Submission**: User inputs code into Agent config (`--AgentOptions:PairingCode=XXXXXX`). Agent calls `POST /api/agent-devices/pair`.
5. **Validation**: Server verifies code using constant-time comparison, marks code consumed, and registers the device.
6. **Token Issuance**: Server returns short-lived Access Token (15 min) and Refresh Token (7 days).
7. **Client Protection**: Agent encrypts received tokens using DPAPI and stores them locally.
