import { useState } from "react";
import ReactMarkdown from "react-markdown";
import rehypeSanitize from "rehype-sanitize";
import { ChevronDown, ChevronUp } from "lucide-react";
import { Button } from "@/components/ui/button";

interface ReadmeSectionProps {
  markdown: string;
}

export function ReadmeSection({ markdown }: ReadmeSectionProps) {
  const [expanded, setExpanded] = useState(false);

  return (
    <div className="mt-6">
      <h3 className="mb-3 text-lg font-semibold">README</h3>
      <div
        className={`relative overflow-hidden rounded-lg border border-[var(--border)] bg-[var(--bg-card)] p-6 ${
          expanded ? "" : "max-h-96"
        }`}
      >
        <div className="prose prose-sm dark:prose-invert max-w-none">
          <ReactMarkdown rehypePlugins={[rehypeSanitize]}>
            {markdown}
          </ReactMarkdown>
        </div>
        {!expanded && (
          <div className="absolute inset-x-0 bottom-0 h-24 bg-gradient-to-t from-[var(--bg-card)] to-transparent" />
        )}
      </div>
      <div className="mt-2 text-center">
        <Button variant="ghost" size="sm" onClick={() => setExpanded(!expanded)}>
          {expanded ? (
            <>
              <ChevronUp size={16} />
              Show less
            </>
          ) : (
            <>
              <ChevronDown size={16} />
              Show more
            </>
          )}
        </Button>
      </div>
    </div>
  );
}
