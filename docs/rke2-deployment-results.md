# Kubernetes deployment validation — 2026-09-23

Prepared and validated locally. No RKE2/other Kubernetes deployment or Helm installation/uninstallation was performed.

## Results

- `dotnet build Chatbot.csproj -c Release --no-restore -p:OutputPath=obj/deploy-build/`: succeeded, zero warnings/errors. Separate output avoided overwriting the user's running local web/worker binaries.
- Published integration harness in Linux .NET runtime: **140 checks passed** against disposable PostgreSQL/pgvector. Includes existing chat, retrieval and Milestone 3 processing tests, 10 deletion checks, database field/connection-string compatibility and safe password quoting, and readiness/liveness checks.
- `helm lint charts/chatbot`: passed.
- `helm lint charts/chatbot-postgres`: passed.
- `helm template` for releases `chatbot` and `chatbot-postgres` in namespace `chatbot`: passed.
- **30 parsed-YAML reference/architecture checks passed**, including two Deployments, one StatefulSet, correct PostgreSQL 17 mount, retained 10Gi claim template, default StorageClass omission, internal database Services, headless governing Service, web-only HTTPRoute, selectors, shared database configuration/Secret, image/model paths, probes and resources.
- Additional renders passed for legacy full connection-string Secret + model PVC override and a custom PostgreSQL StorageClass.
- Docker `--target model` stage built successfully. It downloaded and verified model.onnx, vocab.txt, tokenizer.json, tokenizer_config.json, config.json and README.md against all six pinned hashes. Resulting local verification image: `chatbot-minilm-buildcheck`.

The Linux test container used one CPU/768 MiB and ran as UID 1654. These are test conditions, not RKE2 sizing evidence. Optional live Qwen3 tests were skipped in this deployment regression run; earlier milestone observations remain documented separately. Docker reported unavailable swap limiting and Npgsql an optional Kerberos-library warning; password-authenticated tests passed.

## Validation boundaries

The complete private-NuGet application Dockerfile build/GHCR publication was not run in this task. Application assemblies were built/published locally, the actual Docker model-preparation stage was built separately, and application integration tests ran in the Linux runtime image. CI must build/publish the final combined image with its existing GitHub package BuildKit credential before installation.

No Kubernetes API server-side admission, live storage provisioning, actual pod rollout, Gateway/Cloudflare/Google callback, cluster DNS/Ollama reachability or RKE2 performance was tested. Helm/YAML checks validate structure/references offline. The runtime startup migration lock was exercised against real PostgreSQL through existing concurrent-initialization tests.

## Files changed for Kubernetes

- Added `charts/chatbot-postgres/Chart.yaml`, `values.yaml`, `values.schema.json`, `templates/_helpers.tpl`, `templates/service.yaml`, `templates/statefulset.yaml`.
- Updated application `Chart.yaml`, `values.yaml`, helpers, web/worker Deployment templates, web Service and login-key PVC; added `values.schema.json` and `templates/validate.yaml`. Existing HTTPRoute template is reused unchanged.
- Updated `Dockerfile` and `.dockerignore`; added `build/fetch-minilm.py` for build-time verified artifact preparation. Existing GHCR workflow remains in use without another image pipeline.
- Added `Persistence/DatabaseConfiguration.cs`, `Services/DatabaseHealthCheck.cs`; updated startup registrations/endpoints in `Program.cs`.
- Added `tests/check-helm.py`, expanded integration tests for configuration/probes, and updated README/deployment documentation.

## Earlier request completed in the same work session

A compact Delete control was added beside document names in `Components/SessionDocuments.razor`, styled in `wwwroot/app.css`. `DocumentService.DeleteAsync` and the antiforgery-protected controller DELETE endpoint enforce user/chat ownership and remove originals with cascading vector/chunk cleanup. Tests cover unauthorized/cross-chat deletion, antiforgery, original unavailability, cascade cleanup and stale worker publication prevention. Existing conversation text/source snapshots remain as history. No real user document was deleted by validation.
