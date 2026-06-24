# Respuestas — Simulacro de Práctica (Tecnologías Disruptivas)

> Respuestas a las 5 preguntas del `Practica - Simulacro de preguntas.md`, basadas en la documentación de la cátedra (apunte, tutoriales y rúbrica).

---

## 1. Flujo para relacionar un sonido de feedback con agarrar un objeto con las manos

**Flujo:**
`Objeto agarrable (Grabbable + Hand Grab Interactable)` → `Pointable Unity Event Wrapper (referenciando el Grabbable)` → `evento When Select` → `Audio Source (con el sonido, Play On Awake desactivado)` → `función AudioSource.Play`

**Paso a paso:**
1. Tener el objeto **agarrable con las manos**: con **Grabbable** + **Hand Grab Interactable** (y su Rigidbody/colliders).
2. Agregarle el componente **Pointable Unity Event Wrapper** y **referenciar el Grabbable** del objeto.
3. Crear un **Audio Source** con el sonido a reproducir y **desactivar Play On Awake**.
4. En el evento **When Select** del wrapper, agregar un **listener** (`+`).
5. Arrastrar el objeto que tiene el **Audio Source** y elegir la función **AudioSource → Play**.

> (Para el sonido al **soltar**, lo mismo con el evento **When Release**.)

---

## 2. ¿Por qué más cuidado con el feedback negativo que con el positivo?

Porque en **VR el jugador no observa el juego, lo habita con su cuerpo**. Las sensaciones **negativas** (errores, castigos, fallos, movimientos incómodos) se viven de forma **física e intensa**: generan frustración, incomodidad e incluso **mareo**, **rompen la presencia** y pueden hacer que el usuario **quiera sacarse el visor**. El feedback **positivo** refuerza y motiva sin ese riesgo. Un feedback negativo mal calibrado **expulsa al jugador de la experiencia**, así que hay que planificarlo con mucho cuidado.

> Además (criterio de la rúbrica): el feedback **no puede depender solo del audio**; el jugador puede jugar sin sonido, así que las marcas negativas/positivas deben entenderse también de forma **visual**.

---

## 3. Flujo para establecer poses de manos

**Flujo:**
`Hand Grab Interactable` → `Add Hand Grab Pose (with scale 1)` → `posicionar/rotar el Hand Grab Pose en el punto de agarre` → `ajustar los dedos (Fingers Freedom)` → `Create Mirror Hand Grab Interactable (pose espejo)`

**Paso a paso:**
1. En el objeto con **Hand Grab Interactable**, clic en **Add Hand Grab Pose (with scale 1)** → se genera una **mano virtual** (objeto **Hand Grab Pose**).
2. **Posicionar y rotar** el GameObject **Hand Grab Pose** hacia el punto de agarre del objeto.
3. **Ajustar los dedos** uno a uno (con los círculos de cada falange) y configurar en el script **Hand Grab Pose**: el **tipo de mano** y el **Fingers Freedom** de cada dedo (**Bloqueado / Restringido / Libre**).
4. **Create Mirror Hand Grab Interactable** → genera la **pose espejo** para la otra mano (ajustar rotación, p. ej. Y: 180, si hace falta).
5. *(Opcional)* crear **copias con distintas escalas** de mano (con el slider) → el sistema **interpola** la pose adecuada.

**Componentes / objetos intervinientes:**
- **Hand Grab Interactable** (define el agarre con manos y aloja las poses).
- **Hand Grab Pose** (script/objeto de la pose, con Fingers Freedom).
- **Grabbable** y **Rigidbody** (referenciados en el objeto).
- **Colliders** ubicados en la zona de agarre.
- **Manos sintéticas** (Synthetic Hands).
- Para que las poses funcionen con **controles**: en **OVR Manager** poner *Controller Driven Hand* → **Conform to Controller**, y *Show State* → **Always**.

---

## 4. Puse un *event wrapper interactor* con un evento de partículas en *Select* y no pasa nada. ¿Por qué?

Porque el **objeto que se agarra es un *interactable*, no un *interactor***. Un **event wrapper de interactor** responde a los eventos del **interactor** (la mano / el control que realiza la acción), no a los del objeto que recibe el agarre. Para disparar feedback en el objeto agarrado hay que usar el **Pointable / Interactable Unity Event Wrapper** y **referenciar el Grabbable / Interactable** correcto; ahí sí el evento **Select** se dispara al agarrarlo y reproduce las partículas.

Causas concretas del "no pasa nada":
- Se usó el **wrapper equivocado** (de interactor) en un objeto que es **interactable**.
- **Falta la referencia** al Pointable/Interactable correspondiente en el wrapper, así que el evento nunca se ejecuta.
- (A revisar también) que el objeto realmente tenga sus **componentes de agarre** (Grabbable + Hand Grab Interactable); si no, nunca se "selecciona".

---

## 5. Sin programar: evitar atravesar paredes con desplazamiento natural + mecánica según el ojo derecho o izquierdo

**Para no atravesar paredes/superficies con el desplazamiento natural:**
- Necesitás **colisión del jugador**: un **Character Controller / Collider** en el **Camera Rig** (el player) y **colliders** en las paredes y superficies. Así el cuerpo del jugador choca y no atraviesa, sin escribir código.
- (En el SDK de Meta se integra junto con el bloque de **locomoción / player controller** que respeta colisiones.)

**Para la mecánica basada en el ojo derecho o izquierdo:**
- Integrar el **Building Block de Eye Tracking (seguimiento ocular / Eye Gaze)**, que detecta hacia dónde mira cada ojo. Requiere un visor con **eye tracking** (p. ej. Meta Quest Pro).

> *Nota:* la parte de eye tracking no está detallada en la documentación que tengo; conviene confirmar el nombre exacto del building block con los apuntes de clase.
