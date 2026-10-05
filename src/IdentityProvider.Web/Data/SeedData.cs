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

        var spaClientId = configuration["OpenIddict:SpaClientId"] ?? "identity-spa";
        var spaDescriptor = CreateInteractiveDescriptor(
            spaClientId,
            "Identity Provider SPA",
            ClientTypes.Public,
            requirePkce: true);

        foreach (var uri in SplitUris(configuration["OpenIddict:RedirectUris"]))
        {
            spaDescriptor.RedirectUris.Add(uri);
        }

        foreach (var uri in SplitUris(configuration["OpenIddict:PostLogoutRedirectUris"]))
        {
            spaDescriptor.PostLogoutRedirectUris.Add(uri);
        }

        await UpsertApplicationAsync(applicationManager, spaDescriptor, clientSecret: null, cancellationToken);

        // Northstar is a public SPA. Azure Static Web Apps Free SKU cannot host custom Easy Auth.
        var northstarClientId = configuration["OpenIddict:NorthstarClientId"] ?? "northstar";
        var northstarRedirects = SplitUris(configuration["OpenIddict:NorthstarRedirectUris"]).ToList();
        if (northstarRedirects.Count == 0)
        {
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("SeedData");
            logger.LogWarning(
                "Skipping {ClientId} client seed because OpenIddict:NorthstarRedirectUris is empty.",
                northstarClientId);
        }
        else
        {
            var northstarDescriptor = CreateInteractiveDescriptor(
                northstarClientId,
                "Northstar",
                ClientTypes.Public,
                requirePkce: true);

            foreach (var uri in northstarRedirects)
            {
                northstarDescriptor.RedirectUris.Add(uri);
            }

            foreach (var uri in SplitUris(configuration["OpenIddict:NorthstarPostLogoutRedirectUris"]))
            {
                northstarDescriptor.PostLogoutRedirectUris.Add(uri);
            }

            await UpsertApplicationAsync(applicationManager, northstarDescriptor, clientSecret: null, cancellationToken);
        }
    }

    private static OpenIddictApplicationDescriptor CreateInteractiveDescriptor(
        string clientId,
        string displayName,
        string clientType,
        bool requirePkce)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientType = clientType,
            ConsentType = ConsentTypes.Implicit,
            DisplayName = displayName,
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
            }
        };

        if (requirePkce)
        {
            descriptor.Requirements.Add(Requirements.Features.ProofKeyForCodeExchange);
        }

        return descriptor;
    }

    private static async Task UpsertApplicationAsync(
        IOpenIddictApplicationManager applicationManager,
        OpenIddictApplicationDescriptor descriptor,
        string? clientSecret,
        CancellationToken cancellationToken)
    {
        var clientId = descriptor.ClientId
            ?? throw new InvalidOperationException("ClientId is required.");

        if (!string.IsNullOrWhiteSpace(clientSecret))
        {
            descriptor.ClientSecret = clientSecret;
        }

        var existing = await applicationManager.FindByClientIdAsync(clientId, cancellationToken);
        if (existing is null)
        {
            await applicationManager.CreateAsync(descriptor, cancellationToken);
            return;
        }

        await applicationManager.UpdateAsync(existing, descriptor, cancellationToken);
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
