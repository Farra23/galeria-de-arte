REQUERIMIENTOS — ERP Galería ACATRAS
v2 — pulido, sin quitar nada
Convenciones [INSISTIÓ] = pedido repetido por el cliente en más de una pantalla · [+] = agregado mío, a validar · [] = ambigüedad a definir

0. REQUISITOS TRANSVERSALES
Aparecieron repetidos en 4+ pantallas. Se definen una vez y valen para todas.

0.1 Filtros en todas las listas [INSISTIÓ]
Lo pediste en Obras, Ventas (OBVIO), Alquileres (todo filtrable), Adelantos, Agenda y Auditoría (FILTRABLE en mayúsculas). Regla general toda tabla tiene buscador de texto libre + filtros por sus campos relevantes + orden por cualquier columna (clic en el encabezado) + chips de filtros aplicados con X para quitarlos. [+] Que los filtros queden en la URL, así podés guardar o compartir una vista filtrada.

0.2 Imprimir y exportar a PDF [INSISTIÓ]
Pedido en Obras, Certificado, Retiros y Liquidaciones. Regla toda lista filtrada se exporta a PDF respetando el filtro activo, y todo comprobante (liquidación, retiro, certificado) tiene versión PDF con diseño de documento formal. [+] Sumar exportar a ExcelCSV en las listas — para el dueño que viene de Excel, es la red de seguridad que lo hace confiar en el sistema nuevo.

0.3 Envío por mail y por teléfono [INSISTIÓ]
Pedido en Certificado, Retiros, Liquidaciones y Agenda. [] Atención acá mandar por mail es directo. Mandar a un número de teléfono significa WhatsApp, y eso tiene tres caminos con costos muy distintos (a) botón que abre WhatsApp Web con el mensaje ya escrito y vos apretás enviar — gratis e inmediato; (b) WhatsApp Business API — automático pero se paga por mensaje y requiere aprobación de Meta; (c) SMS — barato pero no manda PDF. Recomiendo (a) para la primera versión. Hay que preguntarle al cliente si le sirve apretar un botón más.

0.4 Reversibilidad de operaciones [INSISTIÓ]
Lo dijiste en Retiros (devueltos de manera simple), Alquileres (lo mismo que con los retiros temporales) y Devoluciones. Regla toda operación que saca una pieza del stock de forma no definitiva se revierte con un botón desde la fila de la lista, sin formularios. La pieza vuelve al stock y a estado disponible.

0.5 Estado único de la obra [+]
Esto no lo pediste explícito pero se desprende de todo lo demás Retiros temporales, Alquileres y Devoluciones necesitan bloquear la venta de una pieza. Propongo un campo Estado en cada obra con estos valores Disponible · Retirada temporal · Alquilada · Retirada definitiva · Sin stock. En la lista de Obras se ve como badge, y las no disponibles aparecen atenuadas (vos dijiste inhabilitada o con otro color; como la paleta es blanco y negro, sería gris claro + badge). Registrar venta bloquea toda obra que no esté Disponible.

0.6 Confirmación antes de lo irreversible [+]
Pediste revisión previa explícita en Liquidaciones. Lo mismo debería aplicar a retiro definitivo, borrado de cualquier registro y cambio global de precios. Modal de confirmación que muestre exactamente qué va a pasar.

0.7 Idioma y formato
Todo en español rioplatense. Fechas ddmmaaaa. Miles con punto, decimales con coma. Moneda siempre con símbolo explícito ($  U$S) — nunca un número suelto sin moneda.

0.8 Nunca se convierte entre monedas [+ derivado de tu decisión]
Al eliminar la cotización, Pesos y Dólares son dos contabilidades paralelas que nunca se mezclan. Un artista con obras en ambas monedas tiene dos liquidaciones. Los totales siempre se muestran como par $ 10.100  U$S 0.

1. LOGIN
Usuario y contraseña. Usuario admin precargado con su contraseña.
[+] Recordarme y cierre de sesión por inactividad (que no quede abierto en la PC del mostrador).
[] ¿Cuántos usuarios reales van a existir Definiste ABM de usuarios en Configuración, así que asumo que el dueño da de alta a sus empleados. ¿Hay diferencia de permisos entre ellos, o todos ven todo
2. RESUMEN (dashboard)
Pantalla de entrada, fuera del menú lateral.

Ventas del mes + gráficos + últimas ventas + datos relevantes.
Total de plata que se debe a artistas (en pesos y en dólares, separados).
Lista de artistas a los que se les debe y no vinieron a cobrar. Al hacer clic, se va a liquidar. Cuando se confirma la liquidación, ese artista desaparece de la lista y baja el total adeudado.
[+] Que esa lista muestre hace cuánto que ese artista no cobra — ordenada por antigüedad, los más viejos arriba. Es la señal de a este llamalo ya.
[+] Avisos operativos obras sin foto, obras sin técnica cargada (hoy son 3.303 en el Excel), retiros temporales vencidos hace mucho, alquileres sin fecha de recupero.
[+] Que cada KPI sea clickeable y lleve a la lista filtrada correspondiente.
3. OBRAS
3.1 Lista
Columnas [+] miniatura · código · nombre · artista · rubro · técnica · moneda · costo · precio venta · stock · [+] estado · fecha ingreso.

Filtros búsqueda por código de obra, nombre de obra, nombre de artista o código de artista · rubro · técnica · rango de precio de venta · IVA síno · existencia · moneda. [+] Estado y rango de fecha de ingreso.

Acciones imprimir  exportar a PDF la lista filtrada. [+] Exportar a Excel.

Al hacer clic en una obra → ficha editable, con imagen y cambio de precio. [+] Que la ficha tenga pestañas Datos · Historial de precios · Historial de movimientos (ventas, retiros, alquileres, devoluciones de esa pieza).

3.2 Agregar obra
Código automático el código de obra es 1 + el último. [] Importante aclarar esto en el Excel el código es de 6 dígitos = 3 del artista + 3 de la obra, y el correlativo es por artista (cada artista tiene su propio contador Próximo Código). Asumo que seguimos igual la obra nueva de Lozoya toma el siguiente número de Lozoya, no el siguiente del sistema entero. Confirmalo, porque cambia todo el modelo de datos. También ¿qué pasa cuando un artista llega a 999 obras (hoy ninguno está cerca, pero conviene decidirlo).

Artista desplegable de artistas ya cargados. Si el artista es nuevo y no tiene ninguna obra, un link que lleve a Nuevo artista. El código del artista se muestra al lado del artista seleccionado.

Nombre de la obra campo de texto con autocompletado inteligente sobre las obras de ese mismo artista. A medida que escribís carav… sugiere Caravanas azules · 106 con un botón Agregar existencia al lado. Esto evita duplicados.

Resto del formulario desplegable de rubro · desplegable de técnica · medidas (largo, alto, ancho) · serie (cantidad de piezas) · existencia · observaciones · moneda (Pesos  USD) · costo · utilidad en % · IVA · precio de venta calculado automáticamente.

Checkbox Pago contado  A liquidar

A liquidar (default) flujo normal, se le paga al artista cuando se vende.
Pago contado la galería le pagó al artista al ingresar la pieza. Esa obra aparece en la liquidación con importe 0 y el detalle Pago contado, y se suma a la pestaña Piezas pagas de la ficha del artista.
[] ¿En qué momento aparece en la liquidación cuando ingresa la pieza, o cuando efectivamente se vende Lo lógico es cuando se vende (el artista ve esto se vendió, ya te lo pagué), pero confirmalo.
Imagen se puede subir al cargar la obra o más tarde. [+] Varias fotos por obra, con una principal.

3.3 Serie — regla de negocio
Una serie es un conjunto de piezas similares que el artista no quiere detallar una por una.

Caso	Serie	Existencia	Resultado
10 caravanas iguales	1	10	1 obra, 1 código, stock 10
10 caravanas distintas	10	1	10 obras, 10 códigos consecutivos, stock 1 cu
En el segundo caso se llena un solo formulario y el sistema crea las 10 obras. Las piezas quedan marcadas como pertenecientes a la misma serie, sin modificar el código (un campo aparte, no un sufijo). [+] Que en la lista de Obras se puedan agruparfiltrar por serie, y que la ficha muestre Pieza 3 de 10 · Serie Caravanas mar 2026.

[] ¿Qué pasa con serie 10 + existencia 5 ¿Se permite (10 obras de 5 unidades cada una) o se bloquea

3.4 Obra que ya existe
Si vas a agregar una obra que ya existe, el sistema muestra el formulario completo con todo igual y solo aumenta la existencia.
[] ¿Cómo decide que ya existe Propongo mismo artista + nombre idéntico. Si es parecido pero no idéntico, lo ofrece el autocompletado del punto 3.2 y vos decidís. [+] Que si el costo del formulario nuevo difiere del de la obra existente, avise antes de sumar existencia (podría ser un cambio de precio encubierto).

4. ARTISTAS
4.1 Lista
Código · nombre · taller · celular · correo · cantidad de obras · obras en stock · saldo a pagar (en pesos y dólares por separado). Clic → ficha.

4.2 Ficha del artista
Encabezado con los datos, botones Editar y Liquidar. Pestañas

Pestaña	Contenido
Datos	Ficha completa del artista
Obras	Todas las obras que tuvo (históricas, no solo en stock)
Ventas	Todas sus ventas
Liquidaciones	Histórico de liquidaciones, con acceso al PDF de cada una
Adelantos	Fecha y monto
Piezas pagas	Las marcadas como pago contado al ingresar
Retiros	Temporales y definitivos
[+] Alquileres	Faltaba en tu lista, pero si el artista cobra por alquiler tiene que poder verlo
[+] Que el encabezado muestre siempre el saldo actual a pagar, en las dos monedas.

4.3 Nuevo artista
Crea la ficha con los datos de arriba. [] ¿El código de artista es automático (siguiente libre) o lo elige el usuario En el Excel hay huecos (van del 102 al 910), así que puede que lo elijan a mano.

5. VENTAS
5.1 Lista
Fecha · código de obra · nombre · artista · cantidad vendida · moneda · precio final de venta. Filtrable [INSISTIÓ].
Se elimina boleta, cliente, cotización.

5.2 Registrar venta
Buscador por código que al encontrarla muestra nombre de la obra, foto si tiene, nombre del artista, y cuántas quedan en stock.
Si es la última pieza → aviso destacado ¡ÚLTIMA PIEZA! (en negritaalto contraste, dado que la paleta es blanco y negro).
Cantidad · moneda (USDPesos).
Costo de ingreso (lo que puso el artista) visible con un ojito, oculto por defecto.
Precio de venta en grande, precargado con el calculado y editable — se negocia con el cliente, no puede ser fijo. Viene redondeado.
Exento de IVA (por defecto) o IVA — precargado desde la ficha de la obra.
Fecha con calendario desplegable — se registran ventas tardías.
Observaciones.
Al confirmar la obra pasa a vendida, existencia −1, y la pieza se agrega a Ventas.
[+] Si la venta se hace a un precio distinto del calculado, que quede registrado el desvío (sirve para la auditoría y para entender cuánto se negocia).

5.3 Certificado de autenticidad
Después de registrar la venta, poder emitir el certificado de la pieza vendida

PDF con la ficha de la pieza, solo los datos relevantes (sin costo del artista ni utilidad).
Enviable por mail o a un número de teléfono, e imprimible [INSISTIÓ].
[] ¿Qué datos exactamente Propongo código, nombre de la obra, artista, técnica, rubro, medidas, año, foto y una leyenda de autenticidad firmada por la galería. Nunca precio ni costo.
[+] Numeración correlativa del certificado y registro de a quiéncuándo se emitió.
6. RETIROS
6.1 Lista
Fecha · artista · código · obra · temporaldefinitivo · motivo. Filtrable.

6.2 Nuevo retiro
Código con buscador que muestra la obra · cantidad a retirar · temporaldefinitivo · motivo.
Envío por mail o teléfono de un PDF con los datos del retiro, e imprimible [INSISTIÓ].

6.3 Reglas
Un retiro temporal debe poder devolverse al stock con un botón, desde la lista filtrada. La pieza vuelve a estar en venta.
Una pieza en retiro temporal NO se puede vender. Aparece inhabilitadaatenuada en la lista de Obras.
[] El retiro definitivo, ¿deja la obra en el histórico con existencia 0 y estado Retirada definitiva, o desaparece de la lista Propongo lo primero nunca borrar, siempre marcar.
[+] Fecha estimada de devolución en el retiro temporal, y aviso en el Resumen cuando se pasa.
7. ALQUILERES
7.1 Lista
Fecha · código · obra · artista · cliente · monto mensual · monto del artista. Todo filtrable [INSISTIÓ].

7.2 Nuevo alquiler
Código · moneda · importe de alquiler calculado como un % variable y editable sobre el precio de venta de la obra.

Ejemplo obra de 1.000, alquiler al 10% → 100.
Sobre esos 100 se aplica un segundo porcentaje que reparte entre galería y artista, elegible, y el sistema calcula automáticamente cuánto va a cada parte.

[] Dos cosas a aclarar acá

Escribiste qué porcentaje le toca a la galería y qué porcentaje le toca al cliente — asumo que quisiste decir al artista (el cliente es quien paga el alquiler, no quien cobra). Confirmalo.
Decís monto mensual en la lista pero importe alquiler en el formulario. ¿El alquiler es recurrente mes a mes Si lo es, el sistema tiene que generar automáticamente el cargo cada mes mientras la pieza esté alquilada, y eso cambia bastante la Agenda de pagos. Si es un importe único por período, es mucho más simple. Esta respuesta tiene peso real sobre el tamaño del proyecto.
7.3 Reglas
En Liquidación y en Agenda de pagos aparece lo que le toca al artista, con detalle Alquiler, nombre de obra, código, etc.
Una pieza alquilada no se puede vender y se devuelve al stock con un botón, igual que el retiro temporal [INSISTIÓ].
[+] Registrar fecha de inicio y fecha de recupero prevista, con aviso en el Resumen.
8. DEVOLUCIONES
Cuando un cliente devuelve algo que había sido vendido.

Buscar por código, nombre de obra o artista.
La pieza vuelve al stock original y deja de figurar como vendida.
Interacción con la liquidación — el punto delicado
Si el artista ya cobró esa pieza → el importe queda como adelanto a descontar de la próxima liquidación.
Si todavía no se liquidó → en Agenda de pagos y en Liquidación se marca como Devolución y no suma plata.
[] Signo de los adelantos dijiste que un adelanto puede ser negativo. Necesito que definamos la convención de una vez, porque si queda ambigua se van a cometer errores de plata

¿Un adelanto positivo es plata que le di al artista (y por lo tanto resta en la liquidación)
¿Y un adelanto negativo es una corrección que suma
[+] Sugerencia en vez de números con signo, usar dos tipos explícitos — Adelanto (resta) y Ajuste a favor (suma). Se ve claro en el PDF que recibe el artista y elimina la posibilidad de equivocar el signo.

[+] Motivo de la devolución y fecha, para poder analizarlo después.

9. ADELANTOS
9.1 Lista
Fecha · artista · moneda · monto (puede ser negativo). Filtrable por artista y por fecha [INSISTIÓ].
[+] Columna que muestre si ya fue descontado y en qué liquidación (en el Excel existe Fecha Descontado — es información valiosa que no conviene perder).

9.2 Nuevo adelanto
Artista · moneda · monto · fecha · [+] motivoobservaciones.

9.3 Regla
Los adelantos aparecen en la liquidación con fecha y motivo Adelanto.

10. LIQUIDACIONES
10.1 Generar
Desplegable de artistas + buscador por código.
Filtro por fecha, marcando cuál fue la última fecha que se liquidó a ese artista.
Selector de moneda.
10.2 Contenido de cada liquidación
Cada línea lleva fecha, código de obra, nombre de obra, cantidad y los datos propios de su tipo

Tipo	Qué muestra
Venta de pieza	El costo (el precio que eligió el artista al entregar la obra). Si la obra se ingresó con una observación, esa observación aparece acá
Alquiler	El porcentaje que le toca al artista
Pago contado	Importe 0 y detalle Pago contado
Adelanto	Con su fecha, detalle Adelanto
[+] Devolución	Marcada como tal, sin sumar plata (viene del punto 8)
Cierra con el total a pagar, ya neteado de adelantos y devoluciones.

10.3 Requisitos del documento [INSISTIÓ]
Tiene que verse profesional. Es el comprobante que se lleva el artista.
PDF descargable e imprimible.
Fecha de emisión = fecha de hoy, bien visible (es de suma importancia).
Deja constancia de qué piezas se le pagaron y cuándo fueron vendidas  alquiladas  adelantadas.
[+] Número correlativo de liquidación, para que artista y galería puedan referirse a la liquidación 0147.
[+] Datos de la galería en el encabezado (nombre, dirección, contacto).
10.4 Flujo de confirmación [INSISTIÓ]
Vista previa — muestra cómo va a quedar antes de confirmar.
Botón Confirmar.
Mensaje Liquidación confirmada y registrada.
Envío por correo al artista (usa el mail de su ficha).
Queda en la pestaña Liquidaciones de la ficha del artista.
[] Una vez confirmada, ¿se puede anular En el Excel era irreversible. Pero vos pediste cancelar la última operación en Auditoría (punto 13), lo cual entraría en conflicto. Hay que decidir irreversible con vista previa obligatoria (más seguro), o anulable por admin dejando rastro (más flexible). Yo recomiendo la primera para la liquidación específicamente, y la segunda para el resto de las operaciones.

11. AGENDA DE PAGOS
Es la liquidación en versión mensual y anticipada sirve para avisarle a los artistas que vendieron piezas y que pueden pasar a cobrar. Cuando efectivamente vienen, ahí se hace la liquidación.

Se genera automáticamente. Muestra el mes actual en un lugar visible.
Columnas artista · cantidad de piezas vendidas en el mes (o alquiladas, o plata adelantada) · total pesos · total dólares · plata de meses anteriores · plata total · comentarios · fecha pactada · fecha confirmada.
Clic en la fila → desglose de las obras fecha de venta, nombre, código, valor, costo del artista, etc.
11.1 Regla del arrastre
Solo si no se liquidó, el saldo se suma a plata meses anteriores. Porque no todos vienen a cobrar todos los meses.

Ejemplo en mayo vendió 1 pieza de $100 y no vino a cobrar. En junio vende 1 de $10.000.
→ Plata meses anteriores 100 · Pesos del mes 10.000 · Total $ 10.100  U$S 0

11.2 Fechas
Al hacer clic en los campos de fecha de la fila se abre un calendario desplegable para marcarlas ahí mismo.
La lógica le decís a un artista que venga el jueves, te dice que no puede y confirma el lunes → vos filtrás por calendario y días antes vas a retirar la plata, priorizando a los que vienen primero.

11.3 Filtros y orden [INSISTIÓ]
Por nombre ascendente, por fecha confirmada ascendente, y por el resto de los campos.
[+] Vista próximos 7 días — que es literalmente el caso de uso que describiste.

11.4 Aviso por mail
Botón a la derecha de cada fila, con mensaje predeterminado

Estimado [nombre del artista], usted tiene [$ X en pesos y U$S X] para retirar. La fecha pactada sería el [fecha que selecciona el administrador]. Recuerde confirmar al [número de teléfono].

[+] Que el texto de la plantilla sea editable desde Configuración, y que se pueda seleccionar varias filas y mandar todos los avisos juntos.
[+] Botón Liquidar directo en la fila — es el paso natural cuando el artista aparece.

12. CAMBIO DE PRECIOS
Unitario se pone el código → aparece la ficha de la obra, igual que si entraras desde Obras y apretaras Editar.
Global se elige un artista de un desplegable o ingresando su código → se seleccionan varias piezas cuyo precio va a cambiar.
[+] En el global, poder aplicar el cambio como porcentaje (ej. +15% a todo) además de precio a precio — en el Excel el 66% de los cambios eran globales, así que es el caso frecuente.
[+] Vista previa de precio actual → precio nuevo antes de aplicar, y fecha de vigencia.
[+] Todo cambio queda en el historial de precios de la obra y en Auditoría.
13. AUDITORÍA
Automática y de TODO.
Filtrable [INSISTIÓ] por fecha, usuario, pantalla, tipo de operación.
Registra fecha, hora, usuario, pantalla, campo, valor anterior, valor nuevo, código de artista, código de obra.
Permite cancelar la última operación.
[] Sobre el cancelar la última operación — es el requerimiento con más filo de toda la lista. Hay que acotarlo, porque deshacer cualquier cosa es muy distinto de deshacer lo último

¿Es deshacer la última operación del usuario actual, o la última del sistema
¿Vale para cualquier cosa, incluida una liquidación confirmada
¿Hay ventana de tiempo (ej. solo dentro de los 10 minutos)
[+] Mi recomendación deshacer solo la última operación del usuario logueado, dentro de la sesión, y excluyendo las liquidaciones confirmadas (que ya tienen su propia vista previa obligatoria). El deshacer queda registrado como una operación más en la auditoría — nunca se borra el rastro.

[+] Como Movimientos deja de ser pantalla, la Auditoría pasa a ser la única fuente para reconstruir cómo se llegó al stock actual. Internamente conviene seguir guardando el registro de movimientos aunque no tenga menú propio es lo que da de comer a esta pantalla y lo que te salva si un stock queda descuadrado.

14. CONFIGURACIÓN
ABM de usuarios (alta, baja, modificación).
ABM de rubros.
ABM de técnicas.
[+] Parámetros del negocio IVA % (hoy 22), redondeo en pesos (10), redondeo en dólares (1), utilidad por defecto (50).
[+] Datos de la galería (nombre, dirección, teléfono, logo) para que aparezcan en los PDF.
[+] Plantillas de los mails (aviso de agenda, envío de liquidación, certificado).
15. NAVEGACIÓN
Menú lateral desplegablecolapsable con botón de tres rayas. Login y Resumen quedan fuera del menú.

⚠️ Esto contradice el prompt de Figma, que definía una sidebar fija de 240px siempre visible. Hay que actualizarlo.

Agrupación propuesta

  Resumen
  CATÁLOGO      Obras · Artistas
  OPERACIÓN     Ventas · Retiros · Alquileres · Devoluciones
  PAGOS         Adelantos · Liquidaciones · Agenda de pagos
  CONTROL       Cambio de precios · Auditoría
  SISTEMA       Configuración
13 pantallas + login. El prompt de Figma tiene 5 que ya no van (Clientes, Movimientos, Remitos, Cotización del dólar, y Historial de liquidaciones como pantalla suelta) y le faltan 3 nuevas (Alquileres con reparto, Devoluciones, Certificados).

16. FUERA DE ALCANCE — se elimina
Se elimina	Consecuencia
Clientes	No hay CRM ni trazabilidad de a quién se vendió
Movimientos (pantalla)	La info vive en Auditoría [+] pero el registro interno se conserva
Remitos de ingreso	Coherente en el Excel solo lo usaban 3 artistas de 179
Boleta  nº de factura	El ERP no es el sistema fiscal. [] ¿Facturan por otro lado (DGI, factura electrónica)
Cotización del dólar	Resuelve solo el peor hallazgo del análisis. Pesos y dólares nunca se mezclan