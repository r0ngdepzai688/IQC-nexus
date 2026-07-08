# 02. Project Stages

This document details the business requirements for every phase of the New Models project lifecycle.

## PV (Pilot Validation)

### 1. Receive all New Model Plans
- **Receive model list from HQ:** Synchronize list of new models scheduled for introduction.
- **Receive launch schedule:** Capture key milestone dates (PV, PR, SR, MP).
- **Receive BOM:** Import Bill of Materials.
- **Receive Vendor List:** Import approved vendor list for the model.
- **Receive Risk Information:** Import preliminary risks identified by HQ/R&D.

### 2. DFx (Design for Excellence)
- **Collect DFx:** Gather DFx documents from suppliers and R&D.
- **Collect Risk Factors:** Identify potential manufacturing and quality risks.
- **Send HQ:** Forward collected data to HQ for review.
- **Track Reply Status:**
  - `Pending`: Waiting for HQ review.
  - `Reviewed`: HQ has reviewed but action required.
  - `Completed`: HQ has approved.

### 3. BOM Analysis
- **Download BOM:** Extract from PLM/ERP system.
- **Filter Direct Materials:** Isolate materials relevant to IQC.
- **Generate IQC Material List:** Create actionable list of materials requiring inspection.
- **Supplier Mapping:** Link materials to their respective vendors.
- **Category Mapping:** Classify materials (e.g., Mechanical, Electrical, Packaging).

### 4. Grade A Material
- **Download Grade A List:** Obtain list of critical components.
- **Compare with BOM:** Cross-reference Grade A items with the current BOM.
- **Determine purchased materials:** Finalize list of items to be purchased and inspected.

### 5. Request Vendor Capability
Collect and validate the following metrics from vendors:
- `CPK` (Process Capability Index)
- `Capability`
- `Yield`
- `Performance`

### 6. Approval Tracking
Monitor the completion of required documentation:
- Track Code Approval
- Track Inspection Spec (Standard)
- Track ISS Creation
- Track Drawing Release
- Track Material Approval

### 7. Golden Sample
Manage physical references used for inspection:
- **Types tracked:** Golden Sample, Limit Sample, Color Sample.
- **Attributes:** Storage Location, Approval Status.

### 8. Vendor Verification
- **Collect Vendor Data:** Gather audit and performance history.
- **Random Verification:** Perform spot checks on vendor claims.
- **Capability Review:** Assess if vendor meets mass production requirements.
- **Gap Analysis:** Identify discrepancies between requirements and current capability.
- **Issue List:** Document and track non-conformances.

---

## PR (Production Readiness)
- **Track incoming lots:** Monitor first deliveries of materials.
- **Track IQC Result:** Record pass/fail rates and defects.
- **Evaluate Grade A:** Perform deep-dive evaluation on critical materials.
- **Collect MPPR data:** Gather Mass Production Pilot Run results.
- **Prepare Executive Report:**
  - Risk Summary
  - Final Readiness (Go/No-Go for SR)

---

## SR (System Readiness)
- **Track Mass Production Readiness:** Final checks on systems, lines, and documentation.
- **Supplier Issues:** Consolidate and resolve any lingering vendor problems.
- **Open Issues:** Close out all PR phase action items.
- **Remaining Risks:** Document accepted risks and mitigation plans.

---

## MP (Mass Production)
- **Follow KPI:** Monitor real-time performance metrics.
- **Yield:** Track manufacturing yield rates.
- **Issue Trend:** Identify recurring defects or quality drops.
- **Supplier Performance:** Ongoing vendor evaluation based on production data.
- **Mass Production Health:** Overall systemic health check.
