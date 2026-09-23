CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE spike_passages (
    id integer PRIMARY KEY,
    source_text text NOT NULL,
    embedding vector(384) NOT NULL
);
