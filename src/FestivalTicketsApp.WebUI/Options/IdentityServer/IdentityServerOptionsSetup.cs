using FestivalTicketsApp.Shared;
using Microsoft.Extensions.Options;

namespace FestivalTicketsApp.WebUI.Options.IdentityServer;

public class IdentityServerOptionsSetup(IConfiguration configuration)
    : OptionsSetupBase(configuration), 
      IConfigureOptions<IdentityServerOptions>
{
    public static readonly string SectionName = "IdentityServer";
    
    public void Configure(IdentityServerOptions options)
    {
        _configuration.GetSection(SectionName)
            .Bind(options);
    }
}