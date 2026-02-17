# Firecrawl — User Setup Guide

Firecrawl tools use a **user-provided API key** — there are no Daisi-level app settings to configure. Each user provides their own Firecrawl API key during tool configuration in the Marketplace.

## For Users

### Step 1: Create a Firecrawl Account

1. Go to [Firecrawl](https://www.firecrawl.dev/) and click **Sign Up**
2. Create an account with email or GitHub

### Step 2: Choose a Plan

Firecrawl offers several plans:

| Plan | Credits/month | Price |
|------|--------------|-------|
| Free | 500 | $0 |
| Hobby | 3,000 | $16/mo |
| Standard | 100,000 | $83/mo |
| Growth | 500,000 | $333/mo |

Credits are consumed per page scraped/crawled. Choose based on your expected usage.

### Step 3: Get Your API Key

1. Log in to the [Firecrawl Dashboard](https://www.firecrawl.dev/app)
2. Navigate to **API Keys** (or the dashboard home page)
3. Copy your API key — it starts with `fc-`

### Step 4: Configure in Daisinet Marketplace

When you install a Firecrawl tool from the Daisinet Marketplace and click **Configure**, enter:

| Parameter | Value | Required |
|-----------|-------|----------|
| `apiKey` | Your Firecrawl API key (`fc-...`) | Yes |
| `baseUrl` | Custom API base URL | No (defaults to `https://api.firecrawl.dev`) |

The `baseUrl` parameter is only needed if you're running a self-hosted Firecrawl instance.

## Available Tools

| Tool | Credits Per Use | Description |
|------|----------------|-------------|
| **Scrape** | 1 credit/page | Scrape a single URL, returns clean markdown |
| **Crawl** | 1 credit/page | Crawl multiple pages from a starting URL |
| **Search** | 1 credit/result | Search the web and extract content from results |
| **Extract** | 5 credits/page | AI-powered structured data extraction using a schema |
| **Map** | 1 credit | Discover all URLs on a website (sitemap) |

## Self-Hosting (Optional)

Firecrawl is open source and can be self-hosted:

1. Clone the [Firecrawl repo](https://github.com/mendableai/firecrawl)
2. Follow the self-hosting guide in the README
3. Set the `baseUrl` configuration parameter to your self-hosted instance URL

## For Platform Administrators

No Daisi-level app settings are required for Firecrawl. All credentials are user-provided via the Configure flow. The `local.settings.json` and Azure Function App Settings do not need any Firecrawl entries.

## Troubleshooting

- **"Unauthorized" (401)** — The API key is invalid. Check that it starts with `fc-` and was copied correctly
- **"Rate limit exceeded" (429)** — You've hit the rate limit for your plan. Wait and retry, or upgrade your plan
- **"Insufficient credits"** — Your monthly credits are exhausted. Wait for the next billing cycle or upgrade
- **Crawl returns empty results** — The target site may block crawlers. Try the Scrape tool for a single page, or check if the site uses heavy JavaScript rendering
- **Extract returns unexpected data** — Refine the extraction schema/prompt to be more specific about the data structure you want
