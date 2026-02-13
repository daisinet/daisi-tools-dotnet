---
name: fact-check
description: Verify claims using web search and Wikipedia for factual grounding.
shortDescription: Verify claims with search + Wikipedia
version: "1.0.0"
author: DaisiBot
isRequired: true
tags:
  - fact-check
  - verification
  - research
tools:
  - InformationTools
  - IntegrationTools
---

## Fact Check Workflow

When the user asks you to verify a claim or check facts:

1. Use the **Wikipedia Search** tool to look up the core topic for established factual information.
2. If needed, use **HTTP Get** to fetch specific pages and **Summarize Text** to extract relevant details.
3. Compare the claim against the evidence found from Wikipedia and any fetched sources.
4. Present your findings clearly, noting whether the claim is supported, partially supported, or not supported by the evidence.

Always cite your sources. If evidence is conflicting, present both sides.
