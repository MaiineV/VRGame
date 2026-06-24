# Snap Interaction — Posicionar Objetos Automáticamente en VR (Unity Meta Quest)

> Tema: interacciones de tipo **Snap** para ajustar objetos a una posición predefinida automáticamente, definir posiciones de retorno y filtrar qué objetos encajan en cada Snap. Unity para Meta Quest.
> Fuente: serie *Unity Adventure* (YouTube).

---

## 1. Snap Interaction: Posiciona Objetos Automáticamente en VR

🔗 https://www.youtube.com/watch?v=Ogt09S_3tVc

**Para qué sirve el Snap:**
- Asegurar que un objeto se coloque de forma fácil y precisa en una **ubicación predefinida**.
- Establecer **posiciones predeterminadas** a las que el objeto regresa al terminar la interacción (evita perder objetos importantes en el entorno y da una experiencia más fluida).

---

### Paso 1 — Crear la escena
1. `File > New Scene > Basic`.
2. Eliminar la **Main Camera**.
3. `Ctrl + S`, guardar en `Scenes` con el nombre **`Snap Interaction`**.

### Paso 2 — Agregar el prefab de interacciones
1. En **Project** buscar **`OVR Camera Rig Interaction`** → cambiar búsqueda a **All**.
2. Arrastrar el prefab a la jerarquía.

### Paso 3 — Crear el objeto agarrable (cubo)
1. Clic derecho `> 3D Object > Cube`.
   - Posición: **X = 0**, **Y = 1.25**, **Z = 0.35**.
   - Escala: **0.15** en X, Y, Z.
2. Hacerlo agarrable: clic derecho sobre el cubo `> Interaction SDK > Add Grab Interaction` → agregar el **Rigidbody** con **Fix All** → **Create**.

### Paso 4 — Configurar el Snap Interactor (en el objeto que se mueve)
1. En el cubo (padre), en el **Collider** asegurarse de activar **Is Trigger**.
2. Dentro del cubo, clic derecho `> Create Empty`, llamarlo **`Snap Interactor`**.
3. `Add Component` → **Snap Interactor**. Referencias:
   - **Pointable Element:** el componente que procesa los eventos de interacción (seleccionado, soltado, tocado, etc.). En este caso es el **Grabbable** → arrastrar el **ISDK Hand Grab Interaction**.
   - **Rigidbody:** referenciarlo.
   - Abajo están las **Options** (se ven en detalle más adelante).

### Paso 5 — Crear el Snap Interactable (el destino donde encaja el objeto)
1. **Fuera del cubo**, clic derecho `> Create Empty`, llamarlo **`Snap Interactable`**.
   - Es el objeto que se posiciona **donde queremos que el objeto haga Snap** (a dónde se moverá).
   - Posición: **X = 0.35**, **Y = 1**, **Z = 0.35**.
2. `Add Component`:
   - **Box Collider** → **define la zona donde se activa el Snap**: al llevar el objeto a esa área y soltarlo, el Snap lo posiciona en el centro/posición de este Game Object. (El collider puede ser **esfera, cápsula o caja**.)
   - **Rigidbody** → **desactivar Use Gravity** y **activar Is Kinematic**.
   - **Snap Interactable** (el componente más importante).

### Paso 6 — Visual de la zona del Snap (opcional pero útil)
1. Dentro de **Snap Interactable**, clic derecho `> 3D Object > Cube`, un poco más grande: escala **0.155** en X, Y, Z.
2. Crear material: clic derecho `> Create > Material`, llamarlo **`Snap Visual`**:
   - **Rendering Mode = Transparent**, color **verde**, **Alpha = 100** (transparente).
   - Asignarlo al cubo (también llamado **Snap Visual**). Eliminar su **collider**.
   - Ajustar el tamaño del collider al del cubo: **0.155** en X, Y, Z.
3. Mecánica para mostrar/ocultar el visual: `Add Component` → **Interactable Unity Event Wrapper**:
   - Referenciar **Snap Interactable**.
   - **When Select** (Snap activo) → arrastrar el cubo visual → función **GameObject > SetActive = false** (se oculta).
   - **When Unselect** (sin Snap) → **GameObject > SetActive = true** (se muestra).

### Paso 7 — Probar
- `Ctrl + S` → en RV: agarrar el cubo, llevarlo a la zona del Snap y soltarlo → se ajusta a la ubicación definida. El cubo verde desaparece al hacer Snap y reaparece al sacarlo.

---

### Paso 8 — Opciones del Snap Interactor (con varios Snaps)
Para ejemplificar, **duplicar el Snap dos veces** (`Ctrl + D`):
- Snap 1 (original): X = 0.35.
- Snap 2: posición **X = 0**.
- Snap 3: posición **X = -0.35**.

Seleccionar **Snap Interactor** → abrir **Options**:

- **Snap Pose Transform:** la transform que actúa como **pivote** del objeto al moverse hacia el punto de Snap. Por defecto usa la transform del Game Object donde está el script. Si se cambia el pivote (p. ej. a una esquina del cubo), al hacer Snap esa esquina queda en la posición del Snap.
- **Default Interactable:** el punto de Snap predeterminado donde el objeto se posiciona inicialmente; permanece ahí hasta que se interactúa con él. Referenciar uno de los Snaps creados (el del centro = Snap 1).
- **Time Out Interactable:** el punto de Snap al que el objeto **regresa automáticamente** después de cierto tiempo, una vez terminada la interacción (lo tomás, lo movés, lo soltás y vuelve). Referenciar el primer Snap creado.
- **Time Out:** el tiempo para ese regreso. Acá se pone **3** (segundos).

### Paso 9 — Filtros: limitar con qué Snaps puede encajar (Tags)
Para que el objeto **no** interactúe con cierto Snap, se usan **interactable filters** basados en **etiquetas (tags)**.

**En los Snaps (interactables):**
1. Seleccionar **todos** los Snaps → `Add Component` → **Tag Set**.
2. Clic en `+` y agregar una etiqueta a cada uno:
   - Snap 1 → **cube**
   - Snap 2 → **cube**
   - Snap 3 → otra etiqueta, p. ej. **sphere**

**En el Snap Interactor:**
1. `Add Component` → **Tag Set Filter**.
2. En **Options**:
   - **Required Tags:** agregar **cube** (se aconseja una sola etiqueta).
   - **Excluded Tags:** agregar las que NO queremos, p. ej. **sphere** (acá sí se puede agregar más de una).
3. Referenciar el **Tag Set Filter** dentro del **Snap Interactor** → en **Options > Interactable Filters** arrastrarlo.

### Paso 10 — Probar el resultado final
- `Ctrl + S` → en RV:
  - El cubo se coloca en el **Snap predeterminado**.
  - Si se deja fuera de un Snap, tras **3 segundos** regresa al Snap definido como **Time Out**.
  - Al llevarlo al **tercer Snap** (etiqueta *sphere*) **no funciona**, porque está excluido.

---
