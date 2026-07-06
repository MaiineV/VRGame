# Plan de Migracion — Meta Interaction SDK (Building Blocks)

**Version:** 1.2
**Fecha:** 2026-07-05
**Estado:** Fases 0-4 completadas (ver seccion 8). Teleport + limpieza de laser pointer manual
completadas (ver seccion 9).
**Relacionado:** [Technical.md](Technical.md)

---

## 1. Contexto y motivacion

El proyecto tiene `com.meta.xr.sdk.all` 201.0.0 instalado (incluye el Interaction SDK completo y el flujo de Building Blocks) pero no lo usa para nada: todo el grab de vasos y botellas es codigo manual (`SimpleVRGrabber.cs`), no hay manos virtuales renderizadas, no existe ray/distance grab para gameplay, y las botellas no tienen orientacion de agarre — solo poses estaticas en los slots de la estanteria.

`Technical.md` (seccion 2, stack VR) afirmaba que el proyecto usa "XR Interaction Toolkit 3.x (XRI)" — esto era incorrecto: no hay paquete XRI en `Packages/manifest.json`, solo Meta XR SDK All. Corregido en este doc y en `Technical.md`.

Objetivo: migrar grab cercano, grab a distancia (ray), manos visibles y orientacion de agarre al Interaction SDK usando el flujo de Building Blocks (Meta > Tools > Building Blocks) en vez de ensamblar componentes a mano, preservando toda la logica de gameplay existente (`GrabBridge`, `Bottle`, `Glass`, `ShelfSlot`, `ServeSocket`, `BottleUnlockGate`).

**Decision de UX tomada:** el grab pasa de **toggle** (apretar una vez agarra, apretar de nuevo suelta) a **hold-to-grab** (mantener el grip apretado), que es el selector default de los Building Blocks. Evita escribir un `ISelector` custom y mantiene la premisa de "solo building blocks". Riesgo aceptado: posible fatiga de sesiones largas — a validar en playtest tras la Fase 2.

---

## 2. Grounding tecnico (verificado en codigo real)

- `GrabBridge.cs` (`Assets/2. Scripts/Gameplay/Interactions/GrabBridge.cs`) ya esta disenado para ser agnostico al stack VR — su propio comentario dice que tanto XRI como el `Grabbable` de Meta pueden conectarse a `SetHeld(bool)`. Expone `OnGrab()` / `OnRelease()` pensados para wirearse desde eventos de Inspector. **No se toca.**
- `Oculus.Interaction.Grabbable` (lo que instala el building block `HandGrab`) tiene `_kinematicWhileSelected = true` por default → replica el comportamiento actual sin cambios en `ServeSocket.cs`.
- `Oculus.Interaction.InteractableUnityEventWrapper` expone `WhenSelect` / `WhenUnselect` en el Inspector → punto de wiring hacia `GrabBridge.OnGrab()` / `OnRelease()`, sin codigo nuevo.
- `IGrabGate.cs` tiene un solo consumidor (`SimpleVRGrabber`) y un solo implementador (`BottleUnlockGate.cs`) — confirmado por grep, 3 archivos en total referencian la interfaz.
- **Correccion importante sobre el veto de compra:** `CanGrab` y `ShouldBeVisible()` (en `BottleUnlockGate.cs`) son cosas distintas. Una botella "en venta" (`DayShop`, locked) es visible aunque el jugador no pueda pagarla — el veto de agarre no pasa por `SetVisible()` / `Apply()`, se evalua en vivo cada vez que `SimpleVRGrabber.TryGrab` pregunta `gate.CanGrab`. Si el veto se migrara a togglear `HandGrabInteractable.enabled` solo dentro de `Apply()` (que corre solo cuando cambia la visibilidad), una botella visible-pero-no-afordable nunca se re-evaluaria cuando el jugador gana o gasta plata sin que cambie el estado de shop. Hay que extender el poll ya existente en `Update()` (mismo patron que `_lastVisible`) para trackear un `_lastGrabbable` derivado de `CanGrab`, independiente de la visibilidad, y togglear ahi `HandGrabInteractable.enabled` / `DistanceHandGrabInteractable.enabled`.
- `Glass.ResetForPool()` (linea 84-85) llama `GetComponent<GrabBridge>().SetHeld(false)` al reciclar un vaso pooled — sigue funcionando, pero si el interactor de Meta todavia lo tiene seleccionado hay que forzar tambien su release (verificar API exacta en el Editor: `Grabbable` / `HandGrabInteractable` probablemente exponen un `ForceRelease()`, o hay que llamar `Unselect()` sobre el interactor que lo tiene seleccionado).
- Building blocks confirmados instalados (`Library/PackageCache/com.meta.xr.sdk.interaction.ovr@.../Editor/Blocks/`):
  - Interactors: `[BB] Grab Interactor`, `[BB] Hand Poke`, `[BB] Hand Ray`
  - Interactable installation routines: `HandGrab`, `DistanceHandGrab`, `TouchHandGrab`, `Pokeable`, `RayInteraction`
  - Hand visuals: `[BB] Left Hand Synthetic`, `[BB] Right Hand Synthetic`
  - `HandTracking` (fuente de datos de tracking, en el paquete core)
- No existe un building block unico para "snap rotation al agarrar". El mecanismo real de Meta es autorar un **Hand Grab Pose** por objeto (componente/asset que graba una pose de mano natural). El `SnapInteractor` / `SnapInteractable` es un sistema aparte, pensado para devolver un objeto a una pose fija en un socket (ej. estanteria), no para orientar el agarre en si.

---

## 3. Fases

### Fase 0 — Prep
- Corregir `Technical.md` (stack VR, ver seccion 4).
- Este doc queda linkeado desde ahi.

### Fase 1 — Manos visibles
- Building Block `HandTracking` sobre el `OVRCameraRig`.
- `[BB] Left Hand Synthetic` / `[BB] Right Hand Synthetic` bajo los anchors de mano en `Bar.unity`.
- Cosmetico unicamente por ahora — el grip sigue siendo controller-driven (`OVRInput`), sin pinch-to-grab en este alcance.
- **Done:** ambas manos se ven trackeadas en Editor (OVR simulator) y en dispositivo, sin romper el grab actual (que sigue activo hasta la Fase 2).

### Fase 2 — Grab cercano (reemplaza SimpleVRGrabber)
- `[BB] Grab Interactor` en cada hand anchor (`RightHandAnchor` / `LeftHandAnchor` en `Bar.unity`).
- Borrar los 2 componentes `SimpleVRGrabber` de esos mismos anchors.
- Installation routine `HandGrab` en la raiz de cada uno de los 9 prefabs grabbables: `Bottle.prefab`, `Bottle5.prefab`, `Bottle_JackDaniel.prefab`, `Bottle_Hennessy.prefab`, `Bottle_Champagne.prefab`, `Bottle_Wine.prefab`, `Bottle_SimpleBottle.prefab`, `Glass.prefab`, `Glass_Asset.prefab`.
- `InteractableUnityEventWrapper` en cada uno → `WhenSelect → GrabBridge.OnGrab()`, `WhenUnselect → GrabBridge.OnRelease()`.
- **Veto de compra:** extender `BottleUnlockGate` segun la seccion 2 (poll independiente de visibilidad). Una vez migrado, borrar `IGrabGate.cs` (sin consumidores).
- **Haptics:** `SimpleVRGrabber` dispara pulsos (`HapticService`) al agarrar/soltar (lineas 270-271, 278-279 del archivo actual) — se pierde al borrar el script. Re-wirear desde `WhenSelect` / `WhenUnselect` o desde `GrabBridge.Grabbed` / `Released`.
- **Done:** agarrar/sostener/soltar/tirar cada uno de los 9 prefabs funciona igual que hoy; `BottleUnlockGate` sigue cobrando y ocultando bien; `ServeSocket` no se rompe; haptics siguen sonando.

### Fase 3 — Ray / distance grab
- `[BB] Hand Ray` en cada hand anchor, conviviendo con el Grab Interactor de la Fase 2.
- Installation routine `DistanceHandGrab` en los mismos 9 prefabs.
- Segundo `InteractableUnityEventWrapper` (o extender el existente si soporta multiples `IInteractableView` — verificar en Editor) apuntando a los mismos `GrabBridge.OnGrab()` / `OnRelease()` (es idempotente: `GrabBridge.SetHeld` ya ignora si `IsHeld` no cambia).
- **Done:** apuntar y traer una botella/vaso a distancia funciona en toda la barra; sin carrera/desync con el grab cercano.

### Fase 4 — Orientacion de agarre (snap rotation vertical)
- Autorar un `HandGrabPose` por silueta distinta: la forma "caja" cubre 5 variantes de botella; `Bottle_SimpleBottle` (colisor capsula) necesita la suya; `Glass` / `Glass_Asset` la suya.
- **Stretch, no bloqueante:** evaluar `SnapInteractor` para que una botella soltada cerca de su `ShelfSlot` vuelva sola a pose vertical — `ShelfSlot.ResetInPlace()` ya cubre el caso de respawn, es solo valor incremental. Ticket separado, no criterio de done de esta migracion.
- **Done:** agarrar una botella desde cualquier angulo da un agarre natural y consistente; deja de aparecer agarrada de costado o al reves.

---

## 4. Archivos afectados

| Accion | Archivo |
|---|---|
| Borrar (tras Fase 2) | `SimpleVRGrabber.cs`, `IGrabGate.cs` |
| Editar | `BottleUnlockGate.cs` (poll de grab-enabled independiente de visibilidad) |
| Editar | `Glass.cs` (force-release del interactor en `ResetForPool()`) |
| Editar | `Technical.md` (seccion 2, stack VR — ver seccion 5 de este doc) |
| Sin cambios | `GrabBridge.cs`, `Bottle.cs`, `ShelfSlot.cs`, `ServeSocket.cs` |
| Escena | `Bar.unity` — sacar 2x `SimpleVRGrabber`; agregar building blocks de hands/interactors |
| Prefabs (9) | agregar `HandGrabInteractable` + `Grabbable` (F2), `DistanceHandGrabInteractable` (F3), `HandGrabPose` (F4), event wrappers |

---

## 5. Correccion pendiente en Technical.md

`Technical.md`, tabla de stack VR ("XR Interaction Toolkit" → "3.x (XRI)" → "Grab, socket, direct interactor; integra con Input System") debe reemplazarse por una fila que refleje el stack real: **Meta XR Interaction SDK** (`com.meta.xr.sdk.interaction` + `com.meta.xr.sdk.interaction.ovr`, parte de `com.meta.xr.sdk.all`), flujo de Building Blocks, input via `OVRInput` (el New Input System esta instalado pero sin uso — cero referencias en `Assets/2. Scripts`).

---

## 6. Riesgos a chequear en el Editor antes/durante implementacion (con UnityMCP)

- Nombre exacto del metodo de force-release de `Grabbable` / `HandGrabInteractable` (necesario para `Glass.ResetForPool`).
- Si `InteractableUnityEventWrapper` soporta multiples `IInteractableView` o hace falta un wrapper por interactable.
- Si el installation routine de `[BB] Grab Interactor` asume/requiere un `OVRHand` presente incluso para grab por controller — si es asi, la Fase 1 pasa a ser prerequisito duro de la Fase 2, no solo cosmetico.
- El collider `CapsuleCollider` de `Bottle_SimpleBottle.prefab` (vs `BoxCollider` del resto) — confirmar que no rompe el default de las installation routines de `HandGrab` / `DistanceHandGrab`.
- Throw feel del `ThrowWhenUnselected` de Meta sin headset (el fallback de `SimpleVRGrabber` para editor sin controlador real desaparece) — validar que lo que importa es el feel en dispositivo, no en editor.

---

## 7. Verificacion por fase

- **F1:** chequeo visual en Editor + dispositivo, cero riesgo logico (grab viejo sigue activo).
- **F2:** grab/hold/release/throw en los 9 prefabs; `BottleUnlockGate` cobra y oculta bien; `ServeSocket` detecta fill/rest igual; haptics suenan.
- **F3:** distance-grab en todo el ancho de la barra, todas las variantes; sin doble-select con el grab cercano.
- **F4:** playtest subjetivo — agarre prolijo desde angulos variados.

---

## 8. Ejecucion real (2026-07-05)

Fases 0-3 ejecutadas en esta sesion invocando directamente la API interna de Building Blocks
(`BlockData.AddToProject` / `AddToObjects` via reflection, dentro del Editor) — mismo codigo que
dispara el boton "Add block" de la ventana Meta > Tools > Building Blocks, sin ensamblar
componentes a mano.

**Hallazgos vs. el plan original:**

- De los "9 prefabs" listados, solo **7 son botellas/vasos de gameplay reales**:
  `Bottle`, `Bottle_Champagne`, `Bottle_Hennessy`, `Bottle_JackDaniel`, `Bottle_SimpleBottle`,
  `Bottle_Wine`, `Glass`. `Bottle5.prefab` no tiene ningun script de gameplay (`Bottle`/`GrabBridge`)
  y no lo referencia nada en el proyecto (prop huerfano); recibio `HandGrab`+`DistanceGrab` igual
  (inofensivo) pero sin wiring de eventos. `Glass_Asset.prefab` es solo el mesh visual anidado
  dentro de `Glass.prefab` (Transform+MeshFilter+MeshRenderer, sin Rigidbody) — no se toco.
- El block **"Grab Interactor"** (Fase 2) ya instala en un solo paso tanto `HandGrabInteractor`
  (agarre cercano) como `DistanceHandGrabInteractor` (agarre a distancia) en cada mano — el block
  separado "Hand Ray Interactor" del plan original no hizo falta para el caso de uso (agarrar a
  distancia). Fase 3 termino siendo, del lado de la mano, un subproducto de Fase 2.
- Wiring de eventos: se uso `PointableUnityEventWrapper` (no `InteractableUnityEventWrapper`) sobre
  el `Grabbable` de la raiz de cada prefab — un unico wrapper por objeto cubre selects/unselects de
  **ambos** interactores (cercano y distancia), ya que `Grabbable` los agrega a los dos. `WhenSelect`
  -> `GrabBridge.OnGrab()`, `WhenUnselect` -> `GrabBridge.OnRelease()` (persistent calls sin argumento).
- **Veto de compra:** `BottleUnlockGate` ahora cachea `HandGrabInteractable[]`/`DistanceHandGrabInteractable[]`
  en `Awake()` y los apaga/prende en un poll independiente (`ApplyGrabbable()`), separado del poll de
  visibilidad, tal como preveia la seccion 2.
- **Haptics:** el pulso de agarrar/soltar se re-implemento directamente en `GrabBridge.SetHeld()` via
  `IHapticService.PulseBoth(...)` (patron ya usado en otros scripts del proyecto para casos sin mano
  especifica) — evita acoplar `GrabBridge` a `Oculus.Interaction`.
- **Regresion conocida (no resuelta):** `GrabBridge.HeldByHand` ya no lo setea nadie (antes lo hacia
  `SimpleVRGrabber` via `OVRInput.Controller`). Esto apaga el pulso de haptics **dirigido a una sola
  mano** que usa `PourDetector.cs:234-237` mientras se sirve un trago con la botella agarrada — sigue
  sonando el pulso general de agarrar/soltar, pero no el de "estas sirviendo". Requiere leer la mano
  desde el interactor real de Meta (`HandGrabInteractor`/`DistanceHandGrabInteractor` que dispara el
  `PointerEvent`) para reconstruir el dato; quedo fuera de alcance por acoplar `GrabBridge` al SDK.
- `Glass.ResetForPool()` **no se toco**: confirmado por `GlassPoolService.RecycleOldestUnheld()` (que
  salta explicitamente un vaso sostenido) y `Spawn()` (que solo reutiliza instancias ya inactivas en
  el pool) que `ResetForPool` nunca corre con el vaso realmente agarrado — el riesgo de la seccion 2
  no aplica en la practica.
- Limpieza adicional: se borraron `SimpleVRGrabber.cs`, `IGrabGate.cs` y `Assets/2. Scripts/Editor/ToggleGrabSetter.cs`
  (utilidad de editor que solo tenia sentido con el toggle-grab viejo). Se encontraron y removieron
  2 componentes `SimpleVRGrabber` "Missing Script" que habian quedado huerfanos en `PlayerAnchor.prefab`
  (la fuente real de los hand anchors) ademas de las 2 instancias en `Bar.unity`.

**Fase 4 — resuelta con el comportamiento nativo del SDK (decision del usuario):**

Se evaluo autorar un `HandGrabPose` por silueta (captura de la pose real de los dedos), pero se
descarto: no hay forma de generarla por codigo sin arriesgar una pose antinatural, y el usuario
prefirio explicitamente **no** usar la herramienta de autoria de poses del SDK — usar tal cual lo
que el SDK ya trae, clipping incluido.

Lo que ya viene instalado (sin tocar nada mas) cubre el pedido:

- **Orientacion del agarre:** sin `HandGrabPose` autorada, `HandGrabInteractable.CalculateBestPose`
  cae a su fallback nativo: agarra en el punto mas cercano del collider preservando la rotacion con la
  que la mano se acerco (`GrabPoseHelper.CollidersScore` + snap al hit point, confirmado leyendo
  `HandGrabInteractable.cs`). Funcional, no roto.
- **Cierre de dedos:** el rig instalado en Fase 1/2 ya trae `ControllerPinchInjector` (confirmado en
  la jerarquia real de los interactores, bajo `HandGrabAPI`) — es el componente nativo de Meta que
  maneja el pinch/grab de los dedos directamente desde el trigger/grip del control, sin depender de
  ninguna pose por objeto. Es "la animacion que viene con el SDK": generica, la misma para cualquier
  objeto, sin autoria adicional.

No se requiere trabajo adicional para dar por cerrada esta fase.

---

## 9. Auditoria posterior — Teleport + limpieza de VrLaserPointer (2026-07-05)

Tras cerrar las Fases 0-4, se audito el resto del proyecto en busca de otras interacciones VR
resueltas a mano en vez de con Building Blocks. Aparecieron dos casos: la locomocion por teleport
(`ThumbstickLocomotion.cs`, arco/reticle 100% custom) y un laser pointer manual muerto en el menu
(`VrLaserPointer.cs`). Ambos resueltos en esta sesion.

### 9.1 Teleport — Building Block, variante NavMesh

Se evaluaron las 3 variantes de `TeleportInstallationRoutine` (Hotspot, NavMesh,
PhysicsLayerBlocker). Hotspot es un punto fijo (no sirve para apuntar libremente); Physics Layer
Blocker con `AllowTeleport=false` solo bloquea el arco contra paredes, no define zona de aterrizaje.
**NavMesh** es la unica que reproduce el "apuntar a cualquier punto del piso, verde/rojo segun
validez" que tenia la implementacion manual.

**Problema detectado:** el NavMesh que ya existia (`RuntimeNavMeshBaker.cs`, para pathing de NPCs)
esta tuneado a agentes de 0.6 escala (radio 0.2m, altura 1.2m) — reusarlo para el teleport del
jugador dejaria aterrizar pegado a mostradores/estanteria. Solucion: `RuntimePlayerNavMeshBaker.cs`
(mismo patron de bake async por `NavMeshBuilder`/`NavMeshData`, pero crea su propio agent type en
`Awake()` via `NavMesh.CreateSettings()`, radio 0.35m / altura 1.75m, sobre los mismos bounds
40x12x40 que ya cubrian todo el piso). Es idempotente entre sesiones de Play repetidas sin reload de
dominio: busca un agent type existente con la misma tunning antes de crear uno nuevo, para no
correr el indice cada vez que se entra a Play.

**Instalacion:** vía reflection contra `Meta.XR.BuildingBlocks.Editor.InterfaceBlockData` desde
`execute_code` (mismo mecanismo que Fases 1-3). A diferencia de la migracion de grab, esta vez el
`InstallWithDependencies` SI abrio una `InstallationWindow` visible (titulo "Teleport") esperando
confirmacion — se resolvio invocando `Confirm()` de esa ventana por reflection tras fijar
`variant = NavMesh` en el asset `TeleportInstallationRoutine` (ojo: esto persiste el default de esa
variante en el asset del paquete — aviso de Unity sobre "immutable package asset modificado",
esperado y aceptado ya que el proyecto solo necesita NavMesh).

**Resultado real de la instalacion** (mas completo de lo esperado): ademas de los 2
`TeleportInteractable` (Walkable/Non Walkable, cada uno con su propio `NavMeshSurface` +
`ReticleDataTeleport` nativo), el wizard agrego `TeleportControllerInteractor` x2 (control),
`TeleportMicrogestureInteractor` x2 (gesto de mano sin control), y un `BodyTeleportInteractor` bajo
un GameObject "Locomotor" con un `FirstPersonLocomotor` (CharacterController + Rigidbody +
CapsuleCollider) — no el `PlayerLocomotor` minimo que se anticipaba. **El wizard ya dejo
`_playerOrigin`/`_playerEyes` correctamente wireados** (a `PlayerAnchor` y `CenterEyeAnchor`) sin
intervencion manual — contrario a lo previsto, no hizo falta wirear nada a mano ahi.

Se seteo manualmente `_agentIndex = 1` en el `NavMeshSurface` de ambos `TeleportInteractable`
(via `manage_components set_property`, usando el nombre completo `Oculus.Interaction.Surfaces.
NavMeshSurface` porque el nombre corto es ambiguo) para que consulten el NavMesh del jugador en vez
del default (indice 0 = Humanoid = el de los NPCs).

**Verificacion real (Play mode, no solo lectura de codigo):** se simulo un `LocomotionEvent`
absoluto llamando `FirstPersonLocomotor.HandleLocomotionEvent(...)` directamente desde
`execute_code` — el `PlayerController` (CharacterController) se movio al punto pedido sin
excepciones, y `PlayerAnchor` (el rig de camara) lo siguio en los frames siguientes. Cero errores en
consola durante toda la sesion de Play.

`ThumbstickLocomotion.cs` quedo recortado: se borraron `HandleTeleport()`, `ComputeArc()`,
`EnsureArcBuffer()`, `ShowAim()`, `HideAim()`, `EnsureReticle()`, `TintReticle()` y todos los campos
de arco/reticle/aim. Se mantuvieron intactos `HandleSmoothMove()`, `HandleTurn()` (snap-turn),
`HandleHeight()`/`HandleCalibration()` (altura) — sin equivalente en el building block. El campo
`Mode` se mantiene para seguir gateando `HandleSmoothMove()` cuando el modo es Teleport (para no
pelear con el gesto de aim del SDK en el mismo stick). `ComfortWirer.cs` no cambio de comportamiento,
solo se actualizo el comentario.

Limpieza de jerarquia: se borro un GameObject huerfano ("HandLeft PointerPose", Transform vacio sin
padre) que quedo como residuo del proceso de instalacion del wizard, y se reparento "Teleport
NavMesh" bajo "Navigation (Player)" por prolijidad (cosmetico — `NavMeshSurface.Transform` no
depende de la jerarquia).

### 9.2 VrLaserPointer — codigo muerto, eliminado

`VrLaserPointer.cs` (raycast manual para el menu) estaba **deshabilitado** (`m_Enabled: 0`) en
`MainMenu.unity` y no existia en absoluto en `Bar.unity` — nada lo reactivaba en runtime, cero
riesgo de doble-fire. El sistema realmente activo ya era el Ray Interaction del SDK
(`RayInteractable` + `PointableCanvas` + `PointableCanvasModule` + `GraphicRaycaster`, ya instalado
y funcionando en `MenuCanvas`), con `MenuButton.cs` implementando los handlers uGUI estandar
(`IPointerClickHandler`/`IPointerEnterHandler`/`IPointerExitHandler`) que ese modulo alimenta.

Se borro `VrLaserPointer.cs`, se removio el componente deshabilitado + su `LineRenderer` asociado de
`RightHandAnchor` en `MainMenu.unity` (via `GameObjectUtility.RemoveMonoBehavioursWithMissingScript`,
mismo patron que la limpieza de `PlayerAnchor.prefab` en la Fase 2), y se corrigieron los dos
comentarios doc obsoletos en `MenuButton.cs` que decian "Called by VrLaserPointer" (ahora describen
correctamente que los llaman los propios handlers uGUI de `MenuButton`, alimentados por el
`PointableCanvasModule`). Verificado en Play mode: el menu sigue resaltando/clickeando botones sin
cambios, sin errores.

### Archivos de esta auditoria

| Accion | Archivo |
|---|---|
| Nuevo | `Assets/2. Scripts/Gameplay/Systems/RuntimePlayerNavMeshBaker.cs` |
| Editado (recorte) | `Assets/2. Scripts/Gameplay/Interactions/ThumbstickLocomotion.cs` |
| Editado (comentario) | `Assets/2. Scripts/Editor/ComfortWirer.cs` |
| Borrado | `Assets/2. Scripts/UI/Menu/VrLaserPointer.cs` |
| Editado (comentarios) | `Assets/2. Scripts/UI/Menu/MenuButton.cs` |
| Escena | `Bar.unity` — building block Teleport (NavMesh) + `Navigation (Player)` baker |
| Escena | `MainMenu.unity` — sacado componente `VrLaserPointer` deshabilitado + su LineRenderer |
