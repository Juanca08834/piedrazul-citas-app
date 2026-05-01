# Piedrazul - Sistema Web de Reserva de Citas Médicas

Aplicación web tipo SPA para la reserva y gestión de citas médicas del Centro Médico Piedrazul.

El sistema permite a pacientes reservar citas desde la web y al personal interno gestionar citas, disponibilidad médica y exportación de listados.

---

## Tecnologías utilizadas

### Frontend
- React
- Vite
- JavaScript
- CSS

### Backend
- ASP.NET Core Web API
- .NET 8
- Entity Framework Core
- PostgreSQL

### Autenticación
- JWT
- Keycloak
- Modo demo para pruebas rápidas

### Infraestructura
- Docker
- Docker Compose

---

## Funcionalidades principales

### Portal público
- Página de presentación del centro médico.
- Registro de pacientes.
- Inicio de sesión de pacientes.
- Reserva de citas como paciente registrado.
- Reserva rápida como invitado.

### Portal del paciente
- Visualización de citas.
- Actualización del perfil.
- Reserva de nuevas citas.

### Portal interno
- Acceso separado para personal administrativo y médico.
- Gestión de citas por médico/terapista y fecha.
- Creación de citas para pacientes que contactan por WhatsApp o llamada.
- Búsqueda de pacientes por documento.
- Configuración de disponibilidad médica.
- Exportación de citas a PDF.

### Administración
- Configuración de semanas habilitadas para agendamiento.
- Configuración de días de atención.
- Configuración de franjas horarias por profesional.
- Configuración del intervalo de tiempo entre citas.

---

## Estructura del proyecto

```text
piedrazul-citas-app/
├─ backend/
│  ├─ Piedrazul.sln
│  ├─ keycloak/
│  ├─ postgres/
│  ├─ src/
│  │  ├─ Piedrazul.Api/
│  │  ├─ Piedrazul.Application/
│  │  ├─ Piedrazul.Domain/
│  │  └─ Piedrazul.Infrastructure/
│  └─ tests/
├─ src/
├─ docker-compose.yml
├─ .env.example
└─ README.md
