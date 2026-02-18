# Instagram — OAuth Setup for Publishing

Sets up the OAuth 2.0 credentials for the Instagram publishing tool (images, videos, and carousels). Instagram uses Facebook Login (Meta's OAuth) with Instagram-specific permissions.

## Prerequisites

- A Facebook account
- An **Instagram Business** or **Instagram Creator** account (personal accounts cannot use the API)
- A Facebook Page connected to your Instagram account
- Access to [Meta for Developers](https://developers.facebook.com/)

## Step 1: Set Up Instagram Business Account

If your Instagram account isn't already a Business or Creator account:

1. Open the Instagram app
2. Go to **Settings > Account > Switch to professional account**
3. Choose **Business** or **Creator**
4. Connect to a Facebook Page (create one if needed)

## Step 2: Create a Meta Developer App

If you already have a Meta app from the [Facebook setup](facebook-setup.md), you can reuse it. Otherwise:

1. Go to [Meta for Developers](https://developers.facebook.com/)
2. Click **My Apps > Create App**
3. Select **Other** > **Business**
4. Name it (e.g., `Daisi Secure Tools`) and click **Create App**

## Step 3: Add Instagram Products

1. In your app dashboard, find **Add Products to Your App**
2. Add **Instagram** (or **Instagram Basic Display** and **Instagram Graph API**)
3. Also ensure **Facebook Login** is added (Instagram uses Facebook Login for OAuth)

## Step 4: Configure OAuth Settings

1. Go to **Facebook Login > Settings**
2. Under **Valid OAuth Redirect URIs**, add:
   - `https://<your-function-app>.azurewebsites.net/api/social/instagram/auth/callback`
   - `https://localhost:7071/api/social/instagram/auth/callback` (for local development)
3. Click **Save Changes**

## Step 5: Get App Credentials

1. Go to **App Settings > Basic**
2. Copy the **App ID** — this is your `InstagramClientId`
3. Click **Show** next to **App Secret** — this is your `InstagramClientSecret`

## Step 6: Request Permissions

1. Go to **App Review > Permissions and Features**
2. Request the following:
   - `instagram_basic` — Access basic Instagram account info
   - `instagram_content_publish` — Publish content to Instagram
   - `pages_read_engagement` — Read Page data (required for Instagram API)
   - `pages_show_list` — List Pages linked to Instagram accounts
3. Submit each for review with a description of use

In **Development Mode**, these work for app admins/testers without review.

## Step 7: Configure App Settings

Add the following to your Azure Function App Settings (or `local.settings.json` for local dev):

| Setting | Value |
|---------|-------|
| `InstagramClientId` | The App ID from Step 5 |
| `InstagramClientSecret` | The App Secret from Step 5 |

## Permissions Reference

| Permission | Purpose |
|-----------|---------|
| `instagram_basic` | Read Instagram account profile and media |
| `instagram_content_publish` | Publish photos, videos, and carousels |
| `pages_read_engagement` | Read Facebook Page data linked to Instagram |
| `pages_show_list` | List Facebook Pages to find linked Instagram accounts |

## Publishing Requirements

The Instagram Content Publishing API has specific requirements:

- **Images** must be at a publicly accessible URL (the API fetches them server-side)
- **Videos** must be at a publicly accessible URL and in a supported format (MP4, MOV)
- **Carousels** can contain 2-10 images or videos
- Content is published as the Instagram Business/Creator account linked to the Facebook Page

## Troubleshooting

- **"The Instagram account is not a Business or Creator account"** — Switch the Instagram account to Professional in the Instagram app settings
- **"No Instagram account connected"** — The Facebook Page must be connected to an Instagram Business/Creator account. Go to the Facebook Page Settings > Instagram to connect
- **"Media URL is not accessible"** — Images/videos must be at public URLs. The Instagram API downloads media server-side and cannot access private or localhost URLs
- **"Application does not have permission"** — Check that `instagram_content_publish` is approved or that the user is a tester of the app
