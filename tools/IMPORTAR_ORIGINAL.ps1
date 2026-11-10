# Local PS1 data import for a user-owned BIN/ISO.
# Run from PowerShell:
# .\tools\IMPORTAR_ORIGINAL.ps1 -Disc "C:\Jogos\Yu-Gi-Oh! FM Original (Drop x15).bin"
# Optional: -DueltraceReport "C:\...\investigacoes\...fusoes.json"
# Original disc bytes and generated descriptions never leave your PC.
param(
    [Parameter(Mandatory = $true)] [string]$Disc,
    [string]$Upstream = "",
    [string]$DueltraceReport = "",
    [switch]$Art
)
$ErrorActionPreference = "Stop"
$project = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
if (-not (Test-Path -LiteralPath $Disc -PathType Leaf)) {
    throw "Arquivo ISO/BIN nao encontrado: $Disc"
}
$discPath = (Resolve-Path -LiteralPath $Disc).Path
$pythonCmd = Get-Command "py" -ErrorAction SilentlyContinue
if (-not $pythonCmd) { $pythonCmd = Get-Command "python" -ErrorAction SilentlyContinue }
if (-not $pythonCmd) { throw "Python 3 nao encontrado. Instale Python e execute novamente." }
$python = $pythonCmd.Source
$upstreamPath = $Upstream
if (-not $upstreamPath) {
    $upstreamPath = Join-Path $project "PrivateExtracts\tools\fm-recompiled"
    if (-not (Test-Path (Join-Path $upstreamPath "tools\pc\fm_editor\gamedata.py"))) {
        if (-not (Get-Command "git" -ErrorAction SilentlyContinue)) {
            throw "Git nao encontrado para baixar o parser livre. Informe -Upstream."
        }
        New-Item -ItemType Directory -Force -Path (Split-Path $upstreamPath) | Out-Null
        & git clone --depth 1 --filter=blob:none "https://github.com/Unchiga/Yu-Gi-Oh-Forbidden-Memories-Recompiled.git" $upstreamPath
        if ($LASTEXITCODE -ne 0) { throw "Falha ao obter o parser livre." }
    }
}
Write-Host "Importando dados locais para o Unity (nenhuma ISO sera enviada ao GitHub)..."
$importArgs = @(
    (Join-Path $project "tools\import_fm_disc.py"),
    "--disc", $discPath,
    "--upstream", $upstreamPath,
    "--project", $project,
    "--edition", "original-ps1-drop-x15"
)
if ($Art) { $importArgs += "--art" }
& $python @importArgs
if ($LASTEXITCODE -ne 0) { throw "Extracao falhou; veja a mensagem acima." }
$exportFile = Join-Path $project "Assets\StreamingAssets\RebornContent\original_export.json"
& $python (Join-Path $project "tools\validate_import.py") $exportFile "--complete"
if ($LASTEXITCODE -ne 0) { throw "Exportacao incompleta ou inconsistente." }
if ($DueltraceReport) {
    if (-not (Test-Path -LiteralPath $DueltraceReport -PathType Leaf)) {
        throw "Relatorio DUELTRACE nao encontrado: $DueltraceReport"
    }
    $auditFile = Join-Path $project "PrivateExtracts\reborn-disc\auditoria_dueltrace.json"
    & $python (Join-Path $project "tools\audit_dueltrace_original.py") `
        "--export" $exportFile "--dueltrace" $DueltraceReport "--out" $auditFile
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Existem divergencias; nao ative receitas de fusao sem analisar a auditoria."
    }
}
Write-Host "Concluido: catalogo e receitas gravados somente no projeto Unity local."
Write-Host "Abra agora o projeto no Unity Hub e confira a mensagem de importacao."
