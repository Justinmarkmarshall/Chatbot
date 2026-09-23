# Milestone 3 validation — 2026-09-23

The initial Windows/Linux runs passed **88 integration checks**. The final expanded Linux run passed **92 integration checks**, additionally testing competing-worker exclusion, retry ownership, persistent no-text failure, and absence of partial chunks. The table below records the initial comparable runs, including all prior chat/upload checks. Test PostgreSQL used a disposable tmpfs data directory; the user's application database was not used.

| Measurement | Windows development host | Linux Docker Desktop container |
|---|---:|---:|
| MiniLM load including artifact hash verification | 733.1 ms | 6705.0 ms |
| Extract/chunk/embed seven chunks | 1308.7 ms | 1157.4 ms |
| Process working set sampled after that operation | 286.8 MiB | 329.2 MiB |
| Three-section exact cosine query, client elapsed | 4.0 ms | 3.3 ms |
| Relevant Kubernetes result cosine similarity | 0.8048 | 0.8048 |

These are single-run smoke measurements, not percentiles or peak-memory benchmarks. The Linux container was constrained to one CPU/768 MiB, ran as UID 1654, mounted the model read-only, and used mcr.microsoft.com/dotnet/aspnet:10.0 (digest sha256:2d584d8147faddb0d678c5748d47953e5b8e18621ed4fb7049a91381d9d7746f). It used only the local database network during tests; inference uses local files. Docker reported swap limiting unavailable on this host. Npgsql emitted an optional Kerberos-library warning; password-authenticated PostgreSQL tests all succeeded.

Verified: 39 independent tokenizer cases; actual MiniLM inference; real generated PDF with large-font heading/page metadata; Markdown heading separation; 254-token cap and exact 50-token overlap; final-content preservation; 384 dimensions; atomic queued/ready transitions; exact cosine relevance ranking; owner-scoped vector isolation; interrupted-job recovery without duplicates; failed processing preserves originals; retry; upgrade from schema 1 without chat loss. Existing authenticated HTTP, antiforgery, chat persistence, upload limits and ownership tests continue to pass. Build: zero warnings/errors. Helm worker template lint passed with the worker enabled.

Not measured: RKE2 memory/latency, production PDFs, OCR, multi-column extraction quality, live Google browser login, or live cluster deployment. No RAG/answer changes were made. The Linux check runs the published .NET test/application assemblies in the same runtime base image; it does not rebuild the private-package Dockerfile.

Reproduce Windows checks: `./tests/run-integration.ps1`.

Reproduce Linux checks:

```powershell
dotnet publish tests/Chatbot.IntegrationTests/Chatbot.IntegrationTests.csproj -c Release -o tests/linux-publish
./tests/run-linux-integration.ps1
```

The existing embedding experiment's local model cache and reference fixture file must be present. Test scripts stop only their disposable database after completion.


Final Linux rerun: model load 6401.6 ms; seven chunks 961.3 ms; sampled working set 330.8 MiB; exact query 3.9 ms; similarity 0.8048. All 92 checks passed.
