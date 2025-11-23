using Duende.IdentityServer;
using Duende.IdentityServer.Services;
using FestivalTicketsApp.IdentityServer.Infrastructure.Data;
using FestivalTicketsApp.IdentityServer.Infrastructure.Data.Entities;
using FestivalTicketsApp.IdentityServer.Infrastructure.Options.ExternalProviderCredentialOptions;
using FestivalTicketsApp.IdentityServer.Infrastructure.Options.RegistrationCodesOptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FestivalTicketsApp.IdentityServer.Infrastructure.Configuration;

internal static class HostingExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddRazorPages();
        
        builder.Services.AddScoped<IProfileService, ProfileService>();

        builder.Services.ConfigureOptions<RegistrationCodesOptionsSetup>();

        builder.Services.AddHttpClient("FestivalTicketsApp", client =>
        {
            client.BaseAddress = new Uri("https://localhost:7129");
        });
        
        var migrationsAssembly = typeof(Program).Assembly.GetName().Name;
        string? connectionString = builder.Configuration.GetConnectionString("IdentityLocalInstance");

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                
                options.SignIn.RequireConfirmedAccount = false;
                options.SignIn.RequireConfirmedEmail = false;
                options.SignIn.RequireConfirmedPhoneNumber = false;

                options.Password.RequireDigit = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        builder.Services
            .AddIdentityServer(options =>
            {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;
                options.EmitStaticAudienceClaim = true;
            })
            .AddDeveloperSigningCredential()
            .AddConfigurationStore(options =>
            {
                options.ConfigureDbContext = b => 
                    b.UseSqlServer(connectionString, sql => 
                        sql.MigrationsAssembly(migrationsAssembly));
            })
            .AddOperationalStore(options =>
            {
                options.ConfigureDbContext = b => 
                    b.UseSqlServer(connectionString, sql => 
                        sql.MigrationsAssembly(migrationsAssembly));
            })
            .AddAspNetIdentity<ApplicationUser>();

        ExternalProviderCredentialOptions googleCredentials = builder.Configuration
            .GetSection($"{ExternalProviderCredentialOptionsSetup.RootSectionName}:Google")
            .Get<ExternalProviderCredentialOptions>()!;
        
        builder.Services.AddAuthentication()
            .AddGoogle(options =>
            {
                options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

                // register your IdentityServer with Google at https://console.developers.google.com
                // enable the Google+ API
                // set the redirect URI to https://localhost:5001/signin-google
                options.ClientId = googleCredentials.ClientId;
                options.ClientSecret = googleCredentials.ClientSecret;
            });

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseStaticFiles();
        app.UseRouting();
        app.UseIdentityServer();
        app.UseAuthorization();

        app.MapRazorPages()
            .RequireAuthorization();

        return app;
    }
}