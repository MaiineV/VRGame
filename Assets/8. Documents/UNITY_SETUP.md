# Pour Decisions — Unity Setup Guide

Guía end-to-end para dejar el proyecto corriendo en Quest 2/3 desde cero.
Código ya está. Esto es **todo lo que hay que hacer en el editor**.

---

## 0. Pre-requisitos del proyecto

### Unity version
- Unity 6.x (usa `Rigidbody.linearVelocity`, `FindFirstObjectByType`).

### Packages (Package Manager)
- **XR Plugin Management**
- **Oculus XR Plugin** (o Meta XR SDK)
- **XR Interaction Toolkit** (opcional si querés helpers; el proyecto usa grab/poke propios)
- **Universal RP** (URP)
- **TextMeshPro** (Window → TextMeshPro → Import TMP Essentials)

### Player Settings
- **Edit → Project Settings → Player → Android**
  - Minimum API Level: **Android 10 (API 29)**
  - Target API Level: Automatic
  - Scripting Backend: **IL2CPP**
  - Target Architecture: **ARM64** (destildá ARMv7)
  - Color Space: **Linear**
  - Graphics APIs: solo **Vulkan** (o GLES3 como fallback)
  - Multithreaded Rendering: **ON**
  - Static/Dynamic Batching: OFF (SRP Batcher se encarga)

### XR Plug-in Management
- **Edit → Project Settings → XR Plug-in Management → Android**
  - Activar **Oculus**
- **Oculus settings**: Stereo Rendering Mode → **Multiview**

### URP Asset
- Crear URP Asset + Renderer si no existe.
- **Graphics → Scriptable Render Pipeline Settings**: asignar el URP Asset.
- En el URP Asset:
  - HDR: **OFF**
  - MSAA: **4x** (Quest 2 tolera; si hay drops, bajar a 2x)
  - Shadows: **Soft OFF**, distance baja (≤15m)
  - Post-processing: evitar si no es imprescindible

---

## 1. Estructura de escenas

Se necesitan **2 escenas** en Build Settings (**File → Build Settings**):

1. `Loading` (index 0) — escena mínima con un Canvas de "Loading..."
2. `Bar` (index 1) — la escena de juego

Ambas deben estar en Build Settings con esos nombres exactos (los usa `GameBootstrap`).

### Escena `Bootstrap` (opcional pero recomendada)
Podés tener una tercer escena `Bootstrap` que solo contenga el GameObject `GameBootstrap` y se cargue primero, o meter `GameBootstrap` directo en `Bar` — funciona igual porque hace `DontDestroyOnLoad`.

---

## 2. GameObject raíz: `GameBootstrap`

Crear en la primera escena un GameObject vacío:

**GameObject: `[GameBootstrap]`**
- Component: `GameBootstrap` (Core namespace)
  - First Scene Name: `Bar`
  - Loading Scene Name: `Loading`
  - Skip If Already In Target Scene: ✔
- Component: `UpdateServiceObject` (Services.UpdateService)
  - Este es el "pump" que llama MyUpdate/MyFixedUpdate/MyLateUpdate en los listeners.

Si no ponés `UpdateServiceObject`, el bootstrap usa fallback y te tira warning — preferible agregarlo.

---

## 3. ScriptableObjects (Resources)

Todos los SOs críticos se cargan por `Resources.Load`. Crear carpeta:

```
Assets/Resources/Database/
```

### 3.1 `SfxDatabase` (Resources/Database/SfxDatabase.asset)
- **Create → Pour Decisions → Audio → SFX Database**
- Por cada SfxId, asignar:
  - Clip (AudioClip)
  - Volume (0.8 default)
  - Pitch Min/Max (0.95 / 1.05)
  - Spatial Blend (1 = 3D, 0 = 2D)
  - Min Retrigger Interval (0.05s default, anti-spam)
  - Loop (solo **PourLoop** en true)

**IDs a poblar:**
| SfxId | Uso | Loop | Spatial |
|---|---|---|---|
| PourLoop (10) | Stream de bebida | ✔ | 1 |
| GlassBreak (20) | Vaso roto | — | 1 |
| BottleBreak (21) | Botella rota | — | 1 |
| CashSale (30) | Venta exitosa | — | 1 |
| CashExpense (31) | Gasto | — | 1 |
| CustomerServed (40) | Cliente servido | — | 1 |
| CustomerLeft (41) | Cliente se fue | — | 1 |
| NightStart (50) | Inicio de noche | — | 0 |
| NightEnd (51) | Fin de noche | — | 0 |
| ButtonPress (60) | Poke button | — | 1 |

### 3.2 `RecipeDatabase`
- **Create → Pour Decisions → Data → Recipe Database**
- Lista de RecipeSO (cada receta con RecipeId, BasePrice, ingredientes).

### 3.3 `NightConfigSO`
- **Create → Pour Decisions → Data → Night Config**
- Duración, spawn rate de clientes, mix de recetas permitidas.
- Se asigna en el Inspector del `NightClipboard` (field `_config`).

### 3.4 (Opcional) Ejecutar `MvpContentBootstrap`
- En el editor: **Tools → Pour Decisions → Bootstrap MVP Content**
- Auto-genera SfxDatabase + recetas seed.

---

## 4. Rig VR (jugador)

### GameObject: `XR Origin`
Usar **Oculus → Building Blocks** o crear manual:

```
XR Origin
├── Camera Offset
│   ├── Main Camera (Camera, TrackedPoseDriver → Head)
│   ├── LeftHandAnchor (TrackedPoseDriver → Left Hand)
│   │   ├── HandModel_L (mesh)
│   │   └── GrabPoint (empty, con PokeFinger si es mano de poke)
│   └── RightHandAnchor
│       ├── HandModel_R
│       └── GrabPoint
```

### Hand components
- **Una mano** (ej. derecha) — grab: usar OVRGrabber o tu sistema → invoca `GrabBridge.SetHeld(true/false)` en el objeto agarrado.
- **Otra mano** (izquierda) — poke: esfera Collider (**is Trigger: ON**, radius ~0.015m) en la yema del índice. Tag: `Finger` (opcional).

---

## 5. Objetos físicos del bar

### 5.1 Glass prefab
```
Glass (Rigidbody, mass 0.3, drag 0.5)
├── Collider (Capsule o Mesh convex)
├── GrabBridge           ← expone IsHeld + eventos Grabbed/Released
├── Breakable            ← SfxId: GlassBreak; umbral de impacto
├── LiquidRenderer       ← MaterialPropertyBlock para shader líquido
│   └── LiquidMesh (MeshRenderer con shader "PourDecisions/Liquid")
└── LiquidWobble         ← requiere Rigidbody (se auto-añade)
    - Responsiveness: 8
    - Idle Threshold: 0.02
    - Idle Frames To Stop: 30
```

**Liquid mesh**: cilindro de 12-16 rings verticales (low poly). El shader usa `_WobbleVelocity` que `LiquidWobble` escribe cada frame desde `linearVelocity.xz`.

### 5.2 Bottle prefab
Igual que Glass pero sin LiquidRenderer/Wobble. Tiene `PourDetector` + `Breakable` (SfxId: BottleBreak).

### 5.3 CashRegister
```
CashRegister (GameObject)
└── CashRegister.cs      ← se subscribe a SaleRegistered/ExpenseRegistered
    → emite CashSale / CashExpense automáticamente
```

### 5.4 Breakable pool sources
Cualquier objeto rompible se registra con `IBreakablePoolService` automáticamente al entrar escena si tiene `Breakable`.

---

## 6. NightClipboard (UI diegética)

Prefab del **clipboard físico**:

```
NightClipboard (Rigidbody, mass 0.2)
├── ClipboardMesh (MeshRenderer)
├── Collider (Box, para agarrarlo)
├── GrabBridge
├── NightClipboard.cs
├── IdleGroup (GameObject) — visible solo en Idle
│   ├── TMP_Text "Night N"            → _idleNightNumber
│   ├── TMP_Text "Best: $XXXX"        → _idleBestEarnings
│   ├── TMP_Text "Cash: $XXXX"        → _idleCash
│   └── PokeButton "START NIGHT"      → _startButton
├── RunningGroup — visible solo NightRunning
│   └── PokeButton "ABORT"            → _abortButton
└── SummaryGroup — visible solo NightSummary
    ├── TMP_Text "Cash $X"            → _summaryCash
    ├── TMP_Text "Sales: N"           → _summarySales
    ├── TMP_Text "Failed: N"          → _summaryFailed
    ├── TMP_Text "Expenses -$X"       → _summaryExpenses
    ├── TMP_Text "Earnings +$X"       → _summaryNightlyEarnings
    └── PokeButton "CONTINUE"         → _continueButton
```

En el Inspector de `NightClipboard`:
- Config: asignar el `NightConfigSO`
- Grab: arrastrar el `GrabBridge` del mismo GO
- Los 3 groups (GameObject refs)
- Los 3 botones (PokeButton refs)
- Los 8 TMP_Text refs

### PokeButton setup
```
Button (MeshRenderer con quad/cube)
├── Collider (Box, Is Trigger: ON)  ← zona de detección del dedo
├── PokeButton.cs
│   - Press SFX: ButtonPress
│   - Debounce: 0.25s
└── Label_Child (TMP_Text en world space)
```

---

## 7. Orden de layers / tags

Tags a crear:
- `Finger` (yema del índice)
- `Breakable` (opcional, para filtros de colisión)

Layers sugeridas:
- `Interactable` (vaso, botella, clipboard)
- `Hand` (modelos de mano)
- `Environment` (barra, piso)

En **Physics Settings** → matriz de colisión:
- Hand ↔ Hand: OFF
- Hand ↔ Interactable: ON (para agarrar)

---

## 8. Audio Listener

- El **AudioListener** va en la **Main Camera** del XR Origin.
- Borrar cualquier otro AudioListener de la escena (warning si hay 2+).
- `AudioService` crea sus propias AudioSources bajo `[AudioService]` GameObject con `DontDestroyOnLoad`.

---

## 9. Build & Run (Quest)

1. **Edit → Project Settings → Player → Android → Other Settings**
   - Package Name: `com.tucompania.pourdecisions`
2. Conectar Quest vía USB, habilitar **Developer Mode** en la app Meta Quest.
3. **File → Build Settings → Android** → Switch Platform.
4. Run Device: tu Quest.
5. **Build and Run**.

### Test de persistencia
- Jugá una noche completa, llegá al summary, presioná CONTINUE.
- Cerrá la app (botón home de Quest).
- Volvé a abrir → el cash y el contador de noches deben persistir.
- Save vive en:
  - PC: `%USERPROFILE%\AppData\LocalLow\<Company>\<Product>\save.json`
  - Quest: `/sdcard/Android/data/<bundle>/files/save.json`

---

## 10. Checklist de verificación pre-build

- [ ] `GameBootstrap` + `UpdateServiceObject` en escena inicial
- [ ] Bar y Loading en Build Settings
- [ ] `Resources/Database/SfxDatabase.asset` con clips asignados
- [ ] `RecipeDatabase` + `NightConfigSO` creados
- [ ] XR Origin con 2 manos (grab + poke)
- [ ] Glass prefab con GrabBridge + LiquidRenderer + LiquidWobble + Breakable
- [ ] NightClipboard con 3 groups, 3 botones, 8 TMP refs, config asignada
- [ ] CashRegister en escena
- [ ] Main Camera con único AudioListener
- [ ] Color Space: Linear
- [ ] ARM64 + IL2CPP + Vulkan
- [ ] Stereo Rendering: Multiview

---

## 11. Troubleshooting

| Síntoma | Causa probable |
|---|---|
| "ServiceLocator.Get failed" | Falta registrar un servicio en GameBootstrap o el orden de registro viola una dependencia de ctor |
| SFX no sonando | SfxDatabase no asignó clip para ese ID, o AudioListener ausente |
| Clipboard no reacciona | GrabBridge no está invocándose al agarrar (revisar integración con grabber) |
| Wobble congelado | GrabBridge no seteado en LiquidWobble o Rigidbody kinematic |
| Save no persiste | Revisar logs de `SaveService` — permisos en persistentDataPath |
| FPS bajo en Quest 2 | MSAA a 2x, desactivar shadows, perfilar con OVR Metrics Tool |
