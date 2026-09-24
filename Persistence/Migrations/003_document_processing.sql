CREATE EXTENSION IF NOT EXISTS vector WITH SCHEMA public;
ALTER TABLE chat_documents
 ADD COLUMN processing_status text NOT NULL DEFAULT 'not_started' CHECK(processing_status IN ('not_started','queued','processing','ready','failed')),
 ADD COLUMN processing_error text,
 ADD COLUMN processing_attempts integer NOT NULL DEFAULT 0,
 ADD COLUMN processing_token uuid,
 ADD COLUMN chunk_count integer NOT NULL DEFAULT 0,
 ADD COLUMN extracted_text text;
UPDATE chat_documents SET processing_status='queued' WHERE status='uploaded';
CREATE INDEX chat_documents_queue ON chat_documents(created_at) WHERE processing_status IN ('queued','processing');
CREATE TABLE document_chunks (
 document_id uuid NOT NULL REFERENCES chat_documents(id) ON DELETE CASCADE,
 ordinal integer NOT NULL CHECK(ordinal>=0),
 heading text NOT NULL,
 page_number integer,
 start_offset integer NOT NULL CHECK(start_offset>=0),
 end_offset integer NOT NULL CHECK(end_offset>start_offset),
 content text NOT NULL,
 token_count integer NOT NULL CHECK(token_count BETWEEN 1 AND 254),
 embedding public.vector(384) NOT NULL,
 model_profile text NOT NULL CHECK(model_profile='minilm-l6-v2-fp32-1110a243-mean-l2-v1'),
 PRIMARY KEY(document_id,ordinal)
);
