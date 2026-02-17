using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SecureToolProvider.Common;
using Daisi.SecureTools.Google;
using Daisi.SecureTools.Microsoft365;
using Daisi.SecureTools.Firecrawl;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ISetupStore, InMemorySetupStore>();
        services.AddSingleton<AuthValidator>();
        services.AddSingleton<GoogleServiceFactory>();
        services.AddSingleton<GraphClientFactory>();
        services.AddSingleton<FirecrawlClient>();
        services.AddHttpClient();
    })
    .Build();

host.Run();
