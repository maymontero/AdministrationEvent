# Organización visual de mesas

Se habilita por invitación con «Habilitar plano de mesas» al crear o editar. Las nuevas invitaciones lo tienen desactivado por defecto. Desactivarlo oculta sus accesos y bloquea las rutas del plano, conservando la distribución y las mesas para poder reactivarlo. La migración OptionalSeatingPlan conserva habilitados los eventos que ya tenían un plano o mesas.

Acceso: «Plano de mesas» en cada invitación del panel, o «Organizar mesas en el plano» en Etiquetas y mesas. Se mantiene la autorización Owner de la administración de invitados.

Opción A: superficie vacía con cuadrícula. Opción B: PNG/JPG del lugar de hasta 5 MB, ajustado sin recorte. La imagen se guarda en la base y solo se sirve mediante una ruta autenticada; no queda expuesta en wwwroot.

Las mesas comparten identidad, nombre y capacidad con la organización de invitados. Crear una mesa la guarda inmediatamente en esa lista; las posiciones se guardan al pulsar «Guardar distribución». Quitar del plano conserva la mesa y sus invitados. Las mesas que ya existen pueden incorporarse desde el selector. Se admite forma redonda o rectangular y giros de 45°.

Coordenadas normalizadas para mantener la distribución al cambiar el tamaño de pantalla. Movimiento con pointer events (mouse/táctil) y flechas de teclado. Aviso de cambios sin guardar al abandonar la página y mensajes de error de guardado. Máximo 200 mesas posicionadas por plano. El editor no asigna invitados mediante arrastre: la asignación continúa en la lista de mesas.

Migración requerida: `20261022114304_SeatingFloorPlan`. Aplicar con `dotnet ef database update` antes de usar esta función en otro entorno.

Verificación: DatabaseChecks cubre persistencia, validación de posiciones e identidad de mesas, conservación de mesas al quitar posiciones, carga de imagen y lectura por invitación. El flujo MySQL usa una invitación temporal y la elimina al terminar.

Cada posición admite una descripción opcional de hasta 200 caracteres. Se edita al seleccionar la mesa y se muestra como etiqueta al pasar el mouse o enfocar con teclado; se guarda con la distribución.
