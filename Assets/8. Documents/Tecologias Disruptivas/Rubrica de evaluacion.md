# Rúbrica de evaluación de trabajos prácticos — Tecnologías Disruptivas

> Transcripción del PDF `Rubrica Tecnologias Disruptivas.pdf`.

---

Se debe entregar un **videojuego de realidad virtual para Meta Oculus 3 o 3S**, que debe tener:

- **Escena de menú principal** funcional (puede ser escena aparte o dentro de la escena principal).
- **Escena jugable.**
- La **mecánica principal o las mecánicas principales** del proyecto bien desarrolladas.
- **Accesibilidad** bien estructurada, para poder llevar adelante la experiencia jugable desde el principio hasta el final.
- **Ausencia de bugs inhabilitantes** para ejecutar el proyecto.
- **Menor cantidad de bugs posible.**
- **Tutorial o explicación clara** para el usuario que incluya el **objetivo** y los **controles** a usar.

> Esto es independiente de cualquier otra instancia de evaluación. Si alguna otra instancia no está aprobada, el alumno/a debe recuperarla para considerarse aprobado/a. Una vez aprobadas las otras instancias, los resultados se promedian para la nota final.

## Apartados evaluados

| Apartado | Explicación | Puntuación |
| :-- | :-- | :--: |
| **Optimización en engine / off game** | Uso de herramientas propias de los motores para optimizar y reducir tiempos de carga: sistemas de oclusión, formatos específicos de compilación para los exports y eliminación de assets no utilizados de las carpetas raíz. | 2/10 |
| **Iluminación** | Establecimiento y uso de luces que otorguen buen rendimiento, usando las herramientas del motor. | 2/10 |
| **Físicas** | Sistemas de físicas bien optimizados, reciclado de objetos y eliminación o establecimiento de referencias a objetos. Uso correcto de los componentes del SDK, como el playerController y todo lo relacionado a física de manos, controles y desplazamientos. | 1/10 |
| **Manejo de assets** | Configuración de imágenes, creación de atlas de texturas, configuración de mipmaps y propiedades de renderizado. Selección justificada de formatos correctos de audio e imagen. Evaluación y modificación de topologías en modelos 3D. Aplicación de **patrones de programación y trabajo que evitan la creación y destrucción de objetos**. | 2/10 |
| **Accesibilidad** | Prácticas y decisiones de diseño para que la mayor cantidad de personas pueda jugar, sin importar capacidades físicas/sensoriales: contrastes de color; señalización en objetos interactuables o estáticos; manejo de espacios de textos e imágenes en el HUD o en el mundo en momentos clave; UI interactuable con manos y controles entendible; uso de sonidos para refuerzos positivos o negativos; controles fluidos, coherentes y entendibles; señalización/explicación de los controles y el objetivo principal; iluminación básica. | 2/10 |
| **Planificación** | Entrega de documento de desarrollo con detalles técnicos y justificaciones sobre las decisiones tomadas al aplicar diseños de programación sobre la ejecución de una mecánica, y explicación detallada de las mecánicas jugables. | 1/10 |

## Aclaraciones importantes

- El trabajo debe ser entregado **en tiempo y forma**.
- Si **falta alguno** de los apartados mencionados, el trabajo queda **automáticamente desaprobado**.
- "Diseños coherentes": apostar a que el usuario **no entiende nada** y **no es un jugador de elite**. El docente evalúa **encarnando a un usuario**, no como jugador experto.
- El usuario **no tiene por qué adivinar** qué botones tocar: las indicaciones deben ser **completas, concisas y breves** (animación, texto, diálogos).
- **Bugs inhabilitantes:** interrumpen la ejecución o impiden la progresión (un botón que no funciona, físicas rotas, no ejecución de elementos clave). Si esto pasa, el juego está **desaprobado**.
- Para evitarlos, **probar los juegos exportados en emulador y en el dispositivo de destino** (a veces corre bien en el motor pero no en la build).
- La documentación debe incluir los **nombres de todos los integrantes**. Quien no figure → **AUSENTE** y deberá recuperar.
- El **uso único de sonidos/onomatopeyas/música NO alcanza** como feedback: el jugador puede jugar **sin audio**, y el tutorial / las marcas clave deben entenderse igual.
- Ante errores, la devolución es **oral y por escrito**, justificando a qué apartado pertenece.
- **Tiempos de carga bajos** y tasa que **sobrepase los 75 FPS en todo momento**.
- Los motores pueden tener **comportamientos anómalos**; ante cualquiera que perjudique el desarrollo, **consultar al docente** (el desconocimiento no justifica el fallo).
