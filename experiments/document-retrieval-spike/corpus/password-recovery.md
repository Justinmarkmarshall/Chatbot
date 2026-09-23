# Workshop account recovery

This document describes the fictional Cedar workshop's account support process. It is a test scenario with local policy choices, not a general identity-provider integration guide. The booking site has password-based accounts for this scenario. Operators distinguish forgotten passwords, forgotten account names, and temporary login lockouts because the actions are different. A person unable to sign in should not be asked to reveal an existing password to a volunteer.

## Requesting a reset link

Start password recovery from the sign-in page and provide the email address associated with the account. The page displays the same acknowledgement whether or not that address is registered. This avoids confirming account membership through the recovery form. The workshop sends a recovery message only when a matching account exists. Support volunteers can explain the process without disclosing whether a particular address belongs to another member.

The recovery message may arrive after a short delay. Check the intended mailbox and its junk folder before sending repeated requests. If a member has several email addresses, they should use the one originally registered with the workshop. The form does not search unrelated mailboxes or reveal which alternate address was used at registration. Support staff should not forward a reset message to an address supplied in an unverified chat.

## Link expiry and replacement

In this scenario a password-reset link expires after thirty minutes, and requesting a new link invalidates earlier links. Use the newest message when several messages are present. An expired link must be replaced by a new recovery request; repeatedly opening it cannot extend its validity. The page explains expiry without displaying the token in diagnostic output or asking the user to paste it into a support ticket.

The reset page accepts a new password only after the recovery token has passed validation. Opening the page is not equivalent to completing the change. If the browser loses the connection during submission, try signing in through the ordinary sign-in page before assuming that the change failed. Never include the chosen password in the incident record used to investigate the connection problem.

## Sessions after a password change

Completing password recovery revokes the workshop account's existing sessions. The member signs in again with the new password on each device. This session revocation is part of the fictional workshop policy and is separate from link expiry. A message saying that an old session has ended should therefore not be interpreted as evidence that the new password was rejected.

If a device continues displaying an old page, refresh it and perform a fresh sign-in before changing the password again. Cached page content can remain visible after the server session is no longer valid. The recovery checklist asks the member to confirm access using a new authenticated request, not merely by observing a page that was already open before the change.

## Temporary lockout

The workshop applies a fifteen-minute waiting period after its repeated-login-failure limit is reached. This waiting period is not the thirty-minute lifetime of a recovery link. A lockout message concerns recent login attempts, whereas an expired-link message concerns a particular recovery token. Record which message appeared before advising the member to wait or request a replacement link.

## Lost mailbox access

If the member can no longer access the registered mailbox, stop the ordinary email recovery process and use the workshop's separate identity-review appointment. Volunteers must not bypass the mailbox check by inventing a new destination address during the same support chat. The review appointment is deliberately outside the reset-link workflow and requires its own evidence and approval record.

Before ending support, confirm the relevant outcome:

- A fresh reset message was requested, if needed.
- The password change completed successfully.
- The member tested a new sign-in.
- No password or recovery token was copied into the support notes.
