"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
import { AlertTriangle, ArrowLeft, CheckCircle2, Database, FileSpreadsheet, ShieldCheck, UploadCloud } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import {
  commitBatch,
  getBatchPreview,
  getManualFiles,
  getReviewSummary,
  inspectMasterPlanHeaders,
  processManualUpload,
  resolveExistingSku,
  resolveReviewItem,
  resolveWarningRow,
  uploadMasterPlan,
  type HeaderInspection,
  type ImportBatch,
  type ImportReviewSummary,
  type ManualFile,
} from "@/lib/api/dataHubApi";
import { buildHeaderMappings, canCommitImport, commitResultMessage, getMappingIssues, validateMasterPlanFile } from "./importWorkflow";

type Step = "select" | "map" | "review" | "committed";

export default function ManualImportPage() {
  const [step, setStep] = useState<Step>("select");
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [inspection, setInspection] = useState<HeaderInspection | null>(null);
  const [mappingSelections, setMappingSelections] = useState<Record<number, string>>({});
  const [batch, setBatch] = useState<ImportBatch | null>(null);
  const [review, setReview] = useState<ImportReviewSummary | null>(null);
  const [manualFiles, setManualFiles] = useState<ManualFile[]>([]);
  const [manualFilesLoading, setManualFilesLoading] = useState(true);
  const [manualFilesError, setManualFilesError] = useState<string | null>(null);
  const [requestRunning, setRequestRunning] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const requestLock = useRef(false);

  useEffect(() => {
    let active = true;
    void getManualFiles()
      .then(files => {
        if (active) setManualFiles(files);
      })
      .catch(reason => {
        if (active) setManualFilesError(reason instanceof Error ? reason.message : "Unable to load server files.");
      })
      .finally(() => {
        if (active) setManualFilesLoading(false);
      });
    return () => { active = false; };
  }, []);

  const mappingIssues = useMemo(
    () => inspection ? getMappingIssues(inspection, mappingSelections) : [],
    [inspection, mappingSelections],
  );
  const commitEnabled = canCommitImport(batch, review, requestRunning);

  const runRequest = async (action: () => Promise<void>) => {
    if (requestLock.current) return;
    requestLock.current = true;
    setRequestRunning(true);
    setErrorMessage(null);
    try {
      await action();
    } catch (reason) {
      setErrorMessage(reason instanceof Error ? reason.message : "The request could not be completed.");
    } finally {
      requestLock.current = false;
      setRequestRunning(false);
    }
  };

  const handleFileSelection = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;
    const validationError = validateMasterPlanFile(file);
    if (validationError) {
      setErrorMessage(validationError);
      event.target.value = "";
      return;
    }
    void runRequest(async () => {
      const result = await inspectMasterPlanHeaders(file);
      const proposed = Object.fromEntries(
        result.columns
          .filter(column => column.suggestedCanonical && !column.ambiguous)
          .map(column => [column.columnIndex, column.suggestedCanonical!]),
      );
      setSelectedFile(file);
      setInspection(result);
      setMappingSelections(proposed);
      setStep("map");
    });
  };

  const refreshBatch = async (batchId: string) => {
    const [nextBatch, nextReview] = await Promise.all([
      getBatchPreview(batchId),
      getReviewSummary(batchId),
    ]);
    setBatch(nextBatch);
    setReview(nextReview);
  };

  const enterReview = async (nextBatch: ImportBatch) => {
    const nextReview = await getReviewSummary(nextBatch.batchId);
    setBatch(nextBatch);
    setReview(nextReview);
    setStep("review");
  };

  const handleUpload = () => {
    if (!selectedFile || !inspection || mappingIssues.length || requestRunning) return;
    void runRequest(async () => {
      const uploaded = await uploadMasterPlan(selectedFile, "NewModels", buildHeaderMappings(mappingSelections));
      await enterReview(uploaded);
    });
  };

  const handleManualUpload = (fileName: string) => {
    void runRequest(async () => {
      await enterReview(await processManualUpload(fileName));
    });
  };

  const handleExistingResolution = (resolution: "Skip" | "Cancel") => {
    if (!batch) return;
    void runRequest(async () => {
      await resolveExistingSku(batch.batchId, resolution);
      await refreshBatch(batch.batchId);
    });
  };

  const handleWarningResolution = (rowNumber: number, resolution: "Accept" | "Skip") => {
    if (!batch) return;
    void runRequest(async () => {
      await resolveWarningRow(batch.batchId, rowNumber, resolution);
      await refreshBatch(batch.batchId);
    });
  };

  const handleReviewResolution = (reviewItemId: number, action: "Override" | "Ignore" | "CreateMissing") => {
    if (!batch) return;
    void runRequest(async () => {
      await resolveReviewItem(reviewItemId, action);
      await refreshBatch(batch.batchId);
    });
  };

  const handleCommit = () => {
    if (!batch || !commitEnabled) return;
    void runRequest(async () => {
      const committed = await commitBatch(batch.batchId);
      setBatch(committed);
      setStep("committed");
      void getReviewSummary(committed.batchId).then(setReview).catch(() => undefined);
    });
  };

  const reset = () => {
    setStep("select");
    setSelectedFile(null);
    setInspection(null);
    setMappingSelections({});
    setBatch(null);
    setReview(null);
    setErrorMessage(null);
  };

  return (
    <div className="min-h-full bg-gradient-to-br from-indigo-50/50 via-white to-blue-50/30 p-8 dark:from-indigo-950/20 dark:via-[#0B0F17] dark:to-blue-900/10">
      <div className="mx-auto max-w-6xl">
        <div className="mb-6 flex items-center gap-2 text-sm font-semibold uppercase tracking-wide text-primary">
          <Database className="h-4 w-4" /> Data Hub <span aria-hidden>›</span> Master Plan import
        </div>
        <h1 className="text-4xl font-black tracking-tight">Import New Models Master Plan</h1>
        <p className="mt-2 max-w-3xl text-muted-foreground">
          Inspect and map workbook headers, review every staged row, then atomically insert only new SKUs.
        </p>

        <ol className="my-8 grid grid-cols-2 gap-3 md:grid-cols-4" aria-label="Import progress">
          {(["select", "map", "review", "committed"] as Step[]).map((value, index) => {
            const labels = ["Select workbook", "Map headers", "Review rows", "Committed"];
            const activeIndex = (["select", "map", "review", "committed"] as Step[]).indexOf(step);
            return (
              <li key={value} className={`rounded-xl border p-3 text-sm font-semibold ${index <= activeIndex ? "border-primary bg-primary/5 text-primary" : "text-muted-foreground"}`}>
                {index + 1}. {labels[index]}
              </li>
            );
          })}
        </ol>

        {errorMessage && <div role="alert" className="mb-6 rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm font-semibold text-rose-700">{errorMessage}</div>}

        {step === "select" && (
          <div className="grid max-w-5xl gap-6 lg:grid-cols-2">
          <Card className="rounded-3xl p-8">
            <div className="rounded-2xl border-2 border-dashed p-10 text-center">
              <UploadCloud className="mx-auto mb-4 h-12 w-12 text-primary" />
              <h2 className="text-2xl font-bold">Select the official workbook</h2>
              <p className="mt-2 text-sm text-muted-foreground">Supported: .xlsx, .xls, .xlsm; non-empty; maximum 50 MB.</p>
              <label className="mt-6 inline-flex cursor-pointer rounded-xl bg-primary px-6 py-3 font-bold text-white aria-disabled:cursor-not-allowed aria-disabled:opacity-50" aria-disabled={requestRunning}>
                {requestRunning ? "Inspecting headers…" : "Select local file"}
                <input aria-label="Master Plan workbook" type="file" accept=".xlsx,.xls,.xlsm" disabled={requestRunning} className="sr-only" onChange={handleFileSelection} />
              </label>
              <p className="mt-5 text-xs text-muted-foreground">No approved template artifact exists in the repository, so this screen does not offer an unverified download.</p>
            </div>
          </Card>
          <Card className="rounded-3xl p-8">
            <h2 className="text-2xl font-bold">Process a server file</h2>
            <p className="mt-2 text-sm text-muted-foreground">Files already available in the Data Hub manual-upload directory use the same review and atomic commit workflow.</p>
            {manualFilesLoading && <p className="mt-6 text-sm text-muted-foreground">Loading server filesâ€¦</p>}
            {manualFilesError && <p role="alert" className="mt-6 text-sm font-semibold text-rose-700">{manualFilesError}</p>}
            {!manualFilesLoading && !manualFilesError && manualFiles.length === 0 && <p className="mt-6 text-sm text-muted-foreground">No server files are available.</p>}
            <ul className="mt-6 space-y-3">
              {manualFiles.map(file => (
                <li key={file.fileName} className="flex items-center justify-between gap-4 rounded-xl border p-4">
                  <div className="min-w-0">
                    <p className="truncate font-semibold">{file.fileName}</p>
                    <p className="text-xs text-muted-foreground">{file.sizeBytes.toLocaleString()} bytes Â· {file.duplicateStatus}</p>
                  </div>
                  <Button size="sm" disabled={requestRunning || file.duplicateStatus === "ExactDuplicate"} onClick={() => handleManualUpload(file.fileName)}>Process</Button>
                </li>
              ))}
            </ul>
          </Card>
          </div>
        )}

        {step === "map" && inspection && selectedFile && (
          <div>
            <Button variant="ghost" disabled={requestRunning} onClick={reset} className="mb-4"><ArrowLeft className="mr-2 h-4 w-4" />Choose another file</Button>
            <Card className="rounded-3xl p-6">
              <div className="mb-5 flex flex-wrap items-start justify-between gap-4">
                <div>
                  <h2 className="text-2xl font-bold">Header mapping</h2>
                  <p className="text-sm text-muted-foreground">{selectedFile.name} · detected header row {inspection.headerRow}</p>
                </div>
                <Button onClick={handleUpload} disabled={requestRunning || mappingIssues.length > 0}>
                  {requestRunning ? "Uploading…" : "Upload and stage"}
                </Button>
              </div>
              <div className="mb-5 flex flex-wrap gap-2">
                {inspection.requiredFields.map(field => <Badge key={field}>Required: {field}</Badge>)}
                {inspection.canonicalFields.filter(field => !inspection.requiredFields.includes(field)).map(field => <Badge key={field} variant="outline">Optional: {field}</Badge>)}
              </div>
              {mappingIssues.length > 0 && <div role="alert" className="mb-5 rounded-xl bg-amber-50 p-4 text-sm text-amber-900">{mappingIssues.map(issue => <div key={issue}>• {issue}</div>)}</div>}
              <div className="overflow-x-auto">
                <table className="w-full text-left text-sm">
                  <thead><tr className="border-b"><th className="p-3">Column</th><th className="p-3">Detected header</th><th className="p-3">Canonical field</th><th className="p-3">Detection</th></tr></thead>
                  <tbody>{inspection.columns.map(column => (
                    <tr key={column.columnIndex} className="border-b">
                      <td className="p-3">{column.columnIndex + 1}</td>
                      <td className="p-3 font-medium">{column.header || "(empty)"}</td>
                      <td className="p-3">
                        <select aria-label={`Map ${column.header || `column ${column.columnIndex + 1}`}`} className="w-full rounded-lg border bg-background p-2" value={mappingSelections[column.columnIndex] ?? ""} onChange={event => setMappingSelections(current => ({ ...current, [column.columnIndex]: event.target.value }))}>
                          <option value="">Ignore / unresolved</option>
                          {inspection.canonicalFields.map(field => <option key={field} value={field}>{field}{inspection.requiredFields.includes(field) ? " (required)" : ""}</option>)}
                        </select>
                      </td>
                      <td className="p-3">{column.ambiguous ? <Badge variant="destructive">Ambiguous</Badge> : column.suggestedCanonical ? <Badge variant="outline">Suggested</Badge> : <Badge variant="outline">Unknown</Badge>}</td>
                    </tr>
                  ))}</tbody>
                </table>
              </div>
            </Card>
          </div>
        )}

        {step === "review" && batch && review && (
          <div>
            <div className="mb-5 flex flex-wrap items-end justify-between gap-4">
              <div><h2 className="text-2xl font-bold">Validation and review</h2><p className="text-sm text-muted-foreground">Batch {batch.batchId} · {review.fileName}</p></div>
              <Button onClick={handleCommit} disabled={!commitEnabled}>{requestRunning ? "Working…" : "Confirm atomic insert"}</Button>
            </div>
            <div className="mb-6 grid gap-3 sm:grid-cols-3 lg:grid-cols-6">
              {[['Ready', batch.validRows], ['Warnings', review.warningRows], ['Errors', review.errorRows], ['Existing SKU', review.existingSkuConflicts], ['Skipped', review.skippedRows], ['Total', batch.totalRows]].map(([label, value]) => (
                <Card key={String(label)} className="p-4"><div className="text-xs uppercase text-muted-foreground">{label}</div><div className="text-2xl font-black">{value}</div></Card>
              ))}
            </div>
            <div className="mb-5 rounded-xl border border-blue-200 bg-blue-50 p-4 text-sm text-blue-900">
              <ShieldCheck className="mr-2 inline h-4 w-4" /> Insert-only policy: existing SKUs are never overwritten. Default is no mutation; explicitly Skip those rows or Cancel the entire batch.
            </div>
            {review.existingSkuConflicts > 0 && (
              <div className="mb-5 flex flex-wrap gap-3 rounded-xl border border-amber-200 bg-amber-50 p-4">
                <Button disabled={requestRunning} onClick={() => handleExistingResolution("Skip")}>Skip all existing-SKU rows</Button>
                <Button disabled={requestRunning} variant="destructive" onClick={() => handleExistingResolution("Cancel")}>Cancel entire import</Button>
              </div>
            )}
            <Card className="overflow-hidden rounded-3xl">
              <div className="overflow-x-auto">
                <table className="w-full text-left text-sm">
                  <thead><tr className="border-b bg-muted/40"><th className="p-3">Row</th><th className="p-3">SKU</th><th className="p-3">Field</th><th className="p-3">Current value</th><th className="p-3">Severity</th><th className="p-3">Message</th><th className="p-3">Action</th></tr></thead>
                  <tbody>{review.rows.map((row, index) => {
                    const existingSku = row.conflictType === "ExistingSku";
                    const resolvableWarning = row.severity === "Warning" && !existingSku && row.reviewItemId == null;
                    return (
                      <tr key={`${row.rowNumber}-${row.field}-${index}`} className="border-b align-top">
                        <td className="p-3">{row.rowNumber}</td><td className="p-3 font-mono">{row.sku || "—"}</td><td className="p-3">{row.field}</td><td className="max-w-48 break-words p-3">{row.currentValue || "—"}</td>
                        <td className="p-3"><Badge variant={row.severity === "Error" ? "destructive" : "outline"}>{row.severity}</Badge></td>
                        <td className="max-w-md p-3">{row.message || (row.severity === "Ready" ? "Ready to insert." : "No message supplied.")}</td>
                        <td className="p-3">{row.reviewItemId != null ? <div className="flex flex-wrap gap-2">{row.supportedActions.map(action => <Button key={action} size="sm" variant={action === "Override" ? "default" : "outline"} disabled={requestRunning} onClick={() => handleReviewResolution(row.reviewItemId!, action)}>{action}</Button>)}</div> : resolvableWarning ? <div className="flex gap-2"><Button size="sm" disabled={requestRunning} onClick={() => handleWarningResolution(row.rowNumber, "Accept")}>Accept</Button><Button size="sm" variant="outline" disabled={requestRunning} onClick={() => handleWarningResolution(row.rowNumber, "Skip")}>Skip</Button></div> : existingSku ? "Use batch choice above" : "—"}</td>
                      </tr>
                    );
                  })}</tbody>
                </table>
              </div>
            </Card>
            {!commitEnabled && batch.status === "Staged" && <p className="mt-4 flex items-center gap-2 text-sm font-semibold text-amber-700"><AlertTriangle className="h-4 w-4" />Commit remains disabled until at least one row is ready and every blocking error, review, warning, and existing-SKU conflict is resolved.</p>}
            {batch.status === "Cancelled" && <p className="mt-4 font-semibold text-rose-700">This batch was cancelled. No Master Plan records were changed.</p>}
          </div>
        )}

        {step === "committed" && batch && (
          <Card className="mx-auto max-w-2xl rounded-3xl p-10 text-center">
            <CheckCircle2 className="mx-auto mb-5 h-16 w-16 text-emerald-500" />
            <h2 className="text-3xl font-black">Commit successful</h2>
            <p className="mt-3 text-lg text-muted-foreground">{commitResultMessage(batch)}</p>
            <div className="mt-7 flex justify-center gap-3"><Button variant="outline" onClick={reset}>Import another file</Button><Button nativeButton={false} render={<Link href="/new-model" />}>View Master Plan</Button></div>
          </Card>
        )}

        <div className="mt-8 text-sm text-muted-foreground"><FileSpreadsheet className="mr-2 inline h-4 w-4" />The legacy `/new-models/master-plan/import` route continues to redirect here.</div>
      </div>
    </div>
  );
}
