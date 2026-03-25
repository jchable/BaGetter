import { useQuery } from "@tanstack/react-query";
import { api } from "@/api/client";
import { Button } from "@/components/ui/button";
import { ExternalLink } from "lucide-react";

interface OAuthProvider {
  name: string;
  displayName: string;
}

interface OAuthButtonsProps {
  readonly returnUrl?: string;
}

export function OAuthButtons({ returnUrl = "/" }: OAuthButtonsProps) {
  const { data: providers } = useQuery({
    queryKey: ["oauth-providers"],
    queryFn: () => api.get<OAuthProvider[]>("/api/ui/account/oauth-providers"),
    staleTime: 300_000,
  });

  if (!providers || providers.length === 0) return null;

  return (
    <div className="space-y-2">
      <div className="relative my-4">
        <div className="absolute inset-0 flex items-center">
          <span className="w-full border-t border-[var(--border)]" />
        </div>
        <div className="relative flex justify-center text-xs uppercase">
          <span className="bg-[var(--bg-card)] px-2 text-[var(--fg-muted)]">
            Or continue with
          </span>
        </div>
      </div>

      {providers.map((provider) => (
        <a
          key={provider.name}
          href={`/api/ui/account/external-login?provider=${encodeURIComponent(provider.name)}&returnUrl=${encodeURIComponent(returnUrl)}`}
          className="block"
        >
          <Button variant="outline" className="w-full gap-2">
            <ExternalLink size={16} />
            {provider.displayName}
          </Button>
        </a>
      ))}
    </div>
  );
}
