# Kubernetes Services

This document describes service discovery for the fictional Cedar booking application. The application has a Deployment named booking and a Service with a similar name. The Service gives clients a stable endpoint while Pods are replaced. It does not carry the application executable or decide which container image is deployed. During troubleshooting, write down both the resource kind and namespace instead of referring only to booking.

## Selecting application Pods

A Service selector matches labels on Pods. If the selector does not match the intended Pods, the Service can have no usable endpoints even when the Deployment has ready replicas. Compare the selector with the actual Pod labels and then inspect the corresponding EndpointSlices. Do not assume that the Deployment resource name automatically supplies a matching label. The workshop uses an application label consistently, but a typing mistake still breaks that convention.

An endpoint investigation starts with readiness as well as labels. A Pod can match the selector while remaining unavailable for normal traffic because it is not ready. Readiness failures belong in the application investigation, whereas selector mistakes belong in the Service configuration review. Both failures can produce a similar user symptom. Preserve the label comparison and probe result separately so fixing one does not conceal the other.

## Service ports and target ports

The Service port is the port clients use on the Service; targetPort identifies the destination port on the selected Pods. The fictional booking Service accepts connections on port 80 and forwards them to the application's listening port 8080. Changing the Service port does not make the application process listen on a new port. Check the container configuration when the endpoint exists but connections fail at the destination.

Named ports make the relationship easier to read when several workloads use different numeric ports. A name still needs to resolve to the intended container port. During a change, test the Service address from an allowed client inside the cluster before examining an external route. This isolates the internal connection path and reduces the number of unrelated gateway settings being considered at once.

## ClusterIP and external exposure

A ClusterIP Service supplies an internal cluster address. A LoadBalancer Service requests external exposure from the cluster's load-balancer implementation. The workshop's local cluster requires that implementation to exist; writing the Service type does not manufacture an external network device. An external address that remains pending therefore calls for checking the load-balancer controller and its address configuration, not repeatedly restarting application Pods.

An external Service address is also distinct from HTTP hostname and path routing. A LoadBalancer Service does not itself define HTTPRoute hostname rules. In the workshop scenario, those rules belong to Gateway API resources. Keep transport connectivity tests separate from HTTP routing tests, especially when one public listener serves several applications using different hostnames on the same address.

## Headless discovery

A headless Service omits the normal virtual Service IP and supports discovery of individual endpoints. The workshop uses this pattern only in a separate test namespace where clients explicitly understand multiple endpoint addresses. It is not the default remedy for a broken booking Service. Changing an ordinary Service to headless alters the discovery contract and may surprise clients that were written for a stable virtual endpoint.

## Investigation checklist

Follow the internal traffic path before changing resources:

- Confirm the application is listening on its declared port.
- Confirm intended Pods are ready and match the selector.
- Inspect EndpointSlices for the Service.
- Test the Service port from an allowed cluster client.
- Investigate external routing only after recording the internal result.

This sequence provides evidence for the next investigation stage. It is not permission to disable policies, bypass authentication, or replace a gateway simply because a single request timed out.
