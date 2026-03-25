import { Package } from "lucide-react";
import { useState } from "react";
import { cn } from "@/components/ui/utils";

interface PackageIconProps {
  url: string | null | undefined;
  alt: string;
  size?: number;
  className?: string;
}

export function PackageIcon({ url, alt, size = 48, className }: PackageIconProps) {
  const [failed, setFailed] = useState(false);

  const containerStyle = { width: size, height: size, minWidth: size, minHeight: size };

  if (!url || failed) {
    return (
      <div
        className={cn(
          "flex items-center justify-center rounded-md bg-[var(--bg-muted)]",
          className,
        )}
        style={containerStyle}
      >
        <Package size={size * 0.5} className="text-[var(--fg-muted)]" />
      </div>
    );
  }

  return (
    <div
      className={cn("flex items-center justify-center overflow-hidden rounded-md", className)}
      style={containerStyle}
    >
      <img
        src={url}
        alt={alt}
        className="h-full w-full object-contain"
        onError={() => setFailed(true)}
      />
    </div>
  );
}
