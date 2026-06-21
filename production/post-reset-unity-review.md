# Revisión post-reset — Verificación en Unity (con MCP)

> **Para la próxima sesión de Claude (con MCP de Unity conectado).**
> Todo lo de abajo se implementó **editando archivos directamente en disco** (sin Unity abierto, sin MCP). Falta la verificación dentro del engine: compilar, entrar en Play y confirmar que todo quedó bien conectado. Este doc lista qué revisar y cómo, con resultado esperado y fix si algo falla.

## Contexto: qué se hizo (4 fases + 1 fix)

Pasada de "feedback sensorial" sobre el VR bar game (Unity 6.0.3, Meta XR/Quest):

- **Fase 1** — Audio (6 SFX que estaban mudos), Haptics (servicio nuevo), Animación de cliente (CrossFade por movimiento + borrachera).
- **Fase 2** — Partículas (`VfxService` construido por código) + hooks (splash/shatter/serve/coins).
- **Fase 3** — Atmósfera (`AtmosphereService`: post-proceso bloom/grading, mood lighting, blink de noche).
- **Pendientes** — Ambiente de bar (loop), pasos de NPC, partículas con textura suave.
- **Fix** — CS0118 en `AtmosphereService` (alias `GameStateId`).

Ver también la memoria `sensory-feedback-roadmap.md` y `gamestate-namespace-collision.md`.

---

## ✅ Checklist de verificación

### 1. Compilación (lo primero)
- **MCP:** refrescar/recompilar (AssetDatabase.Refresh) y leer la consola.
- **Esperado:** 0 errores. El CS0118 de `GameState` ya está resuelto con alias.
- **Si falla:** mirar errores nuevos. Sospechar choques de nombres en clases bajo `Services.*` (ver `gamestate-namespace-collision.md`).

### 2. Wiring de audio (SfxDatabase) — el check más importante
- **Qué:** `Assets/Resources/Database/SfxDatabase.asset` referencia 8 clips por GUID. Confirmar que **ninguno** quedó en *None* tras importar.
- **MCP:** abrir el asset / inspeccionar los `AudioClip` de cada entrada, o entrar en Play y disparar cada evento.
- **IDs → clip esperado:**
  | id | SfxId | archivo |
  |----|-------|---------|
  | 10 | PourLoop | PourLoop.wav |
  | 20 | GlassBreak | GlassBreak.wav |
  | 21 | BottleBreak | BottleBreak.wav |
  | 30 | CashSale | CashSale.wav |
  | 31 | CashExpense | CashExpense.wav |
  | 60 | ButtonPress | ButtonPress.wav |
  | 70 | Footstep | Footstep.wav |
  | 110 | BarAmbience | BarAmbience.wav |
- **Si falla (clip = None):** GUID del `.meta` ≠ GUID en el `.asset`. Reasignar el clip en el inspector o corregir el GUID.

### 3. Registro de servicios (runtime)
- **Qué:** `GameBootstrap` registra `IHapticService`, `IVfxService`, `IAtmosphereService`. **Orden crítico:** `AtmosphereService` debe registrarse **después** de `GameStateService` (se suscribe a él en `Initialize()`).
- **MCP:** entrar en Play desde la escena **Boot** (no directo a Bar, para que corra el bootstrap) y revisar la consola: sin warnings de ServiceLocator.
- **Esperado:** los 3 servicios inicializan sin error.

### 4. Verificación por feature (Play en escena Bar)

**Audio**
- [ ] Servir un trago → suena loop de pour.
- [ ] Romper vidrio/botella → suena rotura.
- [ ] Venta/gasto → suena caja.
- [ ] Apretar botón diegético → suena click.
- [ ] **Ambiente:** murmullo de fondo bajo, todo el tiempo en el bar.
- [ ] **Pasos:** cliente caminando → pasos posicionales; quieto → silencio.

**Haptics** (requiere Quest/headset)
- [ ] Vibra al agarrar/soltar, al servir (mano que sostiene), al romper (ambas), al botón, al servir cliente.

**Animación de cliente** (Human)
- [ ] Camina con animación Walking (no se desliza en Idle).
- [ ] Se tambalea (Drunk/Drunk Walk) si está borracho.
- [ ] ⚠️ Si el cuerpo se "desliza" separándose del root → revisar que `CustomerHuman` tenga `ApplyRootMotion = 0` (estaba en 0) y que los clips sean in-place.

**Partículas**
- [ ] Splash al servir (color del líquido), shatter al romper, sparkle verde al servir OK, monedas doradas en la venta.
- [ ] Se ven **redondas/suaves** (textura radial), no cuadradas.

**Atmósfera / post-proceso**
- [ ] Iniciar noche → **blink** (parpadeo a negro) + bar más cálido/tenue + bloom en luces.
- [ ] Terminar noche (NightSummary) → otro blink + vuelta a iluminación neutra.
- [ ] La **viñeta de confort** sigue andando al moverse con thumbstick (no se pisa con el grading).
- [ ] **Dependencia:** post-proceso requiere *Post Processing* activo en la cámara URP + Volume global. Como la viñeta ya funcionaba, debería estar OK. Si no se ve bloom/grading, revisar ahí.

### 5. Performance (Quest)
- **MCP/Profiler:** medir frame time en noche con clientes.
- **Sospechoso #1:** Bloom. Si baja el framerate, ajustar `NightBloom`/`BloomThreshold` o desactivar en `AtmosphereService.cs`.
- Partículas: bursts chicos y poolizados; bajar counts en `VfxService.cs` si hace falta.

---

## Inventario de archivos

**Nuevos**
- Audio: `Assets/6. Audio/{ButtonPress,CashSale,CashExpense,PourLoop,GlassBreak,BottleBreak,BarAmbience,Footstep}.wav` (+ `.meta`)
- `Assets/2. Scripts/Services/Haptics/{IHapticService,HapticService}.cs`
- `Assets/2. Scripts/Services/Vfx/{IVfxService,VfxService}.cs`
- `Assets/2. Scripts/Services/Atmosphere/{IAtmosphereService,AtmosphereService}.cs`
- `Assets/2. Scripts/Data/Enums/VfxId.cs`
- `production/tmp/gen_sfx.py` (generador de SFX, temporal; fuera de Assets)

**Editados**
- `Assets/Resources/Database/SfxDatabase.asset` (8 clips wireados)
- `Assets/2. Scripts/Data/Enums/SfxId.cs` (Footstep=70, BarAmbience=110)
- `Assets/2. Scripts/Core/Bootstrap/GameBootstrap.cs` (registro de servicios)
- `Assets/2. Scripts/Gameplay/Interactions/{GrabBridge,SimpleVRGrabber}.cs`
- `Assets/2. Scripts/Gameplay/Liquid/PourDetector.cs`
- `Assets/2. Scripts/Gameplay/Systems/Breakable.cs` (+ campo `_breakVfxColor`)
- `Assets/2. Scripts/UI/Diegetic/{PokeButton,MusicController,ProgressionBoard}.cs`
- `Assets/2. Scripts/Gameplay/Customer/States/WaitingState.cs`
- `Assets/2. Scripts/Gameplay/Customer/CustomerEntity.cs`

---

## Notas / limitaciones conocidas
- **Animación de beber: NO implementada** — no existe clip de "beber" (solo Idle/Walking/Drunk/Drunk Walk). Necesita un asset real (Mixamo/grabado), luego se cablea como las demás (CrossFade) o trigger temporal en `WanderingState`.
- **SFX sintéticos** (placeholder): los breaks y demás se generaron por código (`gen_sfx.py`). Reemplazables por CC0/grabados.
- **Partículas:** quads con textura radial generada por código; para gotas/chispas más lindas, asignar una textura/material custom al `VfxService`.
- **Servicios sin tuning en inspector:** son clases planas; constantes en código (`VfxService.cs`, `HapticService.cs`, `AtmosphereService.cs`). Convertir a MonoBehaviour si se quiere tunear en el editor.
- **Diferido:** reflejos/SSAO (caro en Quest).

## Campos `[SerializeField]` nuevos (defaults via inline initializer)
Estos se agregaron a componentes existentes; Unity aplica el default del código a instancias ya serializadas. Revisar/tunear opcionalmente:
- `Breakable._breakVfxColor` (gris-claro; poner ámbar en prefabs de botella si se quiere)
- `CustomerEntity._drunkAnimThreshold` (0.4), `_walkSpeedThreshold` (0.05), `_strideLength` (0.55)
- `MusicController._ambienceVolume` (0.3)
