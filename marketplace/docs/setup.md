# Secure Tools — Provider Setup Guide

This guide covers how to configure the OAuth apps and API keys needed to run the Daisi Secure Tools Azure Function App. Each provider requires its own developer account and credentials.

## Overview

The Secure Tools Function App requires two types of credentials:

1. **OAuth Client Credentials** — Daisi's app registrations that let users authorize access to their accounts via OAuth 2.0. These are set as Azure Function App Settings.
2. **User API Keys** — Credentials that individual users provide during tool configuration (e.g., Twilio Account SID, Firecrawl API key). These are NOT app settings — they're entered by users in the Marketplace Configure page.

## App Settings Reference

After creating each OAuth app below, add the credentials to the Azure Function App Settings (or `local.settings.json` for local dev):

| Setting | Provider | Guide |
|---------|----------|-------|
| `DaisiAuthKey` | System | Shared secret matching the ORC's `SECURE_AUTH_KEY` |
| `GoogleClientId` / `GoogleClientSecret` | Google | [Google Setup](google-setup.md) |
| `MicrosoftClientId` / `MicrosoftClientSecret` / `MicrosoftTenantId` | Microsoft 365 | [Microsoft 365 Setup](microsoft365-setup.md) |
| `XClientId` / `XClientSecret` | X (Twitter) | [X Setup](x-twitter-setup.md) |
| `FacebookClientId` / `FacebookClientSecret` | Facebook | [Facebook Setup](facebook-setup.md) |
| `InstagramClientId` / `InstagramClientSecret` | Instagram | [Instagram Setup](instagram-setup.md) |
| `LinkedInClientId` / `LinkedInClientSecret` | LinkedIn | [LinkedIn Setup](linkedin-setup.md) |
| `RedditClientId` / `RedditClientSecret` | Reddit | [Reddit Setup](reddit-setup.md) |
| `TikTokClientKey` / `TikTokClientSecret` | TikTok | [TikTok Setup](tiktok-setup.md) |
| `SlackClientId` / `SlackClientSecret` | Slack | [Slack Setup](slack-setup.md) |
| `TeamsClientId` / `TeamsClientSecret` / `TeamsTenantId` | Teams | [Teams Setup](teams-setup.md) |
| `WhatsAppClientId` / `WhatsAppClientSecret` | WhatsApp | [WhatsApp Setup](whatsapp-setup.md) |
| `XDmClientId` / `XDmClientSecret` | X Direct Messages | [X DM Setup](xdm-setup.md) |

## User-Configured Providers (No App Settings Needed)

These providers don't require Daisi-level OAuth apps. Users provide their own credentials during tool configuration:

| Provider | Guide |
|----------|-------|
| Twilio (SMS, Voice, Email) | [Twilio Setup](twilio-setup.md) |
| Telegram | [Telegram Setup](telegram-setup.md) |
| Firecrawl | [Firecrawl Setup](firecrawl-setup.md) |

## Redirect URI Pattern

All OAuth providers need a redirect URI configured. The pattern is:

```
https://<function-app-url>/api/<provider-route>/auth/callback
```

Examples:
- **Dev:** `https://daisi-secure-tools-dev.azurewebsites.net/api/google/auth/callback`
- **Prod:** `https://daisi-secure-tools.azurewebsites.net/api/google/auth/callback`
- **Local:** `https://localhost:7071/api/google/auth/callback`

Each provider guide specifies the exact route to use.

## Deployment

After configuring all app settings:

```bash
cd Daisi.SecureTools
func azure functionapp publish <app-name> --build remote
```

Or set the app settings via CLI:

```bash
az functionapp config appsettings set \
  --name <app-name> \
  --resource-group <resource-group> \
  --settings \
    DaisiAuthKey="<your-auth-key>" \
    GoogleClientId="<client-id>" \
    GoogleClientSecret="<client-secret>" \
    ...
```
