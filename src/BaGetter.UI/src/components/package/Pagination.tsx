import { ChevronLeft, ChevronRight } from "lucide-react";
import { Button } from "@/components/ui/button";

interface PaginationProps {
  page: number;
  hasMore: boolean;
  onPageChange: (page: number) => void;
}

export function Pagination({ page, hasMore, onPageChange }: PaginationProps) {
  if (page <= 1 && !hasMore) return null;

  return (
    <div className="flex items-center justify-center gap-2 pt-4">
      <Button
        variant="outline"
        size="sm"
        disabled={page <= 1}
        onClick={() => onPageChange(page - 1)}
      >
        <ChevronLeft size={16} />
        Previous
      </Button>
      <span className="px-3 text-sm text-[var(--fg-muted)]">Page {page}</span>
      <Button
        variant="outline"
        size="sm"
        disabled={!hasMore}
        onClick={() => onPageChange(page + 1)}
      >
        Next
        <ChevronRight size={16} />
      </Button>
    </div>
  );
}
