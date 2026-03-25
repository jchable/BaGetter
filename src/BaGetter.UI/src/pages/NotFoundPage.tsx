import { Link } from "react-router";
import { FileQuestion } from "lucide-react";
import { Button } from "@/components/ui/button";

export function NotFoundPage() {
  return (
    <div className="flex flex-col items-center justify-center py-20 text-center">
      <FileQuestion size={48} className="mb-4 text-[var(--fg-muted)]" />
      <h1 className="text-2xl font-bold">Page Not Found</h1>
      <p className="mt-2 text-[var(--fg-muted)]">
        The page you are looking for does not exist.
      </p>
      <Link to="/" className="mt-6">
        <Button>Go Home</Button>
      </Link>
    </div>
  );
}
