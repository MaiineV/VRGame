# Configuración del entorno — Crear un proyecto de VR y RM en Unity (Meta Quest)

> Cómo crear desde cero un proyecto de Realidad Virtual / Mixta para Meta Quest con el Meta XR All-in-One SDK.
> 🔗 Tutorial base: ¿Cómo crear Realidad Mixta y Virtual? Meta Quest 3, Pro, 2 — https://www.youtube.com/watch?v=S8o40kNDurw

## 1. Crear el proyecto en Unity

**Verificar el módulo de Android:** antes de crear el proyecto, en **Unity Hub > Installs** comprobar que la versión de Unity tenga el ícono de **Android**. Si no está, agregar el módulo con: **Android Build Support**, **OpenJDK**, **Android SDK** y **NDK Tools**.

**Crear el proyecto:** en Unity Hub, `Projects > New Project`, elegir la versión de Unity, plantilla **3D**, nombre y crear.

**Cambiar la plataforma a Android:** `File > Build Settings` → seleccionar **Android** → **Switch Platform**. Es esencial porque los Meta Quest funcionan sobre Android.

## 2. Importar el Meta XR All-in-One SDK

Es el componente central que da acceso a todas las funcionalidades de los Quest.
1. En la **Unity Asset Store** buscar **Meta XR All-in-One SDK**.
2. **Add to My Assets** y aceptar los términos.
3. Abrir en Unity desde **Package Manager > My Assets**.
4. Instalar el paquete y **reiniciar el editor** cuando se solicite.
5. Verificar en **Package Manager** (vista **In Project**) que estén todos los sub-paquetes.

## 3. Configurar el proyecto (Project Setup Tool)

El SDK incluye una herramienta de configuración automática: **`Oculus > Tools > Project Setup Tool`**.
1. Verificar que la pestaña activa sea **Android**.
2. **Fix All** para corregir automáticamente todas las configuraciones.
3. **Apply All** para aplicar los cambios.
4. Repetir el proceso en la pestaña de **Windows**.

**Configuración de renderizado:** en `Player Settings > Android > Other Settings > Rendering`, según este tutorial se quita **Vulkan** y se deja únicamente **OpenGL ES3**; luego volver al Project Setup Tool y aplicar de nuevo.
> ⚠️ Nota: en otro video (agarrar objetos) se indica lo contrario — cambiar de **OpenGL ES3 a Vulkan** si el casco se cierra al ejecutar. Es decir, si con OpenGL ES3 falla, probar Vulkan.

## 4. Agregar funcionalidades con Building Blocks

Desde **`Oculus > Tools > Building Blocks`**, arrastrar a la escena (detalle completo en `Building Blocks.md`):
- **Camera Rig** (incluye **OVR Camera Rig** y **OVR Manager**) — indispensable.
- **Pass Through** — para Realidad Mixta.
- **Hand Tracking** (configurar la frecuencia del tracking en **Alta**).
- **Synthetic Hands** — representación visual de las manos.
- **Grabbable Item** — objeto agarrable.

**Configurar el Grabbable Item:** referenciar las manos sintéticas con los componentes de interacción:
1. Mano izquierda (**Left Hand > Left Grab > Hand Grab Visual**) → referenciar **Synthetic Left Hand**.
2. Mano derecha (**Right Hand > Right Grab**) → referenciar **Synthetic Right Hand**.
3. Posicionar el objeto en un lugar accesible (p. ej. Y: 1.5, Z: 0.25).

## 5. Probar con Oculus / Quest Link (sin compilar)

*(Detalle en `Como probar y exportar el juego.md`.)*
1. Activar **Modo Desarrollador** desde la app Meta Quest del teléfono.
2. En Oculus para PC: `Settings > Beta` → activar *Pass Through over Meta Quest Link*.
3. Conectar el casco por **USB-C** (Quest Link) o **wifi** (Air Link); verificar en *Devices* que aparezca en verde.
4. Activar **Quest Link** en el casco y hacer **Play** en Unity.

## 6. De Realidad Virtual a Realidad Mixta

La transición es sencilla por la arquitectura modular: basta con agregar el **Building Block Pass Through**. El soporte se añade automáticamente al **Camera Rig**. Compatible con Quest 2, Pro y 3 (mejor calidad en **Quest 3**).

## 7. Compilar y desplegar (Build APK)

1. `File > Build Settings`.
2. Agregar la escena actual a la lista.
3. En **Run Device**, seleccionar el casco conectado (usar **Refresh** si no aparece).
4. **Build and Run**.
5. Crear una carpeta para el **APK** resultante y esperar a que termine. La app se instala y ejecuta automáticamente en el dispositivo.
