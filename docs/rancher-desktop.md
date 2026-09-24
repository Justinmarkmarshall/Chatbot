# Install locally on Rancher Desktop (PowerShell 7)

These steps use the existing charts, one local application image, and Windows-hosted Ollama. No Envoy, Cloudflare or Gateway API installation is required. Commands are run from `C:\Dev\AI\Chatbot\Chatbot`. They install a separate database; they do not migrate your existing Docker PostgreSQL data.

The inspected local cluster on 2026-09-24 was `rancher-desktop`, Kubernetes v1.36.4, with default storage class `local-path`. The database chart requires Kubernetes 1.32 or newer.

## 1. Check the cluster

```powershell
Set-Location C:\Dev\AI\Chatbot\Chatbot
kubectl --context rancher-desktop get nodes
kubectl --context rancher-desktop get storageclass
kubectl --context rancher-desktop create namespace chatbot --dry-run=client -o yaml | kubectl --context rancher-desktop apply -f -
```

## 2. Make Ollama reachable

Ensure `qwen3:1.7b` is installed (`ollama list`, or `ollama pull qwen3:1.7b` if missing). The application chart does not install Ollama.

For the Windows Ollama desktop application, quit Ollama from the system tray, set its listener, then reopen it:

```powershell
[Environment]::SetEnvironmentVariable('OLLAMA_HOST', '0.0.0.0:11434', 'User')
```

If you start Ollama manually instead, use a separate terminal:

```powershell
$env:OLLAMA_HOST = '0.0.0.0:11434'
ollama serve
```

Do not start a second server if the desktop application is already serving. Allow Rancher Desktop/WSL traffic through Windows Firewall if needed, keeping port 11434 restricted to the intended local network. Verify connectivity from the cluster:

```powershell
kubectl --context rancher-desktop -n chatbot run ollama-check --rm -i --restart=Never --image=curlimages/curl:8.12.1 -- curl --fail --max-time 15 http://host.docker.internal:11434/api/tags
```

The response should list `qwen3:1.7b`. If DNS fails, try `host.rancher-desktop.internal` and use the working name in step 5. Do not use `localhost:11434` in the Helm value; that would refer to the Chatbot pod.

Sources: [Rancher Desktop host networking](https://docs.rancherdesktop.io/faq/), [Ollama listener configuration](https://docs.ollama.com/faq).

## 3. Build the current app image

Use Rancher Desktop's Moby/dockerd engine for these Docker commands. Check `docker info` to confirm that the daemon is Rancher Desktop. If using containerd, build with `nerdctl --namespace k8s.io` instead so Kubernetes can see the image; see [Rancher Desktop images](https://docs.rancherdesktop.io/tutorials/working-with-images/).

The private authentication package requires package-read credentials. If `NuGetPackageSourceCredentials_github` is already set, keep it; otherwise:

```powershell
$packageUser = Read-Host 'GitHub username'
$packageToken = Read-Host 'GitHub token with read:packages and access to Marshall.Authentication.Google' -MaskInput
$env:NuGetPackageSourceCredentials_github = "Username=$packageUser;Password=$packageToken;ValidAuthenticationTypes=Basic"
Remove-Variable packageToken
```

Build with a new tag for every update. The build downloads and verifies the pinned MiniLM artifacts; runtime pods need no model download:

```powershell
$imageTag = 'local-' + (Get-Date -Format yyyyMMddHHmmss)
docker build --secret id=nuget_credentials,env=NuGetPackageSourceCredentials_github -t "chatbot:$imageTag" .
if ($LASTEXITCODE -ne 0) { throw 'Image build failed' }
```

## 4. Create the database and Google Secrets

First install only: enter a database password and the same Google OAuth client credentials used by the local application. Values are passed over stdin, not saved in Helm values or command history. Do not rerun the database Secret command with a different password after the database has initialized; changing a Secret alone does not rotate a PostgreSQL password.

```powershell
$databasePassword = Read-Host 'New local PostgreSQL password' -MaskInput
@{ apiVersion='v1'; kind='Secret'; metadata=@{name='chatbot-postgres-auth'; namespace='chatbot'}; type='Opaque'; stringData=@{password=$databasePassword} } | ConvertTo-Json -Depth 5 -Compress | kubectl --context rancher-desktop apply -f -
Remove-Variable databasePassword

$googleClientId = Read-Host 'Google OAuth client ID'
$googleClientSecret = Read-Host 'Google OAuth client secret' -MaskInput
@{ apiVersion='v1'; kind='Secret'; metadata=@{name='chatbot-google-auth'; namespace='chatbot'}; type='Opaque'; stringData=@{'client-id'=$googleClientId; 'client-secret'=$googleClientSecret} } | ConvertTo-Json -Depth 5 -Compress | kubectl --context rancher-desktop apply -f -
Remove-Variable googleClientSecret
```

In that Google OAuth client's **Authorized redirect URIs**, add exactly:

```text
http://localhost:8080/signin-google
```

This is an HTTP localhost callback for the port-forward below; the existing `https://localhost:7035/signin-google` entry is a different URL.

## 5. Install PostgreSQL, then the application

Run in the same terminal as the build so `$imageTag` is available:

```powershell
helm upgrade --install chatbot-postgres ./charts/chatbot-postgres --kube-context rancher-desktop --namespace chatbot --set persistence.storageClass=local-path --wait --timeout 10m
if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL installation failed' }

helm upgrade --install chatbot ./charts/chatbot --kube-context rancher-desktop --namespace chatbot --set image.repository=chatbot --set-string "image.tag=$imageTag" --set image.pullPolicy=Never --set httpRoute.enabled=false --set dataProtection.persistence.storageClass=local-path --set-string ollama.baseUrl=http://host.docker.internal:11434 --wait --timeout 10m
if ($LASTEXITCODE -ne 0) { throw 'Chatbot installation failed; inspect pod status and logs' }
```

The database host, Secret names and model path already match the chart defaults. The document worker is enabled by default. Application/worker startup applies schema migrations automatically, including pgvector; no separate migration command is needed.

`image.pullPolicy=Never` deliberately uses the local image. `ErrImageNeverPull` means the image/tag is absent from Kubernetes' image store: check the container runtime and, for containerd, the `k8s.io` namespace.

## 6. Open Chatbot

Keep this command running in its own terminal:

```powershell
kubectl --context rancher-desktop -n chatbot port-forward service/chatbot-chatbot 8080:80
```

Open **http://localhost:8080**, sign in, and create a chat. Use `localhost`, not `127.0.0.1`, to match the registered callback. If port 8080 is occupied, change the local port and the registered OAuth redirect URI together.

## 7. Check status and processing

```powershell
kubectl --context rancher-desktop -n chatbot get pods,services,pvc
kubectl --context rancher-desktop -n chatbot logs deployment/chatbot-chatbot --tail=100
kubectl --context rancher-desktop -n chatbot logs deployment/chatbot-chatbot-documents --tail=100
kubectl --context rancher-desktop -n chatbot exec chatbot-postgres-0 -- psql -U chatbot -d chatbot -c "SELECT extversion FROM pg_extension WHERE extname='vector';"
```

For subsequent app updates, rebuild with a fresh `$imageTag` and rerun only the application Helm command in step 5. The PostgreSQL release and stored chats/documents are independent of the application release. Local PVCs persist across pod restarts, but resetting Rancher Desktop/Kubernetes can remove local data.

This guide's chart settings were rendered locally; no Helm release was installed as part of writing the guide. End-to-end Google sign-in and host Ollama connectivity must be checked during installation.
