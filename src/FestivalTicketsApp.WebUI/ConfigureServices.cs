using FestivalTicketsApp.Application.ClientService;
using FestivalTicketsApp.Application.ClientService.DTO;
using FestivalTicketsApp.Application.EventService;
using FestivalTicketsApp.Application.EventService.DTO;
using FestivalTicketsApp.Application.HostService;
using FestivalTicketsApp.Application.TicketService;
using FestivalTicketsApp.Infrastructure.BackgroundJobsService;
using FestivalTicketsApp.Infrastructure.Data;
using FestivalTicketsApp.WebUI.Helpers;
using FestivalTicketsApp.WebUI.Options.IdentityServer;
using FestivalTicketsApp.WebUI.Options.PublicKeys;
using FestivalTicketsApp.WebUI.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;

using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace FestivalTicketsApp.WebUI;

public static class ConfigureServices
{
    public static readonly string DefaultDbInstance = "LocalInstance2";
    
    public static IServiceCollection AddDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString(DefaultDbInstance);
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }

    public static IServiceCollection AddExternalAuthentication(
        this IServiceCollection services, 
        IdentityServerOptions identityServerOptions)
    {
        services.AddAuthentication(options => 
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddOpenIdConnect(options =>
            {
                options.Authority = "https://localhost:5001";
                
                options.CallbackPath = "/signin-oidc";
                options.SignedOutCallbackPath = "/signout-callback-oidc";
                
                options.ClientId = "FestivalTicketsApp";
                options.ClientSecret = identityServerOptions.MvcClientSecret;
                
                options.ResponseType = OpenIdConnectResponseType.Code;
                
                options.SaveTokens = true;
                options.UsePkce = true;
                
                options.Scope.Add(OidcConstants.StandardScopes.OfflineAccess);
                options.Scope.Add(OidcConstants.StandardScopes.OpenId);
                options.Scope.Add("ClientInfo");
                
                options.Scope.Remove(OidcConstants.StandardScopes.Profile);
                
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    RoleClaimType = JwtClaimTypes.Role,
                    NameClaimType = JwtClaimTypes.GivenName
                };
                options.GetClaimsFromUserInfoEndpoint = true;
                options.ClaimActions.MapAll();

                options.Events.OnUserInformationReceived = OpenIdEventHandlers.OnUserInformationReceived;
                options.Events.OnRemoteFailure = OpenIdEventHandlers.OnRemoteFailure;
            });
    
        return services;
    }

    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IHostService, HostService>();
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<IBackgroundJobsService, BackgroundJobsService>();

        return services;
    }

    public static IServiceCollection AddValidation(this IServiceCollection services)
    {
        services.AddFluentValidationAutoValidation(configuration =>
        {
            configuration.DisableDataAnnotationsValidation = true;
        });
        services.AddScoped<IValidator<ClientCreateDto>, ClientCreateDtoValidator>();
        services.AddScoped<IValidator<PlaneEventDto>, PlaneEventDtoValidator>();
        services.AddScoped<IValidator<CreateTicketTypeDto>, CreateTicketTypeDtoValidator>();

        return services;
    }

    public static IServiceCollection AddConfiguration(this IServiceCollection services)
    {
        services
            .ConfigureOptions<PublicKeysOptionsSetup>()
            .ConfigureOptions<IdentityServerOptionsSetup>();

        return services;
    }

   public static IServiceCollection AddHangfire(this IServiceCollection services, IConfiguration configuration)
{
    // Hangfire disabled on macOS.
    return services;
}

}
