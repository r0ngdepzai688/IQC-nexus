"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { HeaderInspection, HeaderMapping, inspectMasterPlanHeaders, uploadMasterPlan } from "@/lib/api/dataHubApi";

export default function MappingDictionaryPage() {
  const router = useRouter();
  const [file, setFile] = useState<File | null>(null);
  const [inspection, setInspection] = useState<HeaderInspection | null>(null);
  const [mapping, setMapping] = useState<Record<number, string>>({});
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const inspect = async (selected: File) => {
    setBusy(true); setError(null); setFile(selected);
    try {
      const result = await inspectMasterPlanHeaders(selected);
      setInspection(result);
      setMapping(Object.fromEntries(result.columns.filter(column => column.suggestedCanonical && !column.ambiguous).map(column => [column.columnIndex, column.suggestedCanonical!] )));
    } catch (reason) { setError(reason instanceof Error ? reason.message : "Header inspection failed."); }
    finally { setBusy(false); }
  };

  const submit = async () => {
    if (!file || !inspection) return;
    const mappings: HeaderMapping[] = Object.entries(mapping).filter(([, canonical]) => canonical).map(([columnIndex, canonicalField]) => ({ columnIndex: Number(columnIndex), canonicalField }));
    const canonical = mappings.map(value => value.canonicalField);
    const duplicates = canonical.filter((value, index) => canonical.indexOf(value) !== index);
    const missing = inspection.requiredFields.filter(value => !canonical.includes(value));
    const ambiguous = inspection.columns.filter(column => column.ambiguous && !mapping[column.columnIndex]);
    if (duplicates.length) return setError(`Duplicate canonical mappings: ${[...new Set(duplicates)].join(", ")}.`);
    if (missing.length) return setError(`Missing required mappings: ${missing.join(", ")}.`);
    if (ambiguous.length) return setError("Resolve every ambiguous header before validation.");
    setBusy(true); setError(null);
    try {
      const batch = await uploadMasterPlan(file, "NewModels", mappings);
      router.push(`/support/data-hub/review-queue?batchId=${encodeURIComponent(batch.batchId)}`);
    } catch (reason) { setError(reason instanceof Error ? reason.message : "Validation failed."); }
    finally { setBusy(false); }
  };

  return <div className="p-8 space-y-6">
    <div><h1 className="text-3xl font-black">Workbook Header Mapping</h1><p className="text-muted-foreground">Inspect headers, confirm canonical fields, then re-run server validation.</p></div>
    {error && <div role="alert" className="rounded-xl bg-destructive/10 p-4 text-destructive">{error}</div>}
    <Card><CardContent className="p-6"><input type="file" accept=".xlsx,.xls,.xlsm" disabled={busy} onChange={event => { const selected = event.target.files?.[0]; if (selected) void inspect(selected); }} /></CardContent></Card>
    {inspection && <Card><CardHeader><CardTitle>Detected header row {inspection.headerRow}</CardTitle></CardHeader><CardContent className="space-y-3">
      {inspection.columns.map(column => <div key={column.columnIndex} className="grid grid-cols-1 md:grid-cols-2 gap-3 items-center border-b pb-3"><div><p className="font-semibold">{column.header || `(Column ${column.columnIndex + 1})`}</p>{column.ambiguous && <p className="text-xs text-amber-600">Ambiguous—explicit choice required</p>}</div><select aria-label={`Map ${column.header}`} className="rounded-lg border bg-background p-2" value={mapping[column.columnIndex] ?? ""} onChange={event => setMapping(current => ({ ...current, [column.columnIndex]: event.target.value }))}><option value="">Ignore column</option>{inspection.canonicalFields.map(field => <option key={field} value={field}>{field}{inspection.requiredFields.includes(field) ? " (required)" : " (optional)"}</option>)}</select></div>)}
      <Button disabled={busy} onClick={submit}>Validate with this mapping</Button>
    </CardContent></Card>}
  </div>;
}
