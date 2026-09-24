# PostgreSQL backup and restore

This runbook describes the fictional Cedar workshop database. It stores equipment reservations and maintenance notes for a small volunteer team. The database is useful but not a payment system. These procedures belong to the fixed retrieval test corpus, not to a real production environment. Operators record the database name, backup date, and requested recovery outcome before selecting a procedure. A request to recover yesterday's tables is different from a request to retain next month's backups.

## Creating a backup

The evening operator creates a plain SQL export with pg_dump and writes it to the staging directory. The export is named with the database identifier and the UTC date. A successful command produces a file, but file existence alone does not establish that the file can be restored. Check the exit status, inspect the job log, and record the resulting file size in the backup ledger before copying it elsewhere.

The workshop also permits custom-format archives for occasional migration rehearsals. Those archives are labelled separately from plain SQL files. Never rename one format to look like the other. An operator looking only at the filename can otherwise select the wrong restore tool. Export jobs run with an account that can read the required data; that account is not the same as the account used to administer recovery databases.

## Restoring a plain SQL backup

Create an empty scratch database before rehearsing recovery. Load a plain SQL dump by passing the saved file to psql against the scratch database. The workshop procedure uses psql with its file option and stops on SQL errors. Check both the process exit code and the import log. A completed shell command with ignored SQL errors is not a successful rehearsal, even if some tables are visible afterwards.

Compare the restored reservation count with the ledger entry stored beside the export. Open several maintenance notes and inspect the newest booking date. The recovery report includes the backup identifier, target database, elapsed time, and verification outcome. Do not redirect the running workshop application to this scratch database. Rehearsals demonstrate recoverability; they do not authorize replacing live data or changing application connection settings.

## Restoring a custom archive

A custom-format pg_dump archive is restored with pg_restore, not by feeding it to psql. Use a fresh scratch target and review the archive contents before starting. The migration rehearsal checklist includes ownership and extension prerequisites because these can differ between machines. Keep the plain SQL restore instructions nearby for comparison, but do not mix the commands from the two procedures within one recovery attempt.

## Retention and offsite copies

The fictional workshop keeps fourteen daily exports and six monthly exports. Retention cleanup runs only after a new export has passed its scheduled verification. The offsite directory receives a copy with the original checksum and backup ledger entry. Copy completion is logged separately from export completion, so a working export job cannot hide a failing offsite transfer. A restore request must identify which copy was actually used.

## Point-in-time recovery boundary

Daily logical exports restore the state captured in those exports. Recovery to an arbitrary moment between exports requires a separate physical-backup and WAL-archiving plan. The workshop has not implemented that plan in this scenario. Do not claim that selecting an approximate export timestamp provides point-in-time recovery. Escalate that requirement before promising a recovery point that the available artifacts cannot support.

Before closing any recovery ticket, complete the following checks:

- Record which format and restore tool were used.
- Keep the restore log with the verification record.
- Confirm that the recovered content matches the requested date.
- Keep the original backup until the review is complete.

These closing checks are deliberately adjacent to the recovery-boundary discussion. They apply to rehearsals as well as incident work and must not be mistaken for a description of an implemented WAL archive.
