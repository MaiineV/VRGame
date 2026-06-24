# Interfaces en Realidad Virtual y Mixta — Unity Meta Quest

> Tema: creación e interacción con interfaces de usuario (UI) en Realidad Virtual y Mixta usando Unity y Meta Quest (manos o controles).
> Fuente: serie *Unity Adventure* (YouTube).

---

## 1. ¿Cómo crear una interfaz para Realidad Virtual y Mixta? Unity Meta Quest

🔗 https://www.youtube.com/watch?v=6v3LSyfwtBY&list=PLSc07dYXbtBmgusnX4n4MYK0aJiHyYQSO&index=6

**Objetivo del video:** crear e interactuar con una interfaz en realidad virtual y mixta desde el Meta Quest, usando las manos o los controles.

### Punto de partida
- El proyecto ya está configurado para crear Realidad Virtual y Mixta para Meta Quest. (Si no lo tenés configurado, en la descripción del video hay un enlace a otro video que explica esa configuración en detalle.)

### Paso 1 — Crear una nueva escena
1. `File > New Scene > Basic`.
2. Guardar con `Ctrl + S`. La escena se llama **`UI Interaction`** y se guarda en la carpeta `Scenes`.
3. Eliminar la **Main Camera** de la escena.

### Paso 2 — Agregar el prefab base de interacciones de Meta
Para todas las interacciones de base con las manos y los controles se usa un prefab que viene en el paquete de interacciones de Meta.
1. En la ventana **Project**, en el buscador escribir: **`OVR Camera Rig Interaction`**.
2. Cambiar el filtro de búsqueda de **Assets** a **All**.
3. Arrastrar ese prefab a la **jerarquía**.
4. Si se abre el prefab, se ve que ya viene configurado con todas las interacciones necesarias para controles y manos: **Poke, Grab, Ray, Locomotion, Distance**, etc. Cerrar el prefab.

### Paso 3 — Crear el contenedor de la interfaz y el Canvas
1. Crear un objeto vacío: clic derecho `> Create Empty`, llamarlo **`UI`**.
   - Posición: **X = 0**, **Y = 1.25**, **Z = 0.5**.
2. Crear el Canvas: clic derecho sobre **UI** `> UI > Canvas`.

### Paso 4 — Habilitar la interacción con el Canvas (módulo automático de Meta)
Desde la **versión 62**, Meta incluyó en el paquete de interacciones un módulo que automatiza la configuración del Canvas (antes había que agregar componentes y configurarlos a mano).

1. Clic derecho sobre el **Canvas** `> Interaction SDK` → aparecen las *quick actions*: **Add Ray** y **Add Poke**.
   - **Ray** = interactuar **desde la distancia**.
   - **Poke** = interactuar **directamente** (tocando).

2. **Agregar Ray Interaction:**
   - Se abre una ventana que avisa que el Canvas debe estar en modo **World Space** → clic en **Fix**.
   - También pide agregar el módulo a la escena para poder interactuar con cualquier interfaz → clic en **Fix**.
   - En **Settings** se define si se interactúa con **manos**, **controles** o **ambos**. Si faltan componentes necesarios, se agregan automáticamente.
   - Al crear, se agrega un nuevo Game Object con todos los elementos necesarios para interactuar con ese Canvas.

3. **Agregar Poke Interaction:** repetir → clic derecho `> Interaction SDK > Add Poke Interaction` → crear.

> Desde este momento se puede interactuar con cualquier elemento que se agregue al Canvas.

### Paso 5 — Configurar el Canvas
- Posición: **X = 0**, **Y = 0**, **Z = 0**.
- **Width = 250**, **Height = 500**.
- **Scale = 0.002** en X e Y.
- `Shift + F` para enfocar el Canvas en la vista.

### Paso 6 — Contenedor ordenado de elementos (Vertical Layout Group)
1. Crear un objeto vacío `> Create Empty` llamado **`UI Elements`**.
2. En el **Rect Transform**, mantener presionado **Shift + Alt** y hacer clic en el anclaje, seleccionando el que ajusta automáticamente (stretch completo).
3. `Add Component` → buscar **Vertical Layout Group**:
   - **Padding Left = 20**, **Right = 20**.
   - **Spacing = 20**.
   - **Child Alignment = Middle Center**.
   - En **Control Child Size**: activar **Width**.

### Paso 7 — Construir la interfaz (elementos)
Agregar dentro de **UI Elements** (clic derecho `> UI > ...`):
1. **Texto** (`UI > Text - TextMeshPro`): escribir **"XR Adventure"**, en **negrita**, con **Auto Size**, **centrado**.
2. **Botón** (`UI > Button - TextMeshPro`): dejarlo como está.
3. **Dropdown** (`UI > Dropdown`).
4. **Slider** (`UI > Slider`).
5. **Toggle** (`UI > Toggle`).

> El código de ejemplo usado en el video está enlazado en la descripción.

### Paso 8 — Asignar funciones a cada elemento
Cada elemento se conecta a una función que está en el script de ejemplo. En cada elemento se arrastra el **Game Object que contiene el código** y se elige la función:
- **Botón** → función **`Generar objetos`** (en el demo).
- **Dropdown** → función **`Change Object`** (cambiar objeto).
- **Slider** → función **`Rotate Object`** (rotar objeto).
- **Toggle** → función **`Toggle Object`**.

### Paso 9 — Guardar y probar
1. `Ctrl + S` para guardar.
2. Probar en Realidad Virtual:
   - Interactuar con los **controles** → funciona.
   - Interactuar con las **manos** → también funciona.

### Notas / siguiente paso
- En el próximo video de la serie se aprende a crear interfaces más elaboradas (ver sección 2: Spatial UI).

---

## 2. UI Avanzada: Interfaz de Apple Vision Pro para Meta Quest — Spatial UI

🔗 https://www.youtube.com/watch?v=d6lqMpDwLvo

**Objetivo del video:** crear e interactuar con una **interfaz espacial** (Spatial UI, estilo Apple Vision Pro) para RV/RM. Continúa desde la escena del video anterior (interfaz básica con el prefab de interacciones de Meta).

### Paso 1 — Crear el contenedor y el Canvas
1. Clic derecho `> Create Empty`, llamarlo **`Spatial UI`**.
   - Posición: **X = 0**, **Y = 1.5**, **Z = 1**.
2. Clic derecho `> UI > Canvas`.
3. Habilitar la interacción (módulo de Meta), igual que el video anterior:
   - Clic derecho sobre el Canvas `> Interaction SDK > Add Ray Interaction` → **Fix** → Create. (Interacción a distancia.)
   - Clic derecho `> Interaction SDK > Add Poke Interaction` → Create. (Interacción directa.)
4. Configurar el Canvas:
   - Posición: **X = 0**, **Y = 0**, **Z = 0**.
   - **Width = 800**, **Height = 360**.
   - **Scale = 0.001** en X y en Y.

### Paso 2 — Fondo semitransparente (estilo Apple)
1. Dentro del Canvas, clic derecho `> Create Empty`, llamarlo **`Background`**.
   - En el Inspector, en el anclaje mantener **Ctrl + Shift**, clic sobre el anclaje y seleccionar **Stretch / Stretch**. Poner todos los valores en **0** (izquierda, arriba, derecha, abajo).
2. Sobre **Background**, clic derecho `> UI > Image`.
   - Estirar igual: **Ctrl + Shift** → stretch → todo en **0**.
   - En **Source Image** buscar **`Round Corners B Cut`** (si no aparece, clic en el icono para mostrar imágenes de paquetes; es una imagen del paquete de la **versión 63** de Meta — si falta, hay un paquete con todas las imágenes en la descripción del video).
   - **Color:** R = 110, G = 110, B = 110, **Alpha = 100** (semitransparente).
   - Renombrar la imagen como **`Panel`**.

### Paso 3 — Contenedor de contenido (Content)
1. Clic derecho `> Create Empty` dentro del Canvas, llamarlo **`Content`**.
   - **Ctrl + Shift** → stretch → todo en **0**.
2. `Add Component` → **Vertical Layout Group**:
   - **Padding Left = 20**, **Right = 20**.
   - **Child Alignment = Middle Center**.
   - En **Control Child Size**: activar **Width**.
3. El contenido se divide en dos partes. Dentro de **Content** crear:
   - **`Upper`** (parte superior): `Create Empty`, **Height = 150**.
   - **`Bottom`** (parte inferior): duplicar (`Ctrl + D`) el anterior y renombrarlo.

### Paso 4 — Parte superior (Upper): una imagen
1. Dentro de **Upper**, clic derecho `> UI > Image`.
   - Estirar: **Ctrl + Shift** → stretch → todo en **0**.
   - **Source Image:** **`Round Corners B 4`** (clic en el ojo/icono y seleccionar).
   - **Color:** verde → R = 0, G = 170, B = 110.

### Paso 5 — Parte inferior (Bottom): Scroll View horizontal
1. Dentro de **Bottom**, clic derecho `> UI > Scroll View`.
2. En el Inspector (componente Scroll Rect): **desactivar Vertical** (será solo horizontal).
3. Eliminar las **dos scrollbars** (vertical y horizontal).
4. **Anclaje del Scroll View:** **Ctrl** → **Middle Stretch**. Posición: izquierda = 0, Y = 0, derecha = 0, **Height = 150**. Fondo se deja igual.
5. **Viewport:** cambiar solo el padding/posición: **Top = 8.5** y **Bottom = 8.5**.
6. **Content (del Scroll View)** (donde van los botones):
   - **Anclaje:** **Ctrl** → **Left Stretch**. Posición: X = 0, Top = 0, Bottom = 0. **Pivot:** X = 0, Y = 0.5. Poner valores en 0.
   - `Add Component` → **Horizontal Layout Group**:
     - **Padding Left = 16**, **Right = 16**.
     - **Spacing = 12**.
     - **Child Alignment = Middle Center**.
     - **Control Child Size:** activar **Width** y **Height**; desactivar los dos **Child Force Expand**.
   - `Add Component` → **Content Size Fitter** → **Horizontal Fit = Preferred Size**.
   - `Add Component` → **Toggle Group** → activar **Allow Switch Off**.

### Paso 6 — Crear un botón Toggle
> Un toggle devuelve **true** al presionarse y **false** al volver a presionarse o al presionar otro botón.

1. Dentro del Content del Scroll View, clic derecho `> Create Empty`, llamarlo **`Button Toggle`**.
2. `Add Component` → **Toggle Deselect** (se referenciará después).
3. `Add Component` → **Layout Element** → activar **Width** y **Height** → **Width = 210**, **Height = 120**.

**Referencias visuales del botón (3 imágenes superpuestas):**
1. **Imagen de fondo:** clic derecho `> UI > Image`.
   - **Ctrl + Shift** → stretch → valores en **1** (izquierda 1, arriba 1, derecha 1, abajo 1).
   - **Source Image:** **`Round Corners B 2`**.
   - **Color:** naranja → R = 250, G = 150, B = 0.
2. **Borde:** duplicar (`Ctrl + D`), renombrar **`Border`**, subirlo en el orden, **color blanco**, posición **-2, -2, -2, -2**.
3. **Borde de selección:** duplicar (`Ctrl + D`), renombrar **`Selection Border`**, **color verde** (se muestra cuando el botón está seleccionado/true).

**Configurar el componente Toggle Deselect** (en **Button Toggle**):
- **Target Graphics:** referenciar **Border**.
- **Graphics:** referenciar **Selection Border**.
- **Group:** arrastrar **Content** (que tiene el Toggle Group).
- **Colores:**
  - **Normal Color:** copiar el hex de la imagen de fondo (naranja) → pegar → bajar **Alpha a 0**.
  - **Highlighted Color:** blanco.
  - **Pressed Color:** el mismo naranja copiado pero un poco más oscuro.
  - **Selected Color:** copiar el hex del **Selection Border** (verde) → pegar → bajar **Alpha a 0**.
- Abajo se puede asignar el **estado inicial** (true/false) y una **función** que se llama cuando cambia el estado del toggle (recibe true/false).

4. Duplicar el botón (`Ctrl + D`) varias veces → se ajustan solos al contenido.

> El resultado se puede convertir en **prefab** para usarlo como plantilla y personalizarlo.

### Paso 7 — Personalizar (con el paquete de materiales del video)
1. **Imagen superior:** en el paquete `Spatial UI` agregar **Upper Image** → arrastrar como Source Image → color blanco.
2. **Botones:** dejar solo uno para duplicarlo; cambiar su **Image** (arrastrar la del paquete) y el color.
3. **Script de demo:** agregar al objeto `Spatial UI` el script **`Advanced UI Interaction Demo`** (buscar `UI Interaction Demo Advance`).
   - En el **Button Toggle**, en **On Value Changed**, llamar una función del script: arrastrar **Spatial UI** → función **Advanced UI Interaction Demo > Display Object** (muestra un objeto 3D al tocar el botón).
4. Duplicar el botón (`Ctrl + D`).

### Paso 8 — Construir y probar
1. Construir la app para RV o RM.
2. Para **Realidad Mixta** se puede configurar la escena agregando el **Building Block** correspondiente (hay un video del canal con todos los detalles).
3. Resultado: la interfaz espacial funciona; se interactúa con manos y controles, directamente o desde lejos.

---
