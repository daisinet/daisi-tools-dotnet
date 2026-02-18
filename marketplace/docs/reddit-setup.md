# Reddit — OAuth Setup for Posting

Sets up the OAuth 2.0 credentials for the Reddit submission tool (text and link posts to subreddits).

## Prerequisites

- A Reddit account
- Access to [Reddit App Preferences](https://www.reddit.com/prefs/apps)

## Step 1: Create a Reddit App

1. Go to [Reddit App Preferences](https://www.reddit.com/prefs/apps)
2. Scroll to the bottom and click **create another app...**
3. Fill in:
   - **name:** `Daisi Secure Tools`
   - **App type:** Select **web app**
   - **description:** (optional) `AI-powered Reddit posting via Daisinet`
   - **about url:** your website (e.g., `https://daisi.ai`)
   - **redirect uri:** `https://<your-function-app>.azurewebsites.net/api/social/reddit/auth/callback`
4. Click **create app**

## Step 2: Get App Credentials

After creating the app:

1. The **Client ID** is the string shown directly under the app name (under "web app")
2. The **Client Secret** is labeled as `secret`
3. Copy both values

## Step 3: Add Additional Redirect URIs

Reddit only allows one redirect URI per app. If you need multiple (e.g., dev and prod):

1. Create separate apps for each environment, OR
2. Use the production URI and configure `OAuthRedirectUri` app setting to override in dev

For local development, create a separate app with redirect URI:
- `https://localhost:7071/api/social/reddit/auth/callback`

## Step 4: Configure App Settings

Add the following to your Azure Function App Settings (or `local.settings.json` for local dev):

| Setting | Value |
|---------|-------|
| `RedditClientId` | The Client ID from Step 2 |
| `RedditClientSecret` | The Client Secret from Step 2 |

## Scopes Reference

| Scope | Purpose |
|-------|---------|
| `submit` | Submit text posts and link posts to subreddits |
| `identity` | Read the authenticated user's identity (username) |

## OAuth Notes

- The OAuth flow requests `duration=permanent` to get a refresh token (without this, the token expires in 1 hour with no way to refresh)
- Reddit uses Basic authentication (Client ID as username, Client Secret as password) for the token exchange endpoint

## Rate Limits

- Reddit's API rate limit is **100 requests per minute** per OAuth token
- New accounts and accounts with low karma may have additional posting restrictions enforced by Reddit
- Subreddits may have their own posting frequency rules

## Troubleshooting

- **"Forbidden" when submitting** — The authenticated user may not have permission to post in the target subreddit (karma requirements, account age, etc.)
- **"Invalid redirect_uri"** — Reddit requires an exact match. Check for trailing slashes
- **"You are doing that too much"** — Reddit rate-limits posting for newer accounts. Wait and try again
- **Only one redirect URI** — Reddit doesn't support multiple redirect URIs per app. Create separate apps for dev/prod environments
