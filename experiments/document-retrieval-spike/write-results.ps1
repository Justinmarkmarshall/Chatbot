$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    $r = Get-Content results/retrieval.json -Raw | ConvertFrom-Json
    $envInfo = Get-Content results/environment.json -Raw | ConvertFrom-Json
    $observations = Get-Content failure-observations.json -Raw | ConvertFrom-Json -AsHashtable
    function F($v) { if ($null -eq $v) { return 'none' }; return ([double]$v).ToString('F4', [Globalization.CultureInfo]::InvariantCulture) }
    function P($v) { return (100 * [double]$v).ToString('F1', [Globalization.CultureInfo]::InvariantCulture) + '%' }
    $lines = [Collections.Generic.List[string]]::new()
    $lines.Add('# Document retrieval results')
    $lines.Add('')
    $lines.Add("Documents: $($r.DocumentCount); words: $($r.WordCount); questions: $($r.QuestionCount) ($($r.AnswerableCount) answerable, $($r.NoAnswerCount) no-answer). Model: all-MiniLM-L6-v2 FP32; dimensions: 384; search: exact cosine.")
    $lines.Add('')
    $lines.Add("Measured at $($r.TimestampUtc.ToUniversalTime().ToString('o')) in a Linux development container on the Windows/WSL2 host. Valid experiment: **$($r.ValidExperiment)**. Initial Recall@3 >= 90% target reached: **$($r.QualityTargetReached)**. No RKE2 measurements were made.")
    $lines.Add('')
    $lines.Add('## Retrieval quality')
    $lines.Add('')
    $lines.Add('| Strategy | Chunks | Min / median / p95 / max content tokens | Recall@1 | Recall@3 | Recall@5 | MRR |')
    $lines.Add('| --- | ---: | --- | ---: | ---: | ---: | ---: |')
    foreach ($s in $r.Strategies) { $lines.Add("| $($s.Strategy) | $($s.ChunkCount) | $($s.MinTokens) / $($s.MedianTokens) / $($s.P95Tokens) / $($s.MaxTokens) | $(P $s.Recall1) | $(P $s.Recall3) | $(P $s.Recall5) | $(F $s.Mrr) |") }
    $lines.Add('')
    $lines.Add('Recall uses the requested at-least-one relevant chunk definition (hit rate). MRR uses complete rankings. All evidence spans must fit in one chunk; absent complete chunks score zero. Only answerable questions enter these metrics. The agreed content cap is 254 plus two special tokens; subdivisions overlap by 50 content tokens. There was no 400-token run.')
    $lines.Add('')
    $lines.Add('## Development-container latency')
    $lines.Add('')
    $lines.Add('| Strategy | Embedding median / p95 ms | Database median / p95 ms | End-to-end median / p95 ms | Measured searches |')
    $lines.Add('| --- | ---: | ---: | ---: | ---: |')
    foreach ($s in $r.Strategies) { $lines.Add("| $($s.Strategy) | $(F $s.Embedding.Median) / $(F $s.Embedding.P95) | $(F $s.Database.Median) / $(F $s.Database.P95) | $(F $s.EndToEnd.Median) / $(F $s.EndToEnd.P95) | $($s.EndToEnd.Count) |") }
    $lines.Add('')
    $lines.Add("Five warmups and 50 samples per question per strategy; top five fully materialized; one reused connection outside timing. Model startup: $(F $r.ModelStartupMs) ms (artifact verification $(F $r.ArtifactVerificationMs), tokenizer $(F $r.TokenizerLoadMs), ONNX load $(F $r.ModelLoadMs)). All-strategy corpus embedding: $(F $r.FullCorpusEmbeddingMs) ms; insertion: $(F $r.InsertMs) ms; chunks inserted: $($r.TotalChunksInserted). Chunking and validation: $(F $r.ChunkingAndValidationMs) ms.")
    $lines.Add('')
    $lines.Add("Runtime: $($r.Runtime); $($r.OS); $($r.Architecture). Host CPU: $($envInfo.HostProcessor). Docker: $($envInfo.DockerOS); kernel $($envInfo.Kernel). Runner and database each limited to one CPU; runner 768 MiB, database 512 MiB. Runner peak RSS: $(F ($r.ProcessPeakRssBytes/1MB)) MiB. These sequential tiny-corpus timings do not measure production throughput or capacity.")
    $lines.Add('Docker reported memory limits without swap-limit support on this WSL2 kernel; no swap cap is claimed.')
    $memory = @(Get-Content results/postgres-memory.json -Raw | ConvertFrom-Json | Where-Object Available)
    if ($memory.Count -gt 0) {
        $lines.Add('')
        $lines.Add("PostgreSQL cgroup memory: first idle snapshot $(F ($memory[0].CurrentBytes/1MB)) MiB; maximum sampled while runner active $(F ((($memory | Where-Object Phase -eq 'during-runner' | Measure-Object CurrentBytes -Maximum).Maximum)/1MB)) MiB; kernel lifetime peak including initialization $(F ((($memory | Measure-Object KernelLifetimePeakBytes -Maximum).Maximum)/1MB)) MiB. Samples include cgroup-accounted cache, not only PostgreSQL process RSS; sparse sampling can miss peaks.")
    }
    $lines.Add('')
    $lines.Add('## Validity and exact search')
    $lines.Add('')
    $lines.Add("Original MiniLM regression: $($r.OriginalMiniLmRegressionPassed), $($r.RegressionFixtures) tokenizer fixtures. Chunk determinism, full source coverage, exact overlap, token caps, oversized subdivision, all provenance roundtrips and finite 384-dimensional vector roundtrips passed: $($r.ValidExperiment). Independent double cosine comparisons: $($r.CosineComparisons); maximum absolute difference: $($r.MaximumCosineDistanceDifference), tolerance 1e-5.")
    $lines.Add('')
    $lines.Add("PostgreSQL: $($r.PostgresVersion). pgvector: $($r.PgvectorVersion). Indexes:")
    $lines.Add('')
    $lines.Add('```sql')
    foreach ($index in $r.Indexes) { $lines.Add($index.Definition) }
    $lines.Add('```')
    foreach ($s in $r.Strategies) {
        $lines.Add(''); $lines.Add("Representative exact-search plan: $($s.Strategy), first evaluation question, LIMIT 5.")
        $lines.Add(''); $lines.Add('```text')
        foreach ($line in $s.Questions[0].ExplainAnalyzeBuffers) { $lines.Add(($line -replace "'\[[^']+\]'::vector", "'[384 values; full literal in retrieval.json]'::vector")) }
        $lines.Add('```')
    }
    $lines.Add(''); $lines.Add('## Failures and score observations'); $lines.Add('')
    $rankings = [Collections.Generic.List[string]]::new()
    $failureLines = [Collections.Generic.List[string]]::new()
    $rankings.Add('# Question rankings'); $rankings.Add(''); $rankings.Add('Top five shown below. All ranks and source text, raw latency samples, evidence spans and plans are preserved in retrieval.json. Offsets are UTF-16, zero-based, end exclusive.')
    $failureLines.Add('# Every answerable top-one failure'); $failureLines.Add(''); $failureLines.Add('A top-one miss is retained even when Recall@3 succeeds. Classifications are conservative diagnostics, not revised labels. Boundary split can describe a truncated top result or the absence of any complete relevant chunk; each observation distinguishes them. Manual observations supersede the runner''s preliminary unknown diagnosis without changing any metric or relevance label.')
    foreach ($s in $r.Strategies) {
        $misses = @($s.Questions | Where-Object { $_.Case.Answerable -and $_.FirstRelevantRank -ne 1 })
        $noComplete = @($misses | Where-Object { $null -eq $_.FirstRelevantRank })
        $caseSummary = if ($misses.Count -eq 0) { 'none' } else { ($misses | ForEach-Object { $_.Case.Id + ' (first relevant ' + $(if ($null -eq $_.FirstRelevantRank) {'none'} else {$_.FirstRelevantRank}) + ')' }) -join ', ' }
        $lines.Add("- $($s.Strategy): $($misses.Count)/$($r.AnswerableCount) top-one misses; $($noComplete.Count) questions have no complete relevant chunk. Cases: $caseSummary.")
        foreach ($q in $s.Questions) {
            $heading = "## $($s.Strategy) / $($q.Case.Id)"
            $rankings.Add(''); $rankings.Add($heading); $rankings.Add(''); $rankings.Add($q.Case.Question); $rankings.Add('')
            $rankings.Add("Answerable: $($q.Case.Answerable). Expected: $($q.Case.ExpectedDocument) / $($q.Case.ExpectedSection). First relevant rank: $(if ($null -eq $q.FirstRelevantRank) {'none'} else {$q.FirstRelevantRank}); similarity: $(F $q.FirstRelevantSimilarity); highest irrelevant similarity: $(F $q.HighestIrrelevantSimilarity).")
            $rankings.Add(''); $rankings.Add('| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |'); $rankings.Add('| ---: | --- | --- | --- | ---: | ---: | ---: | --- |')
            foreach ($hit in ($q.FullRanking | Select-Object -First 5)) {
                $rankings.Add("| $($hit.Rank) | $($hit.DocumentId) / $($hit.ChunkIndex) | $($hit.Heading.Replace('|','/')) | $($hit.StartOffset)..$($hit.EndOffset) | $($hit.TokenCount) | $(F $hit.CosineDistance) | $(F $hit.Similarity) | $($q.RelevantIds -contains $hit.Id) |")
            }
            if ($q.Case.Answerable -and $q.FirstRelevantRank -ne 1) {
                $observation = $observations["$($s.Strategy)/$($q.Case.Id)"]
                $diagnosis = if ($null -ne $observation) { $observation.classification } else { $q.Diagnosis }
                $failureLines.Add(''); $failureLines.Add($heading); $failureLines.Add(''); $failureLines.Add($q.Case.Question); $failureLines.Add('')
                $failureLines.Add("Expected document/section: $($q.Case.ExpectedDocument) / $($q.Case.ExpectedSection). First relevant rank: $(if ($null -eq $q.FirstRelevantRank) {'none'} else {$q.FirstRelevantRank}). Diagnostic: **$diagnosis**.")
                if ($null -ne $observation) { $failureLines.Add(''); $failureLines.Add($observation.observation) }
                $failureLines.Add(''); $failureLines.Add('Required evidence:')
                foreach ($e in $q.Case.Evidence) { $failureLines.Add(''); $failureLines.Add('> '+$e) }
                $failureLines.Add(''); $failureLines.Add("Evidence spans: $(($q.EvidenceSpans | ForEach-Object { "$($_.Start)..$($_.End)" }) -join ', ').")
                $selected = @($q.FullRanking | Select-Object -First 5)
                if ($null -ne $q.FirstRelevantRank -and $q.FirstRelevantRank -gt 5) { $selected += $q.FullRanking[$q.FirstRelevantRank-1] }
                foreach ($hit in $selected) {
                    $failureLines.Add(''); $failureLines.Add("### Rank $($hit.Rank): $($hit.DocumentId) / chunk $($hit.ChunkIndex)"); $failureLines.Add('')
                    $failureLines.Add("Heading: $($hit.Heading). Offsets: $($hit.StartOffset)..$($hit.EndOffset). Tokens: $($hit.TokenCount). Distance: $(F $hit.CosineDistance). Similarity: $(F $hit.Similarity). Relevant: $($q.RelevantIds -contains $hit.Id).")
                    $failureLines.Add(''); $failureLines.Add('```text'); $failureLines.Add($hit.SourceText); $failureLines.Add('```')
                }
            }
        }
    }
    $lines.Add(''); $lines.Add('No-answer questions still return neighbours:'); $lines.Add('')
    $lines.Add('| Question | Strategy | Top document | Top similarity |'); $lines.Add('| --- | --- | --- | ---: |')
    foreach ($s in $r.Strategies) { foreach ($q in ($s.Questions | Where-Object { !$_.Case.Answerable })) { $lines.Add("| $($q.Case.Id) | $($s.Strategy) | $($q.FullRanking[0].DocumentId) | $(F $q.FullRanking[0].Similarity) |") } }
    $relevantScores = @($r.Strategies | ForEach-Object { $_.Questions } | Where-Object { $_.Case.Answerable -and $null -ne $_.FirstRelevantSimilarity } | ForEach-Object FirstRelevantSimilarity | Measure-Object -Minimum -Maximum)[0]
    $irrelevantScores = @($r.Strategies | ForEach-Object { $_.Questions } | Where-Object { $_.Case.Answerable } | ForEach-Object HighestIrrelevantSimilarity | Measure-Object -Minimum -Maximum)[0]
    $lines.Add(''); $lines.Add("Across answerable cases/strategies, first relevant similarity ranges $(F $relevantScores.Minimum) to $(F $relevantScores.Maximum); highest irrelevant similarity ranges $(F $irrelevantScores.Minimum) to $(F $irrelevantScores.Maximum). These observed ranges do not define a cutoff. Similarity is not confidence or evidence that an answer exists.")
    $lines.Add(''); $lines.Add('See [all question rankings](results/rankings.md), [every top-one failure with source text and boundaries](results/failures.md), and [full machine-readable evidence](results/retrieval.json). Inputs and expectations were not revised after retrieval.')
    $lines.Add(''); $lines.Add('Manual inspection finds a genuine two-paragraph evidence split for incident closure, nearby procedural distractors for custom restore / rollback / session revocation, and mixed-topic windows for link expiry. Two fixed-window misses also reveal strict-label limitations: the custom-archive result already names pg_restore but loses the end of the evidence sentence; the train result omits the leading word contact. Their frozen scores remain unchanged. These are conservative evidence-span metrics, not independently adjudicated human relevance judgments. Detailed observations are separate from frozen labels in failure-observations.json.')
    $lines.Add(''); $lines.Add('## Interpretation and recommendation'); $lines.Add('')
    $best = $r.Strategies | Sort-Object @{Expression='Recall3';Descending=$true},@{Expression='Mrr';Descending=$true} | Select-Object -First 1
    $lines.Add("The strongest strategy on this frozen corpus by Recall@3, then MRR, is **$($best.Strategy)**. The result supports comparing chunk structure before considering production integration; it does not establish a universally best chunk size. Exact search remains measurable at this small dataset size, with the latency above rather than an extrapolated capacity claim.")
    $lines.Add(''); $lines.Add('Review the documented misses and evidence boundaries before choosing another experiment. Keep the MiniLM FP32 and exact-search baseline for that decision. A synthetic corpus, only 22 answerable cases, strict single-chunk evidence labels, one fixed strategy order and one sequential development run limit generalization. No real-document or RKE2 evaluation, answer synthesis, or production integration was performed. No further experiment has been started.')
    $lines.Add(''); $lines.Add('Recommendation: retain heading-aware chunking as the leading candidate, and make any later evaluation focus on independently labelled real sections and boundary cases before adding retrieval complexity. All natural sections in this corpus fit within 177 content tokens, so oversized heading-section retrieval was not evaluated; only the synthetic correctness fixture exercises that subdivision. Heading-aware results also include more context than paragraphs, so this run does not isolate heading text from chunk size. Exact 50-token overlap can start inside a word; those fragments are preserved in the ranked evidence. The perfect heading-aware score on this small authored set is not a production quality claim.')
    if (Test-Path results/preservation-check.json) {
        $audit = Get-Content results/preservation-check.json -Raw | ConvertFrom-Json
        $lines.Add(''); $lines.Add("Previous-spike preservation audit: $($audit.AllUnchanged), $($audit.CheckedFiles) files checked. See [audit](results/preservation-check.json).")
    }
    $lines | Set-Content RESULTS.md
    $rankings | Set-Content results/rankings.md
    $failureLines | Set-Content results/failures.md
} finally { Pop-Location }
