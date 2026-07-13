"use client";

import React, { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { Card, CardHeader, CardTitle, CardContent, CardDescription } from "@/components/ui/card";
import { Table, TableHeader, TableRow, TableHead, TableBody, TableCell } from "@/components/ui/table";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { getManualFiles, processManualUpload, ManualFile } from "@/lib/api/dataHubApi";
import { FileUp, FileClock, AlertCircle, CheckCircle2 } from "lucide-react";

export default function MasterPlanImportPage() {
  const router = useRouter();
  const [files, setFiles] = useState<ManualFile[]>([]);
  const [loading, setLoading] = useState(true);
  const [processingFile, setProcessingFile] = useState<string | null>(null);

  useEffect(() => {
    fetchFiles();
  }, []);

  const fetchFiles = async () => {
    try {
      const data = await getManualFiles();
      setFiles(data);
    } catch (error) {
      console.error("Failed to fetch files:", error);
    } finally {
      setLoading(false);
    }
  };

  const handleProcess = async (fileName: string) => {
    setProcessingFile(fileName);
    try {
      const batch = await processManualUpload(fileName);
      router.push(`/support/data-hub?tab=history&batchId=${batch.batchId}`);
    } catch (error) {
      console.error("Failed to process file:", error);
      alert("Failed to process file. Check console for details.");
    } finally {
      setProcessingFile(null);
    }
  };

  const formatSize = (bytes: number) => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  return (
    <div className="p-8 max-w-[1600px] mx-auto space-y-8 animate-in fade-in duration-500">
      <div className="flex justify-between items-end">
        <div>
          <h1 className="text-3xl font-bold tracking-tight mb-2">Import Master Plan</h1>
          <p className="text-muted-foreground text-lg">Manual file ingestion pipeline for New Models</p>
        </div>
        <Button variant="outline" className="gap-2">
          <FileUp className="w-4 h-4" />
          Upload New File
        </Button>
      </div>

      <Card className="border-none shadow-sm bg-card/50 backdrop-blur">
        <CardHeader>
          <CardTitle className="text-xl flex items-center gap-2">
            <FileClock className="w-5 h-5 text-primary" />
            Pending Files
          </CardTitle>
          <CardDescription>Files detected in the ManualUpload drop zone</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="rounded-xl border bg-card overflow-hidden">
            <Table>
              <TableHeader className="bg-muted/50">
                <TableRow>
                  <TableHead className="font-semibold">File Name</TableHead>
                  <TableHead className="font-semibold">Size</TableHead>
                  <TableHead className="font-semibold">Modified At</TableHead>
                  <TableHead className="font-semibold">Status</TableHead>
                  <TableHead className="text-right font-semibold">Action</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading ? (
                  <TableRow>
                    <TableCell colSpan={5} className="text-center py-8 text-muted-foreground">
                      Scanning drop zone...
                    </TableCell>
                  </TableRow>
                ) : files.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={5} className="text-center py-8 text-muted-foreground">
                      No files found in ManualUpload directory.
                    </TableCell>
                  </TableRow>
                ) : (
                  files.map((file) => (
                    <TableRow key={file.fileName} className="group hover:bg-muted/50 transition-colors">
                      <TableCell className="font-medium">{file.fileName}</TableCell>
                      <TableCell className="text-muted-foreground">{formatSize(file.sizeBytes)}</TableCell>
                      <TableCell className="text-muted-foreground">
                        {new Date(file.lastModifiedUtc).toLocaleString()}
                      </TableCell>
                      <TableCell>
                        {file.duplicateStatus === "ExactDuplicate" ? (
                          <Badge variant="destructive" className="gap-1">
                            <AlertCircle className="w-3 h-3" /> Exact Duplicate
                          </Badge>
                        ) : file.duplicateStatus === "DuplicateContent" ? (
                          <Badge variant="secondary" className="bg-amber-500/10 text-amber-500 hover:bg-amber-500/20 border-none gap-1">
                            <AlertCircle className="w-3 h-3" /> Same Content
                          </Badge>
                        ) : (
                          <Badge variant="secondary" className="bg-emerald-500/10 text-emerald-500 hover:bg-emerald-500/20 border-none gap-1">
                            <CheckCircle2 className="w-3 h-3" /> Ready
                          </Badge>
                        )}
                      </TableCell>
                      <TableCell className="text-right">
                        <Button
                          size="sm"
                          disabled={file.duplicateStatus === "ExactDuplicate" || processingFile === file.fileName}
                          onClick={() => handleProcess(file.fileName)}
                          className="shadow-sm"
                        >
                          {processingFile === file.fileName ? "Processing..." : "Process Import"}
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
