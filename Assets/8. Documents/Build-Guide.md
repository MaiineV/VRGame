# Build Guide — Pour Decisions VR (Meta Quest 2/3)

**Version:** 1.0
**Fecha:** 2026-05-26
**Engine:** Unity 6 (6000.3.11f1) + URP 17.3.0
**Target principal:** Meta Quest 2 (ARM64, Android 10+)
**Output:** APK sideload (`production/builds/PourDecisions_<version>.apk`)

---

## 0. Diagnostico del proyecto (estado actual)

Snapshot leido de `ProjectSettings/ProjectSettings.asset`, `Packages/manifest.json` y `Assets/XR/Settings/OpenXRPackageSettings.asset` al 2026-05-26.

### Lo que ya esta bien configurado

| Item | Valor actual | Estado |
|---|---|---|
| Unity Editor | 6000.3.11f1 | OK |
| Render pipeline | URP 17.3.0 (`com.unity.render-pipelines.universal`) | OK |
| XR stack | OpenXR 1.16.1 + Meta XR SDK All 201.0.0 | OK |
| Scripting Backend Android | IL2CPP (`scriptingBackend.Android: 1`) | OK |
| Target Architectures | ARM64 unicamente (obligatorio Quest) | OK |
| Min SDK Version Android | API 29 (Android 10) | OK |
| Graphics APIs Android | Vulkan (15) + OpenGLES3 (0b), Auto OFF | OK |
| OpenXR Android features | MetaXRFeature, MetaXRFoveation, MetaXRSubsampledLayout, OculusTouchControllerProfile | OK |
| Build Scenes | `Boot.unity` (0) -> `Loading.unity` (1) -> `Bar.unity` (2) | OK |
| Scripting Define Symbols Android | `OVR_DISABLE_HAND_PINCH_BUTTON_MAPPING;USE_INPUT_SYSTEM_POSE_CONTROL;USE_STICK_CONTROL_THUMBSTICKS` | OK |

### Lo que hay que cambiar antes de la primera build "real"

| Item | Valor actual | Valor objetivo |
|---|---|---|
| Company Name | `DefaultCompany` | `Educabot` (o el legal real) |
| Product Name | `My project` | `Pour Decisions` |
| Application Identifier (Android) | `com.UnityTechnologies.com.unity.template.urpblank` | `com.educabot.pourdecisions` |
| Bundle Version | `0.1.0` | Subir a `0.1.1`/`0.2.0` cuando corresponda |
| Bundle Version Code | `1` | Incrementar +1 por cada build distribuida |
| Target SDK Android | `Automatic (highest)` | Mantener Automatic salvo que Meta exija 32+ para store |

> El `applicationIdentifier` actual viene del template URP — si lo dejas, dos juegos con el mismo bundle id colisionan en el casco.

---

## 1. Pre-requisitos (una sola vez)

### 1.1 En la PC de desarrollo

1. **Unity Hub** con el Editor `6000.3.11f1` instalado.
2. Modulo **Android Build Support** anadido al Editor:
   - Unity Hub -> Installs -> tres puntos del 6000.3.11f1 -> Add Modules
   - Tildar: `Android Build Support` -> dentro: `OpenJDK`, `Android SDK & NDK Tools`.
3. Verificar herramientas externas en Unity:
   - `Edit -> Preferences -> External Tools -> Android`
   - Tildar las tres opciones `Use Unity-installed` (JDK / Android SDK / NDK / Gradle).
4. **ADB** accesible. Si no esta en PATH, ubicacion por defecto:
   ```
   C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe
   ```
   Recomendado agregarlo al PATH del sistema.

### 1.2 En el Meta Quest 2

1. Crear "Organization" de desarrollador (una sola vez por cuenta Meta):
   - https://developer.oculus.com/manage/organizations/create/
2. Activar modo desarrollador en el casco:
   - App **Meta Quest** en el celular -> Tu casco -> Headset settings -> Developer Mode -> ON.
3. Cable USB-C con transferencia de datos (no solo carga).
4. Al conectar el casco al PC, ponerselo y aceptar el prompt:
   - **"Permitir depuracion USB desde este equipo"** -> marcar "Siempre permitir" -> Permitir.

### 1.3 Verificar conexion

```powershell
adb devices
```

Salida esperada:
```
List of devices attached
1WMHHxxxxxxxxx  device
```

Si dice `unauthorized` -> volver a aceptar el prompt en el casco.
Si no aparece nada -> probar otro cable USB, otro puerto.

---

## 2. Configuracion del proyecto (Player Settings)

`File -> Build Profiles` -> seleccionar `Android` -> `Switch Platform` si no esta activo.

> En Unity 6 la ventana **Build Profiles** reemplaza a Build Settings. La logica es la misma.

Luego `Edit -> Project Settings -> Player -> pestana Android (icono Android)`:

### 2.1 Icon & Identification

- **Company Name:** `Educabot`
- **Product Name:** `Pour Decisions`
- **Package Name:** `com.educabot.pourdecisions`
- **Version:** `0.1.0`
- **Bundle Version Code:** `1`
- **Minimum API Level:** `Android 10.0 (API 29)`
- **Target API Level:** `Automatic (highest installed)`

### 2.2 Configuration

- **Scripting Backend:** `IL2CPP` (obligatorio Quest)
- **Api Compatibility Level:** `.NET Standard 2.1`
- **Target Architectures:** SOLO `ARM64`. **Desmarcar** `ARMv7` (Quest no acepta builds 32-bit).
- **Install Location:** `Automatic`
- **Internet Access:** `Auto` (subir a `Require` si la build llama a APIs externas)
- **Write Permission:** `Internal`

### 2.3 Resolution and Presentation

- **Default Orientation:** `Landscape Left`
- **Use 32-bit Display Buffer:** ON
- **Disable Depth and Stencil:** OFF

### 2.4 Other Settings -> Rendering

- **Auto Graphics API:** OFF
- **Graphics APIs:** orden `Vulkan` -> `OpenGLES3`
- **Color Space:** `Linear`
- **Multithreaded Rendering:** ON
- **Static Batching / Dynamic Batching:** ON
- **GPU Skinning:** ON
- **Lightmap Encoding:** `Normal Quality`
- **Texture Compression Format Override (Android):** `ASTC`

### 2.5 Other Settings -> Optimization

- **Prebake Collision Meshes:** ON
- **Keep Loaded Shaders Alive:** ON
- **Optimize Mesh Data:** ON
- **Strip Engine Code:** ON
- **Managed Stripping Level:** `Low` (subir a `Medium` solo despues de validar)
- **Active Input Handling:** `Input System Package (New)`

---

## 3. XR Plug-in Management

`Edit -> Project Settings -> XR Plug-in Management -> pestana Android (icono Android)`:

- Provider: **OpenXR** tildado.
- (No tildar Oculus al mismo tiempo — son alternativas.)

### 3.1 OpenXR Settings -> Android

- **Render Mode:** `Single Pass Instanced` (1)
- **Depth Submission Mode:** `None`
- **Interaction Profiles:** debe estar **Oculus Touch Controller Profile** (ya esta `m_enabled: 1` en el repo).

### 3.2 OpenXR Feature Groups (Android)

Validar que esten activas:

- `Meta XR Feature` (Meta) — OK en repo
- `Meta XR Foveation` (Meta) — OK en repo
- `Meta XR Subsampled Layout` (Meta) — OK en repo
- `Oculus Touch Controller Profile` (Unity) — OK en repo

> El proyecto usa el path **Meta XR SDK** (`MetaXRFeature`) en lugar del path Unity puro (`MetaQuestFeature`). Son alternativas — no activar ambas o vas a tener crashes al iniciar.

---

## 4. Build Scenes

`File -> Build Profiles -> Scene List`. Orden requerido:

```
0: Assets/1. Scenes/Boot.unity       <- bootstrap (carga servicios)
1: Assets/1. Scenes/Loading.unity    <- pantalla de carga
2: Assets/1. Scenes/Bar.unity        <- escena de juego
```

> El orden importa: la 0 es la que arranca cuando el sistema lanza la APK.

Estado actual en `EditorBuildSettings.asset`: ya esta en este orden. OK.

---

## 5. Build via Editor (camino estandar)

### 5.1 Generar el APK con instalacion automatica

1. Conectar el Quest 2 por USB. Verificar `adb devices`.
2. `File -> Build Profiles`.
3. Confirmar:
   - Plataforma: Android (activa)
   - Run Device: tu Quest aparece en el dropdown.
   - Compression Method: `LZ4HC` (default OK).
4. Click **Build And Run**.
5. Cuando pida ruta, guardar en:
   ```
   production/builds/PourDecisions_v0.1.0.apk
   ```
   (Crear la carpeta `production/builds/` si no existe.)
6. La primera build IL2CPP tarda 5-15 minutos. Las siguientes son incrementales (1-3 min).

### 5.2 Solo generar el APK (sin instalar)

Mismo flujo pero click **Build** en vez de Build And Run. Util para mandar el APK a un tester por separado.

### 5.3 Encontrar el juego en el Quest

Ponerse el casco -> **Library** -> filtro arriba a la derecha -> **Unknown Sources** -> ahi aparece "Pour Decisions".

---

## 6. Build via script (automatizada)

Recomendado para iteracion rapida y CI. Crear `Assets/2. Scripts/Editor/BuildScript.cs`:

```csharp
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System;
using System.IO;

public static class BuildScript
{
    [MenuItem("Pour Decisions/Build/Quest APK (Development)")]
    public static void BuildQuestDev() => BuildQuest(development: true);

    [MenuItem("Pour Decisions/Build/Quest APK (Release)")]
    public static void BuildQuestRelease() => BuildQuest(development: false);

    private static void BuildQuest(bool development)
    {
        var version = PlayerSettings.bundleVersion;
        var suffix = development ? "dev" : "release";
        var outputDir = Path.Combine(Application.dataPath, "../production/builds");
        Directory.CreateDirectory(outputDir);

        var apkPath = Path.Combine(outputDir, $"PourDecisions_v{version}_{suffix}.apk");

        var scenes = new[]
        {
            "Assets/1. Scenes/Boot.unity",
            "Assets/1. Scenes/Loading.unity",
            "Assets/1. Scenes/Bar.unity",
        };

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = apkPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = development
                ? (BuildOptions.Development | BuildOptions.AllowDebugging)
                : BuildOptions.None,
        };

        EditorUserBuildSettings.buildAppBundle = false; // APK, no AAB
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);

        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
            Debug.Log($"[Build] OK -> {apkPath} ({summary.totalSize / 1024 / 1024} MB, {summary.totalTime})");
        else
            Debug.LogError($"[Build] FAIL -> {summary.result} ({summary.totalErrors} errors)");
    }
}
```

Uso desde el Editor:
- `Pour Decisions -> Build -> Quest APK (Development)` para builds con logs y profiler.
- `Pour Decisions -> Build -> Quest APK (Release)` para builds optimizadas.

Uso desde CLI (CI):

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe" `
  -batchmode -nographics -quit `
  -projectPath "C:\Users\Educabot\Unity\Projects\VRGame" `
  -buildTarget Android `
  -executeMethod BuildScript.BuildQuestRelease `
  -logFile production/builds/build.log
```

---

## 7. Instalar un APK ya generado (sin Build And Run)

```powershell
adb devices
adb install -r "production\builds\PourDecisions_v0.1.0.apk"
```

- `-r` = reinstalar conservando datos del save.
- Si cambiaste el `applicationIdentifier` desde la version anterior: primero `adb uninstall com.viejo.id`.
- Si la firma cambio (debug -> release o viceversa): mismo `uninstall` previo.

---

## 8. Debug en vivo

### 8.1 Logs filtrados de Unity

```powershell
adb logcat -s Unity ActivityManager CRASH
```

Captura `Debug.Log` + crashes nativos + lifecycle de la Activity.

### 8.2 Logs limpios desde un punto

```powershell
adb logcat -c
adb logcat -s Unity > production\builds\logs\session-2026-05-26.txt
```

Primer comando limpia el buffer, segundo guarda a archivo.

### 8.3 Profiler (Development build)

1. Build con **Development Build** + **Autoconnect Profiler** tildados.
2. Casco y PC en la misma red Wi-Fi.
3. `Window -> Analysis -> Profiler` -> dropdown `Android Player (...)` -> conectar.
4. Target: **<11.1ms/frame** para 90fps Quest 3, **<13.8ms/frame** para 72fps Quest 2.

---

## 9. Checklist pre-build (correr antes de cada release)

- [ ] `applicationIdentifier` correcto (`com.educabot.pourdecisions`)
- [ ] `bundleVersion` actualizado (semver)
- [ ] `AndroidBundleVersionCode` incrementado vs build anterior
- [ ] Solo `ARM64` en target architectures
- [ ] `IL2CPP` activo (no Mono)
- [ ] OpenXR provider activo en pestana Android
- [ ] Solo UN feature set XR activo (Meta XR Feature, no MetaQuestFeature simultaneo)
- [ ] Escenas en orden `Boot -> Loading -> Bar`
- [ ] `Color Space: Linear`
- [ ] `Texture Compression: ASTC`
- [ ] Quest conectado y reconocido por `adb devices`
- [ ] Cache del shader (`Library/ShaderCache`) presente — si no, primera build sera lenta

---

## 10. Errores comunes

| Sintoma | Causa probable | Fix |
|---|---|---|
| `adb: device unauthorized` | No aceptaste el prompt en el casco | Ponerse el casco, click "Permitir" |
| Pantalla negra al abrir | Boot.unity no esta en indice 0 | Reordenar Build Scenes |
| Crash inmediato | OpenXR sin feature de runtime | Activar Meta XR Feature en pestana Android |
| `INSTALL_FAILED_UPDATE_INCOMPATIBLE` | Firma del APK cambio | `adb uninstall com.educabot.pourdecisions` y reinstalar |
| Build falla en gradle | JDK/SDK/NDK incorrecto | `Preferences -> External Tools -> Use Unity-installed` |
| Performance pobre (<60fps) | Render mode `Multi Pass` en vez de `Single Pass Instanced` | Cambiar en OpenXR Settings Android |
| `ArmV7 not supported` | ARMv7 tildado | Desmarcar ARMv7, dejar solo ARM64 |
| Build pesa 500MB+ | Texturas sin compresion ASTC o assets sin usar | Setear ASTC, correr asset audit |
| Controladores no responden | Falta `OculusTouchControllerProfile` en OpenXR | Activarlo en pestana Android |
| Stuttering / dropped frames | Foveation off | Activar Meta XR Foveation (ya esta on por default en repo) |

---

## 11. Performance targets (recordatorio rapido)

Del TDD seccion 1: **VR-first, todo lo demas despues**. Si rompe estos numeros, no entra.

| Metrica | Quest 2 | Quest 3 |
|---|---|---|
| Framerate sostenido | 72 fps | 90 fps |
| Frame budget | 13.8 ms | 11.1 ms |
| Draw calls | <80 | <120 |
| Triangulos visibles | <500k | <750k |
| Texture memory | <512 MB | <1 GB |
| APK size objetivo | <150 MB | <200 MB |

---

## 12. Referencias

- TDD: `Assets/8. Documents/Technical.md`
- High Concept: `Assets/8. Documents/High Concept — Pour Decisions.md`
- GDD: `Assets/8. Documents/GDD — Pour Decisions.md` (stash actual al 2026-05-26)
- Build settings vivos: `ProjectSettings/ProjectSettings.asset`, `ProjectSettings/EditorBuildSettings.asset`
- XR config: `Assets/XR/Settings/OpenXRPackageSettings.asset`
- Meta Quest Developer Hub (alternativa a `adb` con UI): https://developer.oculus.com/meta-quest-developer-hub/
