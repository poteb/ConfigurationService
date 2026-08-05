using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using pote.Config.Admin.Api.Auth;
using pote.Config.Admin.Api.Controllers;
using pote.Config.Admin.Api.Model.RequestResponse;
using pote.Config.DataProvider.Interfaces;
using pote.Config.DbModel;

namespace pote.Config.UnitTests.Controllers;

[TestFixture]
public class AuthControllerTests
{
    private IUserDataAccess _users = null!;
    private AuthController _controller = null!;
    private readonly PasswordHasher<User> _hasher = new();

    private const string ValidPassword = "CorrectHorse1!Battery";

    [SetUp]
    public void SetUp()
    {
        _users = Substitute.For<IUserDataAccess>();
        var authService = new AuthService(_users, new PasswordHasher<User>(), new AuthSettings(),
            Substitute.For<ILogger<AuthService>>());
        _controller = new AuthController(Substitute.For<ILogger<AuthController>>(), authService, _users,
            new LocalAuthProviderSetup(), Substitute.For<IAuditLogHandler>());
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    private User MakeUser(string username, string password)
    {
        var user = new User { Username = username, Role = UserRoles.Admin };
        user.PasswordHash = _hasher.HashPassword(user, password);
        return user;
    }

    private void SetAuthenticatedUser(Guid userId, string username, string sessionToken)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(AuthPolicies.UserIdClaim, userId.ToString()),
            new Claim("sessionToken", sessionToken)
        }, "test");
        _controller.HttpContext.User = new ClaimsPrincipal(identity);
    }

    [Test]
    public async Task Login_Success_ReturnsTokenAndNoStoreHeader()
    {
        var user = MakeUser("anna", ValidPassword);
        _users.GetUserByUsername("anna", Arg.Any<CancellationToken>()).Returns(user);

        var result = await _controller.Login(new LoginRequest { Username = "anna", Password = ValidPassword }, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        Assert.IsNotNull(ok);
        var response = (LoginResponse)ok!.Value!;
        Assert.AreEqual("anna", response.Username);
        Assert.IsNotEmpty(response.Token);
        Assert.AreEqual("no-store", _controller.Response.Headers.CacheControl.ToString());
    }

    [Test]
    public async Task Login_Failure_ReturnsPlain401()
    {
        _users.GetUserByUsername("nobody", Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _controller.Login(new LoginRequest { Username = "nobody", Password = ValidPassword }, CancellationToken.None);

        Assert.IsInstanceOf<UnauthorizedResult>(result.Result);
    }

    [Test]
    public async Task Login_Failure_TakesAtLeastMinimumDuration()
    {
        _users.GetUserByUsername("nobody", Arg.Any<CancellationToken>()).Returns((User?)null);
        var started = DateTime.UtcNow;

        await _controller.Login(new LoginRequest { Username = "nobody", Password = ValidPassword }, CancellationToken.None);

        Assert.GreaterOrEqual(DateTime.UtcNow - started, AuthController.MinimumFailureDuration - TimeSpan.FromMilliseconds(50));
    }

    [Test]
    public async Task Redeem_UnknownToken_ReturnsGeneric400()
    {
        _users.ConsumeInvite("tok", Arg.Any<CancellationToken>()).Returns((UserInvite?)null);
        _users.ConsumeReset("tok", Arg.Any<CancellationToken>()).Returns((PasswordReset?)null);

        var result = await _controller.Redeem(new RedeemRequest { Token = "tok", Password = ValidPassword }, CancellationToken.None);

        Assert.IsInstanceOf<BadRequestObjectResult>(result.Result);
    }

    [Test]
    public async Task Logout_DeletesSession()
    {
        SetAuthenticatedUser(Guid.NewGuid(), "anna", "session-token-1");

        await _controller.Logout(CancellationToken.None);

        await _users.Received(1).DeleteSession("session-token-1", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ChangePassword_WeakPassword_Returns400()
    {
        SetAuthenticatedUser(Guid.NewGuid(), "anna", "tok");

        var result = await _controller.ChangePassword(new ChangePasswordRequest { CurrentPassword = ValidPassword, NewPassword = "weak" }, CancellationToken.None);

        Assert.IsInstanceOf<BadRequestObjectResult>(result);
    }

    [Test]
    public async Task ChangePassword_WrongCurrent_Returns400()
    {
        var user = MakeUser("anna", ValidPassword);
        _users.GetUserById(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        SetAuthenticatedUser(user.Id, "anna", "tok");

        var result = await _controller.ChangePassword(new ChangePasswordRequest { CurrentPassword = "Wrong1!Currentxxx", NewPassword = "NewPassword1!xxxx" }, CancellationToken.None);

        Assert.IsInstanceOf<BadRequestObjectResult>(result);
    }

    [Test]
    public void GetProvider_ReturnsLocalMetadata()
    {
        var result = _controller.GetProvider();

        var ok = result.Result as OkObjectResult;
        Assert.IsNotNull(ok);
        StringAssert.Contains("local", System.Text.Json.JsonSerializer.Serialize(ok!.Value));
    }

    [Test]
    public void AnonymousEndpoints_AreMarkedAllowAnonymous()
    {
        foreach (var name in new[] { nameof(AuthController.Login), nameof(AuthController.Redeem), nameof(AuthController.GetProvider) })
        {
            var method = typeof(AuthController).GetMethod(name)!;
            Assert.IsNotEmpty(method.GetCustomAttributes(typeof(AllowAnonymousAttribute), false), $"{name} should be [AllowAnonymous]");
        }
    }

    [Test]
    public void ChangePassword_RequiresRealUserPolicy()
    {
        var method = typeof(AuthController).GetMethod(nameof(AuthController.ChangePassword))!;
        var attr = (AuthorizeAttribute)method.GetCustomAttributes(typeof(AuthorizeAttribute), false).Single();
        Assert.AreEqual(AuthPolicies.RealUser, attr.Policy);
    }
}
