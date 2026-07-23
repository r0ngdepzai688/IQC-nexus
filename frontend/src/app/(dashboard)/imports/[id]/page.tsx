"use client";

import React, { useCallback, useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { PermissionGate } from "@/components/portal/PermissionGate";
import { EmptyState, ServiceErrorState, SlowNetworkState } from "@/components/portal/states";
import { UserBadge } from "@/components/ui/user-badge";
import { importDataProvider, importRepository } from "@/lib/imports";
import {
  ImportJob,
  ImportPreviewDetail,
  MappingProfileConfig,
  MappingResultSummary,
  ValidationResultSummary,
} from "@/lib/imports/contracts";
import { ArrowLeft, CheckCircle2, AlertTriangle, XCircle, FileText, Settings, ShieldCheck, RefreshCw } from "lucide-react";

export default function ImportJobDetailPage() {
  const params = useParams();
  const id = (params?.id as string) || "";

  return (
    <PermissionGate permission="import.view">
      <ImportDetailContent jobId={id} />
    </PermissionGate>
  );
}

function ImportDetailContent({ jobId }: { jobId: string }) {
  const router = useRouter();
  const [job, setJob] = useState<ImportJob | null>(null);
  const [activeTab, setActiveTab] = useState<"overview" | "mapping" | "validation" | "preview">("overview");
  const [state, setState] = useState<"loading" | "ready" | "error">("loading");

  // Mapping state
  const [headerRowNumber, setHeaderRowNumber] = useState(1);
  const [caseInsensitive, setCaseInsensitive] = useState(true);
  const [targetField1, setTargetField1] = useState("PartNo");
  const [targetField2, setTargetField2] = useState("Quantity");
  const [mappingResult, setMappingResult] = useState<MappingResultSummary | null>(null);
  const [mappingLoading, setMappingLoading] = useState(false);

  // Validation state
  const [validationResult, setValidationResult] = useState<ValidationResultSummary | null>(null);
  const [validationLoading, setValidationLoading] = useState(false);
  const [severityFilter, setSeverityFilter] = useState<string>("All");

  // Preview state
  const [preview, setPreview] = useState<ImportPreviewDetail | null>(null);
  const [previewLoading, setPreviewLoading] = useState(false);

  const loadJob = useCallback(
    (signal?: AbortSignal) => {
      setState("loading");
      importRepository
        .getJob(jobId, signal)
        .then((j) => {
          setJob(j);
          setState("ready");
        })
        .catch((err) => {
          if (err?.name !== "AbortError") setState("error");
        });
    },
    [jobId]
  );

  useEffect(() => {
    const controller = new AbortController();
    loadJob(controller.signal);
    return () => controller.abort();
  }, [loadJob]);

  const handleApplyMapping = async () => {
    setMappingLoading(true);
    try {
      const profile: MappingProfileConfig = {
        headerRowNumber,
        caseInsensitiveHeaderMatching: caseInsensitive,
        rules: [
          { sourceColumnIdentifier: "Part Number", targetField: targetField1, isRequired: true, transformationType: 1 },
          { sourceColumnIdentifier: "Quantity", targetField: targetField2, isRequired: true, transformationType: 3 },
        ],
        ignoredSourceColumns: ["Unmapped Header"],
      };
      const res = await importRepository.applyMapping(jobId, profile);
      setMappingResult(res);
      loadJob();
    } catch (err) {
      alert("Failed to apply mapping profile.");
    } finally {
      setMappingLoading(false);
    }
  };

  const handleRunValidation = async () => {
    setValidationLoading(true);
    try {
      const res = await importRepository.runValidation(jobId, [
        { kind: 0, severity: 2, targetField: targetField1 }, // Required
        { kind: 2, severity: 1, targetField: targetField2, minNumeric: 1 }, // NumericRange
      ]);
      setValidationResult(res);
      loadJob();
    } catch (err) {
      alert("Failed to execute validation engine.");
    } finally {
      setValidationLoading(false);
    }
  };

  const handleGeneratePreview = async () => {
    setPreviewLoading(true);
    try {
      const res = await importRepository.generatePreview(jobId);
      setPreview(res);
      loadJob();
    } catch (err) {
      alert("Failed to generate preview attestation.");
    } finally {
      setPreviewLoading(false);
    }
  };

  if (state === "loading") return <TableSkeleton />;
  if (state === "error" || !job) return <ServiceErrorState retry={() => loadJob()} />;

  return (
    <div className="page-stack" style={{ gap: "24px", padding: "24px 0" }}>
      {/* Header */}
      <section className="page-heading" style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start" }}>
        <div>
          <button
            onClick={() => router.push("/imports")}
            className="secondary-action"
            style={{ marginBottom: "12px", display: "inline-flex", alignItems: "center", gap: "6px", fontSize: "0.875rem" }}
          >
            <ArrowLeft size={16} /> Back to Imports
          </button>
          <p className="eyebrow">IMPORT JOB ORCHESTRATION</p>
          <h2>{job.fileName}</h2>
          <p style={{ color: "var(--muted, #64748b)" }}>Job ID: <code>{job.id}</code> · Provider: {job.source}</p>
        </div>

        <div style={{ textAlign: "right" }}>
          <span className={`status-badge ${job.status === "Failed" ? "danger" : job.status === "Completed" ? "success" : "warning"}`}>
            {job.status}
          </span>
          <p style={{ marginTop: "6px", fontSize: "0.85rem", color: "var(--muted, #64748b)" }}>
            State: <strong>{job.state}</strong>
          </p>
        </div>
      </section>

      {importDataProvider === "fixture" && (
        <div className="fixture-banner" role="note" style={{ background: "#f8fafc", borderLeft: "4px solid #3b82f6", padding: "12px 16px", borderRadius: "8px" }}>
          <strong>Fixture Provider Active</strong>
          <span style={{ marginLeft: "8px" }}>Synthetic orchestration data is simulated via client fixture adapter.</span>
        </div>
      )}

      {/* Tabs */}
      <div style={{ display: "flex", gap: "8px", borderBottom: "1px solid var(--border, #e2e8f0)", paddingBottom: "8px" }}>
        <button
          onClick={() => setActiveTab("overview")}
          className={`tab-btn ${activeTab === "overview" ? "active" : ""}`}
          style={{ padding: "8px 16px", borderRadius: "8px", border: "none", background: activeTab === "overview" ? "#3b82f6" : "transparent", color: activeTab === "overview" ? "#fff" : "inherit", cursor: "pointer", fontWeight: 500 }}
        >
          <FileText size={16} style={{ display: "inline", marginRight: "6px" }} /> Job Overview
        </button>

        <button
          onClick={() => setActiveTab("mapping")}
          className={`tab-btn ${activeTab === "mapping" ? "active" : ""}`}
          style={{ padding: "8px 16px", borderRadius: "8px", border: "none", background: activeTab === "mapping" ? "#3b82f6" : "transparent", color: activeTab === "mapping" ? "#fff" : "inherit", cursor: "pointer", fontWeight: 500 }}
        >
          <Settings size={16} style={{ display: "inline", marginRight: "6px" }} /> Mapping Configuration
        </button>

        <button
          onClick={() => setActiveTab("validation")}
          className={`tab-btn ${activeTab === "validation" ? "active" : ""}`}
          style={{ padding: "8px 16px", borderRadius: "8px", border: "none", background: activeTab === "validation" ? "#3b82f6" : "transparent", color: activeTab === "validation" ? "#fff" : "inherit", cursor: "pointer", fontWeight: 500 }}
        >
          <AlertTriangle size={16} style={{ display: "inline", marginRight: "6px" }} /> Validation Results
        </button>

        <button
          onClick={() => setActiveTab("preview")}
          className={`tab-btn ${activeTab === "preview" ? "active" : ""}`}
          style={{ padding: "8px 16px", borderRadius: "8px", border: "none", background: activeTab === "preview" ? "#3b82f6" : "transparent", color: activeTab === "preview" ? "#fff" : "inherit", cursor: "pointer", fontWeight: 500 }}
        >
          <ShieldCheck size={16} style={{ display: "inline", marginRight: "6px" }} /> Review & Preview
        </button>
      </div>

      {/* Tab 1: Overview */}
      {activeTab === "overview" && (
        <section className="panel" style={{ padding: "24px", background: "var(--card-bg, #ffffff)", borderRadius: "12px", border: "1px solid var(--border, #e2e8f0)" }}>
          <h3 style={{ marginBottom: "16px" }}>Metadata & Lifecycle History</h3>

          <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(240px, 1fr))", gap: "16px", marginBottom: "24px" }}>
            <div style={{ padding: "16px", border: "1px solid #e2e8f0", borderRadius: "8px" }}>
              <span style={{ fontSize: "0.85rem", color: "#64748b" }}>Creator</span>
              <div style={{ marginTop: "4px" }}>
                <UserBadge name={job.creator} size="sm" />
              </div>
            </div>

            <div style={{ padding: "16px", border: "1px solid #e2e8f0", borderRadius: "8px" }}>
              <span style={{ fontSize: "0.85rem", color: "#64748b" }}>Created At</span>
              <div style={{ marginTop: "4px", fontWeight: 600 }}>{new Date(job.createdAt).toLocaleString()}</div>
            </div>

            <div style={{ padding: "16px", border: "1px solid #e2e8f0", borderRadius: "8px" }}>
              <span style={{ fontSize: "0.85rem", color: "#64748b" }}>Validation Summary</span>
              <div style={{ marginTop: "4px", fontWeight: 600 }}>
                {job.errors} Errors · {job.warnings} Warnings
              </div>
            </div>

            <div style={{ padding: "16px", border: "1px solid #e2e8f0", borderRadius: "8px" }}>
              <span style={{ fontSize: "0.85rem", color: "#64748b" }}>Preview Version</span>
              <div style={{ marginTop: "4px", fontWeight: 600 }}>{job.previewVersion || "Not Generated"}</div>
            </div>
          </div>

          <h4>Lifecycle State Progression</h4>
          <ol style={{ display: "flex", gap: "12px", listStyle: "none", padding: 0, marginTop: "12px", flexWrap: "wrap" }}>
            {["Created", "Inspecting", "ReadyForMapping", "Validating", "ReadyForReview"].map((st, idx) => (
              <li
                key={st}
                style={{
                  padding: "8px 14px",
                  borderRadius: "20px",
                  background: job.state === st ? "#3b82f6" : "#f1f5f9",
                  color: job.state === st ? "#fff" : "#475569",
                  fontSize: "0.875rem",
                  fontWeight: 500,
                }}
              >
                {idx + 1}. {st}
              </li>
            ))}
          </ol>
        </section>
      )}

      {/* Tab 2: Mapping Configuration */}
      {activeTab === "mapping" && (
        <section className="panel" style={{ padding: "24px", background: "var(--card-bg, #ffffff)", borderRadius: "12px", border: "1px solid var(--border, #e2e8f0)" }}>
          <h3>Provider-Neutral Mapping Configuration</h3>
          <p style={{ color: "#64748b", marginBottom: "20px" }}>Map source worksheet columns to target schema fields deterministically.</p>

          <div style={{ display: "grid", gap: "16px", maxWidth: "600px", marginBottom: "24px" }}>
            <label style={{ display: "flex", flexDirection: "column", gap: "6px" }}>
              Header Row Number
              <input
                type="number"
                value={headerRowNumber}
                onChange={(e) => setHeaderRowNumber(Number(e.target.value))}
                style={{ padding: "8px 12px", borderRadius: "8px", border: "1px solid #cbd5e1" }}
              />
            </label>

            <label style={{ display: "flex", alignItems: "center", gap: "8px" }}>
              <input
                type="checkbox"
                checked={caseInsensitive}
                onChange={(e) => setCaseInsensitive(e.target.checked)}
              />
              Case-Insensitive Header Matching
            </label>

            <div style={{ border: "1px solid #e2e8f0", padding: "16px", borderRadius: "8px" }}>
              <strong>Column Rules</strong>
              <div style={{ marginTop: "12px", display: "grid", gap: "12px" }}>
                <div>
                  <span style={{ fontSize: "0.85rem", color: "#64748b" }}>Source: "Part Number" (Header 1)</span>
                  <input
                    type="text"
                    value={targetField1}
                    onChange={(e) => setTargetField1(e.target.value)}
                    placeholder="Target Field Name"
                    style={{ display: "block", width: "100%", padding: "8px", marginTop: "4px", borderRadius: "6px", border: "1px solid #cbd5e1" }}
                  />
                  <small style={{ color: "#64748b" }}>Transformation: Trim Text (Required)</small>
                </div>

                <div>
                  <span style={{ fontSize: "0.85rem", color: "#64748b" }}>Source: "Quantity" (Header 2)</span>
                  <input
                    type="text"
                    value={targetField2}
                    onChange={(e) => setTargetField2(e.target.value)}
                    placeholder="Target Field Name"
                    style={{ display: "block", width: "100%", padding: "8px", marginTop: "4px", borderRadius: "6px", border: "1px solid #cbd5e1" }}
                  />
                  <small style={{ color: "#64748b" }}>Transformation: Parse Integer (Required)</small>
                </div>
              </div>
            </div>

            <button
              onClick={handleApplyMapping}
              disabled={mappingLoading}
              className="primary-action"
              style={{ padding: "10px 20px", borderRadius: "8px", background: "#3b82f6", color: "#fff", border: "none", cursor: "pointer", fontWeight: 600 }}
            >
              {mappingLoading ? "Executing Mapping..." : "Apply Mapping Profile"}
            </button>
          </div>

          {mappingResult && (
            <div style={{ padding: "16px", background: "#f0fdf4", border: "1px solid #bbf7d0", borderRadius: "8px" }}>
              <h4 style={{ color: "#166534" }}>Mapping Executed Successfully</h4>
              <p>Total Processed Rows: {mappingResult.totalRowsProcessed}</p>
              <p>Mapped Records: {mappingResult.mappedRecordCount}</p>
              <p>Unmapped Source Columns: {mappingResult.unmappedSourceColumns.join(", ") || "None"}</p>
            </div>
          )}
        </section>
      )}

      {/* Tab 3: Validation Results */}
      {activeTab === "validation" && (
        <section className="panel" style={{ padding: "24px", background: "var(--card-bg, #ffffff)", borderRadius: "12px", border: "1px solid var(--border, #e2e8f0)" }}>
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "16px" }}>
            <h3>Validation Engine Results</h3>
            <button
              onClick={handleRunValidation}
              disabled={validationLoading}
              style={{ padding: "8px 16px", borderRadius: "8px", background: "#3b82f6", color: "#fff", border: "none", cursor: "pointer", fontWeight: 500, display: "inline-flex", alignItems: "center", gap: "6px" }}
            >
              <RefreshCw size={16} /> {validationLoading ? "Running..." : "Execute Validation"}
            </button>
          </div>

          {validationResult ? (
            <div>
              <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(160px, 1fr))", gap: "12px", marginBottom: "20px" }}>
                <div style={{ padding: "12px", background: "#f8fafc", borderRadius: "8px" }}>
                  <span style={{ fontSize: "0.8rem", color: "#64748b" }}>Evaluated Records</span>
                  <div style={{ fontSize: "1.25rem", fontWeight: 700 }}>{validationResult.summary.totalRecordsEvaluated}</div>
                </div>
                <div style={{ padding: "12px", background: "#f0fdf4", borderRadius: "8px" }}>
                  <span style={{ fontSize: "0.8rem", color: "#166534" }}>Valid Records</span>
                  <div style={{ fontSize: "1.25rem", fontWeight: 700, color: "#166534" }}>{validationResult.summary.validRecords}</div>
                </div>
                <div style={{ padding: "12px", background: "#fefce8", borderRadius: "8px" }}>
                  <span style={{ fontSize: "0.8rem", color: "#854d0e" }}>Warnings</span>
                  <div style={{ fontSize: "1.25rem", fontWeight: 700, color: "#854d0e" }}>{validationResult.summary.warningCount}</div>
                </div>
                <div style={{ padding: "12px", background: "#fef2f2", borderRadius: "8px" }}>
                  <span style={{ fontSize: "0.8rem", color: "#991b1b" }}>Errors</span>
                  <div style={{ fontSize: "1.25rem", fontWeight: 700, color: "#991b1b" }}>{validationResult.summary.errorCount}</div>
                </div>
              </div>

              <h4>Diagnostics Sample</h4>
              {validationResult.diagnosticsSample.length ? (
                <table className="data-table" style={{ width: "100%", marginTop: "12px", borderCollapse: "collapse" }}>
                  <thead>
                    <tr style={{ background: "#f8fafc" }}>
                      <th style={{ padding: "10px", textAlign: "left" }}>Rule / Code</th>
                      <th style={{ padding: "10px", textAlign: "left" }}>Severity</th>
                      <th style={{ padding: "10px", textAlign: "left" }}>Target Field</th>
                      <th style={{ padding: "10px", textAlign: "left" }}>Source Coordinate</th>
                      <th style={{ padding: "10px", textAlign: "left" }}>Message</th>
                    </tr>
                  </thead>
                  <tbody>
                    {validationResult.diagnosticsSample.map((d, idx) => (
                      <tr key={idx} style={{ borderBottom: "1px solid #e2e8f0" }}>
                        <td style={{ padding: "10px" }}><code>{d.code}</code></td>
                        <td style={{ padding: "10px" }}>
                          <span className={`status-badge ${d.severity >= 2 ? "danger" : "warning"}`}>
                            {d.severity === 1 ? "Warning" : d.severity === 2 ? "Error" : "Info"}
                          </span>
                        </td>
                        <td style={{ padding: "10px" }}>{d.targetField || "N/A"}</td>
                        <td style={{ padding: "10px" }}>
                          {d.coordinate ? `Row ${d.coordinate.rowNumber}, Col ${d.coordinate.columnNumber}` : "Workbook"}
                        </td>
                        <td style={{ padding: "10px" }}>{d.message}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              ) : (
                <p style={{ color: "#64748b", marginTop: "12px" }}>No validation diagnostics triggered.</p>
              )}
            </div>
          ) : (
            <EmptyState title="No Validation Results" detail="Run the validation engine to generate field and record diagnostics." />
          )}
        </section>
      )}

      {/* Tab 4: Review & Preview */}
      {activeTab === "preview" && (
        <section className="panel" style={{ padding: "24px", background: "var(--card-bg, #ffffff)", borderRadius: "12px", border: "1px solid var(--border, #e2e8f0)" }}>
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "16px" }}>
            <h3>Mandatory Review & Preview Attestation</h3>
            <button
              onClick={handleGeneratePreview}
              disabled={previewLoading}
              className="primary-action"
              style={{ padding: "8px 16px", borderRadius: "8px", background: "#3b82f6", color: "#fff", border: "none", cursor: "pointer", fontWeight: 600 }}
            >
              {previewLoading ? "Generating Preview..." : "Generate Preview Attestation"}
            </button>
          </div>

          {preview ? (
            <div>
              {/* Attestation Banner */}
              <div style={{ padding: "16px", background: "#f0fdf4", border: "1px solid #86efac", borderRadius: "8px", marginBottom: "20px" }}>
                <div style={{ display: "flex", alignItems: "center", gap: "8px", color: "#166534", fontWeight: 700 }}>
                  <ShieldCheck size={20} /> Tamper-Evident Preview Attestation Issued
                </div>
                <p style={{ fontSize: "0.875rem", marginTop: "4px", color: "#15803d" }}>
                  Content Fingerprint: <code>{preview.attestation.contentFingerprint}</code>
                </p>
                <p style={{ fontSize: "0.85rem", color: "#166534" }}>
                  Signature: <code>{preview.attestation.signature}</code> · Issued: {new Date(preview.attestation.generatedAt).toLocaleString()}
                </p>
              </div>

              <h4>Representative Records Preview</h4>
              <div style={{ overflowX: "auto", marginTop: "12px" }}>
                <table className="data-table" style={{ width: "100%", borderCollapse: "collapse" }}>
                  <thead>
                    <tr style={{ background: "#f8fafc" }}>
                      <th style={{ padding: "10px", textAlign: "left" }}>Record #</th>
                      <th style={{ padding: "10px", textAlign: "left" }}>Coordinate</th>
                      <th style={{ padding: "10px", textAlign: "left" }}>Fields (Target: Mapped Value [Original])</th>
                      <th style={{ padding: "10px", textAlign: "left" }}>Diagnostics</th>
                    </tr>
                  </thead>
                  <tbody>
                    {preview.representativeRecords.map((rec) => (
                      <tr key={rec.recordIndex} style={{ borderBottom: "1px solid #e2e8f0" }}>
                        <td style={{ padding: "10px" }}><strong>#{rec.recordIndex}</strong></td>
                        <td style={{ padding: "10px" }}>{rec.rowCoordinate.worksheetName} R{rec.rowCoordinate.rowNumber}</td>
                        <td style={{ padding: "10px" }}>
                          {rec.fields.map((f, i) => (
                            <span key={i} style={{ display: "inline-block", marginRight: "12px", background: "#f1f5f9", padding: "4px 8px", borderRadius: "4px", fontSize: "0.85rem" }}>
                              <strong>{f.targetField}</strong>: {String(f.mappedValue)} <small style={{ color: "#64748b" }}>[{f.originalNormalizedValue}]</small>
                            </span>
                          ))}
                        </td>
                        <td style={{ padding: "10px" }}>
                          {rec.diagnostics.length > 0 ? (
                            <span className="status-badge warning">{rec.diagnostics.length} issue(s)</span>
                          ) : (
                            <span className="status-badge success">Clean</span>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <div style={{ marginTop: "24px", padding: "16px", background: "#f8fafc", borderRadius: "8px", border: "1px border #cbd5e1" }}>
                <strong>Commit Action Status: Explicitly Deferred</strong>
                <p style={{ fontSize: "0.875rem", color: "#64748b", marginTop: "4px" }}>
                  Per milestone boundary rules, production DB commitment is deferred. This preview contract guarantees content fingerprint integrity prior to future commit execution.
                </p>
              </div>
            </div>
          ) : (
            <EmptyState title="Preview Not Generated" detail="Click 'Generate Preview Attestation' to review representative mapped records and receive a server attestation fingerprint." />
          )}
        </section>
      )}
    </div>
  );
}

function TableSkeleton() {
  return <div style={{ padding: "32px", textAlign: "center", color: "#64748b" }}>Loading import job orchestration...</div>;
}