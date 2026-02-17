# Daisi Tools
This solution contains the Tools for the inference engine that are provided by Daisi. It shows examples of how you can build tools that the inference engine can use to extend the base functionality.

## Secure Tool Integrations

`Daisi.SecureTools` is a single Azure Function App that hosts all secure tool provider integrations. Each provider gets its own route prefix (`/api/google/...`, `/api/m365/...`, `/api/firecrawl/...`) so a single Consumption Plan and storage account serves all providers.

### Google Workspace

Connects Daisi AI agents to Google Workspace via OAuth 2.0. One authorization grants access to Gmail, Drive, Calendar, and Sheets.

| Tool ID | Route Prefix | Description |
|---------|-------------|-------------|
| `daisi-google-gmail-search` | `/api/google/` | Search emails using Gmail query syntax |
| `daisi-google-gmail-unread` | | Get unread emails from inbox |
| `daisi-google-gmail-read` | | Read full email content by ID |
| `daisi-google-gmail-send` | | Send an email |
| `daisi-google-drive-search` | | Search Google Drive files |
| `daisi-google-drive-read` | | Read/download file content |
| `daisi-google-calendar-list` | | List calendar events |
| `daisi-google-calendar-create` | | Create a calendar event |
| `daisi-google-sheets-read` | | Read spreadsheet data |
| `daisi-google-sheets-write` | | Write data to a spreadsheet |

**Setup:** OAuth 2.0 — user authorizes via Google popup. Requires `GoogleClientId` and `GoogleClientSecret` in app settings.

### Microsoft 365

Connects Daisi AI agents to Microsoft 365 via Microsoft Graph API with OAuth 2.0. Supports Outlook, OneDrive, Calendar, and Teams.

| Tool ID | Route Prefix | Description |
|---------|-------------|-------------|
| `daisi-m365-mail-search` | `/api/m365/` | Search Outlook emails |
| `daisi-m365-mail-unread` | | Get unread emails |
| `daisi-m365-mail-read` | | Read full email content |
| `daisi-m365-mail-send` | | Send email via Outlook |
| `daisi-m365-onedrive-search` | | Search OneDrive files |
| `daisi-m365-onedrive-read` | | Read/download OneDrive file |
| `daisi-m365-calendar-list` | | List Outlook calendar events |
| `daisi-m365-calendar-create` | | Create a calendar event |
| `daisi-m365-teams-send` | | Post a Teams channel message |

**Setup:** OAuth 2.0 — user authorizes via Microsoft popup. Requires `MicrosoftClientId`, `MicrosoftClientSecret`, and `MicrosoftTenantId` in app settings.

### Firecrawl — Web Scraping & Search

Connects Daisi AI agents to the Firecrawl API for web scraping, crawling, and AI-powered data extraction. Uses API key authentication (no OAuth).

| Tool ID | Route Prefix | Description |
|---------|-------------|-------------|
| `daisi-firecrawl-scrape` | `/api/firecrawl/` | Scrape a single page, return markdown |
| `daisi-firecrawl-crawl` | | Crawl multiple pages from a domain |
| `daisi-firecrawl-search` | | Search the web and extract content |
| `daisi-firecrawl-extract` | | AI-powered structured data extraction |
| `daisi-firecrawl-map` | | Discover all URLs on a site |

**Setup:** API key — user provides their Firecrawl API key during configuration.

### Social Media

Connects Daisi AI agents to social media platforms for posting content. Each platform uses OAuth 2.0 and has its own route prefix under `/api/social/`. All API calls use raw `HttpClient` — no platform-specific SDK dependencies.

#### X (Twitter)

| Tool ID | Route Prefix | Description |
|---------|-------------|-------------|
| `daisi-social-x-post` | `/api/social/x/` | Post a tweet with optional media, reply, or quote tweet |

**Setup:** OAuth 2.0 — requires `XClientId` and `XClientSecret`. Scopes: `tweet.read tweet.write users.read media.write offline.access`.

#### Facebook

| Tool ID | Route Prefix | Description |
|---------|-------------|-------------|
| `daisi-social-facebook-post` | `/api/social/facebook/` | Post to a Facebook Page (text, photo, or link) |

**Setup:** OAuth 2.0 — requires `FacebookClientId` and `FacebookClientSecret`. Scopes: `pages_manage_posts pages_read_engagement pages_show_list`. Posts to Pages only (not personal profiles).

#### Reddit

| Tool ID | Route Prefix | Description |
|---------|-------------|-------------|
| `daisi-social-reddit-submit` | `/api/social/reddit/` | Submit a text or link post to a subreddit |

**Setup:** OAuth 2.0 — requires `RedditClientId` and `RedditClientSecret`. Scopes: `submit identity`. Uses `duration=permanent` for refresh tokens.

#### LinkedIn

| Tool ID | Route Prefix | Description |
|---------|-------------|-------------|
| `daisi-social-linkedin-post` | `/api/social/linkedin/` | Post text or image content to LinkedIn |

**Setup:** OAuth 2.0 — requires `LinkedInClientId` and `LinkedInClientSecret`. Scopes: `w_member_social openid profile`. Uses versioned LinkedIn REST API headers.

#### Instagram

| Tool ID | Route Prefix | Description |
|---------|-------------|-------------|
| `daisi-social-instagram-publish` | `/api/social/instagram/` | Publish image, video, or carousel to Instagram |

**Setup:** OAuth 2.0 via Facebook Login — requires `InstagramClientId` and `InstagramClientSecret`. Scopes: `instagram_basic instagram_content_publish pages_read_engagement pages_show_list`. Requires a Business or Creator Instagram account. Media must be at public URLs.

#### TikTok

| Tool ID | Route Prefix | Description |
|---------|-------------|-------------|
| `daisi-social-tiktok-publish` | `/api/social/tiktok/` | Publish video or photo post to TikTok |

**Setup:** OAuth 2.0 — requires `TikTokClientKey` and `TikTokClientSecret`. Scopes: `video.upload video.publish user.info.basic`. Unaudited apps can only post as PRIVATE visibility.

### Communications

Connects Daisi AI agents to messaging and communications platforms for sending SMS, making voice calls, sending emails, and messaging via WhatsApp, Telegram, X DMs, Teams, and Slack. Reuses the shared `SocialHttpClient` and `MediaHelper` from Social providers.

#### Twilio (SMS, Voice, Email)

| Tool ID | Route Prefix | Description |
|---------|-------------|-------------|
| `daisi-comms-twilio-sms` | `/api/comms/twilio/` | Send an SMS message |
| `daisi-comms-twilio-voice` | | Initiate a voice call with TwiML |
| `daisi-comms-twilio-email` | | Send an email via SendGrid |

**Setup:** API key — user provides Twilio Account SID, Auth Token, and optionally a SendGrid API key and default `fromPhone` during configuration. No OAuth required.

#### WhatsApp

| Tool ID | Route Prefix | Description |
|---------|-------------|-------------|
| `daisi-comms-whatsapp-send` | `/api/comms/whatsapp/` | Send a text, template, or media message via WhatsApp Business |

**Setup:** OAuth 2.0 via Facebook Login (Meta Cloud API) — requires `WhatsAppClientId` and `WhatsAppClientSecret`. Scopes: `whatsapp_business_management whatsapp_business_messaging`.

#### Telegram

| Tool ID | Route Prefix | Description |
|---------|-------------|-------------|
| `daisi-comms-telegram-send` | `/api/comms/telegram/` | Send a message, photo, document, or video via Telegram Bot API |

**Setup:** API key — user provides their Telegram Bot Token (from BotFather) during configuration. No OAuth required.

#### X Direct Messages

| Tool ID | Route Prefix | Description |
|---------|-------------|-------------|
| `daisi-comms-xdm-send` | `/api/comms/xdm/` | Send a direct message on X (Twitter) |

**Setup:** OAuth 2.0 — requires `XDmClientId` and `XDmClientSecret` (independent from social X posting config). Scopes: `dm.read dm.write users.read offline.access`.

#### Microsoft Teams

| Tool ID | Route Prefix | Description |
|---------|-------------|-------------|
| `daisi-comms-teams-send` | `/api/comms/teams/` | Send a message to a Teams chat |

**Setup:** OAuth 2.0 — requires `TeamsClientId`, `TeamsClientSecret`, and `TeamsTenantId` (independent from M365 config). Scopes: `Chat.ReadWrite ChatMessage.Send User.Read offline_access`.

#### Slack

| Tool ID | Route Prefix | Description |
|---------|-------------|-------------|
| `daisi-comms-slack-send` | `/api/comms/slack/` | Send a message to a Slack channel with optional threading |

**Setup:** OAuth 2.0 — requires `SlackClientId` and `SlackClientSecret`. Scopes: `chat:write chat:write.public files:write`.

## Shared Library: SecureToolProvider.Common

Production-grade shared library used by all secure tool integrations. Provides:

- **`ISetupStore` / `InMemorySetupStore` / `PersistentSetupStore`** — Installation state and credential storage. In-memory for local dev, Azure Table Storage for production.
- **`OAuthHelper`** — OAuth 2.0 authorization code flow with PKCE. Generic across providers — parameterized by authorize URL, token URL, client ID, client secret, and scopes.
- **`SecureToolFunctionBase`** — Base class for Azure Functions with standard endpoints (install, uninstall, configure, execute, auth/start, auth/callback, auth/status). Subclasses only implement `ExecuteToolAsync()`.
- **`AuthValidator`** — Shared `X-Daisi-Auth` and `installId` validation.
- **`Models/`** — Request/response DTOs, OAuth config, and token models.

## SecureToolProvider (Reference Implementation)

The `SecureToolProvider/` directory contains a reference Azure Functions implementation of the Daisinet Secure Tool Provider API. This demonstrates how marketplace providers can build tools that execute on their own servers while keeping credentials private.

**What it shows:**
- `SecureToolFunctions.cs` — Four HTTP endpoints implementing the provider contract:
  - `/install` — ORC calls on purchase with `X-Daisi-Auth` header, `installId`, and optional `bundleInstallId`
  - `/uninstall` — ORC calls on deactivation with `X-Daisi-Auth` header and `installId`
  - `/configure` — Manager UI calls directly with `installId` (no auth header)
  - `/execute` — Consumer hosts call directly with `installId` (no auth header)
- `SetupStore.cs` — In-memory installation registry, setup data storage, and bundle-aware OAuth token keying (replace with Azure Key Vault or encrypted DB in production)
- `Models.cs` — Request/response models matching the Daisinet provider API contract

**OAuth reference endpoints:**
- `/auth/start` (GET) — OAuth initiation. Receives `installId`, `returnUrl`, `service` as query params. In production, redirects to external consent screen; in the reference impl, simulates by redirecting to own callback.
- `/auth/callback` (GET) — OAuth callback. Decodes state, exchanges code for tokens (simulated), stores them via `SetupStore.SaveOAuthTokens()`, and redirects popup back to Daisinet's `/marketplace/oauth-callback`.
- `/auth/status` (POST) — Connection status check. Receives `{ installId, service }`, returns `{ connected, serviceName, userLabel }`. Called by the Manager UI to display OAuth connection badges.

**Bundle OAuth support:**
When tools are bundled in a Plugin, the ORC sends a shared `bundleInstallId` during `/install`. The `SetupStore` maps installs to their bundle and resolves OAuth token keys to the bundle level, so authenticating from any tool in the bundle makes all sibling tools show "Connected". Non-OAuth setup (API keys, etc.) remains per-tool via each tool's own `InstallId`.

**Authentication model:**
- `/install` and `/uninstall` are ORC-originated — verified via `X-Daisi-Auth` shared secret
- `/configure` and `/execute` are consumer-originated — verified by checking that the `installId` was registered via `/install`. The `installId` is an opaque, unguessable identifier that serves as a bearer token.

**To use as a starting point:**
1. Clone the `SecureToolProvider` directory
2. Replace the echo logic in `Execute` with your actual tool implementation
3. Replace `SetupStore` with a secure storage backend (Azure Key Vault recommended)
4. Set the `ExpectedAuthKey` constant to match what you configure in the marketplace item
5. Deploy to Azure Functions (or any HTTP-capable host)
6. Create a marketplace item with Secure Execution enabled, pointing to your deployed URL

See the [Creating Secure Tools](https://daisi.ai/learn/marketplace/creating-secure-tools) guide and the [API Reference](https://daisi.ai/learn/marketplace/secure-tool-api-reference) for the full contract specification.

## Project Structure

```
daisi-tools-dotnet/
├── Daisi.Tools.csproj                       # Local tools library
├── Daisi.Tools.Tests/                       # Local tool tests
├── SecureToolProvider/                      # Reference implementation (echo tool)
├── SecureToolProvider.Common/               # Shared library for secure tools
├── SecureToolProvider.Common.Tests/         # Shared library tests
├── Daisi.SecureTools/                       # Consolidated secure tools Function App
│   ├── Google/                              #   Google Workspace provider
│   │   ├── GoogleFunctions.cs               #     Routes: /api/google/*
│   │   ├── GoogleServiceFactory.cs
│   │   └── Tools/                           #     10 tool implementations
│   ├── Microsoft365/                        #   Microsoft 365 provider
│   │   ├── Microsoft365Functions.cs         #     Routes: /api/m365/*
│   │   ├── GraphClientFactory.cs
│   │   └── Tools/                           #     9 tool implementations
│   ├── Firecrawl/                           #   Firecrawl provider
│   │   ├── FirecrawlFunctions.cs            #     Routes: /api/firecrawl/*
│   │   ├── FirecrawlClient.cs
│   │   └── Tools/                           #     5 tool implementations
│   ├── Social/                              #   Social media providers
│   │   ├── ISocialToolExecutor.cs           #     Common tool interface
│   │   ├── SocialHttpClient.cs              #     Shared HTTP client wrapper
│   │   ├── MediaHelper.cs                   #     Media download/conversion
│   │   ├── X/                               #     Routes: /api/social/x/*
│   │   ├── Facebook/                        #     Routes: /api/social/facebook/*
│   │   ├── Reddit/                          #     Routes: /api/social/reddit/*
│   │   ├── LinkedIn/                        #     Routes: /api/social/linkedin/*
│   │   ├── Instagram/                       #     Routes: /api/social/instagram/*
│   │   └── TikTok/                          #     Routes: /api/social/tiktok/*
│   └── Comms/                               #   Communications providers
│       ├── ICommsToolExecutor.cs             #     Common tool interface
│       ├── Twilio/                           #     Routes: /api/comms/twilio/*
│       ├── WhatsApp/                         #     Routes: /api/comms/whatsapp/*
│       ├── Telegram/                         #     Routes: /api/comms/telegram/*
│       ├── XDm/                              #     Routes: /api/comms/xdm/*
│       ├── Teams/                            #     Routes: /api/comms/teams/*
│       └── Slack/                            #     Routes: /api/comms/slack/*
├── Daisi.SecureTools.Tests/                 # Consolidated secure tools tests
├── marketplace/                             # Marketplace pipeline
│   ├── catalog.json                         #   Declarative tool/plugin catalog
│   └── sync-marketplace.py                  #   Cosmos DB sync script
├── .github/workflows/
│   └── sync-marketplace.yml                 #   CI/CD workflow for marketplace sync
└── Daisi.Tools.sln                          # Solution file
```

## Marketplace Pipeline

The `marketplace/` directory contains an automated CI/CD pipeline that syncs first-party tools and plugins to the Cosmos DB marketplace on every push to `dev` or `main`.

### How It Works

```
marketplace/catalog.json  →  sync-marketplace.py  →  GitHub Actions workflow
      (source of truth)        (upsert to Cosmos)      (trigger on dev/main push)
```

1. **`marketplace/catalog.json`** — Declarative source of truth for all 38 first-party tools and 3 plugins. Defines providers, tools, and plugin bundles with full metadata (parameters, descriptions, tags, setup requirements).
2. **`marketplace/sync-marketplace.py`** — Python script that reads the catalog and upserts `MarketplaceItem` documents to Cosmos DB using `azure-cosmos` + `azure-identity` (OIDC via `DefaultAzureCredential`).
3. **`.github/workflows/sync-marketplace.yml`** — GitHub Actions workflow triggered on pushes to `dev` or `main` that touch `marketplace/` or `Daisi.SecureTools/`. Also supports manual `workflow_dispatch`.

### Branch → Environment Mapping

| Branch | GitHub Environment | Target Database |
|--------|-------------------|-----------------|
| `dev`  | `development`     | Dev Cosmos DB   |
| `main` | `production`      | Prod Cosmos DB  |

### Catalog Structure

The catalog defines three sections:

- **Providers** (15) — Route prefixes and setup parameters (OAuth or API key) for each tool provider
- **Tools** (38) — Individual tool definitions with ID, name, description, tags, parameters, and AI use instructions
- **Plugins** (3) — Bundles that group tools sharing a single OAuth flow: Google Workspace (10 tools), Microsoft 365 (9 tools), Firecrawl (5 tools)

### Adding a New Tool

1. Add the tool implementation under `Daisi.SecureTools/`
2. Add a provider entry to `catalog.json` if the provider is new
3. Add the tool entry to the `tools` array in `catalog.json` with all metadata
4. If the tool belongs to an existing plugin, add its `toolId` to the plugin's `toolIds` array
5. Push to `dev` — the pipeline will automatically create the marketplace item in the dev database

### Required GitHub Repository Secrets

Shared secrets (same for dev and prod):

| Secret | Purpose |
|--------|---------|
| `AZURE_CLIENT_ID` | OIDC federated credential for Azure login |
| `AZURE_TENANT_ID` | Azure AD tenant |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription |
| `DAISI_ACCOUNT_ID` | System account ID for first-party tools |

Per-environment secrets (prefixed `DEV_` / `PROD_`):

| Secret | Purpose |
|--------|---------|
| `DEV_COSMOSDB_ACCOUNT_NAME` | Dev Cosmos DB account name |
| `DEV_COSMOSDB_DATABASE_NAME` | Dev database name |
| `DEV_SECURE_ENDPOINT_URL` | Dev Azure Functions base URL |
| `DEV_SECURE_AUTH_KEY` | Dev shared secret for X-Daisi-Auth |
| `PROD_COSMOSDB_ACCOUNT_NAME` | Prod Cosmos DB account name |
| `PROD_COSMOSDB_DATABASE_NAME` | Prod database name |
| `PROD_SECURE_ENDPOINT_URL` | Prod Azure Functions base URL |
| `PROD_SECURE_AUTH_KEY` | Prod shared secret for X-Daisi-Auth |

### Idempotency

The sync script uses `upsert_item()` and preserves existing metrics (download counts, ratings, featured status) from previously synced items. It is safe to run repeatedly — the same catalog produces the same documents.

## Running Tests

```bash
# Secure tool tests (59 tests)
dotnet test Daisi.SecureTools.Tests

# Shared library tests (22 tests)
dotnet test SecureToolProvider.Common.Tests
```

## Deployment

All secure tool providers deploy as a single Azure Function App:

```bash
cd Daisi.SecureTools
func azure functionapp publish <app-name> --build remote
```

Required app settings:

| Setting | Description |
|---------|-------------|
| `DaisiAuthKey` | Shared secret for ORC authentication |
| `GoogleClientId` | Google OAuth client ID |
| `GoogleClientSecret` | Google OAuth client secret |
| `MicrosoftClientId` | Microsoft OAuth client ID |
| `MicrosoftClientSecret` | Microsoft OAuth client secret |
| `MicrosoftTenantId` | Azure AD tenant ID (default: `common`) |
| `XClientId` | X (Twitter) OAuth client ID |
| `XClientSecret` | X (Twitter) OAuth client secret |
| `FacebookClientId` | Facebook OAuth app ID |
| `FacebookClientSecret` | Facebook OAuth app secret |
| `RedditClientId` | Reddit OAuth client ID |
| `RedditClientSecret` | Reddit OAuth client secret |
| `LinkedInClientId` | LinkedIn OAuth client ID |
| `LinkedInClientSecret` | LinkedIn OAuth client secret |
| `InstagramClientId` | Instagram (Facebook Login) app ID |
| `InstagramClientSecret` | Instagram (Facebook Login) app secret |
| `TikTokClientKey` | TikTok OAuth client key |
| `TikTokClientSecret` | TikTok OAuth client secret |
| `TwilioAccountSid` | Twilio Account SID (optional — can be user-configured) |
| `TwilioAuthToken` | Twilio Auth Token (optional — can be user-configured) |
| `TwilioSendGridApiKey` | SendGrid API key for email (optional — can be user-configured) |
| `WhatsAppClientId` | WhatsApp (Meta) OAuth app ID |
| `WhatsAppClientSecret` | WhatsApp (Meta) OAuth app secret |
| `XDmClientId` | X DMs OAuth client ID |
| `XDmClientSecret` | X DMs OAuth client secret |
| `TeamsClientId` | Teams OAuth client ID |
| `TeamsClientSecret` | Teams OAuth client secret |
| `TeamsTenantId` | Teams Azure AD tenant ID (default: `common`) |
| `SlackClientId` | Slack OAuth client ID |
| `SlackClientSecret` | Slack OAuth client secret |
