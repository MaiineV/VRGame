# Technical Design Document — Pour Decisions VR

**Version:** 1.1
**Fecha:** 2026-05-04
**Engine:** Unity 6 (6000.3.11f1) + URP
**Target:** Meta Quest 2/3 (standalone), PC VR (OpenXR/SteamVR)
**Framerate objetivo:** 90 fps Quest 3 / 72 fps Quest 2 — nunca debajo
**Arquitectura:** Service Locator + FSM generica + SO-driven data + UpdateService tick pump
**Codebase:** ~79 scripts, ~2,124 LOC

---

## 1. Principios de diseno tecnico

1. **VR-first, todo lo demas despues.** Cada decision se valida contra framerate y confort. Si rompe 90 fps en Quest 3, no entra.
2. **Un solo pump de Update.** Cero `Update()` / `FixedUpdate()` / `LateUpdate()` en MonoBehaviours de gameplay. Todo pasa por `UpdateService` via `IUpdateListener` / `IFixedUpdateListener` / `ILateUpdateListener`.
3. **Service Locator, no singletons.** Ningun sistema de gameplay accede a otro por referencia directa. Se resuelve por interfaz (`ServiceLocator.Get<IRecipeService>()`).
4. **Data = ScriptableObjects.** Ninguna constante de diseno en codigo. Recetas, ingredientes, clientes, noches, borrachera: todos son SO.
5. **Eventos directos en entidades.** Los componentes de gameplay emiten eventos C# (`CustomerEntity.Served`, `Breakable.OnBroken`). Los servicios se suscriben directamente — sin EventBus intermediario.
6. **Cero allocations en hot paths.** Pooling para chunks de rotura, AudioSources. Listas preasignadas. `NonAlloc` en physics queries. `LiquidMix` zero-alloc con buffers fijos.
7. **Estacionario y diegetico.** Jugador fijo detras de la barra; UI montada en el mundo (clipboard, caja registradora, pizarra). Nada flota como HUD — previene motion sickness y reduce overdraw.

---

## 2. Stack tecnico VR

| Componente | Eleccion | Justificacion |
|---|---|---|
| **XR Plugin Management** | OpenXR + Oculus | Cobertura Quest + SteamVR con una sola capa de input. |
| **XR Interaction Toolkit** | 3.x (XRI) | Grab, socket, direct interactor; integra con Input System. |
| **Render pipeline** | URP con Forward+ | Obligatorio para Quest. Vulkan backend. |
| **Input** | New Input System + `InputSystem_Actions.inputactions` | Mapeado estandar XR: grip, trigger, thumbstick. |
| **Fisica** | PhysX (nativo Unity) | `Rigidbody` en botellas y vasos. Collision detection Continuous para objetos criticos. |
| **Liquidos** | Vertex anim shader (`_FillAmount` + `_LiquidColor`) + volumen por float | Sin particles — demasiado caro en Quest. |
| **Destruccion** | Swap a prefab pre-fracturado (3-5 chunks) desde pool | Nada de runtime mesh cutting. |
| **Audio** | Pool preasignado de 12 `AudioSource` con generation-tagged handles | Grupos via `SfxDatabase` entries con spatialBlend configurable. |
| **Texturas** | ASTC 6x6 Quest, Mipmaps ON, Read/Write OFF | Regla dura para mobile VR. |

---

## 3. Arquitectura general

### 3.1. Layout de carpetas

```
Assets/
+-- 1. Scenes/
|   +-- Core/         Boot, Loading
|   +-- Gameplay/     Bar
+-- 2. Scripts/
|   +-- Core/
|   |   +-- Bootstrap/    GameBootstrap.cs
|   |   +-- FSM/          StateMachine.cs, IState.cs
|   |   +-- Managers/     BaseManager.cs, IManager.cs, SceneLoadManager.cs
|   |   +-- SceneNames.cs
|   +-- Data/
|   |   +-- SO/           IngredientSO, BottleSO, RecipeSO, CustomerSO, NightConfigSO,
|   |   |                 DrunkennessConfigSO, IngredientPalette, SfxDatabase, SceneLoadSO
|   |   +-- Enums/        IngredientId, RecipeId, SfxId
|   +-- Services/
|   |   +-- ServiceLocator.cs
|   |   +-- Audio/        IAudioService, AudioService
|   |   +-- Database/     IDatabaseService, DatabaseService
|   |   +-- Economy/      IEconomyService, EconomyService
|   |   +-- GameState/    IGameStateService, GameStateService, GameState (enum)
|   |   +-- Night/        INightService, NightService
|   |   +-- Recipe/       IRecipeService, RecipeService
|   |   +-- Save/         ISaveService, SaveService, SaveData
|   |   +-- UpdateService/ IUpdateService, UpdateService, UpdateServiceObject, UpdateListeners
|   +-- Gameplay/
|   |   +-- Customer/     CustomerEntity, CustomerSeatPoint, CustomerStateIds (enum)
|   |   |   +-- States/   ApproachingState, WaitingState, DrinkingState, LeavingState
|   |   +-- Liquid/       LiquidMix, LiquidContainer, LiquidRenderer, LiquidWobble, PourDetector
|   |   +-- Interactions/ Bottle, Glass, GrabBridge, ServeSocket, SimpleVRGrabber
|   |   +-- Systems/      Breakable, BreakablePoolService
|   |   +-- CashRegister/ CashRegister
|   |   +-- BarSceneRoot.cs
|   +-- UI/
|   |   +-- Diegetic/     GlassFillBar, NightClipboard, PokeButton
|   |   +-- OrdersBoard, TicketView
|   +-- Utilities/        MyLogger, Pool/ (ListPool, PoolGeneric)
+-- 3. Scriptable Objects/
+-- 4. Prefabs/
+-- 5. Assets/
+-- 6. Audio/
+-- 7. Plugins/
+-- 8. Documents/
```

### 3.2. Flujo de arranque

```
Boot scene (persistente, DontDestroyOnLoad)
 +-- GameBootstrap.Awake()
 |   +-- ServiceLocator.Initialize()
 |   +-- RegisterServices()         [9 servicios]
 |   +-- Setup physics layers
 +-- GameBootstrap.Start()
     +-- SceneLoadManager.Load("Bar") con Loading scene opcional
```

`UpdateServiceObject` (MonoBehaviour en Bar) bombea `MyUpdate` / `MyFixedUpdate` / `MyLateUpdate` a todos los listeners registrados. Fallback pump en `GameBootstrap.Update()` si el GameObject no existe.

### 3.3. FSM generica

```csharp
StateMachine<TKey, TOwner>
```

- `TKey`: enum que identifica estados (ej: `CustomerStateId`)
- `TOwner`: entidad duena (ej: `CustomerEntity`)
- Lifecycle: `Enter(owner)` -> `Update(owner)` -> `Exit(owner)`
- Los estados implementan `IState<TOwner>`
- Sin jerarquia (reemplazo del HSM original, refactoreado en ca43282)

**Uso actual:** Customer FSM (`StateMachine<CustomerStateId, CustomerEntity>`).

### 3.4. Service Locator

```csharp
ServiceLocator.Register<IService>(instance)
ServiceLocator.Get<IService>()
```

- Inyeccion por constructor via refleccion
- Inicializacion lazy con deteccion de dependencias circulares
- Todos los servicios implementan `IGameService` con metodo `Initialize()`

---

## 4. Servicios (9, puros C#)

Todos implementan `IGameService`. Se registran en `GameBootstrap.RegisterServices()`. Cero MonoBehaviour (excepto `UpdateServiceObject` como anchor de ticks).

| Servicio | Responsabilidad | Dependencias |
|---|---|---|
| `IUpdateService` | Distribuye ticks a listeners registrados. Sin esto, nada se mueve. | — |
| `IDatabaseService` | Carga todos los SOs desde `Resources/Database/` al boot. Acceso tipado. | — |
| `IRecipeService` | Evalua `LiquidMix` vs `RecipeSO`. Devuelve `RecipeMatch(recipe, score, isExact)`. | IDatabaseService |
| `IEconomyService` | Cash persistente, sales/expenses per-night, NightlyEarnings tracking. | IDatabaseService, ISaveService |
| `INightService` | Timer de noche, spawn de clientes (pool), asignacion a asientos, condicion de cierre. | IUpdateService, IEconomyService |
| `IGameStateService` | FSM global: Boot -> Idle <-> NightRunning <-> NightSummary. | INightService, IEconomyService, ISaveService |
| `IAudioService` | Pool de 12 AudioSources. One-shots (round-robin) + loops (generation-tagged handles). Min retrigger interval per SfxId. | — |
| `ISaveService` | JSON persistente. Atomic writes (temp + File.Replace). Auto-save en pause/quit. | — |
| `IBreakablePoolService` | Object pool para shards de objetos rotos. | — |
| `IUnlockService` | Tracking de recetas/ingredientes desbloqueados. Lookup O(1) con HashSet. `TryUnlock()` debita via EconomyService. | ISaveService, IEconomyService |

### Grafo de dependencias

```
IDatabaseService -+-> IRecipeService
                  +-> IEconomyService --+-> IGameStateService
ISaveService -----+-> IEconomyService   |
                  +-> IUnlockService    +
IEconomyService --+-> IUnlockService
IUpdateService ---+-> INightService
IEconomyService --+
```

---

## 5. ScriptableObjects

| SO | Campos clave | Uso |
|---|---|---|
| `IngredientSO` | `displayName`, `type` (Alcohol/Mixer/Garnish), `color`, `pourRate` | Definicion de cada ingrediente |
| `BottleSO` | `ingredient` (ref a IngredientSO), `capacityMl`, `prefab`, `repairCost` | Config de cada botella fisica |
| `RecipeSO` | `steps[]` (ingredientId + targetMl + tolerance), `foreignToleranceMl` | Definicion de recetas y validacion |
| `CustomerSO` | `prefab`, `walkSpeed`, `patience`, `drinkSeconds`, `baseTip` | Perfil de comportamiento del cliente |
| `NightConfigSO` | `durationSec`, `spawnInterval`, `maxConcurrent`, `customerPool[]`, `recipePool[]` | Configuracion por noche |
| `DrunkennessConfigSO` | `alcoholMlForMax`, `maxTipMultiplier`, `wobbleAmplitude/Frequency` + arrays de niveles (ver seccion 7.5) | Sistema de borrachera |
| `IngredientPalette` | Cache O(1) de colores por IngredientId (256-slot array) | Lookup rapido de color de liquido |
| `SfxDatabase` | Entries: `AudioClip`, `volume`, `pitch`, `spatialBlend` por `SfxId` | Registro centralizado de audio |
| `ShopItemSO` | `displayName`, `description`, `price`, `itemType` (Recipe/Ingredient), `recipeId`, `ingredientId`, `thumbnail` | Items comprables en la tienda de dia |
| `ShakeConfigSO` | `accelThreshold`, `shakesRequired`, `shakeWindowSec`, `cooldownBetweenShakes`, `hapticAmplitude`, `pourRateMlPerSec` | Configuracion del gesto de shake del mezclador |
| `ServiceBellSO` | `cooldownSec`, `wobbleAngleDeg`, `springStiffness`, `damping`, `hapticAmplitude`, `hapticDurationSec` | Configuracion de la campana de servicio |
| `GlassMarksSO` | `mark0/1/2` (float 0-1), `markColor`, `markWidth` | Posiciones y estilo de las marcas visuales en vasos |
| `RegisterTicketConfigSO` | `slideStartLocalPos`, `slideEndLocalPos`, `slideDuration`, `slideCurve` (AnimationCurve) | Animacion del ticket de caja |
| `NightClockConfigSO` | `startHour` (20.0=8PM), `spanHours` (9.0), `updateIntervalSec` (2.0), `sfxTick`, `sfxVolume` | Reloj de noche diegetico |

**Reglas:**
- Enums con valores explicitos espaciados (10, 20, 30...) para insercion futura.
- Nunca renombrar/quitar campos de SO serializados — rompe referencias de assets.
- Datos de diseno siempre en SO, nunca hardcoded.

### Enums clave

```csharp
IngredientId (byte):  None=0, Vodka=10, Gin=20, Rum=30, Whiskey=40, Tequila=50,
                      Tonic=100, Cola=110, Lime=120, Sugar=130, Ice=140

IngredientType:       Alcohol=10, Mixer=20, Garnish=30

GameState:            Boot=0, Idle=10, NightRunning=20, NightSummary=30

CustomerStateId:      Approaching=10, Waiting=20, Drinking=30, Leaving=40

ProportionType (byte): None=0, Strong=10, Half=20, Light=30, Pure=40

ShopItemType:         Recipe=10, Ingredient=20

SfxId:                PourLoop, GlassBreak, BottleBreak, CashSale, CashExpense,
                      ShopPurchase=70, ShopDenied=71, ShakeLoop=80, ShakeMixed=81,
                      BellDing=90, TicketSlide=100, ClockTick=110
```

---

## 6. Estructuras de datos clave

### RecipeMatch (struct)

```csharp
RecipeMatch { RecipeSO Recipe, float Score, bool IsExact }
```

- `Score`: 0..1 — promedio ponderado de precision por ingrediente
- `IsExact`: todos los pasos dentro de tolerancia Y foreign ingredients bajo umbral
- `RecipeMatch.None`: constante estatica para "no match"

### LiquidMix (struct, zero-alloc)

- Max 8 ingredientes simultaneos (buffers fijos)
- `Add(ingredientId, volumeMl)` — acumula volumen
- `GetColor()` — blend ponderado por volumen (CPU, solo cuando cambia la mezcla)
- `GetTotalMl()`, `GetAlcoholMl()` — queries para validacion y borrachera

### SaveData

```csharp
SaveData {
    int version,                    // v2 — migracion desde v1 agrega unlocks
    int cash,
    int nightsCompleted,
    int bestNightEarnings,
    int[] unlockedRecipeIds,        // RecipeId values desbloqueados
    int[] unlockedIngredientIds     // IngredientId values desbloqueados
}
```

- JSON plano en `Application.persistentDataPath/save.json`
- Atomic writes: temp file + `File.Replace()` para prevenir corrupcion
- Migracion v1→v2: inicializa con recetas/ingredientes del MVP (FernetCocaStrong, Cola, Fernet)
- `int[]` en vez de enum arrays porque `JsonUtility` no serializa List de enums

---

## 7. Mecanicas VR — diseno tecnico

### 7.1. Agarre de botellas y vasos

- `GrabBridge` — componente VR-agnostico que traduce eventos de grab del framework XR (XRI/Meta SDK) a estado `isHeld` + UnityEvents.
- `Rigidbody`: `interpolate=Interpolate`, `collisionDetectionMode=Continuous` para botellas llenas; `Discrete` para vasos vacios.
- Haptics: pulso corto al contacto con socket; pulso fuerte al exito de receta; vibracion proporcional al angulo durante vertido.

### 7.2. Vertido (PourDetector)

Componente en cada botella. Se registra como `IFixedUpdateListener` mientras esta agarrada.

```
FixedUpdate:
  if (grabbed && angleFromUpward > tiltThreshold):
    volumeMl = pourRate * dt * tiltFactor(angle)
    RaycastNonAlloc DOWN (buffer preasignado, mascara LiquidContainer) hasta 0.6m
    si impacta LiquidContainer:
      container.Receive(ingredientId, volumeMl)
      play/continue loop SFX via AudioService handle
    else:
      stop loop SFX
```

- Tilt range: 60deg (inicio) a 130deg (maximo caudal).
- Un solo raycast por botella volcada por FixedUpdate, nunca por particula.
- Loop SFX con generation-tagged handle — safe contra reuso de slots.

### 7.3. LiquidContainer y LiquidRenderer

`LiquidContainer` es clase base para Glass y cualquier contenedor futuro (Shaker).

- Owns `LiquidMix` — recibe ingredientes via `Receive()`
- Throttled refresh: solo actualiza renderer cuando el delta de volumen supera un epsilon

`LiquidRenderer`:
- Shader con `_FillAmount` (0-1) y `_LiquidColor` (blend de la mezcla)
- `_WobbleVelocity` alimentado por `LiquidWobble` (solo activo mientras el objeto esta held)
- Actualizado via `MaterialPropertyBlock` — nunca `material.SetX()` (evita instancias de material)

### 7.4. Recetas y validacion

Se dispara cuando un Glass entra en el `ServeSocket` (trigger collider) frente al asiento del cliente.

```csharp
IRecipeService.Evaluate(LiquidMix mix, RecipeId targetId) -> RecipeMatch
  - Por cada step: score = 1 - (|actual - target| / max(tolerance, target))
  - Foreign penalty: si ingredientes no listados > ForeignToleranceMl -> isExact = false
  - Score final: promedio de step scores * (1 - foreign_penalty)

IRecipeService.FindBest(LiquidMix mix) -> RecipeMatch
  - Escanea todas las recetas, devuelve la de mayor score
```

Resultado mapeado a satisfaccion del cliente:
- `isExact = true` -> "Le gusto" (pago completo + propina)
- `isExact = false, score > 0` -> "Meh" (pago reducido)
- `score = 0 o receta incorrecta` -> "No le gusto" (pago minimo o nulo)

### 7.5. Clientes (FSM)

`CustomerEntity` es MonoBehaviour delgado que actua como owner de `StateMachine<CustomerStateId, CustomerEntity>`.

**Estados:**
```
Approaching -> Waiting -> Drinking -> Leaving
                |                       ^
                +--(timeout/fail)-------+
```

| Estado | Logica | Transicion |
|---|---|---|
| **Approaching** | `MoveTowards()` hacia el asiento asignado | Llega al asiento -> Waiting |
| **Waiting** | Escucha `ServeSocket.Served` event. Decrementa timer de paciencia. | Trago servido -> Drinking (si exact) o Leaving. Paciencia agotada -> Leaving. |
| **Drinking** | Countdown (`DrinkTimer`). Modificado por borrachera (ver 7.6). | Timer agotado -> Leaving |
| **Leaving** | `MoveTowards()` hacia exit point. Wobble lateral proporcional a `Drunkenness`. Roll de rotura de vaso. | Llega al exit -> Despawn |

**Sin NavMeshAgent** — movimiento simple con `MoveTowards()` (suficiente para el bar lineal).

**Campos de CustomerEntity:**
- `WaitTimer` / `DrinkTimer` — contadores de fase
- `Drunkenness` — float [0, 1] calculado en WaitingState al recibir trago
- `TargetRecipe` — la receta que pidio
- `Seat` — referencia al `CustomerSeatPoint` asignado

**Eventos emitidos:**
- `Served(recipe, score, isExact)` -> NightService maneja economia
- `Left(wasHappy)` -> NightService libera asiento

### 7.6. Sistema de borrachera

Implementado como modificadores sobre los estados existentes del Customer FSM. Sin estados nuevos.

**Calculo (en WaitingState):**

```
D = alcoholMl / DrunkennessConfigSO.AlcoholMlForMax    (clamped 0..1)
L = GetLevel(D)  // 0-3 segun thresholds
```

**Niveles (umbrales sobre el float D):**

| Nivel | Nombre | Rango D | Trigger tipico |
|---|---|---|---|
| 0 | Sobrio | 0.00-0.29 | Pure (mixer solo) |
| 1 | Achispado | 0.30-0.69 | Liviano 30/70 |
| 2 | Borracho | 0.70-0.99 | Mitad 50/50 |
| 3 | Muy Borracho | 1.00 | Fuerte 70/30 |

**Efectos por nivel:**

| Efecto | L0 | L1 | L2 | L3 |
|---|---|---|---|---|
| Tip multiplier | x1.0 | x1.1 | x1.3 | x1.5 |
| DrinkTimer mult | x1.0 | x0.8 | x0.6 | x0.5 |
| Break risk (vaso) | 0% | 10% | 25% | 45% |
| Wobble amplitude | 0 | D*amp | D*amp | D*amp |
| SFX | — | — | Balbuceo | Grito/queja |

**Puntos de integracion:**

| Donde | Que hace |
|---|---|
| `WaitingState` | Calcula D y L al recibir el trago (ya implementado el calculo de D) |
| `DrinkingState.Enter()` | `DrinkTimer *= drinkSpeedMult[L]` |
| `LeavingState.Enter()` | Roll breakage: `if (Random.value < breakRisk[L])` -> `glass.Break()` |
| `LeavingState.Update()` | Wobble lateral con amplitud proporcional a D (ya implementado) |
| Calculo de propina | `tip *= tipMultiplier[L]` (ya implementado como continuo, migrar a discreto) |

**DrunkennessConfigSO (campos):**

| Campo | Tipo | Default |
|---|---|---|
| `alcoholMlForMax` | float | 60 |
| `levelThresholds` | float[3] | [0.30, 0.70, 1.00] |
| `tipMultipliers` | float[4] | [1.0, 1.1, 1.3, 1.5] |
| `breakRisks` | float[4] | [0.0, 0.10, 0.25, 0.45] |
| `drinkSpeedMult` | float[4] | [1.0, 0.8, 0.6, 0.5] |
| `wobbleAmplitude` | float | config |
| `wobbleFrequency` | float | config |

**Loop de tension central:** Mas alcohol = mas propina pero mas riesgo de rotura (que cuesta plata). El jugador decide el tradeoff en cada trago.

### 7.7. Destruccion (Breakable)

- `OnCollisionEnter` con `impulse > threshold` -> swap a prefab pre-fracturado del pool.
- Prefab roto: 3-5 chunks con Rigidbodies.
- `BreakablePoolService` maneja spawn/despawn de shards.
- Evento en `Bottle.HandleBroken()` -> `EconomyService.RegisterExpense(repairCost)`.
- Cleanup: chunks devueltos al pool despues de un lifetime configurable.
- **Nuevo con borrachera:** `LeavingState` puede forzar `Break()` en el vaso del asiento (sin necesitar impulso fisico) via roll de probabilidad.

### 7.8. Caja registradora

- `CashRegister` — MonoBehaviour con `TMP_Text` world-mounted.
- Escucha eventos de `EconomyService`: `CashChanged`, `SaleRegistered`, `ExpenseRegistered`.
- Flash pop animado: "+$50" (verde) / "-$20" (rojo) con timer.
- SFX via `IAudioService` (CashSale / CashExpense).
- Al final de la noche: resumen visible en `NightClipboard`.

### 7.9. Sistema de proporciones

El GDD define 4 proporciones: Fuerte (70/30), Mitad (50/50), Liviano (30/70), Puro (100%).

**Capacidad estandar del vaso: 200 ml.** Tolerancia: ±10% del total = ±20 ml.

| ProportionType | Alcohol (ml) | Mixer (ml) | Total |
|---------------|-------------|-----------|-------|
| Strong (Fuerte) | 140 | 60 | 200 |
| Half (Mitad) | 100 | 100 | 200 |
| Light (Liviano) | 60 | 140 | 200 |
| Pure (Puro) | 200 | 0 | 200 |

**Ejemplo — Fernet con Coca Fuerte:**

| Step | IngredientId | targetMl | toleranceMl | Rango valido |
|------|-------------|---------|------------|-------------|
| 1 | Fernet | 140 | 20 | [120, 160] ml |
| 2 | Cola | 60 | 20 | [40, 80] ml |

**Estrategia: un RecipeSO por combinacion receta + proporcion.** FernetCocaStrong, FernetCocaHalf, FernetCocaLight son 3 assets separados en el Inspector. Sin cambio de codigo en RecipeService — el scoring existente valida por bandas de tolerancia.

**Impacto en RecipeId:**

```csharp
FernetCocaStrong = 110, FernetCocaHalf = 111, FernetCocaLight = 112,
GinTonicStrong   = 120, GinTonicHalf   = 121, GinTonicLight   = 122,
```

**Display del pedido:** Campos nuevos en RecipeSO:

| Campo | Tipo | Descripcion |
|-------|------|-------------|
| `_proportionType` | ProportionType | Indica que proporcion representa |
| `_displayBaseName` | string | Nombre base sin proporcion ("Fernet con Coca") |

Display: `_displayBaseName + " — " + LocalizeProportionType(_proportionType)` → "Fernet con Coca — Fuerte"

### 7.10. Marcas visuales de proporcion en vasos

Lineas guia en vasos y shakers al ~30%, ~50%, ~70% del llenado para guiar al jugador.

**Implementacion: propiedades shader adicionales** en el shader existente de LiquidRenderer. Sin draw calls extra, sin romper GPU instancing.

**Propiedades shader nuevas:**

| Propiedad | Tipo HLSL | Default | Descripcion |
|-----------|-----------|---------|-------------|
| `_Mark0` | float | 0.30 | Posicion UV marca baja (Liviano) |
| `_Mark1` | float | 0.50 | Posicion UV marca media (Mitad) |
| `_Mark2` | float | 0.70 | Posicion UV marca alta (Fuerte) |
| `_MarkColor` | float4 | (1,1,1,0.5) | Color RGBA de las lineas |
| `_MarkWidth` | float | 0.007 | Grosor en UV space |

**Logica fragment shader:**

```hlsl
float uvY = i.uv.y;
float inMark = saturate(
    step(abs(uvY - _Mark0), _MarkWidth) +
    step(abs(uvY - _Mark1), _MarkWidth) +
    step(abs(uvY - _Mark2), _MarkWidth));
col = lerp(col, _MarkColor, inMark * _MarkColor.a);
```

**Integracion:** `LiquidRenderer` escribe las marcas una sola vez en `Awake()` via `MaterialPropertyBlock` (posiciones estaticas). Solo `_FillAmount` y `_LiquidColor` se actualizan en cada `Refresh()`.

**GlassMarksSO** configura las posiciones y estilo. Vasos y shaker pueden compartir el mismo asset o tener distintos si difieren en UV layout.

Las marcas son siempre visibles — el jugador necesita verlas ANTES de verter para planificar.

### 7.11. Shaker / Mezclador (Via B de preparacion)

Para tragos que requieren mezcla (ej: Gin Tonic). El jugador vierte ingredientes en el shaker, lo agita, y luego vierte del shaker al vaso del cliente.

**Shaker hereda de `LiquidContainer`** (misma base que Glass).

**Archivos nuevos:**
- `Shaker.cs` — hereda LiquidContainer. Agrega `bool IsMixed`, evento `Mixed`, metodo `Consume(float factor)` para vertido proporcional, `ResetMix()`
- `ShakerGestureDetector.cs` — `IFixedUpdateListener`, activo solo mientras held (via GrabBridge)
- `ShakerPourDetector.cs` — variante de PourDetector que consume del shaker en vez de una botella

**Deteccion del gesto de shake:**

```
- Cada FixedUpdate: leer rb.linearVelocity.y
- Detectar inversion de signo (cambio de direccion vertical)
- Si |velocidad| > ShakeConfigSO.accelThreshold al invertir → contar como 1 shake
- Cooldown entre shakes para evitar spam (configurable)
- Si shakesCount >= ShakeConfigSO.shakesRequired dentro de shakeWindowSec → IsMixed = true
- Al completar: evento Mixed, one-shot SFX (ShakeMixed), haptic burst
- Durante shake: loop SFX (ShakeLoop), vibracion haptica ritmica
```

**Vertido desde el shaker (ShakerPourDetector):**

- Mismo patron que `PourDetector` (tilt detection + RaycastNonAlloc down)
- Solo funciona si `IsMixed == true` (antes de mezclar, el shaker no vierte)
- Consume proporcionalmente via `Shaker.Consume(factor)` → reduce todos los ingredientes del mix por el mismo factor
- `LiquidMix.ScaleAll(float factor)` — metodo nuevo en LiquidMix

**RecipeSO:** campo nuevo `_requiresShaker` (bool, informativo para UI — no bloquea validacion). La validacion en RecipeService no cambia: evalua el LiquidMix del vaso final sin importar como llego el liquido.

### 7.12. Campana de servicio

Objeto fisico diegetico en la barra. El jugador lo golpea para iniciar la noche.

**ServiceBell.cs** — MonoBehaviour con `OnCollisionEnter`.

**Deteccion del golpe:**

```
OnCollisionEnter(collision):
  if (impulse < threshold) return          // golpe muy suave
  if (GameStateService.Current != Idle) return  // solo en Idle
  if (_cooldownTimer > 0) return           // anti-spam

  GameStateService.BeginNight()
  AudioService.PlayOneShot(SfxId.BellDing, position)
  SendHapticImpulse(collision hand)
  StartWobbleAnimation()
  _cooldownTimer = config.CooldownSec
```

**Animacion de wobble (damped spring, sin Animator):**

```
alpha = -stiffness * theta - damping * omega
omega += alpha * dt
theta += omega * dt
bellMesh.localRotation = Quaternion.Euler(theta, 0, 0)
```

Con stiffness=120, damping=8 el wobble se estabiliza en ~0.5s. Se registra como `IUpdateListener` solo durante el wobble y se desregistra cuando `|theta| < 0.1 && |omega| < 0.5`.

**Relacion con NightClipboard:** Ambos funcionan. Campana es la interaccion primaria (VR feel), boton del clipboard es fallback. Ambos llaman `IGameStateService.BeginNight()` que ya tiene guarda `if (Current != Idle) return`.

### 7.13. Ticket fisico de caja registradora

Objeto diegetico que sale de la caja registradora al final de la noche con el resumen economico.

**RegisterTicket.cs** — MonoBehaviour, `IUpdateListener` (para el tween).

**Prefab:**

```
RegisterTicket
+-- BoxCollider (para agarre)
+-- GrabBridge (agarre VR)
+-- MeshRenderer (quad — el papel)
+-- Canvas (World Space)
    +-- TMP_Text: _titleLabel    ("RESUMEN DE NOCHE")
    +-- TMP_Text: _nightLabel    ("Noche #3")
    +-- TMP_Text: _earningsLabel ("Ingresos: $XXX")
    +-- TMP_Text: _expensesLabel ("Gastos:  -$XXX")
    +-- TMP_Text: _totalLabel    ("TOTAL:    $XXX")
```

**Animacion de salida (tween, sin Animator):**

```
localPos = Lerp(slideStartPos, slideEndPos, curve.Evaluate(t / duration))
```

- `slideStartPos`: dentro de la ranura (oculto)
- `slideEndPos`: sobresaliendo de la ranura
- `duration`: 0.8s default, curva ease-out
- Al completar: collider activado para agarre
- SFX: `PlayOneShot(SfxId.TicketSlide)` al iniciar

**Flujo:**

| GameState | Accion del ticket |
|-----------|------------------|
| NightRunning | Posicion oculta, collider desactivado |
| NightSummary | Popula datos desde EconomyService/SaveService → inicia tween → collider activo al terminar |
| Idle (sig. noche) | Reset a posicion oculta, collider desactivado, textos limpiados |

**Relacion con NightClipboard:** Coexisten. El ticket es el resumen diegetico primario (el jugador lo agarra y lee). El clipboard mantiene los botones de control (Continue). Ambos leen las mismas fuentes de verdad (EconomyService, SaveService) de forma independiente.

### 7.14. Reloj de noche (timer diegetico)

Reloj diegetico en la pared del bar que muestra la hora de juego durante la noche.

**NightClock.cs** — MonoBehaviour, `IUpdateListener`.

**Formula de conversion:**

```
elapsed  = NightService.DurationSec - NightService.TimeRemaining
gameHour = startHour + (elapsed / durationSec) * spanHours
```

| Simbolo | Tipo | Default | Descripcion |
|---------|------|---------|-------------|
| `startHour` | float | 20.0 | 8PM en formato 24h |
| `spanHours` | float | 9.0 | 8PM a 5AM = 9 horas |
| `durationSec` | float | — | Leido de `INightService.DurationSec` |
| `elapsed` | float | — | Segundos reales transcurridos |

**Ejemplos (noche de 600s):**

| elapsed (s) | gameHour | Display |
|-------------|----------|---------|
| 0 | 20.0 | 8:00 PM |
| 200 | 23.0 | 11:00 PM |
| 400 | 26.0 mod 24 = 2.0 | 2:00 AM |
| 600 | 29.0 mod 24 = 5.0 | 5:00 AM |

**Formato 12h** recomendado — mas diegetico para un bar.

**Ubicacion:** Pared del bar detras de la zona de trabajo, visible con un giro leve de cabeza. Fijo (no movible como el clipboard).

**Visibilidad:** Solo activo durante NightRunning. Se activa/desactiva escuchando `IGameStateService.StateChanged`.

**Refresh rate:** Cada 2 segundos reales (~6 min de juego por tick). No necesita ser per-frame.

**Integracion:** Requiere agregar `float DurationSec { get; }` a `INightService` (un getter de una linea en NightService).

**SFX opcional:** Tick ambiental muy bajo (vol 0.04) cada refresh. Agrega atmosfera sin competir con gameplay.

---

## 8. UI (100% diegetica)

Toda la UI es world-space. Cero screen-space canvas. Cero HUD flotante.

| Elemento | Componente | Ubicacion | Interaccion |
|---|---|---|---|
| Pedido del cliente | Espacial (sobre NPC) | Sobre la cabeza | Solo lectura |
| Paciencia | Espacial (barra) | Sobre la cabeza | Solo lectura |
| Fill del vaso | `GlassFillBar` | Sobre el vaso (billboard) | Solo lectura |
| Borrachera | Visual/corporal | Animacion del NPC (sway) | Solo lectura |
| Clipboard de noche | `NightClipboard` | Sobre el mostrador (grabbable) | PokeButtons: Start / Abort / Continue |
| Caja registradora | `CashRegister` | En la caja (world TMP) | Solo lectura + flash |
| Pizarra de recetas | `OrdersBoard` | Detras del jugador | Solo lectura (darse vuelta) |
| Tickets | `TicketView` | En pizarra de pedidos | Solo lectura |
| Ticket de caja | `RegisterTicket` | Sale de la caja registradora | Agarrable (GrabBridge) |
| Reloj de noche | `NightClock` | Pared del bar | Solo lectura (solo NightRunning) |
| Catalogo tienda | `ShopCatalog` | Sobre el mostrador (solo Idle) | PokeButtons para comprar |
| Marcas de proporcion | Shader (GlassMarksSO) | En vasos y shakers | Solo lectura (siempre visible) |

**NightClipboard** tiene 3 canvas groups activos segun `GameState`:
- **Idle:** Boton "Start Night" + stats de progresion (noches completadas, mejor recaudacion, cash)
- **NightRunning:** Boton "Abort"
- **NightSummary:** Boton "Continue" + resumen (cash, sales, failed, expenses, nightly earnings)

**PokeButton:** Interaccion por colision fisica (poke con dedo indice). Sin `GraphicRaycaster`. Solo responde si el clipboard esta held (via `GrabBridge`).

---

## 9. Sistema de audio

`AudioService` — servicio puro C# (no MonoBehaviour).

**Pool architecture:**
- 12 `AudioSource` objects preasignados
- **One-shots:** round-robin por slots libres
- **Loops:** slots reservados con generation counter. Handle = `generation << 16 | slotIndex`. Handles stale se auto-invalidan si el slot rota.
- **Min retrigger interval:** per-`SfxId` para prevenir spam (ej: pasos, colisiones)

**Configuracion por clip (en SfxDatabase):**
- `volume`, `pitch`, `spatialBlend` (0 = 2D, 1 = 3D)

**SFX confirmados por gameplay:**

| SfxId | Trigger | Tipo |
|---|---|---|
| PourLoop | Botella inclinada sobre contenedor | Loop (handle) |
| GlassBreak | Vaso roto (impulso o borrachera) | One-shot |
| BottleBreak | Botella rota (impulso) | One-shot |
| CashSale | Venta registrada | One-shot |
| CashExpense | Gasto registrado (rotura) | One-shot |
| ShopPurchase | Item comprado en tienda | One-shot |
| ShopDenied | Compra denegada (sin plata) | One-shot |
| ShakeLoop | Shaker siendo agitado | Loop (handle) |
| ShakeMixed | Mezcla completada exitosamente | One-shot |
| BellDing | Campana de servicio golpeada | One-shot |
| TicketSlide | Ticket saliendo de la caja | One-shot |
| ClockTick | Tick ambiental del reloj (vol 0.04) | One-shot |

---

## 10. Economia y persistencia

### EconomyService (per-night tracking)

| Campo | Persistencia | Descripcion |
|---|---|---|
| `Cash` | Persiste entre noches (SaveData) | Saldo total acumulado |
| `Sales` | Reset per-night | Cantidad de ventas exitosas |
| `FailedOrders` | Reset per-night | Clientes que se fueron sin pagar |
| `Expenses` | Reset per-night | Costos por rotura |
| `NightlyEarnings` | Reset per-night | Ganancia neta de la noche |

**Flujo de una venta:**
1. Cliente servido -> `RegisterSale(recipe, score, tip)`
2. Base pay = `recipe.BasePrice * score`
3. Tip modificado por borrachera: `tip * tipMultiplier[drunkLevel]`
4. `Cash += basePay + modifiedTip`

**Flujo de un gasto:**
1. Botella/vaso roto -> `RegisterExpense(amount, description)`
2. `Cash -= amount`

### SaveService

- **Formato:** JSON plano
- **Ubicacion:** `Application.persistentDataPath/save.json`
- **Escritura atomica:** temp file + `File.Replace()` — previene corrupcion si la app crashea mid-write
- **Triggers:** `OnApplicationPause()`, `OnApplicationQuit()` via GameBootstrap
- **Version tracking:** campo `version` para migraciones futuras de schema

---

## 11. Escenas

| Escena | Contenido | Persistencia |
|---|---|---|
| **Boot** | `GameBootstrap` (singleton, DontDestroyOnLoad) | Persistente |
| **Loading** | Pantalla de carga async (opcional, intermedia) | Transitoria |
| **Bar** | Gameplay completo: barra, asientos, botellas, clientes, UI diegetica | Gameplay |

### BarSceneRoot (singleton en Bar)

Referencia centralizada a objetos de la escena:
- `Seats[]` — array de `CustomerSeatPoint` (3 para MVP)
- `CustomerSpawnPoint` / `CustomerExitPoint` — puntos de spawn/despawn
- `BottleShelfPoints[]` — posiciones de botellas en la estanteria
- `CashRegisterAnchor` — mount point para display de caja
- `PlayerAnchor` — posicion del XR camera rig

---

## 12. Reglas de performance (obligatorias)

### Hot paths

- Cero `Update/FixedUpdate/LateUpdate` en MB de gameplay. Solo en `UpdateServiceObject` y `GameBootstrap` (fallback).
- Cero LINQ en runtime.
- Cero `foreach` en colecciones dinamicas no-array.
- Cero `Instantiate/Destroy` en hot paths -> `PoolGeneric` / `BreakablePoolService` para todo spawn recurrente.
- Cero allocations per frame. Verificar con Profiler (GC Alloc = 0 B en Play).
- Cero `GetComponent` en hot paths -> cachear en `Awake`.
- Structs sobre clases donde aplique (`LiquidMix`, `RecipeMatch`).
- Enum/int keys sobre strings.
- `Physics.RaycastNonAlloc` con buffer preasignado (4 slots por `PourDetector`).
- `MaterialPropertyBlock` para cambios de shader por instancia.

### Rendering (URP Mobile / Quest)

- Single Pass Instanced obligatorio.
- Static batching + GPU Instancing en mobiliario.
- Lightmaps horneados — cero luces dinamicas en tiempo real excepto maximo 1 spot si cabe en budget.
- Sombras: baked only. Si dinamica, Hard Shadows Only, distancia <= 5m.
- Texturas: ASTC 6x6, Mipmaps ON, Read/Write OFF.
- Atlas de materiales para botellas (un material compartido con GPU Instancing).
- Occlusion Culling activo en el bar.
- Light Probes para los clientes (objetos dinamicos).
- Post-processing: OFF en Quest salvo Tonemapping.

### VR-especifico

- `Application.targetFrameRate` = 72 (Quest 2) / 90 (Quest 3).
- Fixed Foveated Rendering: nivel Medium en Quest.
- Zero heavy work en `LateUpdate` (camara).
- Camara fija al playspace del usuario — sin movimiento artificial, cero locomotion.
- Transiciones de escena: fade a negro diegetico (plano delante del headset, no post-process).

### Audio

- OGG Vorbis. Mono para SFX cercanos, estereo solo para musica.
- Pool de 12 AudioSource preasignados.
- SpatialBlend configurable per-clip via SfxDatabase.

---

## 13. Estado de implementacion

### Implementado y funcional

| Sistema | Estado | Archivos clave |
|---|---|---|
| Service Locator + DI | Completo | `ServiceLocator.cs` |
| FSM generica | Completo | `StateMachine.cs`, `IState.cs` |
| Bootstrap + scene loading | Completo | `GameBootstrap.cs`, `SceneLoadManager.cs` |
| Update tick pump | Completo | `UpdateService.cs`, `UpdateServiceObject.cs` |
| Database (SO loading) | Completo | `DatabaseService.cs` |
| Liquid system (pour/mix/render/wobble) | Completo | `LiquidMix.cs`, `LiquidContainer.cs`, `PourDetector.cs`, `LiquidRenderer.cs` |
| Bottles + Glass | Completo | `Bottle.cs`, `Glass.cs`, `GrabBridge.cs` |
| ServeSocket + recipe validation | Completo | `ServeSocket.cs`, `RecipeService.cs` |
| Customer FSM (4 estados) | Completo | `CustomerEntity.cs`, `States/*.cs` |
| Night service (spawn/timer) | Completo | `NightService.cs` |
| Economy (cash/sales/expenses) | Completo | `EconomyService.cs` |
| GameState FSM | Completo | `GameStateService.cs` |
| Save system (atomic JSON) | Completo | `SaveService.cs`, `SaveData.cs` |
| Audio (pooled, handles) | Completo | `AudioService.cs`, `SfxDatabase.cs` |
| Breakables + pool | Completo | `Breakable.cs`, `BreakablePoolService.cs` |
| Cash register (diegetic) | Completo | `CashRegister.cs` |
| NightClipboard (3 states) | Completo | `NightClipboard.cs` |
| PokeButton | Completo | `PokeButton.cs` |
| Ingredient palette (O(1) color) | Completo | `IngredientPalette.cs` |
| Editor scaffolders | Completo | `Editor/*.cs` (9 archivos) |

### Parcialmente implementado

| Sistema | Estado | Falta |
|---|---|---|
| Borrachera | Calculo de D + tip multiplier continuo + wobble al salir | Niveles discretos, breakRisk roll, drinkSpeed modifier, SFX por nivel (disenado en GDD v1.3, ver seccion 7.6) |

### No implementado — con spec tecnica (listo para implementar)

| Sistema | Prioridad | Seccion TDD | Estimacion |
|---|---|---|---|
| Niveles discretos de borrachera | Alta | 7.6 | 1 dia |
| Sistema de proporciones (RecipeSO por proporcion) | Alta | 7.9 | 0.5 dia (config en Inspector) |
| Shaker/Mezclador (Via B) | Alta | 7.11 | 2 dias |
| Campana de servicio | Media | 7.12 | 0.5 dia |
| Tienda de dia / IUnlockService | Media | Servicios (sec 4) + SaveData (sec 6) | 2 dias |
| Marcas visuales de proporcion | Media | 7.10 | 1 dia (shader + SO) |
| Ticket fisico de caja | Media | 7.13 | 1 dia |
| Reloj de noche | Media | 7.14 | 0.5 dia |

### No implementado — sin spec (post-MVP)

| Sistema | Prioridad | Notas |
|---|---|---|
| Pizarron de recetas | Media | Display diegetico detras del jugador con recetas disponibles |
| Hand tracking | Baja | Soporte planificado, actualmente deshabilitado |
| LOD para NPCs | Baja | 2 niveles basados en distancia a barra |
| Spill VFX/penalty | Baja | Derramar fuera del vaso |

---

## 14. Checklist de validacion por feature

Toda feature nueva pasa por:

- [ ] No introduce `Update/FixedUpdate/LateUpdate` en MB de gameplay.
- [ ] No genera GC alloc en Play (verificado con Profiler).
- [ ] Si spawnea, usa pool.
- [ ] Data nueva -> SO, no constantes en codigo.
- [ ] Comunicacion cross-sistema -> ServiceLocator, nunca singleton directo.
- [ ] Funciona con Single Pass Instanced.
- [ ] Mantiene 90 fps en Quest 3 con carga peor-caso (3 clientes + 2 botellas volcandose + 1 rotura).
- [ ] Haptics y audio con feedback en todas las interacciones de manos.
- [ ] Sin motion sickness: nada se mueve bajo los pies del jugador; transiciones con fade diegetico.

---

## 15. Convenciones de codigo

- `PascalCase` clases y metodos publicos
- `_camelCase` campos privados serializados
- `camelCase` variables locales
- `PascalCaseSO` para ScriptableObjects
- `I` prefix para interfaces (`IGameService`, `IState`)
- Enums con valores explicitos espaciados (10, 20, 30...) para insercion futura
- Codigo en **ingles**, comunicacion en **espanol**
- Branching: `feature/<nombre-corto>` desde `main`
- Commits: conventional commit format (`feat:`, `fix:`, `refactor:`, etc.)

---

## 16. Glosario tecnico

| Termino | Significado | No usar |
|---|---|---|
| **Noche** | Sesion completa de gameplay (intro + servicio + cierre) | Nivel, ronda |
| **Servicio** | Fase activa de atencion a clientes dentro de una noche | Turno, partida |
| **Trago** | Resultado de una receta servida | Coctel (salvo UI) |
| **Pedido** | Solicitud de un cliente vinculada a una `RecipeSO` | Orden (ambiguo) |
| **Proporcion** | Ratio de ingredientes: Fuerte/Mitad/Liviano/Puro | Porcentaje |
| **Borrachera** | Mecanica de intoxicacion por trago servido | Alcoholismo, ebriedad |
| **Dano** | Costo economico por rotura | Destruccion (generico) |
| **Servir** | Colocar/verter trago en el vaso del ServeSocket | Entregar |
| **ServeSocket** | Trigger collider frente al asiento que valida la receta | Socket de servicio |
| **Mezclador/Shaker** | Contenedor para tragos que requieren mezcla (Via B) | Coctelera |
| **Pour directo** | Verter ingredientes directo al vaso del cliente (Via A) | — |
| **Proporcion** | Ratio de ingredientes: Fuerte/Mitad/Liviano/Puro | Porcentaje |
| **Tick pump** | UpdateService distribuyendo frames a listeners | Game loop |
