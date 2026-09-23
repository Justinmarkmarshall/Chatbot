Unit-test implementation validation (2026-09-23): **56 xUnit cases passed on Windows/.NET 10**, zero failures or skips. Latest run reported 929 ms test duration (excluding build/restore). **All 140 existing integration checks also passed in the Linux .NET 10 container against disposable PostgreSQL**, including real MiniLM and pgvector. Live Qwen3 checks were not enabled for this run. The test database was stopped afterwards. See [test commands and coverage](README.md).

Small testability changes: token-count/embedding delegates for isolated processing tests, extracted upload validation, and store/retrieval interfaces for chat orchestration. Production still uses the same concrete implementations. Regression fixes cover cancellation during chunking/preparation, surrogate-pair boundaries in long unbroken text, and null Ollama message objects. No live model, network, database, or model artifacts are required by the unit suite after NuGet restore. Generated-PDF tests use the real in-process PdfPig library; synthetic token counts do not establish real tokenizer parity (the integration suite checks that separately).

Deployment/deletion validation: **140 Linux integration checks and 30 Helm render checks passed**. See [Kubernetes validation](../docs/rke2-deployment-results.md).

Milestone 4: **128 Windows checks (including live Qwen3), 125 Linux checks passed**. See [document-grounded chat results](../docs/document-grounded-chat-results.md).

# Milestone 1 and 2 validation

Latest combined run: **75 integration checks passed** against the disposable
PostgreSQL container, including all 42 earlier chat checks. The application built
with zero warnings and zero errors. The container was stopped after the run.

Milestone 2 adds verification of session-attached metadata, exact original bytes,
SHA-256, new-service restoration, cross-account and cross-chat isolation, unread
unauthorized upload streams, invalid extension/content/UTF-8/empty/oversized files,
persisted failure and cancellation status, visible Uploading reservations, quota
accounting, expired-upload recovery, multipart antiforgery, authenticated attachment
downloads and their response headers, and restored document UI markup. A real
version-1 schema with messages was upgraded twice without changing the original
chat history, then accepted a document. A signature-only PDF fixture checks byte
preservation, not PDF structural validity. No uploaded contents entered Ollama.

No new package or Helm change was needed for document storage. Live browser upload
interactions, external Google sign-in, and RKE2 deployment remain untested.

## Earlier milestone 1 run

Measured on 2026-09-23 in the development environment: .NET 10 on Windows,
PostgreSQL 17 in a Linux Docker container under Rancher Desktop/WSL2.

`./tests/run-integration.ps1 -NoRestore`: **42 integration checks passed**.
The disposable database container was stopped afterwards. The suite exercised:

- Concurrent schema initialization and repeat startup without losing records.
- Multiple sessions, persisted titles, ordered user/assistant messages, and
  restoration through new application services and a fresh authenticated client.
- Stable-subject ownership despite shared/changed email addresses; anonymous,
  missing-subject and cross-account read/rename/send rejection.
- Ordered Ollama conversation context and isolation between chat tabs.
- Concurrent-send rejection across service instances and through HTTP (409).
- Cancellation, model failure, empty replies, partial-response persistence,
  orphan recovery and stale-generation fencing.
- Real Kestrel routing, authentication, antiforgery rejection without a token,
  creation, streaming, history and the original `/api/chat` endpoint.
- Server-rendered Razor tabs, title controls and saved messages; foreign chat
  content absent from another account's rendered page.
- The real Ollama adapter's serialization and premature-EOF detection.

Build succeeded. Helm lint passed with dummy Google and database Secret names;
rendered output uses `secretKeyRef` for `ConnectionStrings__Chatbot`. No Helm release
was installed or upgraded.

Initial runs exposed parameterized multi-command SQL and missing MVC antiforgery
registration issues. The final 42-check run passed after their fixes and the
generation-deadline change.

Limitations: deterministic model replies and test-only identities were used. No
live Google sign-in/logout, live Qwen inference, interactive-browser click automation,
production database, or RKE2 deployment was tested. Tests verify restored ownership
and history across fresh identities/services, not Google's external availability.
Deployment requires a PostgreSQL connection Secret and existing Google credentials.
See `docs/persistent-chat-sessions.md` for setup.
