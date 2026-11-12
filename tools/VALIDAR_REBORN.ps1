# Reborn v0.26 - local read-only verification, no remote Actions required.
# Works with Python 3.10+ and .NET 8; does not need Node.js or a PS1 disc.
# Run from the Unity project root:
# powershell -ExecutionPolicy Bypass -File .\tools\VALIDAR_REBORN.ps1
param([switch]$ExigirFusoes)
$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Write-Host "REBORN 0.26 - VALIDACAO LOCAL" -ForegroundColor Cyan
Write-Host "Projeto: $root"

$python = Get-Command "py" -ErrorAction SilentlyContinue
if (-not $python) { $python = Get-Command "python" -ErrorAction SilentlyContinue }
if (-not $python) { throw "Python 3 nao encontrado. Verificacao local incompleta." }

$checks = @(
    "check_unity_editor_asmdef.py",
    "check_unity_editor_csharp_syntax.py",
    "check_keyboard_init_v0241.py",
    "check_duel_qa_v0242.py",
    "check_keyboard_fusion_v024.py",
    "check_fusion_assistant_v025.py",
    "check_fusion_chains_v026.py",
    "check_ai_fusion_v027.py",
    "check_opponent_vfx_v028.py"
)
foreach ($check in $checks) {
    Write-Host ("Validando: " + $check) -ForegroundColor Cyan
    & $python.Source (Join-Path $PSScriptRoot $check)
    if ($LASTEXITCODE -ne 0) { throw "Falha na validacao: $check (exit $LASTEXITCODE)" }
}

$dotnet = Get-Command "dotnet" -ErrorAction SilentlyContinue
if ($dotnet) {
    Write-Host "Executando testes deterministas do motor .NET..." -ForegroundColor Cyan
    & $dotnet.Source run --project (Join-Path $root "tools\Reborn.Core.Smoke\Reborn.Core.Smoke.csproj") --configuration Release
    if ($LASTEXITCODE -ne 0) { throw "Falha nos testes do motor Reborn.Core." }
} else {
    Write-Warning "dotnet nao esta no PATH; Core Smoke nao foi executado localmente."
}

$export = Join-Path $root "Assets\StreamingAssets\RebornContent\original_export.json"
if (Test-Path -LiteralPath $export) {
    Write-Host "Auditando receitas da ISO local..." -ForegroundColor Cyan
    & $python.Source (Join-Path $PSScriptRoot "diagnosticar_fusoes.py") --project $root
    if ($LASTEXITCODE -ne 0) {
        if ($ExigirFusoes) { throw "Receitas de fusao ausentes ou invalidas." }
        Write-Warning "O catalogo local precisa de receitas verificadas. Use IMPORTAR_ORIGINAL.ps1."
    }
} else {
    if ($ExigirFusoes) { throw "original_export.json ausente: nao ha tabela de fusoes." }
    Write-Warning "Sem original_export.json: o fallback DUELTRACE pode ter so fusoes observadas manualmente."
}

Write-Host "Verificacoes locais concluidas. Abra Unity 6000.3.28f1 e execute os testes EditMode." -ForegroundColor Green
Write-Warning "Scripts nao verificam a compilacao do Unity ou a jogabilidade visual."
