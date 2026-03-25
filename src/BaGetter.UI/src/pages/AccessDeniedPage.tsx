import { Link } from "react-router";
import { ShieldX } from "lucide-react";
import { Button } from "@/components/ui/button";

export function AccessDeniedPage() {
  return (
    <div className="flex flex-col items-center justify-center py-20 text-center">
      <ShieldX size={48} className="mb-4 text-error" />
      <h1 className="text-2xl font-bold">Access Denied</h1>
      <p className="mt-2 text-[var(--fg-muted)]">
        You do not have permission to access this page.
      </p>
      <Link to="/" className="mt-6">
        <Button>Go Home</Button>
      </Link>
    </div>
  );
}
