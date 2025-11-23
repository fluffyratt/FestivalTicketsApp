using Microsoft.Extensions.Configuration;

namespace FestivalTicketsApp.Shared;

public abstract class OptionsSetupBase
{
    protected readonly IConfiguration _configuration;

    protected OptionsSetupBase(IConfiguration configuration)
    {
        _configuration = configuration;
    }
}