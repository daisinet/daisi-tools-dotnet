using Daisi.SDK.Clients.V1.Orc;
using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace Daisi.Tools.Drive
{
    public abstract class DriveToolBase : DaisiToolBase
    {
        protected static DriveClient? GetDriveClient(IToolContext toolContext, out ToolResult? error)
        {
            var factory = toolContext.Services.GetService<DriveClientFactory>();
            if (factory is null)
            {
                error = new ToolResult
                {
                    Success = false,
                    ErrorMessage = "Drive client is not available."
                };
                return null;
            }

            error = null;
            return factory.Create();
        }
    }
}
