using FestivalTicketsApp.Shared;
using Microsoft.Extensions.Options;

namespace FestivalTicketsApp.IdentityServer.Infrastructure.Options.ExternalProviderCredentialOptions;

public class ExternalProviderCredentialOptionsSetup(IConfiguration configuration) 
    : OptionsSetupBase(configuration),
      IConfigureNamedOptions<ExternalProviderCredentialOptions>
{
    public static readonly string RootSectionName = "ExternalProvidersInfo";
    
    public void Configure(ExternalProviderCredentialOptions options)
    {
        throw new NotImplementedException();
    }

    public void Configure(string? name, ExternalProviderCredentialOptions options)
    {
        _configuration.GetSection($"{RootSectionName}:{name}")
            .Bind(options);
    }
}