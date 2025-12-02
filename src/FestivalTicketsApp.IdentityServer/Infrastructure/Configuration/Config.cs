using Duende.IdentityServer.Models;
using IdentityModel;

namespace FestivalTicketsApp.IdentityServer.Infrastructure.Configuration;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
        new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
            new IdentityResource(
                name: "ClientInfo",
                userClaims:
                [
                    JwtClaimTypes.Subject,
                    JwtClaimTypes.GivenName,
                    JwtClaimTypes.FamilyName,
                    JwtClaimTypes.Email,
                    JwtClaimTypes.PhoneNumber,
                    JwtClaimTypes.Role
                ]
            )
        };

    public static IEnumerable<ApiScope> ApiScopes =>
        new ApiScope[]
        {
            // за бажанням можеш додати свої скоупи
            // new ApiScope("festival_api", "Festival API")
        };

    public static IEnumerable<Client> Clients =>
        new Client[]
        {
            new Client
            {
                ClientId = "FestivalTicketsApp",
                ClientName = "Festival Tickets App",
                ClientSecrets = { new Secret("1C867CC4-F5EC-4DC2-81FE-615B14C242BE".Sha256()) },

                AllowedGrantTypes = GrantTypes.Code,

                RedirectUris = { "https://localhost:5087/signin-oidc" },
                PostLogoutRedirectUris = { "https://localhost:5087/signout-callback-oidc" },

                RequirePkce = true,
                AllowOfflineAccess = true,
                AllowedScopes =
                {
                    OidcConstants.StandardScopes.OpenId,
                    "ClientInfo"
                },

                CoordinateLifetimeWithUserSession = true
            },
        };
}
