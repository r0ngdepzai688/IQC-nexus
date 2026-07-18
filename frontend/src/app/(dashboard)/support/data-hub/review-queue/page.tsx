"use client";
import { Construction } from "lucide-react";

export default function ReviewQueuePage() {
  return (
    <div className="h-full flex flex-col items-center justify-center p-8 bg-transparent">
      <div className="w-20 h-20 bg-orange-50 dark:bg-orange-900/30 rounded-full flex items-center justify-center mb-6 shadow-sm">
        <Construction className="w-10 h-10 text-orange-500" />
      </div>
      <h1 className="text-3xl font-black text-foreground mb-4">Business Review Queue</h1>
      <p className="text-muted-foreground font-medium max-w-md text-center">
        This module is currently under construction. It will display business logic conflicts requiring manual resolution (e.g. PIC mismatches).
      </p>
    </div>
  );
}
