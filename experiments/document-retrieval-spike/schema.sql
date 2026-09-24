CREATE EXTENSION IF NOT EXISTS vector;
CREATE TABLE document_chunks (
 id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
 document_id text NOT NULL,
 chunk_strategy text NOT NULL,
 chunk_index integer NOT NULL,
 heading text,
 source_text text NOT NULL,
 token_count integer NOT NULL CHECK (token_count BETWEEN 1 AND 254),
 start_offset integer NOT NULL,
 end_offset integer NOT NULL,
 embedding vector(384) NOT NULL,
 UNIQUE (document_id, chunk_strategy, chunk_index)
);
