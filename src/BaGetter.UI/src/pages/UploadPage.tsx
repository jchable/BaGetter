import { useAppConfig } from "@/api/account";
import { CopyButton } from "@/components/common/CopyButton";
import { LoadingSpinner } from "@/components/common/LoadingSpinner";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

interface CommandCardProps {
  readonly title: string;
  readonly command: string;
  readonly docUrl: string;
  readonly docLabel: string;
}

function CommandCard({ title, command, docUrl, docLabel }: CommandCardProps) {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">{title}</CardTitle>
      </CardHeader>
      <CardContent>
        <div className="flex items-center gap-2 rounded-md bg-[var(--bg-muted)] px-3 py-2">
          <code className="flex-1 overflow-x-auto text-sm">{command}</code>
          <CopyButton text={command} />
        </div>
        <p className="mt-3 text-xs text-[var(--fg-muted)]">
          See{" "}
          <a
            href={docUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="text-primary-500 hover:underline"
          >
            {docLabel}
          </a>{" "}
          for more info.
        </p>
      </CardContent>
    </Card>
  );
}

export function UploadPage() {
  const { data: config, isLoading } = useAppConfig();

  if (isLoading) return <LoadingSpinner className="py-20" />;

  const serviceIndexUrl = config?.serviceIndexUrl ?? "[service-index-url]";
  const publishUrl = config?.publishUrl ?? "[publish-url]";

  return (
    <div>
      <h1 className="text-2xl font-bold">Upload</h1>
      <p className="mt-2 text-[var(--fg-muted)]">
        You can push packages using the service index{" "}
        <code className="rounded bg-[var(--bg-muted)] px-1.5 py-0.5 text-sm">{serviceIndexUrl}</code>.
      </p>

      <div className="mt-6 grid gap-4 md:grid-cols-2">
        <CommandCard
          title=".NET CLI"
          command={`dotnet nuget push -s ${serviceIndexUrl} package.nupkg`}
          docUrl="https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-nuget-push"
          docLabel=".NET CLI documentation"
        />
        <CommandCard
          title="NuGet"
          command={`nuget push -Source ${serviceIndexUrl} package.nupkg`}
          docUrl="https://docs.microsoft.com/en-us/nuget/tools/cli-ref-push"
          docLabel="NuGet CLI documentation"
        />
        <CommandCard
          title="Paket"
          command={`paket push --url ${serviceIndexUrl.replace("/v3/index.json", "")} package.nupkg`}
          docUrl="https://fsprojects.github.io/Paket/paket-push.html"
          docLabel="Paket documentation"
        />
        <CommandCard
          title="PowerShellGet"
          command={`Register-PSRepository -Name "BaGet" -SourceLocation "${serviceIndexUrl}" -PublishLocation "${publishUrl}" -InstallationPolicy "Trusted"\nPublish-Module -Name PS-Module -Repository BaGet`}
          docUrl="https://docs.microsoft.com/en-us/powershell/module/powershellget/publish-module"
          docLabel="PowerShellGet documentation"
        />
      </div>
    </div>
  );
}
