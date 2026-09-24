ALTER TABLE chat_messages ADD COLUMN document_sources jsonb;
ALTER TABLE chat_messages ADD CONSTRAINT chat_messages_document_sources_array
 CHECK(document_sources IS NULL OR (role='assistant' AND jsonb_typeof(document_sources)='array'));
