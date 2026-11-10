# Import data from an installed DUELTRACE profile (no ROM rescan required).
# Usage: .\tools\IMPORTAR_DO_DUELTRACE.ps1
# Or: .\tools\IMPORTAR_DO_DUELTRACE.ps1 -Perfil "C:\...\profiles\<sha>\profile.json"
# For a batch reviewed in DUELTRACE, pass -IncluirLoteRevisado AND -RelatorioFusoes.
param(
    [string]$Perfil = "",
    [string]$RelatorioFusoes = "",
    [switch]$IncluirLoteRevisado,
    [switch]$ArtesLocais
)
$ErrorActionPreference = "Stop"
$project = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$pythonCmd = Get-Command "py" -ErrorAction SilentlyContinue
if (-not $pythonCmd) { $pythonCmd = Get-Command "python" -ErrorAction SilentlyContinue }
if (-not $pythonCmd) { throw "Python 3 nao instalado ou fora do PATH." }
$python = $pythonCmd.Source
$parameters = @(
    (Join-Path $project "tools\import_dueltrace_profile.py"),
    "--project", $project
)
if ($Perfil) {
    if (-not (Test-Path -LiteralPath $Perfil -PathType Leaf)) {
        throw "Perfil DUELTRACE nao encontrado: $Perfil"
    }
    $parameters += @("--profile", (Resolve-Path -LiteralPath $Perfil).Path)
}
if ($ArtesLocais) { $parameters += "--copy-local-art" }
if ($IncluirLoteRevisado) {
    if (-not $RelatorioFusoes) {
        throw "-IncluirLoteRevisado exige -RelatorioFusoes validado pelo DUELTRACE."
    }
    $parameters += "--include-reviewed-batch"
}
if ($RelatorioFusoes) {
    if (-not (Test-Path -LiteralPath $RelatorioFusoes -PathType Leaf)) {
        throw "Relatorio de fusoes nao encontrado: $RelatorioFusoes"
    }
    $parameters += @("--fusion-report", (Resolve-Path -LiteralPath $RelatorioFusoes).Path)
}
Write-Host "DUELTRACE -> REBORN: lendo apenas o perfil local de dados..."
& $python @parameters
if ($LASTEXITCODE -ne 0) { throw "Importacao DUELTRACE interrompida. O jogo original nao foi alterado." }
$export = Join-Path $project "Assets\StreamingAssets\RebornContent\original_export.json"
& $python (Join-Path $project "tools\validate_import.py") $export "--complete"
if ($LASTEXITCODE -ne 0) { throw "O Reborn rejeitou o export; consulte o relatorio privado." }
Write-Host "Concluido: abra o projeto Unity para usar cartas, fusoes e textos importados."
Write-Host "Os efeitos de magia sao informativos ate a rotina ser validada no motor."
