# X Direct Messages — OAuth Setup

Sets up the OAuth 2.0 credentials for the X (Twitter) Direct Messages tool. This uses a **separate** app registration from the X posting integration.

## Prerequisites

- An X (Twitter) account
- Access to the [X Developer Portal](https://developer.x.com/en/portal/dashboard)
- A developer account on the **Basic** tier or higher

## Why a Separate App?

The X DM tool requires different OAuth scopes (`dm.read`, `dm.write`) than the posting tool (`tweet.write`). Keeping them separate allows users to grant only the permissions they need. A user might want to send DMs without granting tweet posting access.

You can use the same developer project but create a separate app within it.

## Step 1: Create an App (or Reuse Existing Project)

If you already have a project from the [X posting setup](x-twitter-setup.md):

1. In the Developer Portal, go to your existing project
2. Click **+ Add App** to create a new app within the same project
3. Name it (e.g., `Daisi Secure Tools - DMs`)

If starting fresh, follow Steps 1-2 from the [X posting setup guide](x-twitter-setup.md).

## Step 2: Configure OAuth 2.0

1. In the new app's settings, go to **User authentication settings**
2. Click **Set up**
3. Configure:
   - **App permissions:** Select **Read, Write, and Direct Messages**
   - **Type of App:** Select **Web App, Automated App or Bot**
   - **Callback URI / Redirect URL:** `https://<your-function-app>.azurewebsites.net/api/comms/xdm/auth/callback`
   - **Website URL:** your website URL
4. Click **Save**
5. Copy the **Client ID** and **Client Secret**

## Step 3: Add Local Development Redirect URI

1. Go back to **User authentication settings**
2. Add:
   - `https://localhost:7071/api/comms/xdm/auth/callback`
3. Click **Save**

## Step 4: Configure App Settings

Add the following to your Azure Function App Settings (or `local.settings.json` for local dev):

| Setting | Value |
|---------|-------|
| `XDmClientId` | The OAuth 2.0 Client ID from Step 2 |
| `XDmClientSecret` | The OAuth 2.0 Client Secret from Step 2 |

## Scopes Reference

| Scope | Purpose |
|-------|---------|
| `dm.read` | Read direct messages |
| `dm.write` | Send direct messages |
| `users.read` | Read user profile info (needed to resolve recipient) |
| `offline.access` | Get refresh tokens |

## Important Notes

- **DM access requires Basic tier or higher** on the X Developer Portal
- App permissions must be set to **Read, Write, and Direct Messages** — "Read and write" alone is not sufficient for DMs
- The recipient must allow DMs from the sender (privacy settings)

## Troubleshooting

- **"403 Forbidden" on DM send** — Check that the app's permissions include "Direct Messages" and your developer plan is Basic or higher
- **"You cannot send messages to this user"** — The recipient has DMs restricted. They must follow the sender or have open DMs enabled
- **"Invalid redirect_uri"** — Must exactly match the URI configured in the Developer Portal
- **Different Client ID than X posting** — This is expected. DMs and posting use separate apps with different OAuth scopes
