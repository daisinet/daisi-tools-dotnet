using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Drive.v3;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;

namespace Daisi.SecureTools.Google;

/// <summary>
/// Factory that creates Google API service objects from an OAuth access token.
/// Each service is created with a GoogleCredential scoped to the provided token.
/// </summary>
public class GoogleServiceFactory
{
    private const string ApplicationName = "Daisi SecureTools Google";

    /// <summary>
    /// Create a GmailService authenticated with the given access token.
    /// </summary>
    public GmailService CreateGmailService(string accessToken)
    {
        return new GmailService(CreateInitializer(accessToken));
    }

    /// <summary>
    /// Create a DriveService authenticated with the given access token.
    /// </summary>
    public DriveService CreateDriveService(string accessToken)
    {
        return new DriveService(CreateInitializer(accessToken));
    }

    /// <summary>
    /// Create a CalendarService authenticated with the given access token.
    /// </summary>
    public CalendarService CreateCalendarService(string accessToken)
    {
        return new CalendarService(CreateInitializer(accessToken));
    }

    /// <summary>
    /// Create a SheetsService authenticated with the given access token.
    /// </summary>
    public SheetsService CreateSheetsService(string accessToken)
    {
        return new SheetsService(CreateInitializer(accessToken));
    }

    private static BaseClientService.Initializer CreateInitializer(string accessToken)
    {
        var credential = GoogleCredential.FromAccessToken(accessToken);
        return new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = ApplicationName
        };
    }
}
