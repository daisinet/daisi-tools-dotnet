using Daisi.SDK.Extensions;
using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;

namespace Daisi.Tools.Web.Clients
{
    public class HttpClientTool : DaisiToolBase
    {
        const string P_URL = "url";
        const string P_SUMMARIZE = "summarize";
        const string P_METHOD = "method";
        const string P_CONTENT = "content";
        const string P_MEDIATYPE = "media-type";

        public override string Name => "Daisi Web Client";

        public override string Description => "Use this tool to send an HTTP request to a URL. Returns HTML.";

        public override ToolParameter[] Parameters => new[]{
            new ToolParameter() { Name = P_URL, Description = "This is the fully qualified URL to send the request, including the protocol. Example - https://daisi.ai", IsRequired = true },
            new ToolParameter() { Name = P_SUMMARIZE, Description = "Options: \"True\" or \"False\". If True, the return content will be a summary on the page's main content. If false, the response will be the raw text returned from the URL. Default is False.", IsRequired = false },
            new ToolParameter() { Name = P_METHOD, Description = "Options: \"GET\",\"POST\",\"PUT\",\"PATCH\". The HTTP method to use for the request. Default is GET.", IsRequired = false },
            new ToolParameter() { Name = P_CONTENT, Description = "The content to POST, PUT, or PATCH. Not needed if method is \"GET\".", IsRequired = false },
            new ToolParameter() { Name = P_MEDIATYPE, Description = "The media type for the Content. Default is \"application/json\".", IsRequired = false }
        };

        public override ToolExecutionContext GetExecutionContext(IToolContext toolContext, CancellationToken cancellationToken, params ToolParameter[] parameters)
        {
            var pMethod = parameters.GetParameter(P_METHOD, false);
            var method = pMethod?.Values.FirstOrDefault() ?? "get";
            method = method.ToLower();

            var pUrl = parameters.GetParameter(P_URL);
            var url = pUrl.Values.FirstOrDefault();

            string? outgoingContent = null;
            string? mediaType = null;
            if (method != "get")
            {
                var pContent = parameters.GetParameter(P_CONTENT);
                outgoingContent = pContent.Values.FirstOrDefault();

                var pMediaType = parameters.GetParameter(P_MEDIATYPE, false);
                mediaType = pMediaType?.Values.FirstOrDefault() ?? "application/json";
            }

            var pSummarize = parameters.GetParameter(P_SUMMARIZE, false);
            var summarize = false;
            
            if(pSummarize is not null)
                bool.TryParse(pSummarize.Values.FirstOrDefault(), out summarize);

            var executionMessage = string.Format("HTTP {0}: {1} {2}", method.ToUpper(), url?.Left(30), summarize ? " (Summarize)" : "");

            var task = RunHttp(toolContext, method, url, mediaType, outgoingContent, summarize, cancellationToken);

            return new ToolExecutionContext() { ExecutionMessage = executionMessage, ExecutionTask = task };
        }
        

        public override string? ValidateGeneratedParameterValues(ToolParameter par)
        {
            var firstVal = par.Values.FirstOrDefault();
            if (par.Name == P_URL && (string.IsNullOrWhiteSpace(firstVal) || !IsUrl(firstVal)))
            {
                return $"The parameter \"{P_URL}\" is not formatted as a fully qualified url. Example: https://google.com";
            }

            return base.ValidateGeneratedParameterValues(par);
        }

        async Task<ToolResult> RunHttp(IToolContext toolContext, string method, string? url, string? mediaType, string? outgoingContent, bool summarize, CancellationToken cancellationToken)
        {
            try
            {
                var result = new ToolResult();

                IHttpClientFactory? httpClientFactory = toolContext.Services.GetService<IHttpClientFactory>();
                if (httpClientFactory is not null)
                {
                    using var client = httpClientFactory.CreateClient();

                    using HttpResponseMessage httpResponse =
                        method switch
                        {
                            "post" => await client.PostAsync(url, new StringContent(outgoingContent!, Encoding.UTF8, mediaType), cancellationToken),
                            "put" => await client.PutAsync(url, new StringContent(outgoingContent!, Encoding.UTF8, mediaType), cancellationToken),
                            "patch" => await client.PatchAsync(url, new StringContent(outgoingContent!, Encoding.UTF8, mediaType), cancellationToken),
                            _ => await client.GetAsync(url, cancellationToken)
                        };

                    httpResponse.EnsureSuccessStatusCode();

                    string responseBody = await httpResponse.Content.ReadAsStringAsync();

                    result.Output = responseBody;
                    result.OutputMessage = $"This is the HTML from {url} to use in your responses";
                    result.Success = true;
                    result.OutputFormat = Protos.V1.InferenceOutputFormats.Html;
                }
                else
                {
                    result.ErrorMessage = "HttpClientFactory is not available in the current context.";
                    result.Success = false;
                }

                return result;
            }
            catch (Exception ex)
            {
                return new ToolResult() { Success = false, ErrorMessage = ex.Message };
            }
        }
    }
}
