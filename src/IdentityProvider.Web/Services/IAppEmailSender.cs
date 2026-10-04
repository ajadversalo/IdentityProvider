namespace IdentityProvider.Web.Services;

public interface IAppEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
