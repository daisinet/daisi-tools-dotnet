using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;
using MathEvaluation;
using MathEvaluation.Context;
using MathEvaluation.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Daisi.Tools.Math
{
    public class BasicMathTool : IDaisiTool
    {
        const string P_EXPRESSION = "expression";
        const string P_CULTURE = "culture";
        const string P_MATHCONTEXT = "math-context";

        public string Name => "Daisi Math";

        public string Description => "Evaluates basic math expressions and returns a single result from the expression";

        public ToolParameter[] Parameters => new ToolParameter[]{
            new ToolParameter(){ Name = P_EXPRESSION, Description = "This is the math expression that is to be evaluated for a single value", IsRequired = true },
            new ToolParameter(){ Name = P_CULTURE, Description = "This is culture to use when evaluating the expression. Default is \"en-US\".", IsRequired = false },
            new ToolParameter(){ Name = P_MATHCONTEXT, Description = "Optional Values: \"basic\"-general numeric math without functions; \"scientific\"-basic math plus supports for all trigonomic functions; \"programming\"-basic math, plus supports programming notation such as floor division (\"//\"), exponentiation (\"**\"), and modulo (\"%\") operations; \"dotnet\"-dotnet math syntax and allows for all dotnet 10 math functions and types. Default is \"basic\".", IsRequired = false }
        }; 

        public ToolExecutionContext GetExecutionContext(IToolContext toolContext, CancellationToken cancellationToken, params ToolParameter[] parameters)
        {
            string executionMessage = $"Evaluating Expression Using Basic Math Tool";
            var task = Task.Run(() =>
            {
                try
                {
                    var expressionParam = parameters.GetParameter(P_EXPRESSION);
                    var cultureParam = parameters.GetParameter(P_CULTURE, false);
                    var contextParam = parameters.GetParameter(P_MATHCONTEXT, false);
                    var e = expressionParam!.Values.FirstOrDefault();
                    var c = cultureParam!.Values.FirstOrDefault() ?? "en-US";
                    var mc = contextParam!.Values.FirstOrDefault() ?? "basic";

                    MathContext mathContext =
                        mc.ToLower() switch
                        {
                            "scientific" => new ScientificMathContext(),
                            "dotnet" => new DotNetMathContext(),
                            _ => new MathContext()
                        };

                    using var express = new MathExpression(e!, mathContext, new CultureInfo(c));

                    string outputMessage = string.Empty;
                    express.Evaluating += (object? sender, EvaluatingEventArgs args) =>
                    {
                        outputMessage += string.Format("{0}: {1} = {2}; {3}\n",
                            args.Step,
                            args.MathString[args.Start..(args.End + 1)],
                            args.Value,
                            args.IsCompleted ? " //completed" : string.Empty
                            );
                    };

                    var result = express.Evaluate();

                    return new ToolResult() { 
                        Output = result.ToString(), 
                        OutputMessage = outputMessage, 
                        OutputFormat = Daisi.Protos.V1.InferenceOutputFormats.PlainText, 
                        Success = true 
                    };
                }
                catch (Exception ex)
                {
                    return new ToolResult() { Success = false, ErrorMessage = ex.Message };
                }
            });
            return new ToolExecutionContext() { ExecutionMessage = executionMessage, ExecutionTask = task };
        }
    }
}
