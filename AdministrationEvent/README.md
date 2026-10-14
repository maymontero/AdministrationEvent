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
