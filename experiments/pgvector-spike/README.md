# Exact pgvector retrieval spike

Experimental console code only. Uses the existing pinned MiniLM FP32 artifacts and source-links `../embedding-spike/OnnxEmbedder.cs`. No production dependency, DI registration, EF Core, Dapper, upload pipeline, document domain, worker, RAG, UI or deployment changes.

Read [RESULTS.md](RESULTS.md) for measured results. The eight passages and seven questions in `cases.json` are the approved fixed test set. Six have a predefined expected top-1 passage; the Magic Flute question has none. There is no relevance threshold.

## Prerequisites and clean reproduction

- Linux x64 Docker engine and Docker Compose; PowerShell 7 for the orchestration/report scripts.
- Existing model files from the first spike under `../embedding-spike/models/minilm/`, unchanged. This experiment does not download or prepare models and uses no Python.
- Internet access for image pulls and NuGet restore at **build time**. Runtime inference needs only local models. Database access stays on an internal Compose network, without publishing a host port.

From this directory, the canonical clean reproduction command is:

```powershell
./run-spike.ps1 -Clean
./write-results.ps1
```

**`-Clean` removes only this experiment's Compose resources and named database volume before building/running.** It is required for a clean reproduction. PostgreSQL entrypoint scripts in `/docker-entrypoint-initdb.d` run only when the database volume is new. Editing `schema.sql` and restarting an existing volume does not rerun initialization. This is an experiment initializer, not a migration system.

The script starts PostgreSQL, records idle memory, reruns the original MiniLM regression in an offline container, and only starts retrieval after regression exit code zero. It samples database memory during the runner and records image/runtime details. It stops the experiment containers at the end, retaining the volume for inspection; the next `-Clean` run deletes that volume. Use `docker compose down --volumes` here to remove these resources explicitly.

Restore when developing the console code outside Docker:

```powershell
dotnet restore --configfile nuget.config --locked-mode
dotnet build -c Release --no-restore
```

The runner is container-oriented (`/models`, `/results`, database hostname `db`). Host database connectivity is intentionally not configured.

Linux bind-mount owners must allow container UID 1654 to write `results/`. Both .NET containers run non-root with read-only root filesystems and read-only model mounts.

## Dependency and schema choices

Pinned direct NuGet dependencies: Npgsql 10.0.3, Pgvector 0.3.2, Microsoft.ML.OnnxRuntime 1.30.0, Microsoft.ML.Tokenizers 2.0.0. Transitives are locked in `packages.lock.json`. Npgsql is configured with the normal `NpgsqlDataSourceBuilder.UseVector()` integration. Model source revisions and checksums remain in the original `models.json`.

PostgreSQL uses `pgvector/pgvector:0.8.6-pg17-bookworm`; the resolved repository digest is recorded in `results/environment.json`. Initialization creates `vector` and `spike_passages(id integer PRIMARY KEY, source_text text NOT NULL, embedding vector(384) NOT NULL)` before the runner connects. Experiment-only credentials are deliberately visible in Compose; no application secrets or databases are used.

Each run locally embeds the eight passages, truncates only `spike_passages`, and inserts the new rows in a transaction. Readback verifies exact text/float32 round trips, finite values and 384 dimensions. The runner independently reads `pg_extension` and records the actual extension version. It creates no extension or table itself.

`search.sql` is the exact approved query, unchanged. Parameters are a typed `Pgvector.Vector` and integer limit. Correctness inspection uses LIMIT 8 to retain all ranks, and LIMIT 3 to validate the actual returned subset. Timings and EXPLAIN use LIMIT 3. No vector indexes, query substitution, score threshold, reranking or optimization are added.

Independent .NET cosine distance uses explicit double dot-product and magnitude accumulators, comparing all 56 question/passage pairs to PostgreSQL at tolerance `1e-5`. Source text, IDs and expected answers are not changed on failures. The runner writes JSON before returning a nonzero exit for assertion failures; the orchestration script preserves those results and reports failure.

## Timing and memory boundaries

One data source and one open connection are reused. Connection creation, model loading, seeding, correctness checks, EXPLAIN and JSON output are outside measured retrieval iterations. Every question gets five warmups and 50 recorded searches (350 measured searches total).

- **Embedding:** `OnnxEmbedder.Embed(question)` only, including tokenization, ONNX CPU inference, pooling and normalization.
- **Database:** immediately before `ExecuteReaderAsync` through reading/materialising the last row. Includes round-trip; command/parameter construction is outside this timer.
- **End to end:** before embedding through vector construction, command construction, execution and complete result reading/disposal. No new connection is opened inside this boundary.

All use `Stopwatch`. Median is calculated from ordered individual samples; p95 is nearest-rank `ceil(0.95*n)`. Overall metrics pool all 350 samples, not averages of per-question percentiles. The report also includes each question's metrics.

Model startup records complete embedder construction and separately reports SHA-256 verification, tokenizer load and ONNX session initialization. File hashing precedes loading and warms storage caches; these are not true cold-disk startup benchmarks.

PostgreSQL memory uses cgroup v2 `memory.current` snapshots before/after the run and approximately once per second during it. Maximum sampled current memory is **not** an exact workload peak. `memory.peak`, when available, is a kernel-recorded container-lifetime peak and includes initialization. Cgroup usage includes more than PostgreSQL private process RSS, including charged file cache. Sampling via `docker exec` introduces small measurement overhead. Unsupported memory sampling is recorded as unavailable, not invented.

## Evidence and limits

`results/retrieval.json` holds ranks, all raw timing samples, independent cosine error, actual indexes, and complete `EXPLAIN (ANALYZE, BUFFERS)` output for every question. Plans may print long parameter vectors; the Markdown report abbreviates only that vector literal and links to the complete raw output. A top-N **sort** in an exact sequential-scan plan is not an approximate vector index.

`results/embedding-regression-linux.json` is a new regression output. Prior files under `embedding-spike/results/` are not overwritten. The original embedding spike also passed locally on Windows with its output stored in this experiment's results directory.

These are development-machine Docker/WSL2 results for eight short passages, not a scalable retrieval benchmark or an RKE2 capacity estimate. No simultaneous Qwen load or production database concurrency was tested. Nearest-neighbour retrieval always has a closest row in a nonempty collection; this does not establish that any row answers the question.
