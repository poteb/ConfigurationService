using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using pote.Config.Admin.Api.Auth;
using pote.Config.DataProvider.Interfaces;
using pote.Config.DbModel;

namespace pote.Config.UnitTests.Auth;

[TestFixture]
public class AuthServiceTests
{
    private IUserDataAccess _users = null!;
    private AuthService _sut = null!;
    private readonly PasswordHasher<User> _hasher = new();

    private const string ValidPassword = "CorrectHorse1!Battery";

    [SetUp]
    public void SetUp()
    {
        _users = Substitute.For<IUserDataAccess>();
        _sut = new AuthService(_users, new PasswordHasher<User>(), new AuthSettings(),
            Substitute.For<ILogger<AuthService>>());
    }

    private User MakeUser(string username, string password, string role = UserRoles.Admin, bool isGuest = false, bool deleted = false)
    {
        var user = new User { Username = username, Role = role, IsGuest = isGuest, Deleted = deleted };
        user.PasswordHash = _hasher.HashPassword(user, password);
        return user;
    }

    // ---------- Login ----------

    [Test]
    public async Task Login_ValidRealUser_ReturnsSessionAndDeletesGuest()
    {
        var user = MakeUser("anna", ValidPassword);
        _users.GetUserByUsername("anna", Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.Login("anna", ValidPassword, CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.AreEqual("anna", result!.Username);
        Assert.AreEqual(UserRoles.Admin, result.Role);
        Assert.IsFalse(result.IsGuest);
        Assert.IsNotEmpty(result.Token);
        Assert.Greater(result.ExpiresUtc, DateTime.UtcNow.AddHours(7));
        await _users.Received(1).HardDeleteGuest(Arg.Any<CancellationToken>());
        await _users.Received(1).InsertSession(Arg.Is<Session>(s => s.UserId == user.Id && s.Token == result.Token), Arg.Any<CancellationToken>());
        await _users.Received(1).UpdateLastLogin(user.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Login_GuestUser_DoesNotDeleteGuest()
    {
        var guest = MakeUser("guest", "guest", isGuest: true);
        _users.GetUserByUsername("guest", Arg.Any<CancellationToken>()).Returns(guest);

        var result = await _sut.Login("guest", "guest", CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.IsTrue(result!.IsGuest);
        await _users.DidNotReceive().HardDeleteGuest(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Login_UnknownUser_ReturnsNull()
    {
        _users.GetUserByUsername("nobody", Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _sut.Login("nobody", ValidPassword, CancellationToken.None);

        Assert.IsNull(result);
        await _users.DidNotReceive().InsertSession(Arg.Any<Session>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Login_WrongPassword_ReturnsNull()
    {
        var user = MakeUser("anna", ValidPassword);
        _users.GetUserByUsername("anna", Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.Login("anna", "WrongPassword1!xx", CancellationToken.None);

        Assert.IsNull(result);
    }

    [Test]
    public async Task Login_SoftDeletedUser_ReturnsNull()
    {
        var user = MakeUser("anna", ValidPassword, deleted: true);
        _users.GetUserByUsername("anna", Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.Login("anna", ValidPassword, CancellationToken.None);

        Assert.IsNull(result);
    }

    // ---------- Redeem: invites ----------

    [Test]
    public async Task Redeem_ValidInvite_CreatesUserWithInviteRoleAndLogsIn()
    {
        _users.ConsumeInvite("tok", Arg.Any<CancellationToken>())
            .Returns(new UserInvite { Username = "anna", Role = UserRoles.User, ExpiresUtc = DateTime.UtcNow.AddDays(1) });
        _users.GetUserByUsername("anna", Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _sut.Redeem("tok", ValidPassword, CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.AreEqual("anna", result!.Username);
        Assert.AreEqual(UserRoles.User, result.Role);
        await _users.Received(1).InsertUser(Arg.Is<User>(u => u.Username == "anna" && u.Role == UserRoles.User && !u.IsGuest), Arg.Any<CancellationToken>());
        await _users.Received(1).HardDeleteGuest(Arg.Any<CancellationToken>());
        await _users.Received(1).InsertSession(Arg.Any<Session>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Redeem_InviteUsernameTakenMeanwhile_ReturnsNull()
    {
        _users.ConsumeInvite("tok", Arg.Any<CancellationToken>())
            .Returns(new UserInvite { Username = "anna", Role = UserRoles.User });
        _users.GetUserByUsername("anna", Arg.Any<CancellationToken>()).Returns(MakeUser("anna", ValidPassword));

        var result = await _sut.Redeem("tok", ValidPassword, CancellationToken.None);

        Assert.IsNull(result);
        await _users.DidNotReceive().InsertUser(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Redeem_InvalidPassword_ReturnsNullWithoutConsumingToken()
    {
        var result = await _sut.Redeem("tok", "weak", CancellationToken.None);

        Assert.IsNull(result);
        await _users.DidNotReceive().ConsumeInvite(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _users.DidNotReceive().ConsumeReset(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ---------- Redeem: resets ----------

    [Test]
    public async Task Redeem_ValidReset_UpdatesHashRevokesSessionsAndLogsIn()
    {
        var user = MakeUser("anna", "OldPassword1!xxxx");
        _users.ConsumeInvite("tok", Arg.Any<CancellationToken>()).Returns((UserInvite?)null);
        _users.ConsumeReset("tok", Arg.Any<CancellationToken>())
            .Returns(new PasswordReset { UserId = user.Id, ExpiresUtc = DateTime.UtcNow.AddDays(1) });
        _users.GetUserById(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.Redeem("tok", ValidPassword, CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.AreEqual("anna", result!.Username);
        await _users.Received(1).UpdatePasswordHash(user.Id, Arg.Any<string>(), Arg.Is((string?)null), Arg.Any<CancellationToken>());
        await _users.Received(1).DeleteSessionsForUser(user.Id, Arg.Any<CancellationToken>());
        await _users.Received(1).InsertSession(Arg.Any<Session>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Redeem_ResetForDeletedUser_ReturnsNull()
    {
        var user = MakeUser("anna", ValidPassword, deleted: true);
        _users.ConsumeInvite("tok", Arg.Any<CancellationToken>()).Returns((UserInvite?)null);
        _users.ConsumeReset("tok", Arg.Any<CancellationToken>()).Returns(new PasswordReset { UserId = user.Id });
        _users.GetUserById(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.Redeem("tok", ValidPassword, CancellationToken.None);

        Assert.IsNull(result);
        await _users.DidNotReceive().UpdatePasswordHash(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Redeem_UnknownToken_ReturnsNull()
    {
        _users.ConsumeInvite("tok", Arg.Any<CancellationToken>()).Returns((UserInvite?)null);
        _users.ConsumeReset("tok", Arg.Any<CancellationToken>()).Returns((PasswordReset?)null);

        var result = await _sut.Redeem("tok", ValidPassword, CancellationToken.None);

        Assert.IsNull(result);
    }

    // ---------- Change password ----------

    [Test]
    public async Task ChangePassword_ValidCurrent_UpdatesAndRevokesOtherSessions()
    {
        var user = MakeUser("anna", ValidPassword);
        _users.GetUserById(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _users.UpdatePasswordHash(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(true);

        var ok = await _sut.ChangePassword(user.Id, ValidPassword, "NewPassword1!xxxx", "keep-token", CancellationToken.None);

        Assert.IsTrue(ok);
        // Guarded by the current hash so concurrent changes cannot both win.
        await _users.Received(1).UpdatePasswordHash(user.Id, Arg.Any<string>(), Arg.Is(user.PasswordHash), Arg.Any<CancellationToken>());
        await _users.Received(1).DeleteOtherSessionsForUser(user.Id, "keep-token", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ChangePassword_ConcurrentChangeWonTheRace_ReturnsFalse()
    {
        var user = MakeUser("anna", ValidPassword);
        _users.GetUserById(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _users.UpdatePasswordHash(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(false);

        var ok = await _sut.ChangePassword(user.Id, ValidPassword, "NewPassword1!xxxx", "keep-token", CancellationToken.None);

        Assert.IsFalse(ok);
        await _users.DidNotReceive().DeleteOtherSessionsForUser(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ChangePassword_WrongCurrent_ReturnsFalse()
    {
        var user = MakeUser("anna", ValidPassword);
        _users.GetUserById(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var ok = await _sut.ChangePassword(user.Id, "WrongCurrent1!xxx", "NewPassword1!xxxx", "keep", CancellationToken.None);

        Assert.IsFalse(ok);
        await _users.DidNotReceive().UpdatePasswordHash(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ChangePassword_WeakNewPassword_ReturnsFalse()
    {
        var user = MakeUser("anna", ValidPassword);
        _users.GetUserById(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var ok = await _sut.ChangePassword(user.Id, ValidPassword, "weak", "keep", CancellationToken.None);

        Assert.IsFalse(ok);
    }

    // ---------- Guest bootstrap / first user ----------

    [Test]
    public async Task EnsureGuestSeeded_EmptyStore_InsertsGuestAdmin()
    {
        _users.CountUsers(Arg.Any<CancellationToken>()).Returns(0);

        await _sut.EnsureGuestSeeded(CancellationToken.None);

        await _users.Received(1).InsertUser(Arg.Is<User>(u =>
            u.Username == "guest" && u.IsGuest && u.Role == UserRoles.Admin && u.PasswordHash.Length > 0), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EnsureGuestSeeded_NonEmptyStore_DoesNothing()
    {
        _users.CountUsers(Arg.Any<CancellationToken>()).Returns(3);

        await _sut.EnsureGuestSeeded(CancellationToken.None);

        await _users.DidNotReceive().InsertUser(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateFirstUser_CreatesAdminAndLogsIn()
    {
        _users.GetUserByUsername("anna", Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _sut.CreateFirstUser("anna", ValidPassword, CancellationToken.None);

        Assert.AreEqual("anna", result.Username);
        Assert.AreEqual(UserRoles.Admin, result.Role);
        Assert.IsFalse(result.IsGuest);
        await _users.Received(1).InsertUser(Arg.Is<User>(u => u.Username == "anna" && u.Role == UserRoles.Admin && !u.IsGuest), Arg.Any<CancellationToken>());
        // The auto-login counts as the first real login, which deletes the guest user.
        await _users.Received(1).HardDeleteGuest(Arg.Any<CancellationToken>());
        await _users.Received(1).InsertSession(Arg.Any<Session>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public void CreateFirstUser_UsernameTaken_Throws()
    {
        _users.GetUserByUsername("anna", Arg.Any<CancellationToken>()).Returns(MakeUser("anna", ValidPassword));

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateFirstUser("anna", ValidPassword, CancellationToken.None));
    }

    [Test]
    public void CreateFirstUser_WeakPassword_Throws()
    {
        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateFirstUser("anna", "weak", CancellationToken.None));
    }
}
