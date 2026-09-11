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

    Requiere el SDK de .NET 10 (no solo el Runtime) porque usa `dotnet ef`. Se corre en la PC
    de desarrollo y después se copia la carpeta publicada a la PC de la galería.

    PUBLICACIÓN AUTOCONTENIDA (por defecto): la carpeta publicada lleva el runtime de .NET
    adentro, así que la PC de la galería NO necesita tener .NET instalado. Eso saca de encima
    toda una categoría de fallas futuras: "dejó de arrancar porque desinstalaron algo", "hay que
    instalar el runtime nuevo cada vez que sale una versión", "la PC quedó con una versión de
    .NET que ya no tiene soporte". La carpeta pesa ~100 MB más; para un copiado por pendrive
    cada varios meses, es un precio que conviene pagar.
    Con -Autocontenido:$false se publica como antes (la PC de la galería necesita el Runtime).

.PARAMETRO Destino
    Carpeta donde queda la app publicada. Por defecto una carpeta fuera del repo
    (C:\GaleriaACATRAS\app) para no mezclar binarios de producción con el código fuente.

.EJEMPLO
    .\deploy\publicar.ps1
    .\deploy\publicar.ps1 -Destino "D:\GaleriaACATRAS\app"
#>
param(
    [string]$Destino = "C:\GaleriaACATRAS\app",
    [switch]$Autocontenido = $true,
    [string]$Arquitectura = "win-x64"
)

$ErrorActionPreference = "Stop"
$raizRepo = Split-Path -Parent $PSScriptRoot
$proyecto = Join-Path $raizRepo "src\Galeria.Web\Galeria.Web.csproj"

if ($Autocontenido) {
    Write-Host "Publicando $proyecto (Release, autocontenido $Arquitectura) en $Destino ..." -ForegroundColor Cyan
    Write-Host "  La carpeta va a incluir el runtime de .NET: la PC de la galería no necesita tenerlo instalado." -ForegroundColor Gray
    dotnet publish $proyecto -c Release -o $Destino -r $Arquitectura --self-contained true
} else {
    Write-Host "Publicando $proyecto (Release, requiere Runtime instalado) en $Destino ..." -ForegroundColor Cyan
    dotnet publish $proyecto -c Release -o $Destino
}
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
Write-Host "Verificá la instalación con: .\deploy\verificar-instalacion.ps1 -RutaApp `"$Destino`"" -ForegroundColor Cyan
Write-Host "La primera vez, hace falta además crear el usuario admin (ver docs/INSTALACION_PASO_A_PASO.md)." -ForegroundColor Yellow
Write-Host "Próximo paso: copiar esta carpeta a la PC de la galería (si es otra máquina) y correr .\deploy\instalar-servicio.ps1 ahí, como Administrador." -ForegroundColor Cyan
