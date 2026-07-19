import { redirect } from "next/navigation";

export default function DataHubHistoryRedirect() {
  redirect("/support/data-hub?tab=history");
}
