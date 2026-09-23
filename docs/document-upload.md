# Milestone 2: documents attached to chats

Select or create a chat, then choose a PDF or UTF-8 `.txt` file in its Documents
section. The original file and its metadata are saved in PostgreSQL under that
chat. Returning to the same Google account restores the document list. Use
**Download original** to retrieve the unchanged bytes. **Refresh documents** loads
status changes made in another browser. Switching chats cancels an active upload
in the old chat's component.

This milestone does not extract text, parse PDF structure, chunk, embed, search,
or send documents to Ollama. `Uploaded` means the original is saved, not that the
document is indexed, safe to execute, or ready for question answering.

## Persistence decision

For this small-homelab milestone, bounded originals are stored as PostgreSQL
`bytea` alongside metadata. This is a deliberate simplification of the earlier
filesystem/worker design proposal: one database backup now captures chat ownership,
metadata and original bytes, with no database/filesystem commit gap or additional
PVC. Larger documents or a larger collection would justify revisiting storage.

Migration `002_documents.sql` adds `chat_documents` without changing existing chat
records. Startup applies it under the existing migration lock. Each record stores
a UUID, chat FK, sanitized display filename, canonical media type, state, byte
length, SHA-256, original bytes, timestamps, upload deadline and a safe error code.
Original bytes, length, hash and Uploaded status publish in one atomic update.
Successful originals are immutable; uploading the same file again creates another
record. No deduplication, replacement or deletion workflow is introduced.

No new packages, pgvector dependency, storage service, Helm setting, or Docker
volume is needed. Continue using the persistent PostgreSQL database configured in
[Milestone 1](persistent-chat-sessions.md). Its storage and backups now include
original files as well as messages. Keep the database volume when restarting.

## States and failure behaviour

| Stored status | Meaning |
| --- | --- |
| `uploading` | Metadata and quota reservation exist; original is not downloadable. |
| `uploaded` | Original, length and SHA-256 have been committed together. |
| `failed` | No original is published; the safe error code describes the failure. |

Cancellation, invalid content, actual-byte overflow and ordinary read failures
finalize a reserved record as Failed independently of request cancellation.
An expired Uploading row is marked Failed on a later list/status/upload request.
The five-minute database deadline prevents late upload completion from publishing
an expired reservation. Recovery is lazy, not a background worker; immediately
after a hard crash a row may still show Uploading until its deadline passes.

If PostgreSQL is unavailable, immediate failure finalization can also fail. The
record is recovered by deadline on a later request. If the original committed
but the response was lost, refreshing may reveal an Uploaded record. Check the
list before retrying to avoid duplicate records. Invalid filenames/extensions,
ownership failures, rejected request sizes, or quota failures before reservation
do not create a Failed record.

## Limits and validation

- Up to **10 MiB** of actual bytes per file; empty files are rejected.
- **100 MiB** stored/reserved per Google subject. Each in-progress upload reserves
  its full 10 MiB maximum across all that account's chats; completion releases the
  unused part. This is conservative admission, not a promise to fill every final byte.
- Up to **100 upload records per account**, including failed records, to bound
  metadata growth. There is no user deletion/retention UI in this milestone.
- At most **two active uploads per app process**, with a five-minute upload deadline.
  The database serializes per-owner reservations across replicas.
- `.pdf` files must begin with `%PDF-`. This is a signature check, **not full PDF
  validation, malware scanning, extraction, or an assertion that a parser will accept it**.
- `.txt` files must decode as strict UTF-8 and contain no null bytes. Original
  line endings and encoding bytes, including a BOM if supplied, are preserved.
- Client MIME types are not trusted. Media type is assigned from the allowed
  extension and checked content. Filenames have path components removed and are
  limited to 180 characters without control characters; they are never disk paths.

The UI uses Blazor's bounded file stream. The shared service counts actual bytes
regardless of declared length and buffers only up to the per-file limit before
the database update. Multipart HTTP requests also have a bounded request/form
size (10 MiB plus 64 KiB framing allowance); ASP.NET may temporarily buffer them.
This is intentionally for small files, not unbounded upload throughput.

## Ownership and API

Ownership derives only from the authenticated `NameIdentifier` of the chat owner.
Every operation joins through that chat and checks both the route's session ID
and document ID. A foreign and a missing document/chat both return 404. A guessed
document UUID or another chat belonging to the same owner cannot bypass attachment
boundaries. Blazor and HTTP use the same service checks.

All routes require authentication. Uploads also require the existing antiforgery
cookie and `RequestVerificationToken` obtained from `/api/chat/antiforgery`.

| Method | Route | Result |
| --- | --- | --- |
| GET | `/api/chat/sessions/{sessionId}/documents` | Metadata list, without original bytes |
| POST | `/api/chat/sessions/{sessionId}/documents` | Multipart field `file`; 201 with metadata and status Location |
| GET | `/api/chat/sessions/{sessionId}/documents/{id}` | Current metadata/status |
| GET | `/api/chat/sessions/{sessionId}/documents/{id}/original` | Owned Uploaded original only |

Original downloads use `Content-Disposition: attachment`, `application/octet-stream`,
`X-Content-Type-Options: nosniff`, and `Cache-Control: no-store`. Originals are not
served publicly, embedded as HTML, or interpreted as application instructions.
Validation errors return 400, request/file-size prechecks can return 413, and
concurrency/quota rejection returns 409. Partial originals are never downloadable.

## Local check

1. Start your existing PostgreSQL container and restart Chatbot; migration 2 applies.
2. Sign in, select a chat, and upload a small UTF-8 text file or PDF.
3. Confirm Uploaded, download it, and compare it with the original.
4. Refresh or sign out/back in; the attachment should remain.
5. Switch to a different chat/account; the first chat's files must not appear there.
6. Try invalid content or cancel a sufficiently slow upload; inspect its Failed status.

Automated validation: `./tests/run-integration.ps1`. The combined suite passed
**75 checks**, including the prior chat regression, version-1 upgrade preserving
history, byte-for-byte originals, SHA-256, cross-account and cross-chat denial,
invalid files, cancellation, quotas, expiry recovery, multipart antiforgery,
download headers and server-rendered document controls. This used real PostgreSQL
in a disposable Linux container. A live interactive browser upload, external Google
sign-in and RKE2 deployment were not tested in this run.

A small Delete button beside each document removes its original and indexed chunks/vectors. Deletion is owner/chat-scoped and HTTP DELETE requires antiforgery validation. Existing conversation text and historical source snapshots remain; their original-download links return not found after deletion. An in-flight worker cannot recreate the deleted row.
