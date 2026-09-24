CREATE TABLE chat_documents (
    id uuid PRIMARY KEY,
    session_id uuid NOT NULL REFERENCES chat_sessions(id) ON DELETE CASCADE,
    filename varchar(180) NOT NULL,
    media_type text NOT NULL,
    status text NOT NULL CHECK (status IN ('uploading','uploaded','failed')),
    byte_length bigint,
    sha256 text,
    original bytea,
    error_code text,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    upload_deadline timestamptz NOT NULL DEFAULT (now() + interval '5 minutes'),
    CHECK ((status='uploaded' AND original IS NOT NULL AND byte_length IS NOT NULL AND sha256 IS NOT NULL AND byte_length=octet_length(original)
        AND byte_length BETWEEN 1 AND 10485760 AND length(sha256)=64 AND error_code IS NULL)
        OR (status IN ('uploading','failed') AND original IS NULL AND byte_length IS NULL AND sha256 IS NULL))
);
CREATE INDEX chat_documents_session_created ON chat_documents(session_id,created_at,id);
