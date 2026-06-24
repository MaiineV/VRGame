# Cómo probar y exportar el juego

> Página **aparte** del Resumen (esto NO es teoría): emulador, pruebas, exportación y requisitos de entrega técnica.

## Probar con Quest Link / Air Link (sin compilar)
1. Activar **Modo Desarrollador** desde la app Meta Quest del teléfono.
2. En Oculus para PC: `Settings → Beta` → activar *Pass Through over Meta Quest Link*.
3. Conexión:
   - **Quest Link (cable USB-C):** verificar en *Devices* que el casco aparezca en verde.
   - **Air Link (wifi):** equivalente inalámbrico (PC y casco en la misma red).
4. En el casco: activar Quest Link / Air Link.
5. En Unity: **Play** → la escena corre en el casco en tiempo real.

## Emulador y verificación de errores
- Probar el juego **exportado** tanto en **emulador** como en el **dispositivo de destino**: a veces corre bien en el motor pero **falla en la build**.
- **Bugs inhabilitantes** (un botón que no funciona, físicas rotas, no ejecución de elementos clave) → **desaprueban** el trabajo. Minimizarlos.
- Ante comportamientos anómalos del motor, **consultar al docente** (el desconocimiento no justifica el fallo).

## Compilar e instalar (Build APK)
1. `File → Build Settings` → plataforma **Android** (Switch Platform).
2. Agregar la escena a la lista.
3. En **Run Device** elegir el casco (Refresh si no aparece).
4. **Build and Run** → carpeta para el APK → se instala y ejecuta en el casco.

## Requisitos de rendimiento (entrega)
- **Tiempos de carga bajos.**
- **Más de 75 FPS en todo momento.**
- Usar **formatos específicos de compilación** para los exports y **eliminar assets no usados** de las carpetas raíz.
- Render: **OpenGL ES3** o **Vulkan** según el setup (Vulkan si el casco se cierra al ejecutar); aplicar el **Project Setup Tool**.
