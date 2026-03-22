# Prompt para Gemini - Post de LinkedIn

Copia y pega este prompt en Gemini para generar tu post de LinkedIn:

---

Escríbeme un post de LinkedIn en español. El post debe anunciar un proyecto personal/portfolio que acabo de construir como proyecto de aprendizaje.

**El proyecto se llama ShadowSOC** y es un dashboard de Security Operations Center (SOC) en tiempo real que visualiza ciberataques en un mapa mundial interactivo con estética hacker (fondo negro, colores rojos, fuente monospace).

**Stack tecnológico:**
- C# .NET 10 (backend)
- Angular 21 (frontend)
- Apache Kafka (streaming de eventos)
- RabbitMQ (cola de mensajes)
- SignalR (WebSockets, push en tiempo real)
- Leaflet (mapa interactivo con animaciones de ripple)
- Docker Compose (infraestructura)
- AbuseIPDB API (IPs maliciosas reales, no datos inventados)
- ip-api.com (geolocalización de IPs)

**Lo que hace:**
- Obtiene IPs maliciosas reales de AbuseIPDB
- Simula eventos de seguridad y los pasa por un motor de detección
- Muestra ataques en un mapa oscuro con animaciones
- Terminal feed en tiempo real, estadísticas de países, tipos de ataque, severidad
- Todo en tiempo real sin polling, usando WebSockets

**Requisitos del post:**
- Tono profesional pero con personalidad, que se note pasión por lo que hago
- Debe ser atractivo tanto para desarrolladores como para recruiters/hiring managers
- Mencionar que lo construí como proyecto de aprendizaje para explorar arquitecturas event-driven y sistemas en tiempo real
- Máximo 1300 caracteres (el sweet spot de LinkedIn para engagement)
- Incluir una llamada a la acción / pregunta al final que genere comentarios (por ejemplo, preguntar qué proyecto les gustaría construir, o qué stack usan para real-time, etc.)
- Sugerir al lector que adjunte un video o GIF del dashboard en funcionamiento (incluir una nota entre paréntesis tipo "[Adjuntar video/GIF del dashboard]")
- Incluir hashtags relevantes al final (5-7 hashtags máximo)
- NO usar emojis en exceso, máximo 3-4 en todo el post y solo donde aporten
- La primera línea debe ser un hook potente que capture la atención en el feed
- NO hacer el post demasiado técnico; que un recruiter entienda el valor del proyecto
- Mencionar brevemente la arquitectura (Kafka -> Consumer -> RabbitMQ -> SignalR -> Angular)
