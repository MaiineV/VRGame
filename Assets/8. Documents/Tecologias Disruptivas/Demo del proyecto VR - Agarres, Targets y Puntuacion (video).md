# Demo del proyecto VR — Agarres, Targets y Puntuación (video)

> Tema: muestra/demostración del proyecto final de Realidad Virtual y Mixta (Unity + Meta Quest) que integra agarrar objetos, lanzar a dianas (targets), puntuación, teletransportación y demás interacciones de la serie.
> Fuente: video propio `muestra agarres fullHD.mp4`.
>
> ⚠️ **Nota:** este resumen está **reconstruido a partir de fotogramas del video** (lo que se ve en pantalla). NO incluye la narración hablada (el audio no se puede transcribir automáticamente). El video parece grabado con **OBS Studio** durante una **clase online** (se ven miniaturas de participantes a la derecha; etiqueta "LOPEZ JUAN IGNACIO").

---

## 1. muestra agarres fullHD (`muestra agarres fullHD.mp4`)

**Proyecto:** Unity 2022.3.6f3 (Android / DX11), `3S Learn - NivelDePrueba`, con **Meta XR Tools**.

### Lo que se ve en la demo (modo Play)
1. **Pantalla de inicio:** un nivel VR con un botón amarillo **"Comenzar"**, una **mano** (hand tracking), un personaje tipo payaso (Clown Assistant) y elementos decorativos (paredes a rayas rojas/grises).
2. **Juego de puntería/lanzamiento:** sobre una mesa hay **esferas de colores** (azul, verde, amarillo) que se pueden **agarrar con la mano** y lanzar hacia **dianas (targets) de colores** en la pared (bullseyes rojo, azul, amarillo, verde).
3. Se muestra el **agarre de una esfera** (la mano toma la bola azul) — demostración de la interacción de grab vista en los tutoriales.
4. **Pantalla de resultado:** al terminar aparece el texto **"Tu puntuación es: 18"** — hay un sistema de puntuación según las dianas acertadas.

### Lo que se ve en el Editor de Unity (estructura del proyecto)
La **Hierarchy** muestra que el nivel integra todos los conceptos de la serie:
- `Scenario` → `Ground`, `Table`, `Entrance`, `Wall`...
- `Canvas de bienvenida` (UI).
- Esferas y dianas: `BlueBall`, `RedBall`, `YellowBall`, `GreenTarget`, `RedTarget`, `BlueTarget`, `YellowTarget`.
- `EventSystem`.
- **Building Blocks de Meta:** `[BuildingBlock] Camera Rig`, `[BuildingBlock] Passthrough` (realidad mixta), `[BuildingBlock] Real Hands`.
- **Locomoción:** `Teleport NavMesh`, `Stairs`, `InvalidTeleportArea`, `PlatformLowR / MedR / HighR`.
- `Clown Assistant`, `Base`, `Snap Interactable`.
- `[BuildingBlock] Cube`.

**Inspector del `[BuildingBlock] Cube`** (ejemplo de objeto agarrable), con los componentes:
- **Transform**, **Building Block (Script)**, **Cube (Mesh Filter)**, **Mesh Renderer**.
- **Box Collider** (con Is Trigger).
- **Rigidbody** (Mass 1, Use Gravity / Is Kinematic, etc.).
- **Grabbable (Script)**.
- **Grab Free Transformer (Script)**.
- **Rigidbody Kinematic Locker (Script)**.

> **Idea del video:** es la **muestra del proyecto final integrado** — combina agarrar/lanzar objetos (Grabbable), dianas con puntuación, manos reales, passthrough (RM), teletransportación (NavMesh + plataformas) y snap, es decir, la aplicación de todos los temas de la serie en un solo nivel jugable.

---
