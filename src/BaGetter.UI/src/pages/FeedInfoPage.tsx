import { useState } from "react";
import { useAppConfig } from "@/api/account";
import { CopyButton } from "@/components/common/CopyButton";
import { LoadingSpinner } from "@/components/common/LoadingSpinner";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

interface UrlRowProps {
  readonly label: string;
  readonly url: string;
}

function UrlRow({ label, url }: UrlRowProps) {
  return (
    <tr>
      <td className="whitespace-nowrap py-2 pr-4 font-medium">{label}</td>
      <td className="w-full py-2">
        <div className="flex items-center gap-2">
          <code className="flex-1 select-all overflow-x-auto rounded bg-[var(--bg-muted)] px-2 py-1 text-sm">
            {url}
          </code>
          <CopyButton text={url} />
        </div>
      </td>
    </tr>
  );
}

interface TabItem {
  name: string;
  lines: string[];
}

interface TabbedCommandsProps {
  readonly tabs: TabItem[];
}

function TabbedCommands({ tabs }: TabbedCommandsProps) {
  const [activeIndex, setActiveIndex] = useState(0);
  const active = tabs[activeIndex]!;
  const text = active.lines.join("\n");

  return (
    <div className="text-sm">
      <div className="flex flex-wrap gap-1">
        {tabs.map((tab, i) => (
          <button
            key={tab.name}
            className={`rounded-t px-3 py-1.5 text-sm transition-colors ${
              i === activeIndex
                ? "bg-primary-600 font-semibold text-white dark:bg-primary-800"
                : "bg-[var(--bg-muted)] text-[var(--fg-muted)] hover:bg-surface-200 dark:hover:bg-surface-700"
            }`}
            onClick={() => setActiveIndex(i)}
          >
            {tab.name}
          </button>
        ))}
      </div>
      <div className="flex">
        <div className="flex-1 overflow-x-auto rounded-b rounded-tr bg-primary-600 px-3 py-2 font-mono text-white dark:bg-primary-800">
          {active.lines.map((line) => (
            <div key={line}>
              <span className="select-none text-primary-300">&gt; </span>
              {line}
            </div>
          ))}
        </div>
        <div className="flex items-stretch">
          <CopyButton
            text={text}
            className="h-full rounded-none rounded-br bg-amber-500 text-white hover:bg-amber-600"
          />
        </div>
      </div>
    </div>
  );
}

export function FeedInfoPage() {
  const { data: config, isLoading } = useAppConfig();

  if (isLoading) return <LoadingSpinner className="py-20" />;

  const serviceIndexUrl = config?.serviceIndexUrl ?? "[service-index-url]";
  const publishUrl = config?.publishUrl ?? "[publish-url]";
  const symbolPublishUrl = config?.symbolPublishUrl ?? "[symbol-publish-url]";
  const baseUrl = serviceIndexUrl.replace(/\/v3\/index\.json$/, "");
  const symbolServerUrl = `${baseUrl}/api/download/symbols`;

  const addSourceTabs: TabItem[] = [
    {
      name: ".NET CLI",
      lines: [`dotnet nuget add source ${serviceIndexUrl} --name BaGetter`],
    },
    {
      name: "NuGet CLI",
      lines: [`nuget sources add -Name BaGetter -Source ${serviceIndexUrl}`],
    },
    {
      name: "Visual Studio",
      lines: [
        "1. Go to Tools > NuGet Package Manager > Package Manager Settings",
        "2. Select Package Sources",
        '3. Click the + button to add a new source',
        `4. Set Name to "BaGetter" and Source to:`,
        `   ${serviceIndexUrl}`,
        "5. Click Update, then OK",
      ],
    },
    {
      name: "nuget.config",
      lines: [
        "<configuration>",
        "  <packageSources>",
        `    <add key="BaGetter" value="${serviceIndexUrl}" />`,
        "  </packageSources>",
        "</configuration>",
      ],
    },
  ];

  const pushTabs: TabItem[] = [
    {
      name: ".NET CLI",
      lines: [
        `dotnet nuget push -s ${serviceIndexUrl} -k YOUR_API_KEY package.nupkg`,
      ],
    },
    {
      name: "NuGet CLI",
      lines: [
        `nuget push -Source ${serviceIndexUrl} -ApiKey YOUR_API_KEY package.nupkg`,
      ],
    },
    {
      name: "Paket",
      lines: [
        `paket push --url ${baseUrl} --api-key YOUR_API_KEY package.nupkg`,
      ],
    },
  ];

  const symbolTabs: TabItem[] = [
    {
      name: ".NET CLI",
      lines: [
        `dotnet nuget push -s ${serviceIndexUrl} -k YOUR_API_KEY package.snupkg`,
      ],
    },
    {
      name: "NuGet CLI",
      lines: [
        `nuget push -Source ${serviceIndexUrl} -ApiKey YOUR_API_KEY package.snupkg`,
      ],
    },
  ];

  return (
    <div>
      <h1 className="text-2xl font-bold">Feed Info</h1>
      <p className="mt-2 text-[var(--fg-muted)]">
        Use the URLs below to connect your NuGet clients to this feed.
      </p>

      {/* Feed URLs */}
      <Card className="mt-6">
        <CardHeader>
          <CardTitle>Feed URLs</CardTitle>
        </CardHeader>
        <CardContent>
          <table className="w-full">
            <tbody>
              <UrlRow label="NuGet V3 Feed URL" url={serviceIndexUrl} />
              <UrlRow label="Package Push URL" url={publishUrl} />
              <UrlRow label="Symbol Push URL" url={symbolPublishUrl} />
              <UrlRow label="Symbol Server URL" url={symbolServerUrl} />
            </tbody>
          </table>
        </CardContent>
      </Card>

      {/* Add as package source */}
      <h2 className="mt-8 text-xl font-semibold">Add as package source</h2>
      <div className="mt-3">
        <TabbedCommands tabs={addSourceTabs} />
      </div>

      {/* Push packages */}
      <h2 className="mt-8 text-xl font-semibold">Push packages</h2>
      <div className="mt-3">
        <TabbedCommands tabs={pushTabs} />
      </div>

      {/* Push symbol packages */}
      <h2 className="mt-8 text-xl font-semibold">Push symbol packages</h2>
      <div className="mt-3">
        <TabbedCommands tabs={symbolTabs} />
      </div>

      {/* Symbol server */}
      <h2 className="mt-8 text-xl font-semibold">Symbol server (debugging)</h2>
      <Card className="mt-3">
        <CardContent className="pt-6">
          <p>
            To load symbols when debugging, add this URL as a symbol server in
            Visual Studio:
          </p>
          <p className="mt-2 font-medium">
            Debug &gt; Options &gt; Debugging &gt; Symbols &gt; New location
          </p>
          <div className="mt-3 flex items-center gap-2">
            <code className="flex-1 select-all rounded bg-[var(--bg-muted)] px-2 py-1 text-sm">
              {symbolServerUrl}
            </code>
            <CopyButton text={symbolServerUrl} />
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
