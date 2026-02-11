using Daisi.SDK.Models.Tools;
using Daisi.Tools.Math;
using Daisi.Tools.Tests.Helpers;

namespace Daisi.Tools.Tests.Math
{
    public class BasicMathToolTests
    {
        [Fact]
        public void Id_ReturnsExpectedValue()
        {
            var tool = new BasicMathTool();
            Assert.Equal("daisi-math-basic", tool.Id);
        }

        [Fact]
        public void Parameters_ExpressionIsRequired()
        {
            var tool = new BasicMathTool();
            Assert.True(tool.Parameters.First(p => p.Name == "expression").IsRequired);
        }

        [Fact]
        public void Parameters_CultureIsOptional()
        {
            var tool = new BasicMathTool();
            Assert.False(tool.Parameters.First(p => p.Name == "culture").IsRequired);
        }

        [Fact]
        public void Parameters_MathContextIsOptional()
        {
            var tool = new BasicMathTool();
            Assert.False(tool.Parameters.First(p => p.Name == "math-context").IsRequired);
        }

        [Fact]
        public async Task Execute_SimpleAddition_ReturnsCorrectResult()
        {
            var tool = new BasicMathTool();
            var context = new MockToolContext();
            var parameters = new ToolParameterBase[]
            {
                new() { Name = "expression", Value = "2 + 3", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.True(result.Success);
            Assert.Equal("5", result.Output);
        }

        [Fact]
        public async Task Execute_Multiplication_ReturnsCorrectResult()
        {
            var tool = new BasicMathTool();
            var context = new MockToolContext();
            var parameters = new ToolParameterBase[]
            {
                new() { Name = "expression", Value = "7 * 8", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.True(result.Success);
            Assert.Equal("56", result.Output);
        }

        [Fact]
        public async Task Execute_InvalidExpression_ReturnsFailure()
        {
            var tool = new BasicMathTool();
            var context = new MockToolContext();
            var parameters = new ToolParameterBase[]
            {
                new() { Name = "expression", Value = "invalid expression @@#$", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.False(result.Success);
        }
    }
}
