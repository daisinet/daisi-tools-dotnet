---
name: web-search
description: Search the web for information using a query.
tools: [InformationTools]
---

# Web Search Skill
## Description
Use this skill to search the web and find information needed to answer user queries.

## Analysis Step
Analyze the user query and chat history to determine a short, focused search term that will yield the best results. Try to emulate what your human might search if they were trying to understand something better.

## Expected Skill Output
A JSON array of URL strings from search results. Use these URLs to identify relevant sources, then optionally fetch the most promising URLs for detailed information.

## Tools To Use
### 1. ID: daisi-info-web-search
- Use this tool to search the web for relevant results.
- Tool Parameters
	- query: The search term determined in your Analysis Step. Keep it concise and specific.
	- max-results: (Optional) The maximum number of results to return. Default is 5.
- Expected Tool Output
	- A JSON array of URL strings.

### 2. ID: daisi-web-clients-http-get (Optional)
- After receiving search results, use this tool to fetch full content from the most relevant URLs.
- Tool Parameters
	- url: One of the URLs from the search results that appears most relevant to the user's query.
- Expected Tool Output
	- The full HTML or text content from the URL, which you can use to formulate your answer.

## Example Flow
1. User asks: "What is the latest version of Python?"
2. You determine search term: "Python latest version 2026"
3. Call `daisi-info-web-search` with query "Python latest version 2026"
4. Review the returned URL list
5. If more detail is needed, call `daisi-web-clients-http-get` on the most relevant URL
6. Formulate your answer using the information gathered
