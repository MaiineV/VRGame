# Tipos de Luces en videojuegos

> Tema: tipos de luces en motores de videojuegos y renderizado (dinámicas, estáticas, bakeadas) y requisitos para trabajar con ellas.
> Fuente: imagen de estudio `Tipos de Luces.png` (transcripción del texto).

---

## Uso de Luces en videojuegos

En los motores de videojuegos y renderizado tenemos principalmente **3 opciones de luces**:
- **Dinámicas**
- **Estáticas**
- **Bakeadas**

Todas ellas sirven para mejorar las visuales de los escenarios del juego. Pero tienen formas de aplicarse muy puntuales y su **coste de rendimiento** es algo a evaluar.

### Dinámicas
- Son las **más caras**.
- Generan efectos visuales en el escenario siempre y cuando los materiales y los objetos en escena lo permitan. (Los materiales muchas veces son los que determinan si la luz puede o no afectar una malla.)
- Se **actualizan en tiempo real**, se aplican fácil y directamente al GPU.
- En **mobiles** puede haber hasta **3 simultáneas**. Luego comienzan a decaer peligrosamente.

### Estáticas
- Solo afectan a **elementos estáticos** del nivel y su coste no es tan alto.
- **No se actualizan en tiempo de ejecución.**

### Bakeadas
- Son los efectos de las luces sobre las **texturas de los materiales**, sobre mapas de UV.
- Su costo es **súper barato**, pero **tardan bastante en hacerse**. La máquina debe calcular los efectos y rebotes de luz sobre cada objeto para poder aplicar esto.
- En motores de videojuego está bien, pero **la forma de generarlas es diferente según el motor**.
- Si un **FBX** ya las tiene aplicadas, el motor las reconoce.

### Requisito para trabajar con luces (de forma barata)
- Nuestro material o shader principal debe ser **susceptible a las mismas**.
- Debemos tener en los modelos a afectar los **mapas de UV bien hechos y sin solapamiento**.
- Siempre las mallas o materiales tienen mapas de **UV1**; estos **no reflejan los efectos de la luz**. Pero los **UV2 sí**.
- Los UV2 **deben crearse manualmente**, pues si no, no podremos bakear luces sobre un modelo 3D.

---
