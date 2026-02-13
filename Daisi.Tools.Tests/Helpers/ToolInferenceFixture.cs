using Daisi.Host.Core.Models;
using Daisi.Host.Core.Services;
using Daisi.Host.Core.Services.Interfaces;
using Daisi.Host.Core.Services.Models;
using Daisi.Protos.V1;
using Daisi.SDK.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Daisi.Tools.Tests.Helpers
{
    /// <summary>
    /// Shared xUnit fixture that loads a local GGUF model and ToolService
    /// for inference-based tool selection tests.
    ///
    /// GGUF models are stored at: C:\ggufs
    /// </summary>
    public class ToolInferenceFixture : IAsyncLifetime
    {
        private const string ModelFolderPath = @"C:\ggufs";
        private const string ModelFileName = "gemma-3-4b-it-Q4_K_M.gguf";

        public LocalModel LocalModel { get; private set; } = null!;
        public ToolService ToolService { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            var settingsService = new TestSettingsService(ModelFolderPath);
            var logger = NullLogger<ToolService>.Instance;

            var aiModel = new AIModel
            {
                Name = "gemma-3-4b-it-Q4",
                FileName = ModelFileName,
                Enabled = true,
                IsDefault = true
            };

            LocalModel = new LocalModel(aiModel, NullLogger.Instance, settingsService);
            LocalModel.Load();

            ToolService = new ToolService(settingsService, logger);
            ToolService.LoadTools();

            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            if (LocalModel is not null)
                await LocalModel.DisposeAsync();
        }

        public async Task<ToolSession> CreateToolSessionAsync(string prompt, HttpMessageHandler handler)
        {
            var serviceProvider = BuildServices(handler);
            DaisiStaticSettings.Services = serviceProvider;

            var chatSession = await LocalModel.CreateInteractiveChatSessionAsync();
            return await ToolService.CreateToolSessionFromUserInput(prompt, LocalModel, chatSession);
        }

        public MockToolContext CreateToolContext()
        {
            return new MockToolContext();
        }

        private static ServiceProvider BuildServices(HttpMessageHandler handler)
        {
            var services = new ServiceCollection();
            services.AddHttpClient(string.Empty)
                .ConfigurePrimaryHttpMessageHandler(() => handler);
            return services.BuildServiceProvider();
        }

        private class TestSettingsService : ISettingsService
        {
            public Settings Settings { get; set; }

            public TestSettingsService(string modelFolderPath)
            {
                Settings = new Settings
                {
                    Model = new ModelSettings
                    {
                        ModelFolderPath = modelFolderPath,
                        LLama = new LLamaSettings
                        {
                            ContextSize = 4096,
                            GpuLayerCount = -1,
                            BatchSize = 512
                        }
                    }
                };
            }

            public string GetRootFolder() => Path.GetTempPath();
            public T ParseJson<T>(string json, bool discardUnknownFields = true) where T : Google.Protobuf.IMessage<T>, new()
                => throw new NotImplementedException();
            public Task LoadAsync() => Task.CompletedTask;
            public Task SaveAsync() => Task.CompletedTask;
        }
    }

    [CollectionDefinition("InferenceTests")]
    public class InferenceTestCollection : ICollectionFixture<ToolInferenceFixture>
    {
    }
}
