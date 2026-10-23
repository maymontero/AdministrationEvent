# Editor visual de invitaciones

Acceso Owner desde «Editor visual» en cada tarjeta del panel. Para otra cuenta, habilitar explícitamente «Permitir editar diseño (sin publicar)» en Cuentas para esa invitación. Se respetan la fecha de habilitación, la revocación y la desactivación de la cuenta. El editor no concede acceso a invitados, configuración general ni publicación.

Orientado a iPad y web, ancho mínimo recomendado 768 px. El lienzo usa 900 px y escala de forma uniforme, con posiciones relativas horizontales y píxeles verticales. No reorganiza el contenido para móvil; la tarjeta se reduce manteniendo la composición.

Secciones con alto, color e imagen de fondo; reordenar, duplicar o eliminar. Elementos: título, texto, imagen, botón HTTPS y cuenta regresiva según la fecha del evento. Posición táctil/mouse mediante pointer events, tamaño desde la esquina, posición/tamaño numéricos y flechas de teclado. Tipografía, color, alineación y orden de superposición. Biblioteca y carga de imágenes por colección existente. Deshacer y rehacer en la sesión.

La primera apertura prepara un punto de partida con los textos e imágenes de la invitación; no es una conversión idéntica de las plantillas existentes. Guardar crea un borrador separado de la presentación actual. Vista previa muestra el borrador guardado. Solo Owner puede aplicar una instantánea del borrador a la invitación o volver al diseño anterior; la publicación general y enlaces conservan su estado original. RSVP y música se mantienen debajo del lienzo cuando están habilitados.

Persistencia en VisualDesigns, con versión para impedir sobrescribir un guardado de otra sesión. La versión aplicada se conserva separada del borrador. Migración: VisualInvitationDesigner, posterior a SeatingFloorPlan.

Las fuentes locales de `wwwroot/fonts` son compartidas. El selector muestra nombre y muestra visual, permite buscar, carga las muestras visibles y guarda la ruta de la fuente con cada elemento; el renderizador carga esa misma fuente en la tarjeta. La selección de un elemento conserva su orden real de capas, incluso mientras está seleccionado.
