import { cn } from "@/components/ui/utils";

interface SearchFiltersProps {
  packageType: string;
  framework: string;
  prerelease: boolean;
  onPackageTypeChange: (value: string) => void;
  onFrameworkChange: (value: string) => void;
  onPrereleaseChange: (value: boolean) => void;
}

const packageTypes = [
  { value: "any", label: "Any Type" },
  { value: "dependency", label: "Dependency" },
  { value: "dotnettool", label: ".NET tool" },
  { value: "dotnettemplate", label: ".NET template" },
];

type FrameworkEntry =
  | { type: "option"; value: string; label: string }
  | { type: "group"; label: string };

const frameworks: FrameworkEntry[] = [
  { type: "option", value: "any", label: "Any Framework" },
  // .NET
  { type: "group", label: ".NET" },
  { type: "option", value: "net10.0", label: ".NET 10.0" },
  { type: "option", value: "net9.0", label: ".NET 9.0" },
  { type: "option", value: "net8.0", label: ".NET 8.0" },
  { type: "option", value: "net7.0", label: ".NET 7.0" },
  { type: "option", value: "net6.0", label: ".NET 6.0" },
  { type: "option", value: "net5.0", label: ".NET 5.0" },
  // .NET Standard
  { type: "group", label: ".NET Standard" },
  { type: "option", value: "netstandard2.1", label: ".NET Standard 2.1" },
  { type: "option", value: "netstandard2.0", label: ".NET Standard 2.0" },
  { type: "option", value: "netstandard1.6", label: ".NET Standard 1.6" },
  { type: "option", value: "netstandard1.5", label: ".NET Standard 1.5" },
  { type: "option", value: "netstandard1.4", label: ".NET Standard 1.4" },
  { type: "option", value: "netstandard1.3", label: ".NET Standard 1.3" },
  { type: "option", value: "netstandard1.2", label: ".NET Standard 1.2" },
  { type: "option", value: "netstandard1.1", label: ".NET Standard 1.1" },
  { type: "option", value: "netstandard1.0", label: ".NET Standard 1.0" },
  // .NET Core
  { type: "group", label: ".NET Core" },
  { type: "option", value: "netcoreapp3.1", label: ".NET Core 3.1" },
  { type: "option", value: "netcoreapp3.0", label: ".NET Core 3.0" },
  { type: "option", value: "netcoreapp2.2", label: ".NET Core 2.2" },
  { type: "option", value: "netcoreapp2.1", label: ".NET Core 2.1" },
  { type: "option", value: "netcoreapp1.1", label: ".NET Core 1.1" },
  { type: "option", value: "netcoreapp1.0", label: ".NET Core 1.0" },
  // .NET Framework
  { type: "group", label: ".NET Framework" },
  { type: "option", value: "net481", label: ".NET Framework 4.8.1" },
  { type: "option", value: "net48", label: ".NET Framework 4.8" },
  { type: "option", value: "net472", label: ".NET Framework 4.7.2" },
  { type: "option", value: "net471", label: ".NET Framework 4.7.1" },
  { type: "option", value: "net463", label: ".NET Framework 4.6.3" },
  { type: "option", value: "net462", label: ".NET Framework 4.6.2" },
  { type: "option", value: "net461", label: ".NET Framework 4.6.1" },
  { type: "option", value: "net46", label: ".NET Framework 4.6" },
  { type: "option", value: "net452", label: ".NET Framework 4.5.2" },
  { type: "option", value: "net451", label: ".NET Framework 4.5.1" },
  { type: "option", value: "net45", label: ".NET Framework 4.5" },
  { type: "option", value: "net403", label: ".NET Framework 4.0.3" },
  { type: "option", value: "net4", label: ".NET Framework 4" },
  { type: "option", value: "net35", label: ".NET Framework 3.5" },
  { type: "option", value: "net2", label: ".NET Framework 2" },
  { type: "option", value: "net11", label: ".NET Framework 1.1" },
];

export function SearchFilters({
  packageType,
  framework,
  prerelease,
  onPackageTypeChange,
  onFrameworkChange,
  onPrereleaseChange,
}: SearchFiltersProps) {
  const renderFrameworkOptions = () => {
    const elements: React.ReactNode[] = [];
    let currentGroup: { label: string; options: React.ReactNode[] } | null = null;

    for (const entry of frameworks) {
      if (entry.type === "group") {
        if (currentGroup) {
          elements.push(
            <optgroup key={currentGroup.label} label={currentGroup.label}>
              {currentGroup.options}
            </optgroup>,
          );
        }
        currentGroup = { label: entry.label, options: [] };
      } else if (currentGroup) {
        currentGroup.options.push(
          <option key={entry.value} value={entry.value}>
            {entry.label}
          </option>,
        );
      } else {
        // "Any Framework" — before first group
        elements.push(
          <option key={entry.value} value={entry.value}>
            {entry.label}
          </option>,
        );
      }
    }
    if (currentGroup) {
      elements.push(
        <optgroup key={currentGroup.label} label={currentGroup.label}>
          {currentGroup.options}
        </optgroup>,
      );
    }
    return elements;
  };

  const selectClass = cn(
    "h-9 rounded-md border border-[var(--border)] bg-[var(--bg-card)] px-3 text-sm text-[var(--fg)]",
    "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--ring)]",
  );

  return (
    <div className="flex flex-wrap items-center gap-3">
      <select
        value={packageType}
        onChange={(e) => onPackageTypeChange(e.target.value)}
        className={selectClass}
      >
        {packageTypes.map((t) => (
          <option key={t.value} value={t.value}>
            {t.label}
          </option>
        ))}
      </select>

      <select
        value={framework}
        onChange={(e) => onFrameworkChange(e.target.value)}
        className={selectClass}
      >
        {renderFrameworkOptions()}
      </select>

      <label className="flex cursor-pointer items-center gap-2 text-sm">
        <input
          type="checkbox"
          checked={prerelease}
          onChange={(e) => onPrereleaseChange(e.target.checked)}
          className="rounded border-[var(--border)]"
        />
        Include prerelease
      </label>
    </div>
  );
}
