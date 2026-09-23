# Document retrieval results

Documents: 10; words: 6509; questions: 26 (22 answerable, 4 no-answer). Model: all-MiniLM-L6-v2 FP32; dimensions: 384; search: exact cosine.

Measured at 2026-09-23T12:06:03.0482763Z in a Linux development container on the Windows/WSL2 host. Valid experiment: **True**. Initial Recall@3 >= 90% target reached: **True**. No RKE2 measurements were made.

## Retrieval quality

| Strategy | Chunks | Min / median / p95 / max content tokens | Recall@1 | Recall@3 | Recall@5 | MRR |
| --- | ---: | --- | ---: | ---: | ---: | ---: |
| paragraph | 116 | 8 / 74 / 91 / 100 | 81.8% | 95.5% | 95.5% | 0.8788 |
| fixed-254-50 | 40 | 133 / 254 / 254 / 254 | 86.4% | 100.0% | 100.0% | 0.9242 |
| heading-aware | 59 | 72 / 144 / 174 / 177 | 100.0% | 100.0% | 100.0% | 1.0000 |

Recall uses the requested at-least-one relevant chunk definition (hit rate). MRR uses complete rankings. All evidence spans must fit in one chunk; absent complete chunks score zero. Only answerable questions enter these metrics. The agreed content cap is 254 plus two special tokens; subdivisions overlap by 50 content tokens. There was no 400-token run.

## Development-container latency

| Strategy | Embedding median / p95 ms | Database median / p95 ms | End-to-end median / p95 ms | Measured searches |
| --- | ---: | ---: | ---: | ---: |
| paragraph | 7.4985 / 9.9374 | 1.4459 / 2.1164 | 9.0630 / 11.7001 | 1300 |
| fixed-254-50 | 7.4565 / 10.3089 | 1.1740 / 1.7264 | 8.7179 / 11.7988 | 1300 |
| heading-aware | 7.4631 / 9.9443 | 1.2338 / 1.7935 | 8.7611 / 11.4577 | 1300 |

Five warmups and 50 samples per question per strategy; top five fully materialized; one reused connection outside timing. Model startup: 6238.8528 ms (artifact verification 5298.2846, tokenizer 48.8416, ONNX load 771.6020). All-strategy corpus embedding: 10208.5468 ms; insertion: 215.4208 ms; chunks inserted: 215. Chunking and validation: 2555.9683 ms.

Runtime: .NET 10.0.12; Ubuntu 24.04.5 LTS; X64. Host CPU: Intel64 Family 6 Model 140 Stepping 1, GenuineIntel. Docker: Rancher Desktop WSL Distribution; kernel 6.18.33.2-microsoft-standard-WSL2. Runner and database each limited to one CPU; runner 768 MiB, database 512 MiB. Runner peak RSS: 283.8516 MiB. These sequential tiny-corpus timings do not measure production throughput or capacity.
Docker reported memory limits without swap-limit support on this WSL2 kernel; no swap cap is claimed.

PostgreSQL cgroup memory: first idle snapshot 80.7031 MiB; maximum sampled while runner active 73.8828 MiB; kernel lifetime peak including initialization 94.2734 MiB. Samples include cgroup-accounted cache, not only PostgreSQL process RSS; sparse sampling can miss peaks.

## Validity and exact search

Original MiniLM regression: True, 39 tokenizer fixtures. Chunk determinism, full source coverage, exact overlap, token caps, oversized subdivision, all provenance roundtrips and finite 384-dimensional vector roundtrips passed: True. Independent double cosine comparisons: 5590; maximum absolute difference: 1.27371168501256E-07, tolerance 1e-5.

PostgreSQL: PostgreSQL 17.11 (Debian 17.11-1.pgdg12+2) on x86_64-pc-linux-gnu, compiled by gcc (Debian 12.2.0-14+deb12u1) 12.2.0, 64-bit. pgvector: 0.8.6. Indexes:

```sql
CREATE UNIQUE INDEX document_chunks_document_id_chunk_strategy_chunk_index_key ON public.document_chunks USING btree (document_id, chunk_strategy, chunk_index)
CREATE UNIQUE INDEX document_chunks_pkey ON public.document_chunks USING btree (id)
```

Representative exact-search plan: paragraph, first evaluation question, LIMIT 5.

```text
Limit  (cost=34.48..34.50 rows=5 width=682) (actual time=0.489..0.490 rows=5 loops=1)
  Buffers: shared hit=305
  ->  Sort  (cost=34.48..34.77 rows=116 width=682) (actual time=0.487..0.488 rows=5 loops=1)
        Sort Key: ((embedding <=> '[384 values; full literal in retrieval.json]'::vector)), id
        Sort Method: top-N heapsort  Memory: 31kB
        Buffers: shared hit=305
        ->  Seq Scan on document_chunks  (cost=0.00..32.56 rows=116 width=682) (actual time=0.039..0.431 rows=116 loops=1)
              Filter: (chunk_strategy = 'paragraph'::text)
              Rows Removed by Filter: 99
              Buffers: shared hit=305
Planning Time: 0.105 ms
Execution Time: 0.513 ms
```

Representative exact-search plan: fixed-254-50, first evaluation question, LIMIT 5.

```text
Limit  (cost=32.65..32.66 rows=5 width=682) (actual time=0.284..0.285 rows=5 loops=1)
  Buffers: shared hit=189
  ->  Sort  (cost=32.65..32.75 rows=40 width=682) (actual time=0.283..0.284 rows=5 loops=1)
        Sort Key: ((embedding <=> '[384 values; full literal in retrieval.json]'::vector)), id
        Sort Method: top-N heapsort  Memory: 37kB
        Buffers: shared hit=189
        ->  Seq Scan on document_chunks  (cost=0.00..31.99 rows=40 width=682) (actual time=0.045..0.254 rows=40 loops=1)
              Filter: (chunk_strategy = 'fixed-254-50'::text)
              Rows Removed by Filter: 175
              Buffers: shared hit=189
Planning Time: 0.070 ms
Execution Time: 0.299 ms
```

Representative exact-search plan: heading-aware, first evaluation question, LIMIT 5.

```text
Limit  (cost=33.11..33.12 rows=5 width=682) (actual time=0.329..0.330 rows=5 loops=1)
  Buffers: shared hit=265
  ->  Sort  (cost=33.11..33.26 rows=59 width=682) (actual time=0.328..0.328 rows=5 loops=1)
        Sort Key: ((embedding <=> '[384 values; full literal in retrieval.json]'::vector)), id
        Sort Method: top-N heapsort  Memory: 33kB
        Buffers: shared hit=265
        ->  Seq Scan on document_chunks  (cost=0.00..32.13 rows=59 width=682) (actual time=0.042..0.298 rows=59 loops=1)
              Filter: (chunk_strategy = 'heading-aware'::text)
              Rows Removed by Filter: 156
              Buffers: shared hit=265
Planning Time: 0.061 ms
Execution Time: 0.342 ms
```

## Failures and score observations

- paragraph: 4/22 top-one misses; 1 questions have no complete relevant chunk. Cases: pg-custom (first relevant 2), deploy-rollback (first relevant 2), observe-close (first relevant none), password-session (first relevant 3).
- fixed-254-50: 3/22 top-one misses; 0 questions have no complete relevant chunk. Cases: pg-custom (first relevant 3), password-expiry (first relevant 2), train-delay (first relevant 2).
- heading-aware: 0/22 top-one misses; 0 questions have no complete relevant chunk. Cases: none.

No-answer questions still return neighbours:

| Question | Strategy | Top document | Top similarity |
| --- | --- | --- | ---: |
| absent-oracle | paragraph | observability | 0.3984 |
| absent-quantum | paragraph | observability | 0.2179 |
| absent-sourdough | paragraph | solar | 0.2929 |
| absent-train-price | paragraph | trains | 0.4292 |
| absent-oracle | fixed-254-50 | observability | 0.3233 |
| absent-quantum | fixed-254-50 | observability | 0.1723 |
| absent-sourdough | fixed-254-50 | bread | 0.3290 |
| absent-train-price | fixed-254-50 | trains | 0.3674 |
| absent-oracle | heading-aware | observability | 0.3638 |
| absent-quantum | heading-aware | observability | 0.1356 |
| absent-sourdough | heading-aware | bread | 0.2872 |
| absent-train-price | heading-aware | trains | 0.3640 |

Across answerable cases/strategies, first relevant similarity ranges 0.3696 to 0.7545; highest irrelevant similarity ranges 0.3014 to 0.6673. These observed ranges do not define a cutoff. Similarity is not confidence or evidence that an answer exists.

See [all question rankings](results/rankings.md), [every top-one failure with source text and boundaries](results/failures.md), and [full machine-readable evidence](results/retrieval.json). Inputs and expectations were not revised after retrieval.

Manual inspection finds a genuine two-paragraph evidence split for incident closure, nearby procedural distractors for custom restore / rollback / session revocation, and mixed-topic windows for link expiry. Two fixed-window misses also reveal strict-label limitations: the custom-archive result already names pg_restore but loses the end of the evidence sentence; the train result omits the leading word contact. Their frozen scores remain unchanged. These are conservative evidence-span metrics, not independently adjudicated human relevance judgments. Detailed observations are separate from frozen labels in failure-observations.json.

## Interpretation and recommendation

The strongest strategy on this frozen corpus by Recall@3, then MRR, is **heading-aware**. The result supports comparing chunk structure before considering production integration; it does not establish a universally best chunk size. Exact search remains measurable at this small dataset size, with the latency above rather than an extrapolated capacity claim.

Review the documented misses and evidence boundaries before choosing another experiment. Keep the MiniLM FP32 and exact-search baseline for that decision. A synthetic corpus, only 22 answerable cases, strict single-chunk evidence labels, one fixed strategy order and one sequential development run limit generalization. No real-document or RKE2 evaluation, answer synthesis, or production integration was performed. No further experiment has been started.

Recommendation: retain heading-aware chunking as the leading candidate, and make any later evaluation focus on independently labelled real sections and boundary cases before adding retrieval complexity. All natural sections in this corpus fit within 177 content tokens, so oversized heading-section retrieval was not evaluated; only the synthetic correctness fixture exercises that subdivision. Heading-aware results also include more context than paragraphs, so this run does not isolate heading text from chunk size. Exact 50-token overlap can start inside a word; those fragments are preserved in the ranked evidence. The perfect heading-aware score on this small authored set is not a production quality claim.

Previous-spike preservation audit: True, 55 files checked. See [audit](results/preservation-check.json).
