using System.Threading;
using System.Threading.Tasks;

namespace BaGetter.Core;

/// <summary>Validates NuGet API keys used for package push operations.</summary>
public interface IApiKeyService
{
    Task<bool> IsValidAsync(string apiKey, CancellationToken cancellationToken);
}
