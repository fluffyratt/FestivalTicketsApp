using FestivalTicketsApp.Shared;
using Microsoft.Extensions.Options;

namespace FestivalTicketsApp.IdentityServer.Infrastructure.Options.RegistrationCodesOptions;

public class RegistrationCodesOptionsSetup(IConfiguration configuration)
    : OptionsSetupBase(configuration), 
      IConfigureOptions<RegistrationCodesOptions>
{
    public static readonly string SectionName = "RegistrationCodes";
    
    public void Configure(RegistrationCodesOptions options)
    {
        _configuration.GetSection(SectionName)
            .Bind(options);
    }
}