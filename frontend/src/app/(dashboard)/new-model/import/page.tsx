"use client";

import { useState, useEffect } from "react";
import { 
  Database, UploadCloud, FileSpreadsheet, CheckCircle2, AlertTriangle, 
  ChevronRight, ArrowRight, ShieldCheck, Download, ListChecks, ArrowLeft
} from "lucide-react";
import { motion, AnimatePresence } from "framer-motion";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";

export default function ManualImportPage() {
  const [step, setStep] = useState<1 | 2 | 3 | 4>(1);
  const [file, setFile] = useState<File | null>(null);
  const [isUploading, setIsUploading] = useState(false);
  const [uploadProgress, setUploadProgress] = useState(0);
  const [batchData, setBatchData] = useState<any>(null); // eslint-disable-line @typescript-eslint/no-explicit-any
  const [serverFiles, setServerFiles] = useState<any[]>([]);
  
  // Step 2: Fetch Manual Files
  useEffect(() => {
    if (step === 2) {
      fetch("http://localhost:5000/api/datahub/manual-files")
        .then(r => r.json())
        .then(data => setServerFiles(data))
        .catch(console.error);
    }
  }, [step]);

  // Step 2: Handle Server File Process
  const handleProcessManualFile = async (filename: string) => {
    setIsUploading(true);
    setUploadProgress(10);
    
    const progressInterval = setInterval(() => {
      setUploadProgress(prev => Math.min(prev + 15, 90));
    }, 500);

    try {
      const res = await fetch(`http://localhost:5000/api/datahub/process-manual?fileName=${encodeURIComponent(filename)}`, {
        method: "POST"
      });
      
      clearInterval(progressInterval);
      setUploadProgress(100);
      
      if (!res.ok) throw new Error("Processing failed");
      
      const data = await res.json();
      setBatchData(data);
      
      setTimeout(() => {
        setIsUploading(false);
        setStep(3);
      }, 800);
      
    } catch (err) {
      clearInterval(progressInterval);
      setIsUploading(false);
      alert("Error processing server file.");
    }
  };
  
  // Step 2: Handle File Upload
  const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const selected = e.target.files?.[0];
    if (!selected) return;
    
    setFile(selected);
    setIsUploading(true);
    setUploadProgress(10);
    
    const formData = new FormData();
    formData.append("file", selected);
    formData.append("module", "NewModels");
    
    // Simulate upload progress
    const progressInterval = setInterval(() => {
      setUploadProgress(prev => Math.min(prev + 15, 90));
    }, 500);

    try {
      const res = await fetch("http://localhost:5000/api/datahub/upload", {
        method: "POST",
        body: formData,
        // No Auth header for now as requested
      });
      
      clearInterval(progressInterval);
      setUploadProgress(100);
      
      if (!res.ok) {
        throw new Error("Upload failed");
      }
      
      const data = await res.json();
      setBatchData(data);
      
      setTimeout(() => {
        setIsUploading(false);
        setStep(3);
      }, 800);
      
    } catch (err) {
      clearInterval(progressInterval);
      setIsUploading(false);
      alert("Error uploading file. Please check backend connection.");
    }
  };

  // Step 4: Confirm Commit
  const handleCommit = async () => {
    if (!batchData) return;
    
    setIsUploading(true);
    try {
      const res = await fetch(`http://localhost:5000/api/datahub/commit/${batchData.batchId}`, {
        method: "POST",
      });
      
      if (!res.ok) {
        throw new Error("Commit failed");
      }
      
      const data = await res.json();
      setBatchData(data);
      setStep(4);
    } catch (err) {
      alert("Error committing batch.");
    } finally {
      setIsUploading(false);
    }
  };

  return (
    <div className="h-full flex flex-col bg-transparent relative z-0 overflow-hidden">
      {/* Ambient Premium Background */}
      <div className="absolute inset-0 overflow-hidden pointer-events-none -z-10 bg-gradient-to-br from-indigo-50/50 via-white to-blue-50/30 dark:from-indigo-950/20 dark:via-[#0B0F17] dark:to-blue-900/10"></div>
      
      <div className="px-8 pt-8 pb-4">
        <div className="flex items-center space-x-2 text-sm font-semibold text-primary mb-3 tracking-wide uppercase">
          <Database className="w-4 h-4" />
          <span>Data Hub</span>
          <ChevronRight className="w-4 h-4 text-gray-300" />
          <span>Manual Import</span>
        </div>
        <h1 className="text-4xl font-black tracking-tight text-foreground">Import Data securely.</h1>
        <p className="text-muted-foreground mt-2 font-medium max-w-2xl text-lg">
          Process, validate, and safely ingest Master Plan data into IQC Nexus. 
        </p>
      </div>

      {/* Stepper */}
      <div className="px-8 py-6">
        <div className="flex items-center w-full max-w-3xl">
          {[
            { num: 1, title: "Source Selection" },
            { num: 2, title: "Upload & Parse" },
            { num: 3, title: "Validation Review" },
            { num: 4, title: "Commit" }
          ].map((s, idx) => (
            <div key={s.num} className="flex items-center">
              <div className="flex items-center space-x-3">
                <div className={`w-8 h-8 rounded-full flex items-center justify-center font-bold text-sm transition-all duration-500 ${
                  step === s.num 
                    ? "bg-primary text-white shadow-lg shadow-primary/30 scale-110" 
                    : step > s.num 
                      ? "bg-emerald-500 text-white" 
                      : "bg-gray-100 dark:bg-gray-800 text-gray-400"
                }`}>
                  {step > s.num ? <CheckCircle2 className="w-5 h-5" /> : s.num}
                </div>
                <span className={`font-semibold hidden sm:block ${step >= s.num ? "text-foreground" : "text-gray-400"}`}>
                  {s.title}
                </span>
              </div>
              {idx < 3 && (
                <div className={`w-12 sm:w-24 h-[2px] mx-4 transition-all duration-500 ${step > s.num ? "bg-emerald-500" : "bg-gray-100 dark:bg-gray-800"}`} />
              )}
            </div>
          ))}
        </div>
      </div>

      <div className="flex-1 overflow-y-auto px-8 pb-12 custom-scrollbar">
        <AnimatePresence mode="wait">
          
          {/* STEP 1: SOURCE SELECTION */}
          {step === 1 && (
            <motion.div key="step1" initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, y: -10 }}>
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 mt-4">
                <Card 
                  onClick={() => setStep(2)}
                  className="rounded-[2rem] border-2 border-primary/20 hover:border-primary shadow-lg shadow-primary/5 cursor-pointer bg-white dark:bg-[#121826] transition-all hover:scale-[1.02] overflow-hidden group"
                >
                  <div className="p-8">
                    <div className="w-14 h-14 bg-indigo-50 dark:bg-indigo-900/30 rounded-2xl flex items-center justify-center mb-6 group-hover:bg-primary transition-colors">
                      <FileSpreadsheet className="w-7 h-7 text-primary group-hover:text-white transition-colors" />
                    </div>
                    <h3 className="text-xl font-bold text-foreground mb-2">New Models Master Plan</h3>
                    <p className="text-muted-foreground font-medium mb-6">
                      Import the official R&D Master Plan Excel file. Triggers automated core mapping and business rules.
                    </p>
                    <div className="flex items-center text-primary font-bold">
                      Select Source <ArrowRight className="w-4 h-4 ml-2 group-hover:translate-x-1 transition-transform" />
                    </div>
                  </div>
                </Card>

                <Card className="rounded-[2rem] border-2 border-transparent border-dashed cursor-not-allowed bg-gray-50/50 dark:bg-white/5 opacity-60">
                  <div className="p-8 flex flex-col items-center justify-center text-center h-full">
                    <ListChecks className="w-10 h-10 text-gray-400 mb-4" />
                    <h3 className="text-lg font-bold text-gray-500 mb-2">Inspection Standards</h3>
                    <Badge variant="outline" className="text-gray-400">Coming Soon</Badge>
                  </div>
                </Card>
              </div>
            </motion.div>
          )}

          {/* STEP 2: UPLOAD & PARSE */}
          {step === 2 && (
            <motion.div key="step2" initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, y: -10 }}>
              <Button variant="ghost" onClick={() => setStep(1)} className="mb-6 -ml-4 text-muted-foreground hover:text-foreground">
                <ArrowLeft className="w-4 h-4 mr-2" /> Back to Source Selection
              </Button>
              
              <Card className="rounded-[2rem] border-0 shadow-xl shadow-gray-200/50 dark:shadow-none bg-white dark:bg-[#121826] overflow-hidden max-w-3xl">
                <div className="p-10 flex flex-col items-center justify-center border-2 border-dashed border-gray-200 dark:border-white/10 m-6 rounded-3xl bg-gray-50/50 dark:bg-white/[0.02]">
                  
                  {isUploading ? (
                    <div className="w-full max-w-md text-center py-8">
                      <div className="w-20 h-20 bg-blue-50 dark:bg-blue-900/20 rounded-full flex items-center justify-center mx-auto mb-6 relative">
                        <UploadCloud className="w-10 h-10 text-primary animate-bounce" />
                        <svg className="absolute inset-0 w-full h-full transform -rotate-90">
                          <circle cx="40" cy="40" r="38" className="stroke-gray-200 dark:stroke-gray-800" strokeWidth="4" fill="none" />
                          <circle cx="40" cy="40" r="38" className="stroke-primary transition-all duration-300" strokeWidth="4" fill="none" strokeDasharray="238" strokeDashoffset={238 - (238 * uploadProgress) / 100} strokeLinecap="round" />
                        </svg>
                      </div>
                      <h3 className="text-xl font-bold text-foreground mb-2">Processing File...</h3>
                      <p className="text-muted-foreground font-medium mb-6">Parsing rows and running validation engines.</p>
                      <div className="w-full bg-gray-200 dark:bg-gray-800 rounded-full h-2">
                        <div className="bg-primary h-2 rounded-full transition-all duration-300" style={{ width: `${uploadProgress}%` }}></div>
                      </div>
                    </div>
                  ) : (
                    <>
                      <div className="w-20 h-20 bg-indigo-50 dark:bg-indigo-900/30 text-primary rounded-full flex items-center justify-center mb-6 shadow-sm ring-4 ring-indigo-50 dark:ring-indigo-900/10">
                        <UploadCloud className="w-10 h-10" />
                      </div>
                      <h3 className="text-2xl font-black text-foreground mb-2">Upload Master Plan</h3>
                      <p className="text-muted-foreground font-medium mb-8 text-center max-w-md">
                        Drag and drop your .xlsx file here, or click to browse. Max size 50MB.
                      </p>
                      
                      <div className="relative">
                        <input 
                          type="file" 
                          accept=".xlsx,.xls,.xlsm" 
                          className="absolute inset-0 w-full h-full opacity-0 cursor-pointer"
                          onChange={handleFileUpload}
                        />
                        <Button className="rounded-xl shadow-lg shadow-primary/20 bg-primary hover:bg-primary/90 text-white font-bold h-12 px-8">
                          Select Local File
                        </Button>
                      </div>
                      <a href="#" className="mt-6 text-sm text-indigo-500 hover:underline font-semibold flex items-center">
                        <Download className="w-4 h-4 mr-1" /> Download Template
                      </a>

                      {/* Naming Convention Suggestion */}
                      <div className="mt-6 mb-2 text-center text-sm text-gray-500 font-medium max-w-md bg-gray-50/50 dark:bg-white/5 p-3 rounded-lg border border-gray-100 dark:border-white/10">
                        <span className="block text-xs font-bold uppercase tracking-wider mb-1 text-gray-400">Recommended Format</span>
                        <code className="text-primary bg-primary/10 px-2 py-0.5 rounded">MasterPlan_YYYYMMDD_vNN_Source.xlsx</code>
                      </div>

                      {serverFiles.length > 0 && (
                        <div className="w-full max-w-lg mt-8">
                          <h4 className="text-xs font-bold text-gray-400 dark:text-gray-500 uppercase tracking-wider mb-3 flex items-center justify-center">
                            — Or process server files (ManualUpload) —
                          </h4>
                          <div className="space-y-3">
                            {serverFiles.map(f => {
                              const isExactDuplicate = f.duplicateStatus === "ExactDuplicate";
                              const isDuplicateContent = f.duplicateStatus === "DuplicateContent";
                              return (
                                <div key={f.fileName} className="flex flex-col p-4 bg-white dark:bg-white/5 border border-gray-200 dark:border-white/10 rounded-xl hover:border-primary/50 transition-colors shadow-sm">
                                  <div className="flex items-start justify-between mb-2">
                                    <div className="flex flex-col min-w-0 pr-4">
                                      <span className="text-sm font-bold truncate text-foreground">{f.fileName}</span>
                                      <div className="flex items-center space-x-3 mt-1 text-xs text-gray-500 font-medium">
                                        <span>{(f.sizeBytes / 1024).toFixed(1)} KB</span>
                                        <span>•</span>
                                        <span>{new Date(f.lastModifiedUtc).toLocaleString()}</span>
                                      </div>
                                    </div>
                                    <Button 
                                      size="sm" 
                                      disabled={isExactDuplicate}
                                      onClick={() => handleProcessManualFile(f.fileName)} 
                                      className="rounded-lg text-xs shrink-0 font-bold bg-indigo-600 hover:bg-indigo-700 text-white disabled:opacity-50"
                                    >
                                      {isExactDuplicate ? "Duplicate" : "Process"}
                                    </Button>
                                  </div>
                                  
                                  {isExactDuplicate && (
                                    <div className="flex items-center text-xs font-bold text-rose-500 bg-rose-50 dark:bg-rose-900/20 px-2 py-1.5 rounded mt-1">
                                      <AlertTriangle className="w-3.5 h-3.5 mr-1.5" /> Exact file content already imported
                                    </div>
                                  )}
                                  {isDuplicateContent && (
                                    <div className="flex items-center text-xs font-bold text-orange-500 bg-orange-50 dark:bg-orange-900/20 px-2 py-1.5 rounded mt-1">
                                      <AlertTriangle className="w-3.5 h-3.5 mr-1.5" /> Warning: Same filename, different content
                                    </div>
                                  )}
                                </div>
                              );
                            })}
                          </div>
                        </div>
                      )}
                    </>
                  )}
                </div>
              </Card>
            </motion.div>
          )}

          {/* STEP 3: VALIDATION REVIEW */}
          {step === 3 && batchData && (
            <motion.div key="step3" initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, y: -10 }}>
              <div className="flex justify-between items-end mb-6">
                <div>
                  <h2 className="text-2xl font-black text-foreground">Validation Results</h2>
                  <p className="text-muted-foreground font-medium">Batch ID: <span className="font-mono text-xs ml-1 bg-gray-100 dark:bg-white/10 px-2 py-1 rounded">{batchData.batchId}</span></p>
                </div>
                <Button 
                  onClick={handleCommit}
                  disabled={batchData.validRows === 0 || isUploading}
                  className="rounded-xl shadow-lg shadow-emerald-500/20 bg-emerald-600 hover:bg-emerald-700 text-white font-bold h-11 px-8"
                >
                  {isUploading ? "Committing..." : "Confirm & Commit"}
                </Button>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-8">
                <Card className="rounded-[1.5rem] border-0 shadow-sm bg-white dark:bg-[#121826] p-5">
                  <div className="text-xs font-bold text-gray-500 uppercase tracking-wider mb-2">Total Rows</div>
                  <div className="text-3xl font-black text-foreground">{batchData.totalRows}</div>
                </Card>
                <Card className="rounded-[1.5rem] border-0 shadow-sm bg-white dark:bg-[#121826] p-5 border-b-4 border-b-emerald-500">
                  <div className="text-xs font-bold text-gray-500 uppercase tracking-wider mb-2 flex items-center justify-between">
                    Valid & Ready <ShieldCheck className="w-4 h-4 text-emerald-500" />
                  </div>
                  <div className="text-3xl font-black text-emerald-600">{batchData.validRows}</div>
                </Card>
                <Card className="rounded-[1.5rem] border-0 shadow-sm bg-white dark:bg-[#121826] p-5 border-b-4 border-b-orange-500">
                  <div className="text-xs font-bold text-gray-500 uppercase tracking-wider mb-2 flex items-center justify-between">
                    Review Required <ListChecks className="w-4 h-4 text-orange-500" />
                  </div>
                  <div className="text-3xl font-black text-orange-600">{batchData.reviewRequiredRows}</div>
                </Card>
                <Card className="rounded-[1.5rem] border-0 shadow-sm bg-white dark:bg-[#121826] p-5 border-b-4 border-b-rose-500">
                  <div className="text-xs font-bold text-gray-500 uppercase tracking-wider mb-2 flex items-center justify-between">
                    Critical Errors <AlertTriangle className="w-4 h-4 text-rose-500" />
                  </div>
                  <div className="text-3xl font-black text-rose-600">{batchData.errorRows}</div>
                </Card>
              </div>
              
              {/* Note about UI */}
              <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-100 dark:border-blue-900/50 rounded-xl p-4 flex items-start space-x-3 mb-8">
                <ShieldCheck className="w-5 h-5 text-blue-600 mt-0.5" />
                <div>
                  <h4 className="font-bold text-blue-900 dark:text-blue-100">Safe Preview Mode</h4>
                  <p className="text-sm text-blue-700 dark:text-blue-300 font-medium mt-1">
                    Your data is currently in the Staging area. No core database tables have been modified. 
                    Only rows marked as Valid will be committed. You can resolve errors in the Review Queue after committing.
                  </p>
                </div>
              </div>
            </motion.div>
          )}

          {/* STEP 4: SUCCESS */}
          {step === 4 && batchData && (
            <motion.div key="step4" initial={{ opacity: 0, scale: 0.95 }} animate={{ opacity: 1, scale: 1 }} className="flex flex-col items-center justify-center py-20 text-center">
              <div className="w-24 h-24 bg-emerald-50 dark:bg-emerald-900/20 text-emerald-500 rounded-full flex items-center justify-center mb-6 shadow-xl shadow-emerald-500/10">
                <CheckCircle2 className="w-12 h-12" />
              </div>
              <h2 className="text-4xl font-black text-foreground mb-4">Commit Successful</h2>
              <p className="text-muted-foreground font-medium max-w-md text-lg mb-8">
                Successfully inserted {batchData.createdRecords} new projects and updated {batchData.updatedRecords} existing projects into IQC Nexus.
              </p>
              
              <div className="flex space-x-4">
                <Button variant="outline" onClick={() => { setStep(1); setBatchData(null); }} className="rounded-xl border-gray-200 font-bold h-12 px-6">
                  Import Another File
                </Button>
                <Button onClick={() => window.location.href = '/new-model'} className="rounded-xl shadow-lg shadow-primary/20 bg-primary text-white font-bold h-12 px-6">
                  View Master Plan <ArrowRight className="w-4 h-4 ml-2" />
                </Button>
              </div>
            </motion.div>
          )}

        </AnimatePresence>
      </div>
    </div>
  );
}
