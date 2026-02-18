# TikTok — OAuth Setup for Publishing

Sets up the OAuth 2.0 credentials for the TikTok publishing tool (video and photo posts).

## Prerequisites

- A TikTok account
- Access to the [TikTok Developer Portal](https://developers.tiktok.com/)

## Step 1: Create a TikTok Developer Account

1. Go to [TikTok Developer Portal](https://developers.tiktok.com/)
2. Click **Log in** and sign in with your TikTok account
3. If prompted, register as a developer and complete your profile

## Step 2: Create an App

1. From the developer dashboard, click **Manage apps** or **Create an app**
2. Fill in:
   - **App name:** `Daisi Secure Tools`
   - **Description:** describe the app's purpose
   - **App icon:** upload your logo
   - **Category:** select the appropriate category
   - **Platform:** select **Web**
3. Submit the app for creation

## Step 3: Add Products and Scopes

1. In your app settings, go to **Add products**
2. Add **Login Kit** — enables OAuth authentication
3. Add **Content Posting API** — enables video/photo publishing
4. For each product, configure the required scopes:
   - `video.upload` — Upload video content
   - `video.publish` — Publish uploaded content
   - `user.info.basic` — Read basic user profile

## Step 4: Configure OAuth Settings

1. In your app, go to **Login Kit** configuration
2. Under **Redirect URI**, add:
   - `https://<your-function-app>.azurewebsites.net/api/social/tiktok/auth/callback`
   - `https://localhost:7071/api/social/tiktok/auth/callback` (for local development)
3. Save the configuration

## Step 5: Get App Credentials

1. In your app settings, find the credentials section
2. Copy the **Client Key** — this is your `TikTokClientKey`
3. Copy the **Client Secret** — this is your `TikTokClientSecret`

Note: TikTok uses "Client Key" instead of "Client ID".

## Step 6: Submit for Review

1. TikTok requires app review before your scopes are active for all users
2. Go to **Submit for review** and provide:
   - A demo video showing how the app works
   - A description of why each scope is needed
   - Your privacy policy URL
3. While in **Sandbox mode**, you can test with the app owner's account

## Step 7: Configure App Settings

Add the following to your Azure Function App Settings (or `local.settings.json` for local dev):

| Setting | Value |
|---------|-------|
| `TikTokClientKey` | The Client Key from Step 5 |
| `TikTokClientSecret` | The Client Secret from Step 5 |

## Scopes Reference

| Scope | Purpose |
|-------|---------|
| `video.upload` | Upload video/photo content to TikTok |
| `video.publish` | Publish uploaded content to the user's profile |
| `user.info.basic` | Read basic user info (display name, avatar) |

## Important Notes

- **Unaudited apps** can only publish content with `PRIVATE` visibility. To publish public content, the app must pass TikTok's audit process
- **Video requirements:** MP4 or WebM, max 4GB, max 60 minutes
- **Photo posts:** Supported via the Content Posting API, 1-35 images per post
- The publishing flow is two-step: first upload media, then publish — the tool handles this automatically

## Troubleshooting

- **"Scope not authorized"** — The app hasn't been approved for the requested scopes. Check if the app is still in sandbox mode
- **"Video upload failed"** — Check video format and size requirements. TikTok is strict about encoding
- **Posts are private even though public was requested** — Unaudited apps can only post as private. Submit the app for audit to enable public posting
- **"spam_risk_too_many_pending_share"** — Too many unpublished uploads. Publish or discard pending uploads before creating new ones
