
# Game Design Document — Pour Decisions

**Versión:** 1.3
**Fecha:** 2026-05-04
**Género:** Simulación de gestión (Management Sim) en VR con física interactiva
**Engine:** Unity 6 (6000.3.11f1) + URP
**Plataformas:** Meta Quest 2/3 (standalone) + PC VR (SteamVR/OpenXR)

---

## 1. Concepto del juego

### 1.1 Premisa

Pour Decisions es un juego de simulación en realidad virtual donde el jugador es el barman de su propio bar. Deberá preparar tragos con sus propias manos, atender clientes, gestionar la economía del negocio y lidiar con las consecuencias del alcohol — clientes borrachos, efectos secundarios, y botellas rotas.

### 1.2 Argumento base

El jugador está detrás de la barra. Los clientes llegan, piden, y hay que cumplir. Preparar tragos, servir, cobrar, y sobrevivir la noche. No hay historia: hay laburo, caos, y la caja registradora al final del día.

### 1.3 Qué lo hace único

- Cada trago se prepara con las manos en VR — no hay menús, hay botellas.
- La borrachera es una mecánica central: el estado del cliente cambia el gameplay en tiempo real.
- El caos es parte del diseño: botellas rotas, clientes descontrolados, noches que se van de las manos.
- Loop de gestión integrado: lo que hacés con las manos impacta tu billetera.

### 1.4 Público objetivo

- Jugadores de VR que buscan simuladores interactivos (Job Simulator, Bartender VR, Overcooked).
- Público casual que disfruta gestión + humor + caos físico (Surgeon Simulator, PlateUp!).
- Rango etario: 16+.

---

## 2. Core Gameplay Loop

El juego tiene **dos escenas principales** que se alternan:

```
[Escena Día] → Abrir local → [Escena Noche: 8PM–5AM] → Cierre → [Escena Día] → ...
```

**Escena de Día (Management):**
- Comprar nuevas bebidas/ingredientes con la plata acumulada
- Comprar mejoras para el bar
- Consultar el catálogo de compras (clipboard/catálogo físico sobre el mostrador)
- Prepararse para la próxima noche

**Escena de Noche (Gameplay activo):**
- Atender clientes y preparar tragos
- Recaudar plata
- Manejar el caos

### 2.1 Flujo de una noche (detalle)

1. **Apertura:** El jugador está en la escena de día. Golpea la **campana de servicio** en la barra — suena un "ding" satisfactorio con feedback háptico y se dispara la transición a la noche. Arranca a las 8PM (horario de juego).
2. **Servicio:** Los clientes aparecen y se sientan en los asientos disponibles (máximo 3 simultáneos). Aparece su pedido visible (nombre del trago + proporción).
3. **Preparación:** El jugador agarra botellas y prepara el trago (ver sección 3.1 para las dos vías de preparación).
4. **Servir:** El trago se vierte/coloca en el vaso que está frente al cliente. El sistema valida la receta.
5. **Feedback:** El cliente reacciona según la calidad del trago (ver sección 3.5 Satisfacción).
6. **Timer de paciencia:** Si no le das la bebida a tiempo, el cliente se va enojado. La paciencia se muestra como una **barra que se agota encima de su cabeza**.
7. **Acumulación:** Los clientes se van acumulando (máximo 3 sentados al mismo tiempo).
8. **Fin de noche:** El reloj de juego llega a las 5AM — la noche termina automáticamente. Se recauda lo acumulado.
9. **Cierre:** Transición de vuelta a la escena de día.

**Apertura del local:** Campana de servicio física en la barra. Patrón estándar en VR sims (Job Simulator, Cook-Out) — interacción física simple, satisfactoria, y temática.

**Tienda (escena de día):** Catálogo/clipboard físico sobre el mostrador. El jugador ve los ítems disponibles y selecciona con poke lo que quiere comprar.

**Asientos:** 3 fijos para el MVP. Configurable desde Editor (no hardcoded) — puede escalar en noches futuras.

**Duración real:** 10 minutos (configurable desde el Editor). 8PM–5AM = 9 horas de juego ≈ 10 minutos reales. 1 hora de juego ≈ 67 segundos reales.

---

## 3. Mecánicas

### 3.1 Preparación de tragos

Crafting físico en VR con **dos vías de preparación** según el tipo de trago:

**Vía A — Pour directo (tragos simples, ej: Fernet con Coca):**
1. Agarrar la botella del ingrediente 1 (ej: Fernet)
2. Verter directamente en el **vaso que está frente al cliente**
3. Agarrar la botella del ingrediente 2 (ej: Coca Cola)
4. Verter en el mismo vaso hasta la proporción deseada
5. El cliente toma el vaso automáticamente — el jugador no necesita mover el vaso

**Vía B — Mezclador/Shaker (tragos mezclados, ej: Gin Tonic):**
1. Agarrar la botella del ingrediente 1 (ej: Gin)
2. Verter en el **mezclador/shaker**
3. Agarrar la botella del ingrediente 2 (ej: Tónica)
4. Verter en el mezclador hasta la proporción deseada
5. **Agarrar el shaker y agitar** (gesto de shake en VR)
6. Verter del shaker al vaso del cliente
7. El cliente toma el vaso

**El vaso siempre está pre-posicionado frente al cliente** (en el serve socket del asiento). El jugador nunca necesita agarrar ni mover el vaso — solo vierte el contenido.

### 3.2 Sistema de proporciones

**Las proporciones importan, pero son discretas.** El vertido es continuo (el jugador inclina la botella libremente y ve el líquido subir) pero la **validación al servir redondea a la proporción válida más cercana** con una tolerancia de ±10%.

**Marcas visuales en el vaso/shaker:** Líneas o zonas que dividen el contenedor en regiones (~30%, ~50%, ~70%) para guiar al jugador.

**Proporciones válidas:**

| Palabra | Proporción | Significado |
|---------|------------|-------------|
| **Fuerte** | 70/30 | Más alcohol que mixer |
| **Mitad** | 50/50 | Partes iguales |
| **Liviano** | 30/70 | Más mixer que alcohol |
| **Puro** | 100% | Un solo ingrediente |

**Cómo pide el cliente:** Muestra el nombre del trago + la proporción como palabra y/o representación visual.
Ejemplo: "Fernet con Coca — Fuerte" (con una imagen del vaso mostrando 70/30).

**Validación al servir:**
- Si los ingredientes y la proporción coinciden con el pedido → **Le gustó**
- Si los ingredientes son correctos pero la proporción se desvía → **Meh**
- Si los ingredientes son incorrectos → **No le gustó**

### 3.3 Recetas del MVP

**Recetas iniciales (2 tragos):**

| Trago | Ingredientes | Preparación | Proporciones posibles |
|-------|-------------|-------------|----------------------|
| **Fernet con Coca** | Fernet + Coca Cola | Pour directo | Fuerte, Mitad, Liviano |
| **Gin Tonic** | Gin + Tónica | Mezclador | Fuerte, Mitad, Liviano |

**Ingredientes del MVP (4):**

| Ingrediente | Tipo | Usado en |
|-------------|------|----------|
| Gin | Alcohol | Gin Tonic |
| Tónica | Mixer | Gin Tonic |
| Fernet | Alcohol | Fernet con Coca |
| Coca Cola | Mixer | Fernet con Coca |

**Equipo del bar MVP:**

| Equipo | Función |
|--------|---------|
| Botellas (4) | Ingredientes en la estantería |
| Mezclador/Shaker (1) | Para tragos que requieren mezcla |
| Vasos (en serve sockets) | Pre-posicionados frente a cada asiento |
| Campana de servicio | Para abrir la noche |
| Caja registradora | Para cerrar la noche |

**El jugador arranca con $0 y 1–2 recetas disponibles.** Más recetas e ingredientes se compran en la tienda de día con la plata acumulada.

### 3.4 Sistema de clientes

Los clientes son NPCs que llegan al bar, se sientan, piden **un solo trago** y se van.

**Flujo del cliente (FSM):**
```
Approaching → Waiting (pide trago + proporción) → Drinking → Leaving
```

**Cada cliente pide:**
- Un trago específico (ej: Fernet con Coca)
- Con una proporción específica (ej: Fuerte → 70/30)

**Display del pedido:** Visible sobre la cabeza del cliente — nombre del trago + proporción (palabra y/o visual).

**Cada cliente tiene:**
- **Paciencia** (barra visible que se agota sobre su cabeza — si llega a 0, se va enojado)
- **Pedidos posibles** (lista de recetas que puede pedir)
- **Rango de propina**

**Máximo simultáneo:** 3 clientes sentados al mismo tiempo.
**Pedido por cliente:** 1 trago. Después de tomarlo (o irse), libera el asiento para el siguiente.

### 3.5 Satisfacción del cliente

La reacción del cliente depende de **qué tan bien coincide lo servido con lo pedido** (trago correcto + proporción correcta).

**Resultados posibles al servir:**

| Resultado | Causa | Consecuencia económica |
|-----------|-------|----------------------|
| **Le gustó** | Trago correcto con la proporción pedida | Pago completo + propina |
| **Meh** | Trago correcto pero proporción equivocada | Pago reducido, sin propina (o propina mínima) |
| **No le gustó** | Trago equivocado o ingredientes incorrectos | Pago mínimo o nulo |
| **Se fue (timeout)** | Tardaste demasiado (paciencia agotada) | Nada — el cliente se va enojado |

**Ejemplo:** Un cliente pide "Fernet con Coca — Fuerte (70/30)". Si le das Mitad (50/50) → Meh. Si le das Gin Tonic → No le gustó. Si se agota su barra de paciencia → Se fue.

### 3.6 Sistema de borrachera

La borrachera es una mecánica central: el nivel de alcohol que el jugador pone en el trago determina qué tan borracho sale el cliente. Más alcohol = más propina, pero más caos. Es la decisión de riesgo/recompensa principal del juego.

**Solo los clientes se emborrachan** — nunca el jugador (efectos en el jugador = motion sickness en VR).

#### Cálculo de borrachera

La borrachera se calcula a partir de la cantidad de alcohol servido en el trago:

**D = alcoholMl / alcoholMlForMax** (clampado a [0, 1])

- `alcoholMl`: mililitros de ingredientes tipo Alcohol en el vaso servido
- `alcoholMlForMax`: umbral configurable (default: 60 ml) que corresponde a borrachera máxima

| El jugador sirve... | D aproximado | Nivel |
|---------------------|-------------|-------|
| Pure (solo mixer) | 0.0 | Sobrio |
| Liviano 30/70 | ~0.5 | Achispado |
| Mitad 50/50 | ~0.83 | Borracho |
| Fuerte 70/30 | 1.0 | Muy Borracho |

#### Niveles de borrachera

4 niveles discretos definidos como umbrales sobre el float D:

| Nivel | Nombre | Rango D | Descripción |
|-------|--------|---------|-------------|
| 0 | Sobrio | 0.00–0.29 | Comportamiento normal. Sin efectos. |
| 1 | Achispado | 0.30–0.69 | Cliente alegre, ligeramente inestable. |
| 2 | Borracho | 0.70–0.99 | Cliente impredecible, ruidoso. |
| 3 | Muy Borracho | 1.00 | Máxima intensidad. Caos activo. |

#### Efectos por nivel

| Efecto | L0 Sobrio | L1 Achispado | L2 Borracho | L3 Muy Borracho |
|--------|-----------|-------------|-------------|-----------------|
| **Visual** | Normal | Sway leve sentado | Sway exagerado + cabeza | Trastabilla, casi se cae |
| **Propina** | x1.0 | x1.1 | x1.3 | x1.5 |
| **Velocidad bebida** | x1.0 | x0.8 (bebe rápido) | x0.6 | x0.5 (bebe urgente) |
| **Riesgo rotura vaso** | 0% | 10% | 25% | 45% |
| **Wobble al irse** | Ninguno | Leve | Notable | Máximo |
| **SFX** | — | — | Balbuceo | Grito/queja |

#### Loop de tensión central

Servir tragos fuertes = más propina pero más riesgo de que el cliente rompa el vaso (que cuesta plata). El jugador decide conscientemente en cada trago: ¿maximizo ganancia o evito el caos?

- **Loop positivo:** Más alcohol → más propina. Incentiva servir fuerte.
- **Loop negativo:** Más borrachera → más rotura de vasos → menos vasos disponibles → menos ingresos. Limita naturalmente el abuso.

#### Integración con el FSM (sin nuevos estados)

Los efectos se implementan como modificadores en los estados existentes del cliente:

- **Waiting:** Calcula D y L al recibir el trago.
- **Drinking:** `DrinkTimer *= drinkSpeedMult[L]` — los borrachos beben más rápido.
- **Leaving:** Roll de rotura (`Random < breakRisk[L]`), wobble proporcional a D, SFX si L≥2.
- **Propina:** Se multiplica por `tipMultiplier[L]` al calcular el pago.

#### Configuración (DrunkennessConfigSO)

Todos los valores son configurables desde el Inspector sin tocar código:

| Campo | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `alcoholMlForMax` | float | 60 | ml de alcohol para D=1.0 |
| `levelThresholds` | float[3] | [0.30, 0.70, 1.00] | Umbrales para L1, L2, L3 |
| `tipMultipliers` | float[4] | [1.0, 1.1, 1.3, 1.5] | Multiplicador de propina por nivel |
| `breakRisks` | float[4] | [0.0, 0.10, 0.25, 0.45] | Probabilidad de rotura por nivel |
| `drinkSpeedMult` | float[4] | [1.0, 0.8, 0.6, 0.5] | Multiplicador de DrinkTimer por nivel |
| `wobbleAmplitude` | float | — | Amplitud del wobble al caminar |
| `wobbleFrequency` | float | — | Frecuencia del wobble |

### 3.7 Economía

**Objetivo principal:** Recaudar plata durante la noche.

**Dinero inicial:** $0. El jugador arranca sin nada y acumula desde la primera noche.

**Persistencia:** La plata se acumula entre noches. Lo que recaudás en una noche lo tenés disponible en la escena de día.

**Ingresos (solo durante la noche):**
- Venta de tragos (precio base de la receta)
- Propinas (basadas en calidad del servicio + proporción correcta)

**Gastos:**
- Compra de nuevas bebidas/ingredientes (escena de día)
- Reparación de daños (botellas rotas)
- Mejoras para el bar (post-MVP)

**No se puede perder:** De momento no hay game over. En el peor caso, no recaudás nada durante una noche.

**Progresión económica:** La plata acumulada se usa en la tienda de día para comprar nuevos ingredientes → nuevas recetas → más variedad de pedidos → más complejidad.

### 3.8 Destrucción física

De momento solo se rompen **botellas y vasos** al recibir un impacto fuerte.
- Se hace swap a un prefab pre-fracturado (3-5 chunks) del pool.
- Cada rotura tiene un costo económico.

*TBD: Expandir a mobiliario u otros objetos en el futuro.*

### 3.9 Caja registradora

Al final de la noche, el jugador interactúa con la caja registradora (botón físico diegético).
Sale un ticket impreso con el resumen: ingresos, gastos por daños, total.

---

## 4. Contenido del MVP

| Elemento | Cantidad | Detalle |
|----------|----------|---------|
| Noches jugables | Sin límite | 8PM–5AM (10 min reales, configurable) |
| Recetas iniciales | 1–2 | Fernet con Coca (pour directo), Gin Tonic (mezclador) |
| Ingredientes iniciales | 4 | Gin, Tónica, Fernet, Coca Cola |
| Proporciones | 4 | Fuerte (70/30), Mitad (50/50), Liviano (30/70), Puro (100%) |
| Clientes simultáneos | 3 | 3 asientos fijos (configurable) |
| Escenas | 2 | Día (management/tienda) + Noche (gameplay) |
| Dinero inicial | $0 | Acumula entre noches |
| Equipo | 5 tipos | Botellas, shaker, vasos, campana, caja registradora |
| Mecánicas activas | — | Preparar (2 vías), servir, cobrar, romper, borrachera |

---

## 5. Escenario: El Bar

El jugador está fijo detrás de la barra (gameplay estacionario — previene motion sickness).

**Elementos del entorno:**
- **Barra/mostrador:** Superficie de trabajo principal
- **Estantería de botellas:** Donde el jugador agarra los ingredientes
- **Mezclador/Shaker:** Sobre la barra, para tragos que requieren mezcla
- **Asientos (3):** Frente a la barra, con vaso pre-posicionado en cada uno (serve socket)
- **Pizarrón de recetas:** **Detrás del jugador** — muestra las recetas disponibles y cómo armar cada trago. El jugador se da vuelta para consultarlo.
- **Pizarra de pedidos:** Muestra los pedidos activos de los clientes
- **Caja registradora:** Para cerrar la noche y ver el resumen
- **Campana de servicio:** En la barra, para abrir la noche
- **Catálogo de tienda:** Clipboard/catálogo sobre el mostrador (solo escena de día)

De momento el bar es funcional y minimalista — solo lo necesario para el gameplay. Decoración, ambientación y elementos extra se agregarán en fases posteriores.

*Recomendación: Considerar agregar una radio/jukebox como fuente diegética de música — le da vida al ambiente y es interactiva (el jugador puede cambiar la estación o apagarla).*

---

## 6. UI e información

**Principio:** Toda la UI es diegética o espacial. Nada flota como HUD — previene motion sickness y mejora la inmersión.

| Elemento UI | Tipo | Ubicación |
|-------------|------|-----------|
| Pedido del cliente | Espacial | Sobre la cabeza del NPC (nombre + proporción) |
| Paciencia del cliente | Espacial | Barra que se agota sobre la cabeza del NPC |
| Recetas/Menú | Diegético | Pizarrón de bar detrás del jugador |
| Resumen de noche | Diegético | Ticket que sale de la caja registradora |
| Llenado del vaso | Espacial | Barra de fill flotante sobre el vaso (billboard) |
| Marcas de proporción | Diegético | Líneas/zonas en el vaso y shaker |
| Catálogo de tienda | Diegético | Clipboard sobre el mostrador (escena de día) |
| Nivel de borrachera | Visual/Espacial | Sway corporal del NPC proporcional al nivel (ver 3.6) |

---

## 7. Controles e interacciones VR

### 7.1 Input

- **Controladores:** Grip para agarrar, trigger para interacciones secundarias
- **Hand Tracking:** Soporte planificado (actualmente deshabilitado)

### 7.2 Interacciones principales

| Acción | Cómo se hace |
|--------|-------------|
| Agarrar botella | Grip cerca de la botella |
| Verter líquido | Inclinar la botella/shaker agarrado sobre un vaso |
| Agitar shaker | Movimiento de shake con el shaker agarrado |
| Soltar objeto | Soltar grip |
| Servir trago | Verter en el vaso frente al cliente (validación automática) |
| Golpear campana | Mover la mano hacia la campana (iniciar noche) |
| Cerrar noche | Automático al llegar a las 5AM |
| Consultar recetas | Darse vuelta y mirar el pizarrón |
| Comprar en tienda | Poke sobre ítems del catálogo (escena de día) |

### 7.3 Feedback háptico

- Pulso al golpear la campana de servicio
- Pulso corto al contactar con socket
- Pulso fuerte al éxito de receta
- Vibración al verter líquido (proporcional al ángulo)
- Vibración rítmica al agitar el shaker

---

## 8. Arte visual

### 8.1 Estilo

**Boxel Art:** estética de voxel con formas cúbicas/boxy, low-poly. Visualmente atractivo, liviano para VR, y con identidad clara. Consistente en todos los elementos: bar, botellas, NPCs, UI diegética.

### 8.2 Paleta de colores

Tonos cálidos de bar nocturno: ámbar, madera oscura, luces neón tenues. Colores vivos en botellas y líquidos para guiar la atención del jugador y dar feedback visual claro.

### 8.3 NPCs

Estilo Boxel Art consistente con el resto del bar. Diseño específico de los personajes todavía sin definir — se desarrollará cuando se aborde la dirección artística en detalle.

---

## 9. Audio

Dirección de audio sin definir en detalle. Se establecerá en una fase posterior.

**SFX confirmados por gameplay:**
- Verter líquido (proporcional al flujo)
- Vidrio rompiéndose (botellas y vasos)
- Campana de servicio (ding al abrir noche)
- Caja registradora (click/apertura al cerrar noche)
- Agitar shaker
- Feedback de satisfacción del cliente (positivo/negativo)

**Por definir:**
- Música de fondo (estilo, si es diegética o ambiental)
- Voces/sonidos de clientes (balbuceo, quejas, celebración)
- Sonidos ambientales del bar

*Recomendación: Música diegética desde una radio/jukebox en el bar. Permite que el jugador controle el volumen/estación y es más inmersivo que una banda sonora ambiental. Estilo sugerido: jazz de bar o lo-fi — relajado y que no compita con los SFX de gameplay.*

---

## 10. Progresión y escalamiento

### 10.1 Estructura

**Sin límite de noches ni fin del juego.** El jugador juega noches indefinidamente, acumulando plata y desbloqueando contenido.

La dificultad escala naturalmente con el contenido disponible:
- Más recetas compradas → más variedad de pedidos → más memoria y velocidad requerida
- Más ingredientes → más posibilidades de error
- Clientes más impacientes (configurable por noche)
- Más posibilidad de caos/borrachera

### 10.2 Tienda de día

De momento la tienda solo ofrece **nuevos ingredientes y recetas**. Todo lo demás (equipamiento, decoración, mejoras) se evaluará en el futuro.

*Recomendación para el futuro:*
- *Equipamiento: grifo de cerveza (nueva categoría de bebida), exprimidor (jugos como mixer), hielera (hielo como ingrediente)*
- *Capacidad: más asientos, más espacio en la estantería*
- *Cosmético: decoración del bar, nuevo pizarrón, iluminación*

### 10.3 Consecuencias económicas

De momento no hay consecuencias por no recaudar — no hay game over.

*Recomendación para el futuro: Alquiler semanal del bar. Cada X noches se cobra un alquiler fijo. Si no tenés suficiente plata, no perdés el bar inmediatamente — se acumula deuda y baja la "reputación" del bar, lo que reduce la calidad de clientes (menos propina, más impacientes). Crea tensión sin ser punitivo.*

### 10.4 Objetivo a largo plazo

De momento no hay un objetivo final definido.

*Recomendación: Sistema de reputación/estrellas del bar (1★ a 5★). La reputación sube al recaudar bien, servir correctamente, y desbloquear contenido. Cada estrella podría desbloquear algo visible (un cartel en la puerta del bar, mejores clientes, acceso a tragos premium). No es un "game over" — el jugador puede seguir jugando después de 5★, pero tiene un objetivo claro que perseguir. Referencia: sistema de estrellas de Overcooked / Diner Dash.*

---

## 11. Condiciones de victoria y derrota

- **Objetivo inmediato:** Recaudar la mayor cantidad de plata posible cada noche.
- **Objetivo a largo plazo:** TBD (ver recomendación de sistema de estrellas en sección 10.4).
- **No hay game over:** De momento no se puede perder. En el peor caso, no recaudás nada.
- **Progresión:** La plata acumulada se usa en la escena de día para comprar nuevos tragos.

---

## 12. Monetización

Desarrollo hasta experiencia completa y pulida. Buscar financiamiento para lanzamiento comercial como venta única. Si no se consigue, free-to-play con posibilidad de DLCs (recetas, temáticas de bar, eventos).

---

## 13. Desafíos técnicos conocidos

- Optimización de física en VR para mantener framerate estable (90fps mínimo Quest 3, 72fps Quest 2)
- Detección precisa del ángulo de vertido y proporción de líquido en VR
- Interacciones de shaker satisfactorias (agitar = mezclar bien)
- Balance económico desafiante pero no punitivo
- Motion sickness: gameplay estacionario (jugador fijo detrás de la barra)
- Estilo Boxel Art optimizado para GPU tile-based de Quest (pocos polígonos, texturas livianas)

---

## 14. Glosario

| Término | Significado | No usar |
|---------|-------------|---------|
| **Noche** | Sesión completa de gameplay (intro + servicio + cierre) | Nivel, ronda |
| **Servicio** | Fase activa de atención a clientes dentro de una noche | Turno, partida |
| **Trago** | Resultado de una receta servida | Cóctel (salvo UI) |
| **Pedido** | Solicitud de un cliente: trago + proporción | Orden (ambiguo) |
| **Proporción** | Ratio de ingredientes: Fuerte/Mitad/Liviano/Puro | Porcentaje (confuso) |
| **Borrachera** | Mecánica de intoxicación progresiva por cliente | Alcoholismo, ebriedad |
| **Daño** | Costo económico por rotura | Destrucción (genérico) |
| **Servir** | Verter el trago en el vaso del cliente | Entregar |
| **Mezclador/Shaker** | Contenedor para tragos que requieren mezcla | Coctelera (post-MVP) |
| **Pour directo** | Verter ingredientes directo al vaso del cliente | — |
| **Boxel Art** | Estilo visual: voxel/boxy, low-poly | Pixel art, realistic |

---

## Temas pendientes para sesiones futuras

| Tema | Prioridad | Notas |
|------|-----------|-------|
| ~~Sistema de borrachera~~ | ✅ Diseñado | Ver sección 3.6 |
| **Dirección artística detallada** | Media | Diseño de NPCs, paleta final, props del bar |
| **Dirección de audio** | Media | Música, voces, ambientación sonora |
| **Decoración del bar** | Baja | Elementos no-funcionales para ambientación |
| **Mejoras de tienda** | Baja | Equipamiento, capacidad, cosméticos |
| **Sistema de consecuencias** | Baja | Alquiler, reputación, game over |
| **Objetivo final** | Baja | Sistema de estrellas u otro goal a largo plazo |

---

## 15. Estado de implementación (auditoría GDD vs juego)

> **Auditoría automática del código** (rama `main`, 2026-06-24) contra este GDD.
> Leyenda: ✅ Implementado · 🟡 Parcial / difiere · ❌ No está en el juego.
> Las referencias de archivo apuntan a `Assets/2. Scripts/` salvo aclaración.

### 15.1 Resumen por sección

| Sección del GDD | Estado | Nota |
|---|---|---|
| 2. Core loop (día/noche) | 🟡 | Implementado como **GameState FSM** (`DayShop` ↔ `NightRunning` ↔ `NightSummary`) en una sola escena `Bar.unity`, no dos escenas separadas. |
| 2.1 Apertura con campana | ❌ | Hoy se abre con un **clipboard/poke button** (`NightClipboard`), no una campana física. |
| 2.1 Reloj 8PM–5AM, auto-cierre | 🟡 | Auto-cierre OK (`NightService`). Duración real = **180 s** (config), no 10 min. El 8PM–5AM es conversión cosmética; no hay reloj diegético visible. |
| 2.1 / 3.4 Máx. 3 clientes / 3 asientos | 🟡 | Config soporta 3 (`NightConfigSO.MaxSimultaneous`), pero la escena tiene **solo 2 asientos** (`Seat_0`, `Seat_1`). |
| 3.1 Preparación Vía A (pour directo) | ✅ | `PourDetector` (tilt + raycast → `LiquidContainer.Receive`). |
| 3.1 Preparación Vía B (shaker) | ❌ | No hay objeto shaker, gesto de agitar ni lógica de mezcla. |
| 3.2 Proporciones discretas (30/50/70/100) | ✅ | `FillLevels.Ratios = {0.30, 0.50, 0.70, 1.00}`. |
| 3.2 Tolerancia ±10% | 🟡 | Usa redondeo al bucket más cercano (`ServeSocket.BucketOf`), no una banda explícita ±10% (funcionalmente similar). |
| 3.2 Marcas físicas en vaso/shaker | 🟡 | Solo gauge UI flotante (`GlassFillBar`); sin marcas en el modelo del vaso. |
| 3.3 Receta Gin Tonic | ✅ | `Recipe_GinTonic.asset`. |
| 3.3 Receta Fernet con Coca | ❌ | No existe (ni en `RecipeId` ni asset). |
| 3.3 Ingredientes Gin / Tónica / Coca | ✅ | `IngredientId.Gin/Tonic/Cola`. |
| 3.3 Ingrediente Fernet | ❌ | No existe en `IngredientId`. |
| 3.4 FSM cliente (Approaching→Waiting→Drinking→Leaving) | ✅ | Coincide con el GDD (existen además `GoingToTable`/`Wandering`, hoy sin uso). |
| 3.4 Paciencia + se va por timeout | ✅ | `CustomerSO.PatienceSeconds`, `WaitingState`, `LeavingState`. |
| 3.5 Satisfacción 3 niveles (Le gustó/Meh/No le gustó) | ✅ | `WaitingState` score = nivel + trago; pago escala con score (`EconomyService.RegisterSale`). |
| 3.6 Borrachera — cálculo D = alcoholMl/max | ✅ | `WaitingState.ComputeDrunkenness`. |
| 3.6 Borrachera — niveles discretos (L0–L3) | ❌ | Solo float continuo `Drunkenness`; sin `levelThresholds` ni enum de niveles. |
| 3.6 Efecto — multiplicador de propina | ✅ | `DrunkennessConfig.TipMultiplier(D)` (interpolación, no array por nivel). |
| 3.6 Efecto — wobble al irse ∝ D | ✅ | `LeavingState.ComputeWobble`. |
| 3.6 Efecto — velocidad de bebida (drinkSpeedMult) | ❌ | `DrinkTimer` corre a ritmo fijo. |
| 3.6 Efecto — riesgo de rotura de vaso al irse | ❌ | No hay roll por borrachera; la rotura es solo por impacto físico. |
| 3.6 Efecto — SFX balbuceo/grito (L≥2) | ❌ | No hay SFX de voz del cliente. |
| 3.6 DrunkennessConfigSO (campos del GDD) | 🟡 | Existen `alcoholMlForMax`, `maxTipMultiplier`, `wobbleAmplitude/Frequency`. Faltan `levelThresholds`, `tipMultipliers[4]`, `breakRisks[4]`, `drinkSpeedMult[4]`. |
| 3.7 Economía — ingresos (precio + propina) | ✅ | `RecipeSO.BasePrice`, `CustomerSO.BaseTip`, `EconomyService`. |
| 3.7 Economía — plata persiste entre noches | ✅ | `SaveService` / `EconomyService` / `GameStateService`. |
| 3.7 Economía — dinero inicial $0 | 🟡 | Arranca con **$300** (`SaveService.StartingCash`) para que la tienda del día sea usable. |
| 3.8 Destrucción física (swap a prefab fracturado + pool) | ✅ | `Breakable` + `BreakablePoolService`. |
| 3.8 Costo económico por rotura | 🟡 | Solo **botellas** descuentan (`Bottle` → `RepairCost`). Los **vasos** se rompen gratis. |
| 3.9 Caja registradora + ticket impreso | 🟡 | Hay anchor de caja y resumen en clipboard; falta el **ticket físico** que sale con el resumen. |
| 4. Contenido MVP (recetas) | 🟡 | El juego tiene **8 recetas** (Vodka, VodkaTonic, GinTonic, Champagne, Cognac, Tequila, Whiskey, Wine) — más que el MVP, pero **sin Fernet con Coca**. |
| 5. Escenario (barra, estante, asientos, pizarrones, caja) | ✅ | `BarSceneRoot`, `RecipeMenuBoard`, `OrdersBoard`, sockets. Falta campana física. |
| 6. UI diegética/espacial | ✅ | `CustomerOrderLabel`, `CustomerPatienceBar`, `GlassFillBar`, sway de borracho. |
| 7.1 Hand tracking | 🟡 | No configurado — **alineado con el GDD** (dice "planificado, deshabilitado"). |
| 7.2 Interacciones VR (grab, pour, poke) | ✅ | Meta OVR (`SimpleVRGrabber`, `PourDetector`, `PokeButton`). |
| 7.2 Agitar shaker | ❌ | Sin gesto de agitar. |
| 7.3 Háptica (campana, socket, éxito, pour, shaker) | 🟡 | OK: botón, éxito de receta, pour, rotura. Faltan: pulso al contactar socket y ritmo del shaker. |
| 8. Arte Boxel / low-poly | ✅ | Assets en `Assets/7.Models` (voxel humano, botellas/props low-poly). |
| 9. Audio — SFX de gameplay | 🟡 | Amplia cobertura (`SfxId`). Faltan: ding de campana propio, click de caja, shaker, voces de cliente. |
| 9. Música de fondo | 🟡 | `MusicController` por estado; sin jukebox/radio diegético interactivo. |
| 10–11. Progresión / objetivo a largo plazo | 🟡 | Unlocks de recetas/ingredientes ✅; sin alquiler, reputación/estrellas ni objetivo final (el GDD los marca como futuro). |
| 11. Sin game over | ✅ | Confirmado: no hay condición de derrota. |

---

## 16. A Implementar

Todo lo que está en este GDD pero **todavía no está (o está incompleto) en el juego**, ordenado por prioridad. Cada ítem cita la sección del GDD de origen.

### 16.1 Gaps de MVP (contenido base del GDD que falta)

1. **Receta "Fernet con Coca" (pour directo)** — es 1 de las 2 recetas del MVP y no existe. *(§3.3)*
2. **Ingrediente "Fernet" (tipo Alcohol)** — agregar a `IngredientId` + asset; requisito de la receta anterior. *(§3.3)*
3. **Vía B de preparación — Mezclador/Shaker** — objeto shaker agarrable + **gesto de agitar** + lógica de mezcla + verter del shaker al vaso. Hoy solo existe el pour directo (Vía A). *(§3.1, §7.2)*
4. **3er asiento** — la escena tiene 2 (`Seat_0`, `Seat_1`); sumar el tercero (la config ya soporta `MaxSimultaneous = 3`). *(§2.1, §3.4, §5)*
5. **Campana de servicio física** para abrir la noche (impulso de la mano → `BeginNight`, con háptica + ding). Hoy se abre con un clipboard/poke. *(§2.1, §5, §7.2)*

### 16.2 Sistemas parciales (existen pero les falta lo del GDD)

6. **Niveles discretos de borrachera** (Sobrio / Achispado / Borracho / Muy Borracho) con `levelThresholds = [0.30, 0.70, 1.00]`. Hoy es un float continuo sin buckets. *(§3.6)*
7. **Efecto: velocidad de bebida por nivel** (`DrinkTimer *= drinkSpeedMult[L]`, `[1.0, 0.8, 0.6, 0.5]`). *(§3.6)*
8. **Efecto: riesgo de romper el vaso al irse** (`Random < breakRisk[L]`, `[0, 0.10, 0.25, 0.45]`). Hoy la rotura es solo por impacto físico, no por borrachera. *(§3.6)*
9. **Completar `DrunkennessConfigSO`** con los arrays por nivel: `tipMultipliers[4]`, `breakRisks[4]`, `drinkSpeedMult[4]`, `levelThresholds[3]`. *(§3.6)*
10. **Costo económico al romper VASOS** (hoy solo las botellas descuentan plata). *(§3.8)*
11. **Caja registradora con ticket impreso** que sale con el resumen (ingresos, daños, total) como objeto físico/diegético. *(§3.9, §6)*
12. **Marcas de proporción físicas** (líneas/zonas ~30/50/70) en el modelo del vaso y del shaker, además del gauge UI. *(§3.2, §6)*
13. **Háptica al contactar el socket** (pulso corto al apoyar el vaso/objeto en el socket). *(§7.3)*
14. **Dinero inicial $0** según el GDD (hoy arranca en $300). Decisión de diseño a confirmar: ¿se ajusta el GDD o el juego? *(§3.7)*

### 16.3 Audio y voces pendientes (el GDD ya marca varios como "por definir")

15. **Voces de cliente**: balbuceo (L2), grito/queja (L3), celebración al éxito. *(§3.6, §9)*
16. **SFX dedicados**: ding propio de la campana, click/apertura de la caja registradora, sonido de agitar el shaker (hoy genéricos o ausentes). *(§9)*
17. **Háptica/SFX rítmico del shaker** (depende de implementar el shaker). *(§7.3, §9)*

### 16.4 Recomendaciones / Post-MVP (el GDD ya los marca como futuro)

18. **Radio/Jukebox diegético** como fuente de música controlable por el jugador (hay música por estado, no jukebox). *(§5, §9)*
19. **Alquiler semanal del bar** como consecuencia económica con deuda/reputación. *(§10.3)*
20. **Sistema de reputación / estrellas (1★–5★)** como objetivo a largo plazo. *(§10.4, §11)*
21. **Mejoras de tienda**: grifo de cerveza, exprimidor, hielera, más asientos/estantería, cosméticos. *(§10.2)*
22. **Decoración / ambientación del bar** (props no funcionales). *(§5)*
23. **Hand tracking** (el GDD ya lo da como planificado/deshabilitado — queda como futuro). *(§7.1)*

### 16.5 Notas de divergencia (decisiones de diseño, no necesariamente "a implementar")

- **Dos escenas Día/Noche** → resuelto como **GameState FSM** en una sola `Bar.unity`. Alternativa válida; el GDD podría actualizarse para reflejarlo.
- **Duración de noche** → 180 s reales (config) vs ~10 min del GDD. Es solo tuning.
- **Tolerancia ±10%** → implementada como redondeo al bucket más cercano. Funcionalmente equivalente.
- **Catálogo de recetas** → el juego **excede** el MVP (8 recetas), pero le falta la receta insignia (Fernet con Coca).

> Doc base: *GDD — Pour Decisions v1.3*. Apéndice de auditoría generado por Claude Code el 2026-06-24. TDD relacionado: `Assets/8. Documents/Technical.md`.
