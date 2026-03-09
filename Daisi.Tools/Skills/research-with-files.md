---
name: research-with-files
description: Research a topic via web search, save findings to Daisi Drive, and reference in future conversations.
shortDescription: Research → Save to Drive → Reference later
version: "1.0.0"
author: DaisiBot
isRequired: false
tags:
  - research
  - drive
  - files
  - web
tools:
  - InformationTools
  - DriveTools
---

## Research with Files

When the user asks you to deeply research a topic and save the results:

1. Use **Grokipedia Search** to find relevant factual information about the topic.
2. If the user provides specific URLs, use **HTTP Get** to fetch each page.
3. Use **HTML to Markdown** to convert each page's HTML to clean Markdown.
4. Use **Summarize Text** to summarize each page individually.
5. Synthesize all summaries into a comprehensive research document.
6. Use **Drive Save File** to save the research to Drive:
   - File name: `{topic-slug}.md`
   - Path: `/research`
   - Include sources with URLs at the bottom
7. Optionally save a cache copy as a system file:
   - Path: `/system/research-cache`
   - Set `is-system=true`

### Follow-up References

When a user asks about a previously researched topic:
1. Use **Drive Search** to find existing research files.
2. Use **Drive Read File** to load the saved research.
3. Build your response using the cached research as context.
4. If the research is outdated, offer to refresh it with new web searches.

Always cite the URLs where information was found. If the user asks to update research, search for the existing file first and create a new version.
