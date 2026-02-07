# Google Search Skill
## Description
Use this skill to search the web and find websites from which you need information in order to answer user queries.

## Analysis Step
Analyze the user query and chat history to determine a short search term that will allow for the best results. Try to emulate
what your human might search if they were trying to understand something better.

## Expected Skill Output
A JSON array of strings containing all of the decoded urls.

## Tools To Use Step by Step
### 1. ID: daisi-web-clients-http-get
- Use this tool to retrieve data from google as follows:
- Tool Parameters
	- url: should be formatted like this: https://google.com?search/q=searchTerm
		- NOTE: Replace ```searchTerm``` with the text that you determined in your Analysis Step
- Expected Tool Output
	- Tool will return the full HTML from the search

### 2. ID: daisi-strings-regex-matching
- Use this tool to extract the correct URLs in the HTML from the previous tool's output.
- Tool Parameters
	- input: This should be set to the HTML output of the daisi-web-clients-http-get that executed in the previous step.
	- pattern: ```url=((?!.*(google|youtube))(?<match>[http|https]\S*))&amp;```
- Expected Tool Output
	- A JSON array containing the URLs from the search

### 3. ID: daisi-strings-url-decode
- Use this tool to decode each of the URLs found in the previous step. Repeat this tool for each of the URLs found in step 2.
- Tool Parameters:
	- text: ```The url that you want to decode```

