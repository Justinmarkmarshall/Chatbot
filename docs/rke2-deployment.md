# RKE2: two independent Helm releases

The `chatbot-postgres` release owns database storage and the `chatbot` release owns the web/worker application. The existing application names/selectors are retained to avoid replacing working Deployments solely for cosmetic names:

```text
namespace chatbot
Cloudflare Tunnel -> existing Envoy Gateway -> HTTPRoute chatbot-chatbot
                                                |
                                     Service chatbot-chatbot
                                                |
                                    Deployment chatbot-chatbot (web)
                                      /                       \
                            existing Ollama          Service chatbot-postgres
                                                              |
                                                  StatefulSet chatbot-postgres
                                                              ^
                                                              |
                                         Deployment chatbot-chatbot-documents (worker)
```

Names above assume release names `chatbot` and `chatbot-postgres`, with no fullname overrides. A headless `chatbot-postgres-headless` Service governs the StatefulSet; applications use the ordinary ClusterIP Service `chatbot-postgres:5432`. Neither the database nor worker has an external route, NodePort, LoadBalancer, hostPort, or Ingress. No cluster has been deployed by this change.

## Prerequisites

- Select the intended RKE2 kubeconfig/context explicitly. Use Kubernetes 1.32+ for the stable StatefulSet PVC retention policy.
- Helm, kubectl, Gateway API v1 CRDs, and the existing Envoy Gateway/Cloudflare route infrastructure must already exist. No second gateway/ingress controller is installed.
- Check storage availability: `kubectl get storageclass`. Leave storageClass empty to use the default, or set your actual class in both values files. PostgreSQL initially requests 10Gi; this is an initial allocation, not a measured capacity recommendation.
- An existing Ollama Service with Qwen3 1.7B already installed is required. Configure its real cluster DNS URL; the default `http://ollama:11434` assumes a Service named `ollama` in namespace `chatbot`.
- Build/publish the updated application image using the existing GHCR workflow. Select its immutable `sha-...` tag for deployment, not an older image without the model or health endpoints. Private GHCR images require an existing imagePullSecret, configured for the application chart.

## Image and model preparation

Web and worker share the same Dockerfile and image. Worker args select `--document-worker`. A build-only Python stage downloads the exact revision and all SHA-256-pinned artifacts from `Processing/minilm.json`; any mismatch fails the build. The final .NET image contains `/model-assets/minilm/` and runs as UID/GID 1654. Python and the download script are not copied into the runtime image. No pod downloads MiniLM or Ollama models. Both web query embeddings and worker document embeddings reuse this local model. The optional existing model PVC override remains supported, but is unnecessary for new images.

Build credentials for Marshall.Authentication.Google continue to use the existing BuildKit `nuget_credentials` secret. Local appsettings files, user project settings, experiments and model caches are excluded from Docker context; configure the image with environment variables/Secrets. Do not bake local database/OAuth credentials into an image. The existing GitHub Actions workflow automatically uses the new Dockerfile stages.

## Create namespace and Secrets

The commands below use Bash and assume three private files have already been created outside the repository. The password file must contain the actual password without a trailing newline; OAuth files contain the actual Google client ID and secret. These are operator-supplied values, not values committed to Helm.

```
printf '%s' 'YOUR_REAL_CLIENT_ID.apps.googleusercontent.com' \
  > /secure/chatbot/google-client-id
  ```

```bash
kubectl config current-context
kubectl get storageclass
kubectl create namespace chatbot --dry-run=client -o yaml | kubectl apply -f -
kubectl -n chatbot create secret generic chatbot-postgres-auth \
  --from-file=password=/secure/chatbot/postgres-password \
  --dry-run=client -o yaml | kubectl apply -f -
kubectl -n chatbot create secret generic chatbot-google-auth \
  --from-file=client-id=/secure/chatbot/google-client-id \
  --from-file=client-secret=/secure/chatbot/google-client-secret \
  --dry-run=client -o yaml | kubectl apply -f -
```

Do not overwrite the password Secret with a different value on an existing database: the PostgreSQL entrypoint initializes credentials only on an empty data directory. Credential rotation needs a matching database role password change and app/worker restart. The default bootstrap role `chatbot` is the image-created administrative role so it can apply schema migrations and create `vector`; this is a simple homelab setup, not a least-privilege production role design.

## Values and installation order

This is how the `postgres.yaml` should look, the cluster default is local-path:

```yaml
persistence:
  size: 10Gi
  storageClass: "" # Cluster default, or your actual class.
```

And `chatbot.yaml`:

```yaml
image:
  tag: sha-REPLACE_WITH_PUBLISHED_TAG
# For private GHCR images, create this Secret independently and uncomment:
# imagePullSecrets:
#   - name: ghcr-pull
database:
  host: chatbot-postgres
  passwordSecret: chatbot-postgres-auth
authentication:
  existingSecret: chatbot-google-auth
ollama:
  baseUrl: http://ollama.OLLAMA_NAMESPACE.svc.cluster.local:11434 # Replace with the existing Service DNS.
  deploy:
    enabled: false
httpRoute:
  hostnames:
    - chatbot.marshalllab.uk # Replace if needed; configure the corresponding Google OAuth redirect URI.
  parentRefs:
    - name: public-gateway
      namespace: envoy-gateway-system
      sectionName: http
dataProtection:
  persistence:
    storageClass: ""
```

Update the image tag, Ollama namespace/service and hostname before running these commands from the repository root:

```bash
helm upgrade --install chatbot-postgres ./charts/chatbot-postgres \
  --namespace chatbot -f postgres-rke2.yaml --wait --timeout 10m
helm upgrade --install chatbot ./charts/chatbot \
  --namespace chatbot -f chatbot-rke2.yaml --wait --timeout 10m
kubectl -n chatbot get deployments,statefulsets,pods,services,pvc,httproute
kubectl -n chatbot logs deployment/chatbot-chatbot-documents
kubectl -n chatbot get httproute chatbot-chatbot -o yaml
kubectl -n chatbot exec chatbot-postgres-0 -- \
  psql -U chatbot -d chatbot -c "SELECT extversion FROM pg_extension WHERE extname='vector';"
```

Confirm HTTPRoute `Accepted` and `ResolvedRefs`. The existing Gateway must permit routes from namespace `chatbot` in its listener `allowedRoutes`; the chart does not modify the shared Gateway. Cloudflare must route your configured hostname to that Gateway, and Google must allow `https://YOUR_HOST/signin-google`. Route timeouts remain five minutes for streaming chat. Only web receives Google/Ollama configuration; worker only receives database/model configuration.

A complete connection-string Secret is still supported through `database.existingSecret` and `database.connectionStringKey`. When unset, the chart passes database host/port/name/user plus a password Secret separately; NpgsqlConnectionStringBuilder escapes password delimiters safely. Both modes use the existing ASP.NET environment-variable configuration system. Ensure a legacy connection-string Secret uses cluster Service DNS, never localhost or a node address.

## Envoy: preserve HTTPS for Google sign-in behind Cloudflare

The web Deployment includes:

```yaml
env:
  - name: ASPNETCORE_FORWARDEDHEADERS_ENABLED
    value: "true"
```

ASP.NET must consume the forwarded scheme, but Envoy must first preserve it. In this deployment the browser uses HTTPS to Cloudflare, while `cloudflared -> Envoy` uses HTTP. Without trusted-proxy configuration, Envoy can replace the incoming `X-Forwarded-Proto: https` with `http`. The environment variable alone cannot repair that incorrect incoming header.

An OAuth `Location` header containing `redirect_uri=http%3A%2F%2Fchatbot.marshalllab.uk%2Fsignin-google` confirms the application is still generating an HTTP callback.

For this topology, trust the proxy immediately ahead of Envoy with one trusted hop. Save the following as `cloudflare-forwarded-headers.yaml` in your shared Gateway configuration:

```yaml
apiVersion: gateway.envoyproxy.io/v1alpha1
kind: ClientTrafficPolicy
metadata:
  name: cloudflare-forwarded-headers
  namespace: envoy-gateway-system
spec:
  targetRefs:
    - group: gateway.networking.k8s.io
      kind: Gateway
      name: public-gateway
  clientIPDetection:
    xForwardedFor:
      numTrustedHops: 1
```

This policy is managed separately from the Chatbot Helm release. If a policy already targets this Gateway, merge the setting into it instead of creating a conflicting policy. The one-hop setting assumes the ingress path above; reassess it if proxies change and keep the listener accessible through the trusted path.

Select the intended cluster context, then apply and inspect the policy:

```bash
kubectl apply -f cloudflare-forwarded-headers.yaml
kubectl get clienttrafficpolicy -n envoy-gateway-system cloudflare-forwarded-headers -o yaml
```

Under `status.ancestors[].conditions`, confirm `type: Accepted` has `status: "True"` for `public-gateway`. Upgrade the application Helm release with your existing values to roll out the web environment setting.

Click **Sign in with Google** again and inspect the redirect response's `Location` header in the browser Network panel. The expected query parameter is:

```text
redirect_uri=https%3A%2F%2Fchatbot.marshalllab.uk%2Fsignin-google
```

This decodes to `https://chatbot.marshalllab.uk/signin-google`, which must match Google's authorized redirect URI. Do **not** add an HTTP callback for the public hostname to work around a broken forwarded scheme. If it remains HTTP, inspect the outgoing headers at each proxy.

Envoy documents how trusted hops control incoming `X-Forwarded-Proto`: [Client traffic policy](https://gateway.envoyproxy.io/v1.7/tasks/traffic/client-traffic-policy/#configure-client-ip-detection) and [Envoy header handling](https://www.envoyproxy.io/docs/envoy/latest/configuration/http/http_conn_man/headers.html#x-forwarded-proto).

## Schema and startup

No new migration framework or migration Job is needed. Both entry points call the existing ChatDatabase.InitializeAsync before serving HTTP or running the worker. A transaction-scoped PostgreSQL advisory lock serializes migrations and the schema version ledger, so concurrent pod starts do not concurrently execute DDL. Existing integration tests exercise concurrent initialization and upgrades preserving data. Migration 003 creates the `vector` extension; migration 004 preserves reply source metadata. Starting the database release with `--wait` first makes PostgreSQL available; Kubernetes restarts a process if startup connection/migration attempts fail. Fix persistent schema errors before retrying an upgrade.

Web startup/liveness use `/health/live`. Readiness uses `/health/ready`, including a bounded PostgreSQL check; responses do not expose credentials. These probes do not prove Ollama/model availability. The worker loads/verifies the model at startup and exits on startup failure; it has no HTTP server or Service. Worker database errors are logged and retried. There is no worker progress watchdog: inspect logs and durable processing status for stuck jobs.

## Durable storage and lifecycle

- `data-chatbot-postgres-0`: PostgreSQL 17 data at `/var/lib/postgresql/data/pgdata`, inside the volume mounted at `/var/lib/postgresql/data`.
- Originals (`bytea`), extracted text, chunks, vectors, durable queue, chat history and source snapshots all live in PostgreSQL. Both pods access them through the database. No upload/shared-filesystem PVC is required; original downloads continue through the authorized web API.
- `chatbot-chatbot-data-protection`: persistent ASP.NET login encryption keys at `/keys`; Helm keep annotation preserves it on app uninstall. The web uses fsGroup 1654 for write access. Storage-driver permission behaviour must be verified in your cluster.
- MiniLM artifacts live in the immutable application image, or an explicitly configured legacy model PVC mounted read-only in both pods.

`helm uninstall chatbot -n chatbot` removes application workloads/routes but cannot remove the independent database release. The PostgreSQL StatefulSet explicitly retains claims on scale-down and deletion, including a database-release uninstall. Keep the same database release name/fullname when reinstalling to reuse the retained claim. PVC deletion and PV reclaim policies are separate operator actions. StatefulSet claim size/storage-class changes are not routine `helm upgrade` operations; plan expansion with your CSI driver and do not change PG major versions casually.

**PVC != backup.** A node/disk loss can still lose this single-node database. External PostgreSQL backups (including originals and chat data) and a tested restore procedure remain follow-up work. No backup platform, HA, replication, operator, PgBouncer, sidecar or PITR was introduced.

## Upgrades and legacy chart warning

For a normal app update, use the second Helm command with the new image tag. PostgreSQL and its claims belong to the separate release and remain unchanged. Web and worker use Recreate rollout to avoid overlap/single-node volume mount conflicts; brief downtime is expected. The durable queue recovers jobs after worker restart using the existing global advisory lock and per-job fence. Work claiming is serialized across workers; the chart intentionally permits only zero/one worker, and adds no distributed scaling. Web replicas default to one; more require appropriate shared login-key storage and Blazor connection affinity, neither is provisioned here.

**An older application release may own Ollama through `ollama.deploy.enabled=true`.** Inspect `helm get values chatbot -n chatbot` and `helm get manifest chatbot -n chatbot` before upgrading it. Do not blindly disable that sub-workload: Helm removes resources no longer rendered, potentially including its Ollama cache PVC. Establish the independent Ollama workload and migrate/preserve its model storage before applying this chart. The new chart rejects bundled Ollama mode rather than silently treating it as the requested topology. No existing live workload was changed by this task. Existing web Service/Deployment names remain unchanged.

## Resources and local development

All defaults are editable values, not measured RKE2 capacity:

| Workload | CPU request | Memory request | CPU limit | Memory limit |
|---|---:|---:|---:|---:|
| Web | 100m | 256Mi | unset | 768Mi |
| Worker | 200m | 384Mi | 1 | 1Gi |
| PostgreSQL | 100m | 256Mi | 1 | 1Gi |

`compose.postgres.yaml` remains **local development only**. `chatbot-postgres` Helm release is the **RKE2 database**. Existing local connection strings/user secrets and separate worker startup commands still work. RKE2 runtime, CPU/memory capacity, storage provisioning and gateway routing remain unmeasured until explicitly deployed and observed.

## Offline validation

```bash
dotnet build tests/Chatbot.IntegrationTests/Chatbot.IntegrationTests.csproj -c Release
helm lint charts/chatbot
helm lint charts/chatbot-postgres
helm template chatbot charts/chatbot -n chatbot > chatbot-rendered.yaml
helm template chatbot-postgres charts/chatbot-postgres -n chatbot > chatbot-postgres-rendered.yaml
# PyYAML is a validation-only dependency, not an application dependency.
python -m pip install PyYAML==6.0.3
python tests/check-helm.py chatbot-rendered.yaml chatbot-postgres-rendered.yaml
```

PowerShell integration runners remain `tests/run-integration.ps1` and, after publishing tests to `tests/linux-publish`, `tests/run-linux-integration.ps1`. Render validation checks resource kinds, references, selectors, Secret references, model paths, probes, retention and internal-only database exposure without requiring cluster access.

The PostgreSQL 17 mount and retention choices follow the [official PostgreSQL image documentation](https://hub.docker.com/_/postgres) and [Kubernetes StatefulSet documentation](https://kubernetes.io/docs/concepts/workloads/controllers/statefulset/). See `rke2-deployment-results.md` for actual validation results and boundaries.
