# Técnicas de optimización a usar

> Tema: técnicas de optimización para videojuegos, divididas en las propias del motor y las externas al motor.
> Fuente: imagen de estudio `Tecnicas de optimización a usar.png` (transcripción del texto).

---

## Técnicas de optimización a usar

### Propias del motor
- **Técnicas de oclusión** desde Unity (**Occlusion culling** y **Frustum culling**).
- **Desactivar o no el uso de mip maps** (calidad de texturas 2D para las mallas).
- **LOD (Level of Detail):** se refiere al nivel de detalle y densidad poligonal de mi malla 3D.
- **Compresión de archivos de audio.** Uso de **.OGG** o **.WAV**.
- **Compresión de archivos de imagen.** Evito el uso de **.png** si mi juego no tiene transparencias.
- **Uso de luces estáticas**, y gestión de luces dinámicas y configuración de elementos estáticos o dinámicos en pantalla.

### Externas al motor
- **Establecimiento de Mapas UV0** para reducir, reciclar y optimizar el uso de texturas.
- **Conteo de densidad poligonal** y evitar la duplicación o renderizado innecesario de caras/vértices/bordes.
- **Creación y aplicación de atlas de imágenes** para el escenario y elementos presentes en escena. Los tamaños de los atlas pueden llegar hasta **2k (2048px × 2048px)**.
- **Agrupar mallas de forma coherente.**

---
