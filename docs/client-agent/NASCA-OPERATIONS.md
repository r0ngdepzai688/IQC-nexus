# IQC Nexus Client Agent — NASCA Operations Runbook (Phase 3A.2 Revision)

## Operational Decision: STOP_INCOMPLETE_EVIDENCE

The Client Agent runtime will NOT execute NASCA binaries until all 17 criteria in [NASCA-RUNTIME-READINESS.md](file:///D:/Code_viber/Portal/docs/client-agent/NASCA-RUNTIME-READINESS.md) have status `Pass`.

## Evidence Intake Protocol

To register evidence for future integration phases:
1. Obtain official vendor integration manual or vendor-signed binary metadata.
2. Ingest evidence metadata into [NASCA-EVIDENCE-MANIFEST.md](file:///D:/Code_viber/Portal/docs/client-agent/NASCA-EVIDENCE-MANIFEST.md).
3. Re-evaluate readiness using `NascaReadinessEvaluator`.
4. Ensure `NascaOptions.Enabled` is set to `true` ONLY after decision evaluates to `GO_CLI` or `GO_WATCHED_FOLDER`.
