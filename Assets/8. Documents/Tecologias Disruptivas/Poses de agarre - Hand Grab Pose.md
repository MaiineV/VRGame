# Poses de agarre (Hand Grab Pose) — Meta Quest

> Tema: crear poses de agarre personalizadas para que la mano virtual se adapte a la forma del objeto al agarrarlo, en Unity con el Meta XR Interaction SDK.
> Fuente: tutorial de la serie *Unity Adventure* (recuperado del Resumen Primer Parcial / Google Doc).

---

## 1. ¿Cómo crear poses de agarre para VR? — Hand Grab Pose Meta Quest

🔗 https://www.youtube.com/watch?v=ZnBn0rAMMYo

### Crear una pose de agarre
En el GameObject **Hand Grab** que contiene el componente **Hand Grab Interactable**, en la parte inferior está la sección para definir las poses de agarre.
1. Clic en **Add Hand Grab Pose (with scale 1)** → se genera una mano virtual para definir la pose.

**Posicionar la mano:**
1. Abrir el GameObject generado y buscar el hijo llamado **Hand Grab Pose**.
2. Mover y rotar ese GameObject hacia el **punto de agarre** del objeto (ayudarse de las distintas vistas del editor).

**Ajustar los dedos:**
- Cada falange tiene **círculos** que permiten ajustar su posición. Posicionar los dedos **uno a uno** para crear la pose.
- *Consejo:* buscar un objeto similar en la vida real y agarrarlo en la pose que se quiere recrear, para guiarse con la posición de cada dedo.

### Configuración de la pose (script Hand Grab Pose)
- **Tipo de mano:** derecha o izquierda.
- **Fingers Freedom (libertad por dedo):**
  - **Bloqueado:** el dedo permanece en la posición de la pose, sin importar el dedo real.
  - **Restringido:** sigue al dedo real pero con restricciones para no deformar la pose.
  - **Libre:** se mueve con total libertad.
  - *Depende del tipo de pose y del objeto.*
- Abriendo el campo inferior se ven los **valores de posición de cada falange**.

### Crear la pose espejo (otra mano)
No hace falta rehacer la pose dedo por dedo:
1. En el script **Hand Grab Interactable**, abajo, botón **Create Mirror Hand Grab Interactable**.
2. Se crea un objeto con el sufijo **mirror** que refleja la pose, ya configurado para la otra mano.
3. Puede requerir ajustar la rotación (ej. **Y: 180**) para que la pose quede desde el mismo ángulo.

### Poses con diferentes escalas de mano
Si la experiencia permite ajustar el tamaño de las manos:
1. Usar el **slider** de escala para definir el tamaño (hay previsualización).
2. Crear la **copia** con esa escala.
3. Modificar la pose si hace falta.
- El sistema hace una **interpolación** automática para elegir la pose que mejor se ajusta a las manos del usuario.

### Múltiples puntos de agarre en un mismo objeto
Ejemplo: un vaso que se agarra desde arriba y desde la oreja.
1. Dentro del objeto, crear un nuevo GameObject vacío (ej. **Hand Grab**).
2. Agregar el componente **Hand Grab Interactable**.
3. Verificar que las referencias al **Grabbable** y al **Rigidbody** estén correctas.
4. Añadir la pose, posicionar la mano y crear la **pose espejo**.
- **Los colliders deben estar siempre en las zonas de agarre correspondientes.**

### Que las poses funcionen con los controles
Con las manos funcionan directo; para que se muestren con los **controles** hay que reconfigurar el Rig:
1. En el Inspector del **Camera Rig → OVR Manager**: cambiar **Controller Driven Hand** a **Conform to Controller**.
2. En el bloque de **Hand Tracking**: cambiar **Show State** a **Always** (para ambas manos).

---
