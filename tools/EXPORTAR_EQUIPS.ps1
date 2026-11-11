# Generate the original PS1 Equip compatibility manifest without rescanning ISO.
# Requires the original local disc import's PrivateExtracts/reborn-disc/disc_tables.json.
param()
$ErrorActionPreference = "Stop"
$project = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$pythonCmd = Get-Command "py" -ErrorAction SilentlyContinue
if (-not $pythonCmd) { $pythonCmd = Get-Command "python" -ErrorAction SilentlyContinue }
if (-not $pythonCmd) { throw "Python 3 nao instalado ou fora do PATH." }
& $pythonCmd.Source (Join-Path $project "tools\export_equip_rules.py") "--project" $project
if ($LASTEXITCODE -ne 0) {
    throw "Nao foi possivel validar a tabela Equip extraida do mesmo disco."
}
Write-Host "Equipamentos exportados. Reinicie o Play no Unity para carregar as compatibilidades."
