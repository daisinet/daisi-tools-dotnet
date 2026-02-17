using System.Text.Json;
using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Google.Tools;

/// <summary>
/// Read data from a Google Sheets spreadsheet by spreadsheet ID and range.
/// Returns the values as a JSON array.
/// </summary>
public class SheetsReadTool : IGoogleToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(
        GoogleServiceFactory serviceFactory, string accessToken, List<ParameterValue> parameters)
    {
        var spreadsheetId = parameters.FirstOrDefault(p => p.Name == "spreadsheetId")?.Value;
        var range = parameters.FirstOrDefault(p => p.Name == "range")?.Value;

        if (string.IsNullOrEmpty(spreadsheetId))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'spreadsheetId' parameter is required." };
        if (string.IsNullOrEmpty(range))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'range' parameter is required." };

        var service = serviceFactory.CreateSheetsService(accessToken);

        var getRequest = service.Spreadsheets.Values.Get(spreadsheetId, range);
        var response = await getRequest.ExecuteAsync();

        if (response.Values == null || response.Values.Count == 0)
        {
            return new ExecuteResponse
            {
                Success = true,
                Output = "[]",
                OutputFormat = "json",
                OutputMessage = $"No data found in range '{range}'."
            };
        }

        var output = JsonSerializer.Serialize(response.Values, new JsonSerializerOptions { WriteIndented = true });

        return new ExecuteResponse
        {
            Success = true,
            Output = output,
            OutputFormat = "json",
            OutputMessage = $"Read {response.Values.Count} row(s) from range '{range}'."
        };
    }
}
