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
2. Incorporate the current date into the query to ensure results are recent and relevant.
3. Use the **Wikipedia Search** tool or **HTTP Get** (if a URL is provided) with the contextualized query.

For example, if the user asks "What's the latest on AI regulation?", first get today's date, then search for relevant information using the current year as context.
