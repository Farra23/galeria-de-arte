<#
.SINOPSIS
    Carga las planillas de datos-origen/ en la base publicada.

.DESCRIPCION
    Envoltura de `dotnet run --project src/Galeria.DataImport` para no tener que tipear la línea
    larga a mano. Corre desde la PC de desarrollo, con el .NET 8 disponible (ver
    docs/INSTALACION_PASO_A_PASO.md §Paso 1).

    NO usa --recrear: agrega sobre la base que dejó publicar.ps1 (conserva las tablas de login).
    Para empezar de cero, corré antes .\deploy\publicar.ps1.

.PARAMETRO Destino
    Archivo app.db a llenar. Por defecto la base publicada.

.EJEMPLO
    .\deploy\importar-datos.ps1
    .\deploy\importar-datos.ps1 -Destino "D:\GaleriaACATRAS\app\Datos\app.db"
#>
param(
    [string]$Destino = "C:\GaleriaACATRAS\app\Datos\app.db"
)

$ErrorActionPreference = "Stop"
$raizRepo = Split-Path -Parent $PSScriptRoot
$proyecto = Join-Path $raizRepo "src\Galeria.DataImport"
$origen   = Join-Path $raizRepo "datos-origen"

if (-not (Test-Path $origen)) {
    throw "No existe la carpeta $origen (ahí van los .xlsm/.xlsx del cliente)."
}

$carpetaDestino = Split-Path -Parent $Destino
if (-not (Test-Path $carpetaDestino)) {
    throw "No existe $carpetaDestino. Corré .\deploy\publicar.ps1 primero."
}

Write-Host "Importando $origen  ->  $Destino" -ForegroundColor Cyan
dotnet run --project $proyecto -- --origen $origen --salida $Destino
if ($LASTEXITCODE -ne 0) {
    throw "El importador terminó con errores (código $LASTEXITCODE)."
}

Write-Host "Listo. Revisá el informe: $origen\informe-importacion.txt" -ForegroundColor Green
