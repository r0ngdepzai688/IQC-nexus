import { PermissionGate } from "@/components/portal/PermissionGate";
import { BookOpen, Download, FileText, FileSpreadsheet, MonitorCog, Package } from "lucide-react";
const items = [
 { name: "Applications", detail: "Approved platform applications", status: "No data", icon: Package },
 { name: "Templates", detail: "Controlled import templates", status: "Placeholder", icon: FileSpreadsheet },
 { name: "Documents", detail: "Quality-system documents", status: "Placeholder", icon: FileText },
 { name: "Client Agent", detail: "Reserved category; no implementation", status: "Not available", icon: MonitorCog },
 { name: "Release Notes", detail: "Versioned platform changes", status: "Placeholder", icon: BookOpen },
 { name: "Installation Guides", detail: "Approved deployment guidance", status: "No data", icon: BookOpen },
];
export default function DownloadsPage() { return <PermissionGate permission="download.view"><div className="page-stack"><section className="page-heading"><div><p className="eyebrow">CONTROLLED DISTRIBUTION</p><h2>Download Center</h2><p>Permanent categories for approved platform artifacts.</p></div></section><div className="fixture-banner"><strong>Placeholder provider</strong><span>No files or download endpoints are connected.</span></div><section className="category-grid">{items.map(item => <article className="panel category-card" key={item.name}><item.icon /><div><h3>{item.name}</h3><p>{item.detail}</p></div><span className="status-badge">{item.status}</span><button className="secondary-action" disabled><Download />Download</button></article>)}</section></div></PermissionGate>; }