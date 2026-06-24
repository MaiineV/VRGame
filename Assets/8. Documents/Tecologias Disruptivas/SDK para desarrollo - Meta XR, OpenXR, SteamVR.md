# SDK para desarrollo

## SDK de Meta (Meta XR SDK)

El SDK de Meta está diseñado específicamente para los dispositivos de la familia Meta Quest. Su principal ventaja es el acceso directo a las funcionalidades exclusivas del hardware, como el seguimiento de manos, el passthrough para realidad mixta, el seguimiento espacial avanzado, las optimizaciones específicas para Quest y las herramientas de rendimiento desarrolladas por Meta. Esto permite obtener resultados rápidamente y aprovechar al máximo las capacidades de los dispositivos utilizados en el aula.

Otra ventaja importante es la calidad de la documentación y la cantidad de ejemplos disponibles. Debido a que Meta domina actualmente gran parte del mercado de realidad virtual de consumo, existe una gran comunidad de desarrolladores, tutoriales y recursos educativos.

Sin embargo, el principal inconveniente es la dependencia del ecosistema Meta. Muchas funcionalidades avanzadas son propietarias y pueden requerir modificaciones o reemplazos si el proyecto debe migrarse posteriormente a otros visores. Esto puede generar una cierta dependencia tecnológica y reducir la portabilidad del proyecto hacia otras plataformas.

## OpenXR

OpenXR es un estándar abierto desarrollado por el grupo Khronos con el objetivo de unificar el desarrollo de aplicaciones de realidad virtual y realidad aumentada. Su principal ventaja es la interoperabilidad: una aplicación desarrollada correctamente sobre OpenXR puede ejecutarse en dispositivos de distintos fabricantes con mínimos cambios en el código.

Desde una perspectiva académica y profesional, OpenXR representa una solución muy atractiva porque enseña conceptos más generales y menos dependientes de una empresa específica. Los estudiantes aprenden a trabajar sobre estándares de la industria en lugar de herramientas propietarias.

Como desventaja, OpenXR suele ofrecer acceso únicamente a las características estandarizadas entre los distintos fabricantes. Cuando se desea utilizar alguna función muy específica de un visor concreto, normalmente es necesario recurrir a extensiones particulares o complementar el desarrollo con SDKs propios del fabricante. Esto puede aumentar la complejidad del proyecto y la cantidad de trabajo de integración.

## SteamVR SDK

SteamVR fue durante muchos años uno de los pilares del desarrollo de realidad virtual para PC. Su principal fortaleza es la compatibilidad con una gran variedad de visores conectados a computadora, incluyendo dispositivos de Valve, HTC, Meta y otros fabricantes. También ofrece un ecosistema maduro para experiencias de alta fidelidad gráfica gracias al uso de hardware de escritorio.

Otra ventaja importante es la integración con la plataforma Steam, que facilita la distribución y pruebas de aplicaciones destinadas al mercado de PC VR.

No obstante, SteamVR presenta algunas limitaciones en comparación con enfoques más modernos. Actualmente la industria está migrando progresivamente hacia OpenXR como estándar común, por lo que muchas de las funciones que antes eran exclusivas de SteamVR ahora pueden implementarse mediante OpenXR. Además, SteamVR está orientado principalmente al desarrollo para PC y resulta menos adecuado para dispositivos autónomos como Meta Quest cuando se busca una aplicación independiente que funcione sin computadora.

## Comparación general

El SDK de Meta suele ser la mejor opción cuando el objetivo es desarrollar específicamente para Quest 2 o Quest 3 y aprovechar todas sus capacidades. OpenXR es la alternativa más recomendable cuando se busca compatibilidad multiplataforma y una formación alineada con estándares abiertos. SteamVR continúa siendo una herramienta valiosa para experiencias de PC VR, aunque actualmente gran parte de la industria considera a OpenXR como el camino principal para garantizar interoperabilidad y sostenibilidad a largo plazo.
