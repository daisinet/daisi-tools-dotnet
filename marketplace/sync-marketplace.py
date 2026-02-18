"""
Sync marketplace catalog to Cosmos DB.

Reads catalog.json and upserts MarketplaceItem documents for all first-party
secure tools and plugins. Preserves existing metrics (download counts, ratings)
on update. Safe to run repeatedly (idempotent via upsert).

Required environment variables:
  COSMOSDB_ACCOUNT       - Cosmos DB account name
  COSMOSDB_DATABASE      - Database name
  DAISI_ACCOUNT_ID       - System account ID for first-party tools
  SECURE_ENDPOINT_URL    - Azure Functions base URL (e.g. https://func.azurewebsites.net/api)
  SECURE_AUTH_KEY        - Shared secret for X-Daisi-Auth header
"""

import json
import os
import sys
from datetime import datetime, timezone
from pathlib import Path

from azure.cosmos import CosmosClient
from azure.cosmos.exceptions import CosmosResourceNotFoundError
from azure.identity import DefaultAzureCredential

ICON_URL = "https://daisi.ai/assets/brand-kit/logo/svg/daisi-logomark-color.svg"
AUTHOR = "Daisi"
VERSION = "1.0.0"
CONTAINER_NAME = "Marketplace"

# Fields to preserve from existing documents (not overwritten on update)
PRESERVED_FIELDS = [
    "CreatedAt",
    "DownloadCount",
    "PurchaseCount",
    "AverageRating",
    "RatingCount",
    "IsFeatured",
]


def load_catalog():
    catalog_path = Path(__file__).parent / "catalog.json"
    with open(catalog_path, "r", encoding="utf-8") as f:
        return json.load(f)


def get_env(name):
    value = os.environ.get(name)
    if not value:
        print(f"Error: environment variable {name} is not set")
        sys.exit(1)
    return value


def build_setup_parameters(provider_def, endpoint_url):
    """Build SetupParameterData list from provider definition."""
    params = []
    for sp in provider_def.get("setupParameters", []):
        param = {
            "Name": sp["name"],
            "Description": sp["description"],
            "Type": sp["type"],
            "IsRequired": sp["isRequired"],
            "AuthUrl": "",
            "ServiceLabel": sp.get("serviceLabel", ""),
        }
        suffix = sp.get("authUrlSuffix")
        if suffix:
            # Build full auth URL from endpoint base + auth path
            base = endpoint_url.rstrip("/")
            param["AuthUrl"] = f"{base}/{suffix}"
        params.append(param)
    return params


def build_tool_definition(tool_def):
    """Build SecureToolDefinitionData from tool catalog entry."""
    return {
        "ToolId": tool_def["toolId"],
        "Name": tool_def["name"],
        "UseInstructions": tool_def.get("useInstructions", ""),
        "ToolGroup": tool_def.get("toolGroup", ""),
        "Parameters": [
            {
                "Name": p["name"],
                "Description": p["description"],
                "IsRequired": p["isRequired"],
            }
            for p in tool_def.get("parameters", [])
        ],
    }


def try_read_existing(container, item_id, partition_key):
    """Try to read an existing item, return None if not found."""
    try:
        return container.read_item(item_id, partition_key)
    except CosmosResourceNotFoundError:
        return None


def build_tool_item(tool_def, provider_def, catalog, account_id, endpoint_url, auth_key):
    """Build a MarketplaceItem document for a tool."""
    now = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    route_prefix = provider_def["routePrefix"]

    return {
        "id": tool_def["toolId"],
        "type": "MarketplaceItem",
        "AccountId": account_id,
        "ProviderId": tool_def["provider"],
        "Name": tool_def["name"],
        "Description": tool_def["description"],
        "ShortDescription": tool_def.get("shortDescription", ""),
        "Author": AUTHOR,
        "Version": VERSION,
        "IconUrl": ICON_URL,
        "Tags": tool_def.get("tags", []),
        "Screenshots": [],
        "ItemType": "HostTool",
        "SkillId": None,
        "ToolClassName": None,
        "BundledItemIds": [],
        "PackageBlobUrl": None,
        "PricingModel": "MarketplacePricingFree",
        "CreditPrice": 0,
        "SubscriptionCreditPrice": 0,
        "SubscriptionPeriodDays": 30,
        "Status": "Approved",
        "Visibility": "Public",
        "ReviewedBy": "marketplace-pipeline",
        "ReviewedAt": now,
        "RejectionReason": None,
        "DownloadCount": 0,
        "PurchaseCount": 0,
        "AverageRating": 0.0,
        "RatingCount": 0,
        "IsFeatured": False,
        "IsSecureExecution": True,
        "SecureEndpointUrl": f"{endpoint_url.rstrip('/')}/{route_prefix}",
        "SecureAuthKey": auth_key,
        "SetupParameters": build_setup_parameters(provider_def, endpoint_url),
        "SecureToolDefinition": build_tool_definition(tool_def),
        "CreatedAt": now,
        "UpdatedAt": now,
    }


def build_plugin_item(plugin_def, provider_def, account_id, endpoint_url, auth_key):
    """Build a MarketplaceItem document for a plugin."""
    now = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")

    return {
        "id": f"daisi-plugin-{plugin_def['pluginId']}",
        "type": "MarketplaceItem",
        "AccountId": account_id,
        "ProviderId": plugin_def["provider"],
        "Name": plugin_def["name"],
        "Description": plugin_def["description"],
        "ShortDescription": plugin_def.get("shortDescription", ""),
        "Author": AUTHOR,
        "Version": VERSION,
        "IconUrl": ICON_URL,
        "Tags": plugin_def.get("tags", []),
        "Screenshots": [],
        "ItemType": "Plugin",
        "SkillId": None,
        "ToolClassName": None,
        "BundledItemIds": plugin_def.get("toolIds", []),
        "PackageBlobUrl": None,
        "PricingModel": "MarketplacePricingFree",
        "CreditPrice": 0,
        "SubscriptionCreditPrice": 0,
        "SubscriptionPeriodDays": 30,
        "Status": "Approved",
        "Visibility": "Public",
        "ReviewedBy": "marketplace-pipeline",
        "ReviewedAt": now,
        "RejectionReason": None,
        "DownloadCount": 0,
        "PurchaseCount": 0,
        "AverageRating": 0.0,
        "RatingCount": 0,
        "IsFeatured": False,
        "IsSecureExecution": True,
        "SecureEndpointUrl": f"{endpoint_url.rstrip('/')}/{provider_def['routePrefix']}",
        "SecureAuthKey": auth_key,
        "SetupParameters": build_setup_parameters(provider_def, endpoint_url),
        "SecureToolDefinition": None,
        "CreatedAt": now,
        "UpdatedAt": now,
    }


def merge_with_existing(new_item, existing_item):
    """Preserve metrics and timestamps from an existing document."""
    if existing_item is None:
        return new_item

    for field in PRESERVED_FIELDS:
        if field in existing_item:
            new_item[field] = existing_item[field]

    return new_item


def main():
    # Load configuration
    account_name = get_env("COSMOSDB_ACCOUNT")
    database_name = get_env("COSMOSDB_DATABASE")
    account_id = get_env("DAISI_ACCOUNT_ID")
    endpoint_url = get_env("SECURE_ENDPOINT_URL")
    auth_key = get_env("SECURE_AUTH_KEY")

    # Load catalog
    catalog = load_catalog()
    providers = catalog["providers"]
    tools = catalog["tools"]
    plugins = catalog["plugins"]

    print(f"Catalog loaded: {len(tools)} tools, {len(plugins)} plugins, {len(providers)} providers")

    # Connect to Cosmos DB
    credential = DefaultAzureCredential()
    client = CosmosClient(
        f"https://{account_name}.documents.azure.com:443/",
        credential,
    )
    db = client.get_database_client(database_name)
    container = db.get_container_client(CONTAINER_NAME)

    print(f"Connected to {account_name}/{database_name}/{CONTAINER_NAME}")

    # Sync tools
    tool_count = 0
    tool_errors = 0
    for tool_def in tools:
        tool_id = tool_def["toolId"]
        provider_key = tool_def["provider"]
        provider_def = providers.get(provider_key)

        if not provider_def:
            print(f"  SKIP {tool_id}: unknown provider '{provider_key}'")
            tool_errors += 1
            continue

        try:
            existing = try_read_existing(container, tool_id, account_id)
            item = build_tool_item(tool_def, provider_def, catalog, account_id, endpoint_url, auth_key)
            item = merge_with_existing(item, existing)
            item["UpdatedAt"] = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")

            container.upsert_item(item)

            action = "updated" if existing else "created"
            print(f"  {action}: {tool_id} ({tool_def['name']})")
            tool_count += 1
        except Exception as e:
            print(f"  ERROR {tool_id}: {e}")
            tool_errors += 1

    # Sync plugins
    plugin_count = 0
    plugin_errors = 0
    for plugin_def in plugins:
        plugin_id = f"daisi-plugin-{plugin_def['pluginId']}"
        provider_key = plugin_def["provider"]
        provider_def = providers.get(provider_key)

        if not provider_def:
            print(f"  SKIP {plugin_id}: unknown provider '{provider_key}'")
            plugin_errors += 1
            continue

        try:
            existing = try_read_existing(container, plugin_id, account_id)
            item = build_plugin_item(plugin_def, provider_def, account_id, endpoint_url, auth_key)
            item = merge_with_existing(item, existing)
            item["UpdatedAt"] = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")

            container.upsert_item(item)

            action = "updated" if existing else "created"
            print(f"  {action}: {plugin_id} ({plugin_def['name']})")
            plugin_count += 1
        except Exception as e:
            print(f"  ERROR {plugin_id}: {e}")
            plugin_errors += 1

    # Summary
    print()
    print("=" * 50)
    print(f"Tools:   {tool_count} synced, {tool_errors} errors")
    print(f"Plugins: {plugin_count} synced, {plugin_errors} errors")
    print(f"Total:   {tool_count + plugin_count} items synced")
    print("=" * 50)

    if tool_errors or plugin_errors:
        sys.exit(1)


if __name__ == "__main__":
    main()
