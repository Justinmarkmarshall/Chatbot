param()
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    docker compose up -d --wait postgres
    if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL failed to start' }
    $repository = (Resolve-Path '..').Path
    docker run --rm --network chatbot-persistence-tests_default --user 1654:1654 --memory 768m --cpus 1 --mount "type=bind,source=$repository/tests/linux-publish,target=/app,readonly" --mount "type=bind,source=$repository/experiments/embedding-spike/models,target=/model-assets,readonly" --mount "type=bind,source=$repository/experiments/embedding-spike/fixtures,target=/fixtures,readonly" -e 'CHATBOT_TEST_CONNECTION=Host=postgres;Database=chatbot_persistence_tests;Username=test_only;Password=disposable_test_only' -e CHATBOT_TEST_MODEL_ROOT=/model-assets -e CHATBOT_TEST_TOKEN_FIXTURE=/fixtures/minilm.json -w /app mcr.microsoft.com/dotnet/aspnet:10.0 dotnet Chatbot.IntegrationTests.dll
    if ($LASTEXITCODE -ne 0) { throw 'Linux integration checks failed' }
} finally {
    docker compose stop
    Pop-Location
}
