using NUnit.Framework;
using pote.Config.Admin.Api.Auth;

namespace pote.Config.UnitTests.Auth;

[TestFixture]
public class UsernameRulesTests
{
    [TestCase("anna")]
    [TestCase("anna.smith@example.com")]
    [TestCase("A-b_c.d@e")]
    [TestCase("x")]
    public void Validate_ValidUsernames_ReturnsNull(string username)
    {
        Assert.IsNull(UsernameRules.Validate(username, out _));
    }

    [Test]
    public void Validate_Trims_ReturnsTrimmed()
    {
        Assert.IsNull(UsernameRules.Validate("  anna  ", out var trimmed));
        Assert.AreEqual("anna", trimmed);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("has space")]
    [TestCase("slash/name")]
    [TestCase("question?name")]
    [TestCase("semi;name")]
    [TestCase("tab\tname")]
    public void Validate_InvalidUsernames_ReturnsReason(string username)
    {
        Assert.IsNotNull(UsernameRules.Validate(username, out _));
    }

    [TestCase("guest")]
    [TestCase("GUEST")]
    [TestCase("Guest")]
    [TestCase(" guest ")]
    public void Validate_GuestReserved_CaseInsensitive(string username)
    {
        Assert.IsNotNull(UsernameRules.Validate(username, out _));
    }

    [Test]
    public void Validate_TooLong_ReturnsReason()
    {
        Assert.IsNotNull(UsernameRules.Validate(new string('a', 101), out _));
    }

    [Test]
    public void Validate_MaxLength_IsValid()
    {
        Assert.IsNull(UsernameRules.Validate(new string('a', 100), out _));
    }
}
