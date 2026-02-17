using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SecureToolProvider.Common;
using Daisi.SecureTools.Google;
using Daisi.SecureTools.Microsoft365;
using Daisi.SecureTools.Firecrawl;
using Daisi.SecureTools.Social;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        var storageConnection = context.Configuration["AzureWebJobsStorage"];
        services.AddSingleton(new TableServiceClient(storageConnection));
        services.AddSingleton<PersistentSetupStore>();
        services.AddSingleton<ISetupStore>(sp => sp.GetRequiredService<PersistentSetupStore>());
        services.AddSingleton<AuthValidator>();
        services.AddSingleton<GoogleServiceFactory>();
        services.AddSingleton<GraphClientFactory>();
        services.AddSingleton<FirecrawlClient>();
        services.AddSingleton<SocialHttpClient>();
        services.AddHttpClient();
    })
    .Build();

// Initialize storage tables
var setupStore = host.Services.GetRequiredService<PersistentSetupStore>();
await setupStore.InitializeAsync();

host.Run();
