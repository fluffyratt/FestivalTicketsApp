using System.Security.Claims;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using FestivalTicketsApp.IdentityServer.Infrastructure.Configuration;
using FestivalTicketsApp.IdentityServer.Infrastructure.Data;
using FestivalTicketsApp.IdentityServer.Infrastructure.Data.Entities;
using IdentityModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace FestivalTicketsApp.IdentityServer.Infrastructure.Data;

public static class SeedData
{
    private static readonly List<IdentityRole> Roles =
    [
        new IdentityRole("admin"),
        new IdentityRole("manager"),
        new IdentityRole("client")
    ];

    // шаблони користувачів + їх ролі та claims
    private static readonly Dictionary<ApplicationUser, (string roleName, List<Claim> claims)> Users = new()
    {
        {
            new ApplicationUser
            {
                Id = "62f8d6a0-1fce-4cc9-a6be-4c486ee92522",
                UserName = "alice",
                Email = "AliceSmith@email.com",
                PhoneNumber = "+380950000001",
                EmailConfirmed = true
            },
            (
                "client",
                [
                    new Claim(JwtClaimTypes.Name, "Alice Smith"),
                    new Claim(JwtClaimTypes.GivenName, "Alice"),
                    new Claim(JwtClaimTypes.FamilyName, "Smith"),
                    new Claim(JwtClaimTypes.Email, "AliceSmith@email.com"),
                    new Claim(JwtClaimTypes.PhoneNumber, "+380950000001")
                ]
            )
        },
        {
            new ApplicationUser
            {
                Id = "52f16653-5583-4e40-b4e9-42ad6064d482",
                UserName = "bob",
                Email = "BobSmith@email.com",
                PhoneNumber = "+380950000002",
                EmailConfirmed = true
            },
            (
                "client",
                [
                    new Claim(JwtClaimTypes.Name, "Bob Smith"),
                    new Claim(JwtClaimTypes.GivenName, "Bob"),
                    new Claim(JwtClaimTypes.FamilyName, "Smith"),
                    new Claim(JwtClaimTypes.Email, "BobSmith@email.com"),
                    new Claim(JwtClaimTypes.PhoneNumber, "+380950000002")
                ]
            )
        },
        {
            new ApplicationUser
            {
                UserName = "manager1",
                Email = "manager1@email.com",
                PhoneNumber = "+380660000001",
                EmailConfirmed = true,
            },
            (
                "manager",
                [
                    new Claim(JwtClaimTypes.Name, "Daniel Johnson"),
                    new Claim(JwtClaimTypes.GivenName, "Daniel"),
                    new Claim(JwtClaimTypes.FamilyName, "Johnson"),
                    new Claim(JwtClaimTypes.Email, "manager1@email.com"),
                    new Claim(JwtClaimTypes.PhoneNumber, "+380660000001")
                ]
            )
        },
        {
            new ApplicationUser
            {
                UserName = "admin1",
                Email = "admin1@email.com",
                PhoneNumber = "+380990000001",
                EmailConfirmed = true
            },
            (
                "admin",
                [
                    new Claim(JwtClaimTypes.Name, "Artur Saiian"),
                    new Claim(JwtClaimTypes.GivenName, "Artur"),
                    new Claim(JwtClaimTypes.FamilyName, "Saiian"),
                    new Claim(JwtClaimTypes.Email, "admin1@email.com"),
                    new Claim(JwtClaimTypes.PhoneNumber, "+380990000001")
                ]
            )
        }
    };

    public static async Task EnsureSeedData(WebApplication app)
    {
        using var scope =
            app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();

        // контекст користувачів (Identity)
        var usersContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager  = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager  = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // контексти конфігурації IdentityServer
        var configContext = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        var grantContext  = scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>();

        // якщо бази ще нема – створюємо / оновлюємо схему
        await usersContext.Database.MigrateAsync();
        // за бажанням можна теж мігрувати конфіг-контексти:
        // await configContext.Database.MigrateAsync();
        // await grantContext.Database.MigrateAsync();

        await SeedUsers(userManager, roleManager);
        await SeedConfiguration(configContext, grantContext);
    }

    // ----------------- КОРИСТУВАЧІ + РОЛІ -----------------

    private static async Task SeedUsers(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        // 1) Ролі – створюємо, якщо ще нема
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role.Name!))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role.Name));
                if (result.Succeeded)
                    Log.Debug($"Role {role.Name} created");
                else
                    throw new Exception(result.Errors.First().Description);
            }
        }

        // 2) Користувачі – створюємо / оновлюємо, але не ламаємо існуючих
        foreach (var kvp in Users)
        {
            var userTemplate = kvp.Key;
            var (roleName, claims) = kvp.Value;

            var existingUser = await userManager.FindByNameAsync(userTemplate.UserName!);

            ApplicationUser user;
            if (existingUser is null)
            {
                user = userTemplate;
                var createResult = await userManager.CreateAsync(user, "Pass123$");
                if (!createResult.Succeeded)
                    throw new Exception(createResult.Errors.First().Description);

                Log.Debug($"{user.UserName} created");
            }
            else
            {
                user = existingUser;
                Log.Debug($"{user.UserName} already exists, skipping create");
            }

            // додаємо відсутні claims
            var existingClaims = await userManager.GetClaimsAsync(user);
            foreach (var claim in claims)
            {
                if (!existingClaims.Any(c => c.Type == claim.Type && c.Value == claim.Value))
                {
                    var claimResult = await userManager.AddClaimsAsync(user, new[] { claim });
                    if (!claimResult.Succeeded)
                        throw new Exception(claimResult.Errors.First().Description);
                }
            }

            // додаємо роль, якщо ще не в ній
            if (!await userManager.IsInRoleAsync(user, roleName))
            {
                var roleResult = await userManager.AddToRoleAsync(user, roleName);
                if (!roleResult.Succeeded)
                    throw new Exception(roleResult.Errors.First().Description);
            }
        }
    }

    // ----------------- КОНФІГ ІDENTITYSERVER (CLIENTS / SCOPES) -----------------

    private static async Task SeedConfiguration(
        ConfigurationDbContext configContext,
        PersistedGrantDbContext grantContext)
    {
        // 1) Чистимо старі Clients / IdentityResources / ApiScopes,
        //    щоб переконатися, що RedirectUris беруться з актуального Config.cs

        if (await configContext.Clients.AnyAsync())
        {
            configContext.Clients.RemoveRange(configContext.Clients);
        }

        if (await configContext.IdentityResources.AnyAsync())
        {
            configContext.IdentityResources.RemoveRange(configContext.IdentityResources);
        }

        if (await configContext.ApiScopes.AnyAsync())
        {
            configContext.ApiScopes.RemoveRange(configContext.ApiScopes);
        }

        await configContext.SaveChangesAsync();

        // 2) Додаємо все заново з Config.cs
        foreach (var client in Config.Clients)
        {
            await configContext.Clients.AddAsync(client.ToEntity());
        }

        foreach (var resource in Config.IdentityResources)
        {
            await configContext.IdentityResources.AddAsync(resource.ToEntity());
        }

        foreach (var scope in Config.ApiScopes)
        {
            await configContext.ApiScopes.AddAsync(scope.ToEntity());
        }

        await configContext.SaveChangesAsync();
    }
}

