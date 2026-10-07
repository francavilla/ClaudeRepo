using System.IO;
using BuildExe.Core.Analysis;
using Xunit;

namespace BuildExe.Core.Tests
{
    public class ConditionEvaluatorTests
    {
        private static bool Eval(string condition)
        {
            var properties = new PropertyBag();
            properties.SetGlobal("Configuration", "Release");
            properties.Set("Platform", "AnyCPU");
            properties.Set("Version", "4.8");
            return new ConditionEvaluator(properties, Path.GetTempPath()).Evaluate(condition);
        }

        [Theory]
        [InlineData(" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' ", true)]
        [InlineData(" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ", false)]
        [InlineData("'$(Configuration)' == 'release'", true)]
        [InlineData("'$(Undefined)' == ''", true)]
        [InlineData("'$(Configuration)' != 'Debug' and '$(Platform)' == 'AnyCPU'", true)]
        [InlineData("'$(Configuration)' == 'Debug' or ('$(Platform)' == 'x64')", false)]
        [InlineData("!('$(Configuration)' == 'Debug')", true)]
        [InlineData("$(Configuration) == Release", true)]
        [InlineData("'$(Version)' >= '4.7.2'", true)]
        [InlineData("'$(Version)' < '4.6'", false)]
        [InlineData("Exists('questo-file-non-esiste.props')", false)]
        [InlineData("HasTrailingSlash('C:\\out\\')", true)]
        [InlineData("true", true)]
        [InlineData("", true)]
        public void Valuta_le_condizioni_supportate(string condition, bool expected)
        {
            Assert.Equal(expected, Eval(condition));
        }

        [Theory]
        [InlineData("$([MSBuild]::IsOSPlatform('Windows'))")]
        [InlineData("'$([System.DateTime]::Now.Year)' == '2026'")]
        [InlineData("'a' = 'b'")]
        public void Condizioni_non_valutabili_sollevano_ConditionException(string condition)
        {
            Assert.Throws<ConditionException>(() => Eval(condition));
        }
    }
}
