# LinkedIn — OAuth Setup for Posting

Sets up the OAuth 2.0 credentials for the LinkedIn posting tool (text and image posts to personal profiles).

## Prerequisites

- A LinkedIn account
- Access to the [LinkedIn Developer Portal](https://www.linkedin.com/developers/)

## Step 1: Create a LinkedIn App

1. Go to the [LinkedIn Developer Portal](https://www.linkedin.com/developers/)
2. Click **Create App**
3. Fill in:
   - **App name:** `Daisi Secure Tools`
   - **LinkedIn Page:** Select or create a LinkedIn Company Page to associate with the app
   - **Privacy policy URL:** your privacy policy URL (e.g., `https://daisi.ai/privacy`)
   - **App logo:** upload your logo
4. Check the terms agreement and click **Create app**

## Step 2: Request API Products

1. In your app, go to the **Products** tab
2. Request access to **Share on LinkedIn** (this grants `w_member_social`)
3. Request access to **Sign In with LinkedIn using OpenID Connect** (this grants `openid` and `profile`)
4. Wait for approval (Share on LinkedIn is usually auto-approved)

## Step 3: Configure OAuth Settings

1. Go to the **Auth** tab
2. Under **OAuth 2.0 settings**, find **Authorized redirect URLs for your app**
3. Add:
   - `https://<your-function-app>.azurewebsites.net/api/social/linkedin/auth/callback`
   - `https://localhost:7071/api/social/linkedin/auth/callback` (for local development)
4. Click **Update**

## Step 4: Get App Credentials

1. On the **Auth** tab, find the **Application credentials** section
2. Copy the **Client ID** — this is your `LinkedInClientId`
3. Copy the **Client Secret** (click the eye icon to reveal) — this is your `LinkedInClientSecret`

## Step 5: Configure App Settings

Add the following to your Azure Function App Settings (or `local.settings.json` for local dev):

| Setting | Value |
|---------|-------|
| `LinkedInClientId` | The Client ID from Step 4 |
| `LinkedInClientSecret` | The Client Secret from Step 4 |

## Scopes Reference

| Scope | Purpose | Granted By |
|-------|---------|-----------|
| `w_member_social` | Create posts on behalf of the authenticated member | Share on LinkedIn product |
| `openid` | OpenID Connect authentication | Sign In with LinkedIn product |
| `profile` | Read basic profile info (name, photo) | Sign In with LinkedIn product |

## Important Notes

- Posts are made to the **authenticated user's personal profile**, not a Company Page
- The LinkedIn API uses versioned REST headers — the tool handles this automatically
- Image posts require uploading the image to LinkedIn first via their upload API, then referencing it in the post

## Troubleshooting

- **"Unauthorized" or 401 errors** — Check that the Share on LinkedIn product is approved. Go to the Products tab and verify status
- **"Invalid redirect_uri"** — The redirect URL must exactly match what's configured in the Auth tab, including protocol (https)
- **"Not enough permissions to access: POST /ugcPosts"** — The `w_member_social` scope hasn't been granted. Request the Share on LinkedIn product
- **Token expires quickly** — LinkedIn access tokens expire in 60 days. Refresh tokens last 365 days. The OAuth flow handles refresh automatically
