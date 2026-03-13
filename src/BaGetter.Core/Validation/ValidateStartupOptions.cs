using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BaGetter.Core;

/// <summary>
/// Validates BaGetter's options, used at startup.
/// </summary>
public class ValidateStartupOptions
{
    private readonly IOptions<BaGetterOptions> _root;
    private readonly IOptions<DatabaseOptions> _database;
    private readonly IOptions<StorageOptions> _storage;
    private readonly IOptions<MirrorOptions> _mirror;
    private readonly IOptions<HealthCheckOptions> _healthCheck;
    private readonly IOptions<StatisticsOptions> _statistics;
    private readonly ILogger<ValidateStartupOptions> _logger;

    public ValidateStartupOptions(
        IOptions<BaGetterOptions> root,
        IOptions<DatabaseOptions> database,
        IOptions<StorageOptions> storage,
        IOptions<MirrorOptions> mirror,
        IOptions<HealthCheckOptions> healthCheck,
        IOptions<StatisticsOptions> statistics,
        ILogger<ValidateStartupOptions> logger)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(mirror);
        ArgumentNullException.ThrowIfNull(healthCheck);
        ArgumentNullException.ThrowIfNull(statistics);
        ArgumentNullException.ThrowIfNull(logger);
        _root = root;
        _database = database;
        _storage = storage;
        _mirror = mirror;
        _healthCheck = healthCheck;
        _statistics = statistics;
        _logger = logger;
    }

    public bool Validate()
    {
        try
        {
            // Access each option to force validations to run.
            // Invalid options will trigger an "OptionsValidationException" exception.
            _ = _root.Value;
            _ = _database.Value;
            _ = _storage.Value;
            _ = _mirror.Value;
            _ = _healthCheck.Value;
            _ = _statistics.Value;

            return true;
        }
        catch (OptionsValidationException e)
        {
            foreach (var failure in e.Failures)
            {
                _logger.LogError("{OptionsFailure}", failure);
            }

            _logger.LogError(e, "BaGet configuration is invalid.");
            return false;
        }
    }
}
