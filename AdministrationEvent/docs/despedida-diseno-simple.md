# Playa continua · dibujos simples

La versión actual usa ilustraciones SVG editables de colores planos, con formas básicas, sin acuarela detallada, texturas ni reflejos. El fondo tiene bandas turquesas y espuma blanca, tomando como guía las referencias nuevas de la usuaria.

Los recursos activos están en `wwwroot/images/despedida-playa/simple/`: portada, continuación del mar, cangrejo y estrella, sombrero y chanclas, conchas y estrella, avión y valija, y cinco tragos.

La portada usa una única composición desde arriba: el agua queda en la parte superior y en el borde derecho; las cinco sillas están enteramente en arena seca, orientadas hacia la parte superior. Una sombrilla aparece detrás de las sillas; ninguna silla lleva velo. Un anillo sencillo, sin cruces decorativas, y un velo independiente separan las fechas del dress code. El avión tiene cuerpo y alas alineados y se acompaña de un carry-on con ruedas y manija telescópica; ese grupo se centra en la columna de texto. Fondo y sillas comparten coordenadas para impedir que el agua atraviese la fila.

La arena y el borde derecho del agua continúan en el resto de la página. Se mantienen los textos, fechas, enlaces, música y contador de la copia en borrador `aab30e94-bd25-4fac-8982-36d513cbc31d`. La invitación original a rayas queda intacta.

Las imágenes y prompts de las exploraciones anteriores se conservan como historial y no son el estilo activo de esta versión.

## Movimiento al desplazarse

Solo el layout de playa continua revela los bloques al entrar en pantalla. Llegan suavemente desde la derecha (el mar), con una pequeña oscilación de los dibujos y un desfase entre título y texto; luego quedan quietos. Las animaciones se reinician al salir de pantalla y se repiten al volver, tanto al subir como al bajar. La portada anima el título y dos líneas de olas; el borde continuo del mar permanece fijo para evitar cortes. Las conchas y la estrella se muestran debajo del contador. El contenido permanece visible sin JavaScript o con movimiento reducido, y los enlaces por teclado revelan su sección.
