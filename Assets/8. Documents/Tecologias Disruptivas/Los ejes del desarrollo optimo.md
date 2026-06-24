# Los ejes del desarrollo en realidad virtual

En VR hay tres ideas que mandan y que, si fallan, se nota al instante: affordance, presencia y embodiment. No son conceptos teóricos aislados; funcionan juntos y determinan si el jugador entiende, cree y se siente dentro del juego… o si todo se le cae a pedazos en segundos.

El affordance, tomado de Don Norman, es básicamente que los objetos "expliquen solos" cómo se usan. En VR esto es crítico, porque no tenés teclado ni mouse como muleta. Si ves una palanca, tiene que invitar a agarrarla; si hay un botón, tiene que pedir que lo presiones. Juegos como Half-Life: Alyx lo hacen perfecto: casi no necesitás tutorial. En cambio, cuando algo parece interactivo y no lo es (o al revés), el jugador se frustra rápido. Es diseño roto, sin discusión.

La presencia es la sensación de "estar ahí". No se trata de gráficos hiperrealistas, sino de coherencia. El mundo tiene que responder como esperás: buena escala, sonido consistente, física creíble. Cuando eso pasa, el cerebro compra la ilusión. Cuando no, te das cuenta de que estás en un juego y se rompe todo. The Walking Dead: Saints & Sinners logra esto muy bien con el peso y comportamiento de los objetos. En VR, un error chico —como atravesar una pared— se siente enorme.

El embodiment: no solo estás en el mundo, sino que sentís que ese cuerpo es tuyo. Tus manos virtuales tienen que moverse como las tuyas, sin delay raro ni animaciones que te saquen control. Juegos como Lone Echo lo clavan porque te movés literalmente con tus brazos. Si el jugador siente que está "manejando un personaje", ya perdiste; tiene que sentir que es él.

Lo interesante es cómo se encadenan. Si el affordance está bien, el jugador entiende qué hacer sin pensar. Eso refuerza la interacción, lo que mejora la presencia. Y cuando la presencia es sólida, aparece el embodiment: el jugador deja de analizar y empieza a habitar el mundo. Es un efecto dominó.

En resumen: en VR no alcanza con que algo funcione, tiene que sentirse natural. Si el jugador duda, algo está mal. Si se marea, algo está peor. Y si se olvida de que está jugando, entonces hiciste las cosas bien.

## El diseño UI y la inmersión

En VR, la UI y la calidad visual no se diseñan para "verse bien", sino para no romper la experiencia. Todo lo que huela a interfaz tradicional pegada encima del mundo suele jugar en contra. Acá la regla es simple: si parece un videojuego… el cerebro deja de creer.

La UI diegética es el estándar. En lugar de barras de vida flotando, la información vive dentro del mundo: en un reloj, en el arma, en el entorno. Half-Life: Alyx es el ejemplo clásico: casi no hay HUD y, sin embargo, siempre sabés lo que pasa. Esto no es solo estética, es cognición: cuanto menos tengas que "interpretar", más fluido se siente todo.

Después está la UI espacial, que implica ubicar elementos en el espacio 3D de forma coherente. No alcanza con poner un menú flotando: tiene que estar a una distancia cómoda, con tamaño legible y sin obligar al jugador a forzar la vista o el cuello. En VR, leer mal no es una molestia… es fatiga física. La legibilidad manda: tipografías simples, buen contraste y tamaños generosos.

Otro punto clave es usar las manos como interfaz. En lugar de navegar menús complejos, el jugador debería interactuar tocando, agarrando o señalando. Esto reduce fricción y aumenta la naturalidad. Juegos como Lone Echo llevan esto al extremo: el cuerpo entero es la UI. Cuando esto funciona, desaparece la sensación de "interfaz" y queda solo la acción.

En cuanto a calidad visual, el concepto central es la coherencia antes que el realismo. No necesitás gráficos hiperdetallados; necesitás que todo respete la misma lógica visual. Escala correcta, iluminación consistente y materiales creíbles pesan más que la cantidad de polígonos. Un mundo simple pero coherente funciona mejor que uno realista pero inconsistente.

La escala es especialmente crítica. Si una puerta es demasiado chica o un objeto parece de juguete, el cerebro lo detecta al instante y se rompe la inmersión. VR es implacable con esto. Lo mismo pasa con la iluminación: sombras mal colocadas o luces sin sentido generan una incomodidad difícil de explicar, pero muy fácil de sentir.

También entra en juego la optimización visual inteligente. En VR necesitás rendimiento alto y estable, así que técnicas como LODs, niebla (fog) y reducción de distancia de dibujado no son opcionales, son parte del diseño. Bien usadas, no solo mejoran performance, sino que también ayudan a guiar la mirada y reforzar la atmósfera.

El feedback visual cierra el círculo. Cada acción debe tener una respuesta clara: un cambio de color, una animación, una reacción física. Beat Saber es un caso perfecto: cada corte confirma al instante que hiciste lo correcto. Sin ese feedback, el mundo se siente muerto.

En síntesis: en VR, la mejor UI es la que casi no parece UI, y la mejor calidad visual no es la más detallada, sino la más consistente. Todo tiene que estar al servicio de una experiencia fluida, legible y creíble. Si el jugador tiene que pensar cómo interactuar o esforzarse para ver, el diseño ya perdió.

## Bienestar del usuario al momento de diseñar

Cuando uno diseña para visores standalone como Meta Quest 2 o Meta Quest 3S, hay que cambiar el chip por completo. No estás haciendo un juego "normal" con cámara libre: estás diseñando una experiencia que impacta directamente en el cuerpo del jugador. Si algo está mal, no se percibe como un bug… se siente como mareo, incomodidad o fatiga real.

Primero aparece el tema del rendimiento. Estos dispositivos tienen hardware limitado, más cercano a un celular que a una PC gamer. Eso obliga a tomar decisiones: menos polígonos, menos luces dinámicas, menos efectos pesados. Y esto no es solo una cuestión técnica. Si el juego baja de frames, el jugador no dice "ah, bajó el rendimiento"; se empieza a sentir mal. Un caso simple: estás caminando en un entorno urbano y el juego pasa de fluido a tironeado. Esa mínima inestabilidad ya genera un conflicto sensorial. Por eso, mantener una tasa estable (72–90 FPS) no es opcional, es la base del diseño.

Después entra lo visual. En VR no mirás una pantalla, tenés una lente pegada a los ojos. Eso cambia todo. Los textos chicos o con poco contraste directamente no se leen, y forzar la vista genera cansancio rápido. Por ejemplo, poner un menú con tipografía fina flotando lejos puede parecer elegante en un monitor, pero en VR es ilegible. En cambio, juegos como Beat Saber funcionan porque todo es grande, contrastado y claro. No hay duda de qué está pasando ni qué tenés que hacer.

Ahora, el punto crítico: el mareo. Acá no hay vueltas. El problema aparece cuando lo que ves no coincide con lo que siente tu cuerpo. Si el juego te mueve hacia adelante, pero vos estás quieto, el cerebro entra en conflicto. Es literalmente el mismo mecanismo que se activa cuando te mareas en un auto leyendo. Un ejemplo típico: usar el joystick para avanzar suavemente mientras la cámara se desliza. Para alguien acostumbrado puede ser tolerable, pero para muchos jugadores nuevos es una receta directa al malestar.

Por eso existen soluciones como el teleport o los giros por pasos (snap turning). No son "menos inmersivos", son más tolerables. Half-Life: Alyx lo resuelve muy bien porque te deja elegir cómo moverte. Y eso es clave: no hay una única forma correcta, hay que adaptarse al jugador. También se usan trucos como oscurecer los bordes de la pantalla al moverse o reducir el campo de visión. No son decisiones estéticas, son decisiones fisiológicas.

Otro punto importante es que el jugador debe tener siempre el control de la cámara. En un juego tradicional podés meter una cinemática con movimientos espectaculares. En VR, si movés la cámara sin que el jugador lo espere, lo descolocás. Por ejemplo, una escena donde el personaje "se cae" y la cámara rota sola puede ser muy impactante visualmente, pero también puede marear fuerte. En VR, ese tipo de decisiones hay que pensarlas dos veces.

La interacción también juega un papel enorme. Cuanto más natural sea, menos esfuerzo mental requiere y más cómoda es la experiencia. Agarrar un objeto con la mano y usarlo como esperarías en la vida real reduce fricción. En cambio, si para abrir una puerta tenés que apretar un botón abstracto del control, rompés esa lógica. El cuerpo espera una cosa y el sistema responde otra.

La escala del mundo es otro detalle que parece menor, pero no lo es. Si una mesa es demasiado chica o una puerta demasiado grande, el cerebro lo detecta al instante. No siempre sabés por qué, pero algo "no cierra". En VR, esa incoherencia afecta la inmersión y también la comodidad. Todo tiene que sentirse proporcionado al cuerpo humano.

Al final, todo converge en una idea bastante directa: en VR no diseñás solo para los ojos, diseñás para el sistema sensorial completo. Cada decisión —rendimiento, UI, movimiento, escala— tiene impacto físico en el jugador. Si lo hacés bien, la persona se olvida de que tiene un visor puesto. Si lo hacés mal, no dura ni cinco minutos antes de querer sacárselo.

Ese es el verdadero filtro de calidad en VR. No es si se ve espectacular, sino si se siente natural.

## Optimización — pensar antes de que explote todo

Cuando hablamos de optimización en Unity, no estamos hablando solo de rendimiento técnico, sino de decisiones de diseño. Cada mesh, cada textura y cada luz tiene un costo. Y en destinos limitados como plataformas VR o Mobile, ese costo se acumula muy rápido.

Podemos dividir todo en dos grandes bloques:
- Lo que maneja el motor (Unity).
- Lo que depende de cómo construimos los assets.

### Técnicas propias del motor (Unity)

#### Occlusion Culling y Frustum Culling
Unity no renderiza todo siempre, y eso es clave.
- **Frustum Culling:** solo dibuja lo que está dentro del campo de visión de la cámara.
- **Occlusion Culling:** además, evita renderizar objetos que están tapados por otros.

Si estás dentro de una casa, no tiene sentido renderizar lo que hay afuera detrás de paredes. Si no usás esto, Unity igual lo procesa → pérdida de rendimiento gratuita.

En Unity 6:
- Se configura desde Window > Rendering > Occlusion Culling
- Funciona mejor con escenas bien estructuradas (no todo unido en una sola mega malla)

#### LOD (Level of Detail)
El LOD consiste en tener varias versiones del mismo objeto con distinta cantidad de polígonos.
- Cerca → modelo detallado
- Lejos → modelo simplificado

Un auto a 2 metros puede tener 10k tris, pero a 50 metros puede ser una caja con ruedas.

Clave: El jugador no ve el detalle lejano, pero la GPU igual lo paga si no optimizás.
En Unity:
- Se usa el componente LOD Group
- Se pueden automatizar transiciones

#### Mip Maps
Los mipmaps son versiones más chicas de una textura. Cuando un objeto está lejos, usar la textura completa genera ruido visual y gasto innecesario.

Ejemplo: Un cartel lejano no necesita textura 2048px → con una versión más chica alcanza.

Decisión importante:
- Activarlos mejora rendimiento y calidad
- Pero consumen más memoria

#### Compresión de audio
El audio también pesa (y mucho).
- .WAV → sin compresión, pesado
- .OGG → comprimido, ideal para juegos

Regla práctica:
- Música → OGG
- Efectos cortos → WAV (si necesitas precisión)

#### Compresión de imágenes
No todas las texturas necesitan ser PNG.
PNG = sin compresión con pérdida → más peso en memoria
Solución:
- Usar formatos comprimidos (ASTC en Quest, por ejemplo)
- Evitar PNG si no hay transparencias

Una pared sin transparencia no necesita PNG → estás desperdiciando memoria.

#### Luces: estáticas vs dinámicas
Las luces son uno de los mayores costos en tiempo real.
Estrategia:
- Luces estáticas (baked) → baratas
- Luces dinámicas → caras

Un shopping con 50 luces dinámicas = desastre
El mismo con baked lighting = rendimiento estable

En Unity:
- Usar Light Baking (Global Illumination)
- Marcar objetos como Static

### Técnicas externas al motor (modelado y assets)

#### Mapas UV y reutilización de texturas
Un buen UV mapping permite:
- Reutilizar texturas
- Reducir memoria
En lugar de 10 texturas para 10 objetos, usás 1 bien organizada.

Menos draw calls + menos consumo

#### Densidad poligonal y limpieza de mallas
Acá entra el criterio del artista/desarrollador.
Problemas comunes:
- Caras duplicadas
- Vértices innecesarios
- Bordes internos invisibles

Un cubo mal optimizado puede tener el doble de polígonos sin razón.
Regla: Si no se ve, no debería existir.

#### Atlas de texturas
Un atlas es una sola imagen que contiene múltiples texturas.
Reduce draw calls
- 10 objetos con 10 materiales → 10 draw calls
- 10 objetos con 1 atlas → 1 draw call

**¿Por qué el atlas debe ser potencia de 2?**
- 64X64
- 128X128
- 512X512
- 1024X1024

Las GPUs trabajan internamente optimizadas para texturas en potencias de 2.

¿Qué pasa si no lo hacés?
- Se generan conversiones internas
- Peor rendimiento
- Más consumo de memoria

Los mipmaps son versiones reducidas de la textura: Esto solo funciona correctamente si la textura es potencia de 2.
Si no:
- Se rompen niveles de mipmap
- Aparecen artefactos visuales
- Peor filtrado

#### Agrupar mallas coherentemente
Unir objetos puede ayudar… o arruinar todo.
Bien usado:
- Menos draw calls
- Mejor rendimiento
Mal usado:
- Rompe occlusion culling
- Renderiza cosas que no deberían verse

Unir todo un edificio en una sola malla puede hacer que Unity lo renderice completo, aunque solo veas una parte.

Todo esto apunta a una idea simple pero que cuesta internalizar:
Optimizar no es "hacer que corra mejor", es evitar hacer trabajo innecesario desde el diseño
- Cada polígono cuenta
- Cada textura cuenta
- Cada luz cuenta
Y en contextos como VR o Mobile: cada error se paga en rendimiento… o en la cara del jugador.
