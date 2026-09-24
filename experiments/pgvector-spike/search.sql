SELECT
    id,
    source_text,
    embedding <=> $1 AS cosine_distance,
    1 - (embedding <=> $1) AS similarity
FROM spike_passages
ORDER BY embedding <=> $1, id
LIMIT $2;
