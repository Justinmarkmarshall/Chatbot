# Kubernetes Deployments

The fictional Cedar cluster runs a workshop booking service with two application replicas. This document concerns replacing and observing application Pods. It does not explain how external clients reach the service. A Deployment, a Service, and an HTTPRoute may all mention the same application name while controlling different parts of the system. Record the namespace and resource kind when collecting evidence so similarly named objects do not get confused during an incident.

## Rolling out a new application image

The workshop publishes an immutable image tag for every approved release. Update the Deployment Pod template to reference the new image, then watch rollout status for that Deployment. A changed Pod template produces a new ReplicaSet and replacement Pods. Editing a Service selector is not the normal way to release application code. The release note links the image tag to a short description of the behaviour that is expected to change.

During a rolling update, old and new Pods can coexist. Observe readiness, restart counts, and recent events before declaring the rollout complete. A Pod that is running but not ready should not be treated as a healthy replacement. The operator checks the workshop's booking endpoint after the rollout, using a harmless read operation that does not create reservations. A successful image pull alone says nothing about whether the application serves requests correctly.

## Readiness and liveness checks

Readiness controls whether a Pod is eligible to receive normal Service traffic; liveness determines whether an unhealthy container should be restarted. The two checks are intentionally different in the workshop configuration. A temporary dependency outage may make a Pod unready without requiring repeated restarts. Restarting every replica during the same dependency incident can lengthen recovery and obscure the original problem in a flood of new startup logs.

The readiness endpoint checks that the application can serve its basic booking lookup. It does not run expensive maintenance tasks. The liveness endpoint detects a stuck application process using a lightweight local check. When examining a failed rollout, read the actual probe configuration rather than assuming that a path called health has the same purpose on every service. Probe names are conventions; the configured Kubernetes fields define the behaviour.

## Rollback procedure

If the new release fails its verification, inspect rollout history before selecting a previous revision. Use rollout undo for the affected Deployment and watch the replacement rollout finish. This restores the selected Pod template; it does not reverse database migrations or recover deleted booking data. The workshop release checklist therefore identifies whether a change requires a separate data recovery decision before an operator initiates a rollback.

After rollback, repeat the same harmless booking lookup used for release verification. Record the failing image tag, the chosen revision, and the observed symptoms. Do not delete the evidence simply because service has recovered. The next operator needs to distinguish an application regression from an unrelated network or database incident that happened at the same time.

## Scaling and routing boundaries

Increasing replicas requests more application Pods. It does not create a new public hostname, expose an external port, or configure a browser certificate. The Service provides stable selection of Pods, while the chosen gateway controls incoming HTTP routing. Check those resources independently when capacity appears healthy but users cannot connect. A completed Deployment rollout is useful evidence, but it does not prove that traffic can traverse the entire route.

The shift handover records three separate outcomes:

- The desired application image is running.
- The desired number of replicas is ready.
- The external request path was verified through its own routing checks.

Keeping those observations separate makes it possible to see which layer failed without treating every unavailable webpage as a failed deployment.
