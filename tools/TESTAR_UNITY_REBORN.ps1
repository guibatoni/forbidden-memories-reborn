# Reborn 0.29 - runs actual Unity EditMode tests, not only .NET Core smoke.
# Requires the Unity Hub editor version in ProjectSettings/ProjectVersion.txt.
# Does not modify source, imported content, ISOs or player preferences.
#
# From the project root:
#   powershell -ExecutionPolicy Bypass -File .\tools\TESTAR_UNITY_REBORN.ps1
# Or pass -UnityExe "C:\path\Unity.exe" for a nonstandard install.
param(
    [string]$UnityExe = "",
    [string]$ReportDirectory = ""
)

$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$versionFile = Join-Path $root "ProjectSettings\ProjectVersion.txt"
if (-not (Test-Path -LiteralPath $versionFile)) {
    throw "ProjectSettings/ProjectVersion.txt nao encontrado. Execute na pasta do projeto Unity."
}
$versionLine = Get-Content -LiteralPath $versionFile |
    Where-Object { $_ -match '^m_EditorVersion:\s*(.+)$' } |
    Select-Object -First 1
if (-not $versionLine -or
    $versionLine -notmatch '^m_EditorVersion:\s*([0-9A-Za-z.]+)\s*$') {
    throw "Nao foi possivel identificar a versao Unity do projeto."
}
$expectedVersion = $Matches[1]
Write-Host ("REBORN - UNITY EDITMODE / Editor " + $expectedVersion) -ForegroundColor Cyan
Write-Host ("Projeto: " + $root)

if ([string]::IsNullOrWhiteSpace($UnityExe)) {
    $programFilesX86 = [Environment]::GetFolderPath("ProgramFilesX86")
    $candidates = @(
        (Join-Path $env:ProgramFiles "Unity\Hub\Editor\$expectedVersion\Editor\Unity.exe"),
        (Join-Path $programFilesX86 "Unity\Hub\Editor\$expectedVersion\Editor\Unity.exe"),
        (Join-Path $env:LOCALAPPDATA "Programs\Unity\Hub\Editor\$expectedVersion\Editor\Unity.exe")
    )
    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path -LiteralPath $candidate -PathType Leaf)) {
            $UnityExe = $candidate
            break
        }
    }
    if ([string]::IsNullOrWhiteSpace($UnityExe)) {
        throw ("Unity.exe versao " + $expectedVersion +
            " nao encontrado. Instale pelo Unity Hub ou informe -UnityExe 'C:\caminho\Unity.exe'.")
    }
}
if (-not (Test-Path -LiteralPath $UnityExe -PathType Leaf)) {
    throw ("Unity.exe invalido: " + $UnityExe)
}
if ([string]::IsNullOrWhiteSpace($ReportDirectory)) {
    # Library is excluded from git and never uploaded to GitHub Actions.
    $ReportDirectory = Join-Path $root "Library\RebornValidation"
}
New-Item -ItemType Directory -Path $ReportDirectory -Force | Out-Null
$results = Join-Path $ReportDirectory "Reborn-EditMode-results.xml"
$log = Join-Path $ReportDirectory "Reborn-Unity-Editor.log"
# Never accept last week's successful XML after a failed Unity launch.
if (Test-Path -LiteralPath $results) {
    Remove-Item -LiteralPath $results -Force
}
Write-Host "Unity: $UnityExe"
Write-Host "Resultado XML: $results"
Write-Host "Log: $log"
Write-Warning "Feche o Unity Editor deste projeto antes de iniciar o teste batchmode."

# Avoid -quit with Unity Test Runner. Batchmode exits when tests complete.
$argsUnity = @(
    "-batchmode", "-runTests", "-projectPath", $root,
    "-testPlatform", "EditMode", "-assemblyNames", "Reborn.Core.Tests",
    "-testResults", $results, "-logFile", $log
)
& $UnityExe @argsUnity
$exitCode = $LASTEXITCODE

if (-not (Test-Path -LiteralPath $results -PathType Leaf)) {
    if (Test-Path -LiteralPath $log) {
        Write-Host "Possiveis erros de compilacao/inicializacao:" -ForegroundColor Yellow
        Select-String -LiteralPath $log -Pattern 'error CS[0-9]+|Compilation failed|Compiler errors|Exception' |
            Select-Object -First 12 | ForEach-Object { Write-Host $_.Line }
    }
    throw ("Unity nao produziu XML de testes. Codigo de saida: " + $exitCode +
        ". Consulte " + $log)
}

try {
    [xml]$report = Get-Content -LiteralPath $results -Raw
    $testRun = $report.DocumentElement
    if ($null -eq $testRun -or $testRun.Name -ne "test-run") {
        throw "XML sem elemento NUnit3 test-run."
    }
    $total = [int]$testRun.GetAttribute("total")
    $failed = [int]$testRun.GetAttribute("failed")
    $errors = [int]$testRun.GetAttribute("errors")
    $result = $testRun.GetAttribute("result")
    Write-Host ("Resultados EditMode: total=" + $total +
        " falhas=" + $failed + " erros=" + $errors + " status=" + $result)
    if ($exitCode -ne 0 -or $total -le 0 -or $failed -ne 0 -or
        $errors -ne 0 -or $result -ne "Passed") {
        throw ("Teste Unity nao aprovado. ExitCode=" + $exitCode +
            ", NUnit result=" + $result)
    }
}
catch {
    Write-Host ("Abra o XML para detalhes: " + $results) -ForegroundColor Yellow
    throw
}

Write-Host "PASS: compilacao Unity e testes EditMode aprovados." -ForegroundColor Green
Write-Host ("Resultados e log: " + $ReportDirectory)
