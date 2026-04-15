# Pour Decisions — Demo Auto-Setup

Script: `Assets/2. Scripts/Demo/DemoAutoSetup.cs`

Arma todo el juego desde código cuando das Play. **No es VR, no es óptimo** —
es un mock para visualizar el loop completo en una demo.

---

## 1. Setup (30 segundos)

1. **File → New Scene → Empty** (o empty built-in).
2. Borrá todo de la escena (si quedó algo).
3. **GameObject → Create Empty** → nombralo `[DemoBootstrap]`.
4. Arrastrale el script **`DemoAutoSetup`** (Inspector → Add Component → Demo Auto Setup).
5. **File → Save** la escena (ej. `DemoScene.unity`).
6. **Play**.

Listo. La escena se construye sola: piso, bar, vaso, botella, caja registradora,
clipboard con botones, iluminación, cámara y servicios.

---

## 2. Controles

### Cámara
| Input | Acción |
|---|---|
| **Click derecho + mouse** | Mirar alrededor |
| **W / A / S / D** | Mover horizontal |
| **E / Q** | Subir / Bajar |

### Finger proxy (esfera roja)
- Sigue el mouse en pantalla.
- **Rueda del mouse** = acerca/aleja la esfera de la cámara.
- Sirve para pokear los botones físicos del clipboard si querés mostrar esa interacción.

---

## 3. Panel IMGUI (arriba a la izquierda)

Es el **control principal** de la demo. Simula grab/poke con clicks.

### Info en vivo
- **State**: Idle / NightRunning / NightSummary
- **Cash / Sales / Failed / Expenses / Earn**: se actualizan en tiempo real

### Botones de flujo
| Botón | Qué hace |
|---|---|
| **Begin Night** | Idle → NightRunning (equivale a tocar el botón START del clipboard) |
| **Abort Night** | NightRunning → NightSummary (termina la noche) |
| **Acknowledge Summary** | NightSummary → Idle (persiste save, avanza contador de noche) |

### Botones de testing
| Botón | Qué hace |
|---|---|
| **+$25 Sale** | Registra una venta fake (suma cash y sales) |
| **-$10 Expense** | Registra un gasto fake (resta cash, suma a expenses) |
| **Reset Save** | Borra el progreso guardado y vuelve a cero |

---

## 4. Flujo de demo recomendado (90 segundos)

1. **Play** → mostrar estado **Idle**, clipboard mostrando "Night 1 / Best $0 / $0".
2. Click **Begin Night** → estado cambia a **NightRunning**, clipboard muestra "NIGHT RUNNING".
3. Click **+$25 Sale** ×4 → cash sube a $100, clipboard en vivo.
4. Click **-$10 Expense** ×2 → cash baja a $80.
5. Click **Abort Night** → estado **NightSummary**, clipboard muestra resumen:
   - Cash: $80
   - Sales: 4
   - Failed: 0
   - Expenses: -$20
   - Earnings: +$80
6. Click **Acknowledge Summary** → vuelve a **Idle**, clipboard ahora dice **"Night 2"**, cash persiste en $80, best earnings $80.
7. **Stop y Play de nuevo** → el save persiste. Debe seguir diciendo Night 2 / $80.

Con eso mostrás: state machine, economía, persistencia, UI diegética reactiva, loop completo.

---

## 5. Qué hay en la escena

| GameObject | Qué es |
|---|---|
| `[DemoBootstrap]` | Este script. `DontDestroyOnLoad`. |
| `[UpdatePump]` | Tick de IUpdateListener/IFixedUpdateListener/ILateUpdateListener. |
| `[AudioService]` | Host de las 12 AudioSources del pool (vacías — sin clips). |
| `[DemoCamera]` | Free-fly + AudioListener. |
| `[FingerProxy]` | Esfera roja que sigue el mouse (trigger collider para poke). |
| `[Sun]` | Directional light. |
| `[Floor]` | Plane oscuro. |
| `[Bar]` | Cube marrón (mostrador). |
| `[Glass]` | Cilindro claro + Rigidbody + GrabBridge + líquido ámbar. |
| `[Bottle]` | Cilindro verde + Rigidbody + GrabBridge. |
| `[CashRegister]` | Cubo dorado con `CashRegister.cs` suscrito a Economy. |
| `[NightClipboard]` | Cube tipo clipboard con 3 groups (Idle / Running / Summary), 3 PokeButtons y 8 TMP labels, todo wireado por reflection. |

---

## 6. Limitaciones conocidas

- **Sin sonido**: no hay `SfxDatabase` en Resources, `AudioService` loguea warning y queda en silencio. No afecta nada más.
- **Sin recetas reales**: `DatabaseService` carga de `Resources/Database/Recipes` — si está vacío, `RegisterSale` usa `RecipeId.None` y devuelve 0. Por eso los botones fake de la IMGUI usan `+$25` hardcodeado vía el flujo del servicio (no del price).
- **Sin VR**: todo mouse + teclado. Si tenés headset conectado igual anda, pero la cámara es no-XR.
- **No es build-ready**: primitives + shader `URP/Lit`, IMGUI (descarta para producción). Solo demo.
- **Materiales**: si no estás en URP, cae a `Standard`. Ambos funcionan.

---

## 7. Troubleshooting

| Síntoma | Solución |
|---|---|
| Clipboard vacío / sin texto | TMP Essentials no importados: **Window → TextMeshPro → Import TMP Essential Resources** |
| Magenta everywhere | No estás en URP y el Standard shader tampoco existe. Abrir el script y cambiar `Shader.Find` por uno válido de tu pipeline. |
| Nada se ve | Cámara no está mirando bien. RMB + mouse para rotar, o editar la posición inicial en `BuildCamera()`. |
| "Field X not found on Y" en consola | El nombre del campo privado cambió en el script fuente. Actualizar el `SetPrivate(...)` correspondiente en `BuildClipboard()`. |
| El save persiste entre runs y arruina la demo | Click **Reset Save** antes de empezar. |

---

## 8. Apagar el demo (volver al proyecto real)

- Quitá el component `DemoAutoSetup` del GameObject, o
- Borrá la escena `DemoScene.unity` y abrí la escena `Bar` normal con `GameBootstrap` setup real (ver `UNITY_SETUP.md`).
