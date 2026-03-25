using System;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Authentication;
using BaGetter.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BaGetter.Web;

[Authorize(AuthenticationSchemes = AuthenticationConstants.AllSchemes,
           Policy = AuthenticationConstants.PolicyCanPublish)]
public class SymbolController : Controller
{
    private readonly ISymbolIndexingService _indexer;
    private readonly ISymbolStorageService _storage;
    private readonly IOptionsSnapshot<BaGetterOptions> _options;
    private readonly ILogger<SymbolController> _logger;

    public SymbolController(
        ISymbolIndexingService indexer,
        ISymbolStorageService storage,
        IOptionsSnapshot<BaGetterOptions> options,
        ILogger<SymbolController> logger)
    {
        ArgumentNullException.ThrowIfNull(indexer);
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _indexer = indexer;
        _storage = storage;
        _options = options;
        _logger = logger;
    }

    public async Task Upload(CancellationToken cancellationToken)
    {
        if (_options.Value.IsReadOnlyMode)
        {
            HttpContext.Response.StatusCode = 401;
            return;
        }

        try
        {
            using var uploadStream = await Request.GetUploadStreamOrNullAsync(cancellationToken);
            if (uploadStream == null)
            {
                HttpContext.Response.StatusCode = 400;
                return;
            }

            var result = await _indexer.IndexAsync(uploadStream, cancellationToken);

            switch (result)
            {
                case SymbolIndexingResult.InvalidSymbolPackage:
                    HttpContext.Response.StatusCode = 400;
                    break;

                case SymbolIndexingResult.PackageNotFound:
                    HttpContext.Response.StatusCode = 404;
                    break;

                case SymbolIndexingResult.Success:
                    HttpContext.Response.StatusCode = 201;
                    break;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception thrown during symbol upload");
            HttpContext.Response.StatusCode = 500;
        }
    }

    [Authorize(Policy = AuthenticationConstants.PolicyCanRead)]
    public async Task<IActionResult> Get(string file, string key)
    {
        var pdbStream = await _storage.GetPortablePdbContentStreamOrNullAsync(file, key);
        if (pdbStream == null)
            return NotFound();

        return File(pdbStream, "application/octet-stream");
    }
}
