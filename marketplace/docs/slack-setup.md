# Slack — OAuth Setup for Messaging

Sets up the OAuth 2.0 credentials for the Slack messaging tool (send messages to channels with optional threading).

## Prerequisites

- A Slack account
- A Slack workspace where you have permission to install apps
- Access to the [Slack API Portal](https://api.slack.com/apps)

## Step 1: Create a Slack App

1. Go to [Slack API Portal](https://api.slack.com/apps)
2. Click **Create New App**
3. Select **From scratch**
4. Fill in:
   - **App Name:** `Daisi Secure Tools`
   - **Pick a workspace to develop your app in:** Select your workspace
5. Click **Create App**

## Step 2: Configure OAuth Scopes

1. In the left sidebar, go to **OAuth & Permissions**
2. Scroll down to **Scopes**
3. Under **Bot Token Scopes**, add:
   - `chat:write` — Send messages as the bot
   - `chat:write.public` — Send messages to channels the bot hasn't joined
   - `files:write` — Upload files to channels
4. These scopes are for the bot token, which is what the tool uses

## Step 3: Configure Redirect URLs

1. On the **OAuth & Permissions** page, scroll to **Redirect URLs**
2. Click **Add New Redirect URL** and add:
   - `https://<your-function-app>.azurewebsites.net/api/comms/slack/auth/callback`
3. Click **Add** then **Save URLs**
4. Add another for local development:
   - `https://localhost:7071/api/comms/slack/auth/callback`
5. Click **Add** then **Save URLs**

## Step 4: Get App Credentials

1. In the left sidebar, go to **Basic Information**
2. Under **App Credentials**, find:
   - **Client ID** — this is your `SlackClientId`
   - **Client Secret** — click **Show** to reveal, this is your `SlackClientSecret`

## Step 5: Configure App Settings

Add the following to your Azure Function App Settings (or `local.settings.json` for local dev):

| Setting | Value |
|---------|-------|
| `SlackClientId` | The Client ID from Step 4 |
| `SlackClientSecret` | The Client Secret from Step 4 |

## Step 6: Distribute Your App (Optional)

By default, the app can only be installed in the development workspace. To allow installation in other workspaces:

1. Go to **Manage Distribution** in the left sidebar
2. Complete the checklist (add features, remove hard-coded info, etc.)
3. Click **Activate Public Distribution**

## Scopes Reference

| Scope | Purpose |
|-------|---------|
| `chat:write` | Post messages to channels where the bot is a member |
| `chat:write.public` | Post messages to any public channel without joining |
| `files:write` | Upload files (images, documents) to channels |

## How It Works

When a user authorizes the Slack tool:
1. They select which workspace to install the bot in
2. Slack issues a **bot token** for that workspace
3. The tool uses the bot token to send messages
4. Messages appear as coming from the "Daisi Secure Tools" bot

## Troubleshooting

- **"not_in_channel"** — The bot needs to be invited to private channels before posting. Use `chat:write.public` scope to post to public channels without joining
- **"channel_not_found"** — The channel ID or name is incorrect, or the bot doesn't have access to the channel
- **"invalid_auth"** — The bot token is invalid or revoked. Re-authenticate via the OAuth flow
- **"missing_scope"** — A required scope wasn't granted during installation. The user needs to re-install the app with the updated scopes
