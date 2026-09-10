<#
.SINOPSIS
    Publica el ERP (compilación Release) a una carpeta lista para instalar como servicio.

.DESCRIPCION
    Corre `dotnet publish` en modo Release y después aplica las migraciones de EF Core
    directamente contra la base en la carpeta publicada (--connection apuntado a esa ruta, sin
    depender del directorio de trabajo). El publish en Release NUNCA incluye la base de datos de
    desarrollo (ver Galeria.Web.csproj: esa copia solo pasa en Debug), así que esto siempre
    termina en una base limpia — sin datos de prueba ni el usuario admin de desarrollo.

    Se puede correr de nuevo para actualizar una instalación existente: aplica solo las
    migraciones nuevas, no pisa los datos que ya haya en la base.

    Requiere el SDK de .NET 8 (no solo el Runtime) porque usa `dotnet ef` — se corre en la PC
    de desarrollo, después se copia la carpeta ya publicada (con la base lista) a la PC de la
    galería, que solo necesita el Runtime. Ver docs/INSTALACION_PASO_A_PASO.md.

.PARAMETRO Destino
    Carpeta donde queda la app publicada. Por defecto una carpeta fuera del repo
    (C:\GaleriaACATRAS\app) para no mezclar binarios de producción con el código fuente.

.EJEMPLO
    .\deploy\publicar.ps1
    .\deploy\publicar.ps1 -Destino "D:\GaleriaACATRAS\app"
#>
param(
    [string]$Destino = "C:\GaleriaACATRAS\app"
)

$ErrorActionPreference = "Stop"
$raizRepo = Split-Path -Parent $PSScriptRoot
$proyecto = Join-Path $raizRepo "src\Galeria.Web\Galeria.Web.csproj"

Write-Host "Publicando $proyecto (Release) en $Destino ..." -ForegroundColor Cyan
dotnet publish $proyecto -c Release -o $Destino
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish terminó con errores (código $LASTEXITCODE)."
}

$carpetaDatos = Join-Path $Destino "Datos"
if (-not (Test-Path $carpetaDatos)) {
    New-Item -ItemType Directory -Path $carpetaDatos | Out-Null
}

$cadenaConexion = "DataSource=$(Join-Path $carpetaDatos 'app.db');Cache=Shared"
$proyectoInfra = Join-Path $raizRepo "src\Galeria.Infrastructure"

function Invoke-MigracionConReintento {
    param([string]$Descripcion, [string[]]$ArgumentosEf)

    for ($intento = 1; $intento -le 3; $intento++) {
        Write-Host $Descripcion -ForegroundColor Cyan
        dotnet ef @ArgumentosEf
        if ($LASTEXITCODE -eq 0) { return }

        if ($intento -lt 3) {
            Write-Host "Intento $intento fallo (puede ser un build en curso de otro proceso) - reintentando en 5s..." -ForegroundColor Yellow
            Start-Sleep -Seconds 5
        }
    }

    throw "$Descripcion termino con errores despues de 3 intentos (codigo $LASTEXITCODE)."
}

Invoke-MigracionConReintento "Aplicando migraciones de GaleriaDbContext (dominio: obras, ventas, etc.)..." `
    @("database", "update", "--project", $proyectoInfra, "--startup-project", $proyecto, "--context", "GaleriaDbContext", "--configuration", "Release", "--connection", $cadenaConexion)

Invoke-MigracionConReintento "Aplicando migraciones de ApplicationDbContext (login/Identity)..." `
    @("database", "update", "--project", $proyecto, "--context", "ApplicationDbContext", "--configuration", "Release", "--connection", $cadenaConexion)

Write-Host "Listo. Publicado y migrado en $Destino." -ForegroundColor Green
Write-Host "La primera vez, hace falta además crear el usuario admin (ver docs/INSTALACION_PASO_A_PASO.md)." -ForegroundColor Yellow
Write-Host "Próximo paso: copiar esta carpeta a la PC de la galería (si es otra máquina) y correr .\deploy\instalar-servicio.ps1 ahí, como Administrador." -ForegroundColor Cyan
