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
public class UsersControllerTests
{
    private IUserDataAccess _users = null!;
    private UsersController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _users = Substitute.For<IUserDataAccess>();
        var authService = new AuthService(_users, new PasswordHasher<User>(), new AuthSettings(),
            Substitute.For<ILogger<AuthService>>());
        _controller = new UsersController(Substitute.For<ILogger<UsersController>>(), _users, authService,
            new AuthSettings(), Substitute.For<IAuditLogHandler>());
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "admin1"),
            new Claim(ClaimTypes.Role, UserRoles.Admin),
            new Claim(AuthPolicies.UserIdClaim, Guid.NewGuid().ToString())
        }, "test");
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _controller.HttpContext.User = new ClaimsPrincipal(identity);
    }

    [Test]
    public async Task CreateInvite_ValidRequest_ReturnsToken()
    {
        _users.GetUserByUsername("anna", Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _controller.CreateInvite(new CreateInviteRequest { Username = "anna", Role = "User" }, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        Assert.IsNotNull(ok);
        var token = (TokenResponse)ok!.Value!;
        Assert.IsNotEmpty(token.Token);
        await _users.Received(1).UpsertInvite(Arg.Is<UserInvite>(i => i.Username == "anna" && i.Role == "User" && i.CreatedBy == "admin1"), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateInvite_ExistingUser_Returns400()
    {
        _users.GetUserByUsername("anna", Arg.Any<CancellationToken>()).Returns(new User { Username = "anna" });

        var result = await _controller.CreateInvite(new CreateInviteRequest { Username = "anna", Role = "User" }, CancellationToken.None);

        Assert.IsInstanceOf<BadRequestObjectResult>(result.Result);
    }

    [Test]
    public async Task CreateInvite_SoftDeletedUser_Returns400WithRestoreHint()
    {
        _users.GetUserByUsername("anna", Arg.Any<CancellationToken>()).Returns(new User { Username = "anna", Deleted = true });

        var result = await _controller.CreateInvite(new CreateInviteRequest { Username = "anna", Role = "User" }, CancellationToken.None);

        var bad = result.Result as BadRequestObjectResult;
        Assert.IsNotNull(bad);
        StringAssert.Contains("Restore", System.Text.Json.JsonSerializer.Serialize(bad!.Value));
    }

    [Test]
    public async Task CreateInvite_GuestUsername_Returns400()
    {
        var result = await _controller.CreateInvite(new CreateInviteRequest { Username = "guest", Role = "User" }, CancellationToken.None);

        Assert.IsInstanceOf<BadRequestObjectResult>(result.Result);
    }

    [Test]
    public async Task CreateInvite_InvalidRole_Returns400()
    {
        var result = await _controller.CreateInvite(new CreateInviteRequest { Username = "anna", Role = "Root" }, CancellationToken.None);

        Assert.IsInstanceOf<BadRequestObjectResult>(result.Result);
    }

    [Test]
    public async Task Delete_Self_Returns400()
    {
        _users.GetUserByUsername("admin1", Arg.Any<CancellationToken>()).Returns(new User { Username = "admin1" });

        var result = await _controller.Delete("admin1", permanent: false, CancellationToken.None);

        Assert.IsInstanceOf<BadRequestObjectResult>(result);
        await _users.DidNotReceive().SoftDeleteUser(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Delete_LastAdmin_Returns400()
    {
        var user = new User { Username = "anna", Role = UserRoles.Admin };
        _users.GetUserByUsername("anna", Arg.Any<CancellationToken>()).Returns(user);
        _users.SoftDeleteUser(user.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _controller.Delete("anna", permanent: false, CancellationToken.None);

        Assert.IsInstanceOf<BadRequestObjectResult>(result);
    }

    [Test]
    public async Task Delete_Permanent_OnActiveUser_Returns400()
    {
        _users.GetUserByUsername("anna", Arg.Any<CancellationToken>()).Returns(new User { Username = "anna", Deleted = false });

        var result = await _controller.Delete("anna", permanent: true, CancellationToken.None);

        Assert.IsInstanceOf<BadRequestObjectResult>(result);
        await _users.DidNotReceive().PermanentlyDeleteUser(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Delete_Permanent_OnDeletedUser_Deletes()
    {
        var user = new User { Username = "anna", Deleted = true };
        _users.GetUserByUsername("anna", Arg.Any<CancellationToken>()).Returns(user);

        var result = await _controller.Delete("anna", permanent: true, CancellationToken.None);

        Assert.IsInstanceOf<OkResult>(result);
        await _users.Received(1).PermanentlyDeleteUser(user.Id, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Restore_DeletedUser_Restores()
    {
        var user = new User { Username = "anna", Deleted = true };
        _users.GetUserByUsername("anna", Arg.Any<CancellationToken>()).Returns(user);

        var result = await _controller.Restore("anna", CancellationToken.None);

        Assert.IsInstanceOf<OkResult>(result);
        await _users.Received(1).RestoreUser(user.Id, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ChangeRole_LastAdminDemotion_Returns400()
    {
        var user = new User { Username = "anna", Role = UserRoles.Admin };
        _users.GetUserByUsername("anna", Arg.Any<CancellationToken>()).Returns(user);
        _users.UpdateRole(user.Id, "User", Arg.Any<CancellationToken>()).Returns(false);

        var result = await _controller.ChangeRole("anna", new ChangeRoleRequest { Role = "User" }, CancellationToken.None);

        Assert.IsInstanceOf<BadRequestObjectResult>(result);
    }

    [Test]
    public async Task CreateResetLink_ForGuest_Returns400()
    {
        _users.GetUserByUsername("guest", Arg.Any<CancellationToken>()).Returns(new User { Username = "guest", IsGuest = true });

        var result = await _controller.CreateResetLink("guest", CancellationToken.None);

        Assert.IsInstanceOf<BadRequestObjectResult>(result.Result);
    }

    [Test]
    public void Controller_RequiresAdminPolicy()
    {
        var attr = (AuthorizeAttribute)typeof(UsersController).GetCustomAttributes(typeof(AuthorizeAttribute), false).Single();
        Assert.AreEqual(AuthPolicies.AdminOnly, attr.Policy);
    }

    [Test]
    public void CreateFirstUser_RequiresGuestPolicy()
    {
        var method = typeof(UsersController).GetMethod(nameof(UsersController.CreateFirstUser))!;
        var attr = (AuthorizeAttribute)method.GetCustomAttributes(typeof(AuthorizeAttribute), false).Single();
        Assert.AreEqual(AuthPolicies.GuestOnly, attr.Policy);
    }
}
