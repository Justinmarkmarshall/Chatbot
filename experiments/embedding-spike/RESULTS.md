# Embedding spike results

Measured on 23 September 2026. **Development environment only: no RKE2/homelab measurements were taken.**

## Outcome

Both pinned models successfully generated local 384-dimensional embeddings in a .NET 10 console process, on Windows and in the same Linux runtime image family used by Chatbot. All four final runs returned exit code zero and `Passed: true`.

The Linux runs were non-root, read-only-root containers with networking disabled, read-only model mounts, a one-CPU quota and a 768 MiB memory limit. A separate environment check verified that Python was absent. No external AI service, Ollama or Python inference was involved.

**Recommendation: use all-MiniLM-L6-v2 FP32 as the baseline for the next decision, subject to a homelab run.** It was faster and used less peak process memory than the available E5 FP16 export in these tests. This is a feasibility result, not permission to proceed to ingestion or production integration, and not a general claim that MiniLM is the better retrieval model.

## Actual resource measurements

Times are milliseconds; memory is MiB (1,048,576 bytes). Each final run used a fresh process, one ONNX intra-op thread, sequential execution, five warmups per workload and 50 measured calls. The final four benchmarks were run sequentially. The workstation was not otherwise quiesced, so background host/VM workload and CPU throttling can affect results.

Short workload: 12 repetitions of `document`; long workload: 200 repetitions. Including framing/prefixes, MiniLM inputs were 14/202 tokens and E5 inputs 16/204 tokens. Timings include tokenization, tensor construction, inference, masked mean pooling and normalization. The first-embedding column uses the first sentence from the correctness dataset, rather than the synthetic timing workload.

| Run | Model load ms | First embedding ms | Short median / p95 ms | Long median / p95 ms | Baseline RSS MiB | Loaded RSS MiB | Peak RSS MiB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| windows-minilm | 649.8 | 68.2 | 13.41 / 19.02 | 162.01 / 317.54 | 30.6 | 160.0 | 199.8 |
| windows-e5 | 2,582.1 | 154.9 | 36.07 / 152.24 | 271.06 / 348.97 | 30.6 | 183.8 | 224.1 |
| linux-minilm | 798.4 | 28.7 | 7.83 / 12.30 | 79.77 / 151.66 | 37.9 | 207.9 | 230.7 |
| linux-e5 | 903.8 | 32.7 | 14.85 / 17.24 | 198.18 / 300.84 | 37.9 | 263.1 | 276.7 |

The model-load measurement is session construction only, after artifact hashing and tokenizer setup. Hashing warms the model file cache, so these are **not cold-disk load times**. Tokenizer setup and artifact-verification times are separately available in the raw JSON. Download, image build/pull and container startup are excluded.

Memory is OS-reported process working set, including native ONNX allocations; peak is the process lifetime high-water mark. It is not GC heap size, total container memory, node memory or the future web application's total. The cgroup confirmed `cpu.max=100000 100000` and `memory.max=805306368`. Docker warned that swap limit capabilities were unsupported, so `--memory-swap 768m` must not be interpreted as a verified no-swap environment.

The models use different precision/export profiles: MiniLM upstream `onnx/model.onnx` is FP32, while E5 upstream `onnx/model_O4.onnx` is FP16. Both executed successfully using the CPU package and returned float32 token embeddings. No FP32 E5 export or quantized MiniLM comparison was performed. Differences therefore cannot be attributed solely to model architecture, and the Windows-versus-Linux timings are not a controlled OS comparison.

## Semantic checks

Five fixed query/related/unrelated groups were specified before the measurements. Every group had to satisfy `related cosine - unrelated cosine >= 0.05`. All passed on both platforms. The table below comes from the final Linux reports.

| Model | Group | Related cosine | Unrelated cosine | Margin |
| --- | --- | ---: | ---: | ---: |
| minilm | animals | 0.6516 | -0.0856 | 0.7372 |
| minilm | password | 0.6389 | 0.0487 | 0.5902 |
| minilm | backup | 0.6976 | 0.0317 | 0.6659 |
| minilm | travel | 0.7803 | 0.0646 | 0.7157 |
| minilm | cooking | 0.7289 | 0.0063 | 0.7226 |
| e5 | animals | 0.8614 | 0.6035 | 0.2579 |
| e5 | password | 0.8730 | 0.7000 | 0.1731 |
| e5 | backup | 0.8991 | 0.6724 | 0.2267 |
| e5 | travel | 0.8820 | 0.7131 | 0.1689 |
| e5 | cooking | 0.9016 | 0.7499 | 0.1517 |

E5's higher absolute scores do not indicate higher quality; its score distribution differs. These five easy examples demonstrate semantic separation, not document-retrieval accuracy, Recall@5 or production relevance thresholds.

## Tokenization finding and correctness evidence

Direct use of `Microsoft.ML.Tokenizers.BertTokenizer` 2.0.0 with the initial uncased configuration failed parity on tabs/newlines, emoji and literal added special tokens. For example, the reference treated a tab/newline as word boundaries; the initial implementation joined parts of those words. This was discovered before accepting the spike results.

The final implementation retains the package's **WordPiece engine** and uses explicit Unicode-aware BERT preprocessing in C#: whitespace/control cleaning, CJK/punctuation splitting, lowercasing/accent removal, added-token recognition, CLS/SEP framing and padding. This is scoped to the two pinned tokenizer profiles, not an implementation of arbitrary Hugging Face tokenizers. A future production decision must retain these reference tests and assess wider Unicode coverage; passing fixtures does not prove exhaustive equivalence.

For each model, 39 reference cases were generated independently by Hugging Face `tokenizers==0.22.2` using that model's pinned `tokenizer.json`. They include whitespace, accents, CJK, supplementary-plane characters, literal special tokens, model token boundaries and padding. All 39 passed in each of the four final runs. The .NET implementation was not used to generate expected IDs.

Additional passing checks:

- Output shape `[batch, sequence, 384]`, finite vectors and L2 norm approximately one.
- Attention-mask-aware mean pooling: padded batched versus individual embeddings have cosine above 0.99999.
- Repeatability on repeated text.
- Empty input and over-limit input are rejected, not silently truncated.
- Model and tokenizer artifact SHA-256 verification, pinned fixture/model revisions.
- Native ONNX CPU library loads and performs real inference inside the Linux runtime image.

Independent fixtures cover tokenization, not full reference embedding vectors from PyTorch. Semantic separation and single/batch consistency check the inference pipeline, but this experiment does not claim a full PyTorch-versus-ONNX numerical parity study.

## Environment and reproducibility

- Host: Windows build 26200, Intel64 Family 6 Model 140 Stepping 1; 8 host logical processors.
- Docker: Rancher Desktop / WSL2 Linux x64, kernel `6.18.33.2-microsoft-standard-WSL2`, engine 29.5.3; VM reports 4 CPUs and approximately 7.76 GiB RAM.
- Runtime container: Ubuntu 24.04.5 LTS, .NET 10.0.12; `mcr.microsoft.com/dotnet/aspnet:10.0`, matching Chatbot's base-image family.
- Native host runtime also .NET 10.0.12; local build SDK 10.0.401.
- NuGet: ONNX Runtime 1.30.0, Microsoft.ML.Tokenizers 2.0.0, with transitive versions in `packages.lock.json`.
- MiniLM revision: `1110a243fdf4706b3f48f1d95db1a4f5529b4d41`.
- E5 revision: `ffb93f3bd4047442299a41ebb6fa998a38507c52`.

Artifacts and commands:

- [README.md](README.md): reproducible preparation and offline execution commands.
- [models.json](models.json): source commits, graph precision and SHA-256 hashes.
- [results/environment.json](results/environment.json): image ID, resolved SDK/runtime image digests and test limits.
- [results/container-environment.txt](results/container-environment.txt): OS, absence of Python and cgroup values.
- [results/windows-minilm.json](results/windows-minilm.json), [results/windows-e5.json](results/windows-e5.json): native host measurements.
- [results/linux-minilm.json](results/linux-minilm.json), [results/linux-e5.json](results/linux-e5.json): offline Linux measurements and all latency samples.
- [fixtures/minilm.json](fixtures/minilm.json), [fixtures/e5.json](fixtures/e5.json): independent reference tokenizer output.

## Isolation verification

The only production-project edit for this task is excluding `experiments/**` from SDK default items and explicit Compile/Content/None/EmbeddedResource items. Inspection found zero experiment items in the evaluated web project. The web project builds successfully with an alternate output directory, with zero warnings/errors. Its usual output was locked by the already-running Chatbot process; that process was left running.

No application code, authentication flow, Helm chart, Ollama integration, UI, upload processing, database, worker or RAG feature was changed. Prior unrelated working-tree changes remain untouched. The existing Docker build context is broader than SDK publish items; see README for that distinction.

## Still required on the RKE2 homelab

1. Run this same standalone harness on the actual node CPU/architecture with the pinned artifacts and runtime image identity. Linux x64 development compatibility does not establish ARM64 compatibility.
2. Measure warmed p50/p95 latency and process/container peak memory with the real node's storage and CPU quota. Measure true cold-start behaviour separately if important.
3. Repeat while existing Qwen and usual homelab services are active; measure node memory headroom, CPU throttling and effect on chat responsiveness. Neither simultaneous chat load nor ingestion was tested here.
4. Evaluate realistic document-length text and domain-specific question/passage pairs. These synthetic timing strings and five semantic examples are not a retrieval benchmark.
5. If E5 remains interesting, compare a validated CPU-oriented FP32 or quantized export with equal workloads; do not select two production models based on this spike.

The spike stops here. The evidence supports a local .NET embedding approach and favours MiniLM as the lower-cost baseline in this development environment; deployment capacity and real retrieval quality remain unproven.

