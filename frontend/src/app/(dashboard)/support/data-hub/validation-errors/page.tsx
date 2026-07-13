"use client";
import { AlertTriangle, Construction } from "lucide-react";

export default function ValidationErrorsPage() {
  return (
    <div className="h-full flex flex-col items-center justify-center p-8 bg-transparent">
      <div className="w-20 h-20 bg-rose-50 dark:bg-rose-900/30 rounded-full flex items-center justify-center mb-6 shadow-sm">
        <Construction className="w-10 h-10 text-rose-500" />
      </div>
      <h1 className="text-3xl font-black text-foreground mb-4">Validation Errors</h1>
      <p className="text-muted-foreground font-medium max-w-md text-center">
        This module is currently under construction. It will display structural errors found during the ingestion phase.
      </p>
    </div>
  );
}
