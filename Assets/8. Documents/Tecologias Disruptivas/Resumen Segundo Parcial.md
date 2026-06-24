# Resumen Segundo Parcial — Tecnologías Disruptivas

> Solo **teoría** (temas vistos después del Primer Parcial). El Cronograma, la Rúbrica y el "Cómo probar/exportar el juego" están en **páginas aparte** (no son teoría).
> Lo del Primer Parcial (affordance, presencia, embodiment, iluminación/baking, optimización, herramientas del SDK de Meta) ya está en el **Resumen Primer Parcial** y no se repite.

## Índice
1. Corporalidad
2. Memoria muscular
3. Reticles
4. Game feeling, feedback y planificación
5. Accesibilidad
6. Físicas y reciclado de objetos
7. Comunicación entre scripts y patrones de programación
8. Realidad Aumentada (AR) con Vuforia
9. SDK para desarrollo (Meta XR, OpenXR, SteamVR)

---

# Corporalidad

Es la **utilización del propio cuerpo como herramienta principal de interacción**. En realidad virtual los jugadores giran la cabeza, estiran los brazos, manipulan objetos y se desplazan físicamente dentro del espacio disponible. El diseño debe contemplar estas capacidades y limitaciones para que las acciones resulten naturales. Está directamente ligada al **embodiment** (ver Resumen Primer Parcial).

# Memoria muscular

A medida que el jugador **repite movimientos**, desarrolla **hábitos motores** que le permiten interactuar de manera más eficiente. Por eso los **tutoriales deben introducir las mecánicas de forma gradual**, permitiendo practicar acciones básicas antes de enfrentarse a situaciones complejas.

# Reticles

El sistema de **interacción visual** proporciona información al usuario mediante indicadores conocidos como **reticles**. Permiten **comunicar cuándo una superficie es válida** para una acción. Son importantes en **teletransporte**, **interacción a distancia** y **realidad mixta** (donde se analizan planos o mallas detectadas por el dispositivo).
- En la práctica aparecen como el componente **Reticle Data Teleport** (modo **válido / inválido**), visto en el tutorial de teletransportación.

# Game feeling, feedback y planificación

**Game feeling:** es la **calidad de las sensaciones** transmitidas durante la interacción. Cada acción debe producir una respuesta **clara y coherente** (animaciones, sonidos, vibraciones/hápticos, cambios visuales, reacciones físicas). Cuanto más **inmediata y consistente**, más satisfactoria. Ejemplo: *Beat Saber* — cada corte confirma al instante. Sin feedback, el mundo se siente muerto.

## Feedback positivo vs. negativo
Hay que tener **más cuidado con el feedback negativo que con el positivo**: en VR el jugador habita el juego con su cuerpo, así que lo desagradable (errores, castigos, movimientos incómodos) se vive de forma **física e intensa** (frustración, incomodidad, mareo) y puede romper la presencia o hacer que se saque el visor. El positivo refuerza; el negativo mal calibrado expulsa.
> **Ojo (criterio del profe):** el uso **único** de sonidos/música NO alcanza como feedback. El jugador puede jugar sin audio, así que el tutorial y las marcas clave deben entenderse igual (refuerzo **visual** además del sonoro).

## Tangibilidad del trabajo
Las acciones deben producir **consecuencias visibles y comprensibles**: el usuario debe percibir que **realmente hizo algo** (armar un mecanismo pieza por pieza, recargar un arma manualmente, colocar objetos en su lugar). Refuerza la inmersión.

## Criterios de planificación VR/AR
- Diseñar para el **sistema sensorial completo**, no solo para los ojos.
- Pensar en un usuario que **“no entiende nada”** y no es de elite (el docente evalúa encarnando a ese usuario).
- El usuario **no debe adivinar** qué botones tocar: indicaciones **completas, concisas y breves** (animación, texto o diálogos).
- **Documento de desarrollo:** detalles técnicos y **justificación** de las decisiones de programación, y explicación de las mecánicas jugables.

# Accesibilidad

Prácticas y decisiones de diseño para que **la mayor cantidad de personas** pueda jugar, sin importar capacidades físicas o sensoriales:
- **Contrastes de color** adecuados.
- **Señalización** en objetos interactuables o estáticos.
- Buen manejo de **textos e imágenes** en el HUD o el mundo, en momentos clave.
- **UI entendible** con manos y con controles.
- **Sonidos** de refuerzo positivo/negativo que guíen (complementando lo visual, nunca solo audio).
- **Controles fluidos, coherentes y entendibles.**
- **Señalización/explicación** de controles y objetivo principal.
- **Iluminación básica** que ayude a leer el espacio.

# Físicas y reciclado de objetos

- Sistemas de físicas **bien optimizados**: reciclado de objetos y correcto manejo de referencias.
- **Uso correcto de los componentes del SDK**: player/character controller y todo lo de física de manos, controles y desplazamientos.
- En objetos agarrables, configurar bien **Rigidbody** (Use Gravity / Is Kinematic), **Grabbable** y **colliders** para evitar comportamientos inesperados (sobre todo al re-agarrar en sistemas de Snap).

# Comunicación entre scripts y patrones de programación

**Comunicación entre scripts:** gran parte se resuelve **sin código** con los **Event Wrappers**: un interactable expone eventos (Select, Hover, Release…) y desde el Inspector se enlazan acciones de otros objetos/scripts (audio, partículas, mover objetos, cargar escenas). *(Detalle: ver Resumen Primer Parcial.)*

## Patrones de programación
La idea es usar **patrones que eviten crear/destruir objetos** en runtime.
- El clave es el **Object Pooling (pool de objetos):** en vez de Instantiate/Destroy constante (caro, genera picos), se **reutiliza** un conjunto de objetos pre-creados, activándolos y desactivándolos según se necesiten.
- Aplica a balas, partículas, enemigos, ítems → menos picos y **FPS más estables**.

# Realidad Aumentada (AR) con Vuforia

Concepto general (el paso a paso práctico no está en la documentación disponible):
- **Vuforia Engine SDK** (Android/PC): **reconocimiento de imágenes y superficies** para anclar contenido 3D en el mundo real (**image targets**, análisis de planos).
- Unity también ofrece su **SDK nativo de AR (AR Foundation)**.
- En el curso se pidió un proyecto AR con Vuforia: **logo 3D del equipo + interfaz que dirija a un enlace** del proyecto.

# SDK para desarrollo (Meta XR, OpenXR, SteamVR)

## SDK de Meta (Meta XR SDK)

El SDK de Meta está diseñado específicamente para los dispositivos de la familia Meta Quest. Su principal ventaja es el acceso directo a las funcionalidades exclusivas del hardware, como el seguimiento de manos, el passthrough para realidad mixta, el seguimiento espacial avanzado, las optimizaciones específicas para Quest y las herramientas de rendimiento desarrolladas por Meta. Esto permite obtener resultados rápidamente y aprovechar al máximo las capacidades de los dispositivos utilizados en el aula.

Otra ventaja importante es la calidad de la documentación y la cantidad de ejemplos disponibles. Debido a que Meta domina actualmente gran parte del mercado de realidad virtual de consumo, existe una gran comunidad de desarrolladores, tutoriales y recursos educativos.

Sin embargo, el principal inconveniente es la dependencia del ecosistema Meta. Muchas funcionalidades avanzadas son propietarias y pueden requerir modificaciones o reemplazos si el proyecto debe migrarse posteriormente a otros visores. Esto puede generar una cierta dependencia tecnológica y reducir la portabilidad del proyecto hacia otras plataformas.

## OpenXR

OpenXR es un estándar abierto desarrollado por el grupo Khronos con el objetivo de unificar el desarrollo de aplicaciones de realidad virtual y realidad aumentada. Su principal ventaja es la interoperabilidad: una aplicación desarrollada correctamente sobre OpenXR puede ejecutarse en dispositivos de distintos fabricantes con mínimos cambios en el código.

Desde una perspectiva académica y profesional, OpenXR representa una solución muy atractiva porque enseña conceptos más generales y menos dependientes de una empresa específica. Los estudiantes aprenden a trabajar sobre estándares de la industria en lugar de herramientas propietarias.

Como desventaja, OpenXR suele ofrecer acceso únicamente a las características estandarizadas entre los distintos fabricantes. Cuando se desea utilizar alguna función muy específica de un visor concreto, normalmente es necesario recurrir a extensiones particulares o complementar el desarrollo con SDKs propios del fabricante. Esto puede aumentar la complejidad del proyecto y la cantidad de trabajo de integración.

## SteamVR SDK

SteamVR fue durante muchos años uno de los pilares del desarrollo de realidad virtual para PC. Su principal fortaleza es la compatibilidad con una gran variedad de visores conectados a computadora, incluyendo dispositivos de Valve, HTC, Meta y otros fabricantes. También ofrece un ecosistema maduro para experiencias de alta fidelidad gráfica gracias al uso de hardware de escritorio.

Otra ventaja importante es la integración con la plataforma Steam, que facilita la distribución y pruebas de aplicaciones destinadas al mercado de PC VR.

No obstante, SteamVR presenta algunas limitaciones en comparación con enfoques más modernos. Actualmente la industria está migrando progresivamente hacia OpenXR como estándar común, por lo que muchas de las funciones que antes eran exclusivas de SteamVR ahora pueden implementarse mediante OpenXR. Además, SteamVR está orientado principalmente al desarrollo para PC y resulta menos adecuado para dispositivos autónomos como Meta Quest cuando se busca una aplicación independiente que funcione sin computadora.

## Comparación general

El SDK de Meta suele ser la mejor opción cuando el objetivo es desarrollar específicamente para Quest 2 o Quest 3 y aprovechar todas sus capacidades. OpenXR es la alternativa más recomendable cuando se busca compatibilidad multiplataforma y una formación alineada con estándares abiertos. SteamVR continúa siendo una herramienta valiosa para experiencias de PC VR, aunque actualmente gran parte de la industria considera a OpenXR como el camino principal para garantizar interoperabilidad y sostenibilidad a largo plazo.
