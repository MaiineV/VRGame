# Agarrar objetos en Realidad Virtual y Mixta — Meta Quest

> Tema: crear interacciones para **agarrar objetos** (grab) en RV/RM con manos o controles, en Unity para Meta Quest.
> Fuente: serie *Unity Adventure* (YouTube).

---

## 1. ¿Cómo agarrar objetos en Realidad Virtual y Mixta? - Meta Quest

🔗 https://www.youtube.com/watch?v=F5kjQlIQoFk

**Objetivo del video:** crear interacciones para agarrar objetos en RV/RM usando las **manos** o los **controles**.

**Aclaración inicial:** el proyecto se actualizó a la última versión del paquete **Meta XR All-in-One SDK**, que al grabar era la **versión 65**.

---

### Paso 1 — Crear la escena
1. `File > New Scene > Basic`.
2. `Ctrl + S`, guardar en `Scenes` con el nombre **`Grab Interaction`**.

### Paso 2 — Configurar el Rig con Building Blocks
`Oculus > Tools > Building Blocks`, agregar (clic en `+`):
1. **Camera Rig**
2. **Hand Tracking**
3. **Virtual Hands**

> Nota: existe un bloque **Grab Interaction** que agrega la interacción automáticamente y añade un cubo agarrable, pero usarlo **salta pasos importantes** que conviene conocer. Por eso no se usa acá.

Con esto ya está listo para usar las **manos**. Falta la configuración para los **controles**.

### Paso 3 — Agregar los controles
1. En el buscador de **Project** escribir **`OVR Controllers`**.
2. Cambiar la búsqueda de **Asset** a **All**.
3. Agregar ese prefab **dentro del bloque de interacciones**, al lado de **Hand Interactions**.
4. Eliminar la **Main Camera**.

### Paso 4 — Agregar los interactores (interactors)
En el bloque de interacciones, al abrir manos y controles (izquierda y derecha) hay Game Objects llamados **Interactors**: `Hand Interactor Left/Right` y `Controller Interactors`. Ahí se agregan los interactores que actuarán sobre los objetos.

**Tipos de interactores (vienen como prefabs preconfigurados):** **Grab** (el que se usa acá para agarrar), **Distance Grab**, **Poke** y **Ray**.

1. **Para las manos:** buscar **`Hand Grab Interactor`** → agregarlo en los interactores de la mano **izquierda** y de la **derecha**.
2. **Para los controles:** buscar **`Controller Grab Interactor`** → agregarlo en el control **izquierdo** y el **derecho**.

### Paso 5 — Configurar los visuales de los interactores de mano
Abrir el prefab `Hand Grab Interactor` de la mano **izquierda** → contiene los componentes que usa para agarrar. En **Visual** hay dos componentes importantes:

**a) Hand Grab Visual** (script `Hand Grab State Visual`)
- Maneja y aplica las **poses de agarre** definidas en el objeto, sobre la mano sintética.
- Referenciar la **mano sintética**: arrastrar **Synthetic Left Hand**. Activar el Game Object.

**b) Hand Grab Glow** (script del mismo nombre)
- Crea un efecto de **brillo y contorno** al pasar sobre un objeto o agarrarlo.
- Referencias (mano izquierda): abrir **Synthetic Left Hand** → referenciar **OVR Left Hand Visuals** → abrir de nuevo → referenciar **Hand Mesh Note** en **Hand Renderer** y también en **Material Editor**.
- **Tipo de efecto:** acá se deja en **Contorno (Outline)**. Activarlo para que funcione.

**Mano derecha:** repetir en el interactor de la mano derecha:
- En **Visuals** → activar **Hand Grab Visual** → referenciar la **mano sintética derecha** → activar.
- **Hand Grab Glow** → abrir **Synthetic Right Hand** → referenciar **OVR Right Hand Visual** → abrir → referenciar **Hand Renderer** y **Material Editor**.
- **Tipo:** cambiar a **Brillo (Glow)** (así se ven los dos efectos a la vez: brillo y contorno).

> Los interactores de los **controles** ya traen los componentes necesarios y **no requieren configuración**.

---

### Paso 6 — Configurar un objeto agarrable (método automático — Grab Wizard)
1. Crear un cubo: clic derecho `> 3D Object > Cube`.
2. Seleccionarlo → clic derecho `> Interaction SDK > Add Grab Interaction`.
3. Se abre la ventana **Grab Wizard**, donde se define:
   - **Tipo de interactor:** manos, controles o **ambos** (acá: ambos).
   - **Tipo de gesto** para agarrar (se detalla más abajo).
   - Si se genera un **collider** en el objeto.
   - Componentes requeridos: pide un **Rigidbody** → como no lo tiene, clic en **Fix** para agregarlo.
4. Clic en **Create** → se añaden automáticamente todos los componentes necesarios para agarrar el objeto.

**Ajustar el cubo para probar:**
- Escala: **X = 0.15**, **Y = 0.15**, **Z = 0.15**.
- Posición: **X = 0**, **Y = 1.25**, **Z = 0.35**.

**Prueba (Play):** se puede agarrar el cubo. Al agarrarlo se aplica **brillo + contorno** en la mano derecha y solo **contorno** en la izquierda (personalizable en el script `Hand Grab Glow`). También funciona con ambos controles.

---

### Paso 7 — Configurar un objeto agarrable (paso a paso, manual)
Ejemplo con el modelo 3D de una botella (sirve cualquier modelo). Recursos del video en la descripción.

**Estructura de Game Objects vacíos** (clic derecho `> Create Empty`):
```
Root Grab Object
 ├── Visual          (acá va el modelo 3D)
 ├── Colliders
 ├── Hand Grab       (duplicado de Colliders)
 ├── Controller Grab (duplicado)
 └── SFX
```
- En **Visual** arrastrar el modelo 3D (la botella).

**Componentes en el padre (`Root Grab Object`):**
1. **Rigidbody** — define las características físicas del objeto.
2. **Grabbable** — permite seleccionar y mover el objeto. Toma automáticamente la referencia del Rigidbody.
   - **Decisión de físicas:**
     - Parámetro 1: **desactiva las físicas al agarrar** el objeto.
     - Parámetro 2: **aplica velocidad de lanzamiento al soltar**.
     - En este ejemplo el objeto va **sin físicas**: eliminar la referencia del Rigidbody, **desactivar Use Gravity** y **activar Is Kinematic**.
   - **Transfer On Second Selection:** si está activo, se puede pasar el objeto de una mano a la otra mientras se agarra; si no, se sujeta con ambas manos (como una espada). Acá se **activa**.
   - **Max Grab Points (puntos máximos de agarre):** `-1` = infinitos, o limitarlo a 1 o 2.
   - **Opciones > Transformers:** define cómo se mueve el objeto al ser agarrado (tema avanzado, da más control).
   - **Necesita colliders** para detectar cuándo se selecciona/agarra el objeto.

**Definir los colliders** (en el Game Object `Colliders`):
- Opción A: ir a **Visual** (donde está el Mesh Renderer) → `Add Component` → **Mesh Collider** → activar **Convex** (genera un collider que aproxima la forma).
- Opción B: añadir varios colliders manualmente. Para la botella se usan **dos Capsule Collider**.
- La idea: que el/los collider(s) cubran el objeto o la **zona de agarre**. Depende de cada objeto.

**Componente para agarrar con las MANOS** (en `Hand Grab`):
- Buscar y agregar **Hand Grab Interactable**.
- Parámetros:
  - **Max interactores** (`-1` = sin límite) y **cuántos interactores pueden seleccionarlo** (`-1`).
  - Referencias al **Grabbable** y al **Rigidbody** del padre.
  - **Tipo de gesto (Grab Type):** **Pinch**, **Palm**, o ambos.
  - **Reglas de los dedos (finger rules):** a cada dedo se le asigna un estado: **Ignored** (ignorado), **Optional** (opcional) o **Required** (requerido/obligatorio).
    - Si hay dedos **requeridos**: todos deben hacer el gesto para que se produzca el agarre.
    - Si **no hay requeridos** (como en Pinch): basta con que **al menos uno opcional** haga el gesto.
    - Los **ignorados** no se tienen en cuenta. (Ej.: pinch = pulgar + índice, o pulgar + dedo medio.)
  - **Select Mode (cuándo termina el agarre):**
    - **All Released:** termina cuando **todos** los dedos requeridos/opcionales se soltaron.
    - **Any Released:** termina cuando **alguno** se soltó.
  - **Palm grab rules:** ejemplo con 3 obligatorios y 2 opcionales.
  - **Poses de agarre:** tema tan importante que se dedica el **próximo video** completo a crear e implementar poses de agarre.

**Componente para agarrar con los CONTROLES** (en `Controller Grab`):
- Buscar y agregar **Grab Interactable**.
- Tiene casi los mismos campos, pero **no toma** automáticamente las referencias → hay que asignarlas:
  - Arrastrar **Root Grab Object** para que tome el **Grabbable** y el **Rigidbody**.

> En este punto el objeto ya se puede agarrar con manos y controles.

---

### Paso 8 — Efectos de sonido al agarrar y soltar
Usa un componente que da acceso a los estados antes/durante/después de la interacción.

1. En el padre (`Root Grab Object`) → `Add Component` → buscar **Pointable Unity Event Wrapper**.
2. Referenciar el **Grabbable** (arrastrarlo).
3. **Eventos disponibles:** `When Release` (soltar), `When Hover` (pasar la mano por encima), `When Unhover` (sale), `When Select` (agarrar), `When Unselect` (des-seleccionar), `When Move` (mover), `When Cancel` (cancelar).

**Configurar los audios** (en el Game Object `SFX`):
1. Crear dos Game Objects vacíos: **`Select`** y **`Release`**.
2. En ambos: `Add Component` → **Audio Source** → **desactivar Play On Awake**.
3. En **Select** asignar el audio de agarrar; en **Release** el audio de soltar.

**Conectar los eventos** (en el `Pointable Unity Event Wrapper` del padre):
- **When Release** → `+` → arrastrar el objeto **Release** → función **Audio Source > Play**.
- **When Select** → `+` → arrastrar el objeto **Select** → función **Audio Source > Play**.

> Se pueden agregar otras mecánicas que se llamen al activarse estos eventos.

**Posición final del objeto:** **X = -0.3**, **Y = 1.25**, **Z = 0.3**.

### Paso 9 — Guardar y probar
1. `Ctrl + S`.
2. ⚠️ **Importante (si el casco se cierra al ejecutar):** en `Player Settings`, si la **API gráfica** está en **OpenGL ES 3**, cambiarla a **Vulkan**.
3. En RV se puede agarrar el objeto; en **Realidad Mixta el proceso es exactamente el mismo**.
4. Se puede cambiar el color del brillo/contorno al color del objeto para un efecto más interesante. Al agarrar y soltar se reproduce el sonido configurado.

### Siguiente paso
- El próximo video trata por completo sobre **crear e implementar poses de agarre**.

---
