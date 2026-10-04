using Microsoft.AspNetCore.Identity;

namespace IdentityProvider.Web.Data;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
