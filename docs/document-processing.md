# Document processing (Milestone 3)

The later [document-grounded chat milestone](document-grounded-chat.md) now connects this pipeline to chat. The description below records the Milestone 3 processing contract.

PDF/TXT uploads queue automatically after the original is durably saved. A separate .NET worker extracts text, splits at headings, subdivides oversized sections, embeds them with the pinned all-MiniLM-L6-v2 FP32 model, and saves 384-dimensional pgvector vectors. Chat/Ollama context is unchanged; there is no document search UI or RAG in this milestone.

## Run locally

From the Chatbot project directory, start the existing local database with the updated image (PostgreSQL 17 + pgvector). Keep your existing password and named volume; do not run `down -v`.

```powershell
docker compose -f compose.postgres.yaml up -d --wait
```

`CHATBOT_POSTGRES_PASSWORD` must already be set as described in persistent-chat-sessions.md. The existing `ConnectionStrings:Chatbot` user secret is used by both processes. Startup applies migration 003 without deleting originals or messages; existing uploaded documents are queued. PostgreSQL must have the vector extension installed, and the migration account must be allowed to create it. Back up an existing database before upgrading its image/schema.

Provision the already verified model artifacts into a dedicated directory (weights are not committed, downloaded at runtime, or packaged in the application):

```powershell
New-Item -ItemType Directory -Force model-assets | Out-Null
Copy-Item -Recurse experiments/embedding-spike/models/minilm model-assets/minilm
dotnet user-secrets set "Documents:ModelRoot" "$PWD/model-assets"
dotnet run --launch-profile https
```

In a second terminal, from the same project directory:

```powershell
$env:DOTNET_ENVIRONMENT = 'Development'
dotnet run --no-launch-profile -- --document-worker
```

Use `Documents__ModelRoot` and `ConnectionStrings__Chatbot` environment variables outside development. The model root contains `minilm/model.onnx`, vocabulary, tokenizer/config JSON, and the pinned model README. Every artifact is SHA-256 verified against Processing/minilm.json; missing or modified artifacts stop worker startup. No Python or inference network access is required.

Upload a PDF/TXT to a chat and use **Refresh documents** to see queued → processing → ready and chunk count. A processing failure shows a safe error code and a Retry processing button. Original downloads remain available. If no worker is running, jobs remain queued.

## Processing contract

- TXT is strict UTF-8. Markdown `#` through `######` lines delimit headings. Plain prose falls back to token windows. PDF uses PdfPig ContentOrderTextExtractor; lines with font size at least 1.25 times the page median are heuristic headings. Page boundaries are retained. Multi-column layouts, tables and ambiguous typography may extract imperfectly. There is no OCR: image-only PDFs fail with `no_text`.
- At most 100 PDF pages, 200,000 extracted characters, and 512 chunks per document. Existing original-size and account quotas still apply.
- Content is limited to **254 tokens**, plus CLS/SEP = 256. Oversized sections are subdivided with exactly **50 content tokens of overlap**, retokenized using the matching tokenizer. No silent truncation. When an exact boundary cannot be represented, processing fails rather than changing the contract. Overlap does not cross heading/page boundaries.
- Chunk offsets refer to the persisted extracted text (UTF-16 offsets), not PDF bytes. PDF heading markers are inserted in that normalized extracted representation. Heading metadata and PDF page numbers accompany each chunk.
- Mean pooling with the attention mask and L2 normalization matches the proven MiniLM experiment. The production copy is independent of the experiments; 39 independent tokenizer fixtures verify parity. Model profile is stored per chunk.
- One worker globally holds a PostgreSQL advisory lock while processing. Jobs survive restart; an abandoned processing job is reclaimed, with three attempts before failure. A per-job random fencing token prevents stale publication. Text, chunks, vectors and ready status publish in one transaction. Failures never publish partial chunks. Retry is owner-checked and HTTP mutations require antiforgery validation.
- Processing checks cancellation between stages/chunks with a five-minute deadline. PdfPig and ONNX calls are synchronous and cannot be forcibly interrupted mid-call; process/container limits provide isolation. A stuck parser requires restarting the worker, after which the durable job is recovered. PDF processing belongs in the separate limited worker, not the web process.
- No approximate vector index is created. Npgsql sends vectors as invariant-culture text parameters cast to `public.vector`, avoiding type-mapping initialization before extension migration. There is no EF Core or new vector adapter dependency.

Example exact query for a future retrieval service (bind authenticated subject server-side; do not accept ownership from the client):

```sql
SELECT c.content,c.heading,c.page_number,
       1-(c.embedding OPERATOR(public.<=>) $1::public.vector) AS similarity
FROM document_chunks c
JOIN chat_documents d ON d.id=c.document_id
JOIN chat_sessions s ON s.id=d.session_id
WHERE s.id=$2 AND s.owner_subject=$3 AND d.processing_status='ready'
ORDER BY c.embedding OPERATOR(public.<=>) $1::public.vector,c.ordinal
LIMIT 5;
```

Cosine similarity is not confidence; nearest-neighbour search returns a result even for unrelated questions. The query is documented/tested, not exposed as a new production API in this milestone.

## Containers / RKE2

The application image supports `dotnet Chatbot.dll --document-worker`. Mount model artifacts read-only and supply the database connection through an existing Secret. Helm adds an opt-in worker via `documentProcessing.enabled=true` and `documentProcessing.modelExistingClaim=<provisioned PVC>`. The PVC must contain `minilm/` and be readable by UID/GID 1654. It runs as UID 1654 with one CPU/768 MiB limits and has distinct labels so web traffic cannot reach it. No model download/init container is introduced. PostgreSQL/extension provisioning is external to this chart.

Dependencies added: PdfPig 0.1.16, Microsoft.ML.OnnxRuntime 1.30.0, Microsoft.ML.Tokenizers 2.0.0. PDF extraction follows the [PdfPig documentation](https://github.com/UglyToad/PdfPig). Tests and measured limitations are recorded in document-processing-results.md.
