"use client";

import React, { useCallback, useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Database, History, AlertTriangle, CheckSquare, Settings2, FileText, Activity } from "lucide-react";
import { getHistory, getStagingRecords, getBatchPreview, ImportBatch, StagingMasterPlan } from "@/lib/api/dataHubApi";

export default function DataHubDashboard() {
  const searchParams = useSearchParams();
  const defaultTab = searchParams.get("tab") || "history";
  const urlBatchId = searchParams.get("batchId");

  const [activeTab, setActiveTab] = useState(defaultTab);
  const [history, setHistory] = useState<ImportBatch[]>([]);
  const [selectedBatch, setSelectedBatch] = useState<ImportBatch | null>(null);
  const [stagingRecords, setStagingRecords] = useState<StagingMasterPlan[]>([]);
  const [loading, setLoading] = useState(false);

  const fetchHistory = useCallback(async () => {
    setLoading(true);
    try {
      const data = await getHistory();
      setHistory(data);
    } catch (error) {
      console.error("Failed to fetch history:", error);
    } finally {
      setLoading(false);
    }
  }, []);

  const viewBatchDetails = useCallback(async (batchId: string) => {
    setLoading(true);
    try {
      const batchData = await getBatchPreview(batchId);
      const recordsData = await getStagingRecords(batchId);
      setSelectedBatch(batchData);
      setStagingRecords(recordsData);
      setActiveTab("details");
    } catch (error) {
      console.error("Failed to fetch batch details:", error);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (activeTab !== "history") return;

    const timeoutId = window.setTimeout(() => {
      void fetchHistory();
    }, 0);

    return () => window.clearTimeout(timeoutId);
  }, [activeTab, fetchHistory]);

  useEffect(() => {
    if (!urlBatchId || activeTab !== "history") return;

    const timeoutId = window.setTimeout(() => {
      void viewBatchDetails(urlBatchId);
    }, 0);

    return () => window.clearTimeout(timeoutId);
  }, [activeTab, urlBatchId, viewBatchDetails]);

  const formatDuration = (ms: number) => {
    if (ms < 1000) return `${ms}ms`;
    return `${(ms / 1000).toFixed(1)}s`;
  };

  const getStatusBadge = (status: string) => {
    if (status.includes("Failed")) return <Badge variant="destructive">{status}</Badge>;
    if (status === "Staged") return <Badge variant="secondary" className="bg-blue-500/10 text-blue-500">Staged</Badge>;
    if (status === "Committed") return <Badge variant="secondary" className="bg-emerald-500/10 text-emerald-500">Committed</Badge>;
    return <Badge variant="outline">{status}</Badge>;
  };

  const getRowStatusBadge = (status: string) => {
    switch (status) {
      case "ReadyToInsert":
      case "ReadyToUpdate":
        return <Badge variant="secondary" className="bg-emerald-500/10 text-emerald-500 border-none">{status}</Badge>;
      case "SkipNoChange":
        return <Badge variant="outline" className="text-muted-foreground">{status}</Badge>;
      case "ValidationError":
        return <Badge variant="destructive">{status}</Badge>;
      case "Blocked":
        return <Badge variant="destructive" className="bg-red-900 text-white">{status}</Badge>;
      case "ReviewRequired":
        return <Badge variant="secondary" className="bg-amber-500/10 text-amber-500 border-none">{status}</Badge>;
      default:
        return <Badge variant="outline">{status}</Badge>;
    }
  };

  return (
    <div className="p-8 max-w-[1600px] mx-auto space-y-8 animate-in fade-in duration-500">
      <div className="flex items-center gap-3">
        <div className="p-3 bg-primary/10 rounded-xl text-primary">
          <Database className="w-6 h-6" />
        </div>
        <div>
          <h1 className="text-3xl font-bold tracking-tight mb-1">Data Hub Operations</h1>
          <p className="text-muted-foreground text-lg">Central nervous system for Master Plan data ingestion</p>
        </div>
      </div>

      <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
        <TabsList className="grid grid-cols-5 bg-card/50 backdrop-blur border p-1 rounded-xl w-full max-w-3xl h-12">
          <TabsTrigger value="history" className="gap-2 rounded-lg data-[state=active]:bg-background data-[state=active]:shadow-sm">
            <History className="w-4 h-4" /> History
          </TabsTrigger>
          <TabsTrigger value="details" className="gap-2 rounded-lg data-[state=active]:bg-background data-[state=active]:shadow-sm" disabled={!selectedBatch}>
            <FileText className="w-4 h-4" /> Batch Details
          </TabsTrigger>
          <TabsTrigger value="errors" className="gap-2 rounded-lg data-[state=active]:bg-background data-[state=active]:shadow-sm">
            <AlertTriangle className="w-4 h-4" /> Errors
          </TabsTrigger>
          <TabsTrigger value="review" className="gap-2 rounded-lg data-[state=active]:bg-background data-[state=active]:shadow-sm">
            <CheckSquare className="w-4 h-4" /> Review Queue
          </TabsTrigger>
          <TabsTrigger value="mapping" className="gap-2 rounded-lg data-[state=active]:bg-background data-[state=active]:shadow-sm">
            <Settings2 className="w-4 h-4" /> Mapping
          </TabsTrigger>
        </TabsList>

        <TabsContent value="history" className="mt-6">
          <Card className="border-none shadow-sm bg-card/50 backdrop-blur">
            <CardHeader>
              <CardTitle>Import History</CardTitle>
              <CardDescription>Recent Master Plan ingestion batches</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="rounded-xl border bg-card overflow-hidden">
                <Table>
                  <TableHeader className="bg-muted/50">
                    <TableRow>
                      <TableHead>Batch ID</TableHead>
                      <TableHead>Uploaded By</TableHead>
                      <TableHead>Uploaded At</TableHead>
                      <TableHead>Status</TableHead>
                      <TableHead className="text-right">Total</TableHead>
                      <TableHead className="text-right">Valid</TableHead>
                      <TableHead className="text-right">Errors</TableHead>
                      <TableHead className="text-right">Review</TableHead>
                      <TableHead className="text-right">Duration</TableHead>
                      <TableHead className="text-right">Action</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {loading && history.length === 0 ? (
                      <TableRow>
                        <TableCell colSpan={10} className="text-center py-8">Loading history...</TableCell>
                      </TableRow>
                    ) : history.length === 0 ? (
                      <TableRow>
                        <TableCell colSpan={10} className="text-center py-8 text-muted-foreground">No import history found.</TableCell>
                      </TableRow>
                    ) : (
                      history.map((batch) => (
                        <TableRow key={batch.batchId} className="hover:bg-muted/50 transition-colors">
                          <TableCell className="font-medium font-mono text-sm">{batch.batchId}</TableCell>
                          <TableCell>{batch.uploadedBy}</TableCell>
                          <TableCell className="text-muted-foreground">{new Date(batch.uploadedAt).toLocaleString()}</TableCell>
                          <TableCell>{getStatusBadge(batch.status)}</TableCell>
                          <TableCell className="text-right font-medium">{batch.totalRows}</TableCell>
                          <TableCell className="text-right text-emerald-500 font-medium">{batch.validRows}</TableCell>
                          <TableCell className="text-right text-destructive font-medium">{batch.errorRows}</TableCell>
                          <TableCell className="text-right text-amber-500 font-medium">{batch.reviewRequiredRows}</TableCell>
                          <TableCell className="text-right text-muted-foreground">{formatDuration(batch.durationMs)}</TableCell>
                          <TableCell className="text-right">
                            <Button variant="ghost" size="sm" onClick={() => viewBatchDetails(batch.batchId)}>View Details</Button>
                          </TableCell>
                        </TableRow>
                      ))
                    )}
                  </TableBody>
                </Table>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="details" className="mt-6 space-y-6">
          {selectedBatch && (
            <>
              <div className="grid grid-cols-4 gap-6">
                <Card className="border-none shadow-sm bg-card/50 backdrop-blur">
                  <CardHeader className="pb-2">
                    <CardDescription>Total Rows</CardDescription>
                    <CardTitle className="text-4xl font-light">{selectedBatch.totalRows}</CardTitle>
                  </CardHeader>
                </Card>
                <Card className="border-none shadow-sm bg-emerald-500/5">
                  <CardHeader className="pb-2">
                    <CardDescription className="text-emerald-600/80">Ready to Commit</CardDescription>
                    <CardTitle className="text-4xl font-light text-emerald-600">{selectedBatch.validRows}</CardTitle>
                  </CardHeader>
                </Card>
                <Card className="border-none shadow-sm bg-destructive/5">
                  <CardHeader className="pb-2">
                    <CardDescription className="text-destructive/80">Validation Errors</CardDescription>
                    <CardTitle className="text-4xl font-light text-destructive">{selectedBatch.errorRows}</CardTitle>
                  </CardHeader>
                </Card>
                <Card className="border-none shadow-sm bg-amber-500/5">
                  <CardHeader className="pb-2">
                    <CardDescription className="text-amber-600/80">Review Required</CardDescription>
                    <CardTitle className="text-4xl font-light text-amber-600">{selectedBatch.reviewRequiredRows}</CardTitle>
                  </CardHeader>
                </Card>
              </div>

              <Card className="border-none shadow-sm bg-card/50 backdrop-blur">
                <CardHeader className="flex flex-row items-center justify-between pb-2">
                  <div>
                    <CardTitle className="flex items-center gap-2">
                      <Activity className="w-5 h-5 text-primary" />
                      Staging Preview
                    </CardTitle>
                    <CardDescription>Batch: {selectedBatch.batchId}</CardDescription>
                  </div>
                  {selectedBatch.status === "Staged" && (
                    <Button disabled className="shadow-sm">Commit to Core Database (Phase 2)</Button>
                  )}
                </CardHeader>
                <CardContent>
                  <div className="rounded-xl border bg-card overflow-hidden">
                    <Table>
                      <TableHeader className="bg-muted/50">
                        <TableRow>
                          <TableHead className="w-16">Row</TableHead>
                          <TableHead>Project Name</TableHead>
                          <TableHead>SKU</TableHead>
                          <TableHead>PVR Target</TableHead>
                          <TableHead>PIC</TableHead>
                          <TableHead>Status</TableHead>
                          <TableHead>Details</TableHead>
                        </TableRow>
                      </TableHeader>
                      <TableBody>
                        {stagingRecords.map((record) => (
                          <TableRow key={record.id} className="hover:bg-muted/50">
                            <TableCell className="font-mono text-muted-foreground">{record.rawRowNumber}</TableCell>
                            <TableCell className="font-medium">{record.projectName}</TableCell>
                            <TableCell>{record.sku}</TableCell>
                            <TableCell>{record.pvrTargetDate ? new Date(record.pvrTargetDate).toLocaleDateString() : "-"}</TableCell>
                            <TableCell>{record.hwPic}</TableCell>
                            <TableCell>{getRowStatusBadge(record.rowStatus)}</TableCell>
                            <TableCell className="text-sm">
                              {record.rowStatus === "ValidationError" && <span className="text-destructive">{record.validationMessage}</span>}
                              {record.rowStatus === "ReviewRequired" && <span className="text-amber-500">{record.coreValidationMessage}</span>}
                            </TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </div>
                </CardContent>
              </Card>
            </>
          )}
        </TabsContent>

        <TabsContent value="errors" className="mt-6">
          <Card className="border-none shadow-sm bg-card/50 backdrop-blur">
            <CardHeader>
              <CardTitle>Validation Errors</CardTitle>
              <CardDescription>System-level rejection details</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="text-center py-12 text-muted-foreground">
                Detailed validation error queue interface will be implemented in Phase 2.
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="review" className="mt-6">
          <Card className="border-none shadow-sm bg-card/50 backdrop-blur">
            <CardHeader>
              <CardTitle>Business Review Queue</CardTitle>
              <CardDescription>Logical conflicts requiring human intervention</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="text-center py-12 text-muted-foreground">
                Business review resolution interface will be implemented in Phase 2.
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="mapping" className="mt-6">
          <Card className="border-none shadow-sm bg-card/50 backdrop-blur">
            <CardHeader>
              <CardTitle>Mapping Dictionary</CardTitle>
              <CardDescription>System aliases and canonical fields</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="text-center py-12 text-muted-foreground">
                Mapping dictionary management will be implemented in Phase 2.
              </div>
            </CardContent>
          </Card>
        </TabsContent>

      </Tabs>
    </div>
  );
}
