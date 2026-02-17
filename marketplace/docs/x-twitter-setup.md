# X (Twitter) — OAuth Setup for Posting

Sets up the OAuth 2.0 credentials for the X posting tool (tweets with optional media, replies, and quote tweets).

## Prerequisites

- An X (Twitter) account
- Access to the [X Developer Portal](https://developer.x.com/en/portal/dashboard)
- A developer account (Basic tier or higher)

## Step 1: Create a Developer Account

1. Go to [X Developer Portal](https://developer.x.com/en/portal/dashboard)
2. If you don't have a developer account, click **Sign up** and follow the application process
3. Choose a plan:
   - **Free** — Limited to read-only, no posting
   - **Basic ($100/mo)** — Includes tweet posting and media upload
   - **Pro ($5,000/mo)** — Higher rate limits
4. The **Basic** tier is the minimum required for posting

## Step 2: Create a Project and App

1. In the Developer Portal, go to **Projects & Apps**
2. Click **+ Create Project**
3. Enter a project name (e.g., `Daisi AI`)
4. Select a use case (e.g., `Building tools for users`)
5. Enter a project description
6. Click **Create a new App** within the project
7. Enter an app name (e.g., `Daisi Secure Tools`)
8. Copy the **API Key** and **API Key Secret** (these are for OAuth 1.0a, not needed here, but save them)

## Step 3: Configure OAuth 2.0

1. In your app settings, go to **User authentication settings**
2. Click **Set up**
3. Configure:
   - **App permissions:** Select **Read and write**
   - **Type of App:** Select **Web App, Automated App or Bot**
   - **Callback URI / Redirect URL:** Enter `https://<your-function-app>.azurewebsites.net/api/social/x/auth/callback`
   - **Website URL:** Enter your website URL (e.g., `https://daisi.ai`)
4. Click **Save**
5. Copy the **Client ID** and **Client Secret** shown after saving

## Step 4: Add Additional Redirect URIs

1. Go back to **User authentication settings**
2. Add additional callback URIs:
   - `https://localhost:7071/api/social/x/auth/callback` (for local development)
3. Click **Save**

## Step 5: Configure App Settings

Add the following to your Azure Function App Settings (or `local.settings.json` for local dev):

| Setting | Value |
|---------|-------|
| `XClientId` | The OAuth 2.0 Client ID from Step 3 |
| `XClientSecret` | The OAuth 2.0 Client Secret from Step 3 |

## Scopes Reference

| Scope | Purpose |
|-------|---------|
| `tweet.read` | Read tweet data |
| `tweet.write` | Post and delete tweets |
| `users.read` | Read user profile information |
| `media.write` | Upload media (images, videos) |
| `offline.access` | Get refresh tokens for long-lived access |

## Rate Limits

| Tier | Tweet POST | Media Upload |
|------|-----------|-------------|
| Basic | 1,667/month | 1,667/month |
| Pro | 100,000/month | 100,000/month |

## Troubleshooting

- **"403 Forbidden" on tweet creation** — Verify your developer account is on the Basic tier or higher. The Free tier does not allow posting
- **"You aren't allowed to create a Tweet"** — Check that app permissions are set to "Read and write"
- **"Invalid redirect_uri"** — The callback URI must exactly match what's configured in the Developer Portal
- **Media upload fails** — Ensure the media file is within X's size limits (5MB for images, 512MB for video)
