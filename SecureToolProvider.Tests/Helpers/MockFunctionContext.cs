using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;

namespace SecureToolProvider.Tests.Helpers;

/// <summary>
/// Minimal mock FunctionContext for testing.
/// Registers a JsonObjectSerializer so WriteAsJsonAsync works.
/// </summary>
public class MockFunctionContext : FunctionContext
{
    public override string InvocationId => Guid.NewGuid().ToString();
    public override string FunctionId => "test-function";
    public override TraceContext TraceContext => throw new NotImplementedException();
    public override BindingContext BindingContext => throw new NotImplementedException();
    public override RetryContext RetryContext => throw new NotImplementedException();
    public override IServiceProvider InstanceServices { get; set; }
    public override FunctionDefinition FunctionDefinition => throw new NotImplementedException();
    public override IDictionary<object, object> Items { get; set; } = new Dictionary<object, object>();
    public override IInvocationFeatures Features => throw new NotImplementedException();

    public MockFunctionContext()
    {
        var services = new ServiceCollection();
        services.Configure<WorkerOptions>(options =>
        {
            options.Serializer = new Azure.Core.Serialization.JsonObjectSerializer();
        });
        InstanceServices = services.BuildServiceProvider();
    }
}
