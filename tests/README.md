# Tests

Run the focused unit tests from the Chatbot project directory:

```powershell
./tests/run-unit-tests.ps1
# After restoring packages:
./tests/run-unit-tests.ps1 -NoRestore
```

Or use the cross-platform command:

```sh
dotnet test tests/Chatbot.UnitTests/Chatbot.UnitTests.csproj -p:OutputPath=obj/unit-build/
```

The xUnit project needs .NET 10 and the application's existing NuGet restore credentials. After restore it needs no network, PostgreSQL, Docker, Ollama, or ONNX model files. Its output is separate from the running application's Debug/Release output.

Coverage:

- Token windows: 254/255 boundary, exact 50-token overlap, offsets and coverage, long unbroken text, Unicode surrogate pairs/combining characters, whitespace, impossible progress/overlap, cancellation.
- Prompts: document-only instruction, history order, final question, source metadata, hostile text encoded as JSON, serialized context-size boundary.
- Ollama: request serialization, streamed fragments, final content, missing/null messages, explicit completion, premature EOF, malformed/blank lines, HTTP/server failures, cancellation before and during a request.
- Upload validation: path stripping, filename length/control characters, extensions, empty content, UTF-8, null bytes, PDF signature, inclusive size limit.
- Processing: headings/preamble, adjacent headings, BOM and line-ending offsets, subdivision and embedding calls, text/chunk/page limits, empty text and blank PDFs, page provenance, cancellation. Small generated PDFs exercise PdfPig in-process; no external files are needed.
- Chat orchestration: general chat with no uploads, unavailable uploaded context, grounded prompts, ownership arguments, source persistence before generation, retrieval/persistence failures, partial replies, cancellation, empty generation, checkpoints, final status and turn disposal.

Counting and embedding delegates are synthetic in these tests. They prove windowing and orchestration invariants, not MiniLM tokenization or embedding quality. Real-tokenizer reference fixtures, real model inference, exact vector retrieval, SQL ownership isolation, migrations, locks, worker recovery and deletion cascades remain in `Chatbot.IntegrationTests`.

Run the existing Linux integration suite (requires Docker and the prepared model/fixture directories):

```powershell
dotnet publish tests/Chatbot.IntegrationTests/Chatbot.IntegrationTests.csproj -c Release -p:OutputPath=obj/integration-build/ -o tests/linux-publish
./tests/run-linux-integration.ps1
```

The integration runner starts and stops its own disposable test database. It does not use the application's database. Prompt tests cannot guarantee model grounding; real generation behaviour still needs model evaluation.
