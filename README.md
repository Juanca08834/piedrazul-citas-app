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
- Creación de citas para pacientes por WhatsApp o llamada.
- Búsqueda de pacientes por documento.
- Configuración de disponibilidad médica.
- Exportación de citas a PDF.

### Administración
- Configuración de semanas habilitadas.
- Configuración de días de atención.
- Configuración de franjas horarias.
- Configuración del intervalo entre citas.

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
```

---

## Requisitos previos

- Node.js 20 o superior
- .NET 8 SDK
- Docker Desktop
- Visual Studio 2022 o superior

---

# 🚀 Ejecución rápida (Modo Demo)

---

## 1. Clonar el repositorio

```bash
git clone URL_DEL_REPOSITORIO
cd piedrazul-citas-app
```

---

## 2. Levantar PostgreSQL

```bash
docker compose up -d postgres
```

---

## 3. Ejecutar Backend

1. Abre:

```
backend/Piedrazul.sln
```

2. Configura:

```json
"Authentication": {
  "Mode": "Development"
}
```

3. Ejecuta el proyecto

API:

```
http://localhost:5184
```

Swagger:

```
http://localhost:5184/swagger
```

---

## 4. Configurar Frontend

Crear archivo `.env.local`:

```env
VITE_API_URL=http://localhost:5184
VITE_AUTH_MODE=demo
VITE_KEYCLOAK_URL=http://localhost:8080
VITE_KEYCLOAK_REALM=piedrazul
VITE_KEYCLOAK_CLIENT_ID=piedrazul-web
```

---

## 5. Instalar dependencias

```bash
npm install
```

---

## 6. Ejecutar frontend

```bash
npm run dev
```

App:

```
http://localhost:5173
```

---

# 🧪 Uso del sistema

## Portal público

```
http://localhost:5173
```

Permite:
- Reservar como invitado
- Registrarse
- Iniciar sesión

---

## Portal paciente

```
http://localhost:5173/portal/paciente
```

---

## Portal interno

```
http://localhost:5173/portal/interno/login
```

Modo demo incluye:

- Administrador
- Agendador
- Médico

---

# 🔐 Ejecución con Keycloak

---

## 1. Levantar servicios

```bash
docker compose up -d
```

---

## 2. Configurar backend

```json
{
  "Authentication": {
    "Mode": "Keycloak",
    "Authority": "http://localhost:8080/realms/piedrazul",
    "Audience": "piedrazul-api",
    "RequireHttpsMetadata": false
  }
}
```

---

## 3. Configurar frontend

```env
VITE_API_URL=http://localhost:5184
VITE_AUTH_MODE=keycloak
VITE_KEYCLOAK_URL=http://localhost:8080
VITE_KEYCLOAK_REALM=piedrazul
VITE_KEYCLOAK_CLIENT_ID=piedrazul-web
```

---

## Usuarios de prueba

Administrador:
```
admin.demo / Admin123*
```

Agendador:
```
agenda.demo / Agenda123*
```

Médico:
```
medico.demo / Medico123*
```

---

# 📡 Endpoints principales

## Públicos

```
GET /api/public/info
GET /api/public/providers
POST /api/public/appointments
```

## Paciente

```
GET /api/patient/profile
GET /api/patient/appointments
```

## Interno

```
GET /api/internal/appointments
POST /api/internal/appointments
GET /api/internal/appointments/export/pdf
```

---

# ✔ Validaciones

- Documento: 5–20 dígitos
- Celular: 7–15 dígitos
- Nombres válidos
- Email opcional
- Intervalos entre citas válidos
- Franjas disponibles obligatorias

---

# 🛠 Comandos útiles

Frontend:

```bash
npm run build
npm run preview
```

Backend:

```bash
dotnet restore
dotnet build
dotnet run
dotnet test
```

---

# ⚠ Problemas comunes

## Frontend no conecta

Verifica:

```
VITE_API_URL=http://localhost:5184
```

---

## Error con node_modules

Linux/Mac:

```bash
rm -rf node_modules package-lock.json
npm install
```

Windows:

```powershell
Remove-Item -Recurse -Force node_modules
Remove-Item package-lock.json
npm install
```

---

## Base de datos no funciona

```bash
docker compose up -d postgres
```

---

## Keycloak falla

```bash
docker compose up -d
```

---

# 🧠 Notas

- Arquitectura por capas (Domain, Application, Infrastructure, API)
- Uso de JWT con Keycloak
- Modo demo para pruebas rápidas
- Exportación de citas en PDF
- Preparado para crecimiento y mantenimiento

---

# 👨‍💻 Autores

Proyecto académico  
Ingeniería de Software III  
Universidad del Cauca
