param()
$ErrorActionPreference = "Stop"
$project = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$pythonCmd = Get-Command "py" -ErrorAction SilentlyContinue
if (-not $pythonCmd) { $pythonCmd = Get-Command "python" -ErrorAction SilentlyContinue }
if (-not $pythonCmd) { throw "Python 3 nao instalado." }
& $pythonCmd.Source (Join-Path $project "tools\export_starter_pools.py") "--project" $project
if ($LASTEXITCODE -ne 0) {
    throw "Nao foi possivel gerar os sete grupos a partir de PrivateExtracts/reborn-disc/disc_tables.json."
}
Write-Host "Reabra o Play no Unity para selecionar um dos quatro decks originais sorteados."
