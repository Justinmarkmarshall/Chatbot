# Chatbot

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
