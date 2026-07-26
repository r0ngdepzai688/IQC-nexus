import { FileQuestion } from "lucide-react";
import Link from "next/link";
export default function NotFound() {
  return <main className="standalone-state"><FileQuestion /><p className="eyebrow">404</p><h1>Page not found</h1><p>The requested IQC Nexus page does not exist or has moved.</p><Link className="primary-action" href="/overview">Return to dashboard</Link></main>;
}
