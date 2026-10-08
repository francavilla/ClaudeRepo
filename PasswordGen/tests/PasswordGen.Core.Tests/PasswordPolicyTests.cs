using System.Linq;
using PasswordGen.Core.Policy;
using Xunit;

namespace PasswordGen.Core.Tests
{
    public class PasswordPolicyTests
    {
        [Fact]
        public void Defaults_AreTheCommonCorporateRules()
        {
            var policy = new PasswordPolicy();

            Assert.Equal(10, policy.MinLength);
            Assert.True(policy.RequireUpper);
            Assert.True(policy.RequireLower);
            Assert.True(policy.RequireDigit);
            Assert.True(policy.RequireSpecial);
        }

        [Fact]
        public void Validate_AcceptsCompliantPassword()
        {
            Assert.True(new PasswordPolicy().IsValid("Lampo-Cavallo47!"));
        }

        [Theory]
        [InlineData("Ab1!", "almeno 10")]
        [InlineData("lampo-cavallo47!", "maiuscola")]
        [InlineData("LAMPO-CAVALLO47!", "minuscola")]
        [InlineData("Lampo-Cavallo!!", "cifra")]
        [InlineData("LampoCavallo4747", "speciale")]
        [InlineData("Lampooo-Cavallo47!", "uguali consecutivi")]
        public void Validate_ReportsTheViolatedRule(string password, string expectedFragment)
        {
            var problems = new PasswordPolicy().Validate(password);

            Assert.Contains(problems, p => p.Contains(expectedFragment));
        }

        [Fact]
        public void Validate_RespectsDisabledRequirements()
        {
            var policy = new PasswordPolicy { RequireSpecial = false, RequireDigit = false };

            Assert.True(policy.IsValid("LampoCavalloNebbia"));
        }

        [Fact]
        public void Validate_NullPassword_IsInvalid()
        {
            Assert.False(new PasswordPolicy().IsValid(null));
        }

        [Fact]
        public void Normalized_ClampsLengthAndRestoresSpecials()
        {
            var policy = new PasswordPolicy { MinLength = 500, Specials = "" }.Normalized();

            Assert.Equal(PasswordPolicy.MaxAllowedLength, policy.MinLength);
            Assert.Equal(PasswordPolicy.DefaultSpecials, policy.Specials);
        }

        [Fact]
        public void Clone_IsIndependent()
        {
            var original = new PasswordPolicy();
            var copy = original.Clone();
            copy.MinLength = 20;

            Assert.Equal(10, original.MinLength);
        }
    }
}
