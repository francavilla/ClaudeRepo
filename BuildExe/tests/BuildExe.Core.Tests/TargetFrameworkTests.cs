using System;
using BuildExe.Core.Model;
using Xunit;

namespace BuildExe.Core.Tests
{
    public class TargetFrameworkTests
    {
        [Theory]
        [InlineData("net48", FrameworkFamily.NetFramework, "4.8", ".NET Framework 4.8")]
        [InlineData("net472", FrameworkFamily.NetFramework, "4.7.2", ".NET Framework 4.7.2")]
        [InlineData("net462", FrameworkFamily.NetFramework, "4.6.2", ".NET Framework 4.6.2")]
        [InlineData("net35", FrameworkFamily.NetFramework, "3.5", ".NET Framework 3.5")]
        [InlineData("net8.0", FrameworkFamily.NetCore, "8.0", ".NET 8.0")]
        [InlineData("net8.0-windows", FrameworkFamily.NetCore, "8.0", ".NET 8.0 (Windows)")]
        [InlineData("net6.0-windows10.0.19041.0", FrameworkFamily.NetCore, "6.0", ".NET 6.0 (Windows)")]
        [InlineData("netcoreapp3.1", FrameworkFamily.NetCore, "3.1", ".NET Core 3.1")]
        [InlineData("netstandard2.0", FrameworkFamily.NetStandard, "2.0", ".NET Standard 2.0")]
        [InlineData("NET10.0", FrameworkFamily.NetCore, "10.0", ".NET 10.0")]
        public void Parse_riconosce_i_moniker(string moniker, FrameworkFamily family, string version, string display)
        {
            var tfm = TargetFramework.Parse(moniker);

            Assert.Equal(family, tfm.Family);
            Assert.Equal(Version.Parse(version), tfm.Version);
            Assert.Equal(display, tfm.DisplayName);
            Assert.Equal(moniker, tfm.Moniker);
        }

        [Fact]
        public void Parse_moniker_sconosciuto_non_produce_eseguibili()
        {
            var tfm = TargetFramework.Parse("uap10.0");

            Assert.Equal(FrameworkFamily.Unknown, tfm.Family);
            Assert.False(tfm.CanProduceExecutable);
        }

        [Theory]
        [InlineData("v4.8", "net48")]
        [InlineData("v4.7.2", "net472")]
        [InlineData("v4.0", "net40")]
        [InlineData("v2.0", "net20")]
        public void FromIdentifier_progetti_classici(string version, string expectedMoniker)
        {
            var tfm = TargetFramework.FromIdentifier(null, version);

            Assert.Equal(FrameworkFamily.NetFramework, tfm.Family);
            Assert.Equal(expectedMoniker, tfm.Moniker);
        }

        [Fact]
        public void Parse_forma_estesa()
        {
            var tfm = TargetFramework.Parse(".NETFramework,Version=v4.6.1");

            Assert.Equal(FrameworkFamily.NetFramework, tfm.Family);
            Assert.Equal(new Version(4, 6, 1), tfm.Version);
        }
    }
}
