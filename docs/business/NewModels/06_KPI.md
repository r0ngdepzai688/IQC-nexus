# 06. KPI Layer

The system continuously evaluates the health of New Models projects based on the following Key Performance Indicators.

## Tracked Metrics

1. **Project Progress:** Overall completion percentage vs. timeline.
2. **PV Completion:** % of PV tasks completed.
3. **PR Completion:** % of PR tasks completed.
4. **Risk Count:** Number of Open Risks (Total and Critical).
5. **Pending DFx:** Count of DFx documents awaiting HQ review.
6. **Supplier Capability:** % of vendors meeting target CPK/Yield.
7. **Golden Sample Completion:** % of required Golden Samples approved and stored.
8. **ISS Completion:** % of materials with a released Inspection Spec.
9. **Vendor Approval:** % of BOM vendors fully approved.
10. **IQC Readiness:** Aggregate score of ISS, Golden Samples, and trained staff.
11. **MP Readiness:** Final Go/No-Go metric for Mass Production.
12. **Overall Score:** Composite health score of the New Model launch.

## Traffic Light System

All KPIs utilize a standard RAG (Red, Amber, Green) traffic light logic:

- **Green (Healthy):** Metric is on track, >= 95% of target, or 0 Critical Risks.
- **Yellow (Warning):** Metric is slightly behind, 80-94% of target, or 1-2 High Risks. Requires monitoring and action plan.
- **Red (Critical):** Metric is significantly behind, < 80% of target, or >0 Critical Risks. Requires immediate escalation.
