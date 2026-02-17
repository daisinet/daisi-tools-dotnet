# Daisi Tools
This solution contains the Tools for the inference engine that are provided by Daisi. It shows examples of how you can build tools that the inference engine can use to extend the base functionality.

## SecureToolProvider (Reference Implementation)

The `SecureToolProvider/` directory contains a reference Azure Functions implementation of the Daisinet Secure Tool Provider API. This demonstrates how marketplace providers can build tools that execute on their own servers while keeping credentials private.

**What it shows:**
- `SecureToolFunctions.cs` — Four HTTP endpoints implementing the provider contract:
  - `/install` — ORC calls on purchase with `X-Daisi-Auth` header and `installId`
  - `/uninstall` — ORC calls on deactivation with `X-Daisi-Auth` header and `installId`
  - `/configure` — Manager UI calls directly with `installId` (no auth header)
  - `/execute` — Consumer hosts call directly with `installId` (no auth header)
- `SetupStore.cs` — In-memory installation registry and setup data storage (replace with Azure Key Vault or encrypted DB in production)
- `Models.cs` — Request/response models matching the Daisinet provider API contract

**OAuth reference endpoints:**
- `/auth/start` (GET) — OAuth initiation. Receives `installId`, `returnUrl`, `service` as query params. In production, redirects to external consent screen; in the reference impl, simulates by redirecting to own callback.
- `/auth/callback` (GET) — OAuth callback. Decodes state, exchanges code for tokens (simulated), stores them via `SetupStore.SaveOAuthTokens()`, and redirects popup back to Daisinet's `/marketplace/oauth-callback`.
- `/auth/status` (POST) — Connection status check. Receives `{ installId, service }`, returns `{ connected, serviceName, userLabel }`. Called by the Manager UI to display OAuth connection badges.

**Authentication model:**
- `/install` and `/uninstall` are ORC-originated — verified via `X-Daisi-Auth` shared secret
- `/configure` and `/execute` are consumer-originated — verified by checking that the `installId` was registered via `/install`. The `installId` is an opaque, unguessable identifier that serves as a bearer token.

**To use as a starting point:**
1. Clone the `SecureToolProvider` directory
2. Replace the echo logic in `Execute` with your actual tool implementation
3. Replace `SetupStore` with a secure storage backend (Azure Key Vault recommended)
4. Set the `ExpectedAuthKey` constant to match what you configure in the marketplace item
5. Deploy to Azure Functions (or any HTTP-capable host)
6. Create a marketplace item with Secure Execution enabled, pointing to your deployed URL

See the [Creating Secure Tools](https://daisi.ai/learn/marketplace/creating-secure-tools) guide and the [API Reference](https://daisi.ai/learn/marketplace/secure-tool-api-reference) for the full contract specification.
