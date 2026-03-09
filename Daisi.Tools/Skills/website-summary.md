---
name: website-summary
description: Fetch a URL, convert its HTML content to clean Markdown, then summarize the key information.
shortDescription: Fetch URL → Markdown → Summarize
version: "1.0.0"
author: DaisiBot
isRequired: true
tags:
  - web
  - summary
  - research
tools:
  - InformationTools
---

## Website Summary Workflow

When the user wants to summarize a website or get the main content from a URL:

1. Use the **HTTP Get** tool to fetch the raw HTML content from the given URL.
2. Use the **HTML to Markdown** tool to convert the HTML into clean, readable Markdown.
3. Use the **Summarize Text** tool to create a concise summary of the Markdown content.

Return the summary to the user. If the user wants the full content instead of a summary, return the Markdown from step 2.
