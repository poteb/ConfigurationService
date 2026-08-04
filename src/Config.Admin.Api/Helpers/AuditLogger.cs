using Microsoft.AspNetCore.Mvc;

namespace pote.Config.Admin.Api.Helpers;

public static class AuditLogger
{
    public static async Task AuditLog(this ControllerBase c, string id, string action, Func<string, string, string?, string, string, Task> func, string content = "")
    {
        var remoteIpAddress = c.Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var username = c.User.Identity?.IsAuthenticated == true ? c.User.Identity.Name : null;
        await func(id, remoteIpAddress, username, action, content);
    }
}
