"use client";

import { useEffect, useMemo, useState } from "react";
import { useSearchParams } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { commitBatch, getReviewSummary, ImportReviewSummary, resolveExistingSku, resolveWarningRow } from "@/lib/api/dataHubApi";

type Filter = "all" | "errors" | "warnings" | "existing" | "ready";

export default function ReviewQueuePage() {
  const batchId = useSearchParams().get("batchId") ?? "";
  const [summary, setSummary] = useState<ImportReviewSummary | null>(null);
  const [filter, setFilter] = useState<Filter>("all");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const load = async () => {
    if (!batchId) return;
    try { setSummary(await getReviewSummary(batchId)); setError(null); }
    catch (reason) { setError(reason instanceof Error ? reason.message : "Unable to load review summary."); }
  };
  useEffect(() => {
    const timeout = window.setTimeout(() => { void load(); }, 0);
    return () => window.clearTimeout(timeout);
  }, [batchId]); // eslint-disable-line react-hooks/exhaustive-deps

  const rows = useMemo(() => (summary?.rows ?? []).filter(row => {
    if (filter === "errors") return row.severity === "Error";
    if (filter === "warnings") return row.severity === "Warning";
    if (filter === "existing") return row.message.toLowerCase().includes("sku already exists") || row.severity === "Skipped";
    if (filter === "ready") return row.severity === "Ready";
    return true;
  }), [filter, summary]);
  const blocking = !summary || summary.errorRows > 0 || summary.warningRows > 0 || summary.existingSkuConflicts > 0 || summary.validRows === 0;

  const resolve = async (resolution: "Skip" | "Cancel") => {
    if (!batchId || (resolution === "Skip" && !window.confirm("Skip every existing-SKU row and keep all new rows selected for atomic commit?"))) return;
    if (resolution === "Cancel" && !window.confirm("Cancel this entire import without changing core data?")) return;
    setBusy(true);
    try { await resolveExistingSku(batchId, resolution); await load(); }
    catch (reason) { setError(reason instanceof Error ? reason.message : "Resolution failed."); }
    finally { setBusy(false); }
  };

  const commit = async () => {
    setBusy(true);
    try { await commitBatch(batchId); await load(); }
    catch (reason) { setError(reason instanceof Error ? reason.message : "Commit failed."); }
    finally { setBusy(false); }
  };

  const resolveWarning = async (rowNumber: number, resolution: "Accept" | "Skip") => {
    setBusy(true);
    try { await resolveWarningRow(batchId, rowNumber, resolution); await load(); }
    catch (reason) { setError(reason instanceof Error ? reason.message : "Warning resolution failed."); }
    finally { setBusy(false); }
  };

  if (!batchId) return <div className="p-8"><Card><CardContent className="p-8">Open this screen from an import batch to review it.</CardContent></Card></div>;
  return <div className="p-8 space-y-6">
    <div><h1 className="text-3xl font-black">Import Review</h1><p className="text-muted-foreground">Batch {summary?.batchId ?? batchId} · {summary?.fileName ?? "Loading…"}</p></div>
    {error && <div role="alert" className="rounded-xl bg-destructive/10 p-4 text-destructive">{error}</div>}
    <div className="grid grid-cols-2 md:grid-cols-6 gap-3">
      {[['Ready', summary?.validRows], ['Warnings', summary?.warningRows], ['Errors', summary?.errorRows], ['Existing SKU', summary?.existingSkuConflicts], ['Skipped', summary?.skippedRows], ['Rows shown', rows.length]].map(([label, value]) => <Card key={String(label)}><CardContent className="p-4"><p className="text-xs text-muted-foreground">{label}</p><p className="text-2xl font-bold">{value ?? 0}</p></CardContent></Card>)}
    </div>
    <div className="flex flex-wrap gap-2">{(['all','errors','warnings','existing','ready'] as Filter[]).map(value => <Button key={value} variant={filter === value ? "default" : "outline"} onClick={() => setFilter(value)}>{value}</Button>)}</div>
    <Card><CardHeader><CardTitle>Row-level review</CardTitle></CardHeader><CardContent className="overflow-x-auto"><table className="w-full text-sm"><thead><tr className="text-left"><th className="p-2">Row</th><th>SKU</th><th>Field</th><th>Current value</th><th>Severity</th><th>Actionable message</th><th>Resolution</th></tr></thead><tbody>{rows.map((row, index) => <tr key={`${row.rowNumber}-${row.field}-${index}`} className="border-t"><td className="p-2">{row.rowNumber}</td><td>{row.sku || '—'}</td><td>{row.field}</td><td>{row.currentValue || '—'}</td><td>{row.severity}</td><td>{row.message || row.status}</td><td>{row.severity === "Warning" && !row.message.toLowerCase().includes("sku already exists") && <div className="flex gap-1"><Button size="sm" disabled={busy} onClick={() => resolveWarning(row.rowNumber, "Accept")}>Accept</Button><Button size="sm" variant="outline" disabled={busy} onClick={() => resolveWarning(row.rowNumber, "Skip")}>Skip row</Button></div>}</td></tr>)}</tbody></table>{rows.length === 0 && <p className="py-8 text-center text-muted-foreground">No rows match this filter.</p>}</CardContent></Card>
    <div className="flex flex-wrap gap-3">
      <Button variant="outline" disabled={busy || !summary?.existingSkuConflicts} onClick={() => resolve("Skip")}>Explicitly skip existing SKUs</Button>
      <Button variant="destructive" disabled={busy} onClick={() => resolve("Cancel")}>Cancel entire import</Button>
      <Button disabled={busy || blocking} onClick={commit}>Confirm atomic import</Button>
    </div>
  </div>;
}
