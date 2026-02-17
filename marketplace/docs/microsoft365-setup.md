# Microsoft 365 — OAuth Setup

Sets up the OAuth 2.0 credentials for Microsoft 365 tools (Outlook Mail, OneDrive, Calendar, Teams messaging).

## Prerequisites

- An Azure account with access to [Microsoft Entra ID](https://entra.microsoft.com/) (formerly Azure Active Directory)
- Permission to register applications in your tenant

## Step 1: Register an Application

1. Go to [Microsoft Entra admin center](https://entra.microsoft.com/)
2. Navigate to **Identity > Applications > App registrations**
3. Click **+ New registration**
4. Fill in:
   - **Name:** `Daisi Secure Tools - M365`
   - **Supported account types:** Select **Accounts in any organizational directory and personal Microsoft accounts** (multi-tenant)
   - **Redirect URI:** Select **Web** and enter `https://<your-function-app>.azurewebsites.net/api/m365/auth/callback`
5. Click **Register**
6. On the overview page, copy the **Application (client) ID** — this is your `MicrosoftClientId`

## Step 2: Create a Client Secret

1. In the app registration, go to **Certificates & secrets**
2. Click **+ New client secret**
3. Enter a description (e.g., `Daisi Secure Tools`) and choose an expiration period
4. Click **Add**
5. Copy the **Value** immediately — this is your `MicrosoftClientSecret` (it won't be shown again)

## Step 3: Configure API Permissions

1. Go to **API permissions**
2. Click **+ Add a permission**
3. Select **Microsoft Graph**
4. Select **Delegated permissions**
5. Add the following permissions:
   - `Mail.Read` — Read user's mail
   - `Mail.Send` — Send mail as the user
   - `Files.Read` — Read user's OneDrive files
   - `Calendars.ReadWrite` — Read and write calendar events
   - `ChannelMessage.Send` — Send messages to Teams channels
   - `User.Read` — Read basic user profile
   - `offline_access` — Maintain access (refresh tokens)
6. Click **Add permissions**
7. If you have admin access, click **Grant admin consent for [tenant]** to pre-approve the permissions (optional — users will be prompted otherwise)

## Step 4: Add Additional Redirect URIs

1. Go to **Authentication**
2. Under **Web > Redirect URIs**, add:
   - `https://<your-function-app>.azurewebsites.net/api/m365/auth/callback`
   - `https://localhost:7071/api/m365/auth/callback` (for local development)
3. Click **Save**

## Step 5: Configure App Settings

Add the following to your Azure Function App Settings (or `local.settings.json` for local dev):

| Setting | Value |
|---------|-------|
| `MicrosoftClientId` | The Application (client) ID from Step 1 |
| `MicrosoftClientSecret` | The client secret Value from Step 2 |
| `MicrosoftTenantId` | `common` (allows any Microsoft account to authenticate) |

Setting `MicrosoftTenantId` to `common` enables multi-tenant authentication. If you want to restrict to a specific organization, use that tenant's ID instead.

## Permissions Reference

| Permission | Type | Purpose |
|-----------|------|---------|
| `Mail.Read` | Delegated | Search, list, and read Outlook emails |
| `Mail.Send` | Delegated | Send emails via Outlook |
| `Files.Read` | Delegated | Search and read OneDrive files |
| `Calendars.ReadWrite` | Delegated | List and create calendar events |
| `ChannelMessage.Send` | Delegated | Post messages to Teams channels |
| `User.Read` | Delegated | Read basic user profile info |
| `offline_access` | Delegated | Get refresh tokens for long-lived access |

## Troubleshooting

- **"AADSTS50011: The reply URL does not match"** — The redirect URI in the request doesn't match any URI registered in the app. Check for exact match including protocol and trailing slashes
- **"AADSTS65001: The user or administrator has not consented"** — The user needs to grant consent. Ensure the consent prompt appears during the OAuth flow
- **"AADSTS7000218: The request body must contain client_assertion or client_secret"** — The client secret may have expired. Generate a new one in Certificates & secrets
- **Client secret expired** — Secrets have a maximum lifetime. Set a calendar reminder to rotate before expiration
