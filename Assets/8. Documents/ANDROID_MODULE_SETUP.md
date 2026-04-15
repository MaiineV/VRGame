# Android Module Setup

Guía rápida para resolver errores tipo:
- `Android SDK not found. Please ensure that the path is set correctly in (Edit -> Preferences -> External Tools)...`
- `Build target directory for AndroidPlayer does not exist!` (desde `OVRConfig` / Meta XR SDK)

Ambos errores indican lo mismo: **el módulo Android Build Support de Unity no está instalado**.

---

## Instalar el módulo Android

1. Cerrá Unity completamente.
2. Abrí **Unity Hub**.
3. **Installs** (sidebar izquierdo) → buscá tu versión de Unity 6.x.
4. Click en el ⚙️ (gear) o ⋮ (3 puntos) al lado de la versión → **Add modules**.
5. Tildá **Android Build Support** y expandílo:
   - ✅ OpenJDK
   - ✅ Android SDK & NDK Tools
6. **Install** (pesa ~3 GB, tarda varios minutos).
7. Reabrí el proyecto.

---

## Verificación

La siguiente carpeta debe existir:

```
C:\Program Files\Unity\Hub\Editor\<tu-version>\Editor\Data\PlaybackEngines\AndroidPlayer\
```

Si existe → los errores desaparecen solos.
Si no existe → el módulo no se instaló bien, repetí los pasos.

---

## External Tools (Edit → Preferences → External Tools)

Una vez instalado el módulo, chequeá que en la sección Android estén tildados:

- ✅ **JDK Installed with Unity (recommended)**
- ✅ **Android SDK Tools Installed with Unity (recommended)**
- ✅ **Android NDK Installed with Unity (recommended)**
- ✅ **Gradle Installed with Unity (recommended)**

Si preferís usar un SDK externo (ej: el de Android Studio en `C:\Users\<usuario>\AppData\Local\Android\Sdk`), destildá el checkbox y apuntá manualmente.

---

## Switch Platform

Después del install, en `File → Build Profiles` (o Build Settings):

1. Seleccioná **Android**.
2. Click **Switch Platform**.
3. El warning de Android SDK desaparece.

Mientras el módulo no esté instalado, podés evitar el spam de consola manteniendo la plataforma en **PC, Mac & Linux Standalone**.
