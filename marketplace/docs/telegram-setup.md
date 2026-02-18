# Telegram — User Setup Guide

Telegram tools use a **user-provided Bot Token** — there are no Daisi-level app settings to configure. Each user creates their own Telegram bot and provides the token during tool configuration in the Marketplace.

## For Users

### Step 1: Create a Telegram Bot

1. Open Telegram and search for **@BotFather** (the official bot for creating bots)
2. Start a chat with BotFather and send: `/newbot`
3. BotFather will ask for a **name** for your bot — this is the display name (e.g., `My Daisi Assistant`)
4. BotFather will ask for a **username** — this must end in `bot` (e.g., `my_daisi_assistant_bot`)
5. BotFather will reply with your **Bot Token** — it looks like: `123456789:ABCdefGHIjklMNOpqrsTUVwxyz`
6. Copy this token — you'll enter it when configuring the tool

### Step 2: Configure Your Bot (Optional)

While chatting with BotFather, you can customize your bot:

- `/setdescription` — Set the bot's description (shown on the bot's profile)
- `/setabouttext` — Set the "About" text
- `/setuserpic` — Set the bot's profile picture
- `/setcommands` — Define slash commands the bot responds to

### Step 3: Get Your Chat ID

To send messages, you need the **Chat ID** of the recipient chat:

**For a direct message to yourself:**
1. Send any message to your bot
2. Open `https://api.telegram.org/bot<YOUR_TOKEN>/getUpdates` in a browser
3. Look for `"chat":{"id":123456789}` — this is your Chat ID

**For a group chat:**
1. Add the bot to the group
2. Send a message in the group
3. Check `getUpdates` as above — the group Chat ID will be negative (e.g., `-1001234567890`)

**For a channel:**
1. Add the bot as an administrator of the channel
2. The Chat ID is `@channelname` or the numeric ID from `getUpdates`

### Step 4: Configure in Daisinet Marketplace

When you install the Telegram tool from the Daisinet Marketplace and click **Configure**, enter:

| Parameter | Value |
|-----------|-------|
| `botToken` | Your Bot Token from BotFather (e.g., `123456789:ABCdef...`) |

The Chat ID is provided as a parameter when executing the tool, not during configuration.

## Supported Message Types

| Type | Description |
|------|-------------|
| Text | Plain text or Markdown-formatted messages |
| Photo | Send an image by URL |
| Document | Send a file by URL |
| Video | Send a video by URL |

## Bot Permissions

- Bots can send messages to users who have started a conversation with the bot
- Bots must be added to groups to send messages there
- Bots must be admins of channels to post to them
- Bots cannot initiate conversations — a user must message the bot first

## For Platform Administrators

No Daisi-level app settings are required for Telegram. All credentials are user-provided via the Configure flow. The `local.settings.json` and Azure Function App Settings do not need any Telegram entries.

## Troubleshooting

- **"Unauthorized" (401)** — The bot token is invalid or has been revoked. Create a new bot with BotFather or use `/token` to regenerate
- **"Bad Request: chat not found"** — The Chat ID is incorrect, or the bot hasn't been added to the chat. Ensure the bot has access to the target chat
- **"Forbidden: bot was blocked by the user"** — The recipient has blocked the bot. They need to unblock and send `/start` again
- **"Forbidden: bot is not a member of the channel chat"** — Add the bot as an administrator of the channel
- **Messages not sending to group** — The bot must be added to the group. If group privacy mode is on, the bot can only see commands directed at it
