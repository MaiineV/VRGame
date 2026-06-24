# Bake de Light Projection Maps (Cookies) en Blender

> Tema: hornear (*bake*) mapas de proyección de luz —llamados **cookies** en algunos motores— en **Blender**, para usarlos como proyección de sombras de una luz puntual en **Godot**. Incluye además el bake del mapa de emisión.
> Fuente: tutorial de YouTube (en inglés). Resumen en español manteniendo los términos técnicos.

---

## 1. Bake Light Projection Maps (Cookies) in Blender Tutorial

🔗 https://www.youtube.com/watch?v=g5kgBOBLBPE

**Problema que resuelve:** una luz puntual (point light) dentro de una luminaria (light fixture) proyecta sombras "feas". En lugar de que la malla de la luminaria proyecte la sombra en tiempo real, se le **prohíbe proyectar sombra** y se usa un **projection map** (cookie) horneado. Da sombras suaves y mejor rendimiento.

---

### Paso 1 — Preparar la escena y la esfera UV
1. Borrar el cubo por defecto y demás. **Shift + C** para centrar todo.
2. **Shift + A** → Add → **UV Sphere**.
   - Que sea de **poly alto** para reducir distorsión: **64 × 64**.
   - **Radio** lo bastante grande para cubrir la luz: ~**8 m** es suficiente.
3. **Tab** (modo edición) → **A** (seleccionar todo) → **F3** (búsqueda) → buscar **Flip Normals** → aplicar (normales hacia adentro).
4. **Shade Smooth** (suavizar).
5. **Tab** para salir del modo edición.

### Paso 2 — Material y textura de la esfera
1. Pestaña **Material** (esfera roja a cuadros) → **+ New** → nombrar p. ej. **`shadow map`**.
2. Clic en el punto junto a **Base Color** → **Image Texture** → **New** → nombrar p. ej. **`shadow texture`**.
   - No necesita alta resolución: **256 × 128** alcanza.

### Paso 3 — Traer la luminaria y la luz
1. Importar/traer la **luminaria (light fixture)**:
   - Si no está en Blender, **importarla**.
   - Si está en un .blend, se puede **Link** o **Append**.
     - **Append:** permite modificar la malla (p. ej. borrar la bombilla).
     - **Link:** mantiene los cambios hechos en el otro .blend, pero no se puede editar.
2. Ajustar la malla para que la **fuente de luz quede en el centro de la esfera**.
3. **Shift + A** → Light → **Point Light**.
   - Pestaña **Data** (bombilla) → subir el **Radius** (o arrastrar el círculo alrededor de la luz para redimensionar). Esto da las **sombras suaves**.

### Paso 4 — Primer bake de prueba (Shadow)
1. Pestaña **Render** (cámara) → **Render Engine = Cycles**.
2. Bajar a **Bake** → **Bake Type = Shadow**.
3. Seleccionar la **UV Sphere** creada → **Bake**.
4. En la pestaña **UV Editing** debería verse el projection map horneado (si no, seleccionarlo del desplegable).

### Paso 5 — Arreglar problemas
- **Patrones raros arriba/abajo** (por los triángulos del polo de la esfera UV): mover esos triángulos **fuera del borde de las UVs**.
- **Advertencia de "circular dependency":**
  1. Pestaña **Shading**, esfera seleccionada, en el material del objeto.
  2. **Desconectar** el nodo de imagen `shadow texture` (Blender intentaba renderizar y usar la misma imagen a la vez).
  3. ⚠️ Dejar el nodo de imagen **seleccionado** → el nodo seleccionado es donde Blender hornea.
- **Sombras poco oscuras** (por luz rebotada): en **Render** buscar **bounce** y poner **todos los valores en 0** (no queremos rebote de luz en la esfera).
- Limpiar la búsqueda → **Bake** de nuevo → debería verse mucho mejor.
- Si queda **ruidosa/manchada**: subir **Min Samples** en Render a ~**1000** para suavizar.
- **Image > Save As** (en UV Editing) para guardar la imagen.

### Paso 6 — Corregir orientación para Godot
Los projection maps salen **al revés** y **90° rotados** respecto al UV mapping de Blender. Para alinearlos:
1. En el lado **3D** de la ventana UV Editing: **Tab** (modo edición) → **A** (seleccionar todo).
2. En el lado **2D**: **A** otra vez → **S, Y, -1** (escalar -1 en Y → voltear).
3. En la **vista 3D**: **R, Z, -90** (rotar 90°).
4. **Bake** de nuevo → la sombra queda alineada. **Guardar** la imagen.

### Paso 7 — Configuración en Godot (render layers)
- Hay que **desactivar las sombras** en la malla, pero idealmente solo para la **luz interna** de la luminaria (para que otras luces sí puedan hacer que la luminaria proyecte sombra).
- Godot soporta **render layers**:
  1. Poner la malla de la luminaria en una **capa específica**, p. ej. **19**.
  2. En el **Shadow Caster Mask** de la luz, seleccionar **todo menos la 19**.
  - Así, esa luz no proyecta sombra desde su propia luminaria, pero otras luces (en otras capas) sí pueden hacerla proyectar sombra.
- Si las sombras quedan **muy suaves**: bajar el **Radius**, **rebake** y guardar.

### Paso 8 — Problema del back plate (placa trasera)
La placa trasera de la luminaria proyecta sombra sobre sí misma, matando su iluminación. Opciones:
1. **Quitar la placa** del bake de sombra → hacerla malla aparte que proyecte sombra dura, o dejarla sin sombra.
2. Pintar más **emisión** en la textura para iluminar las zonas en sombra.
3. **Hornear el mapa de emisión** (lo que se hace a continuación).

---

### Paso 9 — Bake del mapa de EMISIÓN (combined)
1. Nuevo archivo de Blender → **Append** la luminaria del .blend original.
2. Crear un material con los **mapas usados en el juego**: textura **diffuse/albedo** y **normal map**.
3. Posicionar la malla correctamente respecto a la luz (usar el mismo offset que antes).
4. **Shift + A** → **Point Light**.
   - La bombilla bloquea la luz: **Ctrl + Numpad +** expande la selección desde un vértice. *(Mejor mover la luz que borrar la bombilla.)*
   - Ajustar **color** de la luz (para que combine con el juego) y **radius** para el efecto deseado.
5. Pestaña **Shading**, objeto seleccionado → **Shift + A** → **Image Texture** → en ese nodo **New** → nombrar **`baked emission`**, ancho/alto = dimensiones de la textura original → **New Image**.
   - El nodo **no se conecta** a nada, pero dejarlo **seleccionado**.
6. Pestaña **Render** → **Render Engine = Cycles** → **Bake Type = Combined** (queremos sombra combinada con el diffuse/albedo) → **Bake**.
7. Revisar en **UV Editing**. El texturizado rellena las zonas entre islas UV para reducir *seams* al hacer mipmapping.
8. **Guardar** la imagen.

### Paso 10 — Retoque no destructivo en Krita
1. Llevar a Krita la **imagen horneada** + la **imagen de emisión original**.
2. Agregar la horneada como **capa** con modo **Addition**.
3. Seleccionar las partes de la bombilla y **borrarlas** de la capa horneada.
   - (Si el material era **double-sided**, la jaula sale demasiado brillante con artefactos de color → en la importación poner **texture compression = lossless**.)

**Workflow no destructivo en Krita:**
1. Nuevo archivo con las dimensiones de la textura.
2. Arrastrar la textura de emisión original → **Insert as new file layer** (que NO sea la que vas a sobrescribir).
3. Arrastrar la textura horneada → también **new file layer** → modo **Addition**.
4. Clic derecho en la capa → **Add Transparency Mask**: pintar de **negro** sobre la máscara oculta esos píxeles (grises = transparencia parcial).
5. **Exportar** como **PNG** al proyecto de Godot.
6. Guardar siempre el documento **.kra** para poder rebake o deshacer sin perder trabajo.

### Truco extra — fingir varias luces desde un punto
- Con la esfera UV centrada donde está la point light del juego, se pueden agregar **tantas luces como quieras en Blender antes de hornear** (p. ej. 4 luces) → en el juego parecen 4 luces saliendo de un solo punto.

---
