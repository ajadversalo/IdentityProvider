using IdentityProvider.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace IdentityProvider.Web.Controllers;

[ApiController]
[Route("api/me")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class MeController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public MeController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userId = User.GetClaim(Claims.Subject);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(new
        {
            id = user.Id,
            email = user.Email,
            displayName = user.DisplayName,
            emailConfirmed = user.EmailConfirmed,
            roles = await _userManager.GetRolesAsync(user),
            createdUtc = user.CreatedUtc
        });
    }
}
