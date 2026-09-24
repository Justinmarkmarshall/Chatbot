# Document-grounded chat results — 2026-09-23

Implemented and verified against a disposable PostgreSQL/pgvector database. No application database, originals, live chat history, or homelab deployment was changed by testing. Synthetic documents were passed to the already-running local Qwen3 model.

## Automated results

- Windows .NET: **125 deterministic checks passed** with fake generation and actual MiniLM/pgvector (included in the final 128-check run).
- Windows .NET with local `qwen3:1.7b`: **128 checks passed**, including three live generation cases below.
- Non-root Linux .NET runtime container (one CPU / 768 MiB): **125 checks passed**. Live Qwen3 tests were explicitly skipped in this container run; the Windows live run tested them separately.
- Build/publish completed with zero warnings/errors. Helm lint passed with the document worker enabled and the shared model claim configured.

The 33 new deterministic checks cover fixed top-five exact ranking, current-user/current-tab SQL isolation, real MiniLM ranking, source metadata and ordered scores, preservation of conversation history, document-only system instructions, unrelated questions without a similarity threshold, JSON encoding of untrusted document instructions, failed/cancelled retrieval with no general-knowledge fallback, explicit rejection of oversized embedding input, and persistence of partial answers together with evidence. Existing 92 chat/document-processing checks continue to pass, including upgrade preservation and original downloads.

Sources survive reloading a new service instance and are exposed through the authenticated history API and the Razor source list. The source list accurately describes retrieved context rather than claiming every candidate was used. The prompt requires supported citations in the answer.

## Complete-request corrections

The complete pasted request superseded the earlier assumption about empty chats. Empty chats now return the same persisted no-processed-documents response as queued/processing/failed-only chats and chats with zero indexed chunks. None of those branches call Ollama. Tests also exclude leftover chunks from failed/processing documents when another document is ready.

Structured logs now expose embedding, exact-query, retrieval-total, generation and total-request durations, with retrieved count and chat ID. Tests verify the allowed log fields exclude account identifiers, question text, source text and vectors. Legacy streaming/history tests now provision actual indexed synthetic documents to exercise generation under the new document-only contract.

## Actual Qwen3 observations

Fixtures describe Project Cedar backups at 02:00 UTC, a recovery owner named Morgan, recovery steps, retention, and unrelated bread/astronomy sections. Other tabs/users contain conflicting backup schedules and secret markers and are excluded by retrieval.

| Question | Observed response | End-to-end elapsed |
|---|---|---:|
| According to Project Cedar documents, when are PostgreSQL backups taken? | Nightly at 02:00 UTC, citing the Backup schedule section [1]. | 30.7 s |
| Who is responsible for that recovery? | Morgan, citing Backup schedule [2] and Recovery procedure [1]. | 26.3 s |
| Who composed The Magic Flute? | “The uploaded documents do not contain enough information to answer this question.” It did not supply Mozart. | 29.9 s |

These timings cover the complete chat call (retrieval, streamed generation and persistence) on this development host. They are single observations, not performance percentiles. The supported answer also superseded an earlier conversational statement about an hourly schedule, following current documentary evidence instead. Live checks assert completed turns and source metadata; the natural-language outputs above were inspected as observations, not asserted by exact-wording tests.

## Limits

Prompt-based grounding and these three successful answers do not prove universal factual correctness, citation accuracy, or resistance to every prompt injection. The application enforces retrieval isolation in SQL and fail-closed error handling; Qwen3 determines evidence sufficiency. No numeric relevance threshold or confidence score was introduced.

The new tests use direct/HTTP service calls plus server-rendered HTML; no new interactive browser/Google login test was performed. Linux testing uses published assemblies in the application runtime base image, not a fresh build of the private-package Dockerfile. RKE2 performance, shared-volume scheduling, production PDF quality, long-history context pressure and multi-user model-memory sizing remain unmeasured.

The MiniLM web model loads lazily and rejects document questions exceeding 254 content tokens without truncation. Source text larger than the documented prompt character guard fails the turn. No ingestion redesign, reranker, approximate vector index, Ollama embedding endpoint, or general-web knowledge source was added.
