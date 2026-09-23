# Document-grounded chat (Milestone 4)

Chat generation now connects the existing MiniLM/pgvector pipeline to Ollama/Qwen3. It does not change document extraction, chunking, the processing worker, or the embedding profile. No NuGet packages were added.

## Behaviour

- A chat with no uploaded documents returns a clear no-processed-documents response without calling Ollama. There is no general-purpose fallback.
- A chat with only queued/processing/failed documents, or no successfully indexed chunks, returns the same no-processed-documents response without calling Ollama. This response is saved in conversation history.
- For a chat with ready documents, the question is embedded using the existing pinned MiniLM FP32 implementation. PostgreSQL ranks eligible chunks with exact cosine distance and returns at most five (all available when fewer than five exist). There is no similarity cutoff or approximate index.
- Both the authenticated Google NameIdentifier and current chat ID are predicates in the database query. The query also requires uploaded/ready status and the matching model profile. There is no retrieval from other tabs, global search, or filtering of cross-user results in memory.
- The current question, the previous ten completed turns, a document-only system instruction, and the retrieved evidence are passed through the existing streaming Ollama client. History provides conversational references only; it is explicitly not factual evidence. Current documents take precedence over prior assistant claims.
- Qwen3 is instructed to assess whether the excerpts answer the question, cite supporting source numbers/names, and say that the uploaded documents do not contain enough information when evidence is insufficient. Nearest neighbours are not automatically relevant. No-answer decisions are made by Qwen3 from the evidence, not by a similarity threshold.
- Filenames, headings and excerpt content are JSON-encoded evidence in a user-role context message. The system instruction says never to follow document instructions or let questions/history override document-only grounding.

These instructions constrain generation but are not a mathematical guarantee that every model output is supported. Real-model smoke tests complement deterministic prompt, retrieval and ownership tests; they are not a comprehensive factuality evaluation.

## Source attribution and persistence

Migration `004_reply_sources.sql` adds a nullable JSONB evidence snapshot to assistant messages. It preserves ranked source number, document ID, original filename, heading, zero-based chunk index, actual PDF page when available, source text, and cosine distance/similarity. The snapshot is saved before generation using the existing owner/turn fence; partial, cancelled and failed replies retain their evidence. Existing messages remain unchanged with null sources.

`GET /api/chat/sessions/{id}` exposes each assistant message's `sources`. The existing plain-text streaming endpoints retain their format; API clients can fetch the saved conversation after streaming for structured metadata. The UI adds a collapsible **Document context supplied** list under replies with links to existing authorized original downloads. This lists retrieved candidates, not a claim that the model used all five. Citations in the answer identify which evidence the model says it used. Scores are not displayed as confidence.

Sources are numbered afresh per turn. The snapshot describes what the model received at that time, even if a document is later reprocessed. Source snapshots are not themselves added to later model history; each turn retrieves current evidence.

## Local use

Keep the existing PostgreSQL/pgvector container, document worker and Ollama running. The web process now also needs the same `Documents:ModelRoot` configuration and readable pinned MiniLM files used by the worker. Existing local user secrets work for both processes:

```powershell
# From C:\Dev\AI\Chatbot\Chatbot; model-assets contains the provisioned minilm/ directory.
dotnet user-secrets set "Documents:ModelRoot" "$PWD/model-assets"
dotnet run --launch-profile https
```

Startup applies migration 004. No additional Docker database command is required for an already running pgvector container.

Upload documents, wait until their processing status is ready, and ask a question in that same tab. Try a supported question, a conversational follow-up, and a question absent from the files. Then switch to another tab to verify it uses only that tab's documents.

The query model loads lazily once per web process, only when ready documents are queried. Access is serialized to bound inference concurrency; MiniLM still uses its existing CPU settings. Document questions are limited to 254 MiniLM content tokens (+2 special tokens). Longer input receives an explicit shorten-question error, never silent truncation. The existing 16,000-character request-size guard also remains; it does not override the smaller model-token limit. A pathological retrieved context over 64,000 serialized characters fails rather than silently dropping evidence. These are input/resource limits, not relevance thresholds.

Missing/corrupt model files, embedding failure or database failure fail the turn and do not fall back to an ungrounded answer. Cancellation propagates through retrieval and generation; synchronous ONNX inference cannot be forcibly interrupted mid-call but cancellation is checked before/after it. Original files and prior messages remain available.

## Deployment

The existing `documentProcessing.modelExistingClaim` now mounts the same model directory read-only in the web deployment and sets `Documents__ModelRoot`. The worker remains separate. Provision a volume access mode/scheduling arrangement that permits both pods to read it; a ReadWriteOnce volume cannot be mounted across different nodes. The web process needs additional memory for one MiniLM session. Homelab sizing has not been measured in this milestone; chart resource limits remain operator-configurable.

## Validation

Run `./tests/run-integration.ps1 -NoRestore` after building/restoring as needed. Optional local Qwen3 checks use synthetic test documents:

```powershell
$env:CHATBOT_TEST_OLLAMA_URL = 'http://localhost:11434'
./tests/run-integration.ps1 -NoRestore
Remove-Item Env:CHATBOT_TEST_OLLAMA_URL
```

Linux regression suite:

```powershell
dotnet publish tests/Chatbot.IntegrationTests/Chatbot.IntegrationTests.csproj -c Release -o tests/linux-publish
./tests/run-linux-integration.ps1
```

Tests use a disposable database, actual MiniLM embeddings, and actual pgvector ranking. Generation contract tests use a fake Ollama; optional Qwen3 smoke runs record answers for manual inspection of supported facts/citations, follow-up context, and refusal to answer from general knowledge. Automated assertions check completion and source metadata rather than probabilistic answer wording. See document-grounded-chat-results.md for measured outcomes and limitations.

## Structured timing logs

Retrieval logs contain `ChatId`, `RetrievedCount`, `EmbeddingMs`, `QueryMs`, and `RetrievalMs`. Embedding timing includes waiting for the shared query model and first-use model loading; QueryMs covers the exact vector query and result reading. RetrievalMs also includes readiness checks. Chat logs expose `GenerationMs`, `TotalMs`, and `Completed`; total duration includes final message persistence. No questions, document text, vectors, or authenticated subject identifiers are included in these timing records. Tests verify the field allowlist. Timings describe individual requests, not RKE2 capacity.

## Files for this milestone

- `Services/QueryEmbedding.cs`, `DocumentRetrieval.cs`, `DocumentPrompt.cs`: reused MiniLM model loader, owner/tab-scoped exact SQL, document-only prompt.
- `Services/ChatService.cs`: orchestration, no-context refusal, source snapshots and timing logs.
- `Models/DocumentSource.cs`, `Models/ChatSession.cs`, `Persistence/ChatStore.cs`, `Persistence/ChatDatabase.cs`, `Persistence/Migrations/004_reply_sources.sql`: source metadata, persistence and migration.
- `Program.cs`: query embedding and retrieval service registrations.
- `Components/Pages/Home.razor`, `Components/SessionDocuments.razor`: source display and document-chat guidance.
- `charts/chatbot/templates/deployment.yaml`, `charts/chatbot/values.yaml`: read-only model access for the web process using the existing configured claim.
- `tests/Chatbot.IntegrationTests/Program.cs`, `RetrievalChecks.cs`: real-database fixtures, ownership/failure/history/source/logging coverage and optional live generation observations.
- `README.md`, `tests/RESULTS.md`, `docs/document-grounded-chat.md`, `docs/document-grounded-chat-results.md`, `docs/document-processing.md`: setup, request flow and validation record.

The Milestone 3 extraction, ingestion, worker, MiniLM implementation and 254/50 chunking code are unchanged by this milestone.
