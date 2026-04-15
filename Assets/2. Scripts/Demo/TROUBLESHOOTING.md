# Troubleshooting — Unity Environment Issues

Errores comunes del entorno Unity que **no bloquean la demo** pero aparecen en consola.
Ninguno es problema del código del juego.

---

## 1. `Build target directory for AndroidPlayer does not exist`

### Stack trace típico
```
UnityEditor.BuildPipeline:GetPlaybackEngineDirectory (UnityEditor.BuildTarget, UnityEditor.BuildOptions)
OVRConfig:GetAndroidSDKPathLocation
UnityADBToolSingleton:.cctor
OVRProjectSetupDeviceTasks:.cctor
```

### Causa
El **Meta XR SDK** corre un check al abrir el proyecto para ver dónde está el Android
SDK. Si no tenés instalado el módulo **Android Build Support** de Unity, ese directorio
no existe y loguea el warning.

### Impacto
- **Correr en Editor (PC): ninguno.** La demo funciona perfecto.
- **Build a Quest device: bloqueante.** No vas a poder hacer Build & Run al headset.

### Fix (solo si vas a buildear al Quest)
1. Cerrá Unity.
2. Abrí **Unity Hub**.
3. Ir a **Installs**.
4. En la versión de Unity que usás (ej. 6000.x), click en la **rueda dentada → Add Modules**.
5. Tildar:
   - ✅ **Android Build Support**
     - ✅ OpenJDK
     - ✅ Android SDK & NDK Tools
6. **Install**.
7. Reabrí Unity. El warning desaparece.

### Si no vas a buildear al device
Ignorá el warning. La demo en Editor anda igual.

---

## 2. `Unable to load the unmanaged library ... BurstCache\JIT\*.dll, error code 4551`

### Stack trace típico
```
Unexpected error in Burst compilation: System.AggregateException
---> System.DllNotFoundException: Unable to load the unmanaged library
  `C:\Users\<user>\Unity\VRGame\Library\BurstCache\JIT\<hash>.dll`, error code 4551
```

### Causa
**Error code 4551** en Windows = el antivirus (Windows Defender típicamente) está
bloqueando la carga del DLL que Burst genera en caliente, o el cache de Burst quedó
corrupto. Pasa seguido en Windows 11 con Defender agresivo.

### Impacto
- **Ninguno funcional.** Burst compila ahead-of-time para optimizar código marcado
  con `[BurstCompile]`. Si falla, Unity cae a **Mono JIT** — más lento pero funcional.
- La demo **corre igual**. Solo perdés perf de las jobs burst-compiled (que en este
  proyecto no estamos usando).

### Fix rápido (limpiar cache)
1. Cerrá Unity completamente.
2. Borrá la carpeta:
   ```
   C:\Users\Educabot\Unity\VRGame\Library\BurstCache\
   ```
   (Toda la carpeta `Library/` se puede borrar sin perder nada — Unity la regenera
   la próxima vez que abras el proyecto, aunque tarda unos minutos.)
3. Reabrí Unity. Recompila limpio.

### Fix definitivo (si reaparece)
Windows Defender está bloqueando la carpeta `Library`. Agregar exclusión:

1. **Windows Security** (buscar en el menú inicio).
2. **Virus & threat protection → Manage settings**.
3. Bajar hasta **Exclusions → Add or remove exclusions → Add an exclusion → Folder**.
4. Seleccionar:
   ```
   C:\Users\Educabot\Unity\VRGame\Library\
   ```
5. Confirmar.

**Es seguro** excluir `Library/` — es contenido generado por Unity, no tiene código
propio ni secretos. Se regenera solo.

### Si seguís queriendo desactivar Burst del todo
Durante el desarrollo podés apagar Burst para que ni siquiera lo intente compilar:

- **Unity → Jobs menu → Burst → Enable Compilation** → destildar.
- O **Jobs → Burst → Safety Checks** según necesites.

Para producción/build, **volver a activarlo**.

---

## Checklist antes de la demo

- [ ] Abre Unity, esperar que termine de compilar (puede tardar 1-5 min la primera vez)
- [ ] Consola: puede haber los 2 warnings de arriba — **ignorables**
- [ ] Consola: **no debe haber errores rojos de compilación** de C#
- [ ] Si hay errores rojos: revisar `UNITY_SETUP.md` (falta TMP Essentials, URP, etc.)
- [ ] Abrir `DemoScene.unity`, verificar que el GO `[DemoBootstrap]` tenga el component `DemoAutoSetup`
- [ ] **Play** → ver el panel IMGUI arriba a la izquierda
- [ ] Probar el flow: Begin Night → Sale → Abort Night → Acknowledge Summary
- [ ] Si todo anda: listo para demo
