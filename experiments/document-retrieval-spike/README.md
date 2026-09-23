# Document retrieval spike

An isolated retrieval-quality experiment. It reuses the pinned all-MiniLM-L6-v2 FP32
`../embedding-spike/OnnxEmbedder.cs` by source linking, with no production service,
UI, ingestion, model changes, or approximate search. The preceding experiments
are preserved. The root web project already excludes `experiments/**` from its
SDK items, so this experiment needs no root project changes.

## Run

Prerequisites: Docker with Linux containers, PowerShell, and the already prepared
MiniLM artifacts in `../embedding-spike/models/minilm/`. Models are verified against
the existing manifest. Building images can require internet access; runtime uses
only .NET and the local model files. There is no Python runtime or runtime download.

```powershell
cd experiments/document-retrieval-spike
./run-spike.ps1
./write-results.ps1
```

The script builds both images, starts this experiment's PostgreSQL, reruns the
original MiniLM regression, then starts retrieval. It records logs, environment,
and sampled PostgreSQL cgroup memory. Containers are stopped on exit. Use
`-SkipBuild` to reuse images. `-Clean` removes **this experiment's** Compose volume
before starting; previous experiments' volumes are separate. Schema initialization
runs on a fresh database volume. Each runner transaction replaces only its own
`document_chunks` table contents. Results in this directory are replaced on rerun;
archive them before trying a different corpus or configuration.

Compose project: `chatbot-document-retrieval-spike`. PostgreSQL 17 with pgvector
0.8.6, one CPU / 512 MiB; runner and regression one CPU / 768 MiB each. The private
network is internal, without published database ports. The runner is nonroot with
a read-only root filesystem. Its only network destination needed at runtime is the
experiment database. Credentials are disposable test credentials. Resource settings
match the preceding experiment, not a new PostgreSQL tuning exercise. Images use
the preceding experiment's tags; actual image identifiers are captured per run.

Dependencies are locked: Npgsql 10.0.3, Pgvector 0.3.2, ONNX Runtime 1.30.0,
Microsoft.ML.Tokenizers 2.0.0. No EF Core or production abstraction is introduced.

## Frozen inputs and relevance

Ten authored synthetic Markdown documents (6,509 whitespace-delimited words,
including Markdown markers) cover related technical topics and unrelated everyday
subjects. Fictional policies are test data, not guidance for real systems.
`corpus/manifest.json` fixes stable IDs. `cases.json` fixes 22 answerable questions
and four no-answer questions. The no-answer cases include both distant topics and
missing facts within nearby subjects. Every strategy uses all documents and cases.

`inputs.lock.json` was generated before any retrieval result and freezes the corpus,
manifest, cases, and exact SQL. The runner refuses a hash mismatch. Changing these
inputs means a new experiment version, not adjusting labels to fit results.

Each answerable case identifies a document and a Markdown section, plus one or
more verbatim evidence spans validated to occur in that section. A chunk is
relevant only if it contains **all** those spans. Multiple chunks may qualify due
to overlap. Merely sharing a document or heading is insufficient. Evidence spanning
two paragraphs deliberately tests whether a single retrieval unit contains a
complete answer; this evaluates single-chunk sufficiency, not a union of top-K
partial answers. A strategy can produce no fully relevant chunk for such a case.
No-answer questions have no relevance labels and are excluded from quality metrics.

## Deterministic chunking

All content is capped at **254 MiniLM content tokens**, leaving two special tokens
within the unchanged 256-token sequence limit. This is the explicitly agreed
replacement for the original requested 400/50 configuration; there is no 400-token
measurement. `token_count` and all token statistics exclude the two special tokens.

- Paragraph: split on blank lines; attach a standalone heading to the following
  block; retain a contiguous list as one block. Subdivide oversized blocks.
- Fixed 254/50: traverse the whole document without respecting section boundaries,
  greedily adding whole whitespace-delimited words up to the token cap.
- Heading-aware: start a section at each Markdown heading, keep that heading in
  the first source fragment, and subdivide oversized sections without merging
  different sections. Every subdivision retains its heading as metadata. Headings
  are not artificially prepended to subsequent fragments' embedding text.

All subdivisions use the same tokenizer-counted windows. The overlap substring
contains exactly 50 content tokens when tokenized by the existing tokenizer.
Prefer a whitespace boundary for its start; if WordPiece token lengths skip that
count, use a character boundary. Complete next windows are always retokenized and
checked against 254, so a partial word cannot cause silent truncation. Windows
can be slightly below 254 to preserve whole words at their ends. The intended
meaning of 254 is a maximum, not padding every input to that length. Nonoverlapping
natural paragraph/section boundaries do not acquire artificial overlap.

Source text remains an exact substring of its document. Start/end offsets are
zero-based UTF-16 character offsets, end exclusive, in the original decoded file.
Chunks crossing headings list all overlapping headings as metadata. Coverage
checks require every non-whitespace source character to occur in a chunk.
The original Unicode tokenizer remains unchanged; this Markdown corpus uses ASCII.
This is a deliberately simple experiment chunker, not a general Markdown parser.

## Validation and measurement

The runner checks deterministic chunking twice per document/strategy, exact source
spans, token caps, exact subdivision overlap, and complete non-whitespace coverage.
A synthetic oversized section exercises subdivision in all three strategies.
Original MiniLM regression results are a prerequisite. Database readback verifies
every provenance field and exact finite 384-element float32 vectors.

The only indexes are the primary key and provenance uniqueness B-trees. Exact
`embedding <=> $1` search is filtered by strategy and ordered by distance then ID.
All returned distances in every complete evaluation ranking are compared with
independent scalar .NET double-accumulator cosine calculations (tolerance 1e-5).
Representative EXPLAIN (ANALYZE, BUFFERS) output is retained for each strategy.
ANALYZE refreshes table statistics; no approximate index or database tuning is added.

Recall@1/3/5 use the requested **at-least-one relevant chunk** definition (also
called hit rate), not the fraction of all relevant chunks returned. MRR uses the
complete ranking, not a top-five cutoff; no fully relevant chunk contributes zero.
A top-one miss is reported as a failure even if top-three retrieval succeeds.
Validity and the initial Recall@3 >= 90% quality target are separate flags.

After correctness evaluation, each question/strategy receives five warmup and
50 measured searches, returning five rows. One data source and connection are
created outside all retrieval timings and reused throughout. The embedding timer
covers Embed only; database timing covers ExecuteReader through complete row
materialization; end-to-end covers embedding, Vector construction and the entire
search call. Validation and serialization are outside timing. Median averages the
two middle observations; p95 is nearest rank. Strategy aggregates pool equal-sized
question samples. Fixed strategy order is recorded; cache and host-load effects
remain possible. Full-corpus embedding time covers every chunk from every strategy
once; startup, validation/chunking and insertion are reported separately.

These are Windows-hosted Linux development-container measurements. Nothing here
measures RKE2, throughput under concurrency, real documents, or production capacity.
Similarity values are observations, not confidence, and no cutoff is introduced.

## Evidence

- `RESULTS.md`: summary, quality, timing, validity, limitations, recommendation.
- `results/retrieval.json`: all chunks, full ranked source text for every question,
  frozen hashes, evidence spans, raw timings, database versions and plans.
- `results/rankings.md`: inspectable top five, first relevant rank and similarities.
- `results/failures.md`: every top-one miss, expected spans and returned source text.
- `failure-observations.json`: manual post-run diagnostics, kept separate from
  frozen relevance labels and never used to calculate metrics.
- `results/embedding-regression-linux.json`: unchanged MiniLM test suite result.
- `results/preserved-before.json` / `preservation-check.json`: preceding spikes audit.
- `results/environment.json`, `postgres-memory.json`, `container-output.txt`:
  execution context and measured memory snapshots (not RKE2 evidence).

Stop after evaluating and recording this experiment. No downstream RAG, ingestion,
production integration, or next experiment is implemented here.
