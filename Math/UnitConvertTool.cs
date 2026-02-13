using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;
using System.Globalization;

namespace Daisi.Tools.Math
{
    public class UnitConvertTool : DaisiToolBase
    {
        private const string P_VALUE = "value";
        private const string P_FROM = "from";
        private const string P_TO = "to";

        public override string Id => "daisi-math-convert";
        public override string Name => "Daisi Unit Convert";

        public override string UseInstructions =>
            "Use this tool to convert between measurement units like kilometers to miles, celsius to fahrenheit, " +
            "kilograms to pounds, liters to gallons. Provide the numeric value, source unit, and target unit. " +
            "Keywords: convert units, kilometers, miles, celsius, fahrenheit, kg, lbs, temperature, distance, weight, volume, speed. " +
            "Do NOT use for math calculations — use daisi-math-basic for arithmetic.";

        public override ToolParameter[] Parameters => [
            new ToolParameter() { Name = P_VALUE, Description = "The numeric value to convert.", IsRequired = true },
            new ToolParameter() { Name = P_FROM, Description = "The source unit (e.g. \"km\", \"miles\", \"kg\", \"lbs\", \"celsius\", \"fahrenheit\").", IsRequired = true },
            new ToolParameter() { Name = P_TO, Description = "The target unit to convert to.", IsRequired = true }
        ];

        public override ToolExecutionContext GetExecutionContext(IToolContext toolContext, CancellationToken cancellation, params ToolParameterBase[] parameters)
        {
            var valueStr = parameters.GetParameterValueOrDefault(P_VALUE);
            var from = parameters.GetParameterValueOrDefault(P_FROM);
            var to = parameters.GetParameterValueOrDefault(P_TO);

            return new ToolExecutionContext
            {
                ExecutionMessage = $"Converting {valueStr} {from} to {to}",
                ExecutionTask = Task.Run(() => Execute(valueStr, from, to))
            };
        }

        internal static ToolResult Execute(string valueStr, string from, string to)
        {
            try
            {
                if (!double.TryParse(valueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                    return new ToolResult { Success = false, ErrorMessage = $"Invalid numeric value: '{valueStr}'" };

                var fromNorm = Normalize(from);
                var toNorm = Normalize(to);

                // Temperature special case
                if (IsTemperature(fromNorm) && IsTemperature(toNorm))
                {
                    var result = ConvertTemperature(value, fromNorm, toNorm);
                    return new ToolResult
                    {
                        Output = $"{result.ToString("G", CultureInfo.InvariantCulture)} {to}",
                        OutputMessage = $"{value} {from} = {result.ToString("G", CultureInfo.InvariantCulture)} {to}",
                        OutputFormat = Protos.V1.InferenceOutputFormats.PlainText,
                        Success = true
                    };
                }

                // Linear conversion via base units
                if (!ConversionFactors.TryGetValue(fromNorm, out var fromInfo))
                    return new ToolResult { Success = false, ErrorMessage = $"Unknown unit: '{from}'" };

                if (!ConversionFactors.TryGetValue(toNorm, out var toInfo))
                    return new ToolResult { Success = false, ErrorMessage = $"Unknown unit: '{to}'" };

                if (fromInfo.Category != toInfo.Category)
                    return new ToolResult { Success = false, ErrorMessage = $"Cannot convert between {fromInfo.Category} and {toInfo.Category}" };

                var baseValue = value * fromInfo.ToBase;
                var converted = baseValue / toInfo.ToBase;

                return new ToolResult
                {
                    Output = $"{converted.ToString("G", CultureInfo.InvariantCulture)} {to}",
                    OutputMessage = $"{value} {from} = {converted.ToString("G", CultureInfo.InvariantCulture)} {to}",
                    OutputFormat = Protos.V1.InferenceOutputFormats.PlainText,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new ToolResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        private static bool IsTemperature(string unit) => unit is "celsius" or "fahrenheit" or "kelvin";

        internal static double ConvertTemperature(double value, string from, string to)
        {
            // Convert to Celsius first
            var celsius = from switch
            {
                "fahrenheit" => (value - 32) * 5.0 / 9.0,
                "kelvin" => value - 273.15,
                _ => value // celsius
            };

            // Convert from Celsius to target
            return to switch
            {
                "fahrenheit" => celsius * 9.0 / 5.0 + 32,
                "kelvin" => celsius + 273.15,
                _ => celsius // celsius
            };
        }

        private static string Normalize(string unit) => unit.Trim().ToLowerInvariant() switch
        {
            "c" or "°c" or "celsius" or "centigrade" => "celsius",
            "f" or "°f" or "fahrenheit" => "fahrenheit",
            "k" or "kelvin" => "kelvin",
            "km" or "kilometer" or "kilometers" or "kilometre" or "kilometres" => "km",
            "m" or "meter" or "meters" or "metre" or "metres" => "m",
            "cm" or "centimeter" or "centimeters" or "centimetre" or "centimetres" => "cm",
            "mm" or "millimeter" or "millimeters" or "millimetre" or "millimetres" => "mm",
            "mi" or "mile" or "miles" => "mi",
            "yd" or "yard" or "yards" => "yd",
            "ft" or "foot" or "feet" => "ft",
            "in" or "inch" or "inches" => "in",
            "kg" or "kilogram" or "kilograms" => "kg",
            "g" or "gram" or "grams" => "g",
            "mg" or "milligram" or "milligrams" => "mg",
            "lb" or "lbs" or "pound" or "pounds" => "lb",
            "oz" or "ounce" or "ounces" => "oz",
            "l" or "liter" or "liters" or "litre" or "litres" => "l",
            "ml" or "milliliter" or "milliliters" or "millilitre" or "millilitres" => "ml",
            "gal" or "gallon" or "gallons" => "gal",
            "qt" or "quart" or "quarts" => "qt",
            "pt" or "pint" or "pints" => "pt",
            "cup" or "cups" => "cup",
            "floz" or "fl oz" or "fluid ounce" or "fluid ounces" => "floz",
            "kmh" or "km/h" or "kph" => "kmh",
            "mph" or "mi/h" => "mph",
            "ms" or "m/s" => "ms",
            var s => s
        };

        private record UnitInfo(string Category, double ToBase);

        private static readonly Dictionary<string, UnitInfo> ConversionFactors = new()
        {
            // Length (base: meters)
            ["km"] = new("length", 1000),
            ["m"] = new("length", 1),
            ["cm"] = new("length", 0.01),
            ["mm"] = new("length", 0.001),
            ["mi"] = new("length", 1609.344),
            ["yd"] = new("length", 0.9144),
            ["ft"] = new("length", 0.3048),
            ["in"] = new("length", 0.0254),

            // Weight (base: grams)
            ["kg"] = new("weight", 1000),
            ["g"] = new("weight", 1),
            ["mg"] = new("weight", 0.001),
            ["lb"] = new("weight", 453.592),
            ["oz"] = new("weight", 28.3495),

            // Volume (base: liters)
            ["l"] = new("volume", 1),
            ["ml"] = new("volume", 0.001),
            ["gal"] = new("volume", 3.78541),
            ["qt"] = new("volume", 0.946353),
            ["pt"] = new("volume", 0.473176),
            ["cup"] = new("volume", 0.236588),
            ["floz"] = new("volume", 0.0295735),

            // Speed (base: m/s)
            ["kmh"] = new("speed", 0.277778),
            ["mph"] = new("speed", 0.44704),
            ["ms"] = new("speed", 1),
        };
    }
}
