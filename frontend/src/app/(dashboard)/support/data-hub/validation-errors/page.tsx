import { redirect } from "next/navigation";

export default function ValidationErrorsRedirect() {
  redirect("/support/data-hub?tab=errors");
}
