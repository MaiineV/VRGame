# Práctica de Baking de luces sobre UV en Blender (videos)

> Tema: práctica grabada del proceso de **bakear luces sobre mapas UV en Blender 4.3.2**, sobre una escena de habitación ("Scenario") con atlas de texturas, una luz Spot y un cubo rojo.
> Fuente: videos propios `Baking2.mp4`, `Bakeado sencillo sobre UV1.mp4` y `Baking parte final UV1 Y UV2.mp4`.
>
> ⚠️ **Nota:** estos resúmenes están **reconstruidos a partir de fotogramas del video** (lo que se ve en pantalla). NO incluyen la narración hablada, porque el audio no se puede transcribir automáticamente. Sirven como guía visual del flujo de trabajo; complementan la teoría de [Tipos de Luces en videojuegos] y [Bake de Light Projection Maps (Cookies) en Blender].

**Escena común a los 3 videos:**
- Blender 4.3.2, archivo `untitled.blend`.
- Objeto **`Scenario`**: una habitación (planos de piso y paredes) con un material **`Material.001`** cuyo Base Color es la textura/atlas **`Suelo y paredes.png`** (tiles de madera, mármol y piedra).
- Objeto **`Object`**: un **cubo rojo** con un material propio (Base Color rojo liso).
- Una luz **`Spot`** colgando del techo.

---

## 1. Bakeado sencillo sobre UV1 (`Bakeado sencillo sobre UV1.mp4`)

Flujo visual observado (bake directo sobre el UV existente / atlas = UV1):

1. **Punto de partida:** la habitación `Scenario` en Object Mode, con el material y el atlas `Suelo y paredes.png`. El cubo rojo en el centro bajo la luz Spot.
2. Se revisa el **material del cubo** (`Material`): Principled BSDF con Base Color rojo, Metallic 0, Roughness 0.5.
3. Se pasa al workspace **UV Editing** en **Edit Mode** sobre `Scenario`.
4. Se ven/ajustan las **UVs de la habitación sobre el atlas** `Suelo y paredes.png` (cada cara cae sobre los tiles del atlas — madera para el piso, mármol/piedra para las paredes). Se mueven (Move) las islas UV para ubicarlas en el atlas.
5. Se configura el **Bake** (pestaña Render → Bake):
   - **Bake Type = Combined.**
   - **Influence:** Lighting Direct + Indirect; Contributions Diffuse, Glossy, Transmission, Emit.
   - **Output:** Target = **Image Textures**, **Clear Image** activado.
   - Margin Type = Adjacent Faces, Size 16 px.
6. Se ejecuta **Bake** → "Rendering Done". El resultado muestra la **luz horneada en la textura**: el círculo de luz del Spot sobre el piso y la sombra bajo el cubo.
7. El resultado se guarda como una **nueva textura** llamada **`Suelo y paredesConLuz.png`** ("con luz"), que pasa a ser el Base Color del material → la habitación ya muestra la iluminación bakeada en su textura.

> **Idea del video:** es la forma "sencilla" — se hornea la luz directamente sobre el UV/atlas existente (UV1). Funciona, pero como el atlas es compartido entre varias caras, la luz queda "pegada" a los tiles compartidos (limitación que se resuelve usando un UV2 dedicado, ver video 3).

---

## 2. Baking2 (`Baking2.mp4`)

Flujo visual observado (preparación de un segundo canal UV — UV2 — para el bake):

1. Habitación `Scenario` en **UV Editing / Edit Mode**, con el atlas `Suelo y paredes.png`.
2. Se revisa el **material** `Material.001`: Principled BSDF, Base Color = `Suelo y paredes.png` (Color Space sRGB, Repeat, Single Image), Metallic 0, Roughness 1.
3. Se hace un **Bake Combined** de prueba (Render → Bake, Target Image Textures, Clear Image) → el resultado sale **muy oscuro** (bake sobre el canal/atlas no dedicado).
4. Se revisa el resultado en Object Mode (se ve la luz del Spot sobre el piso y la cúpula de luz en la pared).
5. Se va al panel **UV Maps** del objeto: además de **`UVMap`** (UV1) se **agrega un segundo canal `UV2`**.
6. Con todo el mesh seleccionado, se usa **`Smart UV Project`** (menú UV) para generar automáticamente el layout del **UV2** (sin solapamiento, ideal para lightmap).
7. En el editor UV se crea una **imagen nueva** (menú `Image > New`): nombre **`UV2`**, **512 × 512 px**, Alpha activado, Generated Type = Blank. Esta imagen será el destino del bake de luz.

> **Idea del video:** preparar el **UV2** (canal exclusivo para la luz) y su imagen destino, porque el UV1/atlas no refleja bien los efectos de luz (concuerda con la teoría: "los UV1 no reflejan la luz, los UV2 sí; deben crearse manualmente").

---

## 3. Baking parte final UV1 y UV2 (`Baking parte final UV1 Y UV2.mp4`)

Flujo visual observado (bake final de calidad sobre UV2 y uso en el shader):

1. Habitación en **Edit Mode**, con UV2 ya generado (Smart UV Project).
2. Se crea la **imagen destino definitiva**: `Image > New`, **1024 × 1024 px**, Alpha, Blank (mayor resolución que el de prueba de 512).
3. Se selecciona el canal **`UV2`** como activo en el editor UV (se ve "UV2" en la barra del editor).
4. Se suben los **samples de render** para mejor calidad:
   - **Sampling > Viewport:** Noise Threshold 0.1, Max Samples 1024, Min 0.
   - **Sampling > Render:** Noise Threshold 0.01, **Max Samples 4096**, Min 0, Time Limit 0 s.
   - **Denoise** activado.
5. Se ejecuta el **Bake (Combined)** sobre la imagen del UV2 → se ve el lightmap horneado en el editor UV (los paneles del UV2 con la luz) y la habitación iluminada con la cúpula de luz y el cubo rojo.
6. Se va al workspace **Shading**: en el árbol de nodos del `Material.001` está el nodo de imagen `Suelo y paredes.png` → Principled BSDF → Material Output, y un **segundo nodo de imagen con el bake del UV2** (listo para combinarse/usarse como mapa de luz).
7. Resultado final: la habitación con la **iluminación bakeada en el UV2 de 1024px**, lista para llevar al motor.

> **Idea del video:** es el cierre del flujo correcto — bake de luz de alta calidad sobre un **UV2 dedicado de 1024×1024** y su integración en el material, en vez de "ensuciar" el atlas compartido (UV1).

---
