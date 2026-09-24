# Gateway API routing

The fictional Cedar cluster publishes a workshop webpage through Gateway API. A gateway listener accepts incoming connections, and an HTTPRoute describes which backend receives matching HTTP traffic. These resources complement the booking Deployment and Service. They do not replace the application process, create database tables, or select a release image. Use the resource status as evidence of controller decisions before guessing from a browser error alone.

## Hostnames and HTTPRoute matches

An HTTPRoute can match a hostname and a path before forwarding a request to a backend Service. The workshop route uses the hostname booking.cedar.example and sends matching booking requests to its internal booking Service. A request with the wrong Host header can miss this route even when it reaches the correct network address. Tests made directly against an address should therefore preserve the intended hostname when checking HTTP routing.

The gateway listener and route must agree about where the route may attach. Inspect the route's parent reference and the listener configuration together. A correctly spelled backend Service name cannot compensate for a route that was never accepted by its intended parent. Keep the original request hostname, path, and response code in the incident record so another operator can repeat the same routing decision.

## Route status conditions

Check Accepted and ResolvedRefs on the relevant HTTPRoute parent status. Accepted describes whether the parent accepts the route, while ResolvedRefs reports whether referenced resources can be resolved appropriately. Read the condition reason and message as well as the boolean status. A route object existing in the API is not sufficient evidence that the controller has installed usable forwarding behaviour for that route.

Status can lag a recent edit. Compare the observed generation with the resource generation when deciding whether a condition describes the current configuration. The workshop records a fresh status snapshot after each routing change. Repeating a browser refresh without examining status often leaves the operator unable to distinguish an outdated controller observation from a persistent configuration error.

## Cross-namespace backend permission

The workshop's shared gateway namespace sometimes forwards to a Service in a separate application namespace. A cross-namespace backend reference requires an appropriate ReferenceGrant in the backend namespace. The grant authorizes the permitted reference; it does not change Service selectors or make an unready backend healthy. Keep the allowed source, target kind, and namespace as narrow as the test scenario requires.

When the grant is missing, inspect the reference-related status before changing the Deployment. A new application image cannot authorize a forbidden cross-namespace reference. Conversely, adding a grant cannot repair a typo in a Service name. The runbook deliberately separates reference authorization, resource resolution, and backend health because all three can lead to similar gateway-facing symptoms.

## TLS termination boundary

TLS configuration belongs to the selected gateway listener in this scenario. The public certificate must cover the hostname used by the client. Backend forwarding and browser certificate validation are separate parts of the request path. A route that forwards correctly over an internal test connection does not prove that the public listener presents the expected certificate to a browser.

The workshop change procedure finishes in two stages. First, verify the listener certificate and route status using the intended hostname. Then verify that the backend receives the intended path and can return the expected harmless page. Record both observations in the same ticket, but keep the stages separate so an error at the public edge does not get labelled as a database failure.

## Routing handover

- Preserve the tested hostname and path.
- Record the parent listener and backend Service namespace.
- Attach the current Accepted and ResolvedRefs conditions.
- Record certificate and application checks separately.

The handover should allow another operator to repeat the exact request. A note that merely says the gateway works is insufficient when several routes share one listener and only one hostname was tested.
