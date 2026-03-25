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

  if (!url || failed) {
    return (
      <div
        className={cn(
          "flex items-center justify-center rounded-md bg-[var(--bg-muted)]",
          className,
        )}
        style={{ width: size, height: size }}
      >
        <Package size={size * 0.5} className="text-[var(--fg-muted)]" />
      </div>
    );
  }

  return (
    <img
      src={url}
      alt={alt}
      width={size}
      height={size}
      className={cn("rounded-md object-contain", className)}
      style={{ width: size, height: size }}
      onError={() => setFailed(true)}
    />
  );
}
