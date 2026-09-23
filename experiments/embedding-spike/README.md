# Local .NET embedding spike

An isolated console experiment. It does not reference Chatbot, start a server, call Ollama or create any document/database infrastructure. The root web project explicitly excludes `experiments/**` from default items and Compile/Content/None/EmbeddedResource items.

Read [RESULTS.md](RESULTS.md) for measured findings and limitations. The checked-in JSON under `results/` contains actual samples, correctness outcomes and environment details; model weights and build outputs are not committed.

## Dependencies

- .NET 10 SDK for building; runtime is entirely .NET.
- `Microsoft.ML.OnnxRuntime` **1.30.0**: CPU execution, native Windows/Linux binaries.
- `Microsoft.ML.Tokenizers` **2.0.0**: WordPiece vocabulary/tokenization engine.
- Docker with Linux x64 containers for the measured container run.
- Python with `tokenizers==0.22.2` **only during preparation/reference generation**. No Python in the runtime image.

`packages.lock.json` locks NuGet transitive packages. `models.json` locks upstream repository commits and SHA-256 hashes for each artifact. The preparation script downloads from those immutable revisions, verifies cached files, and uses Hugging Face's tokenizer implementation to generate independent token-ID, mask and segment-ID fixtures. No inference API is called during preparation or testing.

Downloaded model directories include the upstream model card with its licence declaration (MiniLM Apache-2.0; E5 MIT). For future redistribution, review and carry forward the applicable licence notices. This spike uses upstream ONNX artifacts, not executable remote model code.

## Prepare once

From `experiments/embedding-spike/`, with Python available:

```powershell
python -m venv .venv
.\.venv\Scripts\python.exe -m pip install tokenizers==0.22.2
.\.venv\Scripts\python.exe prepare-models.py
dotnet restore --configfile nuget.config --locked-mode
dotnet build -c Release --no-restore
```

On Linux use `.venv/bin/python` instead. `--resolve` was used only when initially recording upstream commits; normal reproduction must not use it. Preparation can require several hundred MB of downloads and local space. It does not modify the production project or install packages into its environment.

## Run on the development host

Run each model in a separate process, sequentially. The output parent is created if necessary; a failed correctness assertion produces JSON with `Passed:false` and exit code 1. Setup errors such as missing/corrupt files or unsupported graph operations also exit nonzero with an exception.

```powershell
dotnet bin/Release/net10.0/EmbeddingSpike.dll minilm models results/windows-minilm.json
dotnet bin/Release/net10.0/EmbeddingSpike.dll e5 models results/windows-e5.json
```

No network operation or downloader exists in the C# harness. These commands use only local artifacts.

## Run offline in the app's Linux runtime image family

The Dockerfile uses the same `mcr.microsoft.com/dotnet/sdk:10.0` and `mcr.microsoft.com/dotnet/aspnet:10.0` image families as Chatbot. The exact resolved image digests for the recorded measurements are in `results/environment.json`; tags can change on future builds. It runs as the base image's non-root app user.

PowerShell, from the experiment directory:

```powershell
docker build -t chatbot-embedding-spike:local .
New-Item -ItemType Directory -Force results | Out-Null
$modelPath = (Resolve-Path models).Path
$resultPath = (Resolve-Path results).Path
docker run --rm --network none --read-only --cpus 1 --memory 768m --memory-swap 768m --tmpfs /tmp:rw,size=16m --mount "type=bind,source=$modelPath,target=/models,readonly" --mount "type=bind,source=$resultPath,target=/results" chatbot-embedding-spike:local minilm /models /results/linux-minilm.json
docker run --rm --network none --read-only --cpus 1 --memory 768m --memory-swap 768m --tmpfs /tmp:rw,size=16m --mount "type=bind,source=$modelPath,target=/models,readonly" --mount "type=bind,source=$resultPath,target=/results" chatbot-embedding-spike:local e5 /models /results/linux-e5.json
```

Equivalent Linux shell (ensure the non-root container UID can write `results/`):

```sh
mkdir -p results
docker build -t chatbot-embedding-spike:local .
docker run --rm --network none --read-only --cpus 1 --memory 768m --memory-swap 768m --tmpfs /tmp:rw,size=16m \
  --mount "type=bind,source=$PWD/models,target=/models,readonly" \
  --mount "type=bind,source=$PWD/results,target=/results" \
  chatbot-embedding-spike:local minilm /models /results/linux-minilm.json
# Repeat the same command with e5 and /results/linux-e5.json.
```

Image build and preparation can access the internet. The benchmark container cannot: its network mode is `none`. Models are mounted read-only; Python, pip, model downloading and the application source are absent from the runtime image. The recorded run also verified Python was absent using `command -v`.

## Correctness checks

- 39 independently generated tokenizer cases per model, including case folding, accents, CJK/astral characters, punctuation, literal special tokens, whitespace/control characters, exact model limit, over-limit and padding.
- Both pinned models use the same uncased BERT vocabulary and preprocessing conventions. The harness verifies supported tokenizer metadata and artifact hashes before using explicit preprocessing; it is not a generic tokenizer.json interpreter.
- Direct `BertTokenizer` 2.0.0 did not match the reference for several edge cases. The spike therefore uses its `WordPieceTokenizer` with explicit Unicode-aware BERT cleaning, punctuation/CJK splitting, added tokens, normalization and special-token framing. The fixture tests remain the acceptance gate. Wider Unicode/model coverage is not claimed.
- Input IDs/masks/types are Int64 tensors; actual graph metadata is inspected. Output must have `[batch, sequence, 384]` shape.
- Masked mean pooling and L2 normalization, finite values, 384 dimensions and unit norm.
- Five fixed query/related/unrelated groups. The predeclared acceptance margin is related cosine minus unrelated cosine **at least 0.05 for every group**.
- E5 uses `query:` and `passage:` prefixes; MiniLM uses neither. Raw scores are not calibrated or comparable across model families.
- Single versus padded-batch cosine greater than 0.99999, repeatability and rejection of empty/oversized inference input. Tokenizer reference tests may include empty text even though embedding admission rejects it.

## Measurement method

Each model runs in a fresh process with one intra-op and one inter-op thread, sequential execution and spinning disabled. CPU execution is the only enabled provider. Model initialization and first complete embedding are measured separately. Warm latency includes .NET tokenization, tensor creation, ONNX inference, mean pooling and normalization; it excludes printing and JSON serialization.

For each of two synthetic workloads (12 and 200 repetitions of `document`), do five warmups and 50 measured batch-one calls, recording every sample. Report median and nearest-rank p95. MiniLM token counts are 14/202; E5 adds its query prefix. These are controlled sequence lengths, not representative document workloads.

Memory is process working set at baseline, after loading and at the end, plus OS process peak working set. It includes native ONNX allocation, not just the GC heap. It is not total node usage or container cgroup peak. Hash verification precedes model load and warms the file cache: load timings are not cold-storage/disk benchmarks. Preparation, downloads, Docker pull/build and startup of the container are excluded from inference timings.

## Recheck web-project isolation

From the repository root:

```powershell
$items = dotnet msbuild Chatbot.csproj -getItem:Compile,Content,None,EmbeddedResource | ConvertFrom-Json
$leaks = @($items.Items.Compile + $items.Items.Content + $items.Items.None + $items.Items.EmbeddedResource | Where-Object { $_.Identity -match '^experiments[/\\]' })
if ($leaks.Count) { throw 'Experiment leaked into the web project' }
dotnet build Chatbot.csproj --no-restore -p:OutputPath=obj/spike-web-check/
```

The alternate output avoids overwriting an app running locally. Experiments are excluded from application build/publish items, but the existing production Dockerfile's broad `COPY .` can still include local experiment files in its **build-stage context**. No production Dockerfile or ignore-file change is made by this spike; do not mistake SDK publish isolation for Docker context exclusion.
