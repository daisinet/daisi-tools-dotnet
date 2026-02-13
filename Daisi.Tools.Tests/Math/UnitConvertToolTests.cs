using Daisi.SDK.Models.Tools;
using Daisi.Tools.Math;
using Daisi.Tools.Tests.Helpers;

namespace Daisi.Tools.Tests.Math
{
    public class UnitConvertToolTests
    {
        [Fact]
        public void Id_ReturnsExpectedValue()
        {
            var tool = new UnitConvertTool();
            Assert.Equal("daisi-math-convert", tool.Id);
        }

        [Fact]
        public void Parameters_ValueIsRequired()
        {
            var tool = new UnitConvertTool();
            Assert.True(tool.Parameters.First(p => p.Name == "value").IsRequired);
        }

        [Fact]
        public void Parameters_FromIsRequired()
        {
            var tool = new UnitConvertTool();
            Assert.True(tool.Parameters.First(p => p.Name == "from").IsRequired);
        }

        [Fact]
        public void Parameters_ToIsRequired()
        {
            var tool = new UnitConvertTool();
            Assert.True(tool.Parameters.First(p => p.Name == "to").IsRequired);
        }

        [Fact]
        public void Execute_KmToMiles()
        {
            var result = UnitConvertTool.Execute("100", "km", "miles");
            Assert.True(result.Success);
            Assert.Contains("62.1371", result.Output);
        }

        [Fact]
        public void Execute_CelsiusToFahrenheit()
        {
            var result = UnitConvertTool.Execute("100", "celsius", "fahrenheit");
            Assert.True(result.Success);
            Assert.Contains("212", result.Output);
        }

        [Fact]
        public void Execute_FahrenheitToCelsius()
        {
            var result = UnitConvertTool.Execute("32", "fahrenheit", "celsius");
            Assert.True(result.Success);
            Assert.Contains("0", result.Output);
        }

        [Fact]
        public void Execute_KgToLbs()
        {
            var result = UnitConvertTool.Execute("1", "kg", "lbs");
            Assert.True(result.Success);
            Assert.Contains("2.20462", result.Output);
        }

        [Fact]
        public void Execute_LitersToGallons()
        {
            var result = UnitConvertTool.Execute("3.78541", "liters", "gallons");
            Assert.True(result.Success);
            Assert.Contains("1", result.Output);
        }

        [Fact]
        public void Execute_InvalidValue_ReturnsError()
        {
            var result = UnitConvertTool.Execute("abc", "km", "miles");
            Assert.False(result.Success);
            Assert.Contains("Invalid numeric value", result.ErrorMessage);
        }

        [Fact]
        public void Execute_UnknownUnit_ReturnsError()
        {
            var result = UnitConvertTool.Execute("1", "foobar", "miles");
            Assert.False(result.Success);
            Assert.Contains("Unknown unit", result.ErrorMessage);
        }

        [Fact]
        public void Execute_IncompatibleUnits_ReturnsError()
        {
            var result = UnitConvertTool.Execute("1", "km", "kg");
            Assert.False(result.Success);
            Assert.Contains("Cannot convert", result.ErrorMessage);
        }

        [Fact]
        public void ConvertTemperature_CelsiusToKelvin()
        {
            var result = UnitConvertTool.ConvertTemperature(0, "celsius", "kelvin");
            Assert.Equal(273.15, result);
        }

        [Fact]
        public async Task Execute_ViaContext_Works()
        {
            var tool = new UnitConvertTool();
            var context = new MockToolContext();
            var parameters = new ToolParameterBase[]
            {
                new() { Name = "value", Value = "100", IsRequired = true },
                new() { Name = "from", Value = "cm", IsRequired = true },
                new() { Name = "to", Value = "m", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.True(result.Success);
            Assert.Contains("1", result.Output);
        }
    }
}
