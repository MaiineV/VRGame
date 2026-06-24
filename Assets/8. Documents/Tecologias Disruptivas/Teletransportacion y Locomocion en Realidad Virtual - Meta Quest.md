# Teletransportación y Locomoción en Realidad Virtual — Meta Quest

> Tema: moverse en RV mediante **teletransportación** (a zonas o puntos específicos) usando controles o manos, en Unity para Meta Quest. Incluye giro (snap turn) y zonas válidas/inválidas.
> Fuente: serie *Unity Adventure* (YouTube).

---

## 1. ¿Cómo Teletransportarse en Realidad Virtual? Unity Meta Quest

🔗 https://www.youtube.com/watch?v=3LCcRWthIYI

**Objetivo del video:** moverse en RV usando teletransportación, con controles o manos, hacia **zonas** o **puntos** específicos definidos.

### Punto de partida
- Proyecto ya configurado para RV/RM en Meta Quest. (Enlace de configuración en la descripción.)

### Paso 1 — Crear la escena
1. `File > New Scene > Basic`.
2. `Ctrl + S`, guardar en `Scenes` con el nombre **`Locomotion`**.
3. Eliminar la **Main Camera**.

### Paso 2 — Agregar el prefab de interacciones (acceso a locomoción)
1. En el buscador de **Project** escribir **`OVR Camera Rig Interaction`**.
2. Cambiar la búsqueda de **Asset** a **All**.
3. Arrastrar el prefab a la jerarquía.
   - Permite **girar** y **teletransportarse** con controles y manos, mediante los interactores que contiene.
   - Girar funciona directamente; **teletransportarse requiere definir zonas o puntos** dentro del ambiente.

### Ambiente de ejemplo
- Se usa un ambiente con **dos pisos y una escalera**. La idea: teletransportarse en ambos pisos y subir la escalera. (Enlace de descarga en la descripción.)

---

### Paso 3 — Zona de teletransportación del PRIMER PISO (vía NavMesh)
1. Clic derecho `> Create Empty`, llamarlo **`Teleport First Floor`**. Hacer **Reset** en el Transform.
2. `Add Component` → **Teleport Interactable** (define y configura una zona/punto de teletransportación).
   - Necesita una **superficie de teletransportación**, que se puede crear con **NavMesh** o con un **Collider**. Acá: NavMesh.
3. `Add Component` → **NavMesh Surface** y referenciarlo.

**Generar el NavMesh (Bake) sobre el primer piso:**
1. Seleccionar el objeto del **primer piso** del ambiente → `Add Component` → **NavMesh Surface**.
   - Define automáticamente las zonas por donde puede desplazarse un **agente** (el jugador) y da acceso a *pathfinding*.
   - Parámetros del agente (por defecto): paso máximo de altura **0.75 m**, pendiente máxima **45°**, radio (envoltura) **0.5 m**, altura **2 m**.
2. Al hacer **Bake** toma todo el ambiente, pero solo se quiere el primer piso y acercarse más a los objetos → ajustar el agente:
   - `Window > AI > Navigation` → cambiar el **Radius** a **0.15** (el resto igual).
3. Volver al Inspector del NavMesh Surface del primer piso → en **Collect Objects** cambiar a **Current Object Hierarchy** (solo los hijos del primer piso).
4. Clic en **Bake** → genera solo la zona del primer piso.

**Terminar la zona:**
- En **Teleport First Floor**, el script **Teleport Interactable** toma automáticamente la zona generada. En **Options** agregar el nombre de la zona (ej.: *Welcome*).
- `Add Component` → **Reticle Data Teleport** (cambia el objeto que se muestra al final del rayo de teletransportación).

### Paso 4 — Zona de la ESCALERA (vía Collider)
1. Clic derecho `> Create Empty`, llamarlo **`Teleport Stairs`**. **Reset** en Transform.
2. `Add Component` → **Teleport Interactable**.
3. Para la superficie usar **Collider Surface** (requiere un collider).
4. Dentro del objeto, clic derecho `> Create Empty`, llamarlo **`Collider`**:
   - `Add Component` → **Box Collider** → tamaño **X = 1**, **Y = 5**, **Z = 0.1**.
   - Alinear con la escalera: posición **X = 0**, **Y = 2.25**, **Z = -4.5**; rotación **X = -40**.
5. Volver a **Teleport Stairs** → referenciar el **Collider** en el Collider Surface.
6. `Add Component` → **Reticle Data Teleport**.

### Paso 5 — Zona del SEGUNDO PISO (vía Collider)
1. Clic derecho `> Create Empty`, llamarlo **`Teleport Second Floor`**. **Reset** en Transform.
2. `Add Component` → **Teleport Interactable**.
3. Superficie → **Collider Surface** → como collider usar el **Box Collider del piso** (en `Second Floor` referenciar **Floor**).
4. `Add Component` → **Reticle Data Teleport**.
> ⚠️ No olvidar **referenciar la superficie** en el Teleport Interactable, o no funcionará.

### Paso 6 — Zona INVÁLIDA (dónde NO se puede teletransportar)
Para la parte vacía donde no debe poderse teletransportar:
1. Clic derecho `> Create Empty`, llamarlo **`Invalid Teleport`**. **Reset** en Transform.
2. `Add Component` → **Teleport Interactable**, con cambios:
   - **Desactivar la teletransportación** (Allow Teleport off).
   - En **Options**, cambiar el **Score a -10** (para evitar conflicto con las otras zonas, ya que tiene menor puntaje).
3. Agregar la **superficie**: buscar **Surface** → aparece vertical, hay que rotarla a horizontal → rotación **X = 90** → referenciar.
4. `Add Component` → **Reticle Data Teleport** → cambiar el modo de **válido** a **inválido**.

---

### Paso 7 — Puntos de teletransportación (sobre cubos)
1. Clic derecho `> Create Empty`, llamarlo **`Teleport Point`**.
   - Posicionarlo sobre el cubo: **X = 3**, **Y = 1.025** (un poco más arriba), **Z = 3**.
2. `Add Component` → **Teleport Interactable**.
3. Superficie con un collider encima del cubo: dentro de **Teleport Point**, `Create Empty` llamado **`Collider`** → `Add Component` → **Box Collider** → tamaño **X = 1**, **Y = 0.02**, **Z = 1**.
4. Indicar el punto y la dirección: `Create Empty` llamado **`Target Point`**, dejarlo en el centro (0,0), y **rotarlo** para que apunte hacia la escalera (al teletransportarse, el jugador queda mirando ahí).
5. Volver al **Collider** → `Add Component` → **Collider Surface** → referenciar el collider.

**Indicadores visuales (hover / unhover):**
1. Crear un cubo: clic derecho `> 3D Object > Cube`, llamarlo **`Hover`** → escala **X = 1**, **Y = 0.02**, **Z = 1** → **eliminar su collider**.
2. Duplicar (`Ctrl + D`), llamarlo **`Unhover`** → escala más pequeña **X = 0.5**, **Z = 0.5** (Y igual) → asignarle el **material del piso** (buscar **Floor** en Material).
   - **Hover** se muestra cuando el rayo apunta al punto; **Unhover** cuando no. Se logra activando/desactivando estos Game Objects.

**Configurar el Teleport Point:**
1. Referenciar la **superficie**.
2. En **Options > Target** referenciar **Target Point** (indica el punto de teletransportación).
3. Activar el booleano de mantener la **dirección del objeto** (quedar mirando hacia la escalera) → **true**.
4. `Add Component` → **Reticle Data Teleport**.
5. Para los eventos hover/unhover: `Add Component` → **Interactable Unity Event Wrapper**.
   - Al referenciar un interactable (el Teleport Interactable), se tiene acceso a los eventos, entre ellos **hover** y **unhover**.
   - **When Hover** → 2 listeners:
     - **Hover** → `GameObject > SetActive` → **true** (activado).
     - **Unhover (objeto)** → `SetActive` → **false** (desactivado).
   - **When Unhover** → al revés: Hover **false**, Unhover **true**.
   - Estado inicial: **Hover desactivado**.

6. Repetir el mismo proceso para los otros **dos cubos** → quedan **3 puntos** de teletransportación.

### Paso 8 — Guardar y probar
1. `Ctrl + S`.
2. **En RV con controles:**
   - **Teletransportarse:** empujar la palanca del control hacia **arriba** y **soltar** → te teletransportás a la zona definida.
   - Subir al **segundo piso** por la escalera funciona.
   - **Girar:** mover la palanca a izquierda o derecha.
   - Apuntar al **vacío** → el rayo se pone **rojo** (no permitido).
3. **Con las manos:**
   - **Teletransportarse:** hacer el gesto de teletransportación y apuntar al punto → aparecés mirando hacia la escalera.
   - **Girar:** hacer el gesto y esperar a que salgan las **flechas** que indican el sentido.

---
