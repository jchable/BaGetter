using System;
using System.Text.Json.Serialization;
using BaGetter.Authentication;
using BaGetter.Core;
using BaGetter.Web;
using BaGetter.Web.Helper;
using Microsoft.Extensions.DependencyInjection;

namespace BaGetter;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddBaGetterWebApplication(
        this IServiceCollection services,
        Action<BaGetterApplication> configureAction)
    {
        services
            .AddRouting(options => options.LowercaseUrls = true)
            .AddControllers()
            .AddApplicationPart(typeof(PackageContentController).Assembly)
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

        services.AddRazorPages(options =>
        {
            // Toutes les Razor pages requièrent CanRead par défaut
            // sauf /Account/* qui est accessible sans auth (pages de connexion)
            options.Conventions.AuthorizeFolder("/", AuthenticationConstants.PolicyCanRead);
            options.Conventions.AllowAnonymousToFolder("/Account");
            options.Conventions.AuthorizeFolder("/Admin", AuthenticationConstants.PolicyCanAdmin);
        });

        services.AddHttpContextAccessor();
        services.AddScoped<ITenantProvider, BaGetter.Web.Services.HttpContextTenantProvider>();
        services.AddTransient<IUrlGenerator, BaGetterUrlGenerator>();

        services.AddSingleton(ApplicationVersionHelper.GetVersion());

        var app = services.AddBaGetterApplication(configureAction);

        return services;
    }
}
