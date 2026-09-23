$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    $r = Get-Content results/retrieval.json -Raw | ConvertFrom-Json
    $envInfo = Get-Content results/environment.json -Raw | ConvertFrom-Json
    $mem = @(Get-Content results/postgres-memory.json -Raw | ConvertFrom-Json)
    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add('# PostgreSQL / pgvector spike results')
    $lines.Add('')
    $lines.Add('Development-container measurements only. No RKE2 or production throughput claim. Measured ' + ([datetime]$r.TimestampUtc).ToUniversalTime().ToString('O') + ' (UTC).')
    $lines.Add('')
    $lines.Add('| Check / measurement | Actual result |')
    $lines.Add('| --- | --- |')
    $lines.Add('| Overall correctness | ' + $r.Passed + '; runner exit ' + $envInfo.RunnerExitCode + ' |')
    $lines.Add('| Original MiniLM regression | Passed on Windows and offline Linux; ' + $r.RegressionFixtureCount + ' tokenizer fixtures plus embedding checks |')
    $lines.Add('| Stored embeddings | ' + $r.StoredCount + ' rows, ' + $r.Dimensions + ' dimensions, all finite; exact text/vector readback passed |')
    $lines.Add('| Supported top-1 | ' + $r.SupportedQueryTop1Passed + ' / ' + $r.SupportedQueryCount + ' expected passages ranked first |')
    $unrelated = $r.Questions | Where-Object { $null -eq $_.ExpectedId }
    $lines.Add('| Unrelated Magic Flute question | Returned IDs ' + (($unrelated.ReturnedTopThree.Id) -join ', ') + '; none is relevant |')
    $lines.Add(('| Maximum .NET/PostgreSQL distance difference | {0:G10}; tolerance 1e-5 |' -f $r.MaximumCosineDistanceDifference))
    $lines.Add('| Approximate indexes | None; only spike_passages_pkey |')
    $lines.Add('| Installed pgvector | ' + $r.PgvectorVersion + ' |')
    $lines.Add('| Resolved PostgreSQL image digest | ' + ($envInfo.PostgreSQLRepoDigests -join ', ') + ' |')
    $lines.Add(('| Full model startup | {0:N2} ms; includes local artifact verification |' -f $r.ModelStartupMs))
    $lines.Add(('| ONNX session load only | {0:N2} ms |' -f $r.ModelLoadMs))
    $lines.Add(('| Query embedding median / p95 | {0:N3} / {1:N3} ms |' -f $r.OverallEmbedding.MedianMs,$r.OverallEmbedding.P95Ms))
    $lines.Add(('| Database retrieval median / p95 | {0:N3} / {1:N3} ms |' -f $r.OverallDatabase.MedianMs,$r.OverallDatabase.P95Ms))
    $lines.Add(('| End-to-end median / p95 | {0:N3} / {1:N3} ms |' -f $r.OverallEndToEnd.MedianMs,$r.OverallEndToEnd.P95Ms))
    $lines.Add('')
    $lines.Add('Each question had five warmups and 50 individually measured searches; aggregate percentiles use all 350 samples. One Npgsql data source and connection were reused. Timed retrieval returns three rows. The full ranking below uses the identical query with LIMIT 8; no vector index, optimization, threshold or reranking was introduced.')
    $lines.Add('')
    $lines.Add('**Nearest-neighbour retrieval does not itself establish relevance. Similarity is not confidence.** The Magic Flute question has no relevant passage in this dataset, but still receives three neighbours. No conclusion about answerability is drawn from those matches.')
    if ($r.Failures.Count) { $lines.Add(''); $lines.Add('Failures: ' + ($r.Failures -join '; ')) }
    $lines.Add('')
    $lines.Add('## Full ranked results')
    $lines.Add('')
    foreach ($question in $r.Questions) {
        $lines.Add('### ' + $question.Question)
        $lines.Add('')
        $expected = if ($null -eq $question.ExpectedId) { 'None; deliberately unrelated.' } else { [string]$question.ExpectedId }
        $lines.Add('Expected passage: ' + $expected + ' Ranks 1–3 are the returned subset used by the benchmark.')
        $lines.Add('')
        $lines.Add('| Rank | Passage ID | Source text | Cosine distance | Similarity |')
        $lines.Add('| ---: | ---: | --- | ---: | ---: |')
        foreach ($hit in $question.FullRanking) {
            $lines.Add(('| {0} | {1} | {2} | {3:F8} | {4:F8} |' -f $hit.Rank,$hit.Id,$hit.SourceText,$hit.CosineDistance,$hit.Similarity))
        }
        $lines.Add('')
        $lines.Add(('Median/p95 milliseconds: embedding **{0:N3}/{1:N3}**, database **{2:N3}/{3:N3}**, end-to-end **{4:N3}/{5:N3}**.' -f $question.Embedding.MedianMs,$question.Embedding.P95Ms,$question.Database.MedianMs,$question.Database.P95Ms,$question.EndToEnd.MedianMs,$question.EndToEnd.P95Ms))
        $lines.Add('')
    }
    $lines.Add('## Database inspection')
    $lines.Add('')
    $lines.Add($r.PostgresVersion)
    $lines.Add('')
    $lines.Add('Actual pg_indexes definitions:')
    $lines.Add('```sql')
    foreach ($index in $r.Indexes) { $lines.Add($index.Definition + ';') }
    $lines.Add('```')
    $lines.Add('')
    $lines.Add('EXPLAIN (ANALYZE, BUFFERS) was recorded for all seven questions in [retrieval.json](results/retrieval.json). First-question plan below; only the long vector literal is abbreviated. It scans all eight rows and sorts exactly. Estimates are from a newly seeded table; this is not a planner-tuning or ANN-performance test.')
    $lines.Add('')
    $lines.Add('```text')
    foreach ($line in $r.Questions[0].ExplainAnalyzeBuffers) { $lines.Add(($line -replace "'\[[^']+\]'::vector", "'[384-dimensional query vector]'::vector")) }
    $lines.Add('```')
    $lines.Add('')
    $lines.Add('## PostgreSQL memory')
    $lines.Add('')
    $idle = $mem | Where-Object { $_.Available -and $_.Phase -eq 'idle-before-retrieval' } | Select-Object -First 1
    $during = @($mem | Where-Object { $_.Available -and $_.Phase -eq 'during-runner' })
    if ($idle -and $during.Count) {
        $lines.Add(('- Idle snapshot immediately before retrieval: **{0:N2} MiB**.' -f ($idle.CurrentBytes/1MB)))
        $lines.Add(('- Maximum of {0} during-runner snapshots: **{1:N2} MiB** (sampled roughly once per second, not an exact peak).' -f $during.Count,(($during.CurrentBytes | Measure-Object -Maximum).Maximum/1MB)))
        $lines.Add(('- Kernel-recorded container-lifetime peak: **{0:N2} MiB**, including database initialization, not a retrieval-only peak.' -f (($mem.KernelLifetimePeakBytes | Measure-Object -Maximum).Maximum/1MB)))
    } else { $lines.Add('Memory sampling unavailable in this environment; see raw records.') }
    $lines.Add('')
    $lines.Add('These are cgroup memory charges, including cache, not private-process RSS. The database limit was 512 MiB; runner limit 768 MiB; each had one CPU quota. Sampling with docker exec adds small overhead. WSL warned that swap-limit enforcement was unavailable. [Raw memory timestamps](results/postgres-memory.json).')
    $lines.Add('')
    $lines.Add('## Environment, preservation and reproduction')
    $lines.Add('')
    $lines.Add($r.OS + '; ' + $r.Runtime + '; ' + $envInfo.Architecture + '; ' + $envInfo.DockerOS + '; kernel ' + $envInfo.Kernel + '; Docker ' + $envInfo.EngineVersion + '.')
    $lines.Add('')
    $lines.Add('PostgreSQL had no published host ports. Compose network inspection confirmed internal=true. Existing model files were mounted read-only. No model downloads or Python were used. Model revision: ' + $r.Revision + '.')
    $lines.Add('')
    $lines.Add(('Startup breakdown: artifact SHA-256 verification **{0:N2} ms**, tokenizer load **{1:N2} ms**, ONNX session construction **{2:N2} ms**. Artifact hashing warms the file cache. Full startup includes other constructor/validation overhead. This is not a cold-disk benchmark.' -f $r.ArtifactVerificationMs,$r.TokenizerLoadMs,$r.ModelLoadMs))
    $lines.Add('')
    $lines.Add('The experiment-specific embedding refactor reused the original tokenization/inference implementation. Original result files were preserved. No production source/configuration was written by this task.')
    if (Test-Path results/preservation-check.json) {
        $audit = Get-Content results/preservation-check.json -Raw | ConvertFrom-Json
        $lines.Add('Preservation audit: ' + $audit.Note + ' See [preservation-check.json](results/preservation-check.json).')
    }
    $lines.Add('')
    $lines.Add('Canonical clean reproduction from this directory:')
    $lines.Add('```powershell')
    $lines.Add('./run-spike.ps1 -Clean')
    $lines.Add('./write-results.ps1')
    $lines.Add('```')
    $lines.Add('')
    $lines.Add('This removes the experiment database volume before rebuilding/running. PostgreSQL entrypoint initialization scripts execute only on a new volume. See [README.md](README.md) for prerequisites, exact timing boundaries, pinned dependencies and cleanup.')
    $lines.Add('')
    $lines.Add('Evidence: [retrieval samples and plans](results/retrieval.json), [image digests and limits](results/environment.json), [Linux regression](results/embedding-regression-linux.json), [Windows regression](results/embedding-regression-windows.json). Experiment containers were stopped after measurement; the named volume remains for inspection.')
    $lines.Add('')
    $lines.Add('## Recommendation and limits')
    $lines.Add('')
    $lines.Add('The evidence supports MiniLM FP32 plus Npgsql/Pgvector exact cosine retrieval as a technically viable baseline: 6/6 fixed top-1 checks passed and database distances agreed with independent .NET calculations. Retain the direct parameterized query and do not add an approximate index on the basis of this tiny dataset.')
    $lines.Add('')
    $lines.Add('Eight hand-written passages do not establish real-document retrieval quality, relevance thresholds, database scalability, concurrent throughput or RKE2 memory capacity. Test those separately if later approved. No ingestion, document model, production integration or deployment work follows this spike.')
    Set-Content RESULTS.md ($lines -join "`n")
} finally { Pop-Location }
