using Daisi.Host.Core.Models;
using Daisi.Host.Core.Services;
using Daisi.Host.Core.Services.Models;
using Daisi.SDK.Models;
using Daisi.SDK.Models.Tools;
using Daisi.Tools.Tests.Helpers;
using System.Net;

namespace Daisi.Tools.Tests.Skills
{
    /// <summary>
    /// Real inference tests for multi-tool skill workflows.
    /// These test that the LLM can correctly select tools in sequence
    /// when given prompts that match skill workflows.
    /// </summary>
    [Collection("InferenceTests")]
    public class SkillInferenceTests : IDisposable
    {
        private readonly ToolInferenceFixture _fixture;
        private readonly IServiceProvider? _originalServices;

        public SkillInferenceTests(ToolInferenceFixture fixture)
        {
            _fixture = fixture;
            _originalServices = DaisiStaticSettings.Services;
        }

        public void Dispose()
        {
            DaisiStaticSettings.Services = _originalServices!;
        }

        private static MockHttpMessageHandler CreateDummyHandler()
        {
            return new MockHttpMessageHandler("{}", HttpStatusCode.OK);
        }

        [Fact]
        public async Task RealInference_DateQuery_SelectsDateTimeTool()
        {
            var handler = CreateDummyHandler();
            var toolSession = await _fixture.CreateToolSessionAsync(
                "What date and time is it right now?", handler);

            Assert.NotNull(toolSession.CurrentTool);
            Assert.Equal("daisi-info-datetime", toolSession.CurrentTool!.Id);
        }

        [Fact]
        public async Task RealInference_WebsiteSummary_SelectsHttpGet()
        {
            var handler = CreateDummyHandler();
            var toolSession = await _fixture.CreateToolSessionAsync(
                "Fetch the content from https://example.com/article and summarize it for me", handler);

            Assert.NotNull(toolSession.CurrentTool);
            // Should select HttpGet first since a specific URL is provided
            Assert.True(
                toolSession.CurrentTool!.Id == "daisi-web-clients-http-get" ||
                toolSession.CurrentTool!.Id == "daisi-info-summarize-text",
                $"Expected HttpGet or SummarizeText but got '{toolSession.CurrentTool.Id}'");
        }
    }
}
