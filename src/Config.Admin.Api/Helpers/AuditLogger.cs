using Microsoft.AspNetCore.Mvc;

namespace pote.Config.Admin.Api.Helpers;

public static class AuditLogger
{
    public static async Task AuditLog(this ControllerBase c, string id, string action, Func<string, string, string, Task> func)
    {
        var remoteIpAddress = c.Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        await func(id, remoteIpAddress, action);
    }
}