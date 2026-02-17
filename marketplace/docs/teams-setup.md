# Microsoft Teams — OAuth Setup for Messaging

Sets up the OAuth 2.0 credentials for the Teams messaging tool (send messages to Teams chats). This uses a **separate** app registration from the Microsoft 365 integration.

## Prerequisites

- An Azure account with access to [Microsoft Entra ID](https://entra.microsoft.com/)
- Permission to register applications in your tenant

## Why a Separate App?

The Teams messaging tool uses different OAuth scopes than the M365 integration (Outlook, OneDrive, Calendar). Keeping them separate allows users to authorize only the permissions they need. A user might want Teams messaging without granting access to their email.

## Step 1: Register an Application

1. Go to [Microsoft Entra admin center](https://entra.microsoft.com/)
2. Navigate to **Identity > Applications > App registrations**
3. Click **+ New registration**
4. Fill in:
   - **Name:** `Daisi Secure Tools - Teams`
   - **Supported account types:** Select **Accounts in any organizational directory and personal Microsoft accounts**
   - **Redirect URI:** Select **Web** and enter `https://<your-function-app>.azurewebsites.net/api/comms/teams/auth/callback`
5. Click **Register**
6. Copy the **Application (client) ID** — this is your `TeamsClientId`

## Step 2: Create a Client Secret

1. Go to **Certificates & secrets**
2. Click **+ New client secret**
3. Enter a description (e.g., `Daisi Teams Integration`) and choose expiration
4. Click **Add**
5. Copy the **Value** — this is your `TeamsClientSecret`

## Step 3: Configure API Permissions

1. Go to **API permissions**
2. Click **+ Add a permission**
3. Select **Microsoft Graph > Delegated permissions**
4. Add:
   - `Chat.ReadWrite` — Read and write chat messages
   - `ChatMessage.Send` — Send chat messages
   - `User.Read` — Read basic profile
   - `offline_access` — Refresh tokens
5. Click **Add permissions**

## Step 4: Add Additional Redirect URIs

1. Go to **Authentication**
2. Under **Web > Redirect URIs**, add:
   - `https://<your-function-app>.azurewebsites.net/api/comms/teams/auth/callback`
   - `https://localhost:7071/api/comms/teams/auth/callback` (for local development)
3. Click **Save**

## Step 5: Configure App Settings

Add the following to your Azure Function App Settings (or `local.settings.json` for local dev):

| Setting | Value |
|---------|-------|
| `TeamsClientId` | The Application (client) ID from Step 1 |
| `TeamsClientSecret` | The client secret Value from Step 2 |
| `TeamsTenantId` | `common` (allows any Microsoft tenant) |

## Permissions Reference

| Permission | Type | Purpose |
|-----------|------|---------|
| `Chat.ReadWrite` | Delegated | Access and manage Teams chats |
| `ChatMessage.Send` | Delegated | Send messages in chats |
| `User.Read` | Delegated | Read the authenticated user's profile |
| `offline_access` | Delegated | Get refresh tokens |

## Troubleshooting

- **"AADSTS50011: The reply URL does not match"** — The redirect URI must exactly match. Check for protocol, domain, and path
- **"Insufficient privileges"** — The user may need admin consent for `Chat.ReadWrite` in their organization. Have a tenant admin grant consent
- **"Resource not found"** — The chat ID may be incorrect. Use the Microsoft Graph Explorer to test chat IDs
- **Client secret expired** — Generate a new secret and update the app settings
