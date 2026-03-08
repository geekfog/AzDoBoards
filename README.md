# About

<img src="Images/favicon-0512.png" alt="AzDo Boards Icon" style="zoom:25%; float:right;" />AzDo Boards (pronounced "As Doe Boards") is intended to be a tool to assist with project management by leveraging work item data in Azure DevOps Boards. Running externally allows freedom to leverage external technologies for project management-focused capabilities, including reporting, and to quickly add or update information. There is an eye to incorporate AI to help build out projects.

This supports a custom-defined work item hierarchy based on work item types (e.g., Initiative > Epic > Feature > User Story / Bug / Research > Tasks). This helps with reporting, tracking, and querying within Azure DevOps and externally.

***NOTE**: This is under heavy development. In short, it is very incomplete. This is an idea that has been noodled for a while. Now this is being attempted for future project efficiency, to give back, to gauge interest within the open source community, and to explore potential business opportunities. The goal is to try different items to see what looks and feels right. The goal is to make a usable product and remove this banner. Perhaps this could be commercially viable as a subscription, with enterprise-scale multi-tenant support based on the open-source version.*

# Table of Contents

- [Architecture](#architecture)
  - [Authentication](#authentication)
  - [Projects](#projects)
  - [Prerequisites](#prerequisites)
- [Installation](#installation)
  - [1. Entra ID App Registration](#1-entra-id-app-registration)
  - [2. Infrastructure](#2-infrastructure)
  - [3. Azure DevOps Pipeline](#3-azure-devops-pipeline)
- [Local Development](#local-development)

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

# Installation

## 1. Entra ID App Registration

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

## 2. Infrastructure

The pipeline deploys infrastructure automatically on each release via `azure-resources.bicep`, including creating the resource group if it does not already exist. No manual infrastructure steps are required.

After the first release, add the SWA hostname to your Entra ID app registration redirect URIs.

## 3. Azure DevOps Pipeline

The pipeline is defined in `azure-pipelines.yml` at the repository root.

### Variable groups

Create the following variable groups in **Pipelines → Library**:

**`azdoboards-config`** (shared across all environments):

| Variable | Value |
|---|---|
| `a_AzureServiceConnection` | Name of your Azure service connection |
| `a_SubscriptionId` | Your Azure subscription ID |

**`azdoboards-config-dev`**, **`azdoboards-config-uat`**, **`azdoboards-config-prd`** (one per environment):

| Variable | Example Value | Notes |
|---|---|---|
| `a_ResourceGroup` | `azdoboardsdevrg` | Resource group to deploy into |
| `a_AzureLocationPrimary` | `eastus` | Azure region of the resource group |
| `a_SwaName` | `azdoboardsdevswa` | Name for the Static Web App resource |
| `a_SwaSkuName` | `Standard` | Optional; overrides the pipeline default |
| `a_IsStrDoRelease` | `true` | Set to `false` to skip deployment for that environment |
| `AzureAd.Authority` | `https://login.microsoftonline.com/<tenant-id>` | Injected into `appsettings.json` at deploy time |
| `AzureAd.ClientId` | `<client-id>` | Injected into `appsettings.json` at deploy time |
| `AzureDevOps.OrgUrl` | `https://dev.azure.com/<your-org>` | Injected into `appsettings.json` at deploy time |

> `AzureAd.*` and `AzureDevOps.*` are transform variables consumed by the `FileTransform` task. Their dot-notation names must exactly match the key paths in `appsettings.json` and cannot carry a variable prefix.

### Environments

Create the following environments in **Pipelines → Environments**, then configure approval gates and branch controls on each:

| Environment | Approval | Branch Control |
|---|---|---|
| `AzDoBoards DEV` | Optional | `release/*`, `feature/*/build/*` |
| `AzDoBoards UAT` | Required | `release/*` |
| `AzDoBoards PRD` | Required | `release/*` |

### Create the pipeline

In **Pipelines → New pipeline**, point to this repository and select `azure-pipelines.yml` at the root. The pipeline triggers automatically on `release/*` and `feature/*` branches.

#### Pipeline stages

| Stage | Triggers on | Condition |
|---|---|---|
| **Build** | `release/*`, `feature/*` | Always |
| **Release DEV** | `release/*`, `feature/*/build/*` | Build succeeded |
| **Release UAT** | `release/*` | Release DEV stage succeeded |
| **Release PRD** | `release/*` | Release UAT stage succeeded |

Each release stage deploys infrastructure first, then the application. Both steps are skipped when `a_IsStrDoRelease` is `false` for that environment — independently per environment.

# Local Development

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

2. This file is [.gitignore](./.gitignore) and will never be committed.

3. Launch via VS Code: press `F5` → **Launch AzDoBoards.Ui**

\~END~
