param([switch]$Clean, [switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    New-Item -ItemType Directory -Force results | Out-Null
    # compose.yaml has a fixed experiment-only project name and volume.
    if ($Clean) {
        docker compose down --volumes --remove-orphans
        if ($LASTEXITCODE -ne 0) { throw 'Experiment cleanup failed' }
    }
    if (!$SkipBuild) {
        docker compose build
        if ($LASTEXITCODE -ne 0) { throw 'Image build failed' }
    }
    docker compose up -d --wait db
    if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL startup failed' }
    $dbId = (docker compose ps -q db).Trim()
    function Read-Memory([string]$phase) {
        $values = @(docker exec $dbId cat /sys/fs/cgroup/memory.current /sys/fs/cgroup/memory.peak)
        if ($LASTEXITCODE -ne 0) { return [pscustomobject]@{TimestampUtc=[DateTime]::UtcNow; Phase=$phase; Available=$false} }
        [pscustomobject]@{ TimestampUtc=[DateTime]::UtcNow; Phase=$phase; Available=$true; CurrentBytes=[long]$values[0]; KernelLifetimePeakBytes=[long]$values[1] }
    }
    $memory = [System.Collections.Generic.List[object]]::new()
    $memory.Add((Read-Memory 'idle-before-regression'))
    # Always rerun the original tests; do not reuse a previous exited regression container.
    docker compose up -d --force-recreate regression
    if ($LASTEXITCODE -ne 0) { throw 'Regression startup failed' }
    $regressionId = (docker compose ps -a -q regression).Trim()
    $regressionExit = docker wait $regressionId
    if ($LASTEXITCODE -ne 0 -or $regressionExit -ne '0') { throw 'Original embedding regression failed; retrieval not valid' }
    $memory.Add((Read-Memory 'idle-before-retrieval'))
    docker compose up -d --no-deps --force-recreate runner
    if ($LASTEXITCODE -ne 0) { throw 'Runner startup failed' }
    $runnerId = (docker compose ps -a -q runner).Trim()
    do {
        $memory.Add((Read-Memory 'during-runner'))
        $running = docker inspect --format '{{.State.Running}}' $runnerId
        if ($running -eq 'true') { Start-Sleep -Milliseconds 1000 }
    } while ($running -eq 'true')
    $memory.Add((Read-Memory 'after-runner'))
    $memory | ConvertTo-Json -Depth 4 | Set-Content results/postgres-memory.json
    $dbImage = (docker inspect --format '{{.Image}}' $dbId).Trim()
    $imageInfo = (docker image inspect $dbImage | ConvertFrom-Json)[0]
    $runnerInfo = (docker inspect $runnerId | ConvertFrom-Json)[0]
    $dbInfo = (docker inspect $dbId | ConvertFrom-Json)[0]
    $dockerInfo = docker info --format '{{json .}}' | ConvertFrom-Json
    [ordered]@{
        TimestampUtc=[DateTime]::UtcNow
        PostgreSQLImageTag='pgvector/pgvector:0.8.6-pg17-bookworm'
        PostgreSQLImageId=$imageInfo.Id
        PostgreSQLRepoDigests=$imageInfo.RepoDigests
        RunnerImageId=$runnerInfo.Image
        RunnerExitCode=$runnerInfo.State.ExitCode
        DatabasePublishedPorts=$dbInfo.HostConfig.PortBindings
        DockerOS=$dockerInfo.OperatingSystem
        Kernel=$dockerInfo.KernelVersion
        Architecture=$imageInfo.Architecture
        EngineVersion=$dockerInfo.ServerVersion
        HostProcessor=$env:PROCESSOR_IDENTIFIER
        DatabaseMemoryLimitBytes=$dbInfo.HostConfig.Memory
        RunnerMemoryLimitBytes=$runnerInfo.HostConfig.Memory
        DatabaseNanoCpus=$dbInfo.HostConfig.NanoCpus
        RunnerNanoCpus=$runnerInfo.HostConfig.NanoCpus
        NetworkInspection=(docker network inspect chatbot-pgvector-spike_spike --format '{{.Internal}}')
    } | ConvertTo-Json -Depth 6 | Set-Content results/environment.json
    docker compose logs --no-color runner regression > results/container-output.txt
    Write-Output "Runner exit: $($runnerInfo.State.ExitCode)"
    if ($runnerInfo.State.ExitCode -ne 0) { throw 'Correctness failed; results were retained without changing the test cases.' }
} finally {
    # Preserve the experiment volume for inspection; -Clean explicitly removes it next time.
    docker compose stop
    Pop-Location
}
