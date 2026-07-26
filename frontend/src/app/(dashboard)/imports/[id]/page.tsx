"use client";

import React, { useCallback, useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { PermissionGate } from "@/components/portal/PermissionGate";
import { EmptyState, ServiceErrorState, SlowNetworkState } from "@/components/portal/states";
import { UserBadge } from "@/components/ui/user-badge";
import { importDataProvider, importRepository } from "@/lib/imports";
import {
  ImportAuditEvent,
  ImportCommitResult,
  ImportCommitStatus,
  ImportJob,
  ImportPreviewDetail,
  MappingProfileConfig,
  MappingResultSummary,
  ValidationResultSummary,
} from "@/lib/imports/contracts";
import { ArrowLeft, CheckCircle2, AlertTriangle, XCircle, FileText, Settings, ShieldCheck, RefreshCw, Database, History, Lock, Loader2 } from "lucide-react";

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
  const [activeTab, setActiveTab] = useState<"overview" | "mapping" | "validation" | "preview" | "commit" | "audit">("overview");
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

  // Preview state
  const [preview, setPreview] = useState<ImportPreviewDetail | null>(null);
  const [previewLoading, setPreviewLoading] = useState(false);

  // Commit & Polling state
  const [commitLoading, setCommitLoading] = useState(false);
  const [isPollingCommit, setIsPollingCommit] = useState(false);
  const [commitStatusState, setCommitStatusState] = useState<string | null>(null);
  const [commitResult, setCommitResult] = useState<ImportCommitResult | null>(null);
  const [commitError, setCommitError] = useState<string | null>(null);
  const [showCommitModal, setShowCommitModal] = useState(false);

  const [auditEvents, setAuditEvents] = useState<ImportAuditEvent[]>([]);
  const [auditLoading, setAuditLoading] = useState(false);

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

  const loadAudit = useCallback(
    (signal?: AbortSignal) => {
      setAuditLoading(true);
      importRepository
        .getAuditEvents(jobId, 1, 50, signal)
        .then((res) => {
          setAuditEvents(res.items);
        })
        .catch(() => {})
        .finally(() => setAuditLoading(false));
    },
    [jobId]
  );

  useEffect(() => {
    const controller = new AbortController();
    loadJob(controller.signal);
    return () => controller.abort();
  }, [loadJob]);

  useEffect(() => {
    if (activeTab === "audit") {
      const controller = new AbortController();
      loadAudit(controller.signal);
      return () => controller.abort();
    }
  }, [activeTab, loadAudit]);

  // Polling hook for background commit execution
  useEffect(() => {
    if (!isPollingCommit) return;

    const controller = new AbortController();
    const interval = setInterval(() => {
      importRepository
        .getCommitStatus(jobId, controller.signal)
        .then((res) => {
          setCommitStatusState(res.commitStatus);
          if (res.commitStatus === "Completed") {
            setIsPollingCommit(false);
            setCommitLoading(false);
            setCommitResult({
              jobId: res.jobId,
              idempotencyKey: res.idempotencyKey,
              insertedCount: preview?.totalRecordsProcessed || 0,
              updatedCount: 0,
              skippedCount: 0,
              committedAt: new Date().toISOString(),
              replayed: false,
            });
            loadJob();
          } else if (res.commitStatus === "Failed" || res.commitStatus === "Poison") {
            setIsPollingCommit(false);
            setCommitLoading(false);
            setCommitError(`Background commit processing failed with status '${res.commitStatus}'.`);
          }
        })
        .catch((err) => {
          if (err?.name !== "AbortError") {
            setIsPollingCommit(false);
            setCommitLoading(false);
            setCommitError("Failed to poll commit task status.");
          }
        });
    }, 1000);

    return () => {
      clearInterval(interval);
      controller.abort();
    };
  }, [isPollingCommit, jobId, job, loadJob]);

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

  const handleExecuteCommit = async () => {
    setShowCommitModal(false);
    setCommitLoading(true);
    setCommitError(null);
    try {
      const idempotencyKey = `commit-${jobId}-${Date.now()}`;
      const expectedVersion = job?.version || 1;
      const res = await importRepository.commitImportJob(jobId, idempotencyKey, expectedVersion);
      if (res.commitStatus === "Queued" || res.commitStatus === "Pending" || res.commitStatus === "Processing") {
        setCommitStatusState(res.commitStatus);
        setIsPollingCommit(true);
      } else if (res.insertedCount !== undefined) {
        setCommitResult(res as ImportCommitResult);
        setCommitLoading(false);
        loadJob();
      }
    } catch (err: any) {
      setCommitError(err.message || "Transactional commit failed.");
      setCommitLoading(false);
    }
  };

  if (state === "loading") return <TableSkeleton />;
  if (state === "error" || !job) {
    return <ServiceErrorState retry={() => loadJob()} />;
  }

  return (
    <div style={{ padding: "24px", maxWidth: "1400px", margin: "0 auto" }}>
      {/* Top Breadcrumb Header */}
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: "16px" }}>
        <button
          onClick={() => router.push("/imports")}
          style={{ display: "flex", alignItems: "center", gap: "6px", background: "none", border: "none", color: "#64748b", cursor: "pointer", fontWeight: 500 }}
        >
          <ArrowLeft size={16} /> Back to Import Center
        </button>
        <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
          <span className={`status-badge ${job.status === "Completed" ? "success" : job.status === "Failed" ? "danger" : "warning"}`}>
            {job.state}
          </span>
          <UserBadge name={job.creator} />
        </div>
      </div>

      {/* Title */}
      <div style={{ marginBottom: "24px" }}>
        <h1 style={{ fontSize: "1.75rem", fontWeight: 700, margin: 0 }}>{job.fileName}</h1>
        <p style={{ color: "#64748b", fontSize: "0.875rem", marginTop: "4px" }}>
          Job ID: <code>{job.id}</code> · Provider: {job.source} · Created: {new Date(job.createdAt).toLocaleString()}
        </p>
      </div>

      {/* Workflow Tabs */}
      <div style={{ display: "flex", gap: "12px", borderBottom: "1px solid #e2e8f0", marginBottom: "24px" }}>
        <button
          onClick={() => setActiveTab("overview")}
          style={{
            padding: "10px 16px",
            border: "none",
            background: "none",
            borderBottom: activeTab === "overview" ? "2px solid #3b82f6" : "2px solid transparent",
            color: activeTab === "overview" ? "#3b82f6" : "#64748b",
            fontWeight: 600,
            cursor: "pointer",
          }}
        >
          Overview
        </button>
        <button
          onClick={() => setActiveTab("mapping")}
          style={{
            padding: "10px 16px",
            border: "none",
            background: "none",
            borderBottom: activeTab === "mapping" ? "2px solid #3b82f6" : "2px solid transparent",
            color: activeTab === "mapping" ? "#3b82f6" : "#64748b",
            fontWeight: 600,
            cursor: "pointer",
          }}
        >
          Mapping Rules
        </button>
        <button
          onClick={() => setActiveTab("validation")}
          style={{
            padding: "10px 16px",
            border: "none",
            background: "none",
            borderBottom: activeTab === "validation" ? "2px solid #3b82f6" : "2px solid transparent",
            color: activeTab === "validation" ? "#3b82f6" : "#64748b",
            fontWeight: 600,
            cursor: "pointer",
          }}
        >
          Validation Engine
        </button>
        <button
          onClick={() => setActiveTab("preview")}
          style={{
            padding: "10px 16px",
            border: "none",
            background: "none",
            borderBottom: activeTab === "preview" ? "2px solid #3b82f6" : "2px solid transparent",
            color: activeTab === "preview" ? "#3b82f6" : "#64748b",
            fontWeight: 600,
            cursor: "pointer",
          }}
        >
          Review & Attestation
        </button>
        <button
          onClick={() => setActiveTab("commit")}
          style={{
            padding: "10px 16px",
            border: "none",
            background: "none",
            borderBottom: activeTab === "commit" ? "2px solid #3b82f6" : "2px solid transparent",
            color: activeTab === "commit" ? "#3b82f6" : "#64748b",
            fontWeight: 600,
            cursor: "pointer",
            display: "flex",
            alignItems: "center",
            gap: "6px",
          }}
        >
          <Database size={16} /> Transactional Commit
        </button>
        <button
          onClick={() => setActiveTab("audit")}
          style={{
            padding: "10px 16px",
            border: "none",
            background: "none",
            borderBottom: activeTab === "audit" ? "2px solid #3b82f6" : "2px solid transparent",
            color: activeTab === "audit" ? "#3b82f6" : "#64748b",
            fontWeight: 600,
            cursor: "pointer",
            display: "flex",
            alignItems: "center",
            gap: "6px",
          }}
        >
          <History size={16} /> Audit Trail
        </button>
      </div>

      {/* Tab: Overview */}
      {activeTab === "overview" && (
        <section style={{ display: "grid", gridTemplateColumns: "repeat(4, 1fr)", gap: "16px" }}>
          <div style={{ padding: "16px", background: "#f8fafc", borderRadius: "8px", border: "1px solid #e2e8f0" }}>
            <span style={{ color: "#64748b", fontSize: "0.85rem" }}>Lifecycle State</span>
            <div style={{ fontSize: "1.25rem", fontWeight: 700, marginTop: "4px" }}>{job.state}</div>
          </div>
          <div style={{ padding: "16px", background: "#f8fafc", borderRadius: "8px", border: "1px solid #e2e8f0" }}>
            <span style={{ color: "#64748b", fontSize: "0.85rem" }}>Warnings</span>
            <div style={{ fontSize: "1.25rem", fontWeight: 700, color: "#d97706", marginTop: "4px" }}>{job.warnings}</div>
          </div>
          <div style={{ padding: "16px", background: "#f8fafc", borderRadius: "8px", border: "1px solid #e2e8f0" }}>
            <span style={{ color: "#64748b", fontSize: "0.85rem" }}>Errors</span>
            <div style={{ fontSize: "1.25rem", fontWeight: 700, color: "#dc2626", marginTop: "4px" }}>{job.errors}</div>
          </div>
          <div style={{ padding: "16px", background: "#f8fafc", borderRadius: "8px", border: "1px solid #e2e8f0" }}>
            <span style={{ color: "#64748b", fontSize: "0.85rem" }}>Blocking Errors</span>
            <div style={{ fontSize: "1.25rem", fontWeight: 700, color: "#991b1b", marginTop: "4px" }}>{job.blockingErrors}</div>
          </div>
        </section>
      )}

      {/* Tab: Mapping */}
      {activeTab === "mapping" && (
        <section style={{ background: "#ffffff", padding: "20px", borderRadius: "8px", border: "1px solid #e2e8f0" }}>
          <h3 style={{ fontSize: "1.1rem", fontWeight: 600, marginBottom: "16px" }}>Mapping Configuration</h3>
          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "16px", marginBottom: "16px" }}>
            <div>
              <label style={{ display: "block", fontSize: "0.875rem", fontWeight: 600, marginBottom: "4px" }}>
                Source "Part Number" → Target Field
              </label>
              <input
                type="text"
                value={targetField1}
                onChange={(e) => setTargetField1(e.target.value)}
                style={{ width: "100%", padding: "8px", borderRadius: "6px", border: "1px solid #cbd5e1" }}
              />
            </div>
            <div>
              <label style={{ display: "block", fontSize: "0.875rem", fontWeight: 600, marginBottom: "4px" }}>
                Source "Qty" → Target Field
              </label>
              <input
                type="text"
                value={targetField2}
                onChange={(e) => setTargetField2(e.target.value)}
                style={{ width: "100%", padding: "8px", borderRadius: "6px", border: "1px solid #cbd5e1" }}
              />
            </div>
          </div>
          <button
            onClick={handleApplyMapping}
            disabled={mappingLoading}
            style={{ padding: "8px 16px", borderRadius: "6px", background: "#3b82f6", color: "#fff", border: "none", cursor: "pointer", fontWeight: 600 }}
          >
            {mappingLoading ? "Applying Mapping..." : "Apply Mapping Profile"}
          </button>

          {mappingResult && (
            <div style={{ marginTop: "20px", padding: "16px", background: "#f0fdf4", border: "1px solid #86efac", borderRadius: "6px" }}>
              <div style={{ fontWeight: 600, color: "#166534" }}>Mapping Applied Successfully</div>
              <p style={{ fontSize: "0.875rem", marginTop: "4px", color: "#15803d" }}>
                Total Rows Processed: {mappingResult.totalRowsProcessed} · Mapped Records: {mappingResult.mappedRecordCount}
              </p>
            </div>
          )}
        </section>
      )}

      {/* Tab: Validation */}
      {activeTab === "validation" && (
        <section style={{ background: "#ffffff", padding: "20px", borderRadius: "8px", border: "1px solid #e2e8f0" }}>
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "16px" }}>
            <h3 style={{ fontSize: "1.1rem", fontWeight: 600, margin: 0 }}>Validation Engine</h3>
            <button
              onClick={handleRunValidation}
              disabled={validationLoading}
              style={{ padding: "8px 16px", borderRadius: "6px", background: "#3b82f6", color: "#fff", border: "none", cursor: "pointer", fontWeight: 600 }}
            >
              {validationLoading ? "Running Engine..." : "Execute Validation Engine"}
            </button>
          </div>

          {validationResult ? (
            <div>
              <div style={{ display: "grid", gridTemplateColumns: "repeat(4, 1fr)", gap: "12px", marginBottom: "20px" }}>
                <div style={{ padding: "12px", background: "#f8fafc", borderRadius: "6px" }}>
                  <span style={{ fontSize: "0.8rem", color: "#64748b" }}>Valid Records</span>
                  <div style={{ fontWeight: 700, color: "#16a34a" }}>{validationResult.summary.validRecords}</div>
                </div>
                <div style={{ padding: "12px", background: "#f8fafc", borderRadius: "6px" }}>
                  <span style={{ fontSize: "0.8rem", color: "#64748b" }}>Warnings</span>
                  <div style={{ fontWeight: 700, color: "#d97706" }}>{validationResult.summary.warningCount}</div>
                </div>
                <div style={{ padding: "12px", background: "#f8fafc", borderRadius: "6px" }}>
                  <span style={{ fontSize: "0.8rem", color: "#64748b" }}>Errors</span>
                  <div style={{ fontWeight: 700, color: "#dc2626" }}>{validationResult.summary.errorCount}</div>
                </div>
                <div style={{ padding: "12px", background: "#f8fafc", borderRadius: "6px" }}>
                  <span style={{ fontSize: "0.8rem", color: "#64748b" }}>Blocking Errors</span>
                  <div style={{ fontWeight: 700, color: "#991b1b" }}>{validationResult.summary.blockingErrorCount}</div>
                </div>
              </div>
            </div>
          ) : (
            <EmptyState title="Validation Not Run" detail="Click 'Execute Validation Engine' to validate mapped fields against business rules." />
          )}
        </section>
      )}

      {/* Tab: Preview & Attestation */}
      {activeTab === "preview" && (
        <section style={{ background: "#ffffff", padding: "20px", borderRadius: "8px", border: "1px solid #e2e8f0" }}>
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "16px" }}>
            <h3 style={{ fontSize: "1.1rem", fontWeight: 600, margin: 0 }}>Review & Preview Attestation</h3>
            <button
              onClick={handleGeneratePreview}
              disabled={previewLoading}
              style={{ padding: "8px 16px", borderRadius: "6px", background: "#3b82f6", color: "#fff", border: "none", cursor: "pointer", fontWeight: 600 }}
            >
              {previewLoading ? "Generating Preview..." : "Generate Preview Attestation"}
            </button>
          </div>

          {preview ? (
            <div>
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
                <table style={{ width: "100%", borderCollapse: "collapse" }}>
                  <thead>
                    <tr style={{ background: "#f8fafc" }}>
                      <th style={{ padding: "10px", textAlign: "left" }}>Record #</th>
                      <th style={{ padding: "10px", textAlign: "left" }}>Coordinate</th>
                      <th style={{ padding: "10px", textAlign: "left" }}>Fields</th>
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
                              <strong>{f.targetField}</strong>: {String(f.mappedValue)}
                            </span>
                          ))}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          ) : (
            <EmptyState title="Preview Not Generated" detail="Click 'Generate Preview Attestation' to review representative mapped records and receive a server attestation fingerprint." />
          )}
        </section>
      )}

      {/* Tab: Transactional Commit */}
      {activeTab === "commit" && (
        <section style={{ background: "#ffffff", padding: "20px", borderRadius: "8px", border: "1px solid #e2e8f0" }}>
          <h3 style={{ fontSize: "1.1rem", fontWeight: 600, marginBottom: "16px" }}>Durable Transactional Commit</h3>

          {commitResult ? (
            <div style={{ padding: "20px", background: "#f0fdf4", border: "1px solid #86efac", borderRadius: "8px" }}>
              <div style={{ display: "flex", alignItems: "center", gap: "8px", color: "#166534", fontWeight: 700, fontSize: "1.1rem" }}>
                <CheckCircle2 size={24} /> Transactional Commit Completed
              </div>
              <p style={{ marginTop: "8px", color: "#15803d", fontSize: "0.9rem" }}>
                Job ID: <code>{commitResult.jobId}</code> · Idempotency Key: <code>{commitResult.idempotencyKey}</code>
              </p>
              <p style={{ color: "#166534", fontSize: "0.9rem" }}>
                Committed At: {new Date(commitResult.committedAt).toLocaleString()}
                {commitResult.replayed && <span style={{ marginLeft: "8px", background: "#dcfce7", padding: "2px 8px", borderRadius: "4px" }}>Idempotent Replay</span>}
              </p>
            </div>
          ) : isPollingCommit ? (
            <div style={{ padding: "24px", background: "#eff6ff", border: "1px solid #bfdbfe", borderRadius: "8px", textAlign: "center" }}>
              <div style={{ display: "flex", alignItems: "center", justifyContent: "center", gap: "8px", color: "#1d4ed8", fontWeight: 700, fontSize: "1.1rem" }}>
                <Loader2 size={24} className="animate-spin" /> Background Commit Processing
              </div>
              <p style={{ marginTop: "8px", color: "#1e40af", fontSize: "0.9rem" }}>
                Status: <strong>{commitStatusState}</strong>. Executing background worker transaction...
              </p>
            </div>
          ) : (
            <div>
              <div style={{ padding: "16px", background: "#f8fafc", borderRadius: "8px", border: "1px solid #e2e8f0", marginBottom: "20px" }}>
                <div style={{ fontWeight: 600, marginBottom: "8px" }}>Commit Preconditions Check</div>
                <ul style={{ paddingLeft: "20px", margin: 0, fontSize: "0.9rem", color: "#334155" }}>
                  <li>Lifecycle State: <strong>{job.state}</strong> {job.state === "ReadyForReview" ? "✅" : "❌ (Must be ReadyForReview)"}</li>
                  <li>Validation Blocking Errors: <strong>{job.blockingErrors}</strong> {job.blockingErrors === 0 ? "✅" : "❌ (Must be 0)"}</li>
                  <li>Preview Attestation: {preview ? "Issued ✅" : "Not Issued ❌"}</li>
                </ul>
              </div>

              {commitError && (
                <div style={{ padding: "12px", background: "#fef2f2", border: "1px solid #fecaca", borderRadius: "6px", color: "#991b1b", marginBottom: "16px" }}>
                  <strong>Commit Failed:</strong> {commitError}
                </div>
              )}

              <PermissionGate permission="import.commit">
                <button
                  onClick={() => setShowCommitModal(true)}
                  disabled={commitLoading || job.state !== "ReadyForReview" || job.blockingErrors > 0}
                  style={{
                    padding: "12px 24px",
                    borderRadius: "8px",
                    background: job.state === "ReadyForReview" && job.blockingErrors === 0 ? "#16a34a" : "#94a3b8",
                    color: "#fff",
                    border: "none",
                    cursor: job.state === "ReadyForReview" && job.blockingErrors === 0 ? "pointer" : "not-allowed",
                    fontWeight: 700,
                    fontSize: "1rem",
                  }}
                >
                  {commitLoading ? "Enqueuing Commit Task..." : "Commit Mapped Records to Database"}
                </button>
              </PermissionGate>
            </div>
          )}

          {/* Commitment Confirmation Modal */}
          {showCommitModal && (
            <div style={{ position: "fixed", top: 0, left: 0, right: 0, bottom: 0, background: "rgba(0,0,0,0.5)", display: "flex", alignItems: "center", justifyContent: "center", zIndex: 1000 }}>
              <div style={{ background: "#fff", padding: "24px", borderRadius: "12px", maxWidth: "500px", width: "100%" }}>
                <h3 style={{ fontSize: "1.2rem", fontWeight: 700, marginTop: 0 }}>Confirm Background Commit</h3>
                <p style={{ fontSize: "0.9rem", color: "#475569" }}>
                  Are you sure you want to enqueue job <code>{job.id}</code> for background commit execution? This action will process synthetic records into the generic commit target.
                </p>
                <div style={{ display: "flex", justifyContent: "flex-end", gap: "12px", marginTop: "24px" }}>
                  <button
                    onClick={() => setShowCommitModal(false)}
                    style={{ padding: "8px 16px", borderRadius: "6px", background: "#e2e8f0", border: "none", cursor: "pointer", fontWeight: 600 }}
                  >
                    Cancel
                  </button>
                  <button
                    onClick={handleExecuteCommit}
                    style={{ padding: "8px 16px", borderRadius: "6px", background: "#16a34a", color: "#fff", border: "none", cursor: "pointer", fontWeight: 600 }}
                  >
                    Confirm Commit
                  </button>
                </div>
              </div>
            </div>
          )}
        </section>
      )}

      {/* Tab: Audit Trail */}
      {activeTab === "audit" && (
        <section style={{ background: "#ffffff", padding: "20px", borderRadius: "8px", border: "1px solid #e2e8f0" }}>
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "16px" }}>
            <h3 style={{ fontSize: "1.1rem", fontWeight: 600, margin: 0 }}>Append-Only Audit Timeline</h3>
            <button
              onClick={() => loadAudit()}
              style={{ padding: "6px 12px", borderRadius: "6px", background: "#f1f5f9", border: "1px solid #cbd5e1", cursor: "pointer", display: "flex", alignItems: "center", gap: "4px" }}
            >
              <RefreshCw size={14} /> Refresh
            </button>
          </div>

          {auditLoading ? (
            <div style={{ padding: "24px", textAlign: "center", color: "#64748b" }}>Loading audit trail...</div>
          ) : auditEvents.length > 0 ? (
            <div style={{ display: "flex", flexDirection: "column", gap: "12px" }}>
              {auditEvents.map((evt) => (
                <div key={evt.id} style={{ padding: "12px 16px", background: "#f8fafc", borderRadius: "6px", borderLeft: "4px solid #3b82f6" }}>
                  <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                    <span style={{ fontWeight: 700, fontSize: "0.95rem" }}>{evt.eventType}</span>
                    <span style={{ fontSize: "0.8rem", color: "#64748b" }}>{new Date(evt.occurredAt).toLocaleString()}</span>
                  </div>
                  <p style={{ margin: "4px 0", fontSize: "0.875rem", color: "#334155" }}>{evt.message}</p>
                  <div style={{ display: "flex", alignItems: "center", gap: "8px", fontSize: "0.8rem", color: "#64748b" }}>
                    <span>Actor:</span>
                    <UserBadge name={evt.actorUserId} />
                    {evt.fromState && evt.toState && (
                      <span style={{ marginLeft: "12px" }}>
                        Transition: <code>{evt.fromState}</code> → <code>{evt.toState}</code>
                      </span>
                    )}
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <EmptyState title="No Audit Events" detail="No audit trail events recorded yet for this import job." />
          )}
        </section>
      )}
    </div>
  );
}

function TableSkeleton() {
  return <div style={{ padding: "32px", textAlign: "center", color: "#64748b" }}>Loading import job orchestration...</div>;
}