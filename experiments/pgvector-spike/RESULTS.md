# PostgreSQL / pgvector spike results

Development-container measurements only. No RKE2 or production throughput claim. Measured 2026-09-23T11:33:34.2648443Z (UTC).

| Check / measurement | Actual result |
| --- | --- |
| Overall correctness | True; runner exit 0 |
| Original MiniLM regression | Passed on Windows and offline Linux; 39 tokenizer fixtures plus embedding checks |
| Stored embeddings | 8 rows, 384 dimensions, all finite; exact text/vector readback passed |
| Supported top-1 | 6 / 6 expected passages ranked first |
| Unrelated Magic Flute question | Returned IDs 4, 7, 5; none is relevant |
| Maximum .NET/PostgreSQL distance difference | 3.500694712E-08; tolerance 1e-5 |
| Approximate indexes | None; only spike_passages_pkey |
| Installed pgvector | 0.8.6 |
| Resolved PostgreSQL image digest | pgvector/pgvector@sha256:cf134a767f474095eeba57e0117be8e568e011a63f33fbf252f14c9b760f8e6f |
| Full model startup | 5,370.69 ms; includes local artifact verification |
| ONNX session load only | 670.23 ms |
| Query embedding median / p95 | 5.398 / 6.828 ms |
| Database retrieval median / p95 | 0.818 / 1.074 ms |
| End-to-end median / p95 | 6.254 / 7.828 ms |

Each question had five warmups and 50 individually measured searches; aggregate percentiles use all 350 samples. One Npgsql data source and connection were reused. Timed retrieval returns three rows. The full ranking below uses the identical query with LIMIT 8; no vector index, optimization, threshold or reranking was introduced.

**Nearest-neighbour retrieval does not itself establish relevance. Similarity is not confidence.** The Magic Flute question has no relevant passage in this dataset, but still receives three neighbours. No conclusion about answerability is drawn from those matches.

## Full ranked results

### How can I recover database records that were accidentally deleted?

Expected passage: 1 Ranks 1–3 are the returned subset used by the benchmark.

| Rank | Passage ID | Source text | Cosine distance | Similarity |
| ---: | ---: | --- | ---: | ---: |
| 1 | 1 | Restore a PostgreSQL database by importing a previously saved backup file. This can recover data lost through accidental deletion. | 0.34839845 | 0.65160155 |
| 2 | 3 | To reset a forgotten password, select “Forgot password” and follow the recovery link sent to your email address. | 0.68889976 | 0.31110024 |
| 3 | 2 | Schedule database backups every night and retain several generations. Store a copy on another machine. | 0.71587729 | 0.28412271 |
| 4 | 5 | Yeast makes bread dough rise by producing carbon dioxide during fermentation. | 0.96506497 | 0.03493503 |
| 5 | 8 | Tomato plants need sunlight, regular watering and well-drained soil to grow successfully. | 0.97991821 | 0.02008179 |
| 6 | 4 | Bake a bread loaf in a preheated oven at 220 degrees Celsius for approximately thirty minutes. | 1.01245090 | -0.01245090 |
| 7 | 6 | Solar panels convert sunlight into electrical energy using photovoltaic cells. | 1.02310285 | -0.02310285 |
| 8 | 7 | The railway service arrives at the central station at noon. Passengers should reach the platform before departure. | 1.10251783 | -0.10251783 |

Median/p95 milliseconds: embedding **5.654/6.273**, database **0.805/1.020**, end-to-end **6.516/7.128**.

### I cannot remember my account password. How do I regain access?

Expected passage: 3 Ranks 1–3 are the returned subset used by the benchmark.

| Rank | Passage ID | Source text | Cosine distance | Similarity |
| ---: | ---: | --- | ---: | ---: |
| 1 | 3 | To reset a forgotten password, select “Forgot password” and follow the recovery link sent to your email address. | 0.27679241 | 0.72320759 |
| 2 | 1 | Restore a PostgreSQL database by importing a previously saved backup file. This can recover data lost through accidental deletion. | 0.64024299 | 0.35975701 |
| 3 | 2 | Schedule database backups every night and retain several generations. Store a copy on another machine. | 0.84446630 | 0.15553370 |
| 4 | 5 | Yeast makes bread dough rise by producing carbon dioxide during fermentation. | 0.89244342 | 0.10755658 |
| 5 | 6 | Solar panels convert sunlight into electrical energy using photovoltaic cells. | 0.94564076 | 0.05435924 |
| 6 | 4 | Bake a bread loaf in a preheated oven at 220 degrees Celsius for approximately thirty minutes. | 0.98281408 | 0.01718592 |
| 7 | 8 | Tomato plants need sunlight, regular watering and well-drained soil to grow successfully. | 1.01628626 | -0.01628626 |
| 8 | 7 | The railway service arrives at the central station at noon. Passengers should reach the platform before departure. | 1.04791141 | -0.04791141 |

Median/p95 milliseconds: embedding **6.059/7.395**, database **0.885/1.152**, end-to-end **6.965/8.717**.

### What oven temperature and cooking time should I use for a bread loaf?

Expected passage: 4 Ranks 1–3 are the returned subset used by the benchmark.

| Rank | Passage ID | Source text | Cosine distance | Similarity |
| ---: | ---: | --- | ---: | ---: |
| 1 | 4 | Bake a bread loaf in a preheated oven at 220 degrees Celsius for approximately thirty minutes. | 0.16957146 | 0.83042854 |
| 2 | 5 | Yeast makes bread dough rise by producing carbon dioxide during fermentation. | 0.61880732 | 0.38119268 |
| 3 | 7 | The railway service arrives at the central station at noon. Passengers should reach the platform before departure. | 0.90791227 | 0.09208773 |
| 4 | 2 | Schedule database backups every night and retain several generations. Store a copy on another machine. | 0.96356174 | 0.03643826 |
| 5 | 3 | To reset a forgotten password, select “Forgot password” and follow the recovery link sent to your email address. | 0.99244483 | 0.00755517 |
| 6 | 6 | Solar panels convert sunlight into electrical energy using photovoltaic cells. | 1.03665300 | -0.03665300 |
| 7 | 8 | Tomato plants need sunlight, regular watering and well-drained soil to grow successfully. | 1.04176778 | -0.04176778 |
| 8 | 1 | Restore a PostgreSQL database by importing a previously saved backup file. This can recover data lost through accidental deletion. | 1.06855846 | -0.06855846 |

Median/p95 milliseconds: embedding **6.490/7.621**, database **0.911/1.263**, end-to-end **7.489/8.632**.

### How is sunlight turned into electricity?

Expected passage: 6 Ranks 1–3 are the returned subset used by the benchmark.

| Rank | Passage ID | Source text | Cosine distance | Similarity |
| ---: | ---: | --- | ---: | ---: |
| 1 | 6 | Solar panels convert sunlight into electrical energy using photovoltaic cells. | 0.29173086 | 0.70826914 |
| 2 | 8 | Tomato plants need sunlight, regular watering and well-drained soil to grow successfully. | 0.75064302 | 0.24935698 |
| 3 | 5 | Yeast makes bread dough rise by producing carbon dioxide during fermentation. | 0.81656995 | 0.18343005 |
| 4 | 2 | Schedule database backups every night and retain several generations. Store a copy on another machine. | 0.93355214 | 0.06644786 |
| 5 | 1 | Restore a PostgreSQL database by importing a previously saved backup file. This can recover data lost through accidental deletion. | 0.96033317 | 0.03966683 |
| 6 | 3 | To reset a forgotten password, select “Forgot password” and follow the recovery link sent to your email address. | 0.96431287 | 0.03568713 |
| 7 | 7 | The railway service arrives at the central station at noon. Passengers should reach the platform before departure. | 0.96720149 | 0.03279851 |
| 8 | 4 | Bake a bread loaf in a preheated oven at 220 degrees Celsius for approximately thirty minutes. | 0.98878040 | 0.01121960 |

Median/p95 milliseconds: embedding **4.639/5.374**, database **0.810/1.018**, end-to-end **5.461/6.207**.

### When does the train reach the central station?

Expected passage: 7 Ranks 1–3 are the returned subset used by the benchmark.

| Rank | Passage ID | Source text | Cosine distance | Similarity |
| ---: | ---: | --- | ---: | ---: |
| 1 | 7 | The railway service arrives at the central station at noon. Passengers should reach the platform before departure. | 0.29041205 | 0.70958795 |
| 2 | 5 | Yeast makes bread dough rise by producing carbon dioxide during fermentation. | 0.90333557 | 0.09666443 |
| 3 | 4 | Bake a bread loaf in a preheated oven at 220 degrees Celsius for approximately thirty minutes. | 0.93987322 | 0.06012678 |
| 4 | 6 | Solar panels convert sunlight into electrical energy using photovoltaic cells. | 0.95617061 | 0.04382939 |
| 5 | 3 | To reset a forgotten password, select “Forgot password” and follow the recovery link sent to your email address. | 0.98046205 | 0.01953795 |
| 6 | 2 | Schedule database backups every night and retain several generations. Store a copy on another machine. | 1.00538662 | -0.00538662 |
| 7 | 1 | Restore a PostgreSQL database by importing a previously saved backup file. This can recover data lost through accidental deletion. | 1.00566394 | -0.00566394 |
| 8 | 8 | Tomato plants need sunlight, regular watering and well-drained soil to grow successfully. | 1.02853446 | -0.02853446 |

Median/p95 milliseconds: embedding **5.351/6.413**, database **0.819/0.997**, end-to-end **6.238/7.495**.

### What conditions help tomatoes grow?

Expected passage: 8 Ranks 1–3 are the returned subset used by the benchmark.

| Rank | Passage ID | Source text | Cosine distance | Similarity |
| ---: | ---: | --- | ---: | ---: |
| 1 | 8 | Tomato plants need sunlight, regular watering and well-drained soil to grow successfully. | 0.26537681 | 0.73462319 |
| 2 | 6 | Solar panels convert sunlight into electrical energy using photovoltaic cells. | 0.82789183 | 0.17210817 |
| 3 | 5 | Yeast makes bread dough rise by producing carbon dioxide during fermentation. | 0.88805363 | 0.11194637 |
| 4 | 2 | Schedule database backups every night and retain several generations. Store a copy on another machine. | 0.92459597 | 0.07540403 |
| 5 | 7 | The railway service arrives at the central station at noon. Passengers should reach the platform before departure. | 0.98286712 | 0.01713288 |
| 6 | 3 | To reset a forgotten password, select “Forgot password” and follow the recovery link sent to your email address. | 1.01144810 | -0.01144810 |
| 7 | 1 | Restore a PostgreSQL database by importing a previously saved backup file. This can recover data lost through accidental deletion. | 1.05389247 | -0.05389247 |
| 8 | 4 | Bake a bread loaf in a preheated oven at 220 degrees Celsius for approximately thirty minutes. | 1.06292325 | -0.06292325 |

Median/p95 milliseconds: embedding **4.703/5.726**, database **0.800/1.085**, end-to-end **5.568/6.649**.

### Who composed the opera The Magic Flute?

Expected passage: None; deliberately unrelated. Ranks 1–3 are the returned subset used by the benchmark.

| Rank | Passage ID | Source text | Cosine distance | Similarity |
| ---: | ---: | --- | ---: | ---: |
| 1 | 4 | Bake a bread loaf in a preheated oven at 220 degrees Celsius for approximately thirty minutes. | 0.95674348 | 0.04325652 |
| 2 | 7 | The railway service arrives at the central station at noon. Passengers should reach the platform before departure. | 0.98174902 | 0.01825098 |
| 3 | 5 | Yeast makes bread dough rise by producing carbon dioxide during fermentation. | 0.98238010 | 0.01761990 |
| 4 | 1 | Restore a PostgreSQL database by importing a previously saved backup file. This can recover data lost through accidental deletion. | 1.02138928 | -0.02138928 |
| 5 | 2 | Schedule database backups every night and retain several generations. Store a copy on another machine. | 1.03577594 | -0.03577594 |
| 6 | 6 | Solar panels convert sunlight into electrical energy using photovoltaic cells. | 1.04930707 | -0.04930707 |
| 7 | 3 | To reset a forgotten password, select “Forgot password” and follow the recovery link sent to your email address. | 1.05709326 | -0.05709326 |
| 8 | 8 | Tomato plants need sunlight, regular watering and well-drained soil to grow successfully. | 1.15737154 | -0.15737154 |

Median/p95 milliseconds: embedding **4.768/5.516**, database **0.710/0.917**, end-to-end **5.498/6.337**.

## Database inspection

PostgreSQL 17.11 (Debian 17.11-1.pgdg12+2) on x86_64-pc-linux-gnu, compiled by gcc (Debian 12.2.0-14+deb12u1) 12.2.0, 64-bit

Actual pg_indexes definitions:
```sql
CREATE UNIQUE INDEX spike_passages_pkey ON public.spike_passages USING btree (id);
```

EXPLAIN (ANALYZE, BUFFERS) was recorded for all seven questions in [retrieval.json](results/retrieval.json). First-question plan below; only the long vector literal is abbreviated. It scans all eight rows and sorts exactly. Estimates are from a newly seeded table; this is not a planner-tuning or ANN-performance test.

```text
Limit  (cost=35.86..35.87 rows=3 width=52) (actual time=0.026..0.027 rows=3 loops=1)
  Buffers: shared hit=2
  ->  Sort  (cost=35.86..37.99 rows=850 width=52) (actual time=0.024..0.025 rows=3 loops=1)
        Sort Key: ((embedding <=> '[384-dimensional query vector]'::vector)), id
        Sort Method: top-N heapsort  Memory: 25kB
        Buffers: shared hit=2
        ->  Seq Scan on spike_passages  (cost=0.00..24.88 rows=850 width=52) (actual time=0.012..0.017 rows=8 loops=1)
              Buffers: shared hit=2
Planning Time: 0.066 ms
Execution Time: 0.042 ms
```

## PostgreSQL memory

- Idle snapshot immediately before retrieval: **50.48 MiB**.
- Maximum of 8 during-runner snapshots: **59.91 MiB** (sampled roughly once per second, not an exact peak).
- Kernel-recorded container-lifetime peak: **79.66 MiB**, including database initialization, not a retrieval-only peak.

These are cgroup memory charges, including cache, not private-process RSS. The database limit was 512 MiB; runner limit 768 MiB; each had one CPU quota. Sampling with docker exec adds small overhead. WSL warned that swap-limit enforcement was unavailable. [Raw memory timestamps](results/postgres-memory.json).

## Environment, preservation and reproduction

Ubuntu 24.04.5 LTS; .NET 10.0.12; amd64; Rancher Desktop WSL Distribution; kernel 6.18.33.2-microsoft-standard-WSL2; Docker 29.5.3.

PostgreSQL had no published host ports. Compose network inspection confirmed internal=true. Existing model files were mounted read-only. No model downloads or Python were used. Model revision: 1110a243fdf4706b3f48f1d95db1a4f5529b4d41.

Startup breakdown: artifact SHA-256 verification **4,574.28 ms**, tokenizer load **44.20 ms**, ONNX session construction **670.23 ms**. Artifact hashing warms the file cache. Full startup includes other constructor/validation overhead. This is not a cold-disk benchmark.

The experiment-specific embedding refactor reused the original tokenization/inference implementation. Original result files were preserved. No production source/configuration was written by this task.
Preservation audit: appsettings.json changed during this session without being written by the spike; left untouched. All spike edits are under experiments/. See [preservation-check.json](results/preservation-check.json).

Canonical clean reproduction from this directory:
```powershell
./run-spike.ps1 -Clean
./write-results.ps1
```

This removes the experiment database volume before rebuilding/running. PostgreSQL entrypoint initialization scripts execute only on a new volume. See [README.md](README.md) for prerequisites, exact timing boundaries, pinned dependencies and cleanup.

Evidence: [retrieval samples and plans](results/retrieval.json), [image digests and limits](results/environment.json), [Linux regression](results/embedding-regression-linux.json), [Windows regression](results/embedding-regression-windows.json). Experiment containers were stopped after measurement; the named volume remains for inspection.

## Recommendation and limits

The evidence supports MiniLM FP32 plus Npgsql/Pgvector exact cosine retrieval as a technically viable baseline: 6/6 fixed top-1 checks passed and database distances agreed with independent .NET calculations. Retain the direct parameterized query and do not add an approximate index on the basis of this tiny dataset.

Eight hand-written passages do not establish real-document retrieval quality, relevance thresholds, database scalability, concurrent throughput or RKE2 memory capacity. Test those separately if later approved. No ingestion, document model, production integration or deployment work follows this spike.
