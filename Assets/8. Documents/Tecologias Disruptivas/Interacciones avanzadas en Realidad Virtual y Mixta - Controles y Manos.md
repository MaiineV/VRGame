# Interacciones avanzadas en Realidad Virtual y Mixta — Unity Meta Quest

> Tema: interacciones avanzadas en RV/RM usando **controles y manos simultáneamente** (modo multimodal) en Unity para Meta Quest.
> Fuente: serie *Unity Adventure* (YouTube).

---

## 1. Interacciones avanzadas en Realidad Virtual y Mixta: Controles y Manos simultáneamente

🔗 https://www.youtube.com/watch?v=wRamEpyIx9w

**Objetivo del video:** interactuar simultáneamente con los **controles** y las **manos** para crear interacciones avanzadas en experiencias de RV/RM.

**Idea clave (por qué es útil el modo multimodal):** combina las ventajas de ambos métodos: la **inmersión** que dan las manos y la **precisión + el feedback háptico** que dan los controles.

### Requisitos previos
- Proyecto ya configurado para RV/RM en Meta Quest. (Enlace de configuración en la descripción del video.)
- La funcionalidad de usar **manos y controles a la vez** se lanzó en la **versión 62** del paquete de interacciones de Meta.
  - Verificar en `Window > Package Manager` que esté instalada **como mínimo la versión 62**.

### Paso 0 — Importar los ejemplos del paquete de interacciones (opcional pero recomendado)
Sirven para ver cómo Meta implementa cada interacción.
1. En `Window > Package Manager` → `+` → **Add package by name**.
2. Escribir: **`com.meta.xr.sdk.interactions.ovr.samples`** y añadir.
3. Cuando termine, buscar el paquete, seleccionarlo y en **Samples** importar cada ejemplo (Import, Import, Import...).
4. Cerrar. En `Assets` se crea una carpeta **Samples** → abrir → versión 62 → **Escenas de ejemplo** (cada una implementa una funcionalidad de interacción).

**Probar el ejemplo de referencia:**
- Abrir la escena **`Concurrent Hands Controllers Examples`** → importar **TextMeshPro** si lo pide → cerrar → **Play**.
- En la demo: la mano derecha funciona con hand tracking y la izquierda con el control, **simultáneamente**. Al tomar un objeto (la honda/onda) con el control, automáticamente cambia a la pose de mano definida, y se pueden lanzar las esferas.

---

### Implementación paso a paso

### Paso 1 — Crear la escena
1. `File > New Scene > Basic`.
2. `Ctrl + S`, guardar en `Scenes` con el nombre **`Hands Controllers Interaction`**.
3. Eliminar la **Main Camera**.

### Paso 2 — Añadir los Building Blocks
`Oculus > Tools > Building Blocks`, y añadir:
1. **Camera Rig**
2. **Hand Tracking**
3. **Virtual Hands**
4. **Grab Interaction**

### Paso 3 — Configurar el Camera Rig (OVR Manager)
Seleccionar **Camera Rig** → Inspector → componente **OVR Manager**:
1. En **Controller Driven Hand Type** → cambiar a **Conform To Controller**.
2. Activar **Simultaneous Hands and Controllers** (manos y controles simultáneos).
3. Activar el **modo amplio (wide)** para el rastreo de manos.
4. Verificar que **Hand Tracking Support** esté en **Controllers and Hands**.
5. En la sección de manos y controles simultáneos, activar que se **lance automáticamente al iniciar la escena**.

### Paso 4 — Modificar el Hand Tracking (Show State = Always)
1. Abrir **Camera Rig** → **Tracking Space** → **Left Hand Anchor** → **Building Block Hand Tracking**.
   - En el Inspector, en **OVR Hand** → cambiar **Show State** a **Always**.
2. Repetir en **Right Hand Anchor** → **Building Block Hand Tracking** → **OVR Hand** → **Show State = Always**.

### Paso 5 — Configurar el bloque de interacción (mano izquierda)
Abrir **Building Block Interaction** → **Hand Tracking** (mano izquierda) → **Hand Interactor Left** → **Hand Grab Interactor** → **Visual**:
1. En **Hand Grab Visual** (Inspector): referenciar **Synthetic Hand** → arrastrar **Synthetic Hand Left**. Activar ese Game Object.
2. En **OVR Hand Data Source**, agregar **dos componentes**:
   - **Active State Tracker**
   - **OVR Controller In Hand Active State** → referenciar **OVR Controller** en su campo; en opciones, abrir **MonoBehaviour** y agregar **From OVR Hand Data Source**.
3. Verificar que el **tipo de mano** sea el correcto: en este caso **izquierda (Left)**.

### Paso 6 — Configurar el bloque de interacción (mano derecha)
Repetir el Paso 5 en **Right Hand** → **Hand Interactors** → **Hand Grab Interactor** → **Visual**:
1. En **Hand Grab Visual**: referenciar **Synthetic Right Hand**. Activar.
2. En **OVR Hand Data Source**: agregar **Active State Tracker** y **OVR Controller In Hand Active State**, referenciar, y agregar como opción **From OVR Hand Data Source**.
3. Verificar tipo de mano → cambiar a **Right (Hand R)**.

### Paso 7 — Agregar el prefab `OVR Controller Driven Hands`
1. En el buscador de la ventana **Project** escribir: **`OVR Controller Driven Hands`**.
2. Cambiar la búsqueda de **Asset** a **All**.
3. Agregar ese prefab **dentro del bloque de interacción**.
4. Abrir el prefab y, en los **controles sintéticos** de la mano **izquierda** y **derecha**, en el script cambiar **Show State** a **Always** (en ambos controles).

> Con esto termina la **primera parte**.

### Paso 8 — Primera prueba (antes de los controles)
1. Mover el cubo para poder interactuar: posición **X = 0**, **Y = 1.25**, **Z = 0.5**.
2. `Ctrl + S` y **Play**.
3. Resultado: funciona el modo multimodal (una mano con control y la otra con hand tracking). Se puede agarrar el cubo **con la mano**, pero **todavía no con el control** → falta implementar la interacción para los controles.

---

### Paso 9 — Agregar el interactor para los controles
1. En el buscador de **Project** buscar el prefab **`Hand Grab Interactor`**.
2. Abrir **OVR Controller Driven Hands** → **Left Hand** y **Right Hand** → **Hand Interactors**.
3. Agregar el prefab **Hand Grab Interactor** a la **izquierda** y a la **derecha**.

**Configurar el prefab en la mano izquierda** (Inspector de **Hand Grab Interactor**):
1. Habilitar **Hover On Zero**.
2. En **Hand Grab Api** → agregar componente **Controller Pinch Injector** → referenciar **Hand Grab Api** y el control (arrastrar **Left Hand**).
3. En **Visuals** → **Hand Grab Visual** → referenciar **Synthetic Hand** (arrastrar el prefab de arriba, mano izquierda) → activar.
4. En **Hand Interactor Left** → **Best Hover Interactor Group** → agregar el interactor (arrastrar **Hand Grab Interactor**).

**Configurar el prefab en la mano derecha** (mismo proceso):
1. Habilitar **Hover On Zero**.
2. **Hand Grab Api** → agregar **Controller Pinch Injector** → referenciar → control **Right Hand**.
3. **Visuals** → **Hand Grab Visual** → referenciar **Synthetic Hand** (mano derecha) → activar.
4. **Hand Interactors** → agregar el interactor **Hand Grab Interactor**.

### Paso 10 — Agregar el prefab `Controller Ans Velocity Calculator`
*(Velocity Calculator — para calcular la velocidad del control, p. ej. al lanzar objetos.)*
1. Abrir **Left Hand** → en el buscador de Project buscar **`Controller ... Velocity Calculator`**.
2. Agregarlo en la mano **izquierda** → referenciar el control (arrastrar **Left Hand**).
3. Agregarlo en la mano **derecha** → referenciar el control **Right Hand**.

### Paso 11 — Prueba final
1. `Ctrl + S` y **Play**.
2. Resultado: se interactúa con la **mano** (funciona) y con el **control** (al acercarse al objeto, el control desaparece y se usa la mano sintética).
3. Si el objeto tiene una **pose asignada** (como la honda del ejemplo), al agarrarlo con el control se muestra esa pose.

✅ Todo funciona en modo multimodal: manos + controles simultáneamente.

---
