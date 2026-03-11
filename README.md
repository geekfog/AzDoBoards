# About AzDo Boards

## Overview

<img src="Images/favicon-0512.png" alt="AzDo Boards Icon" style="zoom:25%; float:right;" />AzDo Boards (pronounced "As Doe Boards") is intended to assist with project management by leveraging work item data in Azure DevOps Boards. Running externally allows freedom to leverage external technologies for project management-focused capabilities, including reporting, and to quickly add or update information. There is an eye to incorporate AI to help build out projects.

## ⚠️ Warning

***NOTE**: This is under heavy development. In short, it is very incomplete. This is an idea that has been noodled for a while. Now this is being attempted to improve future project efficiency, give back, gauge interest within the open-source community, and explore potential business opportunities. The goal is to try different items to see what looks and feels right. The goal is to make a usable product and remove this banner. Perhaps this could be commercially viable as a subscription, with enterprise-scale multi-tenant support based on the open-source version.* 

## Purpose

The purpose of the application is to integrate with Azure DevOps, especially Boards, and to be extremely performant, working with data in real time. It will use Azure for Authentication, as that is used with Azure DevOps and Entra ID integration. It will assist with project management to successful completion, extending what isn't available in Azure DevOps. This includes:

- High-level planning of an initiative to senior leaders that may cross multiple teams, creating one or more projects within each team
- Provide high-level timelines and resource requirements
- Assist with labor budgeting of the initiative, allowing aggregation by resource type (e.g., contractors vs employees, or different types of contractors, or department of team members, etc.)
- Assist with determining if the project is on track at various work item levels, including a % ranking of how on track (100% means completely on track, 0% means completely off-track)
- Provide easier (quick!) manual project building from the initiative down to the detailed tasks, including or shimming out as needed, and returning to fill in the details. In Azure DevOps Boards, this is painful because it requires many clicks to build a hierarchy of work items, even for small projects.
- Allow quick duplication of work items, easily moving items into different parent work items (with undo capabilities for the changing of mind or mistakes)
- Assist SQA working with developers, based on risk assessment
- Interaction by the user must be performant (UI-based interaction must be sub-second; UI response must be sub-3 seconds)

This supports a custom-defined work item hierarchy based on work item types (e.g., Initiative → Epic → Feature → User Story / Bug / Research → Tasks). This helps with reporting, tracking, and querying within Azure DevOps and externally. The UI is focused on hierarchical and line-based data entry, with minimal clicks, allowing maximum keyboard interaction, while still supporting various devices (from mobile to desktop) with mouse and tap interactions.

Possible roles (one person may do multiple roles or multiple people do a single role, etc.) that are expected:

- Software Engineer Developer (DEV)
- Software Quality Assurance (SQA)
- End-User Tester (EUT)
- Business Analyst (BA)
- Product Owner (PO)
- Project Manager (PM)
- Internal Stakeholder (IS)

| SDLC Stage ↓                        | Activity ↓ / Role →                         | PO ↓ | BA ↓ | PM ↓ | IS ↓ | DEV ↓ | SQA ↓ | EUT ↓ |
| ----------------------------------- | ------------------------------------------- | :--: | :--: | :--: | :--: | :---: | :---: | :---: |
| Discovery / Inception               | 1. Product vision & strategy                |  ✓   |      |      |  ✓   |       |       |       |
|                                     | 2. Business discovery & stakeholder needs   |  ✓   |  ✓   |      |  ✓   |       |       |       |
| Requirements & Analysis             | 3. Requirements elicitation & analysis      |      |  ✓   |      |  ✓   |       |       |       |
|                                     | 4. Requirements validation & sign‑off       |  ✓   |  ✓   |      |  ✓   |       |       |       |
| Planning                            | 5. Resource planning & allocation           |      |      |  ✓   |      |       |       |       |
|                                     | 6. Scheduling, budgeting & risk planning    |      |      |  ✓   |      |       |       |       |
|                                     | 7. Backlog prioritization                   |  ✓   |  ✓   |      |      |       |       |       |
| Design                              | 8. Solution & system design                 |      |  ✓   |      |      |   ✓   |   ✓   |       |
| Implementation                      | 9. Development (coding)                     |      |      |      |      |   ✓   |       |       |
|                                     | 10. Unit & integration testing              |      |      |      |      |   ✓   |       |       |
| Verification & Validation           | 11. System & regression testing             |      |      |      |      |       |   ✓   |       |
|                                     | 12. Non‑functional testing (perf, security) |      |      |      |      |       |   ✓   |       |
| Governance / Control                | 13. Progress tracking & status reporting    |      |      |  ✓   |      |       |       |       |
|                                     | 14. Risk & impediment management            |      |      |  ✓   |      |       |       |       |
|                                     | 15. UAT scenario & criteria design          |  ✓   |  ✓   |      |  ✓   |       |   ✓   |   ✓   |
|                                     | 16. UAT execution                           |      |      |      |  ✓   |       |   ✓   |   ✓   |
| Release & Deployment                | 17. Go/No‑Go decision                       |  ✓   |      |      |  ✓   |       |   ✓   |   ✓   |
|                                     | 18. Release coordination & deployment       |      |      |  ✓   |      |   ✓   |       |       |
| Operations & Continuous Improvement | 19. Post‑release validation                 |  ✓   |      |      |  ✓   |       |   ✓   |   ✓   |
|                                     | 20. Operations, support & improvements      |  ✓   |  ✓   |      |  ✓   |   ✓   |   ✓   |   ✓   |



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
- [Running](#running)
  - [Visual Studio Code](#visual-studio-code)

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
- An Azure DevOps organization
- An Entra ID (Azure AD) tenant
- (Included) [MudBlazor Library](https://www.mudblazor.com/)
- (Recommended) One of the following development IDEs:
  - Visual Studio Code (macOS, Linux, or Windows) 
    - C# Dev Kit extension
    - (Optional) C# extension

  - Visual Studio 2026 IDEs (Windows only)
- (Supported) An Azure subscription for hosting the application via [azure-pipelines.yml](./azure-pipelines.yml)

Future Expected Design:

- SQL Server (local, Azure, or in a Docker container) for application-specific settings storage
- (Optional) Azure Data Studio (macOS, Linux, or Windows) or SSMS (Windows)

# Installation

## 1. Entra ID App Registration

1. Go to **Azure Portal → Entra ID → App registrations → New registration**
2. Name: `AzDoBoards` (or your preference)
3. Supported account types: choose based on your audience
4. Redirect URI: **Single-page application (SPA)**
   - Local dev: `https://localhost:7060/authentication/login-callback`
   - Production: `https://<your-swa-hostname>/authentication/login-callback`
5. After creation, go to **API permissions → Add a permission → Microsoft APIs → Microsoft Graph**
   - Select **Delegated permissions**
   - Add: `email`, `openid`, `profile`, `User.Read`
6. Still on **API permissions → Add a permission → APIs my organization uses**
   - Search for **Azure DevOps**
   - Select **Delegated** → `user_impersonation`
   - Click **Grant admin consent** for all permissions
7. Note your **Tenant ID** and **Client ID** from the Overview page

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
| `a_AzureServiceConnection` | Name of your Azure service connection. |
| `a_IsDebug` | Whether to include debug information in the pipeline (must be set to `true` to show). |
| `a_ReleaseEnvPipeList` | Pipe-delimited list of environments to release to (e.g., `DEV|UAT|PRD`, `DEV|PRD`, `PRD`) — must be uppercase. This is deliberately centralized to avoid approvals for an unreleased environment. |
| `a_SubscriptionId` | Your Azure subscription ID |

**`azdoboards-config-dev`**, **`azdoboards-config-uat`**, **`azdoboards-config-prd`** (one per environment):

| Variable | Purpose | Example Value |
|---|---|---|
| `a_AzureDevOpsOrgUrl` | Substituted into `appsettings.json` by the `FileTransform` task | `https://dev.azure.com/<your-org>` |
| `a_AzureLocationPrimary` | Azure region of the resource group | `eastus` |
| `a_ResourceGroup` | Resource group to deploy into | `azdoboardsdevrg` |
| `a_SwaName` | Name for the Static Web App resource | `azdoboardsdevswa` |
| `a_SwaSkuName` | Optional; overrides the pipeline default | `Standard` |
| `AzureAd.Authority` | Substituted into `appsettings.json` by the `FileTransform` task | `https://login.microsoftonline.com/<tenant-id>` |
| `AzureAd.ClientId` | Substituted into `appsettings.json` by the `FileTransform` task | `<client-id>` |

> **Why `AzureAd.*` variables cannot use the `a_` prefix:**
> Microsoft documentation and tooling expects the MSAL configuration section to be named `AzureAd`. `a_AzureDevOpsOrgUrl` is project-defined and follows the convention — its flat structure means no nesting in `appsettings.json`.

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

Each release stage deploys infrastructure first, then the application. Only environments listed in `a_ReleaseEnvPipeList` will run — the stage is skipped entirely before any approval gate fires.

## 4. Custom Domain

Azure Static Web Apps supports free, automatically managed SSL/TLS certificates for custom domains. To configure one after the initial deployment:

1. Run the pipeline at least once to create the SWA resource and obtain its auto-generated hostname (e.g. `lively-ocean-abc123.4.azurestaticapps.net`). You can find it in the Azure Portal under the SWA resource → **Overview**.

2. At your DNS provider, create a **CNAME record** pointing your desired subdomain to the SWA hostname:

   | Type | Name | Value (Example) |
   |---|---|---|
   | CNAME | `app` _(or your subdomain)_ | `lively-ocean-abc123.4.azurestaticapps.net` |

   > **Apex/root domains** (e.g. `yourdomain.com` without a subdomain) require an ALIAS or ANAME record instead of CNAME. Not all DNS providers support this — check your provider's documentation.

3. In the **Azure Portal**, navigate to the SWA resource → **Custom domains → Add** and enter your domain. Azure will validate the CNAME and automatically provision and renew the SSL certificate.

4. Update your **Entra ID App Registration** redirect URIs to include the new domain:
   - `https://app.yourdomain.com/authentication/login-callback`

# Local Development

1. Fill in your real values in [appsettings.Secrets.json](AzDoBoards.UI/wwwroot/appsettings.Secrets.json):

```json
{
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/<your-tenant-id>",
    "ClientId": "<your-client-id>",
    "ValidateAuthority": true
  },
  "a_AzureDevOpsOrgUrl": "https://dev.azure.com/<your-org>"
}
```

2. This file is [.gitignore](./.gitignore) and will never be committed.

# Running

## Visual Studio Code

1. Open the **Run and Debug** panel (`Cmd+Shift+D` on macOS, `Ctrl+Shift+D` on Windows/Linux) or press `F5`.
2. When prompted to select a debugger, choose **C#**.
3. Select **AzDoBoards.Ui (HTTPS)** as the launch configuration.

The C# Dev Kit extension manages launch configurations internally — no `launch.json` is required.

\~END~
