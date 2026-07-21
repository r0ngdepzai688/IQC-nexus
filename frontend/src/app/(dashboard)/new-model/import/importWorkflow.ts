import type { HeaderInspection, HeaderMapping, ImportBatch, ImportReviewRow, ImportReviewSummary } from "@/lib/api/dataHubApi";

export const MAX_MASTER_PLAN_BYTES = 50 * 1024 * 1024;
const SUPPORTED_EXTENSIONS = [".xlsx", ".xls", ".xlsm"];

export function validateMasterPlanFile(file: Pick<File, "name" | "size">): string | null {
  if (!SUPPORTED_EXTENSIONS.some(extension => file.name.toLowerCase().endsWith(extension))) {
    return "Select an .xlsx, .xls, or .xlsm workbook.";
  }
  if (file.size === 0) return "The selected workbook is empty.";
  if (file.size > MAX_MASTER_PLAN_BYTES) return "The selected workbook exceeds the 50 MB limit.";
  return null;
}

export function buildHeaderMappings(selections: Record<number, string>, inspection?: HeaderInspection): HeaderMapping[] {
  return Object.entries(selections)
    .filter(([, canonicalField]) => canonicalField.length > 0)
    .map(([columnIndex, canonicalField]) => {
      const column = inspection?.columns.find(value => value.columnIndex === Number(columnIndex));
      return { columnIndex: Number(columnIndex), canonicalField, normalizedHeaderPath: column?.effectiveHeaderPath, confirmLearning: Boolean(inspection), workbookFingerprint: inspection?.workbookFingerprint, detectedDataType: column?.detectedDataType };
    });
}

export function getMappingIssues(inspection: HeaderInspection, selections: Record<number, string>): string[] {
  const mappings = buildHeaderMappings(selections);
  const canonicalCounts = new Map<string, number>();
  for (const mapping of mappings) canonicalCounts.set(mapping.canonicalField, (canonicalCounts.get(mapping.canonicalField) ?? 0) + 1);
  const duplicateMappings = [...canonicalCounts].filter(([, count]) => count > 1).map(([field]) => field);
  const mappedFields = new Set(mappings.map(mapping => mapping.canonicalField));
  const missingRequired = inspection.requiredFields.filter(field => !mappedFields.has(field));
  const unresolvedAmbiguous = inspection.columns.filter(column => column.ambiguous && !selections[column.columnIndex]);
  const issues: string[] = [];
  if (missingRequired.length) issues.push(`Missing required mappings: ${missingRequired.join(", ")}.`);
  if (duplicateMappings.length) issues.push(`Duplicate canonical mappings: ${duplicateMappings.join(", ")}.`);
  if (unresolvedAmbiguous.length) issues.push(`Resolve ambiguous columns: ${unresolvedAmbiguous.map(column => column.header).join(", ")}.`);
  return issues;
}

export function canCommitImport(batch: ImportBatch | null, review: ImportReviewSummary | null, requestRunning: boolean): boolean {
  return Boolean(
    batch && review && !requestRunning && batch.status === "Staged" && (batch.validRows > 0 || review.noChangeRows > 0) &&
    batch.errorRows === 0 && batch.reviewRequiredRows === 0 &&
    review.errorRows === 0 && review.warningRows === 0 && review.existingBusinessKeyConflicts === 0,
  );
}

export function commitResultMessage(batch: ImportBatch): string {
  const skipped = batch.skippedRows === 1 ? "1 row was skipped with no change" : `${batch.skippedRows} rows were skipped with no change`;
  return `${batch.createdRecords} record(s) inserted, ${batch.updatedRecords} updated. ${skipped}.`;
}

export function reviewRowMessage(row: ImportReviewRow): string {
  if (row.message?.trim()) return row.message.trim();
  if (row.field === "HwPic" && row.currentValue?.trim()) return `Unknown PIC '${row.currentValue.trim()}'.`;
  if (row.conflictType === "NoChange" || row.status === "SkipNoChange") return "No changes detected for this Basic + Cat.";
  if (row.status === "ReadyToInsert") return "Ready to insert.";
  if (row.status === "ReadyToUpdate") return "Ready to update.";
  if (row.status === "Skipped") return "Row was skipped.";
  return row.field && row.field !== "Row"
    ? `${row.field} requires review.`
    : `Row ${row.rowNumber} requires business review.`;
}
