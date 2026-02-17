# WhatsApp — OAuth Setup for Business Messaging

Sets up the OAuth 2.0 credentials for the WhatsApp messaging tool (send text, template, and media messages via WhatsApp Business). WhatsApp uses Facebook Login (Meta's OAuth).

## Prerequisites

- A Facebook account
- A [Meta Business account](https://business.facebook.com/)
- A WhatsApp Business account linked to your Meta Business account
- Access to [Meta for Developers](https://developers.facebook.com/)

## Step 1: Set Up WhatsApp Business

If you haven't already:

1. Go to [Meta Business Suite](https://business.facebook.com/)
2. Navigate to **Settings > WhatsApp accounts**
3. Add a WhatsApp Business account (or create one)
4. Register a phone number for the WhatsApp Business account
5. Complete the business verification process (required for production use)

## Step 2: Create a Meta Developer App

If you already have a Meta app from the [Facebook setup](facebook-setup.md), you can reuse it. Otherwise:

1. Go to [Meta for Developers](https://developers.facebook.com/)
2. Click **My Apps > Create App**
3. Select **Other** > **Business**
4. Name it (e.g., `Daisi Secure Tools`) and click **Create App**

## Step 3: Add WhatsApp Product

1. In your app dashboard, click **Add Products**
2. Find **WhatsApp** and click **Set Up**
3. Follow the quick start to connect your WhatsApp Business account

## Step 4: Configure OAuth Settings

1. Ensure **Facebook Login** is added to your app
2. Go to **Facebook Login > Settings**
3. Under **Valid OAuth Redirect URIs**, add:
   - `https://<your-function-app>.azurewebsites.net/api/comms/whatsapp/auth/callback`
   - `https://localhost:7071/api/comms/whatsapp/auth/callback` (for local development)
4. Click **Save Changes**

## Step 5: Get App Credentials

1. Go to **App Settings > Basic**
2. Copy the **App ID** — this is your `WhatsAppClientId`
3. Click **Show** next to **App Secret** — this is your `WhatsAppClientSecret`

## Step 6: Request Permissions

1. Go to **App Review > Permissions and Features**
2. Request:
   - `whatsapp_business_management` — Manage WhatsApp Business settings
   - `whatsapp_business_messaging` — Send and receive WhatsApp messages
3. Submit for review with a description of use

In **Development Mode**, these work for test numbers without review.

## Step 7: Configure App Settings

Add the following to your Azure Function App Settings (or `local.settings.json` for local dev):

| Setting | Value |
|---------|-------|
| `WhatsAppClientId` | The App ID from Step 5 |
| `WhatsAppClientSecret` | The App Secret from Step 5 |

## Permissions Reference

| Permission | Purpose |
|-----------|---------|
| `whatsapp_business_management` | Manage WhatsApp Business account settings and phone numbers |
| `whatsapp_business_messaging` | Send and receive messages via the WhatsApp Cloud API |

## Important Notes

- **Message templates:** To send messages outside the 24-hour customer service window, you must use pre-approved message templates
- **Phone number verification:** The WhatsApp Business phone number must be verified before sending messages
- **Rate limits:** WhatsApp has tiered messaging limits based on your business verification status and quality rating
- **Test numbers:** In development mode, you can only send to numbers registered as test numbers in the WhatsApp dashboard

## Troubleshooting

- **"Phone number is not registered"** — The sending phone number must be registered and verified in your WhatsApp Business account
- **"Template not found"** — Template messages must be pre-created and approved in the WhatsApp Business Manager
- **"Recipient is not valid"** — The recipient must have WhatsApp installed and the number must include the country code
- **"Message failed to send"** — Check your messaging tier limits and quality rating in the WhatsApp Business Manager
