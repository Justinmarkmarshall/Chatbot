# Chatbot

<<<<<<< HEAD
A self-hosted .NET 10 and Blazor application for persistent AI conversations and questions about your own documents. Sign in with Google, create named chat tabs, and upload PDF or text files to give each conversation its own searchable sources.

Chatbot uses **Ollama with Qwen3 1.7B** to generate answers, **all-MiniLM-L6-v2 FP32** to generate embeddings, and **PostgreSQL with pgvector** to store conversations, documents and searchable vectors. Inference runs locally; Google handles sign-in.

## What it does

- Creates user-owned chat tabs with editable titles and restored message history.
- Streams answers and saves partial responses when generation is cancelled or fails.
- Attaches PDF and UTF-8 text uploads to a specific chat, retaining the original files for download.
- Processes documents in a separate worker, with durable status, retry and recovery after interruption.
- Retrieves relevant passages only from the authenticated user's current chat.
- Supplies document evidence to Qwen3 and instructs it to cite sources and acknowledge insufficient evidence.
- Supports deleting chats, individual messages and uploaded documents. Document deletion removes the original and indexed chunks; historical reply source snapshots remain.

### General chat and document questions

With **no successfully uploaded documents**, a tab sends its conversation directly to Ollama for general chat.

Once documents are uploaded, answers use the document retrieval path. If none are ready or no usable chunks are available, Chatbot returns a message explaining that it has no processed documents to answer from. Retrieval failures do not fall back to general knowledge.

For document answers, conversation history provides conversational context, not additional factual evidence. Grounding is enforced through model instructions; it is not a guarantee that every generated claim is correct. The source panel shows the chunks supplied to the model, while citations in the answer identify the sources it claims to use.

## Architecture

```text
Browser -- Google sign-in --> Blazor / ASP.NET Core web application
                                      |
              +-----------------------+-----------------------+
              |                                               |
         Upload PDF/TXT                                  Ask a question
              |                                               |
     PostgreSQL original + queued status             Documents in this chat?
              |                                      /                  \
       Document worker                              No                  Yes
              |                                     |                    |
    PdfPig / UTF-8 extraction                     Ollama           MiniLM query vector
              |                                     |                    |
    Heading-aware text chunks                       |           pgvector exact search
              |                                     |             (user + chat scope)
       MiniLM embeddings                            |                    |
              |                                     |           Top 5 source passages
    PostgreSQL text + vectors                       |                    |
         document ready                             +-------- Qwen3 answer
                                                                  |
                                                    Saved reply + source snapshot
```

The web application and worker share PostgreSQL; they do not call each other directly. Ollama is a separate service.

### How document retrieval works

1. The web app saves an original upload in PostgreSQL and queues it for processing.
2. The worker extracts text, preserves PDF page provenance and splits sections around headings. PDF heading detection uses a font-size heuristic; scanned PDFs without extractable text are not OCR-processed.
3. Each chunk is capped at **254 content tokens**, leaving two special tokens within MiniLM's 256-token limit. Oversized sections are subdivided with a **50-token overlap**, rather than silently truncated.
4. ONNX Runtime runs MiniLM locally. The matching tokenizer, mean pooling over non-padding tokens and L2 normalisation produce a **384-dimensional vector** per chunk.
5. The worker atomically publishes the extracted text, chunks, vectors and ready status.
6. For a document question, the web app uses the same model to embed the question. A database query filters by owner, chat, ready status and model profile, then returns the **top five** chunks using exact cosine distance.
7. Qwen3 receives the retrieved text, question and conversation history. It is instructed to use only the supplied document evidence for factual answers and to identify its sources.

There is no approximate vector index, similarity confidence threshold or Ollama embedding model. Nearest-neighbour search can return irrelevant passages; similarity is not confidence that a question is answerable. Uploading documents does not train or modify MiniLM or Qwen3.

## Run locally

The commands below use **PowerShell 7** from the directory containing `Chatbot.csproj`, `compose.postgres.yaml` and `charts/` (for example, `C:\Dev\AI\Chatbot\Chatbot`).

### Prerequisites

- .NET 10 SDK.
- Docker with Compose, such as Rancher Desktop using the Moby engine.
- Ollama with `qwen3:1.7b` available.
- A Google OAuth web client.
- Read access to the `Marshall.Authentication.Google` NuGet package on GitHub Packages.
- Python 3 for the one-time model preparation command below. The web and worker runtimes are entirely .NET and do not require Python.

### 1. Prepare Ollama

Install [Ollama](https://ollama.com/download), start it, then download the model:

```powershell
ollama pull qwen3:1.7b
ollama list
Invoke-RestMethod http://localhost:11434/api/tags
```

The last command should return the installed models. If you manage the server manually, run `ollama serve` in a separate terminal. Do not start a second server if the desktop application is already serving.

### 2. Configure private package restore

`nuget.config` routes `Marshall.Authentication.Google` to GitHub Packages. For a fresh restore, set credentials in the current terminal using a classic PAT with `read:packages` and access to the package:

```powershell
$packageUser = Read-Host 'GitHub username'
$packageToken = Read-Host 'GitHub package-read token' -MaskInput
$env:NuGetPackageSourceCredentials_github = "Username=$packageUser;Password=$packageToken;ValidAuthenticationTypes=Basic"
Remove-Variable packageToken

dotnet restore Chatbot.csproj
```

Do not commit credentials. For Actions access and the optional CI token fallback, see [package authentication troubleshooting](tests/README.md#troubleshooting-github-packages-403).

### 3. Start PostgreSQL and configure the connection

```powershell
$env:CHATBOT_POSTGRES_PASSWORD = Read-Host 'Local PostgreSQL password' -MaskInput
docker compose -f compose.postgres.yaml up -d --wait

dotnet user-secrets set 'Database:Host' 'localhost'
dotnet user-secrets set 'Database:Port' '5434'
dotnet user-secrets set 'Database:Name' 'chatbot'
dotnet user-secrets set 'Database:Username' 'chatbot'
dotnet user-secrets set 'Database:Password' "$env:CHATBOT_POSTGRES_PASSWORD"
```

Compose runs PostgreSQL 17 with pgvector on `localhost:5434` and keeps database files in a named volume. On an existing volume, enter its existing password: changing the environment variable does not rotate the database password.

An existing `ConnectionStrings:Chatbot` setting takes precedence over these individual fields. Update it or remove that specific setting with `dotnet user-secrets remove 'ConnectionStrings:Chatbot'` if you intend to use the fields above. Both web and worker apply versioned migrations at startup, protected by a PostgreSQL advisory lock; no separate migration command is required.

### 4. Configure Google sign-in and application settings

Register this exact **authorized redirect URI** in the Google OAuth client:

```text
https://localhost:7035/signin-google
```

Then configure the app:

```powershell
$googleClientId = Read-Host 'Google OAuth client ID'
$googleClientSecret = Read-Host 'Google OAuth client secret' -MaskInput
dotnet user-secrets set 'Authentication:Google:ClientId' "$googleClientId"
dotnet user-secrets set 'Authentication:Google:ClientSecret' "$googleClientSecret"
Remove-Variable googleClientSecret

dotnet user-secrets set 'Ollama:BaseUrl' 'http://localhost:11434'
dotnet user-secrets set 'Ollama:Model' 'qwen3:1.7b'

$keyPath = Join-Path $PWD.Path 'obj/local-keys'
New-Item -ItemType Directory -Force -Path $keyPath | Out-Null
dotnet user-secrets set 'DataProtection:KeysPath' "$keyPath"
dotnet dev-certs https --trust
```

`/signin-google` is Google's callback; `/login-callback` is a separate application-session endpoint supplied by the authentication package. Use the HTTPS launch profile below so the browser URL and registered callback agree. Local key storage under `obj` is convenient for development; deleting it invalidates existing login cookies.

### 5. Prepare MiniLM and build

```powershell
python build/fetch-minilm.py Processing/minilm.json model-assets
if ($LASTEXITCODE -ne 0) { throw 'Model preparation failed' }

$modelRoot = (Resolve-Path model-assets).Path
dotnet user-secrets set 'Documents:ModelRoot' "$modelRoot"
dotnet build Chatbot.csproj --no-restore
```

The preparation script downloads the pinned model revision and verifies SHA-256 hashes. `Documents:ModelRoot` points to the directory containing `minilm/`, not to `model.onnx` itself. The application also verifies the artifacts when loading them. Model files are excluded from Git and the web project's SDK content globs.

### 6. Start the web app and worker in separate terminals

From the project directory, start the web application:

```powershell
dotnet run --no-build --launch-profile https
```

In a second terminal, from the same directory, start the document worker:

```powershell
dotnet run --no-build --launch-profile document-worker -- --document-worker
```

Both profiles use Development configuration and the project's user secrets. Build before starting the processes; stop them before rebuilding if Windows reports locked output files.

Open **https://localhost:7035**, sign in, create a chat and upload a small PDF or TXT file. Use **Refresh documents** to inspect processing status. Once it is `ready`, ask a question about its contents. If uploads stay queued, check that the worker is running and can access PostgreSQL and the model directory.

## Containers and Kubernetes

The Dockerfile builds one image for both workloads. It prepares and verifies MiniLM in a build-only stage, then copies the model into `/model-assets`. The final image runs as UID/GID 1654; it contains no Python runtime and performs no model download at startup.

With the package credentials from local setup available:

```powershell
docker build --secret id=nuget_credentials,env=NuGetPackageSourceCredentials_github -t chatbot:local .
```

The web process is the default entry point. The same image with `--document-worker` runs the worker. Container configuration comes from environment variables and Secrets; local `appsettings*.json` files are excluded from the Docker context.

| Helm chart | Deploys |
| --- | --- |
| `charts/chatbot-postgres` | A separate PostgreSQL/pgvector StatefulSet, internal Services and persistent database storage |
| `charts/chatbot` | The web app, enabled-by-default document worker, web Service, login-key PVC and optional HTTPRoute |

Install the database release before the application release. The charts reference existing Secrets instead of storing credentials in values. Ollama must already be available separately. The database chart requires Kubernetes 1.32 or newer.

Use the appropriate complete guide:

- [Rancher Desktop](docs/rancher-desktop.md): local image, Secrets, host Ollama, chart installation and port forwarding.
- [RKE2 deployment](docs/rke2-deployment.md): separate releases, storage, Secrets, existing Ollama, Envoy Gateway and Cloudflare Tunnel.
- [Envoy HTTPS forwarding](docs/rke2-deployment.md#envoy-preserve-https-for-google-sign-in-behind-cloudflare): the web setting `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` and Gateway proxy trust must preserve the external HTTPS scheme for Google callbacks. Do not register an HTTP callback for the public hostname as a workaround.

Use an immutable image tag for server upgrades. Web and worker changes roll out together through the app chart; database storage belongs to the independent PostgreSQL release.

## Persistence and ownership

PostgreSQL stores chat sessions, messages, original uploads (`bytea`), extracted text, processing status, chunks and vectors. Kubernetes persists these database files through the PostgreSQL PVC/PV. PDFs are database values, not loose files in the web container.

Ownership uses Google's stable `NameIdentifier` subject claim. Chat and document access is checked against that subject; retrieval filters by owner and chat inside SQL. Any Google account accepted by the configured OAuth application can sign in: there is no application account allowlist.

The web app keeps ASP.NET Data Protection keys on a separate persistent volume so login cookies remain readable after pod restarts. Signing out does not delete chat history. Database administrators remain trusted. Persistent volumes are not backups; preserve database backups separately from the server's disks.

The current forwarded-header configuration trusts proxies broadly. Keep the web app behind the intended trusted ingress path. Live inference is local, but sign-in uses Google, and initial package/model/image preparation requires network access.

## Limits and behaviour

| Area | Current limit or behaviour |
| --- | --- |
| Upload formats | PDF with extractable text; valid UTF-8 TXT |
| Original file size | 10 MiB per upload |
| Account quota | 100 MiB reserved/stored originals and at most 100 upload records; in-progress uploads reserve 10 MiB each |
| PDF processing | At most 100 pages; no OCR |
| Extracted text | At most 200,000 .NET string characters |
| Chunks | At most 512 per document; 254 content tokens each |
| Document question | At most 254 MiniLM content tokens; overlength questions are rejected |
| Chat input | At most 16,000 characters; titles at most 120 |
| Conversation context | Latest ten complete turns; failed, interrupted and incomplete turns are excluded |
| Retrieval | Exact cosine search, top five chunks, current owner/chat only |
| Generation | One active reply per chat; five-minute request deadline |
| Worker | Serial processing with a database lock and fenced publication |

Deleting a document removes its original and indexed chunks. Previous messages and their source snapshots remain; old download links for that document no longer work. Deleting an individual message leaves the other message visible but excludes that incomplete turn from future model history.

## API

The UI uses the same services as the authenticated HTTP API. API clients must retain the Google authentication cookie. Before a mutation, call `GET /api/chat/antiforgery`, retain its cookie, and send the returned `token` in the `RequestVerificationToken` header.

| Method | Route | Purpose |
| --- | --- | --- |
| GET / POST | `/api/chat/sessions` | List owned chats / create a chat |
| GET / PATCH / DELETE | `/api/chat/sessions/{id}` | Read / rename / delete a chat |
| POST | `/api/chat/sessions/{id}/messages` | Send `{ "message": "..." }`; receive streamed text |
| DELETE | `/api/chat/sessions/{sessionId}/messages/{messageId}` | Delete one message |
| GET / POST | `/api/chat/sessions/{sessionId}/documents` | List documents / upload a multipart `file` |
| GET / DELETE | `/api/chat/sessions/{sessionId}/documents/{id}` | Read status / delete a document |
| GET | `/api/chat/sessions/{sessionId}/documents/{id}/original` | Download the original |
| POST | `/api/chat/sessions/{sessionId}/documents/{id}/retry` | Retry failed processing |

The original `POST /api/chat` route remains supported. Omitting `sessionId` creates a new chat; reuse the returned `X-Chat-Session-Id` in subsequent request bodies to continue it. API documentation also requires sign-in. Reload saved history after a failed stream before retrying, as partial responses and the user message may already be stored.

## Tests and CI

Run the focused unit suite:

```powershell
./tests/run-unit-tests.ps1
```

After package restore, unit tests need no database, Docker, Ollama or model weights. They cover chunking, document processing, prompts, upload validation, streaming and chat orchestration.

The Linux integration suite uses a disposable PostgreSQL container, real MiniLM inference and pgvector. It expects model artifacts in the experiment fixture location:

```powershell
python build/fetch-minilm.py Processing/minilm.json experiments/embedding-spike/models
dotnet publish tests/Chatbot.IntegrationTests/Chatbot.IntegrationTests.csproj -c Release -p:OutputPath=obj/integration-build/ -o tests/linux-publish
./tests/run-linux-integration.ps1
```

These checks include ownership isolation, persistence, migrations, processing recovery, deletion, real-tokenizer parity and retrieval. Most generation tests use a deterministic Ollama fake; passing them does not prove live model answer quality or Google sign-in.

The GitHub workflow runs unit tests, Linux integration checks and Helm validation before allowing image publishing on pushes to `main`. Configure **Tests and Helm validation** as a required repository status check to enforce it before merging.

See [test commands and CI troubleshooting](tests/README.md) and [recorded results](tests/RESULTS.md). Historical experiments live under `experiments/`; experiment and test sources are excluded from the production web build and publish output.

## Model choice and project history

Qwen3 1.7B was selected for an 8 GB CPU-only homelab server. Earlier testing found Qwen3 4B too demanding, while 0.6B offered lower answer quality. In that historical workload, 1.7B used approximately 1.9 GB while loaded and left around 2.3 GiB available on the server. These observations are not current capacity guarantees, especially with PostgreSQL and separate web/worker MiniLM instances now running.

The model and Ollama endpoint are configurable. MiniLM embeddings are pinned to a specific model profile; replacing that embedding model requires compatible document reprocessing, not merely changing the chat model setting.

Earlier design and milestone notes remain under `docs/` for context. Use the setup and deployment guides linked above for the current application.

### Local Deployment Quick Start

$env:CHATBOT_POSTGRES_PASSWORD = 'the-password-used-for-your-local-database'

dotnet run --environment Development --no-launch-profile -- --document-worker 

docker compose -f compose.postgres.yaml up -d --wait

dotnet run --launch-profile https
=======
```text
                         ┌──────────────────────┐
                         │  Chatbot web process  │
                         │ .NET 10 / Blazor/API  │
                         └───────┬───────┬──────┘
                                 │       │
                  chat/retrieval │       │ generation
                                 │       │
                                 v       v
                  ┌──────────────────┐  ┌──────────────┐
                  │ PostgreSQL +     │  │ Ollama       │
                  │ pgvector         │  │ qwen3:1.7b   │
                  │ chats, originals │  └──────────────┘
                  │ chunks, vectors  │
                  └────────^─────────┘
                           │
                           │ claims queued documents
                           │
                  ┌────────┴─────────┐
                  │ Document Worker  │
                  │ same application │
                  │ image; separate  │
                  │ process          │
                  └──────────────────┘
```

The web process and Document Worker run the same container image. The worker is
started with `--document-worker`; it has no public HTTP surface. PostgreSQL is
the durable store for chat state, original uploads, document processing state,
and vector chunks. Ollama is a separate workload or service addressed through
configuration.

### Document RAG pipeline

1. An authenticated user uploads a PDF or UTF-8 `.txt` file to a chat. The
   original file and metadata are persisted in PostgreSQL, then the document is
   marked queued.
2. The Document Worker serially claims queued work from PostgreSQL. It extracts
   text from PDFs, preserves page information where available, detects PDF
   headings heuristically, and splits text into token-aware windows of at most
   254 MiniLM content tokens with a 50-token overlap.
3. The worker generates a MiniLM embedding for every chunk and stores the chunk
   text, source metadata, and 384-dimensional vector in `document_chunks`.
   Startup migrations enable the PostgreSQL `vector` extension.
4. For a document-backed chat question, the web process embeds the question with
   the same MiniLM model and performs an exact cosine-distance pgvector query.
   The five nearest chunks from ready documents in that chat are supplied to
   Ollama as quoted context.
5. The document prompt instructs the model to use only retrieved evidence and to
   cite the returned source numbers. If no document is ready, Chatbot reports
   that instead of sending the question to Ollama without document context.

Documents are scoped to their owning chat and authenticated Google subject.
Deleting a document removes its original and cascades to its indexed chunks and
vectors. Historical reply source snapshots remain part of the conversation
history, but deleted originals are no longer downloadable.

## Model assets

The image build downloads the pinned, hash-verified MiniLM files and copies them
to `/model-assets`. Both the web process and worker load those local assets; pods
do not download the embedding model at startup. The model manifest is
[`Processing/minilm.json`](Processing/minilm.json).

`qwen3:1.7b` is the default Ollama model in
[`appsettings.json`](appsettings.json) and the Helm values. It can be changed by
setting `Ollama__Model` or `ollama.model`, provided the configured Ollama service
has that model available.

## Prerequisites

- .NET SDK 10
- Docker Compose for the local PostgreSQL service
- Ollama, with the configured chat model available
- Google OAuth credentials for interactive sign-in
- Access to the GitHub Packages feed used by
  `Marshall.Authentication.Google` when restoring or building locally

The supplied Compose service uses `pgvector/pgvector`, so it is suitable for the
document pipeline as well as persistent chats.

## Local development

1. Configure the package-feed credentials required by `nuget.config`, then
   restore the project:

   ```powershell
   dotnet restore
   ```

2. Start a local pgvector-enabled PostgreSQL instance. Choose a local password;
   do not place it in configuration tracked by Git.

   ```powershell
   $env:CHATBOT_POSTGRES_PASSWORD = 'YOUR_LOCAL_DATABASE_PASSWORD'
   docker compose -f compose.postgres.yaml up -d --wait
   ```

3. Store the PostgreSQL connection string and Google OAuth credentials in user
   secrets. The documented port is the Compose service's host port.

   ```powershell
   dotnet user-secrets set 'ConnectionStrings:Chatbot' 'Host=localhost;Port=5434;Database=chatbot;Username=chatbot;Password=YOUR_LOCAL_DATABASE_PASSWORD'
   dotnet user-secrets set 'Authentication:Google:ClientId' 'YOUR_CLIENT_ID'
   dotnet user-secrets set 'Authentication:Google:ClientSecret' 'YOUR_CLIENT_SECRET'
   ```

   Register `https://localhost:7035/signin-google` as an authorized redirect URI
   in the Google OAuth client. Production deployments need their own HTTPS
   redirect URI.

4. Make the configured model available to Ollama and start the web process:

   ```powershell
   ollama pull qwen3:1.7b
   dotnet run --launch-profile https
   ```

5. In another terminal, start the worker:

   ```powershell
   dotnet run --environment Development --no-launch-profile -- --document-worker
   ```

The web process and worker both initialize the schema safely under a PostgreSQL
advisory lock. Keep the worker running to turn newly uploaded documents into
searchable chunks. To use a non-local Ollama instance, set `Ollama__BaseUrl`;
the default is `http://localhost:11434`.

## Kubernetes deployment

The repository contains two Helm charts:

| Chart | Responsibility |
| --- | --- |
| `charts/chatbot-postgres` | A pgvector PostgreSQL StatefulSet, ClusterIP services, and a retained data PVC. |
| `charts/chatbot` | The web deployment, a separate Document Worker deployment, internal Service, HTTPRoute, and Data Protection PVC. |

Provision PostgreSQL separately, with a Kubernetes Secret containing a `password`
key. The PostgreSQL chart's default persistent volume request is 10 GiB and its
claim is retained when the StatefulSet is deleted or scaled down:

```bash
helm upgrade --install chatbot-postgres ./charts/chatbot-postgres \
  --namespace chatbot --create-namespace \
  --set database.existingSecret=chatbot-postgres-auth
```

Deploy Chatbot with Google OAuth and database credentials supplied by existing
Secrets. `database.existingSecret` should contain a complete connection string
under `connection-string`; alternatively, configure the separate database
password Secret and the database host/name/user values in a deployment-specific
values file.

```bash
helm upgrade --install chatbot ./charts/chatbot \
  --namespace chatbot --create-namespace \
  --set authentication.existingSecret=chatbot-google-auth \
  --set database.existingSecret=chatbot-database
```

The application chart expects an existing Ollama endpoint at
`ollama.baseUrl` by default. Its in-chart Ollama workload is deliberately
disabled and Helm rejects enabling it, so deploy or manage Ollama independently
and ensure the selected `ollama.model` is present there. The chart's web and
worker deployments use the same application image, including the MiniLM assets.

The application is exposed through a Gateway API `HTTPRoute`; configure
`httpRoute.parentRefs` and `httpRoute.hostnames` for the target cluster. Keep the
Data Protection PVC enabled so authentication cookies survive web-pod restarts.
Do not put OAuth, PostgreSQL, or package-feed credentials in Helm values.

## Configuration

| Setting | Purpose |
| --- | --- |
| `ConnectionStrings:Chatbot` | Complete PostgreSQL connection string. |
| `Ollama:BaseUrl` | Ollama HTTP endpoint; defaults to `http://localhost:11434`. |
| `Ollama:Model` | Chat model name; defaults to `qwen3:1.7b`. |
| `Documents:ModelRoot` | Directory containing the packaged `minilm` model; the image default is `/model-assets`. |
| `Authentication:Google:ClientId` / `ClientSecret` | Google OAuth credentials. |

## Validation

Run the focused repository checks after making changes:

```powershell
dotnet test tests/Chatbot.UnitTests/Chatbot.UnitTests.csproj
helm lint charts/chatbot
helm lint charts/chatbot-postgres
```

The integration harness and Helm manifest checks used in CI are available in
[`tests/run-integration.ps1`](tests/run-integration.ps1) and
[`tests/check-helm.py`](tests/check-helm.py).
>>>>>>> aa50e77dff82a75882be224c669ef596799b29c6
