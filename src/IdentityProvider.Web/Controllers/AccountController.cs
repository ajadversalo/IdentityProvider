using System.Text.Encodings.Web;
using IdentityProvider.Web.Data;
using IdentityProvider.Web.Models.Account;
using IdentityProvider.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace IdentityProvider.Web.Controllers;

[ApiController]
[Route("api/account")]
[EnableRateLimiting("account")]
public class AccountController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IAppEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IAppEmailSender emailSender,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
        _configuration = configuration;
        _environment = environment;
    }

    [HttpGet("status")]
    [AllowAnonymous]
    [DisableRateLimiting]
    public async Task<IActionResult> Status()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Ok(new { isAuthenticated = false });
        }

        var user = await _userManager.GetUserAsync(User);
        return Ok(new
        {
            isAuthenticated = true,
            email = user?.Email,
            displayName = user?.DisplayName
        });
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            return Conflict(new { message = "An account with that email already exists." });
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? request.Email : request.DisplayName.Trim(),
            CreatedUtc = DateTimeOffset.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = "Registration failed.", errors = result.Errors.Select(e => e.Description) });
        }

        await _userManager.AddToRoleAsync(user, AppRoles.User);

        var confirmationUrl = await SendConfirmationEmailAsync(user);

        return Ok(new
        {
            message = "Account created. Confirm your email before signing in.",
            confirmationUrl = _environment.IsDevelopment() ? confirmationUrl : null
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            request.Password,
            request.RememberMe,
            lockoutOnFailure: true);

        if (result.IsNotAllowed)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Confirm your email before signing in.",
                code = "email_not_confirmed"
            });
        }

        if (result.IsLockedOut)
        {
            return StatusCode(StatusCodes.Status423Locked, new { message = "Account is locked. Try again later." });
        }

        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        return Ok(new { message = "Signed in." });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { message = "Signed out." });
    }

    [HttpPost("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user is null)
        {
            return BadRequest(new { message = "Invalid confirmation link." });
        }

        var result = await _userManager.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = "Email confirmation failed.", errors = result.Errors.Select(e => e.Description) });
        }

        return Ok(new { message = "Email confirmed. You can sign in." });
    }

    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || user.EmailConfirmed)
        {
            return Ok(new { message = "If that account needs confirmation, a new email is on its way." });
        }

        var confirmationUrl = await SendConfirmationEmailAsync(user);
        return Ok(new
        {
            message = "If that account needs confirmation, a new email is on its way.",
            confirmationUrl = _environment.IsDevelopment() ? confirmationUrl : null
        });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.EmailConfirmed)
        {
            return Ok(new { message = "If that email is registered, a reset link is on its way." });
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetUrl = BuildAppUrl($"/reset-password?userId={Uri.EscapeDataString(user.Id)}&token={Uri.EscapeDataString(token)}");
        await _emailSender.SendAsync(
            user.Email!,
            "Reset your password",
            $"<p>Reset your password by opening this link:</p><p><a href=\"{HtmlEncoder.Default.Encode(resetUrl)}\">{HtmlEncoder.Default.Encode(resetUrl)}</a></p>");

        return Ok(new
        {
            message = "If that email is registered, a reset link is on its way.",
            resetUrl = _environment.IsDevelopment() ? resetUrl : null
        });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user is null)
        {
            return BadRequest(new { message = "Invalid reset link." });
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = "Password reset failed.", errors = result.Errors.Select(e => e.Description) });
        }

        await _userManager.UpdateSecurityStampAsync(user);
        return Ok(new { message = "Password updated. You can sign in." });
    }

    private async Task<string> SendConfirmationEmailAsync(ApplicationUser user)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationUrl = BuildAppUrl(
            $"/confirm-email?userId={Uri.EscapeDataString(user.Id)}&token={Uri.EscapeDataString(token)}");

        await _emailSender.SendAsync(
            user.Email!,
            "Confirm your email",
            $"<p>Confirm your email by opening this link:</p><p><a href=\"{HtmlEncoder.Default.Encode(confirmationUrl)}\">{HtmlEncoder.Default.Encode(confirmationUrl)}</a></p>");

        return confirmationUrl;
    }

    private string BuildAppUrl(string pathAndQuery)
    {
        var publicAppUrl = (_configuration["PublicAppUrl"] ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
        return publicAppUrl + pathAndQuery;
    }
}
