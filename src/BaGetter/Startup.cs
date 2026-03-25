using System;
using System.IO;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using Serilog.Events;
using BaGetter.Core;
using BaGetter.Core.Extensions;
using BaGetter.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using HealthCheckOptions = Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions;

namespace BaGetter;

public class Startup
{
    private IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.ConfigureOptions<ValidateBaGetterOptions>();
        services.ConfigureOptions<ConfigureBaGetterServer>();

        services.AddBaGetterOptions<IISServerOptions>(nameof(IISServerOptions));
        services.AddBaGetterWebApplication(ConfigureBaGetterApplication);

        // Anti-forgery for SPA: emit XSRF-TOKEN cookie, validate X-XSRF-TOKEN header
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-XSRF-TOKEN";
            options.Cookie.Name = "XSRF-TOKEN";
            options.Cookie.HttpOnly = false; // SPA JS must read the cookie
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
        });

        // You can swap between implementations of subsystems like storage and search using BaGetter's configuration.
        // Each subsystem's implementation has a provider that reads the configuration to determine if it should be
        // activated. BaGetter will run through all its providers until it finds one that is active.
        services.AddScoped(DependencyInjectionExtensions.GetServiceFromProviders<IContext>);
        services.AddTransient(DependencyInjectionExtensions.GetServiceFromProviders<IStorageService>);
        services.AddTransient(DependencyInjectionExtensions.GetServiceFromProviders<IPackageDatabase>);
        services.AddTransient(DependencyInjectionExtensions.GetServiceFromProviders<ISearchService>);
        services.AddTransient(DependencyInjectionExtensions.GetServiceFromProviders<ISearchIndexer>);

        services.AddHealthChecks();

        services.AddCors();

        services.AddRateLimiter(rateLimiterOptions =>
        {
            // Upload: 5 requests per user per 10 minutes (generous for CI pipelines)
            rateLimiterOptions.AddSlidingWindowLimiter(RateLimitPolicies.Upload, options =>
            {
                options.PermitLimit = 5;
                options.Window = TimeSpan.FromMinutes(10);
                options.SegmentsPerWindow = 2;
                options.QueueLimit = 0;
            });

            // Search/autocomplete: 200 requests per minute per IP
            rateLimiterOptions.AddFixedWindowLimiter(RateLimitPolicies.Search, options =>
            {
                options.PermitLimit = 200;
                options.Window = TimeSpan.FromMinutes(1);
                options.QueueLimit = 0;
            });

            rateLimiterOptions.RejectionStatusCode = 429;
        });
    }

    private void ConfigureBaGetterApplication(BaGetterApplication app)
    {
        // Authentication: Identity (cookie) + API key + OAuth
        app.AddBaGetterAuthentication(Configuration);

        // Add database providers.
        app.AddPostgreSqlDatabase();
        app.AddSqliteDatabase();

        // Add storage providers.
        app.AddFileStorage();
        app.AddAwsS3Storage();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var options = Configuration.Get<BaGetterOptions>();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseStatusCodePages();
        }
        else
        {
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseForwardedHeaders();
        app.UsePathBase(options.PathBase);

        app.UseSerilogRequestLogging(opts =>
        {
            opts.EnrichDiagnosticContext = (diagCtx, httpCtx) =>
            {
                diagCtx.Set("RequestHost", httpCtx.Request.Host.Value);
                diagCtx.Set("UserName", httpCtx.User?.Identity?.Name ?? "anonymous");
                diagCtx.Set("RemoteIP", httpCtx.Connection.RemoteIpAddress?.ToString());
            };
            opts.GetLevel = (ctx, _, _) =>
                ctx.Request.Path.StartsWithSegments("/health")
                    ? LogEventLevel.Debug
                    : LogEventLevel.Information;
        });

        app.Use(async (context, next) =>
        {
            var h = context.Response.Headers;
            h["X-Content-Type-Options"] = "nosniff";
            h["X-Frame-Options"] = "DENY";
            h["Referrer-Policy"] = "strict-origin-when-cross-origin";
            h["X-Permitted-Cross-Domain-Policies"] = "none";
            h["Content-Security-Policy"] =
                "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; frame-ancestors 'none'; object-src 'none'; base-uri 'self'; form-action 'self'";
            await next();
        });

        app.UseStaticFiles();

        // Serve DocFX documentation at /docs
        var docsPath = Path.Combine(env.ContentRootPath, "wwwroot", "docs");
        if (Directory.Exists(docsPath))
        {
            var docsFileProvider = new PhysicalFileProvider(docsPath);
            app.UseDefaultFiles(new DefaultFilesOptions
            {
                FileProvider = docsFileProvider,
                RequestPath = "/docs"
            });
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = docsFileProvider,
                RequestPath = "/docs"
            });
        }

        app.UseRouting();

        app.UseCors(ConfigureBaGetterServer.CorsPolicy);

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();

        app.UseOperationCancelledMiddleware();

        // Emit XSRF-TOKEN cookie on every response so the SPA can read it
        app.Use(async (context, next) =>
        {
            var antiforgery = context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>();
            var tokens = antiforgery.GetAndStoreTokens(context);
            context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!,
                new Microsoft.AspNetCore.Http.CookieOptions
                {
                    HttpOnly = false,
                    SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
                    Secure = context.Request.IsHttps,
                });
            await next();
        });

        app.UseHealthChecks(options.HealthCheck.Path,
            new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    await report.FormatAsJson(context.Response.Body, options.Statistics.ListConfiguredServices, options.HealthCheck.StatusPropertyName,
                        context.RequestAborted);
                },
                Predicate = check => check.IsConfigured(options)
            }
        );

        app.UseEndpoints(endpoints =>
        {
            var baget = new BaGetterEndpointBuilder();

            baget.MapEndpoints(endpoints);

            // SPA fallback: serve index.html for any route not matched by API or static files
            endpoints.MapFallbackToFile("index.html");
        });
    }
}
