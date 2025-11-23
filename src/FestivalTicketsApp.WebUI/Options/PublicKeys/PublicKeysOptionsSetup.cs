using FestivalTicketsApp.Shared;
using Microsoft.Extensions.Options;

namespace FestivalTicketsApp.WebUI.Options.PublicKeys;

public class PublicKeysOptionsSetup(IConfiguration configuration)
    : OptionsSetupBase(configuration), 
      IConfigureOptions<PublicKeysOptions>
{
    public static readonly string SectionName = "PublicKeys";
    
    public void Configure(PublicKeysOptions options)
    {
        _configuration.GetSection(SectionName)
            .Bind(options);
    }
}