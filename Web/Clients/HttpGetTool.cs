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
    public class HttpGetTool : DaisiToolBase
    {
        const string P_URL = "url";
       // const string P_METHOD = "method";
       // const string P_CONTENT = "content";
       // const string P_MEDIATYPE = "media-type";
        public override string Id => "daisi-web-clients-http-get";
        public override string Name => "Daisi Http Get";
        public override string UseInstructions =>
            "Use this tool when a specific URL is provided and you need to fetch, read, or retrieve its content. " +
            "Sends HTTP GET to the URL and returns the response (HTML, JSON, or text). " +
            "Keywords: fetch url, get url, http get, read url, visit url, open url, download page. " +
            "ALWAYS use this FIRST when a URL needs to be summarized — fetch the content before summarizing. " +
            "Do NOT use for searching — use daisi-info-web-search for search queries.";

        public override ToolParameter[] Parameters => new[]{
            new ToolParameter() { Name = P_URL, Description = "This is the fully qualified URL to send the request, including the protocol. This MUST have at least one value sent.", IsRequired = true },
            //new ToolParameter() { Name = P_METHOD, Description = "The HTTP method to use for the request. Options: \"GET\",\"POST\",\"PUT\",\"PATCH\". Default is GET.", IsRequired = false },
            //new ToolParameter() { Name = P_CONTENT, Description = "The content to POST, PUT, or PATCH. Omit if METHOD  is \"GET\".", IsRequired = false },
            //new ToolParameter() { Name = P_MEDIATYPE, Description = "The media type for the Content sent to the URL. Default is \"application/json\". Omit if METHOD is \"GET\".", IsRequired = false }
        };

        public override ToolExecutionContext GetExecutionContext(IToolContext toolContext, CancellationToken cancellationToken, params ToolParameterBase[] parameters)
        {
            // var pMethod = parameters.GetParameter(P_METHOD, false);
            // var method = pMethod?.Values.FirstOrDefault() ?? "get";
            // method = method.ToLower();

            var pUrl = parameters.GetParameter(P_URL);
            var url = pUrl.Value;

            //string? outgoingContent = null;
            //string? mediaType = null;
            //if (method != "get")
            //{
            //    var pContent = parameters.GetParameter(P_CONTENT);
            //    outgoingContent = pContent.Values.FirstOrDefault();
            //     var pMediaType = parameters.GetParameter(P_MEDIATYPE, false);
            //     mediaType = pMediaType?.Values.FirstOrDefault() ?? "application/json";
            // }

            var executionMessage = string.Format("HTTP GET: {0}", url);

            var task = RunHttp(toolContext, "get", url, null, null, false, cancellationToken);

            return new ToolExecutionContext() { ExecutionMessage = executionMessage, ExecutionTask = task };
        }
        

        public override string? ValidateGeneratedParameterValues(ToolParameterBase par)
        {
            var firstVal = par.Value;
            if (par.Name == P_URL && (string.IsNullOrWhiteSpace(firstVal) || !IsUrl(firstVal)))
            {
                return $"The parameter \"{P_URL}\" is not formatted as a fully qualified url starting with https:// or http://";
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
                    result.OutputMessage = $"This is the HTML from \"{url}\" to use in your responses";
                    result.Success = true;

                    var responseMediaType = httpResponse.Content.Headers.ContentType?.MediaType;

                    if (responseMediaType?.Contains("html") ?? false)
                    {
                        result.OutputFormat = Protos.V1.InferenceOutputFormats.Html;
                        if(result.Output.Contains("<body"))
                        {
                            result.Output = result.Output.Substring(result.Output.IndexOf("<body"));
                        }
                    }
                    else if (responseMediaType?.Contains("json") ?? false)
                        result.OutputFormat = Protos.V1.InferenceOutputFormats.Json;
                    else if (responseMediaType?.Contains("markdown") ?? false)
                        result.OutputFormat = Protos.V1.InferenceOutputFormats.Markdown;
                    else
                        result.OutputFormat = Protos.V1.InferenceOutputFormats.PlainText;

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
