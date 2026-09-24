SELECT id, document_id, chunk_index, heading, source_text,
       embedding <=> $1 AS cosine_distance,
       1 - (embedding <=> $1) AS similarity,
       start_offset, end_offset, token_count
FROM document_chunks
WHERE chunk_strategy = $2
ORDER BY embedding <=> $1, id
LIMIT $3;
