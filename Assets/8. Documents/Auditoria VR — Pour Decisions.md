# Auditoría VR — Pour Decisions
### Basada en "Resumen Tecnologías Disruptivas" vs estado actual del proyecto
**Fecha:** 2026-04-22 | **Engine:** Unity 6000.3.11f1 | **Target:** Meta Quest 2/3 + PC VR

---

## Resumen Ejecutivo

El proyecto tiene una base arquitectónica sólida (HSM, Service Locator, ScriptableObjects, URP mobile) pero **no está usando el Meta Interaction SDK** para las interacciones — todo es un sistema custom prototipo. Esto genera gaps críticos en affordance, embodiment y hand tracking. La optimización de rendering tiene buena base (URP configurado, shaders livianos, ASTC) pero faltan pasos fundamentales (static flags, occlusion culling, LODs).

| Área | Estado | Prioridad |
|------|--------|-----------|
| Meta XR SDK Setup | ⚠️ Parcial | Alta |
| Sistema de Grab/Interacciones | ❌ Prototipo | **Crítica** |
| Hand Tracking | ❌ No implementado | Alta |
| Affordance (feedback visual) | ❌ No implementado | Alta |
| Embodiment (poses de mano) | ❌ No implementado | Alta |
| Presencia (SFX feedback) | ⚠️ Parcial | Media |
| UI (diegética/espacial) | ✅ Bien encaminada | Baja |
| Optimización rendering | ⚠️ Parcial | Alta |
| Motion sickness prevention | ✅ Correcto | - |
| Build settings Android | ⚠️ Issues menores | Media |

---

## 1. Los Tres Ejes del Desarrollo VR

### 1.1 Affordance — ❌ NO IMPLEMENTADO

**Qué dice el documento:** *"Los objetos deben explicar solos cómo se usan. Si ves una palanca, tiene que invitar a agarrarla."*

**Estado actual:**
- No hay hover state, glow, outline ni ninguna señal visual de que un objeto es agarrable
- `GrabBridge` expone eventos `_onGrabbed` / `_onReleased` pero nada los usa para feedback visual
- No hay `PointableUnityEventWrapper` ni `HandGrabGlow` de Meta
- El jugador no tiene forma de saber qué objetos puede agarrar sin probar uno por uno

**Implementación requerida:**
1. Agregar `HandGrabGlow` o shader custom de rim/outline en objetos interactivos
2. Implementar `PointableUnityEventWrapper` con eventos `WhenHover` / `WhenUnhover`
3. Cambio de material o emisión al acercarse a un objeto agarrable
4. Considerar un brillo sutil permanente en botellas/vasos para guiar la atención

---

### 1.2 Presencia — ⚠️ PARCIAL

**Qué dice el documento:** *"El mundo tiene que responder como esperás: buena escala, sonido consistente, física creíble."*

**Estado actual:**
- ✅ Física de líquidos funcional (PourDetector, LiquidContainer, PourStream)
- ✅ Sistema de breakables para destrucción física
- ✅ Audio de pouring implementado via IAudioService
- ❌ Sin SFX de grab/release (agarrar una botella es silencioso)
- ❌ NPCs no tienen animaciones — el jugador no puede distinguir visualmente si un cliente está esperando, tomando o borracho solo mirándolo
- ❌ El sistema de borrachera solo mueve al NPC lateralmente al irse, sin feedback visual mientras está sentado (sin sway, sin cambio de color, sin balbuceo)
- ⚠️ Paciencia del cliente solo se refleja en el TicketView (pizarra), no en el NPC mismo

**Implementación requerida:**
1. Agregar SFX de clink/vidrio al grab/release de botellas y vasos
2. Conectar Animator a los estados del customer (idle, impaciente, bebiendo, borracho)
3. Agregar feedback visual de borrachera progresiva: shader tint, sway idle, partículas
4. Hacer que el NPC muestre impaciencia (golpear la barra, mirar el reloj) cuando la paciencia baja

---

### 1.3 Embodiment — ❌ NO IMPLEMENTADO

**Qué dice el documento:** *"Las manos virtuales tienen que moverse como las tuyas, sin delay raro ni animaciones que te saquen control."*

**Estado actual:**
- `SimpleVRGrabber` es un grabber prototipo que usa `OverlapSphereNonAlloc` con radio 8cm
- No hay representación visual de manos (ni sintéticas, ni mesh)
- No hay `HandGrabPose` para ningún objeto — los dedos no se adaptan a botellas ni vasos
- No hay throw velocity — al soltar un objeto cae con velocidad cero
- Hand tracking deshabilitado en `OculusProjectConfig` (`handTrackingSupport: 0`)

**Implementación requerida:**
1. Reemplazar `SimpleVRGrabber` con Meta Interaction SDK (`HandGrabInteractable` + `Grabbable`)
2. Agregar `OVRControllerDrivenHands` al Camera Rig
3. Crear al menos 3 `HandGrabPose`:
   - Agarre cilíndrico para botellas
   - Agarre wrap para vasos
   - Pinch para objetos pequeños (monedas, tapas)
4. Configurar `Fingers Freedom` por pose (dedos restringidos, no bloqueados)
5. Crear poses espejo con "Create Mirror Hand Grab Interactable"
6. Restaurar throw velocity con `OVRInput.GetLocalControllerVelocity()` al soltar

---

## 2. Diseño UI e Inmersión

### 2.1 UI Diegética/Espacial — ✅ BIEN ENCAMINADA

**Qué dice el documento:** *"La mejor UI es la que casi no parece UI."*

**Estado actual:**
- ✅ Todos los Canvas son `WorldSpace` — no hay Screen Space Overlay
- ✅ `NightClipboard` reemplazó un viejo screen-space view por una pizarra física agarrable con PokeButtons
- ✅ `TextMeshPro` usado en todo: OrdersBoard, CashRegister, TicketView, NightClipboard
- ✅ `GlassFillBar` usa billboard hacia la cámara (buen patrón para indicadores de fill)
- ⚠️ No hay validación de distancia/ángulo cómodo para los canvas WorldSpace
- ⚠️ No hay enforcement de tamaño mínimo de fuente para legibilidad VR

**Mejoras sugeridas:**
1. Validar en Editor que todos los canvas WorldSpace estén entre 0.8m y 2m del jugador
2. Asegurar que las fuentes TMP sean mínimo 36pt equivalente a la distancia de lectura
3. Agregar contraste alto en toda la UI (fondo semi-opaco detrás de texto)

---

## 3. Bienestar del Usuario

### 3.1 Motion Sickness — ✅ CORRECTO

**Qué dice el documento:** *"Mantener 72-90 FPS estables. Gameplay estacionario reduce mareo."*

- ✅ Jugador estacionario detrás de la barra (`PlayerAnchor` fijo)
- ✅ Sin locomoción con joystick
- ✅ Sin movimiento de cámara involuntario
- ✅ Sin cinemáticas con cámara automática
- ✅ El diseño del juego es inherentemente anti-motion-sickness

### 3.2 Rendimiento para Bienestar — ⚠️ RIESGOS

- ❌ Sin Occlusion Culling bakeado — se renderiza geometría oculta
- ❌ Sin LODs — todo se renderiza a máxima resolución siempre
- ❌ Sin objetos marcados como Static — no hay static batching
- ⚠️ Una luz Point dinámica con sombras real-time activa
- Estos problemas combinados pueden causar drops de framerate que producen malestar físico

---

## 4. Movimientos e Interacciones

### 4.1 Locomoción — ✅ N/A (Correcto)

El juego es estacionario. No necesita teleportación ni snap turning. Decisión de diseño correcta.

### 4.2 Hand Tracking — ❌ DESHABILITADO

**Qué dice el documento:** *"Uso de HandTracking y gestos para agarrar o interactuar con objetos."*

- `handTrackingSupport: 0` en OculusProjectConfig
- No hay `OVRHand`, `OVRSkeleton` ni `HandVisual` en el proyecto
- No hay soporte multimodal (manos + controladores simultáneamente)

**Implementación requerida:**
1. Habilitar `handTrackingSupport: 2` (Controllers and Hands)
2. Agregar Building Blocks: Hand Tracking + Virtual Hands
3. Configurar OVRManager: `Controller Driven Hand Type: Conform to Controller`
4. Activar `Simultaneous Hands and Controllers`
5. Agregar `OVRControllerDrivenHands` prefab al rig de interacciones

### 4.3 Poses de Agarre — ❌ NO EXISTEN

**Qué dice el documento:** *"Crear poses personalizadas para que las manos virtuales se adapten a la forma del objeto."*

- Zero `HandGrabPose` en todo el proyecto
- Los dedos nunca se adaptan a ningún objeto
- No hay poses espejo para la otra mano

---

## 5. Optimización Unity para VR

### 5.1 Occlusion Culling — ❌ NO CONFIGURADO

**Qué dice el documento:** *"Evita renderizar objetos tapados por otros. Se configura desde Window > Rendering > Occlusion Culling."*

- Ningún objeto tiene Static flags activados
- No hay bake de occlusion culling
- En un bar con paredes y mostrador, esto es una optimización significativa desperdiciada

### 5.2 LOD (Level of Detail) — ❌ NO IMPLEMENTADO

**Qué dice el documento:** *"Varias versiones del mismo objeto con distinta cantidad de polígonos."*

- No hay LODGroup en ningún prefab ni objeto de la escena Bar
- Botellas, vasos, clientes — todos se renderizan a máxima resolución siempre

### 5.3 Mip Maps — ⚠️ NO VERIFICABLE

- No hay texturas de juego importadas aún (solo TextMesh Pro)
- `globalTextureMipmapLimit: 0` configurado (sin stripping forzado)
- Streaming de mipmaps desactivado — aceptable para escena pequeña

### 5.4 Compresión de Audio — ⚠️ NO VERIFICABLE

- No hay archivos de audio en el proyecto aún
- El sistema de audio (IAudioService, SfxDatabase) está preparado pero sin assets

### 5.5 Compresión de Texturas — ✅ CORRECTO

- ASTC configurado como formato por defecto para Android (correcto para Quest)

### 5.6 Luces Baked vs Dinámicas — ⚠️ ISSUE

**Qué dice el documento:** *"Las luces dinámicas son caras. Usar Light Baking y marcar objetos como Static."*

- Una luz Point dinámica con sombras real-time detectada
- Ningún objeto marcado como Static para baking
- Para un bar (ambiente mayormente estático), baked lighting es ideal

### 5.7 Atlas de Texturas — ⚠️ NO VERIFICABLE

- Sin texturas de juego importadas aún

### 5.8 Agrupación de Mallas — ⚠️ PARCIAL

- SRP Batcher activo (bueno, agrupa automáticamente por shader variant)
- Static batching inoperante por falta de Static flags
- Los shaders custom (Liquid, LiquidStream) declaran CBUFFER correctamente para SRP Batcher ✅

---

## 6. Setup Meta XR SDK

| Config | Estado | Acción |
|--------|--------|--------|
| Meta XR All-in-One SDK v85 | ✅ Importado | - |
| OVRCameraRig en escena | ✅ Presente | Verificar OVRManager settings en Editor |
| Hand Tracking | ❌ Deshabilitado | Habilitar en OculusProjectConfig |
| Virtual/Synthetic Hands | ❌ Ausente | Agregar Building Block |
| Controller support | ✅ Funcional | - |
| Passthrough / MR | ❌ Deshabilitado | Decisión de diseño — OK si es VR puro |
| Bundle ID | ❌ Template default | Cambiar `com.UnityTechnologies...` a ID propio |
| Graphics API | ⚠️ Vulkan presente | Remover Vulkan, dejar solo OpenGL ES3 |
| Single Pass Instanced | ✅ Activo | - |
| IL2CPP | ✅ Activo | - |
| ARM64 only | ✅ Correcto | - |
| Min SDK 29 | ✅ Correcto | - |

---

## 7. Plan de Implementación Priorizado

### Fase 1 — Críticos (Bloquean calidad VR mínima)

| # | Tarea | Área | Esfuerzo |
|---|-------|------|----------|
| 1 | Reemplazar `SimpleVRGrabber` con Meta Interaction SDK | Embodiment | Alto |
| 2 | Agregar `HandGrabInteractable` + `Grabbable` a botellas y vasos | Embodiment | Medio |
| 3 | Crear `HandGrabPose` para botella (cilíndrico) y vaso (wrap) | Embodiment | Medio |
| 4 | Habilitar Hand Tracking + Virtual Hands | Hand Tracking | Medio |
| 5 | Marcar geometría estática como Static (paredes, barra, estantes) | Optimización | Bajo |
| 6 | Bakear Occlusion Culling | Optimización | Bajo |
| 7 | Remover Vulkan de Graphics APIs | Build Config | Bajo |
| 8 | Cambiar Bundle ID del template default | Build Config | Bajo |

### Fase 2 — Importantes (Mejoran significativamente la experiencia)

| # | Tarea | Área | Esfuerzo |
|---|-------|------|----------|
| 9 | Agregar hover glow/outline a objetos interactivos | Affordance | Medio |
| 10 | SFX de grab/release (clink vidrio, golpe madera) | Presencia | Bajo |
| 11 | Animaciones de NPC por estado (idle, impaciente, bebiendo) | Presencia | Alto |
| 12 | Feedback visual de borrachera progresiva en NPCs sentados | Presencia | Medio |
| 13 | Bakear iluminación + convertir point light a Mixed/Baked | Optimización | Medio |
| 14 | Agregar LOD Groups a prefabs de botellas/vasos/NPCs | Optimización | Medio |
| 15 | Configurar multimodal (manos + controllers simultáneos) | Interacciones | Medio |

### Fase 3 — Polish

| # | Tarea | Área | Esfuerzo |
|---|-------|------|----------|
| 16 | Throw velocity al soltar objetos | Interacciones | Bajo |
| 17 | Poses de agarre para objetos secundarios (monedas, tapas) | Embodiment | Bajo |
| 18 | Validar distancias de UI WorldSpace | UI | Bajo |
| 19 | Mip Maps y compresión correcta al importar texturas finales | Optimización | Bajo |
| 20 | Audio compression policy (WAV cortos, OGG largos) | Optimización | Bajo |

---

## 8. Lo que Está Bien

No todo es deuda técnica. Estas decisiones son sólidas:

- **Arquitectura de código** — HSM, Service Locator, SO-driven data. Limpia y extensible.
- **URP Mobile configurada** — Render scale 0.8, SRP Batcher activo, shaders ultra-livianos.
- **Shaders custom** — `Liquid.shader` y `LiquidStream.shader` son óptimos para tile-based GPU de Quest.
- **UI WorldSpace con TMP** — Correctamente implementada, sin HUD overlay.
- **Gameplay estacionario** — Elimina motion sickness por diseño.
- **Single Pass Instanced Stereo** — Correcto para Quest.
- **ASTC textures + IL2CPP + ARM64** — Build pipeline correcto.
- **Sistema de líquidos** — PourDetector, LiquidContainer, PourStream con audio integrado.
- **NightClipboard** — Excelente ejemplo de UI diegética (pizarra física agarrable).
