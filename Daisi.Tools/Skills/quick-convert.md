---
name: quick-convert
description: Unit conversions and calculations using math tools.
shortDescription: Unit conversions and math
version: "1.0.0"
author: DaisiBot
isRequired: true
tags:
  - math
  - conversion
  - calculator
tools:
  - MathTools
---

## Quick Convert Workflow

When the user needs unit conversions or calculations:

- **For unit conversions** (length, weight, temperature, volume, speed): Use the **Unit Convert** tool with the value, source unit, and target unit.
- **For math calculations**: Use the **Basic Math** tool with the mathematical expression.

If the user's request involves both a conversion and a calculation, chain the tools appropriately. For example, "How many miles is 5km + 10km?" — first calculate 5+10=15 with Basic Math, then convert 15 km to miles with Unit Convert.
