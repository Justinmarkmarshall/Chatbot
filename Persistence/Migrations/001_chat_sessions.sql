CREATE TABLE chat_sessions (
    id uuid PRIMARY KEY,
    owner_subject text NOT NULL CHECK (length(owner_subject) BETWEEN 1 AND 255),
    title varchar(120) NOT NULL CHECK (length(btrim(title)) > 0),
    active_turn_id uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX chat_sessions_owner_updated ON chat_sessions(owner_subject, updated_at DESC, id);

CREATE TABLE chat_messages (
    id uuid PRIMARY KEY,
    session_id uuid NOT NULL REFERENCES chat_sessions(id) ON DELETE CASCADE,
    turn_id uuid NOT NULL,
    sequence bigint GENERATED ALWAYS AS IDENTITY,
    role text NOT NULL CHECK (role IN ('user', 'assistant')),
    content text NOT NULL DEFAULT '',
    status text NOT NULL CHECK (status IN ('completed', 'streaming', 'cancelled', 'failed', 'interrupted')),
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (session_id, sequence),
    UNIQUE (session_id, turn_id, role),
    CHECK (role = 'assistant' OR status = 'completed')
);
