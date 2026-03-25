import { useState } from "react";
import { cn } from "@/components/ui/utils";
import { CopyButton } from "@/components/common/CopyButton";

interface InstallTabsProps {
  packageId: string;
  version: string;
  isDotnetTool: boolean;
  isDotnetTemplate: boolean;
  serviceIndexUrl?: string;
}

interface Tab {
  name: string;
  lines: string[];
  docUrl: string;
}

function getTabs(id: string, version: string, isDotnetTool: boolean, isDotnetTemplate: boolean, serviceIndexUrl?: string): Tab[] {
  if (isDotnetTemplate) {
    return [
      {
        name: ".NET CLI",
        lines: [`dotnet new install ${id}::${version}`],
        docUrl: "https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-new-install",
      },
    ];
  }

  if (isDotnetTool) {
    return [
      {
        name: ".NET CLI",
        lines: [`dotnet tool install --global ${id} --version ${version}`],
        docUrl: "https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-install",
      },
    ];
  }

  return [
    {
      name: ".NET CLI",
      lines: [`dotnet add package ${id} --version ${version}`],
      docUrl: "https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-add-package",
    },
    {
      name: "PackageReference",
      lines: [`<PackageReference Include="${id}" Version="${version}" />`],
      docUrl: "https://docs.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files",
    },
    {
      name: "Paket",
      lines: [`paket add ${id} --version ${version}`],
      docUrl: "https://fsprojects.github.io/Paket/paket-add.html",
    },
    {
      name: "Package Manager",
      lines: [`Install-Package ${id} -Version ${version}${serviceIndexUrl ? ` -Source ${serviceIndexUrl}` : ""}`],
      docUrl: "https://docs.microsoft.com/en-us/nuget/tools/ps-ref-install-package",
    },
  ];
}

export function InstallTabs({ packageId, version, isDotnetTool, isDotnetTemplate, serviceIndexUrl }: InstallTabsProps) {
  const tabs = getTabs(packageId, version, isDotnetTool, isDotnetTemplate, serviceIndexUrl);
  const [activeIndex, setActiveIndex] = useState(0);
  const activeTab = tabs[activeIndex]!;
  const command = activeTab.lines.join("\n");

  return (
    <div className="rounded-lg border border-[var(--border)] bg-[var(--bg-card)]">
      {/* Tab headers */}
      <div className="flex border-b border-[var(--border)]">
        {tabs.map((tab, i) => (
          <button
            key={tab.name}
            onClick={() => setActiveIndex(i)}
            className={cn(
              "px-4 py-2 text-sm font-medium transition-colors",
              i === activeIndex
                ? "border-b-2 border-primary-500 text-primary-600 dark:text-primary-400"
                : "text-[var(--fg-muted)] hover:text-[var(--fg)]",
            )}
          >
            {tab.name}
          </button>
        ))}
      </div>

      {/* Tab content */}
      <div className="flex items-center justify-between gap-2 p-3">
        <code className="flex-1 overflow-x-auto text-sm">
          {activeTab.lines.map((line, i) => (
            <div key={i} className="whitespace-nowrap">{line}</div>
          ))}
        </code>
        <CopyButton text={command} />
      </div>

      <div className="border-t border-[var(--border)] px-3 py-2 text-xs text-[var(--fg-muted)]">
        See{" "}
        <a
          href={activeTab.docUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="text-primary-500 hover:underline"
        >
          {activeTab.name}&apos;s documentation
        </a>{" "}
        for more info.
      </div>
    </div>
  );
}
