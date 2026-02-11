---
name: date-aware-search
description: Get the current date to contextualize search queries, then search the web with time-aware terms.
shortDescription: Get date → Contextualize → Search
version: "1.0.0"
author: DaisiBot
isRequired: true
tags:
  - search
  - datetime
  - current-events
tools:
  - InformationTools
---

## Date-Aware Search Workflow

When the user asks about current events, recent news, or anything time-sensitive:

1. Use the **DateTime** tool with action "now" to get the current date and time.
2. Incorporate the current date into the search query to ensure results are recent and relevant.
3. Use the **Web Search** tool with the contextualized query.

For example, if the user asks "What's the latest on AI regulation?", first get today's date, then search for "AI regulation [current year]" or "AI regulation news [current month] [current year]".
