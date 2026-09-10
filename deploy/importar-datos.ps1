<#
.SINOPSIS
    Carga las planillas de datos-origen/ en la base publicada.

.DESCRIPCION
    Envoltura de `dotnet run --project src/Galeria.DataImport` para no tener que tipear la línea
    larga a mano. Corre desde la PC de desarrollo, con .NET 8 disponible
    (ver docs/INSTALACION_PASO_A_PASO.md).

    Por defecto AGREGA sobre una base recién publicada y vacía (la que deja publicar.ps1).
    Con -Rehacer borra la base y la reconstruye desde cero (esquema + datos) — usalo cada vez
    que corregís los .csv y querés reimportar.

.PARAMETRO Destino
    Archivo app.db a llenar. Por defecto la base publicada.

.PARAMETRO Rehacer
    Borra el app.db, vuelve a crear los dos esquemas (dominio + login) y reimporta.

.EJEMPLO
    .\deploy\importar-datos.ps1                # primera vez, después de publicar.ps1
    .\deploy\importar-datos.ps1 -Rehacer       # reimportar tras corregir los .csv
#>
param(
    [string]$Destino = "C:\GaleriaACATRAS\app\Datos\app.db",
    [switch]$Rehacer
)

$ErrorActionPreference = "Stop"
$raizRepo  = Split-Path -Parent $PSScriptRoot
$proyectoDI = Join-Path $raizRepo "src\Galeria.DataImport"
$proyectoWeb = Join-Path $raizRepo "src\Galeria.Web"
$proyectoInfra = Join-Path $raizRepo "src\Galeria.Infrastructure"
$origen = Join-Path $raizRepo "datos-origen"

if (-not (Test-Path $origen)) {
    throw "No existe la carpeta $origen (ahí van los .xlsm/.xlsx del cliente)."
}

$carpetaDestino = Split-Path -Parent $Destino
if (-not (Test-Path $carpetaDestino)) {
    New-Item -ItemType Directory -Path $carpetaDestino -Force | Out-Null
}

if ($Rehacer) {
    Write-Host "Borrando la base anterior y recreando los dos esquemas..." -ForegroundColor Yellow
    Remove-Item "$Destino*" -Force -ErrorAction SilentlyContinue

    $conn = "DataSource=$Destino;Cache=Shared"
    dotnet ef database update --project $proyectoInfra --startup-project $proyectoWeb --context GaleriaDbContext --connection $conn
    if ($LASTEXITCODE -ne 0) { throw "Falló la migración de GaleriaDbContext (código $LASTEXITCODE)." }
    dotnet ef database update --project $proyectoWeb --context ApplicationDbContext --connection $conn
    if ($LASTEXITCODE -ne 0) { throw "Falló la migración de ApplicationDbContext (código $LASTEXITCODE)." }
}

Write-Host "Importando $origen  ->  $Destino" -ForegroundColor Cyan
dotnet run --project $proyectoDI -- --origen $origen --salida $Destino
if ($LASTEXITCODE -ne 0) {
    throw "El importador terminó con errores (código $LASTEXITCODE)."
}

Write-Host "Listo. Revisá el informe: $origen\informe-importacion.txt" -ForegroundColor Green
