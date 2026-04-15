# Technical Design Document — Pour Decisions VR

**Versión:** 0.1 (MVP)
**Engine:** Unity 6.2 + URP
**Target:** Meta Quest 2/3 (standalone), PC VR (OpenXR/SteamVR)
**Framerate objetivo:** 90 fps Quest 3 / 72 fps Quest 2 — **nunca debajo**
**Arquitectura base:** espejo de Proyect_T (Service Locator + SO-driven data + EventBus + UpdateService).

---

## 1. Principios de diseño técnico

1. **VR-first, todo lo demás después.** Cada decisión se valida contra framerate y confort. Si rompe 90 fps en Quest 3, no entra.
2. **Un solo pump de Update.** Cero `Update()` / `FixedUpdate()` / `LateUpdate()` en MonoBehaviours del gameplay. Todo pasa por `UpdateService` vía `IUpdateListener` / `IFixedUpdateListener`.
3. **Service Locator, no singletons.** Ningún sistema de gameplay accede a otro por referencia directa. Se resuelve por interfaz (`ServiceLocator.Get<IRecipeService>()`).
4. **Data = ScriptableObjects.** Ninguna constante de diseño en código. Recetas, ingredientes, clientes, noches: todos son SO.
5. **EventBus para desacople.** Cliente emite `OnDrinkAccepted`; Economía, Audio, VFX y UI reaccionan sin conocerse.
6. **Cero allocations en hot paths.** Pooling para VFX, líquidos, partículas de rotura, vasos, NPCs. Listas preasignadas. `NonAlloc` en physics queries.
7. **Estacionario y diegético.** Jugador fijo detrás de la barra; UI montada en el mundo (pizarra, caja registradora). Nada flota en HUD — previene motion sickness y reduce overdraw.

---

## 2. Stack técnico VR

| Componente | Elección | Justificación |
|---|---|---|
| **XR Plugin Management** | OpenXR + Oculus (ya en `Packages/`) | Cobertura Quest + SteamVR con una sola capa de input. |
| **XR Interaction Toolkit** | 3.x (XRI) | Grab, socket, direct interactor; integra con Input System. |
| **Render pipeline** | URP con Forward+ | Obligatorio para Quest. Vulkan backend. |
| **Input** | New Input System + `InputSystem_Actions.inputactions` (ya presente) | Alineado con Proyect_T (`IInputService`). |
| **Física** | PhysX (nativo Unity) | `Rigidbody` + `ArticulationBody` evaluado solo para botellas críticas. |
| **Líquidos** | Mesh dinámico (vertex anim shader) + proxy de volumen por `float` | Particles NO: demasiado caros en Quest. |
| **Destrucción** | Swap a prefab pre-fracturado (3-5 chunks) + pool | Nada de runtime mesh cutting. |
| **Audio** | AudioMixer + pool de `AudioSource` (patrón Proyect_T) | Grupos Master/SFX/Music/Voice/Ambient. |
| **Compresión texturas** | ASTC 6x6 Quest, Mipmaps ON, Read/Write OFF | Regla dura de Proyect_T aplicada. |

---

## 3. Arquitectura general

### 3.1. Layout de carpetas (idéntico a Proyect_T)

```
Assets/
├── 1. Scenes/
│   ├── Core/        (Boot, Loading)
│   └── Gameplay/    (Bar)
├── 2. Scripts/
│   ├── Core/
│   │   ├── Bootstrap/    GameBootstrap
│   │   └── Managers/     AudioManager, SceneLoadManager, BaseManager, IManager
│   ├── Data/
│   │   ├── SO/           IngredientSO, RecipeSO, CustomerProfileSO, NightConfigSO, BottleSO, GameConfigSO
│   │   └── Enums/        DrunkLevel, CustomerState, PourQuality, NightPhase, IngredientType
│   ├── Services/         ServiceLocator + 8 servicios (ver 4.)
│   ├── Gameplay/
│   │   ├── Interactions/ Bottle, Glass, Shaker, Tap, CashRegister, Breakable
│   │   ├── Liquid/       LiquidMix (struct), LiquidRenderer, PourDetector
│   │   ├── Customer/     CustomerEntity + HSM
│   │   └── Systems/      RecipeValidator, DrunkennessSimulator, OrderGenerator
│   ├── HSM/              (copiar de Proyect_T)
│   ├── EventBus/         (copiar de Proyect_T)
│   ├── UI/               BarBoardUI, CashRegisterUI, OrderBubble
│   └── Utilities/        MyLogger, Extensions, Pool, ListPool
├── 3. Scriptable Objects/
├── 4. Prefabs/
└── 5. Assets/ + 6. Audio/ + 7. Plugins/ + 8. Documents/
```

### 3.2. Flujo de arranque

```
Boot scene (persistente con DontDestroyOnLoad)
 └─ GameBootstrap.Awake()
     ├─ ServiceLocator.Initialize()
     ├─ RegisterServices()       [8 servicios — sección 4]
     └─ InitializeCriticalServices() [IUpdateService, IGameStateService, IDatabaseService]
 └─ GameBootstrap.Start()
     └─ SceneLoadManager.LoadWithLoading("Bar", "Loading")
```

`UpdateServiceObject` vive en `Bar` y bombea `MyUpdate/MyFixedUpdate/MyLateUpdate`. Fallback pump en `GameBootstrap.Update()` si no está (igual a Proyect_T).

---

## 4. Servicios (8, puros C#)

Todos implementan `IGameService`. Se registran en `GameBootstrap.RegisterServices()`. Cero MonoBehaviour.

| Servicio | Responsabilidad VR | Dependencias |
|---|---|---|
| `IUpdateService` | Distribuye ticks a `IUpdatable`. Sin esto, nada se mueve. | — |
| `IDatabaseService` | Carga todos los SOs desde `Resources` o Addressables al boot. Acceso por id/enum. | — |
| `IGameStateService` | HSM global: `Boot → NightIntro → Service → NightSummary → Shop (post-MVP)`. | IUpdateService |
| `IInputService` | Wrapper de XR Input: grip/trigger por mano, haptics (`SendHapticImpulse`), raycasts UI. | — |
| `INightService` | Timer de noche, cola de clientes (pool), asignación a asientos, condición de cierre. | IUpdateService, IDatabaseService |
| `IEconomyService` | Saldo, ingreso por venta, costo por daño, cierre de caja. Solo datos — UI reacciona a eventos. | — |
| `IRecipeService` | Compara `LiquidMix` vs `RecipeSO`, devuelve `PourQuality` (Perfect/Good/Bad/Failed) + score. | IDatabaseService |
| `IUIService` | Coordina paneles diegéticos (pizarra, caja). Suscrito a EventBus. | — |

**No hay `CustomerManager`, `GameManager`, `VFXManager`, `AudioManager` como servicios.** Customers viven en la escena (prefabs en pool controlados por `INightService`). Audio es MonoBehaviour (sección 5). VFX son pools autocontenidos disparados por eventos.

---

## 5. Managers MonoBehaviour (2, mínimos)

Igual a Proyect_T:

- **AudioManager** — `AudioMixer` + pool de `AudioSource`. API: `Play(AudioClipSO, Vector3 pos, float vol)`. Suscrito a `OnBottleBroken`, `OnDrinkServed`, `OnPour`, `OnCustomerAngry`, etc.
- **SceneLoadManager** — transiciones async Boot ↔ Loading ↔ Bar, con fade VR (pantalla negra diegética delante del headset, no post-process — evita sickness).

---

## 6. ScriptableObjects

| SO | Campos clave |
|---|---|
| `IngredientSO` | `id` (enum), `displayName`, `liquidColor`, `type` (Alcohol/Mixer/Garnish), `pourRateMlPerSec`, `unitCost` |
| `BottleSO` | `ingredient: IngredientSO`, `capacityMl`, `mesh`, `brokenPrefab`, `repairCost`, `grabPose` |
| `RecipeSO` | `id`, `displayName`, `steps: RecipeStep[]` (ingrediente + volumen min/max + orden opcional), `basePrice`, `tipMultiplier`, `drunknessPoints` |
| `CustomerProfileSO` | `id`, `meshVariant`, `patienceSec`, `possibleOrders: RecipeSO[]`, `drunkCurve: AnimationCurve`, `personality` (enum), `tipRange` |
| `NightConfigSO` | `durationSec`, `customerSpawns: CustomerSpawn[]` (tiempo + profile), `difficultyMod` |
| `GameConfigSO` | `startingMoney`, `damageCostMultiplier`, `hapticIntensities`, `pourTiltThresholdDeg` |

**Reglas duras (de Proyect_T):**
- Enums con valores explícitos espaciados (10, 20, 30…).
- Nunca renombrar/quitar campos de SO — rompe referencias de assets.

---

## 7. Mecánicas VR — diseño técnico

### 7.1. Agarre de botellas y vasos

- `XRGrabInteractable` con **attach transform** pre-definido (el jugador siempre agarra por el cuello). Un `grabPose` por objeto en el SO.
- `Rigidbody`: `interpolate=Interpolate`, `collisionDetectionMode=Continuous` para objetos críticos (botellas llenas), `Discrete` para vasos vacíos — **no todos continuos** (costo CPU).
- **Hand presence**: modelo de mano se esconde al agarrar (reemplazado por objeto); evita clipping y mejora inmersión.
- Haptics: pulso corto `0.2 amp / 0.05 s` al contacto con socket; `0.6 amp / 0.15 s` al éxito de receta.

### 7.2. Vertido (`PourDetector`)

Componente ligero en el cuello de cada botella.

```
IUpdatable.Tick(dt):
  if (grabbed && angleFromUpward > GameConfigSO.pourTiltThresholdDeg):
      volumeMl = pourRate * dt * tiltFactor(angle)
      raycast DOWN (NonAlloc, máscara "Glass") hasta 0.6m
      si impacta Glass:
          glass.Add(ingredientId, volumeMl)
          emit stream VFX (pooled)
          haptic pulse (intensidad∝tiltFactor)
      else:
          emit spill VFX (pooled) en punto raycast o piso
          EconomyService penalty (configurable)
```

- **Un solo raycast por botella volcada por frame**, nunca por partícula.
- Stream de líquido: quad con shader UV-scroll, NO particles. Se estira entre boca y punto de impacto.
- Se registra como `IFixedUpdateListener` solo mientras `angle > threshold` — auto-desregistro cuando se endereza.

### 7.3. `LiquidMix` y `LiquidRenderer`

`LiquidMix` es **struct** con dos buffers preasignados (`Span<byte> ids`, `Span<float> volumes`, máx 8 ingredientes). Cero GC.

`LiquidRenderer` en el vaso:
- Mesh cilíndrico con shader que toma `_FillAmount` (0–1) y `_LiquidColor` (blend computado de `LiquidMix`).
- Color final = suma ponderada por volumen (CPU, 1 vez cuando cambia la mezcla, no por frame).
- Wobble: vertex shader con sin/cos basado en `Rigidbody.velocity` del vaso (enviado por `MaterialPropertyBlock` — **no `material.SetColor`**, crea instancias).

### 7.4. Recetas y validación

Se dispara cuando el jugador coloca el vaso en el **socket "Serve"** de un cliente (`XRSocketInteractor`).

```csharp
IRecipeService.Validate(LiquidMix mix, RecipeSO target) -> PourQuality
  - chequea presencia de ingredientes requeridos
  - chequea volumen dentro de [min,max] por ingrediente
  - chequea orden si RecipeSO.enforceOrder (tracked en Glass.pourHistory)
  - score 0..1 -> Perfect/Good/Bad/Failed
```

No hay recetas hardcoded — todo desde `RecipeSO`.

### 7.5. Clientes (HSM)

`CustomerEntity` es MonoBehaviour delgado; toda la lógica vive en un HSM copiado de Proyect_T.

Estados: `Idle → Walking → Seated/Ordering → Waiting → Drinking → Satisfied/Drunk/Angry → Leaving`.

- **NavMeshAgent** opcional solo para `Walking`. Apagado al sentarse (ahorra CPU).
- Animaciones: Animator con **Optimize Game Objects** activado; huesos expuestos solo los necesarios (cabeza para LookAt).
- LOD: 2 niveles (full / low-poly sin rig facial) basados en distancia a barra.
- Paciencia: `IUpdatable` que decrementa `patienceSec`; al llegar a 0 → `Angry`, posible rotura de objeto, `OnCustomerAngry`.
- Borrachera: `drunkPoints` acumulan por cada trago servido (`RecipeSO.drunknessPoints`). Niveles discretos (`DrunkLevel` enum) disparan eventos y cambian animator layer (tambalear).

### 7.6. Destrucción de botellas

- Al detectar impacto con `impulse > threshold` (via `OnCollisionEnter` en `Breakable` — único MB con callback físico, aceptable porque es evento discreto) → swap al prefab roto del pool + despawn original.
- Prefab roto: 3–5 chunks pre-fracturados (Blender), Rigidbodies con `Sleep()` automático a <0.1 m/s.
- Evento `OnBottleBroken(cost)` → Economy descuenta, Audio reproduce, VFX spawn pool.
- **Cleanup**: chunks se devuelven al pool a los 5 s.

### 7.7. Caja registradora (fin de noche)

- Interacción diegética: el jugador aprieta un botón físico (`XRSimpleInteractable`) al final de la noche.
- Servicio `IEconomyService` ya tiene los totales acumulados por eventos — no recalcula nada.
- Impresión: ticket físico sale de la caja (animación + TMP sobre quad), jugador lo puede tomar.

---

## 8. EventBus — eventos del proyecto

Copiar sistema de Proyect_T. Eventos definidos como `readonly struct`:

```
OnNightStarted(NightConfigSO)
OnNightEnded(NightResult)
OnCustomerSpawned(CustomerEntity)
OnCustomerSeated(CustomerEntity, int seatIndex)
OnCustomerOrdered(CustomerEntity, RecipeSO)
OnDrinkServed(CustomerEntity, PourQuality, int price, int tip)
OnCustomerLeft(CustomerEntity, LeaveReason)
OnDrunkLevelChanged(CustomerEntity, DrunkLevel prev, DrunkLevel next)
OnBottleBroken(BottleSO, int repairCost)
OnPourStarted(IngredientSO)
OnPourEnded(IngredientSO, float volumeMl)
OnMoneyChanged(int balance, int delta)
```

Consumidores típicos: AudioManager, UI, VFX pools, IEconomyService.

---

## 9. Reglas de performance (obligatorias)

**Hot paths (copiadas de Proyect_T y endurecidas para VR):**
- ❌ Cero `Update/FixedUpdate/LateUpdate` en gameplay. Solo en `UpdateServiceObject` y `GameBootstrap` (fallback).
- ❌ Cero LINQ en runtime.
- ❌ Cero `foreach` en colecciones no-array / dinámicas.
- ❌ Cero `Instantiate/Destroy` → `PoolGeneric` para todo spawn recurrente (clientes, vasos, chunks, VFX, tickets).
- ❌ Cero allocations per frame. Verificar con Profiler (GC Alloc = 0 B en Play).
- ❌ Cero `GetComponent` en hot paths → cachear en `Awake`.
- ✅ Structs sobre clases donde aplique (`LiquidMix`, `PourEvent`).
- ✅ Enum/int keys sobre strings.
- ✅ `Physics.RaycastNonAlloc` con buffer preasignado por componente.
- ✅ `MaterialPropertyBlock` para cambios de color por instancia.

**Rendering (URP Mobile / Quest):**
- Single Pass Instanced obligatorio.
- **Static batching + GPU Instancing** en todo el mobiliario.
- Lightmaps horneados — **cero luces dinámicas en tiempo real** excepto máximo 1 (spot de barra) si cabe en budget.
- Sombras: baked only. Si hay dinámica, `Hard Shadows Only`, distancia ≤5 m.
- Textures: ASTC 6x6, Mipmaps ON, Read/Write OFF, compresión por plataforma.
- Atlas de materiales para botellas (un material compartido con GPU Instancing).
- Occlusion Culling activo en el bar.
- Light Probes para los clientes.
- Post-processing: **OFF** en Quest salvo Tonemapping. Nada de Bloom / SSAO / DOF.
- Skybox simple + fog baked en lightmap.

**VR-específico:**
- `Application.targetFrameRate` = 72/90 según device; `XRSettings.eyeTextureResolutionScale` ajustable en settings (default 1.0 Quest 3, 0.9 Quest 2).
- Fixed Foveated Rendering: nivel Medium en Quest.
- Motion-to-photon: zero heavy work en `LateUpdate` (cámara).
- Cámara fija al playspace del usuario (sentado/parado), **sin movimiento artificial** — cero locomotion.

**Audio:**
- OGG Vorbis, mono para SFX cercanos, estéreo solo para música.
- Pool de 16–24 `AudioSource` 3D, spatializer Oculus.
- Grupos: Master / SFX / Music / Voice / Ambient.

---

## 10. Modelo de datos en memoria

**Pools preasignados (alocados al boot):**
| Pool | Tamaño | Prefab |
|---|---|---|
| `CustomerPool` | 6 | CustomerEntity |
| `GlassPool` | 12 | Glass |
| `LiquidStreamPool` | 4 | stream quad |
| `SpillPool` | 8 | decal + VFX |
| `BrokenChunkPool` | 32 | chunks genéricos |
| `TipCoinPool` | 16 | moneda |
| `OrderBubblePool` | 6 | bubble UI |
| `AudioSourcePool` | 24 | AudioSource 3D |

**Buffers estáticos:**
- `Physics.RaycastNonAlloc` buffer: 4 slots por `PourDetector`.
- `LiquidMix` interno: 8 ingredientes máx (struct, stack-allocable).
- Lista de clientes activos: `List<CustomerEntity>` capacity=6.

---

## 11. Roadmap técnico (orden de implementación)

1. **Portar núcleo desde Proyect_T** (sesión 1): `ServiceLocator`, `IGameService`, `UpdateService` + `UpdateServiceObject` + `IUpdatable`, `EventBus`, `HSM`, `MyLogger`, `PoolGeneric`, `GameBootstrap`, `BaseManager/IManager`, `SceneLoadManager`, `AudioManager`.
2. **Setup escena Bar** (sesión 2): XR Rig fijo detrás de barra, colliders de superficie, lightmaps básicos, OpenXR configurado, scene load funcionando.
3. **Bottle + Glass + PourDetector + LiquidMix + LiquidRenderer** (sesiones 3–4): core feel VR. Validar 90 fps con 4 botellas en mano alternando.
4. **`IDatabaseService` + primeros SOs** (sesión 5): 3 ingredientes, 1 receta, 1 cliente.
5. **`IRecipeService` + socket "Serve"** (sesión 6): validación end-to-end con feedback háptico y audio.
6. **`CustomerEntity` + HSM + `INightService`** (sesión 7): 1 cliente spawneado, pide, espera, se va.
7. **`IEconomyService` + CashRegister diegética** (sesión 8): ciclo económico mínimo.
8. **UI diegética** (sesión 9): pizarra de pedidos + ticket final.
9. **Destrucción `Breakable`** (sesión 10): swap pre-fracturado + pool.
10. **Borrachera + efectos** (sesión 11): `DrunkenessSimulator`, animator layers.
11. **Expansión MVP** (sesión 12+): 2ª receta, 3 clientes, `NightConfigSO` completo, balance.

Cada sesión termina con **Profiler clean** (GC=0, 90 fps en Quest 3).

---

## 12. Checklist de validación por feature

Toda feature nueva pasa por:
- [ ] No introduce `Update/FixedUpdate/LateUpdate` en MB de gameplay.
- [ ] No genera GC alloc en Play (verificado con Profiler Deep Profile OFF).
- [ ] Si spawnea, usa pool.
- [ ] Data nueva → SO, no constantes en código.
- [ ] Comunicación cross-sistema → EventBus o ServiceLocator, nunca singleton directo.
- [ ] Funciona con Single Pass Instanced.
- [ ] Mantiene 90 fps en Quest 3 con carga peor-caso (6 clientes + 2 botellas volcándose + 1 rotura).
- [ ] Haptics y audio con feedback en todas las interacciones de manos.
- [ ] Sin motion sickness: nada se mueve bajo los pies del jugador; transiciones con fade diegético.

---

## 13. Terminología oficial

| Término | Significado | No usar |
|---|---|---|
| **Noche** | Sesión completa de gameplay (intro + servicio + cierre) | Nivel, ronda |
| **Servicio** | Fase activa de atención a clientes dentro de una noche | Turno, partida |
| **Trago** | Resultado de una receta servida | Cóctel (salvo UI) |
| **Pedido** | Solicitud de un cliente vinculada a una `RecipeSO` | Orden (ambiguo) |
| **Borrachera** | Mecánica de intoxicación progresiva por cliente | Alcoholismo, ebriedad |
| **Daño** | Costo económico por rotura | Destrucción (genérico) |
| **Servir** | Colocar vaso en el socket del cliente | Entregar |

---

## 14. Anexo — reglas heredadas de Proyect_T

- Convenciones de nombres: `PascalCase` clases, `_camelCase` privados, `PascalCaseSO` SOs, `I` prefix interfaces.
- Branching: `feature/<iniciales>/<nombre-corto>` desde `develop`. `main` solo releases.
- PR checklist: code review, sin debug logs, sin regresiones de perf, sin allocs en hot paths, sin tests rotos.
- Código en **inglés**, comunicación en **español**.
