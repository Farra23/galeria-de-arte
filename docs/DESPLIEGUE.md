# Despliegue en la PC de la galería

Guía técnica para instalar el ERP en la PC de la galería como Servicio de Windows: arranca solo
con la PC, se reinicia si se cuelga, y queda con un backup mensual automático. Pensada para
**una sola PC**, sin hosting en la nube ni servidor remoto — se accede desde `http://localhost:5121`
en esa misma máquina (o desde otra PC de la misma red local apuntando a la IP de esa máquina).

El manual de uso para el cliente (no técnico) es un documento aparte.

## Requisitos

- **En la PC de desarrollo** (donde se corre `publicar.ps1`): el **SDK de .NET 8** completo —
  hace falta para `dotnet publish` y para `dotnet ef database update`.
- **En la PC de la galería** (donde corre el servicio): alcanza con el
  **ASP.NET Core Runtime 8.0** ("Hosting Bundle" si se instala como servicio de IIS más adelante;
  para esto, con el runtime normal alcanza) — no hace falta el SDK ahí. Se descarga desde la
  página oficial de .NET 8 ("Runtime" → "ASP.NET Core Runtime", no "SDK").
- PowerShell (el que ya viene con Windows alcanza — los scripts son compatibles con Windows
  PowerShell 5.1, no hace falta instalar PowerShell 7).

## Paso a paso (primera instalación)

### 1. Publicar y migrar (en la PC de desarrollo)

```powershell
.\deploy\publicar.ps1
```

Por defecto publica en `C:\GaleriaACATRAS\app`. Esto:
- Compila en modo Release.
- Aplica las migraciones de EF Core (los dos `DbContext`: `GaleriaDbContext` y
  `ApplicationDbContext`, comparten el mismo archivo `Datos\app.db`) directamente contra la base
  en la carpeta publicada.
- **Nunca** incluye la base de datos de desarrollo (ver `Galeria.Web.csproj`: esa copia solo pasa
  en compilación Debug) — el resultado es siempre una base limpia, sin datos de prueba.

Si la PC de la galería es una máquina distinta a la de desarrollo: copiar la carpeta completa
(`C:\GaleriaACATRAS\app`, ya con la base migrada adentro) ahí — por USB, red local, lo que sea más
cómodo. No hace falta el SDK en la máquina de destino porque la base ya viene lista.

### 2. Crear el usuario administrador (solo la primera vez)

Con la base recién migrada no existe ningún usuario todavía. `IdentitySeeder.SeedAdminAsync`
(`Program.cs`, se corre solo al arrancar la app) lo crea la primera vez, leyendo las claves de
configuración `AdminSeed:UserName` y `AdminSeed:Password` — si faltan, la app tira una excepción
clara al arrancar en vez de crear un admin con contraseña por defecto (a propósito, ver
`IdentitySeeder.cs`).

En desarrollo esas claves viven en `dotnet user-secrets`, que no existe en un `.exe` publicado.
En la PC de la galería, definirlas como **variables de entorno de máquina** ANTES de instalar el
servicio (la doble raya baja mapea a `:` en la configuración de .NET), en PowerShell como
Administrador:

```powershell
[Environment]::SetEnvironmentVariable("AdminSeed__UserName", "admin", "Machine")
[Environment]::SetEnvironmentVariable("AdminSeed__Password", "<una contraseña fuerte acá>", "Machine")
```

Un servicio de Windows arranca con las variables de entorno de máquina vigentes en ese momento —
si el servicio ya estaba instalado y corriendo, hay que reiniciarlo (`Restart-Service
GaleriaACATRAS`) después de definirlas para que las tome. **No** dejar la contraseña en
`appsettings.json` del repo (eso se sube a git).

### 3. Instalar el servicio de Windows

En la PC de la galería, PowerShell **como Administrador**:

```powershell
.\deploy\instalar-servicio.ps1
```

Crea el servicio `GaleriaACATRAS`, configurado para arrancar solo con Windows y reiniciarse solo
si se cuelga (hasta 3 veces, esperando 5 segundos entre cada intento). Se puede correr de nuevo
después de publicar una versión nueva — reinstala el servicio tomando el `.exe` actualizado.

Para desinstalarlo (por ejemplo, antes de una reinstalación limpia): `.\deploy\desinstalar-servicio.ps1`
(también como Administrador).

### 4. Programar el backup mensual

También como Administrador:

```powershell
.\deploy\instalar-tarea-backup.ps1
```

Registra una Tarea de Windows que corre `hacer-backup.ps1` el día 1 de cada mes a las 03:00,
copiando `Datos\app.db` y las imágenes de obras (`wwwroot\uploads\obras`) a un `.zip` en
`C:\GaleriaACATRAS\backups`. **Recomendación importante**: ese destino de backups debería apuntar
a un disco externo o a una carpeta sincronizada con OneDrive/Google Drive, no solo al disco
interno de la misma PC — si ese disco falla, un backup guardado ahí mismo no sirve de nada.

Se puede correr un backup manual en cualquier momento con `.\deploy\hacer-backup.ps1` (no
requiere ser Administrador).

### 5. Verificar

- Abrir el navegador en `http://localhost:5121` (o el puerto configurado en `appsettings.json`,
  clave `Urls`) y confirmar que carga la pantalla de login.
- Reiniciar la PC y confirmar que, sin que nadie haga nada, el sistema vuelve a responder solo en
  esa URL — así se confirma que el arranque automático del servicio quedó bien configurado.
- `Get-Service GaleriaACATRAS` para ver el estado del servicio en cualquier momento.
- `Get-ScheduledTask GaleriaACATRAS-Backup` para confirmar que la tarea de backup sigue
  registrada.

## Actualizar a una versión nueva

1. `.\deploy\publicar.ps1` de nuevo (aplica solo las migraciones nuevas, no pisa los datos
   existentes).
2. Copiar la carpeta publicada actualizada a la PC de la galería (si son máquinas distintas).
3. `.\deploy\instalar-servicio.ps1` de nuevo, como Administrador (reinstala el servicio apuntando
   al `.exe` nuevo).

## Decisiones de esta configuración (por qué)

- **Sin HTTPS**: la app se sirve por HTTP plano en `localhost` (ver `Program.cs` — se sacó
  `UseHttpsRedirection`/`UseHsts` a propósito). Es una sola PC sin certificado ni exposición a
  internet; forzar HTTPS ahí solo generaría advertencias sin aportar seguridad real. Si en algún
  momento esto se expone fuera de la red local de la galería, es lo primero que hay que revertir.
- **Un solo archivo de base de datos** (`Datos\app.db`) compartido entre el dominio del negocio y
  el login (Identity) — decisión ya tomada antes de este cambio (ver comentario en `Program.cs`):
  un solo archivo es un solo backup para el cliente.
- **Backup mensual, no diario**: pedido explícito para arrancar simple. Ver el comentario dentro
  de `instalar-tarea-backup.ps1` — en un sistema con movimientos diarios de plata y stock, un
  backup mensual puede perder hasta un mes de datos si el disco falla justo antes. Cambiar a
  semanal es una línea (`/SC MONTHLY /D 1` → `/SC WEEKLY /D SUN`) si en algún momento lo piden.

## Pendiente / a decidir

- Si la galería quiere acceder desde más de un dispositivo en su red local (ej. una tablet en el
  mostrador además de la PC principal), hay que confirmar que el firewall de Windows deje pasar
  conexiones entrantes al puerto configurado (por defecto, Kestrel en `localhost` no acepta
  conexiones de otras máquinas — habría que cambiar el binding a `http://0.0.0.0:5121` o similar,
  lo cual amplía la superficie expuesta en la red local y merece revisarse antes de habilitarlo).
