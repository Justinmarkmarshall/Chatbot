# AIPlatform

Chatbot now supports persistent, Google-account-owned chat tabs, editable titles,
and restored conversation history using PostgreSQL. See the
[persistent chat setup and API guide](docs/persistent-chat-sessions.md) for the
required database connection string and Helm Secret reference.

AIPlatform is a self-hosted .NET Web API that provides a wrapper around a locally hosted Large Language Model (LLM).

The initial implementation uses **Ollama** as the local inference runtime and **Qwen3 1.7B** as the language model.

## Architecture

```text
Client
  │
  │ POST /api/chat
  ▼
┌─────────────────────┐
│     AIPlatform      │
│   ASP.NET Core API  │
└──────────┬──────────┘
           │ HTTP
           │ localhost:11434
           ▼
┌─────────────────────┐
│       Ollama        │
│   Inference Runtime │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│    Qwen3 1.7B       │
│    Local LLM        │
└─────────────────────┘
```

The .NET application acts as a controlled wrapper around Ollama. Clients communicate with AIPlatform rather than communicating directly with the model.

This provides a foundation for adding features such as authentication, conversation history, streaming, RAG, tools and model switching later.

## Model

The initial model is:

```text
qwen3:1.7b
```

### Why Qwen3 1.7B?

The target private-cloud server has **8 GB RAM and no suitable GPU acceleration**, so Ollama performs inference using the CPU.

Several model sizes were considered and tested.

Qwen3 4B proved too large for the available resources and caused the server to become unresponsive when loading the model.

Qwen3 0.6B ran comfortably and consumed approximately 1 GB while loaded, but the smaller model provides more limited reasoning and response quality.

Qwen3 1.7B provided the best compromise between:

* Response quality
* Memory consumption
* CPU inference performance
* Available resources for the existing RKE2 workloads

During testing, Qwen3 1.7B consumed approximately **1.9 GB** while loaded and left approximately **2.3 GiB of system memory available** on the 8 GB private-cloud server.

For this reason, **Qwen3 1.7B is the initial production model for AIPlatform**.

The model can be changed later without changing the public AIPlatform API.

## Installing Ollama

Ollama provides the runtime used to load and execute the language model.

### Windows

Install Ollama using:

```powershell
irm https://ollama.com/install.ps1 | iex
```

Confirm the installation:

```powershell
ollama --version
```

Confirm that the local Ollama API is running:

```powershell
curl http://localhost:11434
```

A successful response should return:

```text
Ollama is running
```

Ollama exposes its local HTTP API on:

```text
http://localhost:11434
```

### Linux

Install Ollama using:

```bash
curl -fsSL https://ollama.com/install.sh | sh
```

Confirm the installation:

```bash
ollama --version
```

## Downloading and Running Qwen3 1.7B

Download and start the model with:

```bash
ollama run qwen3:1.7b
```

The first execution downloads the model before starting an interactive session.

Once downloaded, the same command can be used to start it again:

```bash
ollama run qwen3:1.7b
```

A prompt will then be available:

```text
>>> What is Kubernetes?
```

To exit the interactive Ollama session:

```text
/bye
```

## Checking Installed Models

List locally installed models:

```bash
ollama list
```

Qwen should appear as:

```text
qwen3:1.7b
```

## Checking Running Models

To see models currently loaded by Ollama:

```bash
ollama ps
```

This displays useful information including:

* Model name
* Memory size
* CPU/GPU usage
* Context size
* How long the model will remain loaded

For example, the private-cloud server currently runs Qwen3 1.7B using:

```text
PROCESSOR: 100% CPU
CONTEXT:   4096
```

An empty `ollama ps` does not necessarily mean Ollama itself has stopped. Ollama unloads models from memory after they have been idle.

## AIPlatform Configuration

AIPlatform connects to Ollama using configuration rather than hard-coded addresses.

For local development:

```json
{
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "qwen3:1.7b"
  }
}
```

This allows different Ollama endpoints or models to be configured when AIPlatform is deployed to RKE2.

## Running AIPlatform Locally

Start Ollama and ensure Qwen3 1.7B is available:

```powershell
ollama run qwen3:1.7b
```

Configure PostgreSQL using [the persistent chat setup](docs/persistent-chat-sessions.md), then start the .NET Web API:

```powershell
dotnet run
```

AIPlatform exposes:

```text
POST /api/chat
```

Example request:

```json
{
  "message": "what is kubernetes"
}
```

The request flow is:

```text
POST /api/chat
      │
      ▼
AIPlatform
      │
      ▼
Ollama HTTP API
      │
      ▼
Qwen3 1.7B
      │
      ▼
Generated response
      │
      ▼
AIPlatform response
```

The initial proof of concept successfully generates responses entirely through the locally hosted model without using an external commercial LLM API.

## Kubernetes Deployment

The Helm chart in `charts/chatbot` deploys the application, its internal Service, and a Gateway API `HTTPRoute`.

The target cluster must have the Gateway API `HTTPRoute` CRD installed and a Gateway matching the configured `httpRoute.parentRefs` value.

Install or upgrade the release from the repository root:

```bash
helm upgrade --install chatbot ./charts/chatbot --namespace chatbot --create-namespace --set authentication.existingSecret=chatbot-google --set database.existingSecret=chatbot-database
```

Set the container image and route configuration for the target environment through a values file or `--set` options. The Ollama service endpoint and model are configured through `ollama.baseUrl` and `ollama.model`.

## Next Steps

The initial proof of concept establishes the basic .NET → Ollama → Qwen integration.

Planned improvements include:

1. Persistent conversation history is implemented; validate it with your deployment's Google account and PostgreSQL configuration.
2. Investigate Retrieval-Augmented Generation (RAG) for private documentation.
3. Introduce controlled tools for interacting with private-cloud services.

## Google sign-in

The login page uses `Marshall.Authentication.Google` version `1.0.0`. Anonymous visitors are redirected to `/login`; the chat API and API documentation also require authentication. The interactive component and API use the same owner-scoped persistent chat service.

Configure Google OAuth credentials with user secrets from this directory:

```powershell
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_CLIENT_ID"
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_CLIENT_SECRET"
dotnet run --launch-profile https
```

Register `https://localhost:7035/signin-google` in your Google OAuth client's authorized redirect URIs. For production, register `https://YOUR_HOST/signin-google`. The package requires HTTPS for its cookies. `/login-callback` completes the application session; it is not the Google redirect URI.

The package supplies `/login-google`, `/login-callback` and `/logout`. Failed or cancelled sign-in returns to the login page with an error message. Any Google account accepted by your OAuth application can sign in; there is no account allowlist. Sessions use protected, nonpersistent browser cookies, with the package's seven-day sliding lifetime. Logout clears cookies; there is no database session store or central revocation. Interactive connections close when their authentication expires.

### Package restore and container builds

`nuget.config` routes this package to the owner's GitHub Packages feed. For a fresh restore, configure `NuGetPackageSourceCredentials_github` in your environment with the value `Username=YOUR_GITHUB_LOGIN;Password=YOUR_TOKEN;ValidAuthenticationTypes=Basic`. Use a token with permission to read the package; do not commit it.

The container workflow passes its GitHub token as a BuildKit secret. Grant this repository Actions access in the authentication package's settings. For a local container build, supply the same credentials through an environment variable:

```powershell
docker build --secret id=nuget_credentials,env=NuGetPackageSourceCredentials_github -t chatbot .
```

### Deployment credentials

Provision a Kubernetes Secret containing `client-id` and `client-secret`, then set `authentication.existingSecret` to its name in Helm values (or `--set authentication.existingSecret=chatbot-google`). Helm requires this setting. Credentials are read into `Authentication__Google__ClientId` and `Authentication__Google__ClientSecret`.

Retain the shared `/keys` volume so login cookies survive restarts. Local development can override `DataProtection:KeysPath` with a writable directory. The existing forwarded-header configuration trusts all proxies: keep the application reachable only through your trusted ingress, or configure explicit trusted proxies before exposing it directly.


### Local Deployment

$env:CHATBOT_POSTGRES_PASSWORD = 'the-password-used-for-your-local-database'

dotnet run --environment Development --no-launch-profile -- --document-worker 

docker compose -f compose.postgres.yaml up -d --wait

dotnet run --launch-profile https

### Kubernetes Deployment

Rancher Desktop



