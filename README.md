# AzDo Boards Project Management Tool

***NOTE**: This is under heavy development. In short, it is very incomplete. This is an idea that has been noodled for a while. Now this is being attempted for future project efficiency, to give back, to see if there is interest within the open source community, and to explore potential business opportunities. The goal is to try different items to see what looks and feels right. The goal is to make a usable product and remove this banner. Perhaps this could be commercially viable as a subscription, with enterprise-scale multi-tenant support based on the open-source version.*

## Overview

<img src="Images/favicon-0512.png" alt="AzDo Boards Icon" style="zoom:25%; float:right;" />AzDo Boards (pronounced "As Doe Boards") is to be a tool to assist with project management within Azure DevOps Boards. The UI in Azure DevOps Boards is clunky for project management and for quickly adding or updating information. 

This supports a custom-defined work item hierarchy based on work item types (e.g., Initiative > Epic > Feature > User Story / Bug / Research > Tasks). This helps with reporting, tracking, and querying.

# Architecture

A Blazor WebAssembly application for viewing Azure DevOps boards. Users authenticate via Entra ID and access Azure DevOps resources using their own permissions — no backend required.

## Authentication

```
Browser (Blazor WASM)
  └── MSAL.js (via Microsoft.Authentication.WebAssembly.Msal)
        ├── Login → Entra ID
        └── Token (Azure DevOps scope) → Azure DevOps REST API
```

The application is hosted on **Azure Static Web Apps**, which serves the Blazor WASM static output globally via CDN.

Configuration values (tenant ID, client ID, org URL) are substituted into `appsettings.json` at deploy time by the Azure Pipelines `FileTransform` task, sourcing from a pipeline variable group. No secrets are stored in the repository.

## Projects

| Project | Purpose |
|---|---|
| `AzDoBoards.Ui` | Blazor WebAssembly host application |
| `AzDoBoards.Client` | Core client abstractions |
| `AzDoBoards.Client.DevOps` | Azure DevOps API client (depends on AzDoBoards.Client) |
| `AzDoBoards.Models` | Shared domain models |
| `AzDoBoards.Data` | Data access |
| `AzDoBoards.Utility` | Shared utilities |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- An Azure subscription
- An Azure DevOps organization
- An Entra ID (Azure AD) tenant

## Entra ID App Registration

1. Go to **Azure Portal → Entra ID → App registrations → New registration**
2. Name: `AzDoBoards` (or your preference)
3. Supported account types: choose based on your audience
4. Redirect URI: **Single-page application (SPA)**
   - Local dev: `http://localhost:5039/authentication/login-callback`
   - Production: `https://<your-swa-hostname>/authentication/login-callback`
5. After creation, go to **API permissions → Add a permission → APIs my organization uses**
   - Search for **Azure DevOps**
   - Select **Delegated** → `user_impersonation`
   - Click **Grant admin consent**
6. Note your **Tenant ID** and **Client ID** from the Overview page

## Local Development

1. Fill in your real values in `AzDoBoards.Ui/wwwroot/appsettings.Secrets.json`:

```json
{
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/<your-tenant-id>",
    "ClientId": "<your-client-id>",
    "ValidateAuthority": true
  },
  "AzureDevOps": {
    "OrgUrl": "https://dev.azure.com/<your-org>"
  }
}
```

2. This file is gitignored and will never be committed.

3. Launch via VS Code: press `F5` → **Launch AzDoBoards.Ui**

## Infrastructure Deployment

Infrastructure is defined in `azure-resources.bicep` at the repository root.

```bash
az login
az group create --name <resource-group> --location eastus
az deployment group create \
  --resource-group <resource-group> \
  --template-file azure-resources.bicep \
  --parameters name=<swa-name> sku=Standard
```

After deployment, add the SWA callback URI to your Entra ID app registration.

## CI/CD Pipeline (Azure Pipelines)

The pipeline is defined in `azure-pipelines.yml` at the repository root.

### Variable groups

Create the following variable groups in **Pipelines → Library**:

**`azdoboards-config`** (shared across all environments):

| Variable | Example Value |
|---|---|
| `a_AzureServiceConnection` | Name of your Azure service connection |
| `a_SubscriptionId` | Your Azure subscription ID |

**`azdoboards-config-dev`**, **`azdoboards-config-uat`**, **`azdoboards-config-prd`** (one per environment):

| Variable | Example Value |
|---|---|
| `a_ResourceGroup` | `rg-azdoboards-dev` |
| `a_ResourceGroupLocation` | `eastus` |
| `a_SwaName` | `azdoboards-dev` |
| `a_SwaSkuName` | `Standard` (optional override) |
| `AzureAd.Authority` | `https://login.microsoftonline.com/<tenant-id>` |
| `AzureAd.ClientId` | `<client-id>` |
| `AzureDevOps.OrgUrl` | `https://dev.azure.com/<your-org>` |

The `AzureAd.*` and `AzureDevOps.*` variables are transform variables — their dot-notation names must exactly match the key paths in `appsettings.json` for the `FileTransform` task and cannot carry a prefix.

### Pipeline stages

1. **Build** — Triggered automatically on `release/*` and `feature/*` branches
2. **DEV** — Runs on `release/*` and `feature/*/build/*` branches; infrastructure then app
3. **UAT** — Runs on `release/*` branches only; depends on DEV succeeding
4. **PRD** — Runs on `release/*` branches only; depends on UAT succeeding

### Environment approval gates

Configure approval gates and branch controls under **Pipelines → Environments** in Azure DevOps:

| Environment | Approval | Branch Control |
|---|---|---|
| `AzDoBoards DEV` | Optional | `release/*`, `feature/*/build/*` |
| `AzDoBoards UAT` | Required | `release/*` |
| `AzDoBoards PRD` | Required | `release/*` |

\~END~
