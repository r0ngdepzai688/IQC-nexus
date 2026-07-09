"use client";

import React from "react";
import { MasterPlanRecord } from "@/lib/mock-data/newModelsMock";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { AlertCircle, PlayCircle, Clock } from "lucide-react";

interface CandidatePoolTableProps {
  records: MasterPlanRecord[];
  onActivate: (id: number) => void;
}

export function CandidatePoolTable({ records, onActivate }: CandidatePoolTableProps) {
  
  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'Urgent':
        return <Badge variant="destructive" className="gap-1"><AlertCircle className="w-3 h-3" /> Urgent</Badge>;
      case 'Ready':
        return <Badge variant="default" className="bg-emerald-500/10 text-emerald-500 hover:bg-emerald-500/20 gap-1 border-none"><PlayCircle className="w-3 h-3" /> Ready</Badge>;
      case 'Future':
      default:
        return <Badge variant="secondary" className="gap-1 text-muted-foreground"><Clock className="w-3 h-3" /> Future</Badge>;
    }
  };

  return (
    <div className="rounded-xl border border-border/50 bg-card/50 backdrop-blur-sm overflow-hidden">
      <Table>
        <TableHeader>
          <TableRow className="bg-muted/50 hover:bg-muted/50">
            <TableHead>Project Name</TableHead>
            <TableHead>Basic / SKU</TableHead>
            <TableHead>Region / Grade</TableHead>
            <TableHead>Category</TableHead>
            <TableHead>Q'ty (LPR/LSR)</TableHead>
            <TableHead>PVR Target</TableHead>
            <TableHead>PIC (IQC)</TableHead>
            <TableHead>Status</TableHead>
            <TableHead className="text-right">Action</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {records.length === 0 ? (
            <TableRow>
              <TableCell colSpan={9} className="text-center h-32 text-muted-foreground">
                No candidates available. Please upload a Master Plan.
              </TableCell>
            </TableRow>
          ) : (
            records.map((record) => (
              <TableRow key={record.id} className="group hover:bg-muted/20 transition-colors">
                <TableCell className="font-medium">{record.projectName}</TableCell>
                <TableCell>
                  <div className="flex flex-col">
                    <span className="text-sm">{record.basic}</span>
                    <span className="text-xs text-muted-foreground">{record.sku}</span>
                  </div>
                </TableCell>
                <TableCell>
                  <div className="flex flex-col">
                    <span>{record.areaRegion}</span>
                    <span className="text-xs text-muted-foreground">{record.grade}</span>
                  </div>
                </TableCell>
                <TableCell>
                  <Badge variant="outline" className={record.category === 'LPR' ? 'border-primary/50 text-primary' : 'border-muted text-muted-foreground'}>
                    {record.category}
                  </Badge>
                </TableCell>
                <TableCell>
                  <div className="flex gap-2">
                    <span title="LPR Qty" className="text-sm">{record.qtyLpr}</span>
                    <span className="text-muted-foreground">/</span>
                    <span title="LSR Qty" className="text-sm text-muted-foreground">{record.qtyLsr}</span>
                  </div>
                </TableCell>
                <TableCell>
                  {record.pvrTargetDate || '-'}
                </TableCell>
                <TableCell>
                  {record.picIqc || '-'}
                </TableCell>
                <TableCell>
                  {getStatusBadge(record.actionStatus)}
                </TableCell>
                <TableCell className="text-right">
                  <Button 
                    size="sm" 
                    onClick={() => onActivate(record.id)}
                    className="opacity-0 group-hover:opacity-100 transition-opacity"
                    disabled={record.isActivated}
                  >
                    Activate
                  </Button>
                </TableCell>
              </TableRow>
            ))
          )}
        </TableBody>
      </Table>
    </div>
  );
}
