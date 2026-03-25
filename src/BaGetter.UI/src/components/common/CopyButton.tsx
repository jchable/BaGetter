import { Check, Copy } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useClipboard } from "@/hooks/useClipboard";

interface CopyButtonProps {
  text: string;
  className?: string;
}

export function CopyButton({ text, className }: CopyButtonProps) {
  const { copy, copied } = useClipboard();

  return (
    <Button
      variant="ghost"
      size="icon"
      className={className}
      onClick={() => copy(text)}
      title={copied ? "Copied!" : "Copy to clipboard"}
    >
      {copied ? <Check size={16} /> : <Copy size={16} />}
    </Button>
  );
}
