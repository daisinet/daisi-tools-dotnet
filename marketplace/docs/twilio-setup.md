# Twilio — User Setup Guide (SMS, Voice, Email)

Twilio tools use **user-provided credentials** — there are no Daisi-level app settings to configure. Each user provides their own Twilio Account SID, Auth Token, and optionally a SendGrid API key during tool configuration in the Marketplace.

## For Users

### Step 1: Create a Twilio Account

1. Go to [Twilio](https://www.twilio.com/try-twilio) and sign up for an account
2. Complete email and phone verification
3. You'll receive a trial account with free credits

### Step 2: Get Your Account Credentials

1. Go to the [Twilio Console](https://console.twilio.com/)
2. On the dashboard, find:
   - **Account SID** — starts with `AC`
   - **Auth Token** — click the eye icon to reveal
3. You'll enter these when configuring the tool in the Daisinet Marketplace

### Step 3: Get a Phone Number (for SMS and Voice)

1. In the Twilio Console, go to **Phone Numbers > Manage > Buy a number**
2. Search for a number with the capabilities you need:
   - **SMS** — for sending text messages
   - **Voice** — for making phone calls
3. Click **Buy** and confirm
4. Note the phone number — you can enter it as the `fromPhone` during tool configuration

### Step 4: Set Up SendGrid (for Email)

If you want to use the Twilio Email tool:

1. Go to [SendGrid](https://sendgrid.com/) (owned by Twilio) and sign up
2. Or from the Twilio Console, go to **Email > SendGrid**
3. In the SendGrid dashboard, go to **Settings > API Keys**
4. Click **Create API Key**
5. Enter a name (e.g., `Daisi AI`)
6. Select **Restricted Access** and enable:
   - **Mail Send > Full Access**
7. Click **Create & View**
8. Copy the API key — you'll enter this when configuring the email tool

### Step 5: Configure in Daisinet Marketplace

When you install a Twilio tool from the Daisinet Marketplace and click **Configure**, enter:

| Parameter | Value | Required For |
|-----------|-------|-------------|
| `accountSid` | Your Twilio Account SID (`AC...`) | SMS, Voice |
| `authToken` | Your Twilio Auth Token | SMS, Voice |
| `fromPhone` | Your Twilio phone number (e.g., `+15551234567`) | SMS, Voice (optional default) |
| `sendGridApiKey` | Your SendGrid API key (`SG...`) | Email |

### Trial Account Limitations

- Can only send SMS/calls to verified phone numbers
- Messages include a "Sent from your Twilio trial account" prefix
- Limited free credits
- Upgrade to a paid account to remove these restrictions

## For Platform Administrators

No Daisi-level app settings are required for Twilio. All credentials are user-provided via the Configure flow. The `local.settings.json` and Azure Function App Settings do not need any Twilio entries.

## Troubleshooting

- **"Authenticate" error (HTTP 401)** — The Account SID or Auth Token is incorrect. Verify in the Twilio Console
- **"The 'From' phone number is not a valid, SMS-capable Twilio phone number"** — The `fromPhone` must be a Twilio number you own. Check Phone Numbers in the Twilio Console
- **"Permission to send an SMS has not been enabled for the region"** — Your Twilio account may need geographic permissions enabled. Go to Console > Messaging > Settings > Geo permissions
- **SendGrid "Forbidden" (403)** — The API key doesn't have Mail Send permission. Create a new key with the correct access
- **Trial account: "The number is unverified"** — On trial accounts, you can only send to numbers you've verified. Go to Console > Phone Numbers > Verified Caller IDs to add numbers
