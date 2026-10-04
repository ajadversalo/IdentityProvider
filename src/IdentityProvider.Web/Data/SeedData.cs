using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace IdentityProvider.Web.Data;

public static class SeedData
{
    public const string ApiScope = "api";
    public const string ApiResource = "identity-api";

    public static async Task EnsureSeededAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        var applicationManager = services.GetRequiredService<IOpenIddictApplicationManager>();
        var scopeManager = services.GetRequiredService<IOpenIddictScopeManager>();
        var roleManager = services.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>>();

        foreach (var role in new[] { AppRoles.User, AppRoles.Admin })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole(role));
            }
        }

        if (await scopeManager.FindByNameAsync(ApiScope) is null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = ApiScope,
                DisplayName = "Identity Provider API",
                Resources = { ApiResource }
            });
        }

        var clientId = configuration["OpenIddict:SpaClientId"] ?? "identity-spa";
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientType = ClientTypes.Public,
            ConsentType = ConsentTypes.Implicit,
            DisplayName = "Identity Provider SPA",
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.EndSession,
                Permissions.Endpoints.Token,
                Permissions.Endpoints.Revocation,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Email,
                Permissions.Scopes.Profile,
                Permissions.Scopes.Roles,
                Permissions.Prefixes.Scope + ApiScope
            },
            Requirements =
            {
                Requirements.Features.ProofKeyForCodeExchange
            }
        };

        foreach (var uri in SplitUris(configuration["OpenIddict:RedirectUris"]))
        {
            descriptor.RedirectUris.Add(uri);
        }

        foreach (var uri in SplitUris(configuration["OpenIddict:PostLogoutRedirectUris"]))
        {
            descriptor.PostLogoutRedirectUris.Add(uri);
        }

        var existing = await applicationManager.FindByClientIdAsync(clientId, cancellationToken);
        if (existing is null)
        {
            await applicationManager.CreateAsync(descriptor, cancellationToken);
        }
        else
        {
            await applicationManager.UpdateAsync(existing, descriptor, cancellationToken);
        }
    }

    private static IEnumerable<Uri> SplitUris(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            yield break;
        }

        foreach (var part in value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            yield return new Uri(part, UriKind.Absolute);
        }
    }
}
