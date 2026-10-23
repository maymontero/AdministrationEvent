# Momentos — invitaciones configurables

Aplicación ASP.NET Core MVC con Entity Framework Core y MySQL. Panel en `/admin`, invitaciones públicas en `/invitacion/{slug}`. No hay contraseñas predefinidas.

## MySQL local en macOS

Instalar **MySQL Community Server** desde https://dev.mysql.com/downloads/mysql/ (seleccionar macOS y la arquitectura de tu equipo). MySQL Workbench es opcional; no reemplaza al servidor. Iniciar el servidor y conservar la contraseña del administrador. La línea de comandos del instalador suele estar en `/usr/local/mysql/bin/mysql`.

El proyecto usa **.NET 10**, Entity Framework Core 10.0.9 y el proveedor oficial `MySql.EntityFrameworkCore` 10.0.9. Instalar el SDK .NET 10; `global.json` permite usar versiones estables de la línea 10. En este equipo se verificó con SDK 10.0.401 y MySQL 8.0.26. No se necesita .NET Core 3.1 para compilar ni ejecutar la aplicación.

Si VS Code no detecta el SDK, verificar `dotnet --list-sdks` y ejecutar «Developer: Reload Window». La migración `Net10ProviderAlignment` actualiza los metadatos del cambio de proveedor sin modificar tablas ni datos. Se conserva el identificador de la migración inicial y la columna de contenido `longtext`, por compatibilidad con las bases existentes.

1. Abrir `Data/setup-local.sql`, reemplazar la contraseña de ejemplo y ejecutar su contenido como administrador de MySQL. Crea la base `invitations` y el usuario local `invitations_app` con permisos de desarrollo. Si ese usuario ya existe, el script conserva su contraseña.
2. En la terminal de VS Code, desde la carpeta que contiene `AdministrationEvent.csproj`, configurar secretos locales:

```bash
dotnet user-secrets set "ConnectionStrings:Invitations" "Server=localhost;Port=3306;Database=invitations;User=invitations_app;Password=TU_CONTRASENA"
dotnet user-secrets set "Admin:Password" "TU_CONTRASENA_ADMINISTRATIVA"
```

No guardar contraseñas reales en SQL versionado o `appsettings.json`. También se pueden configurar las variables de entorno `ConnectionStrings__Invitations` y `Admin__Password`. Si la contraseña contiene caracteres especiales de conexión, debe escaparse según el formato del conector.

3. Restaurar dependencias, herramientas y crear las tablas con la migración incluida:

```bash
dotnet restore
dotnet tool restore
dotnet ef database update
dotnet run
```

Usar la dirección que informa el servidor y acceder a `/admin`. Una base nueva empieza sin invitaciones; crear una desde el panel. La aplicación no aplica migraciones automáticamente al iniciar.

## Importar las invitaciones anteriores

Después de aplicar la migración, si hay archivos en `App_Data/Invitations`:

```bash
dotnet run -- --import-invitations
```

La importación conserva IDs, enlaces, diseños y secciones; se ejecuta en una transacción y deja los archivos intactos. Las invitaciones cuyo ID ya existe se omiten para conservar los cambios realizados en la base. Si otro ID utiliza el mismo enlace o hay datos inválidos, se revierte toda la importación. El panel usa MySQL y ya no escribe archivos JSON.

## Configuración de invitaciones

El editor JSON permite cambiar contenido, tipo de evento, fecha local, slug, estado de publicación, portada y tema. `Theme` contiene colores HEX, familias tipográficas, URLs opcionales de fuentes WOFF/WOFF2, tamaño de letra, ancho y radio de bordes. Los recursos remotos deben estar disponibles por HTTPS; las fuentes necesitan permitir CORS. `Sections` es una lista ordenada, sin un número fijo de secciones; permite título, texto, imagen, enlace y visibilidad. `Theme.Layout` permite classic, vineyard o relaxed; `Secondary` y `Highlight` amplían la paleta. Imágenes y fuentes también aceptan rutas locales seguras dentro de `/images/` y `/fonts/`. `ShowCountdown` activa la cuenta regresiva; `HasConfirmedTime: false` muestra solo la fecha y los días restantes, calculados en `TimeZone` (por ejemplo `America/Montevideo`).

`RsvpEnabled` activa el formulario interno. Las respuestas se guardan en `GuestResponses` y se consultan en `/admin/responses/{id}`, con acceso administrativo. Solo las invitaciones publicadas admiten respuestas públicas. La vista previa permite probar el formulario sin guardar respuestas. El formulario recoge nombre, asistencia, cantidad de personas (1–10), contacto y mensaje opcionales; las ausencias cuentan como cero personas. También se pueden seguir usando enlaces externos por sección.

Los modelos de dominio están en `Models/Domain`, con namespace `Application`. `Data/InvitationDbContext.cs` define relaciones, índices únicos y borrado en cascada. La configuración del editor se convierte a entidades: invitación, tema, recursos de colores y fuentes y secciones ordenadas. Las tablas de eventos, lugares, imágenes, audios y configuración RSVP quedan disponibles para funcionalidades futuras. El GUID público de cada invitación se conserva separado del ID numérico interno. Las ediciones concurrentes conservan el último guardado.

## Migraciones y verificaciones

```bash
dotnet ef migrations add NombreDelCambio --output-dir Data/Migrations
dotnet ef migrations script --idempotent --output Data/migrations.sql
dotnet run --project Tests/DatabaseChecks/DatabaseChecks.csproj
# Contra el MySQL configurado, en una transacción que se revierte:
dotnet run --project Tests/DatabaseChecks/DatabaseChecks.csproj -- --mysql
```

Las verificaciones predeterminadas usan SQLite temporal. Con `--mysql` prueban el guardado, edición y publicación contra el servidor configurado, dentro de una transacción que se revierte. También se verificaron las rutas públicas, autenticación administrativa y vista previa en .NET 10.

Antes de desplegar: usar HTTPS, respaldar la base, limitar intentos de acceso y separar los permisos de migraciones del usuario que ejecuta la web. Esta versión tiene una cuenta administrativa y editor JSON; no incluye cuentas de usuarios ni un editor visual. Los formularios usan protección antifalsificación. Los borradores solo son visibles mediante la vista previa autenticada.

## Compilar y depurar en VS Code

Abrir la carpeta que contiene `AdministrationEvent.csproj`, no solo un archivo `.cs`. Usar **Cmd + Shift + B** para compilar la web, **F5** con «Depurar web (.NET 10)» para iniciar el depurador, o la tarea «Ejecutar web». Las tareas de `.vscode` apuntan al proyecto completo. El comando «Run C# File» intenta ejecutar el archivo activo como programa independiente y no corresponde a controladores MVC. Desde la terminal también se puede usar `dotnet build AdministrationEvent.csproj` y `dotnet run --project AdministrationEvent.csproj`.

La web local usa `http://localhost:5088` (panel: `/admin`). Se evita el puerto 5000 porque AirPlay de macOS puede ocuparlo y devolver un error 403. Tanto F5 como `dotnet run --project AdministrationEvent.csproj` usan el puerto 5088. Si quedó abierta una pestaña antigua con `localhost:5000`, abrir la dirección nueva después de reiniciar la aplicación.

## Invitados y confirmaciones

Desde `/admin`, abrir **Invitados y asistencia** en la invitación elegida. Cada invitación tiene su propia lista: las propuestas de diseño mantienen listas independientes hasta elegir cuál se enviará.

- Cargar una persona, pareja o familia, su contacto opcional, cantidad de personas invitadas (1–10) y notas internas.
- Separar lista fija y lista de espera, con filtros y botones para mover grupos entre ambas. La espera no suma en las personas confirmadas.
- Editar, buscar y filtrar pendientes, confirmaciones, ausencias y archivados. Quitar archiva y permite recuperar, conservando las respuestas.
- Al confirmar, elegir explícitamente el total de asistentes, incluyéndose y contando acompañantes sin nombre. «Pepita + uno» y «Pepita y Pepito» son 2 si seleccionan 2; no se intenta adivinar la cantidad por el texto.
- Todos usan el mismo enlace público. La lista y los contactos no se muestran públicamente. Las notas internas tampoco.
- El formulario guarda cada envío en `GuestResponses`. Si el nombre normalizado (sin diferencias de mayúsculas, tildes ni espacios repetidos) coincide con un único invitado activo de la lista fija, vincula su `GuestId`. No es una verificación de identidad. Homónimos y nombres diferentes quedan sin asociar y se revisan manualmente.
- En **Respuestas recibidas**, asociar/desvincular cada envío. La última respuesta asociada por fecha UTC e ID define el estado actual. Las anteriores quedan como historial y no se suman dos veces. Las respuestas sin asociar o de invitados archivados o en espera no suman en los totales.
- Se puede registrar una respuesta desde administración y revisar las cantidades que superen las invitadas. Los mensajes y la fecha de recepción se guardan en MySQL; las fechas se presentan en la zona horaria de la invitación.
- Las pruebas del formulario desde la vista previa no se guardan. Los envíos de la invitación publicada sí.

Aplicar las migraciones `GuestAdministration` y `GuestWaitingList` con `dotnet ef database update` antes de abrir el panel. Pruebas: `dotnet run --project Tests/DatabaseChecks/DatabaseChecks.csproj`; contra MySQL, agregar `-- --mysql` (usa una transacción revertida).

## Etiquetas y mesas

Desde **Invitados y asistencia → Etiquetas y mesas**, crear etiquetas con nombre y color y mesas con nombre y capacidad. Cada invitación mantiene su propia organización. En el editor de invitados se pueden seleccionar varias etiquetas y una mesa por grupo; la lista permite filtrar por etiqueta, mesa o sin mesa.

El listado de mesas incluye los grupos asignados y permite moverlos o dejarlos sin mesa. Los lugares previstos usan la cantidad invitada para pendientes y la cantidad de la última respuesta para confirmados; las ausencias, los archivados y la lista de espera no ocupan lugares. Los acompañantes están incluidos en la cantidad del grupo y se sientan juntos. Se muestra un aviso al superar la capacidad, sin impedir reorganizar. Eliminar una mesa conserva sus invitados sin asignación; eliminar una etiqueta solo quita sus asociaciones. Aplicar la migración `GuestTagsAndTables` antes de usar estas opciones. No incluye plano visual por ahora.

La música de la invitación se configura con `Theme.SpotifyTrackUrl` (por ejemplo `https://open.spotify.com/track/2TD8Bhc5hFPKh52lFJ2Ec5`). Acepta enlaces de canción o embed de Spotify, se guarda en AudioResources y aparece como control flotante en los tres diseños. Vacío oculta el reproductor. El invitado pulsa Play; Spotify determina si reproduce la canción completa o una muestra. No requiere una migración adicional.



El botón flotante `♫ Reproducir` controla Spotify con su iFrame API y cambia a `♫ Pausar` según los eventos reales de reproducción, también cuando se usa Play en el panel. Si la API no responde o el navegador bloquea el inicio, el botón mantiene la posición de la página; el reproductor oficial queda al final, después del formulario de confirmación. `?spotifyTest=true` en la vista previa prueba Brindo sin guardar cambios.

## Cuentas individuales y administración

Aplicar la migración `AdminAccounts` con `dotnet ef database update` antes de desplegar esta versión. `Data/admin-accounts.sql` contiene solo la migración de cuentas, para una base que ya esté actualizada hasta `SectionTitleFontSize`.

La primera entrada crea la cuenta principal `admin` usando la contraseña ya configurada en `Admin__Password`. Luego se guarda únicamente su hash; cambiar esa variable no cambia una contraseña existente. No hay cuentas ni contraseñas predefinidas para terceros. Las cookies del acceso compartido anterior se invalidan: ingresar nuevamente con usuario `admin` y la contraseña existente.

La administradora puede ir a `/admin/accounts`, crear `chea` con nombre Chea y una contraseña de 12 a 128 caracteres, restablecer contraseñas y activar/desactivar cuentas. Las cuentas nuevas solo pueden leer las invitaciones que la administradora les habilite; no pueden administrar invitaciones, invitados ni respuestas. Cada persona puede cambiar su contraseña en `/admin/accounts/password`. Cambiar o restablecer contraseña o desactivar una cuenta revoca sus sesiones. Cinco intentos incorrectos bloquean temporalmente esa cuenta por 15 minutos.

El panel y el ingreso usan la paleta de la tarjeta formal: crema `#f8f0dc`, terracota `#95513b`, oliva `#737047`, dorado `#f5df89` y texto `#463c30`, con Cormorant Garamond y Jost y navegación adaptada a celular.

## Guardá esta fecha

En el panel, `Crear un Guardá esta fecha` duplica una invitación como borrador independiente, conserva fecha, portada, colores, fuentes y Spotify, oculta las secciones y desactiva RSVP y horario. Usa `Theme.Layout: "save-date"` y muestra el reproductor al final y un control de Play con notas musicales encima de Guardá esta fecha, sin botón flotante. La invitación completa se conserva sin modificaciones. No requiere migraciones nuevas. También puede crearse localmente con `dotnet run -- --create-save-date ID_ORIGINAL`.

## Despedida · edición playa

El panel incluye **Despedida · edición playa**, que abre una invitación nueva con el diseño `beach`, dibujos simples independientes y secciones editables. Empieza como borrador, sin RSVP y sin destino. Las fechas iniciales son del 24 de febrero al 3 de marzo de 2027; los horarios quedan por confirmar en el texto de la primera sección. Cambiar Hosts para personalizar el título.

Para guardar el borrador inicial desde la terminal: `dotnet run -- --create-beach`. Si ya existe el slug `despedida-sorpresa`, conserva su contenido y devuelve su ID. La vista previa usa `/admin/preview/ID`. No requiere migraciones.

La despedida muestra un control musical al inicio y una sección final con los tragos dibujados. Configurar `Theme.SpotifyTrackUrl` con el enlace HTTPS de una canción o playlist de Spotify (también acepta enlaces embed). El nombre del campo se conserva por compatibilidad. Sin enlace, Play lleva a la sección y muestra “La playlist está en camino.”; al configurarlo, el control alterna reproducir/pausar según los eventos de Spotify y queda disponible el reproductor oficial al final.

### Permisos por invitación

Aplicar `dotnet ef database update` (migración `InvitationPermissions`). Las invitaciones publicadas son públicas y se abren sin cuenta. Los permisos por persona se aplican exclusivamente a las vistas previas, incluidos los borradores; no se conceden automáticamente a cuentas existentes. La administradora principal conserva todos los accesos y es la única que puede editar, gestionar invitados y cuentas. Puede compartir borradores para lectura mediante permisos. Las demás cuentas son de lectura, incluso si tenían una sesión anterior de administración.

En `/admin/accounts`, crear una cuenta y marcar las invitaciones que puede ver. Guardar cada permiso. Sin fecha, el acceso es inmediato; con fecha, se activa automáticamente en hora de Uruguay (`America/Montevideo`). Desmarcar y guardar revoca el permiso en la siguiente solicitud. Las personas que revisan borradores ingresan por `/admin/login` y encuentran las vistas previas autorizadas en `/mis-invitaciones`; un borrador aún no habilitado no aparece. Las URLs `/invitacion/slug` son públicas solo cuando la tarjeta está publicada. Los archivos decorativos estáticos no son documentos privados y siguen siendo públicos.

### Respaldo de la grabación del 14 de octubre

`Theme.Layout: "vineyard-archive"` aísla la plantilla y los estilos históricos de `852a1c7` de los cambios de la invitación actual. Usa `_VineyardArchive`, `_RsvpArchive` y `vineyard-archive.css`. El JSON local `App_Data/DesignBackups/zenia-diego-reconstruida-grabacion-20261014.json` reconstruye el contenido visible en la grabación aportada (no es un volcado exacto recuperado de la base). Las imágenes originales se conservaron; los mapas son enlaces de búsqueda reconstruidos porque el video no muestra sus URLs. Se guarda como borrador separado sin permisos para otras cuentas.

### Actualizar la base de producción desde una terminal local (Bash)

La imagen de producción solo incluye el runtime y no ejecuta migraciones automáticamente. Desde el checkout actualizado y con el SDK .NET 10 instalado:

```bash
git pull --ff-only origin master
dotnet tool restore
dotnet restore
read -r -s -p 'Conexión MySQL de producción: ' invitation_prod_connection
printf '\n'
ConnectionStrings__Invitations="$invitation_prod_connection" dotnet ef database update
unset invitation_prod_connection
```

Usar la conexión externa de MySQL si se ejecuta fuera del hosting (una dirección privada interna del proveedor no es accesible desde la computadora). El comando aplica solo migraciones pendientes, incluida `InvitationPermissions`. Respaldar la base antes de aplicarlas. Como alternativa para una base que ya tenga `AdminAccounts`, ejecutar una sola vez `Data/invitation-permissions.sql` con el cliente MySQL; no combinar ambas vías para la misma migración. Las configuraciones e invitaciones guardadas en la base local no se copian con Git ni con las migraciones.


Las copias Guardá esta fecha abiertas desde el panel no se guardan hasta confirmar el editor. Un error de validación no crea la invitación y el mismo formulario conserva su ID al reintentarlo. La administradora puede eliminar invitaciones desde el panel con confirmación escrita; se eliminan también sus invitados, respuestas, mesas, etiquetas y permisos. Las operaciones de administración requieren el rol Owner; las vistas previas requieren sesión y permiso por invitación.


### Editor visual y plano de mesas (octubre de 2026)

Antes de usar esta versión en producción, aplicar `dotnet ef database update` desde el checkout actualizado con la conexión de producción configurada según las instrucciones anteriores. Se incorporan `SeatingFloorPlan`, `VisualInvitationDesigner` y `OptionalSeatingPlan`. La imagen de Railway no aplica estas migraciones al arrancar.

El plano se habilita por invitación con «Habilitar plano de mesas». Las nuevas invitaciones lo tienen desactivado; las que ya tenían mesas o distribución lo conservan habilitado. Desactivarlo oculta y bloquea el plano sin borrar los datos.

El editor visual tiene borrador y diseño aplicado separados. En Cuentas, Owner puede habilitar expresamente «Permitir editar diseño (sin publicar)» para una invitación; las otras cuentas mantienen acceso de lectura hasta que se les concede ese permiso. Aplicar el diseño final sigue siendo exclusivo de Owner. Más detalles en `docs/editor-visual.md` y `docs/plano-de-mesas.md`.
