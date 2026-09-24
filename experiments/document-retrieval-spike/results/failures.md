# Every answerable top-one failure

A top-one miss is retained even when Recall@3 succeeds. Classifications are conservative diagnostics, not revised labels. Boundary split can describe a truncated top result or the absence of any complete relevant chunk; each observation distinguishes them. Manual observations supersede the runner's preliminary unknown diagnosis without changing any metric or relevance label.

## paragraph / pg-custom

Which restore program should I use for a custom-format database archive?

Expected document/section: postgres-backup / Restoring a custom archive. First relevant rank: 2. Diagnostic: **semantically similar distractor**.

Rank 1 discusses custom archives and selecting the wrong restore tool, but does not name the required tool. The explicit pg_restore instruction is rank 2. The evidence supports a distractor ranking observation, not a conclusion that MiniLM alone caused it.

Required evidence:

> A custom-format pg_dump archive is restored with pg_restore, not by feeding it to psql.

Evidence spans: 2334..2421.

### Rank 1: postgres-backup / chunk 2

Heading: Creating a backup. Offsets: 969..1397. Tokens: 78. Distance: 0.4216. Similarity: 0.5784. Relevant: False.

```text
The workshop also permits custom-format archives for occasional migration rehearsals. Those archives are labelled separately from plain SQL files. Never rename one format to look like the other. An operator looking only at the filename can otherwise select the wrong restore tool. Export jobs run with an account that can read the required data; that account is not the same as the account used to administer recovery databases.
```

### Rank 2: postgres-backup / chunk 5

Heading: Restoring a custom archive. Offsets: 2303..2765. Tokens: 88. Distance: 0.4781. Similarity: 0.5219. Relevant: True.

```text
## Restoring a custom archive

A custom-format pg_dump archive is restored with pg_restore, not by feeding it to psql. Use a fresh scratch target and review the archive contents before starting. The migration rehearsal checklist includes ownership and extension prerequisites because these can differ between machines. Keep the plain SQL restore instructions nearby for comparison, but do not mix the commands from the two procedures within one recovery attempt.
```

### Rank 3: postgres-backup / chunk 0

Heading: PostgreSQL backup and restore. Offsets: 0..534. Tokens: 99. Distance: 0.5461. Similarity: 0.4539. Relevant: False.

```text
# PostgreSQL backup and restore

This runbook describes the fictional Cedar workshop database. It stores equipment reservations and maintenance notes for a small volunteer team. The database is useful but not a payment system. These procedures belong to the fixed retrieval test corpus, not to a real production environment. Operators record the database name, backup date, and requested recovery outcome before selecting a procedure. A request to recover yesterday's tables is different from a request to retain next month's backups.
```

### Rank 4: postgres-backup / chunk 9

Heading: Point-in-time recovery boundary. Offsets: 3780..4004. Tokens: 42. Distance: 0.6361. Similarity: 0.3639. Relevant: False.

```text
- Record which format and restore tool were used.
- Keep the restore log with the verification record.
- Confirm that the recovered content matches the requested date.
- Keep the original backup until the review is complete.
```

### Rank 5: postgres-backup / chunk 1

Heading: Creating a backup. Offsets: 536..967. Tokens: 86. Distance: 0.6426. Similarity: 0.3574. Relevant: False.

```text
## Creating a backup

The evening operator creates a plain SQL export with pg_dump and writes it to the staging directory. The export is named with the database identifier and the UTC date. A successful command produces a file, but file existence alone does not establish that the file can be restored. Check the exit status, inspect the job log, and record the resulting file size in the backup ledger before copying it elsewhere.
```

## paragraph / deploy-rollback

How do I revert a failed application release, and will that undo database migrations?

Expected document/section: deployments / Rollback procedure. First relevant rank: 2. Diagnostic: **semantically similar distractor**.

The follow-up verification paragraph in the correct rollback section ranks above the paragraph containing rollout undo and the database-migration limitation. Grouping the section succeeds in this corpus; heading context and additional answer content change together.

Required evidence:

> Use rollout undo for the affected Deployment

> it does not reverse database migrations

Evidence spans: 2546..2590, 2674..2713.

### Rank 1: deployments / chunk 6

Heading: Rollback procedure. Offsets: 2901..3274. Tokens: 66. Distance: 0.5007. Similarity: 0.4993. Relevant: False.

```text
After rollback, repeat the same harmless booking lookup used for release verification. Record the failing image tag, the chosen revision, and the observed symptoms. Do not delete the evidence simply because service has recovered. The next operator needs to distinguish an application regression from an unrelated network or database incident that happened at the same time.
```

### Rank 2: deployments / chunk 5

Heading: Rollback procedure. Offsets: 2418..2899. Tokens: 85. Distance: 0.5207. Similarity: 0.4793. Relevant: True.

```text
## Rollback procedure

If the new release fails its verification, inspect rollout history before selecting a previous revision. Use rollout undo for the affected Deployment and watch the replacement rollout finish. This restores the selected Pod template; it does not reverse database migrations or recover deleted booking data. The workshop release checklist therefore identifies whether a change requires a separate data recovery decision before an operator initiates a rollback.
```

### Rank 3: postgres-backup / chunk 4

Heading: Restoring a plain SQL backup. Offsets: 1837..2301. Tokens: 80. Distance: 0.6347. Similarity: 0.3653. Relevant: False.

```text
Compare the restored reservation count with the ledger entry stored beside the export. Open several maintenance notes and inspect the newest booking date. The recovery report includes the backup identifier, target database, elapsed time, and verification outcome. Do not redirect the running workshop application to this scratch database. Rehearsals demonstrate recoverability; they do not authorize replacing live data or changing application connection settings.
```

### Rank 4: postgres-backup / chunk 5

Heading: Restoring a custom archive. Offsets: 2303..2765. Tokens: 88. Distance: 0.6672. Similarity: 0.3328. Relevant: False.

```text
## Restoring a custom archive

A custom-format pg_dump archive is restored with pg_restore, not by feeding it to psql. Use a fresh scratch target and review the archive contents before starting. The migration rehearsal checklist includes ownership and extension prerequisites because these can differ between machines. Keep the plain SQL restore instructions nearby for comparison, but do not mix the commands from the two procedures within one recovery attempt.
```

### Rank 5: postgres-backup / chunk 2

Heading: Creating a backup. Offsets: 969..1397. Tokens: 78. Distance: 0.6711. Similarity: 0.3289. Relevant: False.

```text
The workshop also permits custom-format archives for occasional migration rehearsals. Those archives are labelled separately from plain SQL files. Never rename one format to look like the other. An operator looking only at the filename can otherwise select the wrong restore tool. Export jobs run with an account that can read the required data; that account is not the same as the account used to administer recovery databases.
```

## paragraph / observe-close

What two recovery checks are required before closing the workshop incident?

Expected document/section: observability / Alert acknowledgement and resolution. First relevant rank: none. Diagnostic: **boundary split**.

The external recheck and ten-minute error-ratio requirement occupy separate paragraphs. No paragraph chunk contains both frozen evidence spans. The top result includes only the latter. This is a failure of single-chunk sufficiency; the experiment does not score unions of multiple partial chunks.

Required evidence:

> repeat the same external check that demonstrated the original symptom and record its result

> watch the error ratio remain below the workshop's alert boundary for ten consecutive minutes

Evidence spans: 3633..3724, 3732..3824.

### Rank 1: observability / chunk 9

Heading: Alert acknowledgement and resolution. Offsets: 3727..4064. Tokens: 57. Distance: 0.3327. Similarity: 0.6673. Relevant: False.

```text
Then watch the error ratio remain below the workshop's alert boundary for ten consecutive minutes before marking the incident resolved. These two requirements belong together: a single successful request and a sustained aggregate recovery provide different evidence. Do not close the incident based only on the acknowledgement timestamp.
```

### Rank 2: postgres-backup / chunk 8

Heading: Point-in-time recovery boundary. Offsets: 3712..3778. Tokens: 11. Distance: 0.3643. Similarity: 0.6357. Relevant: False.

```text
Before closing any recovery ticket, complete the following checks:
```

### Rank 3: postgres-backup / chunk 10

Heading: Point-in-time recovery boundary. Offsets: 4006..4213. Tokens: 36. Distance: 0.3847. Similarity: 0.6153. Relevant: False.

```text
These closing checks are deliberately adjacent to the recovery-boundary discussion. They apply to rehearsals as well as incident work and must not be mistaken for a description of an implemented WAL archive.
```

### Rank 4: postgres-backup / chunk 4

Heading: Restoring a plain SQL backup. Offsets: 1837..2301. Tokens: 80. Distance: 0.4656. Similarity: 0.5344. Relevant: False.

```text
Compare the restored reservation count with the ledger entry stored beside the export. Open several maintenance notes and inspect the newest booking date. The recovery report includes the backup identifier, target database, elapsed time, and verification outcome. Do not redirect the running workshop application to this scratch database. Rehearsals demonstrate recoverability; they do not authorize replacing live data or changing application connection settings.
```

### Rank 5: deployments / chunk 5

Heading: Rollback procedure. Offsets: 2418..2899. Tokens: 85. Distance: 0.4829. Similarity: 0.5171. Relevant: False.

```text
## Rollback procedure

If the new release fails its verification, inspect rollout history before selecting a previous revision. Use rollout undo for the affected Deployment and watch the replacement rollout finish. This restores the selected Pod template; it does not reverse database migrations or recover deleted booking data. The workshop release checklist therefore identifies whether a change requires a separate data recovery decision before an operator initiates a rollback.
```

## paragraph / password-session

After recovering my password, do my other devices stay signed in?

Expected document/section: password / Sessions after a password change. First relevant rank: 3. Diagnostic: **semantically similar distractor**.

Rank 1 explains stale pages and fresh sign-in, while the explicit policy revoking all existing sessions is rank 3. The first result is closely related and may imply part of the answer, but does not contain the frozen account-wide revocation statement. Strict evidence labels make this distinction; an independent human relevance review could differ.

Required evidence:

> Completing password recovery revokes the workshop account's existing sessions.

Evidence spans: 2310..2388.

### Rank 1: password / chunk 6

Heading: Sessions after a password change. Offsets: 2684..3052. Tokens: 72. Distance: 0.4338. Similarity: 0.5662. Relevant: False.

```text
If a device continues displaying an old page, refresh it and perform a fresh sign-in before changing the password again. Cached page content can remain visible after the server session is no longer valid. The recovery checklist asks the member to confirm access using a new authenticated request, not merely by observing a page that was already open before the change.
```

### Rank 2: password / chunk 10

Heading: Lost mailbox access. Offsets: 3947..4144. Tokens: 41. Distance: 0.5637. Similarity: 0.4363. Relevant: False.

```text
- A fresh reset message was requested, if needed.
- The password change completed successfully.
- The member tested a new sign-in.
- No password or recovery token was copied into the support notes.
```

### Rank 3: password / chunk 5

Heading: Sessions after a password change. Offsets: 2273..2682. Tokens: 77. Distance: 0.5752. Similarity: 0.4248. Relevant: True.

```text
## Sessions after a password change

Completing password recovery revokes the workshop account's existing sessions. The member signs in again with the new password on each device. This session revocation is part of the fictional workshop policy and is separate from link expiry. A message saying that an old session has ended should therefore not be interpreted as evidence that the new password was rejected.
```

### Rank 4: password / chunk 0

Heading: Workshop account recovery. Offsets: 0..498. Tokens: 89. Distance: 0.5913. Similarity: 0.4087. Relevant: False.

```text
# Workshop account recovery

This document describes the fictional Cedar workshop's account support process. It is a test scenario with local policy choices, not a general identity-provider integration guide. The booking site has password-based accounts for this scenario. Operators distinguish forgotten passwords, forgotten account names, and temporary login lockouts because the actions are different. A person unable to sign in should not be asked to reveal an existing password to a volunteer.
```

### Rank 5: password / chunk 4

Heading: Link expiry and replacement. Offsets: 1875..2271. Tokens: 69. Distance: 0.6010. Similarity: 0.3990. Relevant: False.

```text
The reset page accepts a new password only after the recovery token has passed validation. Opening the page is not equivalent to completing the change. If the browser loses the connection during submission, try signing in through the ordinary sign-in page before assuming that the change failed. Never include the chosen password in the incident record used to investigate the connection problem.
```

## fixed-254-50 / pg-custom

Which restore program should I use for a custom-format database archive?

Expected document/section: postgres-backup / Restoring a custom archive. First relevant rank: 3. Diagnostic: **boundary split**.

Rank 1 ends with 'restored with pg_restore, not by feeding it to', before the final 'psql.' in the frozen evidence sentence. It names the requested program, so the full-sentence evidence rule is stricter than a minimal human answer here. The complete evidence is rank 3, mixed with retention and recovery content. Preserve the miss; it exposes both boundary sensitivity and an evaluation-case limitation.

Required evidence:

> A custom-format pg_dump archive is restored with pg_restore, not by feeding it to psql.

Evidence spans: 2334..2421.

### Rank 1: postgres-backup / chunk 1

Heading: Creating a backup | Restoring a plain SQL backup | Restoring a custom archive. Offsets: 1099..2415. Tokens: 252. Distance: 0.4588. Similarity: 0.5412. Relevant: False.

```text
plain SQL files. Never rename one format to look like the other. An operator looking only at the filename can otherwise select the wrong restore tool. Export jobs run with an account that can read the required data; that account is not the same as the account used to administer recovery databases.

## Restoring a plain SQL backup

Create an empty scratch database before rehearsing recovery. Load a plain SQL dump by passing the saved file to psql against the scratch database. The workshop procedure uses psql with its file option and stops on SQL errors. Check both the process exit code and the import log. A completed shell command with ignored SQL errors is not a successful rehearsal, even if some tables are visible afterwards.

Compare the restored reservation count with the ledger entry stored beside the export. Open several maintenance notes and inspect the newest booking date. The recovery report includes the backup identifier, target database, elapsed time, and verification outcome. Do not redirect the running workshop application to this scratch database. Rehearsals demonstrate recoverability; they do not authorize replacing live data or changing application connection settings.

## Restoring a custom archive

A custom-format pg_dump archive is restored with pg_restore, not by feeding it to
```

### Rank 2: postgres-backup / chunk 0

Heading: PostgreSQL backup and restore | Creating a backup. Offsets: 0..1343. Tokens: 254. Distance: 0.4979. Similarity: 0.5021. Relevant: False.

```text
# PostgreSQL backup and restore

This runbook describes the fictional Cedar workshop database. It stores equipment reservations and maintenance notes for a small volunteer team. The database is useful but not a payment system. These procedures belong to the fixed retrieval test corpus, not to a real production environment. Operators record the database name, backup date, and requested recovery outcome before selecting a procedure. A request to recover yesterday's tables is different from a request to retain next month's backups.

## Creating a backup

The evening operator creates a plain SQL export with pg_dump and writes it to the staging directory. The export is named with the database identifier and the UTC date. A successful command produces a file, but file existence alone does not establish that the file can be restored. Check the exit status, inspect the job log, and record the resulting file size in the backup ledger before copying it elsewhere.

The workshop also permits custom-format archives for occasional migration rehearsals. Those archives are labelled separately from plain SQL files. Never rename one format to look like the other. An operator looking only at the filename can otherwise select the wrong restore tool. Export jobs run with an account that can read the required data; that account is not the same
```

### Rank 3: postgres-backup / chunk 2

Heading: Restoring a plain SQL backup | Restoring a custom archive | Retention and offsite copies | Point-in-time recovery boundary. Offsets: 2150..3562. Tokens: 254. Distance: 0.6068. Similarity: 0.3932. Relevant: True.

```text
to this scratch database. Rehearsals demonstrate recoverability; they do not authorize replacing live data or changing application connection settings.

## Restoring a custom archive

A custom-format pg_dump archive is restored with pg_restore, not by feeding it to psql. Use a fresh scratch target and review the archive contents before starting. The migration rehearsal checklist includes ownership and extension prerequisites because these can differ between machines. Keep the plain SQL restore instructions nearby for comparison, but do not mix the commands from the two procedures within one recovery attempt.

## Retention and offsite copies

The fictional workshop keeps fourteen daily exports and six monthly exports. Retention cleanup runs only after a new export has passed its scheduled verification. The offsite directory receives a copy with the original checksum and backup ledger entry. Copy completion is logged separately from export completion, so a working export job cannot hide a failing offsite transfer. A restore request must identify which copy was actually used.

## Point-in-time recovery boundary

Daily logical exports restore the state captured in those exports. Recovery to an arbitrary moment between exports requires a separate physical-backup and WAL-archiving plan. The workshop has not implemented that plan in this scenario. Do not claim that selecting an approximate export
```

### Rank 4: postgres-backup / chunk 3

Heading: Point-in-time recovery boundary. Offsets: 3277..4213. Tokens: 167. Distance: 0.6299. Similarity: 0.3701. Relevant: False.

```text
Daily logical exports restore the state captured in those exports. Recovery to an arbitrary moment between exports requires a separate physical-backup and WAL-archiving plan. The workshop has not implemented that plan in this scenario. Do not claim that selecting an approximate export timestamp provides point-in-time recovery. Escalate that requirement before promising a recovery point that the available artifacts cannot support.

Before closing any recovery ticket, complete the following checks:

- Record which format and restore tool were used.
- Keep the restore log with the verification record.
- Confirm that the recovered content matches the requested date.
- Keep the original backup until the review is complete.

These closing checks are deliberately adjacent to the recovery-boundary discussion. They apply to rehearsals as well as incident work and must not be mistaken for a description of an implemented WAL archive.
```

### Rank 5: deployments / chunk 2

Heading: Readiness and liveness checks | Rollback procedure | Scaling and routing boundaries. Offsets: 2233..3673. Tokens: 254. Distance: 0.8706. Similarity: 0.1294. Relevant: False.

```text
configuration rather than assuming that a path called health has the same purpose on every service. Probe names are conventions; the configured Kubernetes fields define the behaviour.

## Rollback procedure

If the new release fails its verification, inspect rollout history before selecting a previous revision. Use rollout undo for the affected Deployment and watch the replacement rollout finish. This restores the selected Pod template; it does not reverse database migrations or recover deleted booking data. The workshop release checklist therefore identifies whether a change requires a separate data recovery decision before an operator initiates a rollback.

After rollback, repeat the same harmless booking lookup used for release verification. Record the failing image tag, the chosen revision, and the observed symptoms. Do not delete the evidence simply because service has recovered. The next operator needs to distinguish an application regression from an unrelated network or database incident that happened at the same time.

## Scaling and routing boundaries

Increasing replicas requests more application Pods. It does not create a new public hostname, expose an external port, or configure a browser certificate. The Service provides stable selection of Pods, while the chosen gateway controls incoming HTTP routing. Check those resources independently when capacity appears healthy but users cannot connect. A completed
```

## fixed-254-50 / password-expiry

When does a recovery link expire, and what happens to old links when I request another?

Expected document/section: password / Link expiry and replacement. First relevant rank: 2. Diagnostic: **semantically similar distractor**.

Rank 1 combines sessions, lockout, and mailbox recovery. It mentions the thirty-minute link lifetime but omits the rule invalidating earlier links. The chunk containing both rules is rank 2, only about 0.00474 lower in similarity. Several nearby concepts share each fixed window.

Required evidence:

> a password-reset link expires after thirty minutes, and requesting a new link invalidates earlier links

Evidence spans: 1472..1575.

### Rank 1: password / chunk 2

Heading: Link expiry and replacement | Sessions after a password change | Temporary lockout | Lost mailbox access. Offsets: 2270..3598. Tokens: 254. Distance: 0.5144. Similarity: 0.4856. Relevant: False.

```text
.

## Sessions after a password change

Completing password recovery revokes the workshop account's existing sessions. The member signs in again with the new password on each device. This session revocation is part of the fictional workshop policy and is separate from link expiry. A message saying that an old session has ended should therefore not be interpreted as evidence that the new password was rejected.

If a device continues displaying an old page, refresh it and perform a fresh sign-in before changing the password again. Cached page content can remain visible after the server session is no longer valid. The recovery checklist asks the member to confirm access using a new authenticated request, not merely by observing a page that was already open before the change.

## Temporary lockout

The workshop applies a fifteen-minute waiting period after its repeated-login-failure limit is reached. This waiting period is not the thirty-minute lifetime of a recovery link. A lockout message concerns recent login attempts, whereas an expired-link message concerns a particular recovery token. Record which message appeared before advising the member to wait or request a replacement link.

## Lost mailbox access

If the member can no longer access the registered mailbox, stop the ordinary email recovery process and
```

### Rank 2: password / chunk 1

Heading: Requesting a reset link | Link expiry and replacement | Sessions after a password change. Offsets: 1162..2538. Tokens: 254. Distance: 0.5191. Similarity: 0.4809. Relevant: True.

```text
should use the one originally registered with the workshop. The form does not search unrelated mailboxes or reveal which alternate address was used at registration. Support staff should not forward a reset message to an address supplied in an unverified chat.

## Link expiry and replacement

In this scenario a password-reset link expires after thirty minutes, and requesting a new link invalidates earlier links. Use the newest message when several messages are present. An expired link must be replaced by a new recovery request; repeatedly opening it cannot extend its validity. The page explains expiry without displaying the token in diagnostic output or asking the user to paste it into a support ticket.

The reset page accepts a new password only after the recovery token has passed validation. Opening the page is not equivalent to completing the change. If the browser loses the connection during submission, try signing in through the ordinary sign-in page before assuming that the change failed. Never include the chosen password in the incident record used to investigate the connection problem.

## Sessions after a password change

Completing password recovery revokes the workshop account's existing sessions. The member signs in again with the new password on each device. This session revocation is part of the fictional workshop policy and is separate from
```

### Rank 3: password / chunk 3

Heading: Temporary lockout | Lost mailbox access. Offsets: 3329..4144. Tokens: 153. Distance: 0.6010. Similarity: 0.3990. Relevant: False.

```text
essage concerns a particular recovery token. Record which message appeared before advising the member to wait or request a replacement link.

## Lost mailbox access

If the member can no longer access the registered mailbox, stop the ordinary email recovery process and use the workshop's separate identity-review appointment. Volunteers must not bypass the mailbox check by inventing a new destination address during the same support chat. The review appointment is deliberately outside the reset-link workflow and requires its own evidence and approval record.

Before ending support, confirm the relevant outcome:

- A fresh reset message was requested, if needed.
- The password change completed successfully.
- The member tested a new sign-in.
- No password or recovery token was copied into the support notes.
```

### Rank 4: postgres-backup / chunk 3

Heading: Point-in-time recovery boundary. Offsets: 3277..4213. Tokens: 167. Distance: 0.6168. Similarity: 0.3832. Relevant: False.

```text
Daily logical exports restore the state captured in those exports. Recovery to an arbitrary moment between exports requires a separate physical-backup and WAL-archiving plan. The workshop has not implemented that plan in this scenario. Do not claim that selecting an approximate export timestamp provides point-in-time recovery. Escalate that requirement before promising a recovery point that the available artifacts cannot support.

Before closing any recovery ticket, complete the following checks:

- Record which format and restore tool were used.
- Keep the restore log with the verification record.
- Confirm that the recovered content matches the requested date.
- Keep the original backup until the review is complete.

These closing checks are deliberately adjacent to the recovery-boundary discussion. They apply to rehearsals as well as incident work and must not be mistaken for a description of an implemented WAL archive.
```

### Rank 5: password / chunk 0

Heading: Workshop account recovery | Requesting a reset link | Link expiry and replacement. Offsets: 0..1425. Tokens: 254. Distance: 0.6812. Similarity: 0.3188. Relevant: False.

```text
# Workshop account recovery

This document describes the fictional Cedar workshop's account support process. It is a test scenario with local policy choices, not a general identity-provider integration guide. The booking site has password-based accounts for this scenario. Operators distinguish forgotten passwords, forgotten account names, and temporary login lockouts because the actions are different. A person unable to sign in should not be asked to reveal an existing password to a volunteer.

## Requesting a reset link

Start password recovery from the sign-in page and provide the email address associated with the account. The page displays the same acknowledgement whether or not that address is registered. This avoids confirming account membership through the recovery form. The workshop sends a recovery message only when a matching account exists. Support volunteers can explain the process without disclosing whether a particular address belongs to another member.

The recovery message may arrive after a short delay. Check the intended mailbox and its junk folder before sending repeated requests. If a member has several email addresses, they should use the one originally registered with the workshop. The form does not search unrelated mailboxes or reveal which alternate address was used at registration. Support staff should not forward a reset message to an address supplied in an unverified chat.

##
```

## fixed-254-50 / train-delay

My arriving train was late and I missed the booked connection. What should I do before taking another departure?

Expected document/section: trains / Missed connection after a delay. First relevant rank: 2. Diagnostic: **boundary split**.

Rank 1 starts at 'North Valley staff for authorization', omitting the evidence span's leading 'contact'. Much of the instruction remains intelligible. The full evidence is rank 2, only about 0.00152 lower in similarity. This is a conservative span-based miss and should not be represented as proof that the top text is wholly useless.

Required evidence:

> contact North Valley staff for authorization before boarding an alternative booked-service departure

Evidence spans: 2282..2382.

### Rank 1: trains / chunk 2

Heading: Missed connection after a delay | Assistance booking | Platform and boarding checks. Offsets: 2290..3780. Tokens: 254. Distance: 0.5716. Similarity: 0.4284. Relevant: False.

```text
North Valley staff for authorization before boarding an alternative booked-service departure. Do not buy a replacement merely because the departure board has changed; first establish what the original booking permits after the disruption.

The organiser notes the delayed service and the advice received. A delay affecting the railway is treated differently from a traveller choosing to leave the workshop late. Keeping the reason for the missed connection in the record prevents an exception for disruption from being confused with the normal rules for voluntary itinerary changes.

## Assistance booking

The North Valley scenario asks travellers to book planned boarding assistance at least twenty-four hours before departure. The request records the stations, trains, and type of assistance needed. This fictional notice period is not a statement about real railway requirements. The organiser confirms the arrangement with each traveller rather than assuming that a group ticket automatically includes assistance at every station.

On the day, arrive at the agreed meeting point with time to contact the station team. If the platform changes, check whether the assistance plan also needs an update. The departure board provides train movement information; it is not confirmation that a staff member has received a revised assistance request.

## Platform and boarding checks

Before boarding, compare the destination, departure time, and service information with the itinerary. A train
```

### Rank 2: trains / chunk 1

Heading: Tickets and seat reservations | Flexible and booked-service tickets | Missed connection after a delay. Offsets: 1126..2594. Tokens: 254. Distance: 0.5731. Similarity: 0.4269. Relevant: True.

```text
than boarding a different train solely because it has empty seats. Availability of a seat says nothing about whether a particular ticket is valid on that service.

## Flexible and booked-service tickets

The fictional operator's flexible day ticket permits any listed eligible service on the selected route that day. Its booked-service ticket is valid only on the departure named on the booking unless staff authorize an exception. The workshop records the ticket type for each traveller because mixed ticket types can otherwise produce incorrect assumptions when the group considers an earlier or later departure.

A change to the organiser's calendar does not change the railway booking. Before voluntarily switching trains, read the ticket conditions and ask staff when the permitted service is unclear. The group should not infer flexibility from the fact that two trains visit the same stations. Similar destinations do not establish identical travel conditions.

## Missed connection after a delay

If the incoming train is delayed and a planned connection is missed, retain the original ticket and the delay information. In this fictional scenario, contact North Valley staff for authorization before boarding an alternative booked-service departure. Do not buy a replacement merely because the departure board has changed; first establish what the original booking permits after the disruption.

The organiser notes the delayed service and the advice received.
```

### Rank 3: trains / chunk 3

Heading: Assistance booking | Platform and boarding checks. Offsets: 3494..4502. Tokens: 175. Distance: 0.5886. Similarity: 0.4114. Relevant: False.

```text
The departure board provides train movement information; it is not confirmation that a staff member has received a revised assistance request.

## Platform and boarding checks

Before boarding, compare the destination, departure time, and service information with the itinerary. A train at the expected platform can still be a different service. The workshop group keeps the exit clear while checking tickets and avoids gathering around a doorway when other passengers need to leave. Board only after confirming the service rather than following the first person in the group without checking.

The final departure checklist is short:

- Keep tickets available without exposing account passwords.
- Check the service as well as the platform.
- Confirm assistance arrangements where relevant.
- Preserve delay information if the itinerary is disrupted.

These checks separate travel authorization, seating, and operational information so a change in one does not silently become an assumption about all three.
```

### Rank 4: trains / chunk 0

Heading: Workshop group rail journey | Tickets and seat reservations | Flexible and booked-service tickets. Offsets: 0..1410. Tokens: 254. Distance: 0.7101. Similarity: 0.2899. Relevant: False.

```text
# Workshop group rail journey

This document describes a fictional Cedar workshop outing on the invented North Valley railway. Its ticket and assistance rules are scenario data, not advice about any real operator. The group travels together where practical, but each traveller keeps access to their own ticket. The organiser records the planned service, meeting point, and emergency contact before departure. A seat allocation and permission to travel are related but separate items.

## Tickets and seat reservations

A ticket authorizes travel under its stated conditions; a seat reservation allocates a seat on a particular service. On the fictional North Valley railway, holding a reservation alone does not replace the required ticket. Check both items before boarding. The workshop organiser keeps the coach and seat references beside the itinerary so travellers do not mistake a platform number for a coach number during a hurried connection.

The group may be assigned seats in more than one coach. This does not change the journey shown on the ticket. If a reserved seat cannot be found, ask the onboard staff rather than boarding a different train solely because it has empty seats. Availability of a seat says nothing about whether a particular ticket is valid on that service.

## Flexible and booked-service tickets

The fictional operator's flexible day ticket permits any listed eligible service
```

### Rank 5: services / chunk 3

Heading: Headless discovery | Investigation checklist. Offsets: 3466..4235. Tokens: 136. Distance: 0.7130. Similarity: 0.2870. Relevant: False.

```text
the default remedy for a broken booking Service. Changing an ordinary Service to headless alters the discovery contract and may surprise clients that were written for a stable virtual endpoint.

## Investigation checklist

Follow the internal traffic path before changing resources:

- Confirm the application is listening on its declared port.
- Confirm intended Pods are ready and match the selector.
- Inspect EndpointSlices for the Service.
- Test the Service port from an allowed cluster client.
- Investigate external routing only after recording the internal result.

This sequence provides evidence for the next investigation stage. It is not permission to disable policies, bypass authentication, or replace a gateway simply because a single request timed out.
```
