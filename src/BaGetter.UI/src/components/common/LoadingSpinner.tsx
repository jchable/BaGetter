import { Loader2 } from "lucide-react";
import { cn } from "@/components/ui/utils";

interface LoadingSpinnerProps {
  className?: string;
  size?: number;
}

export function LoadingSpinner({ className, size = 24 }: LoadingSpinnerProps) {
  return (
    <div className={cn("flex items-center justify-center", className)}>
      <Loader2 className="animate-spin text-primary-500" size={size} />
    </div>
  );
}
