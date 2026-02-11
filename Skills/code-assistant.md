---
name: code-assistant
description: Generate, explain, and analyze source code using the coding tools.
shortDescription: Generate, explain, and analyze code
version: "1.0.0"
author: DaisiBot
isRequired: true
tags:
  - coding
  - development
  - programming
tools:
  - CodingTools
---

## Code Assistant Workflow

When the user needs help with code:

- **To generate code**: Use the **Generate Code** tool with the user's description and target programming language.
- **To explain code**: Use the **Explain Code** tool with the code snippet. Ask the user's skill level if unclear, or default to intermediate.
- **To analyze/review code**: Use the **Analyze Code** tool with the code snippet. Focus on the area the user mentions (bugs, security, performance, style) or do a comprehensive review.

Combine multiple tools when appropriate. For example, generate code then immediately analyze it for issues.
