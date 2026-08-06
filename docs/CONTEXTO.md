CONTEXTO — ERP Galería ACATRAS
Qué es
Migración de un ERP artesanal en Excel + VBA (en uso desde 2009) a una app web.
Galería de arte en consignación, Montevideo, Uruguay. Recibe obras de artistas, las vende y les liquida su parte.

Alcance v1: panel de gestión interno. Sin sitio público. Un usuario. Primera versión sin datos migrados.

1. Stack decidido
Pieza	Elección	Por qué
Framework	Blazor Web App (.NET 8), InteractiveServer	Es lo que el dev ya sabe. Interactividad natural para autocompletado y cálculo en vivo
ORM	EF Core 8.0.25	Migraciones versionadas en el repo
Base	SQLite	Un archivo. El backup del cliente es copiar una carpeta
Login	ASP.NET Core Identity	Ya viene del template, con SQLite
PDF	QuestPDF 2024.12.3	Liquidaciones y certificados
Mail	MailKit 4.17.0	SMTP de Gmail
WhatsApp	Link wa.me	Sin API paga — el cliente no quiere costos mensuales
Imágenes	En disco, no en la BD	Mantiene el .db chico y el backup simple
Tests	xUnit + FluentAssertions 6.12.2	Motor de liquidación testeado sin BD
Descartados y por qué: WordPress (es un CMS, no tiene forma de ERP) · HTML/CSS/JS solos (sin backend ni persistencia) · Python/Django (bueno, pero el dev no lo sabe y es su primer cliente real — riesgo innecesario) · Vercel (no corre .NET).

Deploy: demo en hosting gratuito de .NET + Postgres gratis para mostrarle al dueño; producción en la PC de la galería, publicado self-contained (sin instalar .NET), con un .bat que lo levanta. Costo mensual: $0. El cliente no quiere pagos recurrentes.

2. Entorno exacto
Ruta:        C:\Users\fpero\Desktop\Galería de arte    ← con acento y espacios
SDK:         .NET 8.0.419 (NO hay .NET 9 ni 10)
Runtimes:    8.0.25 / 8.0.26
dotnet-ef:   8.0.25 global
IDE:         Rider
Git:         2.53.0 · repo inicializado, rama main
SO:          Windows 11
Terminal:    PowerShell
⚠️ La carpeta no se pudo renombrar a galeria-acatras porque la sesión de Claude Code la tiene tomada. Es cosmético, el repo de GitHub se llama distinto igual.

3. Estado del código
Compila: 0 errores, 0 advertencias. Sin commit todavía (80 archivos en el stage).

Galería de arte/
├── .gitignore              ← excluye datos-origen/, Datos/, *.db, bin/, obj/
├── Galeria.sln
├── docs/                   ← ANALISIS_SISTEMA_GALERIA.md, OPCIONES_PROPUESTA_WEB.md
├── datos-origen/           ← los 6 Excel. NUNCA se commitean (datos personales de 262 artistas)
└── src/
    ├── Galeria.Domain/          (vacío)
    ├── Galeria.Application/     (vacío)
    ├── Galeria.Infrastructure/  (vacío)
    ├── Galeria.Web/             (template Blazor con Identity)
    └── Galeria.Tests/           (vacío)
Arquitectura — el orden de referencias es la regla que sostiene todo:

Web  →  Infrastructure  →  Application  →  Domain
                                            ↑
                              Tests  ────────┘
Domain no referencia a nadie, nunca. Si el motor de liquidación necesita EF Core para calcular, algo se hizo mal.

Es un monolito a propósito: un usuario, una red local. Separarlo en servicios sería complejidad sin problema que resolver.

Paquetes:

Infrastructure: EF Core Sqlite 8.0.25, QuestPDF 2024.12.3, MailKit 4.17.0
Web: EF Core Design/Tools/Sqlite 8.0.25, Identity.EFCore 8.0.25
Tests: FluentAssertions 6.12.2
Estado de ejecución:

Connection string: DataSource=Data\app.db;Cache=Shared
Puertos: https://localhost:7094 / http://localhost:5121
No hay carpeta Migrations — la BD no existe. Home/Counter/Weather andan; Login y Register fallan. Es esperado, no es un bug.
4. Hallazgos sobre los datos reales
Verificados leyendo los Excel con openpyxl, no son suposiciones:

Hallazgo	Detalle
El correlativo es POR ARTISTA	Cada artista arranca en 001. El código visible = codArtista(3) + codObra(3)
Ya se desbordó	El artista 332 tiene 32 obras con código > 999 → códigos de 7 dígitos (3321031). El formato de 6 dígitos que toda la documentación asume ya está roto en producción
62 códigos duplicados	La supuesta clave primaria no es única. Ej: 988027 ×2, mismo artista escrito de dos formas
Largos de código	14.661 de 6 dígitos · 32 de 7 · 2 de 5 · 1 de 3
Volumen	14.696 obras · 262 artistas · 26.020 movimientos · 13.840 ventas · 130.352 registros de auditoría
Dólar congelado	301 cotizaciones, última 13/03/2020. Ya no importa: se eliminó la conversión
→ Por eso: ID interno autonumérico como PK real, y el código de 6 dígitos como campo visible aparte.

Reglas de negocio verificadas contra los datos
Sin IVA:  PrecioVenta = Costo × (1 + Utilidad/100)          520 × 1,5 = 780 ✓
Con IVA:  PrecioVenta = Costo × 1,22 × (1 + Utilidad/100)   250 × 1,22 × 1,5 = 457,50 ✓
Al artista se le paga el Costo, no el PrecioVenta. La utilidad es de la galería (casi siempre 50%, pero es campo por obra).

Parámetros del negocio: Redondeo Pesos = 10 · IVA = 22% · Redondeo Dólar = 1.

5. Decisiones cerradas
#	Decisión
1	Correlativo de obra por artista + ID interno autonumérico como PK
2	Código de artista automático
3	Alquiler mensual indefinido hasta que se devuelva la pieza. Se cobra mes completo empezado
4	Devoluciones: línea propia "Devolución" en la liquidación, NO un adelanto disfrazado
5	Serie > 1 fuerza existencia = 1 (serie 10 + existencia 5 no se permite)
6	Obra duplicada = mismo nombre dentro del mismo artista, detectada por autocompletado al tipear
7	Pago contado: se marca al ingresar, aparece en la liquidación al vender, con importe 0
8	Liquidación confirmada no se anula. Vista previa obligatoria antes de confirmar
9	Retiro definitivo queda en el histórico. Nunca se borra nada, se marca
10	WhatsApp manual vía link wa.me. Sin API paga
11	Un solo usuario con todos los permisos. Roles en v2. El login es el candado de la PC
12	Primera versión vacía. Migración de datos solo si el cliente aprueba
13	Pesos y dólares nunca se convierten. Dos carriles paralelos. Totales siempre como par: $ X / U$S Y
14	Imágenes en disco, en Datos/imagenes/, junto al .db
15	Deshacer la última operación: fuera de v1
16	Certificado de autenticidad: datos mínimos, sin costo ni utilidad. A confirmar con el cliente
Fuera de alcance (eliminado del sistema original): Clientes · Movimientos como pantalla · Remitos · Boleta / nº de factura · Cotización del dólar · Sitio público.

Nota: eliminar la cotización resolvió solo el peor hallazgo del análisis (dólar congelado 6 años).

Backup del cliente: todo en una carpeta Datos/ con el .db y las imagenes/ adentro. Para el dueño es "copiá esta carpeta a un pendrive" — el único modelo de backup que va a entender y hacer.

6. Cómo trabajar con este usuario
Español rioplatense, siempre.
Los commits los hace él. Claude sugiere cuándo y el mensaje; no ejecuta git commit.
Terminal: PowerShell. Nada de rm -rf, mv ni rutas /c/Users/.... Usar Remove-Item, Move-Item, Set-Location. Los comandos de git son iguales en las tres terminales.
Prefiere crear los archivos él mismo y que Claude guíe con instrucciones. Confirmar antes de crear archivos por él.
Es su primer proyecto con un cliente real. Quiere que muestre backend y arquitectura en su portfolio.
UI: blanco y negro estricto, escala de grises. Los estados se distinguen por ícono, peso tipográfico y relleno del badge — nunca por color. Tipografía Inter, densidad de ERP, estilo Linear/Notion. Hay un prompt de Figma con el detalle completo.
7. Próximos pasos
Entidades del dominio en Galeria.Domain: Artista, Obra, Serie, Venta, Retiro, Alquiler, Devolucion, Adelanto, Liquidacion + detalle, Auditoria, Rubro, Tecnica, Parametro
GaleriaDbContext en Galeria.Infrastructure (separado del ApplicationDbContext de Identity, que vive en Web — la autenticación no es parte del dominio de la galería)
Primera migración → esto hace que el login empiece a funcionar
Sembrar el usuario admin y desactivar el registro público (el template lo deja abierto: hoy cualquiera se puede crear una cuenta)
Renombrar Data/ → Datos/ y agregar Datos/imagenes/
CRUD de Obras y Artistas
Motor de liquidación con tests — el corazón del sistema, y lo que más rinde para el portfolio