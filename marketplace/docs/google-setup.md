# Google Workspace — OAuth Setup

Sets up the OAuth 2.0 credentials for Google Workspace tools (Gmail, Drive, Calendar, Sheets).

## Prerequisites

- A Google Cloud account with billing enabled
- Access to the [Google Cloud Console](https://console.cloud.google.com/)

## Step 1: Create a Google Cloud Project

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Click the project dropdown at the top of the page
3. Click **New Project**
4. Enter a project name (e.g., `Daisi Secure Tools`)
5. Click **Create**
6. Select the new project from the project dropdown

## Step 2: Enable Required APIs

1. Go to **APIs & Services > Library**
2. Search for and enable each of the following APIs:
   - **Gmail API**
   - **Google Drive API**
   - **Google Calendar API**
   - **Google Sheets API**
3. For each API, click on it and then click **Enable**

## Step 3: Configure the OAuth Consent Screen

1. Go to **APIs & Services > OAuth consent screen**
2. Select **External** user type (allows any Google account to authenticate)
3. Click **Create**
4. Fill in the required fields:
   - **App name:** `Daisi AI`
   - **User support email:** your support email
   - **Developer contact information:** your email
5. Click **Save and Continue**
6. On the **Scopes** page, click **Add or Remove Scopes** and add:
   - `https://www.googleapis.com/auth/gmail.readonly`
   - `https://www.googleapis.com/auth/gmail.send`
   - `https://www.googleapis.com/auth/drive.readonly`
   - `https://www.googleapis.com/auth/calendar.events`
   - `https://www.googleapis.com/auth/spreadsheets`
7. Click **Update**, then **Save and Continue**
8. On the **Test users** page, add any test users (required while in testing mode)
9. Click **Save and Continue**

## Step 4: Create OAuth Credentials

1. Go to **APIs & Services > Credentials**
2. Click **+ Create Credentials > OAuth client ID**
3. Select **Web application** as the application type
4. Enter a name (e.g., `Daisi Secure Tools`)
5. Under **Authorized redirect URIs**, add:
   - `https://<your-function-app>.azurewebsites.net/api/google/auth/callback`
   - `https://localhost:7071/api/google/auth/callback` (for local development)
6. Click **Create**
7. Copy the **Client ID** and **Client Secret**

## Step 5: Configure App Settings

Add the following to your Azure Function App Settings (or `local.settings.json` for local dev):

| Setting | Value |
|---------|-------|
| `GoogleClientId` | The Client ID from Step 4 |
| `GoogleClientSecret` | The Client Secret from Step 4 |

## Step 6: Publish to Production (Optional)

While in **Testing** mode, only users added as test users can authenticate. To allow any Google user:

1. Go to **APIs & Services > OAuth consent screen**
2. Click **Publish App**
3. Confirm the publishing dialog
4. If your app requests sensitive scopes, Google may require a verification review

## Scopes Reference

| Scope | Purpose |
|-------|---------|
| `gmail.readonly` | Search, list, and read emails |
| `gmail.send` | Send emails |
| `drive.readonly` | Search and read Drive files |
| `calendar.events` | List and create calendar events |
| `spreadsheets` | Read and write Google Sheets data |

## Troubleshooting

- **"Access blocked: This app's request is invalid"** — Check that the redirect URI exactly matches what's configured in the Google Cloud Console, including trailing slashes
- **"This app isn't verified"** — Expected in testing mode. Users will see a warning screen but can click "Advanced" > "Go to app" to proceed
- **"Insufficient permissions"** — Verify all five APIs are enabled in the project
