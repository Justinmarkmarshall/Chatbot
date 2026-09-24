param([switch]$NoRestore)
$ErrorActionPreference = 'Stop'
Push-Location (Join-Path $PSScriptRoot '..')
try {
    # Separate output avoids locking conflicts with a locally running web app or worker.
    $arguments = @('test', 'tests/Chatbot.UnitTests/Chatbot.UnitTests.csproj', '-p:OutputPath=obj/unit-build/', '--verbosity', 'minimal')
    if ($NoRestore) { $arguments += '--no-restore' }
    dotnet @arguments
    if ($LASTEXITCODE -ne 0) { throw 'Unit tests failed' }
} finally {
    Pop-Location
}
