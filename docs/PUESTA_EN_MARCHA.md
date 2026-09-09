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

> Si solo tenés instalado el SDK de .NET 10 (o cualquier versión mayor), podés compilar igual
> **siempre que esté instalado el _targeting pack_ / runtime de .NET 8**. Si `dotnet build` se
> queja de que le falta `Microsoft.NETCore.App` 8.0 o el targeting pack de `net8.0`, instalá el
> SDK de .NET 8 y listo. Tener los dos instalados no molesta.

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

### 2.2 Correr el importador

```powershell
Set-Location "C:\Users\fpero\Desktop\Galería de arte"
dotnet run --project src/Galeria.DataImport -- --origen datos-origen --salida src/Galeria.Web/Datos/app.db --recrear
```

El importador:

1. **Recrea la base** desde cero (aplica las migraciones sobre un `app.db` vacío) si le pasás
   `--recrear`. Sin ese parámetro, agrega sobre la base existente.
2. Carga, en orden: parámetros del negocio → rubros → técnicas → artistas → obras (con su stock)
   → ventas → adelantos → alquileres → retiros → cambios de precio → liquidaciones históricas.
3. Al terminar deja un **informe** (`datos-origen/informe-importacion.txt`) con:
   - cuántos registros entraron de cada tipo;
   - los registros que **no** se pudieron cargar y por qué (fechas imposibles, moneda faltante,
     artista con el nombre escrito de varias formas, códigos duplicados, etc.).

**Revisá ese informe.** Los casos que el importador no puede resolver solo necesitan una
decisión del cliente o una corrección a mano en la planilla; después se vuelve a correr.

### 2.3 Verificar los datos cargados

```powershell
dotnet run --project src/Galeria.Web
```

Entrá a <http://localhost:5121>, logueá con el admin de desarrollo (ver 2.4) y revisá:

- **Obras**: el total y algunas fichas conocidas (costo, precio, stock, artista).
- **Artistas**: los saldos de un par de artistas contra la última liquidación real del Excel.
- **Resumen**: que los totales adeudados tengan sentido.

Si algo no cierra, corregí el importador o la planilla y volvé a correr con `--recrear`.

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

### 3.1 Poner la base con los datos reales en la carpeta publicada

`publicar.ps1` deja una base **limpia** (solo el esquema, sin datos). Copiá encima la base que
generaste en el paso 2:

```powershell
Copy-Item "C:\Users\fpero\Desktop\Galería de arte\src\Galeria.Web\Datos\app.db" `
          "C:\GaleriaACATRAS\app\Datos\app.db" -Force
```

Si en el paso 2 se generaron imágenes de obras, copiá también esa carpeta:

```powershell
Copy-Item "C:\Users\fpero\Desktop\Galería de arte\src\Galeria.Web\wwwroot\uploads" `
          "C:\GaleriaACATRAS\app\wwwroot\uploads" -Recurse -Force
```

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
| Cargar datos de nuevo | `dotnet run --project src/Galeria.DataImport -- --origen datos-origen --salida src/Galeria.Web/Datos/app.db --recrear` |

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
