using Microsoft.AspNetCore.Mvc;

namespace pote.Config.Admin.Api.Helpers;

public static class AuditLogger
{
    /// <summary>
    /// Writes an audit entry. The acting username is taken from the authenticated
    /// principal; pass <paramref name="actingUsername"/> to override it on anonymous
    /// endpoints that establish identity themselves (login, redeem).
    /// An audit failure is logged but never turns a completed operation into an
    /// API error (clients would retry work that already happened).
    /// </summary>
    public static async Task AuditLog(this ControllerBase c, string id, string action, Func<string, string, string?, string, string, Task> func, string content = "", string? actingUsername = null)
    {
        try
        {
            var remoteIpAddress = c.Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var username = actingUsername ?? (c.User.Identity?.IsAuthenticated == true ? c.User.Identity.Name : null);
            await func(id, remoteIpAddress, username, action, content);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Audit write failed for action {Action} on {EntityId}", action, id);
        }
    }
}
