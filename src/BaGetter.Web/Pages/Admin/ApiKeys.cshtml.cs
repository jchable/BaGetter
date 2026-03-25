using BaGetter.Core;
using BaGetter.Core.Configuration;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace BaGetter.Web.Admin;

public class ApiKeysModel : PageModel
{
    private readonly BaGetterOptions _options;

    public ApiKeysModel(IOptionsSnapshot<BaGetterOptions> options)
    {
        _options = options.Value;
    }

    public bool HasGlobalKey { get; set; }
    public int ConfiguredKeyCount { get; set; }

    public void OnGet()
    {
        HasGlobalKey = !string.IsNullOrEmpty(_options.ApiKey);
        ConfiguredKeyCount = _options.Authentication?.ApiKeys?.Length ?? 0;
    }
}
