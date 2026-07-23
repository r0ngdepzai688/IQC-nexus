import { ShieldX } from "lucide-react";
import Link from "next/link";
export default function UnauthorizedPage() {
  return <main className="standalone-state"><ShieldX /><p className="eyebrow">ACCESS RESTRICTED</p><h1>Permission required</h1><p>You are signed in, but your account does not have permission to open this resource.</p><Link className="primary-action" href="/overview">Return to dashboard</Link></main>;
}
