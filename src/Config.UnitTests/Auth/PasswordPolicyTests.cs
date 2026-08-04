using NUnit.Framework;
using pote.Config.Admin.Api.Auth;

namespace pote.Config.UnitTests.Auth;

[TestFixture]
public class PasswordPolicyTests
{
    [TestCase("Abcdefgh1!abcdef")] // exactly 16, all classes
    [TestCase("LongerPassword1!WithMoreChars")]
    public void Validate_ValidPasswords_ReturnsNull(string password)
    {
        Assert.IsNull(PasswordPolicy.Validate(password));
    }

    [TestCase("Ab1!x")]                    // too short
    [TestCase("abcdefgh1!abcdef")]         // no uppercase
    [TestCase("ABCDEFGH1!ABCDEF")]         // no lowercase
    [TestCase("Abcdefgh!abcdefg")]         // no digit
    [TestCase("Abcdefgh1abcdefg")]         // no special
    [TestCase("")]                         // empty
    public void Validate_InvalidPasswords_ReturnsReason(string password)
    {
        Assert.IsNotNull(PasswordPolicy.Validate(password));
    }

    [Test]
    public void Validate_TooLong_ReturnsReason()
    {
        var password = "Aa1!" + new string('x', 130);
        Assert.IsNotNull(PasswordPolicy.Validate(password));
    }
}
