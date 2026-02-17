using System.Text.Json;
using Google.Apis.Sheets.v4.Data;
using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Google.Tools;

/// <summary>
/// Write data to a Google Sheets spreadsheet at a specified range.
/// Accepts values as a JSON 2D array parameter.
/// </summary>
public class SheetsWriteTool : IGoogleToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(
        GoogleServiceFactory serviceFactory, string accessToken, List<ParameterValue> parameters)
    {
        var spreadsheetId = parameters.FirstOrDefault(p => p.Name == "spreadsheetId")?.Value;
        var range = parameters.FirstOrDefault(p => p.Name == "range")?.Value;
        var valuesJson = parameters.FirstOrDefault(p => p.Name == "values")?.Value;

        if (string.IsNullOrEmpty(spreadsheetId))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'spreadsheetId' parameter is required." };
        if (string.IsNullOrEmpty(range))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'range' parameter is required." };
        if (string.IsNullOrEmpty(valuesJson))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'values' parameter is required." };

        IList<IList<object>>? values;
        try
        {
            var parsed = JsonSerializer.Deserialize<List<List<JsonElement>>>(valuesJson);
            if (parsed == null || parsed.Count == 0)
                return new ExecuteResponse { Success = false, ErrorMessage = "The 'values' parameter must be a non-empty 2D array." };

            values = parsed.Select(row =>
                (IList<object>)row.Select(cell => (object)cell.ToString()!).ToList()
            ).ToList();
        }
        catch (JsonException)
        {
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'values' parameter must be valid JSON (2D array)." };
        }

        var service = serviceFactory.CreateSheetsService(accessToken);

        var valueRange = new ValueRange
        {
            Range = range,
            Values = values
        };

        var updateRequest = service.Spreadsheets.Values.Update(valueRange, spreadsheetId, range);
        updateRequest.ValueInputOption = global::Google.Apis.Sheets.v4.SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;

        var response = await updateRequest.ExecuteAsync();

        var result = new
        {
            spreadsheetId = response.SpreadsheetId,
            updatedRange = response.UpdatedRange,
            updatedRows = response.UpdatedRows,
            updatedColumns = response.UpdatedColumns,
            updatedCells = response.UpdatedCells
        };

        var output = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });

        return new ExecuteResponse
        {
            Success = true,
            Output = output,
            OutputFormat = "json",
            OutputMessage = $"Updated {response.UpdatedCells} cell(s) in range '{response.UpdatedRange}'."
        };
    }
}
