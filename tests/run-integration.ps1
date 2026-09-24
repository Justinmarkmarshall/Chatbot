param([switch]$NoRestore)
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
$previousConnection = $env:CHATBOT_TEST_CONNECTION
$previousModel = $env:CHATBOT_TEST_MODEL_ROOT
$previousFixture = $env:CHATBOT_TEST_TOKEN_FIXTURE
$env:CHATBOT_TEST_MODEL_ROOT = Join-Path $PSScriptRoot '../experiments/embedding-spike/models'
$env:CHATBOT_TEST_TOKEN_FIXTURE = Join-Path $PSScriptRoot '../experiments/embedding-spike/fixtures/minilm.json'
try {
    docker compose up -d --wait postgres
    if ($LASTEXITCODE -ne 0) { throw 'Test PostgreSQL failed to start' }
    $port = (docker compose port postgres 5432).Trim().Split(':')[-1]
    $env:CHATBOT_TEST_CONNECTION = "Host=127.0.0.1;Port=$port;Database=chatbot_persistence_tests;Username=test_only;Password=disposable_test_only;Timeout=10"
    if (!$NoRestore) {
        dotnet restore Chatbot.IntegrationTests/Chatbot.IntegrationTests.csproj
        if ($LASTEXITCODE -ne 0) { throw 'Test restore failed' }
    }
    dotnet run --project Chatbot.IntegrationTests/Chatbot.IntegrationTests.csproj -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Integration checks failed' }
} finally {
        $env:CHATBOT_TEST_MODEL_ROOT = $previousModel
    $env:CHATBOT_TEST_TOKEN_FIXTURE = $previousFixture
    $env:CHATBOT_TEST_CONNECTION = $previousConnection
    docker compose stop
    Pop-Location
}

