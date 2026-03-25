import { AlertTriangle } from "lucide-react";
import { Link } from "react-router";
import type { PackageDeprecation } from "@/api/types";

interface DeprecationBannerProps {
  deprecation: PackageDeprecation;
}

function toReadableReason(reason: string): string {
  switch (reason.toLowerCase()) {
    case "legacy":
      return "legacy";
    case "criticalbugs":
    case "criticalbug":
      return "affected by critical bugs";
    case "other":
      return "no longer recommended";
    default:
      return reason;
  }
}

export function DeprecationBanner({ deprecation }: DeprecationBannerProps) {
  const reasonText =
    deprecation.reasons.length > 0
      ? deprecation.reasons.map(toReadableReason).join(", ")
      : null;

  const summary = reasonText
    ? `This package has been deprecated as it is ${reasonText}.`
    : "This package has been deprecated.";

  return (
    <div className="mb-4 flex gap-3 rounded-lg border border-amber-200 bg-amber-50 p-4 dark:border-amber-800 dark:bg-amber-950">
      <AlertTriangle size={20} className="mt-0.5 shrink-0 text-amber-600 dark:text-amber-400" />
      <div className="text-sm">
        <p className="font-medium text-amber-800 dark:text-amber-200">{summary}</p>
        {deprecation.message && (
          <p className="mt-1 text-amber-700 dark:text-amber-300">{deprecation.message}</p>
        )}
        {deprecation.alternatePackage && (
          <p className="mt-1 text-amber-700 dark:text-amber-300">
            Suggested alternative:{" "}
            <Link
              to={`/packages/${encodeURIComponent(deprecation.alternatePackage.id)}`}
              className="font-medium underline"
            >
              {deprecation.alternatePackage.id}
            </Link>
            {deprecation.alternatePackage.range && (
              <span> ({deprecation.alternatePackage.range})</span>
            )}
          </p>
        )}
      </div>
    </div>
  );
}
