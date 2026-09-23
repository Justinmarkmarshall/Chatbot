# Persistent chat sessions

Chat tabs, titles and messages now persist in PostgreSQL. Ownership uses the
authenticated Google `NameIdentifier`, never email, display name or a client-supplied
owner. Signing in again as the same account restores its sessions. Signing out
clears the authentication cookie, not saved chats. Previously transient conversations
cannot be recovered from earlier versions.

## Local setup

Use a dedicated application database; none of the experiment databases is reused.
The development Compose file runs PostgreSQL 17 without pgvector:

```powershell
$env:CHATBOT_POSTGRES_PASSWORD = 'YOUR_LOCAL_DATABASE_PASSWORD'
docker compose -f compose.postgres.yaml up -d --wait
dotnet user-secrets set 'ConnectionStrings:Chatbot' 'Host=localhost;Port=5434;Database=chatbot;Username=chatbot;Password=YOUR_LOCAL_DATABASE_PASSWORD'
dotnet run --launch-profile https
```

Keep your existing Google credentials and Ollama configuration. PostgreSQL binds
only to localhost. Its named volume survives container recreation; deleting that
volume deletes chat history. Changing the password environment variable does not
rotate a password in an already initialized database.

The connection setting can alternatively come from `ConnectionStrings__Chatbot`.
Missing configuration or an unavailable database fails startup. There is no
in-memory fallback or committed application database password.

## User experience

- **New chat** creates a saved tab called New chat. **Save title** renames it.
- Select a tab to restore its messages. `/chat/{id}` bookmarks an owned chat;
  the root page opens the most recently updated session. An ID is not authorization.
- Replies stream as before. **Cancel** saves the partial response with its status.
  Failed and interrupted responses are labelled rather than presented as complete.
- Cancel a running reply before switching tabs. Other browser windows can read
  the same chat, but simultaneous sends to one chat are rejected. **Refresh**
  reloads work performed in another browser.
- Saved messages and titles render as escaped text, not raw HTML.

## Storage and generation

The only new application dependency is Npgsql 10.0.3. Startup runs embedded,
versioned SQL migrations under a database transaction advisory lock. Version 1
creates `chat_sessions`, `chat_messages`, and ordinary relational indexes.
`chatbot_schema_migrations` records the applied version. Repeated startup preserves
data; a newer unsupported schema is rejected. The database role needs DDL permission
for migrations as well as normal data access.

Sessions have a UUID, owner subject, title, UTC timestamps and active-turn fencing
identifier. Messages have a UUID, session FK, turn ID, server-assigned sequence,
role, content, status and UTC creation timestamp. Sequence ordering avoids timestamp
ties. Titles must contain 1-120 trimmed characters; prompts are limited to 16,000
characters. All operations are scoped by the authenticated principal in the shared
chat service. Foreign and nonexistent session IDs are indistinguishable (404).
Ownership is enforced by the application; database administrators remain trusted.

A nonblocking transaction advisory lock serializes generation for each session
across app instances. The user message and assistant placeholder are committed
together before contacting Ollama. Message commits use a separate connection so
they survive process loss. The active-turn identifier fences out stale writers
after a lock connection is lost. This deliberately simple small-homelab design
holds one idle transaction/connection per generation plus a connection for writes;
do not configure a pool of one connection. It is not a high-concurrency architecture.

Replies checkpoint when a chunk arrives after roughly a second, or after 1,024
additional characters, and at completion/cancellation/failure. Final persistence
uses an independent ten-second timeout. A hard crash can lose text since the last
checkpoint. On the next read/send, a streaming reply with no active generation
lock is marked interrupted. Another replica's live generation is not repaired.
No background worker is introduced. Database failures surface as errors rather
than claims that a response was saved successfully.

Ollama receives the ten latest completed turns plus the new prompt, in original
order. Cancelled, failed, interrupted and partial turns remain visible in stored
history but are omitted from model context. All stored history is retained; this
context window does not delete it. There is no token summarizer, and the model's
context limit still applies. Premature EOF or an Ollama error fails the reply.
Generation also has a five-minute total deadline, including streamed-body reads,
so a stalled upstream cannot hold the session lock indefinitely.

## API

All endpoints require authentication. Mutations additionally require an antiforgery
token: GET `/api/chat/antiforgery`, retain its cookie, then send the returned `token`
as `RequestVerificationToken` with the same authenticated session. Blazor event
handlers execute in the authenticated circuit and call the same service directly.

| Method | Route | Body / result |
| --- | --- | --- |
| GET | `/api/chat/sessions` | Owned sessions, newest update first |
| POST | `/api/chat/sessions` | `{ "title": "Kubernetes" }`; created session |
| GET | `/api/chat/sessions/{id}` | Session and ordered messages |
| PATCH | `/api/chat/sessions/{id}` | `{ "title": "Cluster notes" }` |
| POST | `/api/chat/sessions/{id}/messages` | `{ "message": "Hello" }`; streamed text |
| POST | `/api/chat` | `{ "message": "Hello", "sessionId": "..." }`; streamed text |

The original URL remains available. Omitting `sessionId` creates a saved New chat;
the response header `X-Chat-Session-Id` provides its ID for reuse. Direct API clients
must add the antiforgery step. A busy session returns 409, a foreign/missing session
404, invalid text 400, and missing subject 403, before streaming starts. Generation
failure before streaming returns 503; later failure aborts the response. Read
history to inspect saved status. List/history responses are not cacheable. Check
history before retrying a failed send to avoid duplicate prompts.

## Helm / RKE2

Provision PostgreSQL with durable storage and backups separately. The chart does
not install or upgrade a database. Put the connection string in an existing Secret
in the app's namespace, then configure only its reference:

```yaml
database:
  existingSecret: chatbot-database
  connectionStringKey: connection-string
```

The deployment injects `ConnectionStrings__Chatbot`. Do not put credentials in Helm
values. Continue setting `authentication.existingSecret` and retaining Data Protection
keys. Authentication cookie lifetime and revocation behaviour are unchanged; chat
sessions are separate from authentication tickets. No cluster deployment or real
database credential was changed as part of implementation.

## Validation

```powershell
./tests/run-integration.ps1
```

The console integration suite uses a dedicated disposable PostgreSQL container,
the real store/controller, and Kestrel's authentication/antiforgery pipeline. A
deterministic Ollama fake exercises failure, cancellation and concurrency. The
real Ollama adapter is checked separately for context serialization and truncated
responses. Test authentication is compiled only into the excluded test project;
production still uses the Google package. Tests do not require Google credentials
and do not claim a live Google or Qwen roundtrip. See `tests/RESULTS.md` for the
checks actually run.

Test sources and all retrieval experiments are excluded from the web SDK build
and publish items. Milestone 1 itself did not include documents. The subsequent
[Milestone 2](document-upload.md) adds original-file uploads and status to these
owned sessions; embeddings, pgvector, ingestion, RAG, deletion and sharing remain
outside the implemented scope.
