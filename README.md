# AsistenteIA

App móvil **.NET MAUI (C#)** que consulta una IA mediante una API REST con la **API key del propio usuario** y guarda las consultas en un historial local **SQLite**.

## Funciones
- **Inicio**, **Consultar IA**, **Historial**, **Detalle** y **Configuración** (5 pantallas, navegación con `AppShell`).
- API key en campo oculto, guardada con `SecureStorage` (nunca en código, SQLite ni Git).
- Detección local del proveedor por prefijo de la clave; si no es fiable, selector manual. La clave solo se envía al proveedor elegido.
- Prueba de conexión (solicitud de 1 token), carga de modelos y manejo de 400/401/403/404/408/429/5xx, timeout y falta de red.
- CRUD del historial, búsqueda y persistencia tras reiniciar.
- Proveedores en el catálogo (API compatible con OpenAI Chat Completions): Pollinations, OpenAI y Gemini (endpoint compatible). Pollinations (sin clave) y Gemini (con clave propia, modelo `gemini-3.1-flash-lite`) se verificaron contra el servicio real.

## Requisitos
- Windows, .NET SDK 10.0.x, workloads `android` y `maui-windows` (`dotnet workload list`).
- Para Android: teléfono con depuración USB activada (`adb devices` debe mostrarlo como `device`) o emulador.

## Ejecutar
```bash
# Windows
dotnet build AsistenteIA/AsistenteIA.csproj -f net10.0-windows10.0.19041.0
AsistenteIA/bin/Debug/net10.0-windows10.0.19041.0/win-x64/AsistenteIA.exe

# Android (teléfono conectado)
dotnet build AsistenteIA/AsistenteIA.csproj -f net10.0-android -t:Run
```
Luego: **Configuración → pegar API key → Detectar proveedor → Probar conexión → Guardar**.

## Pruebas
```bash
dotnet test tests/AsistenteIA.Tests
# Opcional, contra los servicios reales (variables de entorno, nunca en el repositorio):
ASISTENTEIA_LIVE=1 dotnet test tests/AsistenteIA.Tests
ASISTENTEIA_KEY=<tu_clave> dotnet test tests/AsistenteIA.Tests --filter LiveKey
```

## Estructura
`AsistenteIA/` (MAUI: Views, ViewModels) · `AsistenteIA.Core/` (modelos, cliente REST, configuración, repositorio) · `tests/` · `docs/`.

## Documentación (`docs/`)
Requisitos, Arquitectura, Diagrama de clases (`.drawio` + `.png`), Diagrama de arquitectura, Metodología, Decisiones técnicas, Consumo de API REST, Matriz de riesgos, Auditoría con API key, Manual de usuario y Manual técnico en `.docx` (copias PDF en `docs/pdf/`); casos de prueba en `PRUEBAS.xlsx`; capturas reales de Windows y Android en `docs/evidencias/`.

## Estado
- Compila: Windows y Android (0 errores). iOS y Mac Catalyst quedan fuera.
- Pruebas: 51 xUnit aprobadas (5 en vivo opcionales). Resultados completos en `docs/PRUEBAS.xlsx`.
- Pruebas automatizadas: 51 xUnit en el entorno de desarrollo (incluye pruebas en vivo opcionales con variables de entorno).
- Windows: aplicación ejecutada y capturas de las cinco pantallas en `docs/evidencias`.
- Android: ejecución en un teléfono físico con Android 15 (API 35, arm64), por USB y sin emulador. Casos PRU-14 y PRU-18 a PRU-22 de `docs/PRUEBAS.xlsx`, con capturas `android_*.png`. El estudiante además declara una validación manual en Android; su detalle (casos, fecha, resultados) está pendiente de confirmación.
- No ejecutado: OpenAI contra el servicio; búsqueda en el historial, tiempo de espera y límite 429 en el dispositivo; el flujo con clave real en Windows tras la última corrección.
- Los registros “[Dato de prueba]” de algunas capturas de Windows se sembraron a mano; los de Android provienen de consultas reales.

## Seguridad
`SecureStorage` protege la clave en reposo, pero no ante un dispositivo comprometido; por eso no hay clave por defecto. Detalle en `docs/MATRIZ_RIESGOS.docx`.
