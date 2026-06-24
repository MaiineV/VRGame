# Building Blocks (Meta XR SDK)

Los **Building Blocks** son componentes modulares pre-armados del SDK de Meta para agregar funcionalidades XR rápido, mediante configuraciones predeterminadas. Se acceden desde **`Oculus → Tools → Building Blocks`** (o `Meta XR Tools → Building Blocks`) y se arrastran a la jerarquía. Comprender su funcionamiento interno sigue siendo clave para resolver errores.

---

## Parte 1 — Building Blocks vistos en el curso (los que entran al examen)

Estos son los que aparecen en tu material (Resumen Primer Parcial + tutoriales + proyecto):

| Building Block | Función |
| :-- | :-- |
| **Camera Rig** | Representa al jugador; gestiona el renderizado estereoscópico. Incluye **OVR Camera Rig** (cámaras por ojo) y **OVR Manager** (interfaz hardware↔Unity, modelo de Quest, input). **Indispensable.** |
| **Pass Through** | Habilita el video passthrough → **Realidad Mixta** (ver el entorno real). Mejor calidad en Quest 3. |
| **Hand Tracking** | Rastreo de manos (sensores/cámaras detectan los dedos). Al agregarlo, el input pasa de "controles" a "controles y manos". |
| **Synthetic Hands / Virtual Hands** | Representación visual de las manos + interacciones avanzadas (agarrar/manipular). |
| **Grabbable Item / Grab Interaction** | Objeto que se puede agarrar, mover y lanzar (agrega la interacción de agarre). |
| **Real Hands** | Muestra las manos reales del usuario vía passthrough en vez de manos renderizadas. |

---

## Parte 2 — Lista oficial completa del Meta XR SDK (referencia)

Lista completa y agrupada por categoría (fuente: documentación oficial de Meta Horizon OS). Para acceder a todos hay que tener el **Meta XR All-in-One SDK** (el **Core SDK** es requisito de todos).

### Core
- **Camera Rig:** rastrea cabeza y cuerpo y actualiza la cámara virtual; es la base.
- **Passthrough:** muestra el video en vivo del entorno físico (Realidad Mixta).
- **Controller Tracking:** rastrea posición, rotación y movimiento de los controles.
- **Eye Gaze:** rastrea la dirección de la mirada (selección de UI, render, social).
- **Hand Tracking:** detecta posición, orientación y poses de los dedos (sin controles).

### Interaction
- **Grab Interaction:** agarrar y soltar objetos con poses de mano o grips del control.
- **Interactions Rig:** rig pre-configurado con los interactores estándar (grab, poke, ray, teleport).
- **Poke Interaction:** detecta cuando un dedo/control toca una superficie (presionar botones).
- **Ray Interaction:** lanza un rayo desde la mano/control para apuntar a objetos lejanos.
- **Distance Grab:** agarrar objetos a distancia apuntando y gesticulando.
- **Touch Hand Grab:** agarre por contacto con poses de mano naturales y dedos realistas.
- **Real Hands:** muestra las manos reales por passthrough en vez de manos renderizadas.
- **Controller Buttons Mapper:** asigna botones del control a acciones personalizadas.

### Movement
- **Teleport:** locomoción de point-and-teleport para moverse por espacios grandes.
- **Character Retargeter:** mapea el tracking de cuerpo completo a rigs de personajes.

### Mixed Reality — Passthrough
- **Occlusion:** usa sensado de profundidad para ocultar objetos virtuales detrás de superficies físicas.
- **Passthrough Window:** abre "ventanas" en el entorno virtual para revelar el mundo físico.

### Mixed Reality — Scene
- **Effect Mesh:** aplica efectos visuales (resaltados) a superficies físicas detectadas.
- **Find Spawn Positions:** analiza la geometría de la escena para hallar posiciones válidas.
- **Anchor Prefab Spawner:** instancia prefabs automáticamente en anchors de escena detectados.
- **Instant Content Placement:** detecta superficies y ubica contenido virtual automáticamente.
- **Room Guardian:** dibuja el límite visible del área de juego.
- **Scene Debugger:** muestra wireframes y etiquetas de los elementos de escena detectados.

### Spatial Anchor
- **Spatial Anchor Core:** crea puntos de referencia persistentes atados a ubicaciones físicas.
- **Shared Spatial Anchor Core:** puntos persistentes para compartir entre dispositivos.
- **Colocation:** alinea contenido virtual en la misma ubicación física entre varios visores.
- **Sample Spatial Anchor Controller:** código de ejemplo para guardar/cargar anchors.

### AI
- **Passthrough Camera Access:** acceso directo al feed de la cámara para visión por computadora.
- **Passthrough Camera Visualizer:** muestra el feed crudo en pantalla para debug.
- **Object Detection:** identifica objetos reales con machine learning (bounding boxes).
- **Large Language Models:** modelos de IA multimodal (texto, imágenes, video).
- **Speech to Text:** convierte voz del usuario en texto.
- **Text to Speech:** genera voz realista a partir de texto.

### Audio
- **Spatial Audio:** posiciona fuentes de audio en el espacio 3D (dirección y distancia).

### Haptics
- **Haptics:** dispara vibraciones del control en respuesta a las acciones.

### Voice
- **Dictation:** transcribe voz continua a texto en tiempo real (entrada manos libres).
- **Speak Text:** convierte texto en audio hablado.

### Avatars
- **Networked Avatar:** muestra avatares de usuarios y sincroniza sus movimientos.

### Multiplayer
- **Networked Grabbable Object:** sincroniza el estado de agarre y posición entre usuarios.
- **Auto Matchmaking:** ubica automáticamente a los usuarios en una sala compartida.
- **Custom Matchmaking:** requiere token para unirse a una sala (sesiones privadas).
- **Friends Matchmaking:** permite invitar a amigos de Meta a la sesión.
- **Local Matchmaking:** conecta jugadores en la misma cercanía física vía Bluetooth.
- **Player Name Tag:** etiquetas flotantes con el nombre sobre los avatares.
- **Player Voice Chat:** voz en tiempo real entre usuarios (Photon Voice 2).
- **Networked Character Retargeter:** sincroniza personajes con tracking de cuerpo completo.

---

> **Fuentes:** [Building Blocks Overview — Meta Horizon OS Developers](https://developers.meta.com/horizon/documentation/unity/unity-building-blocks-overview/) · [Explore Meta Quest Features with Building Blocks](https://developers.meta.com/horizon/documentation/unity/bb-overview/)
> *La Parte 2 es la lista oficial completa (más amplia que lo visto en clase). Para el examen, lo central es la Parte 1.*
