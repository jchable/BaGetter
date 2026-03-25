using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BaGetter.Core;
using BaGetter.Web;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Compact;

namespace BaGetter;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        if (!host.ValidateStartupOptions())
        {
            return;
        }

        var app = new CommandLineApplication
        {
            Name = "baget",
            Description = "A light-weight NuGet service",
        };

        app.HelpOption(inherited: true);

        app.Command("import", import =>
        {
            import.Command("downloads", downloads =>
            {
                downloads.OnExecuteAsync(async cancellationToken =>
                {
                    using var scope = host.Services.CreateScope();
                    var importer = scope.ServiceProvider.GetRequiredService<DownloadsImporter>();

                    await importer.ImportAsync(cancellationToken);
                });
            });
        });

        app.Command("deprecate", cmd =>
        {
            cmd.Description = "Deprecate a package on a BaGetter feed";

            var idArg = cmd.Argument("packageId", "Package id to deprecate").IsRequired();
            var versionArg = cmd.Argument("version", "Package version to deprecate").IsRequired();
            var reasonsOpt = cmd.Option("-r|--reasons", "Comma-separated deprecation reasons (Legacy,CriticalBugs,Other)", CommandOptionType.SingleValue).IsRequired();
            var messageOpt = cmd.Option("-m|--message", "Optional deprecation message", CommandOptionType.SingleValue);
            var altIdOpt = cmd.Option("-a|--alternate-package", "Alternate package id", CommandOptionType.SingleValue);
            var altVersionOpt = cmd.Option("-v|--alternate-version", "Alternate package version or range", CommandOptionType.SingleValue);
            var sourceOpt = cmd.Option("-s|--source", "Feed base URL (defaults to http://localhost:5000)", CommandOptionType.SingleValue);
            var apiKeyOpt = cmd.Option("-k|--api-key", "API key with push permissions", CommandOptionType.SingleValue).IsRequired();

            cmd.OnExecuteAsync(async cancellationToken =>
            {
                var reasons = (reasonsOpt.Value() ?? string.Empty)
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(r => r.Trim())
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .ToArray();

                var payload = new DeprecatePackageRequest
                {
                    Reasons = reasons,
                    Message = messageOpt.Value(),
                    AlternatePackageId = altIdOpt.Value(),
                    AlternatePackageVersion = altVersionOpt.Value()
                };

                var source = sourceOpt.HasValue()
                    ? sourceOpt.Value().TrimEnd('/')
                    : "http://localhost:5000";

                var endpoint = $"{source}/api/v2/package/{idArg.Value}/{versionArg.Value}/deprecate";
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("X-NuGet-ApiKey", apiKeyOpt.Value());

                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                var response = await client.PostAsync(endpoint, new StringContent(json, Encoding.UTF8, "application/json"), cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Deprecated {idArg.Value} {versionArg.Value}.");
                    return;
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                Console.Error.WriteLine($"Failed to deprecate package ({(int)response.StatusCode}): {body}");
                Environment.ExitCode = 1;
            });
        });

        app.Command("create-admin", cmd =>
        {
            cmd.Description = "Create the initial admin user (first-run setup)";

            var emailOpt = cmd.Option("-e|--email", "Admin email address", CommandOptionType.SingleValue).IsRequired();
            var passwordOpt = cmd.Option("-p|--password", "Admin password (min 8 chars)", CommandOptionType.SingleValue).IsRequired();
            var displayNameOpt = cmd.Option("-n|--name", "Display name (defaults to email)", CommandOptionType.SingleValue);

            cmd.OnExecuteAsync(async cancellationToken =>
            {
                using var scope = host.Services.CreateScope();
                var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<BaGetter.Core.BaGetterUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<BaGetter.Core.BaGetterRole>>();

                // Ensure roles exist
                foreach (var roleName in new[] { BaGetter.Core.Roles.Owner, BaGetter.Core.Roles.Admin, BaGetter.Core.Roles.Publisher, BaGetter.Core.Roles.Reader })
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                        await roleManager.CreateAsync(new BaGetter.Core.BaGetterRole { Name = roleName });
                }

                var email = emailOpt.Value()!;
                var existing = await userManager.FindByEmailAsync(email);
                if (existing != null)
                {
                    Console.Error.WriteLine($"User {email} already exists.");
                    Environment.ExitCode = 1;
                    return;
                }

                var user = new BaGetter.Core.BaGetterUser
                {
                    UserName = email,
                    Email = email,
                    DisplayName = displayNameOpt.Value() ?? email,
                    EmailConfirmed = true,
                };

                var result = await userManager.CreateAsync(user, passwordOpt.Value()!);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        Console.Error.WriteLine($"  {error.Description}");
                    Environment.ExitCode = 1;
                    return;
                }

                await userManager.AddToRoleAsync(user, BaGetter.Core.Roles.Owner);
                Console.WriteLine($"Admin user {email} created with Owner role.");
            });
        });

        app.Option("--urls", "The URLs that BaGetter should bind to.", CommandOptionType.SingleValue);

        app.OnExecuteAsync(async cancellationToken =>
        {
            await host.RunMigrationsAsync(cancellationToken);
            await host.SeedRolesAsync(cancellationToken);
            await host.RunAsync(cancellationToken);
        });

        await app.ExecuteAsync(args);
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host
            .CreateDefaultBuilder(args)
            .UseSerilog((context, services, loggerConfig) =>
            {
                loggerConfig
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithMachineName()
                    .Enrich.WithThreadId()
                    .WriteTo.Console(new CompactJsonFormatter());
            })
            .ConfigureAppConfiguration((ctx, config) =>
            {
                var root = Environment.GetEnvironmentVariable("BAGET_CONFIG_ROOT");

                if (!string.IsNullOrEmpty(root))
                {
                    config.SetBasePath(root);
                }

                // Optionally load secrets from files in the conventional path
                config.AddKeyPerFile("/run/secrets", optional: true);
            })
            .ConfigureWebHostDefaults(web =>
            {
                web.ConfigureKestrel(options =>
                {
                    // Limit upload size to 8 GiB (matches MaxPackageSizeGiB default).
                    // Can be further restricted by a reverse proxy.
                    options.Limits.MaxRequestBodySize = 8L * 1024 * 1024 * 1024;
                });

                web.UseStartup<Startup>();
            });
    }
}
