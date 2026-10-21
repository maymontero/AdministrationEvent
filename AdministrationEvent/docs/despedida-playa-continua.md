> Exploración anterior, reemplazada por el diseño descrito en `despedida-diseno-simple.md`. Los recursos mencionados aquí pueden no formar parte de la versión actual.

# Prueba de playa continua

Esta variante utiliza `Theme.Layout = "beach-continuous"` y una copia en borrador de la invitación de origen. El diseño a rayas continúa en el layout `beach`.

Original: `323162e8-037d-458f-b3ce-1fbd5cc01afa`. Copia local: `aab30e94-bd25-4fac-8982-36d513cbc31d`, slug `despedida-chea-playa`. Se verificó que el contenido original permaneciera idéntico al guardar la copia. La copia conserva textos, fecha, música y cuenta regresiva; no copia invitados, respuestas ni permisos de otras personas y no está publicada.

Recursos finales: `wwwroot/images/despedida-playa/portada-cinco-reposeras-v1.webp`, `wwwroot/images/despedida-playa/arena-continua-v1.webp` y `wwwroot/images/despedida-playa/toallas-v1.webp`.

Portada con exactamente cinco reposeras vacías; continuación de arena con mar en el margen y una pausa ilustrada de toallas. Las imágenes de esta prueba están en `wwwroot/images/despedida-playa/`. Se crearon con la herramienta integrada ImageGen y se codificaron a WebP. Las toallas se reutilizan de la exploración anterior de esta conversación.

## Prompt de la portada

```text
Edit target: the supplied pink-sky coastal gouache beach illustration.
Keep the exact style, hand-painted brushwork, pink sky, cream clouds, blue sea, orange and cream umbrellas, peach sand, starfish and composition. No words and no people.
Required single change: the beach must have EXACTLY FIVE empty striped lounge chairs, each clearly visible and countable. The current image has only four. Add ONE more matching coral-and-cream striped lounge chair in the diagonal row on the right, between the distant chair and the middle chairs. Adjust spacing slightly if needed so all FIVE separate lounge chairs can be counted individually without overlapping or obscuring one another. Preserve the generous empty pink sky at the top for real webpage titles. Do not add any other objects. Five empty chairs total. Keep the flat freehand summer postcard aesthetic, simple shapes, broad imperfect brush edges, no photorealism, no people, no flowers, no text.
```

## Prompt de la arena

```text
Use case: illustration-story. Asset type: one very tall continuous background for the sandy continuation of a hand-painted coastal invitation, portrait 1:3. A thin strip of faded denim-blue sea runs down the far LEFT 10-15 percent of the entire image, with one or two irregular loose cream-white surf brushstrokes at its edge. The remaining 85 percent is broad quiet PALE PEACH sand, open and uncluttered all the way down for website text. The shoreline has very gentle shallow bends without crossing the central text area. Simple flat matte opaque gouache, freely hand-painted broad strokes, irregular human brush edges, subtle dry brush marks, low detail, same aesthetic as a simple vintage summer travel postcard with pink sky and orange striped parasols. Sand base pale peach #ffe4c8. Faded blue #78aebb with soft cream edges. No sky or horizon, this is a continuation down the sand. NO CHAIRS, no loungers, no umbrellas, NO PEOPLE, no plants or flowers, no rocks, no shells, no towels, no objects at all. No letters, no words, no logo, no border. Only a very simple meandering narrow blue shoreline at left and a large unbroken expanse of pale peach sand on the right. Not photorealistic, no detailed ocean caustics, no realistic sand grain. One continuous simple brush-painted backdrop.
```
