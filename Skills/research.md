---
name: research
description: Multi-source research workflow that searches the web, fetches relevant pages, converts to Markdown, and synthesizes findings.
shortDescription: Search → Fetch → Summarize → Synthesize
version: "1.0.0"
author: DaisiBot
isRequired: true
tags:
  - research
  - web
  - analysis
tools:
  - InformationTools
---

## Research Workflow

When the user asks you to research a topic:

1. Use the **Web Search** tool to find relevant sources for the topic.
2. For the top 2-3 most relevant URLs from search results, use **HTTP Get** to fetch each page.
3. Use **HTML to Markdown** to convert each page's HTML to clean Markdown.
4. Use **Summarize Text** to summarize each page individually.
5. Synthesize all summaries into a comprehensive answer that cites sources.

Always cite the URLs where information was found. If search results are insufficient, try refining the query and searching again.
