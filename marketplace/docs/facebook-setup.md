# Facebook — OAuth Setup for Page Posting

Sets up the OAuth 2.0 credentials for the Facebook posting tool (post text, photos, and links to Facebook Pages).

## Prerequisites

- A Facebook account
- A Facebook Page that you manage
- Access to [Meta for Developers](https://developers.facebook.com/)

## Step 1: Create a Meta Developer Account

1. Go to [Meta for Developers](https://developers.facebook.com/)
2. Click **Get Started** or **My Apps** in the top right
3. If prompted, register as a developer and accept the terms

## Step 2: Create an App

1. From the [App Dashboard](https://developers.facebook.com/apps/), click **Create App**
2. Select **Other** as the use case, then click **Next**
3. Select **Business** as the app type, then click **Next**
4. Fill in:
   - **App name:** `Daisi Secure Tools`
   - **App contact email:** your email
   - **Business Account:** select your business account (or create one)
5. Click **Create App**

## Step 3: Add Facebook Login Product

1. In your app dashboard, find **Add Products to Your App**
2. Find **Facebook Login** and click **Set Up**
3. Select **Web**
4. Enter your site URL (e.g., `https://daisi.ai`) and click **Save**
5. Skip the remaining quickstart steps

## Step 4: Configure OAuth Settings

1. Go to **Facebook Login > Settings** in the left sidebar
2. Under **Valid OAuth Redirect URIs**, add:
   - `https://<your-function-app>.azurewebsites.net/api/social/facebook/auth/callback`
   - `https://localhost:7071/api/social/facebook/auth/callback` (for local development)
3. Click **Save Changes**

## Step 5: Get App Credentials

1. Go to **App Settings > Basic** in the left sidebar
2. Copy the **App ID** — this is your `FacebookClientId`
3. Click **Show** next to **App Secret** and copy it — this is your `FacebookClientSecret`

## Step 6: Request Permissions

1. Go to **App Review > Permissions and Features**
2. Request the following permissions:
   - `pages_manage_posts` — Required to create posts on Pages
   - `pages_read_engagement` — Required to read Page data
   - `pages_show_list` — Required to list Pages the user manages
3. For each permission, click **Request** and provide a description of how it will be used
4. Submit for review

While in **Development Mode**, these permissions work for app admins and testers without review. For production use with any Facebook user, the app must pass App Review.

## Step 7: Configure App Settings

Add the following to your Azure Function App Settings (or `local.settings.json` for local dev):

| Setting | Value |
|---------|-------|
| `FacebookClientId` | The App ID from Step 5 |
| `FacebookClientSecret` | The App Secret from Step 5 |

## Permissions Reference

| Permission | Purpose |
|-----------|---------|
| `pages_manage_posts` | Create, edit, and delete posts on Pages managed by the user |
| `pages_read_engagement` | Read engagement data (likes, comments) on Pages |
| `pages_show_list` | List all Pages the user manages (used to select which Page to post to) |

## Important Notes

- This tool posts to **Facebook Pages only**, not personal profiles. The Graph API does not support posting to personal timelines.
- Users must be an **admin or editor** of the Page they want to post to.
- Posts are made using the Page's access token, so they appear as coming from the Page itself.

## Troubleshooting

- **"Error validating access token"** — The Page access token may have expired. Re-authenticate via the OAuth flow
- **"(#200) Requires extended permission: pages_manage_posts"** — The app hasn't been granted this permission. Check App Review status
- **"This app is in development mode"** — Only app admins and testers can use it. Add test users in **Roles**, or submit for App Review
- **No Pages shown** — The user must be an admin or editor of at least one Facebook Page
