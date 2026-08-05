using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using pote.Config.Admin.Api.Auth;
using pote.Config.DataProvider.Interfaces;

namespace pote.Config.UnitTests.Auth;

[TestFixture]
public class SessionAuthenticationHandlerTests
{
    private IUserDataAccess _users = null!;
    private SessionAuthenticationHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _users = Substitute.For<IUserDataAccess>();
        var options = Substitute.For<IOptionsMonitor<AuthenticationSchemeOptions>>();
        options.Get(Arg.Any<string>()).Returns(new AuthenticationSchemeOptions());
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());
        _handler = new SessionAuthenticationHandler(options, loggerFactory, UrlEncoder.Default, _users);
    }

    private async Task<AuthenticateResult> Authenticate(string? authorizationHeader)
    {
        var context = new DefaultHttpContext();
        if (authorizationHeader != null)
            context.Request.Headers.Authorization = authorizationHeader;
        var scheme = new AuthenticationScheme(AuthPolicies.SchemeName, null, typeof(SessionAuthenticationHandler));
        await _handler.InitializeAsync(scheme, context);
        return await _handler.AuthenticateAsync();
    }

    [Test]
    public async Task ValidToken_ProducesNameRoleAndUserIdClaims()
    {
        var userId = Guid.NewGuid();
        _users.GetSessionUser("tok123", Arg.Any<CancellationToken>()).Returns(new SessionUser
        {
            UserId = userId, Username = "anna", Role = "Admin", IsGuest = false, ExpiresUtc = DateTime.UtcNow.AddHours(1)
        });

        var result = await Authenticate("Bearer tok123");

        Assert.IsTrue(result.Succeeded);
        var principal = result.Principal!;
        Assert.AreEqual("anna", principal.Identity!.Name);
        Assert.IsTrue(principal.IsInRole("Admin"));
        Assert.AreEqual(userId.ToString(), principal.FindFirstValue(AuthPolicies.UserIdClaim));
        Assert.IsNull(principal.FindFirst(AuthPolicies.GuestClaim));
    }

    [Test]
    public async Task GuestSession_ProducesGuestClaim()
    {
        _users.GetSessionUser("tok123", Arg.Any<CancellationToken>()).Returns(new SessionUser
        {
            UserId = Guid.NewGuid(), Username = "guest", Role = "Admin", IsGuest = true, ExpiresUtc = DateTime.UtcNow.AddHours(1)
        });

        var result = await Authenticate("Bearer tok123");

        Assert.IsTrue(result.Succeeded);
        Assert.IsNotNull(result.Principal!.FindFirst(AuthPolicies.GuestClaim));
    }

    [Test]
    public async Task UnknownOrExpiredToken_Fails()
    {
        _users.GetSessionUser("tok123", Arg.Any<CancellationToken>()).Returns((SessionUser?)null);

        var result = await Authenticate("Bearer tok123");

        Assert.IsFalse(result.Succeeded);
        Assert.IsNotNull(result.Failure);
    }

    [Test]
    public async Task MissingHeader_NoResult()
    {
        var result = await Authenticate(null);

        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(result.None);
    }

    [Test]
    public async Task NonBearerHeader_NoResult()
    {
        var result = await Authenticate("Basic abc");

        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(result.None);
    }
}
