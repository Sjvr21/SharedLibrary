using System;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using Shared.Config;
using Xunit;

namespace Shared.Tests.Config
{
    public class ConfigurationTests : IDisposable
    {
        private readonly string _tempFile;

        public ConfigurationTests()
        {
            _tempFile = Path.GetTempFileName();
        }

        public void Dispose()
        {
            if (File.Exists(_tempFile))
                File.Delete(_tempFile);

            // Reset static appConfiguration between tests
            var field = typeof(Configuration).GetField("appConfiguration",
                BindingFlags.NonPublic | BindingFlags.Static);
            field!.SetValue(null, null);
        }

        [Fact]
        public void LoadConfigurationFile_ParsesKeyValuePairs_SkipsCommentsAndEmptyLines()
        {
            File.WriteAllLines(_tempFile, new[]
            {
                "# comment line",
                "",
                "Key1=Value1",
                "Key2 = Value2  ",
                "  Key3  =  Value3=WithEquals "
            });

            var cfg = Configuration.LoadConfigurationFile(_tempFile);

            Assert.Equal("Value1", cfg["Key1"]);
            Assert.Equal("Value2", cfg["Key2"]);
            Assert.Equal("Value3=WithEquals", cfg["Key3"]);
            Assert.Null(cfg["# comment line"]);
        }

        private static void SetAppConfiguration(params (string Key, string Value)[] entries)
        {
            var sd = new StringDictionary();
            foreach (var (key, value) in entries)
            {
                sd[key] = value;
            }

            var field = typeof(Configuration).GetField("appConfiguration",
                BindingFlags.NonPublic | BindingFlags.Static);
            field!.SetValue(null, sd);
        }

        private enum TestEnum
        {
            None = 0,
            Monday = 1,
            Friday = 5
        }

        [Fact]
        public void Get_WithDefaultValue_ReturnsDefaultWhenKeyMissing()
        {
            SetAppConfiguration(("ExistingKey", "42"));

            var result = Configuration.Get("MissingKey", "fallback");

            Assert.Equal("fallback", result);
        }

        [Fact]
        public void Get_Generic_ReturnsDefaultWhenKeyMissingOrEmpty()
        {
            SetAppConfiguration(("EmptyKey", ""));

            int valueForMissing = Configuration.Get("MissingKey", 10);
            int valueForEmpty = Configuration.Get("EmptyKey", 20);

            Assert.Equal(10, valueForMissing);
            Assert.Equal(20, valueForEmpty);
        }

        [Fact]
        public void Get_Generic_ConvertsToBasicTypesUsingInvariantCulture()
        {
            SetAppConfiguration(
                ("MyInt", "123"),
                ("MyBoolTrue", "true"),
                ("MyBoolFalse", "FALSE"),
                ("MyDouble", "3.14"),
                ("MyEnum", "friday")
            );

            int myInt = Configuration.Get<int>("MyInt");
            bool myBoolTrue = Configuration.Get<bool>("MyBoolTrue");
            bool myBoolFalse = Configuration.Get<bool>("MyBoolFalse");
            double myDouble = Configuration.Get<double>("MyDouble");
            TestEnum myEnum = Configuration.Get<TestEnum>("MyEnum");

            Assert.Equal(123, myInt);
            Assert.True(myBoolTrue);
            Assert.False(myBoolFalse);
            Assert.Equal(3.14, myDouble, 3);
            Assert.Equal(TestEnum.Friday, myEnum);
        }

        [Fact]
        public void Get_Generic_ReturnsDefaultOnConversionError()
        {
            SetAppConfiguration(("MyInt", "not-an-int"));

            int value = Configuration.Get("MyInt", 99);

            Assert.Equal(99, value);
        }
    }
}
