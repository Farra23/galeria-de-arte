# Puesta en marcha — ERP Galería ACATRAS

Paso a paso para dejar el sistema andando en la PC de la galería: funcional, sin errores, con los
datos reales cargados y con respaldo automático. Pensado para seguirse de arriba a abajo, una vez.

> Este documento es para quien instala el sistema. El manual del cliente (cómo se usa) es aparte:
> `docs/MANUAL_DE_USO.md`.

---

## 0. Panorama de lo que vas a hacer

| Etapa | Dónde se hace | Resultado |
|---|---|---|
| 1. Preparar la PC de desarrollo | Tu PC | Herramientas para compilar y migrar. |
| 2. Cargar los datos reales | Tu PC | Base `app.db` con artistas, obras e historial. |
| 3. Publicar la app | Tu PC | Carpeta `C:\GaleriaACATRAS\app` lista para copiar. |
| 4. Instalar en la PC de la galería | PC de la galería | La app corre sola como Servicio de Windows. |
| 5. Crear el usuario administrador | PC de la galería | Primer login. |
| 6. Programar el respaldo | PC de la galería | Copia mensual automática. |
| 7. Verificar | PC de la galería | Todo funciona y sobrevive a un reinicio. |

**Modelo de despliegue:** una sola PC, sin nube ni servidor. La app se sirve por HTTP en
`http://localhost:5121` en esa misma máquina. Toda la información (base de datos SQLite +
imágenes) vive en la carpeta `Datos`. El respaldo del cliente es copiar esa carpeta.

Si la PC de la galería es la misma donde desarrollás, las etapas 1–3 y 4–7 se hacen en la misma
máquina y no hay que copiar nada entre PCs.

---

## 1. Preparar la PC de desarrollo

### 1.1 Instalar el SDK de .NET 8

El proyecto es **.NET 8**. Necesitás el **SDK** (no solo el runtime) porque vas a usar
`dotnet publish` y `dotnet ef`.

- Descargar de <https://dotnet.microsoft.com/download/dotnet/8.0> → **SDK 8.0.x** para Windows x64.
- Verificar:

```powershell
dotnet --list-sdks
```

> **Sobre el runtime (importante en esta PC):** el `dotnet` que está primero en el `PATH` es el
> de .NET 10 (`C:\Users\fpero\.dotnet`), y ése **no** encuentra el runtime 8. El runtime 8.0.25/
> 8.0.26 sí está instalado, en `C:\Program Files\dotnet`. Consecuencias:
>
> - `dotnet build` y `dotnet test` funcionan con cualquiera (compilan `net8.0` sin problema).
> - Todo lo que **ejecuta** la app (`dotnet run`, `dotnet ef database update`, `deploy\publicar.ps1`)
>   tiene que usar el dotnet de `Program Files`. La forma más simple es anteponerlo al `PATH` en
>   la sesión de PowerShell:
>   ```powershell
>   $env:Path = "C:\Program Files\dotnet;" + $env:Path
>   ```
>   (o instalar el **SDK de .NET 8** completo, que deja todo resuelto sin tocar el PATH).
> - El importador (`src/Galeria.DataImport`) ya tiene `RollForward=Major`, así que corre con
>   cualquier dotnet aunque solo esté el 10.

### 1.2 Instalar la herramienta `dotnet-ef` 8

```powershell
dotnet tool install --global dotnet-ef --version 8.0.*
dotnet ef --version   # tiene que decir 8.0.x
```

### 1.3 Traer el código y compilar

```powershell
Set-Location "C:\Users\fpero\Desktop\Galería de arte"
dotnet build -c Release
```

Tiene que terminar con **0 errores y 0 advertencias**. Si falla acá, no sigas: resolvé el build
primero.

### 1.4 Correr los tests (opcional pero recomendado)

```powershell
dotnet test
```

Todos en verde.

---

## 2. Cargar los datos reales

La app se construyó y probó con datos de prueba. Antes de la entrega hay que reemplazarlos por
los datos reales del cliente, que vienen de las planillas de Excel.

### 2.1 Dejar las planillas actualizadas

Copiar los archivos de Excel **actualizados** que mandó el cliente en la carpeta `datos-origen`
del repo (misma estructura y nombres que los que ya estaban ahí):

```
datos-origen/
├── Acatràs sin los for next.xlsm   ← artistas, obras, rubros, técnicas, parámetros
├── Control.xlsx                    ← auditoría y cambios de precio
├── Liquidaciones.xlsm              ← pagos, adelantos, alquileres, retiros
├── Entregas.xlsx
├── Historico Entregas.xlsx
└── CorreoLiqui.xlsx
```

> `datos-origen/` está en `.gitignore` — nunca se sube al repo (son datos personales de cientos
> de artistas).

### 2.2 Qué hace el importador

`src/Galeria.DataImport` es un proyecto de consola de un solo uso. Lee las planillas con
ClosedXML y escribe en la base con EF Core (el esquema real del ERP). Carga, en orden:

1. **Parámetros** del negocio (IVA, redondeos), **rubros** y **técnicas** — deduplicando las
   variantes del Excel ("Acrilico S/lienzo" y "Acrilico s/ Lienzo" son una sola).
2. **Artistas** (~265; descarta las ranuras de código sin nombre).
3. **Obras** (~14.800) con su stock, costo, precio, rubro y técnica; estado Disponible o Sin stock
   según la existencia.
4. **Ventas** (~14.100) — toda la historia. Para las ventas viejas cuya obra ya no está en el
   catálogo, crea una "obra fantasma" (existencia 0) con los datos de la propia venta.
5. **Adelantos** que todavía no fueron descontados (los que afectan el saldo actual).
6. **Historial de precios** desde `Control.xlsx` (~1.300 cambios) → pestaña "Historial de precios"
   de cada obra.
7. **Liquidaciones de apertura**: toma de la hoja `Listado Ventas` el saldo real que se le debe
   hoy a cada artista, deja pendientes sus ventas más nuevas hasta ese monto, y marca todo lo
   anterior como pagado con una liquidación de apertura confirmada. Así los saldos arrancan
   exactos sin arrastrar 17 años de ventas ya cobradas.

Parámetros:

- `--origen <carpeta>` — dónde están los `.xlsm/.xlsx` (default `datos-origen`).
- `--salida <archivo>` — la base SQLite destino.
- `--recrear` — borra la base y crea **solo el esquema del dominio** desde cero. Útil para
  iterar en desarrollo; **no** deja las tablas de login, así que con una base así no se puede
  entrar a la app.

Al terminar deja `datos-origen/informe-importacion.txt` con los registros que entraron, las
filas rechazadas agrupadas por motivo, y **avisos para revisar con el cliente**: obras sin
moneda (van como Pesos), rubros/técnicas parecidos que quizás sean lo mismo, y los artistas
cuyo saldo hay que confirmar a mano (nombre partido, o saldo + adelantos abiertos a la vez).

### 2.3 Cargar y verificar en desarrollo

Como `--recrear` no crea las tablas de login, para probar en tu PC conviene armar una base con
los **dos** esquemas y después importar encima:

```powershell
$env:Path = "C:\Program Files\dotnet;" + $env:Path
Set-Location "C:\Users\fpero\Desktop\Galería de arte"

# base nueva con los dos DbContext
$conn = "DataSource=$PWD\src\Galeria.Web\Datos\app.db;Cache=Shared"
Remove-Item src\Galeria.Web\Datos\app.db* -ErrorAction SilentlyContinue
dotnet ef database update --project src/Galeria.Infrastructure --startup-project src/Galeria.Web --context GaleriaDbContext --connection $conn
dotnet ef database update --project src/Galeria.Web --context ApplicationDbContext --connection $conn

# cargar los datos reales (sin --recrear: agrega sobre la base recién migrada)
dotnet run --project src/Galeria.DataImport -- --origen datos-origen --salida src\Galeria.Web\Datos\app.db

# levantar la app para revisar
dotnet run --project src/Galeria.Web
```

Entrá a <http://localhost:5121>, logueá con el admin de desarrollo (ver 2.4) y revisá:

- **Resumen**: el total adeudado a artistas y la lista de artistas con saldo — tienen que
  coincidir con la hoja `Listado Ventas` del Excel.
- **Obras** / **Artistas**: buscá un artista conocido y contrastá su saldo, sus obras y precios.
- El **informe** de la importación, punto por punto.

Si algo no cierra, se corrige el importador o la planilla y se vuelve a empezar desde
`Remove-Item`.

### 2.4 Usuario admin en desarrollo

Para poder entrar mientras verificás, en tu PC:

```powershell
dotnet user-secrets set "AdminSeed:UserName" "admin" --project src/Galeria.Web
dotnet user-secrets set "AdminSeed:Password" "admin1234" --project src/Galeria.Web
```

Estos secretos **no** viajan a la app publicada — en la PC de la galería el admin se crea con
variables de entorno (paso 5).

---

## 3. Publicar la app

En la PC de desarrollo, desde la raíz del repo:

```powershell
.\deploy\publicar.ps1
```

Esto:

- Compila en **Release** en `C:\GaleriaACATRAS\app` (cambialo con `-Destino "D:\..."` si querés).
- Aplica las migraciones de EF Core de **los dos DbContext** (`GaleriaDbContext` del dominio y
  `ApplicationDbContext` del login) sobre la base en la carpeta publicada.
- **Nunca** copia la base de desarrollo (esa copia solo pasa en compilación Debug).

### 3.1 Cargar los datos reales en la base publicada

`publicar.ps1` deja en `C:\GaleriaACATRAS\app\Datos\app.db` una base **limpia**, con los dos
esquemas (dominio + login) ya migrados y **sin datos**. Corré el importador apuntándolo a esa
base, **sin** `--recrear` (así conserva las tablas de login):

```powershell
$env:Path = "C:\Program Files\dotnet;" + $env:Path
Set-Location "C:\Users\fpero\Desktop\Galería de arte"
dotnet run --project src/Galeria.DataImport -- `
    --origen datos-origen --salida "C:\GaleriaACATRAS\app\Datos\app.db"
```

Revisá el informe (`datos-origen/informe-importacion.txt`) una vez más antes de seguir.

> No hace falta copiar ninguna base entre carpetas: la publicada ya queda con los datos adentro.
> Las imágenes de obras (`wwwroot/uploads/`) el cliente todavía no las tiene — se van cargando
> desde la app a medida que las saca.

### 3.2 Si la galería es otra PC

Copiá la carpeta completa `C:\GaleriaACATRAS\app` (ya con la base y las imágenes adentro) a la
PC de la galería por USB o red. En esa PC alcanza con el **ASP.NET Core Runtime 8.0** (no el SDK):

- <https://dotnet.microsoft.com/download/dotnet/8.0> → **ASP.NET Core Runtime 8.0.x** → Windows
  Hosting Bundle o el instalador x64.

---

## 4. Instalar como Servicio de Windows (PC de la galería)

Abrir **PowerShell como Administrador** (clic derecho → *Ejecutar como administrador*) y, desde
la carpeta del repo (o donde tengas los scripts de `deploy`):

```powershell
.\deploy\instalar-servicio.ps1
```

Crea el servicio **GaleriaACATRAS**:

- arranca solo con Windows;
- si el proceso se cuelga, Windows lo reinicia (hasta 3 veces, 5 s entre intentos);
- lee la app de `C:\GaleriaACATRAS\app\Galeria.Web.exe` (cambiá con `-RutaApp` si publicaste en
  otro lado).

Para desinstalarlo (antes de una reinstalación limpia): `.\deploy\desinstalar-servicio.ps1`.

> Cada vez que publiques una versión nueva, volvé a correr `instalar-servicio.ps1` como
> Administrador: reinstala el servicio apuntando al `.exe` actualizado.

---

## 5. Crear el usuario administrador (solo la primera vez)

Con la base recién puesta no hay ningún usuario. La app lo crea al arrancar, leyendo dos
variables de entorno **de máquina**. En PowerShell **como Administrador**, ANTES de que el
servicio arranque por primera vez (o reiniciándolo después):

```powershell
[Environment]::SetEnvironmentVariable("AdminSeed__UserName", "admin", "Machine")
[Environment]::SetEnvironmentVariable("AdminSeed__Password", "<una contraseña fuerte>", "Machine")
Restart-Service GaleriaACATRAS
```

Notas:

- La **doble raya baja** (`__`) es a propósito: así .NET la mapea a `AdminSeed:UserName`.
- Elegí una contraseña fuerte real y guardala donde el cliente la tenga (no en el repo).
- Si faltan estas variables, la app **no arranca** y deja un error claro en el log — es a
  propósito, para no crear nunca un admin con contraseña por defecto.
- Una vez creado el usuario, podés borrar la variable `AdminSeed__Password` si querés; el
  usuario ya queda en la base. (Si algún día borrás toda la base, volvés a necesitarla.)

Después de entrar la primera vez, cambiá la contraseña desde *Configuración → Usuarios* y creá
las cuentas de los empleados que vayan a usar el sistema.

---

## 6. Programar el respaldo mensual

PowerShell **como Administrador**:

```powershell
.\deploy\instalar-tarea-backup.ps1 -DestinoBackups "D:\BackupsGaleria"
```

Registra una Tarea de Windows que el **día 1 de cada mes a las 03:00** comprime `Datos\app.db` +
las imágenes de obras a un `.zip` en la carpeta que le indiques.

> **Importante:** apuntá `-DestinoBackups` a un **disco externo** o a una **carpeta sincronizada
> con OneDrive / Google Drive**. Un respaldo en el mismo disco de la PC no protege contra una
> falla de ese disco.

Backup manual en cualquier momento (no necesita ser Administrador):

```powershell
.\deploy\hacer-backup.ps1 -DestinoBackups "D:\BackupsGaleria"
```

Para más frecuencia que mensual, ver el comentario dentro de `instalar-tarea-backup.ps1`
(cambiar `/SC MONTHLY /D 1` por `/SC WEEKLY /D SUN`).

---

## 7. Verificar

En la PC de la galería:

1. **Navegador** en <http://localhost:5121> → tiene que cargar la pantalla de login.
2. **Login** con el admin creado en el paso 5.
3. Revisar que **Obras**, **Artistas** y **Resumen** muestren los datos reales.
4. **Reiniciar la PC** y, sin tocar nada, volver a abrir <http://localhost:5121>: el sistema
   tiene que responder solo (confirma que el arranque automático quedó bien).
5. Comprobaciones de estado:

```powershell
Get-Service GaleriaACATRAS                       # Status = Running
Get-ScheduledTask GaleriaACATRAS-Backup          # State = Ready
Start-ScheduledTask -TaskName GaleriaACATRAS-Backup   # probar el backup una vez
```

6. Confirmar que se generó el `.zip` en la carpeta de backups.

---

## 8. Dejar al cliente

Antes de irte, dejale por escrito (o en un papel pegado a la PC):

- La dirección: **http://localhost:5121** (y cómo crear un acceso directo en el escritorio /
  fijarlo en la barra del navegador).
- El **usuario y la contraseña** del administrador.
- Dónde están los **backups** y cada cuánto se hacen.
- Que si algún día la app "no abre", lo primero es revisar que la PC esté encendida y, si hace
  falta, `Restart-Service GaleriaACATRAS` desde PowerShell como Administrador.
- El **manual de uso** (`docs/MANUAL_DE_USO.md`, o su PDF).

### Pendientes conocidos (contarle al cliente, y anotar para después)

- **Listas grandes con tope de 500 filas.** Obras (~15.000) y Ventas (~14.000) muestran las
  primeras 500 y avisan "filtrá para ver el resto". Con cualquier filtro (artista, texto, rango)
  andan perfecto. La paginación de verdad (página 1, 2, 3…) queda pendiente. La exportación a
  CSV sí trae todo.
- **Saldos de apertura a confirmar.** El informe de importación marca ~4 artistas cuyo saldo hay
  que revisar a mano con el cliente (nombre escrito de varias formas, o saldo pendiente y
  adelantos abiertos a la vez). "Victoria Gibbs / Taller Govinda" en particular no matcheó
  ningún artista del maestro: su saldo hay que cargarlo desde la app.
- **Historial de liquidaciones no detallado.** Las liquidaciones viejas entraron como una sola
  "liquidación de apertura" por artista, no una por una. El detalle fino sigue en el Excel.
- **Rubros y técnicas con variantes.** El Excel trae "Ensamble/Ensamblajes/Esamblajes" y
  similares. El informe las lista; conviene una pasada de limpieza desde
  *Configuración → Rubros y técnicas*.

---

## 9. Actualizar a una versión nueva (más adelante)

1. En tu PC: `git pull`, `dotnet build -c Release`, `dotnet test`.
2. `.\deploy\publicar.ps1` — aplica solo las migraciones nuevas, **no pisa los datos**.
3. Si la galería es otra PC: copiar la carpeta publicada actualizada (podés dejar afuera
   `Datos\` para no pisar la base de producción; copiá solo los binarios).
4. En la PC de la galería, como Administrador: `.\deploy\instalar-servicio.ps1`.
5. Verificar con el paso 7 (puntos 1–3).

---

## 10. Referencia rápida de comandos

| Necesito… | Comando |
|---|---|
| Ver el estado del servicio | `Get-Service GaleriaACATRAS` |
| Reiniciar la app | `Restart-Service GaleriaACATRAS` (Admin) |
| Parar / arrancar | `Stop-Service` / `Start-Service GaleriaACATRAS` (Admin) |
| Ver los logs del servicio | Visor de eventos → Registros de Windows → Aplicación |
| Backup ahora | `.\deploy\hacer-backup.ps1 -DestinoBackups "D:\BackupsGaleria"` |
| Publicar nueva versión | `.\deploy\publicar.ps1` |
| Reinstalar servicio | `.\deploy\instalar-servicio.ps1` (Admin) |
| Cargar datos en la base publicada | `dotnet run --project src/Galeria.DataImport -- --origen datos-origen --salida "C:\GaleriaACATRAS\app\Datos\app.db"` |
| Rearmar la base de desarrollo | ver §2.3 (migrar los dos DbContext + importar sin `--recrear`) |

---

## 11. Decisiones de esta configuración (por qué es así)

- **Sin HTTPS**: una sola PC en red local, sin certificado ni exposición a internet. Forzar HTTPS
  solo daría advertencias. Si algún día se expone fuera de la red local, es lo primero a revertir
  (`Program.cs`).
- **Un solo archivo de base** (`Datos\app.db`) compartido entre el dominio y el login: un solo
  archivo = un solo backup para el cliente.
- **Servicio de Windows y no una tarea / un `.bat`**: arranca sin que nadie inicie sesión en
  Windows y se recupera solo si se cae.
- **Backup mensual**: es lo que pidió el cliente para arrancar simple. En un sistema con
  movimientos diarios de plata conviene subirlo a semanal en cuanto se pueda.
