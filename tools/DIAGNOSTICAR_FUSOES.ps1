# Audits the local Reborn fusion export without re-reading the BIN/ISO.
# Example: .\tools\DIAGNOSTICAR_FUSOES.ps1 -Card 16
param([int]$Card = 0)
$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$py = Get-Command "py" -ErrorAction SilentlyContinue
if (-not $py) { $py = Get-Command "python" -ErrorAction SilentlyContinue }
if (-not $py) { throw "Python 3 nao encontrado no PATH." }
$argsList = @(
    (Join-Path $root "tools\diagnosticar_fusoes.py"),
    "--project", $root
)
if ($Card -gt 0) { $argsList += @("--card", $Card.ToString()) }
& $py.Source @argsList
if ($LASTEXITCODE -ne 0) {
    throw "Diagnostico encontrou falha nas receitas importadas. Veja mensagens acima."
}
Write-Host "Diagnostico estrutural concluido. Execute uma fusao no Unity para confirmar."
