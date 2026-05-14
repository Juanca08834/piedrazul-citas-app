# Microservicio de Notificaciones — Piedrazul Citas

## Por qué existe este microservicio

El sistema principal de citas (`Piedrazul.Api`) maneja la lógica de negocio: crear citas, cancelar, reagendar, cambiar estados. Sin embargo, **notificar al paciente** sobre esos eventos no es responsabilidad de la API de negocio. Si el envío de notificaciones falla, no debería impedir que la cita se guarde; y en un futuro, el canal de notificación puede cambiar (WhatsApp hoy, email mañana, SMS después) sin tocar la API principal.

Por eso se separó en un microservicio independiente (`Piedrazul.Notifications`) conectado mediante una **cola de mensajes con RabbitMQ**.

---

## Qué problema resuelve

| Problema | Sin microservicio | Con microservicio |
|---|---|---|
| Falla el envío de notificación | La cita no se guarda | La cita se guarda, la notificación se reintenta |
| Cambiar canal de notificación | Modificar la API principal | Solo modificar el microservicio |
| La API se vuelve lenta | Notificar en el mismo hilo bloquea la respuesta | La API publica y responde; el microservicio procesa aparte |
| Escalar las notificaciones | Requiere escalar toda la API | Se escala solo el microservicio |

---

## Arquitectura de la solución

```
┌─────────────────────────────────────────────────────────┐
│                      Cliente Web                        │
│                  (React + TypeScript)                   │
└────────────────────────┬────────────────────────────────┘
                         │ HTTP
                         ▼
┌─────────────────────────────────────────────────────────┐
│                   Piedrazul.Api                         │
│   ┌───────────────────────────────────────────────┐     │
│   │           AppointmentService                  │     │
│   │  - CreateAppointment()                        │     │
│   │  - CancelAppointment()                        │     │
│   │  - UpdateStatus()                             │     │
│   └──────────────┬────────────────────────────────┘     │
│                  │ INotificationClient                  │
│                  │ (RabbitMqNotificationClient)         │
└──────────────────┼──────────────────────────────────────┘
                   │ Publica mensaje (JSON)
                   ▼
┌─────────────────────────────────────────────────────────┐
│                     RabbitMQ                            │
│   Exchange: "piedrazul"  (tipo: Topic)                  │
│   ┌──────────────────────────────────┐                  │
│   │  routing key: appointment.created │                 │
│   │  routing key: appointment.status  │                 │
│   └──────────────────────────────────┘                  │
│   Queue: "notifications.appointments"                   │
│   Binding: appointment.*                                │
└──────────────────────┬──────────────────────────────────┘
                       │ Consume mensajes
                       ▼
┌─────────────────────────────────────────────────────────┐
│              Piedrazul.Notifications                    │
│   AppointmentNotificationConsumer (BackgroundService)   │
│   - Recibe el evento                                    │
│   - Loguea el mensaje                                   │
│   - [Extension point] WhatsApp / SMS / Email            │
└─────────────────────────────────────────────────────────┘
```

---

## Cómo está implementado

### Patrón: Publicador / Suscriptor (Pub/Sub)

El sistema usa el patrón **Pub/Sub** con un **Topic Exchange** de RabbitMQ:

- **Publicador**: `RabbitMqNotificationClient` (en `Piedrazul.Infrastructure`)
- **Broker**: RabbitMQ con exchange de tipo Topic
- **Suscriptor**: `AppointmentNotificationConsumer` (en `Piedrazul.Notifications`)

### Flujo detallado

1. El usuario agenda o cancela una cita a través de la API.
2. `AppointmentService` llama a `INotificationClient.NotifyAppointmentCreatedAsync()` o `NotifyAppointmentStatusChangedAsync()`.
3. `RabbitMqNotificationClient` serializa el evento a JSON y lo publica al exchange `piedrazul` con una routing key (`appointment.created` o `appointment.status`).
4. RabbitMQ enruta el mensaje a la cola `notifications.appointments` (binding: `appointment.*`).
5. `AppointmentNotificationConsumer` recibe el mensaje, lo procesa y envía el ACK a RabbitMQ.
6. Si falla el procesamiento, hace **NACK** con `requeue: true` para que RabbitMQ vuelva a entregar el mensaje.

### Patrón Estrategia en la notificación (INotificationClient)

La API no sabe cómo se notifica. Tiene tres implementaciones intercambiables:

| Implementación | Cuándo se usa |
|---|---|
| `RabbitMqNotificationClient` | Cuando hay `RabbitMq:ConnectionString` configurado |
| `HttpNotificationClient` | Cuando hay `Notifications:BaseUrl` configurado |
| `NoOpNotificationClient` | Cuando no hay ninguna configuración (local sin servicios) |

Esto se configura automáticamente en `ServiceCollectionExtensions.cs`.

### Archivos clave

| Archivo | Rol |
|---|---|
| `Piedrazul.Application/Abstractions/Infrastructure/INotificationClient.cs` | Contrato (interfaz) |
| `Piedrazul.Infrastructure/Notifications/RabbitMqNotificationClient.cs` | Publica eventos a RabbitMQ |
| `Piedrazul.Infrastructure/Notifications/HttpNotificationClient.cs` | Alternativa HTTP |
| `Piedrazul.Infrastructure/Notifications/NoOpNotificationClient.cs` | Sin notificaciones |
| `Piedrazul.Infrastructure/ServiceCollectionExtensions.cs` | Decide qué implementación inyectar |
| `Piedrazul.Notifications/Consumers/AppointmentNotificationConsumer.cs` | Consume y procesa los eventos |
| `Piedrazul.Notifications/Program.cs` | Configura el microservicio |

### Configuración de RabbitMQ

| Concepto | Valor |
|---|---|
| Exchange | `piedrazul` |
| Tipo de Exchange | `Topic` |
| Queue | `notifications.appointments` |
| Binding | `appointment.*` (cubre `appointment.created` y `appointment.status`) |
| Durabilidad | Durable (sobrevive reinicios de RabbitMQ) |
| Persistencia | Mensajes persistent (no se pierden si cae el broker) |
| QoS (prefetch) | 10 mensajes a la vez por consumidor |

---

## Guía para correrlo en vivo

### Prerrequisitos

- Docker Desktop instalado y corriendo
- .NET 10 SDK instalado
- Postman (o cualquier cliente HTTP)

### Paso 1 — Levantar la infraestructura con Docker

```bash
# Desde la raíz del proyecto
docker compose up -d
```

Esto levanta:
- **PostgreSQL** en `localhost:5432`
- **Keycloak** en `localhost:8080`
- **Redis** en `localhost:6379`
- **RabbitMQ** en `localhost:5672` | Management UI en `localhost:15672`

Verificar que los contenedores están corriendo:
```bash
docker compose ps
```

### Paso 2 — Verificar RabbitMQ

Abrir el navegador en `http://localhost:15672`

- Usuario: `guest`
- Contraseña: `guest`

En este punto no hay exchanges ni colas creados todavía (se crean automáticamente al arrancar los servicios).

### Paso 3 — Correr la API principal

```bash
cd backend/src/Piedrazul.Api
dotnet run
```

La API queda disponible en `https://localhost:5001` (o el puerto que indique la consola).

### Paso 4 — Correr el microservicio de Notificaciones

En otra terminal:

```bash
cd backend/src/Piedrazul.Notifications
dotnet run
```

Deberías ver en la consola:
```
AppointmentNotificationConsumer started. Listening on 'piedrazul' → 'notifications.appointments'
```

### Paso 5 — Verificar en RabbitMQ Management UI

Volver a `http://localhost:15672`:

1. Ir a la pestaña **Exchanges** — debe aparecer `piedrazul` (tipo Topic, durable)
2. Ir a la pestaña **Queues** — debe aparecer `notifications.appointments` (durable)
3. En la queue, ir a **Bindings** — debe aparecer `appointment.*` desde `piedrazul`

### Paso 6 — Secuencia de pruebas en Postman

Los UUIDs de los providers se generan automáticamente al arrancar la API. El primer paso **siempre** es pedirlos.

---

#### Request 1 — Obtener los providers (sin auth)

```
GET http://localhost:5001/api/public/providers
```

Respuesta esperada:
```json
[
  {
    "id": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "displayName": "Dra. Ana Gómez",
    "specialty": "Medicina general",
    "defaultSlotIntervalMinutes": 30
  },
  {
    "id": "yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy",
    "displayName": "Carlos Martínez",
    "specialty": "Terapia física",
    "defaultSlotIntervalMinutes": 45
  },
  {
    "id": "zzzzzzzz-zzzz-zzzz-zzzz-zzzzzzzzzzzz",
    "displayName": "Dra. Laura Rivera",
    "specialty": "Psicología",
    "defaultSlotIntervalMinutes": 60
  }
]
```

**Copia el `id` del provider que vayas a usar.**

---

#### Request 2 — Ver disponibilidad de un provider (sin auth)

Horarios del seed (para elegir fecha y hora válidas):

| Provider | Día | Horario | Slots |
|---|---|---|---|
| Ana Gómez | Lunes | 08:00–12:00 | 08:00, 08:30, 09:00, ... 11:30 |
| Ana Gómez | Miércoles | 14:00–18:00 | 14:00, 14:30, ... 17:30 |
| Carlos Martínez | Martes | 08:00–13:15 | 08:00, 08:45, ... 12:30 |
| Carlos Martínez | Jueves | 14:00–18:30 | 14:00, 14:45, ... 17:45 |
| Laura Rivera | Viernes | 09:00–17:00 | 09:00, 10:00, ... 16:00 |

> **Nota:** El seed crea una cita de ejemplo el próximo lunes a las 08:00 con Ana Gómez. Ese slot ya estará ocupado.

```
GET http://localhost:5001/api/public/providers/{providerId}/availability?date=2026-05-11
```

Reemplaza `{providerId}` con el UUID copiado y ajusta la fecha al día correspondiente al horario del provider elegido:
- Ana Gómez → lunes `2026-05-11` o miércoles `2026-05-13`
- Carlos Martínez → martes `2026-05-12` o jueves `2026-05-14`
- Laura Rivera → viernes `2026-05-15`

Respuesta esperada (fragmento):
```json
[
  { "startTime": "08:30", "endTime": "09:00", "available": true },
  { "startTime": "09:00", "endTime": "09:30", "available": true }
]
```

Copia un `startTime` disponible para el siguiente paso.

---

#### Request 3 — Crear cita (dispara `appointment.created` en RabbitMQ)

```
POST http://localhost:5001/api/public/appointments
Content-Type: application/json
```

Body (ajusta `providerId`, `appointmentDate` y `startTime` con los valores obtenidos arriba):
```json
{
  "providerId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "appointmentDate": "2026-05-11",
  "startTime": "08:30",
  "documentNumber": "1000000099",
  "firstName": "Juan",
  "lastName": "Prueba",
  "phone": "3109876543",
  "gender": "Male",
  "birthDate": "2000-03-15",
  "bookAsGuest": true
}
```

Respuesta esperada (200):
```json
{
  "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "providerName": "Dra. Ana Gómez",
  ...
  "status": "Programada"
}
```

**Copia el `id` de la cita para el siguiente paso.**

En la terminal de `Piedrazul.Notifications` deberías ver inmediatamente:
```
[appointment.created] {"Id":"aaaaaaaa-...","AppointmentDate":"2026-05-11","StartTime":"08:30",...}
```

---

#### Request 4 — Buscar la cita por documento (para confirmar el ID)

```
GET http://localhost:5001/api/public/appointments/by-document?document=1000000099
```

Devuelve todas las citas del paciente con ese documento. Confirma el `id` de la cita creada.

---

#### Request 5 — Cambiar estado de la cita (dispara `appointment.status` en RabbitMQ)

Este endpoint requiere rol `Admin`, `Scheduler` o `Doctor` (política `InternalStaff`). En modo Development **no hay JWT**: el `DevelopmentAuthHandler` lee headers HTTP custom para asignar identidad y roles. Solo necesitas pasar un header extra en Postman.

```
PATCH http://localhost:5001/api/internal/appointments/{appointmentId}/status
Content-Type: application/json
X-Debug-Roles: Admin
```

Reemplaza `{appointmentId}` con el UUID de la cita obtenido en el paso anterior.

Headers requeridos en Postman:

| Header | Valor |
|---|---|
| `Content-Type` | `application/json` |
| `X-Debug-Roles` | `Admin` |

Body:
```json
{
  "status": "Cancelada"
}
```

Valores válidos para `status`: `Programada`, `Cancelada`, `Completada`, `No asistió`

> **Cómo funciona el mock de auth:** `DevelopmentAuthHandler` intercepta toda request, lee los headers `X-Debug-Subject`, `X-Debug-Name`, `X-Debug-Email` y `X-Debug-Roles`, y construye un `ClaimsPrincipal` sin validar ningún token. Si no mandas los headers, usa los defaults (`rol: Patient`), por eso da 403 sin el header.

En la terminal de `Piedrazul.Notifications` verás:
```
[appointment.status] {"Id":"aaaaaaaa-...","Status":"Cancelled","AppointmentDate":"2026-05-11",...}
```

---

### Paso 7 — Verificar en RabbitMQ Management UI

En `http://localhost:15672` → pestaña **Queues** → `notifications.appointments`:

- **Ready**: 0 (todos los mensajes fueron consumidos)
- **Unacked**: 0
- Pestaña **Message rates**: verás picos en el gráfico por cada cita creada o cancelada

---

## Conceptos para la sustentación

### ¿Qué es RabbitMQ?

RabbitMQ es un **message broker** que implementa el protocolo **AMQP (Advanced Message Queuing Protocol)**. Actúa como intermediario: un servicio publica mensajes y otro los consume, sin que se conozcan directamente entre sí.

### ¿Qué es un Exchange?

Un Exchange es el componente de RabbitMQ que **recibe los mensajes del publicador y los enruta a las colas** según reglas. Existen cuatro tipos:

| Tipo | Comportamiento |
|---|---|
| **Direct** | Enruta al queue cuya routing key coincida exactamente |
| **Topic** | Enruta por patrón con wildcards (`*` = una palabra, `#` = cero o más) |
| **Fanout** | Enruta a todos los queues enlazados, ignorando la routing key |
| **Headers** | Enruta por atributos del encabezado del mensaje |

En este proyecto se usa **Topic** porque permite escalar a más tipos de eventos con el mismo exchange.

### ¿Qué es un Queue?

Una **Queue** es el buffer donde RabbitMQ almacena los mensajes hasta que un consumidor los procesa. Las propiedades importantes:

- **Durable**: la queue sobrevive reinicios del broker
- **Exclusive**: solo puede ser usada por una conexión
- **Auto-delete**: se borra cuando no hay consumidores

### ¿Qué es un Binding?

Un Binding conecta un Exchange con una Queue y define la **routing key pattern** que determina qué mensajes llegan a esa queue.

En este proyecto: `appointment.*` captura tanto `appointment.created` como `appointment.status`.

### ¿Qué es un BackgroundService?

En .NET, un `BackgroundService` es una clase que implementa `IHostedService` y corre en segundo plano durante toda la vida del proceso. Es el mecanismo estándar para consumers, workers y tareas periódicas. No bloquea el servidor HTTP principal.

### ¿Por qué usar mensajería en vez de llamada HTTP directa?

| Criterio | HTTP directo | Mensajería (RabbitMQ) |
|---|---|---|
| Acoplamiento | Alto (API conoce URL del servicio) | Bajo (solo conoce el exchange) |
| Disponibilidad | Si el destino cae, se pierde la notificación | Los mensajes se guardan en la queue |
| Resiliencia | Sin reintentos por defecto | NACK + requeue automático |
| Escalabilidad | Difícil escalar consumidor independientemente | El consumer escala sin tocar la API |
| Consistencia | La respuesta HTTP puede llegar tarde y bloquear | Fire-and-forget, la API responde de inmediato |

### ¿Qué es ACK y NACK en RabbitMQ?

Cuando un consumidor recibe un mensaje:
- **ACK (Acknowledgement)**: confirma que procesó el mensaje correctamente. RabbitMQ lo elimina de la cola.
- **NACK (Negative Acknowledgement)**: indica que el procesamiento falló. Con `requeue: true`, RabbitMQ vuelve a encolar el mensaje para que otro consumidor (o el mismo) lo intente de nuevo.

En `AppointmentNotificationConsumer`, si el procesamiento lanza una excepción se hace NACK, garantizando que **ningún mensaje se pierde**.

### ¿Qué es el patrón Pub/Sub?

**Publicador/Suscriptor** es un patrón de comunicación asíncrona donde:
- El **publicador** emite eventos sin saber quién los escucha
- El **suscriptor** se registra para recibir ciertos eventos sin saber quién los publica
- Un **broker** (RabbitMQ) media entre ambos

Ventaja principal: **desacoplamiento**. La API puede seguir funcionando aunque el microservicio de notificaciones esté caído.

### ¿Por qué durable y persistent?

- **Exchange/Queue durable**: sobreviven si RabbitMQ se reinicia. Sin esto, se pierde la topología.
- **Mensajes persistent** (`Persistent = true` en `BasicProperties`): se escriben a disco. Sin esto, si RabbitMQ cae con mensajes en cola, se pierden.

Ambas propiedades juntas garantizan **at-least-once delivery**: el mensaje se entrega al menos una vez.

### ¿Qué es el patrón Estrategia aplicado aquí?

`INotificationClient` es una abstracción que permite intercambiar la implementación sin cambiar el código que la usa. El `ServiceCollectionExtensions` decide en tiempo de configuración cuál implementación inyectar:

```
appsettings.Development.json con RabbitMq:ConnectionString  →  RabbitMqNotificationClient
appsettings con Notifications:BaseUrl                        →  HttpNotificationClient
Sin configuración                                            →  NoOpNotificationClient
```

Esto es el **patrón Estrategia** combinado con **Inyección de Dependencias**.

---

## Preguntas frecuentes de sustentación

**¿Por qué no se usa un HttpClient directo para notificar?**
Porque el HTTP es síncrono y acoplado. Si el microservicio de notificaciones está caído, el HTTP falla y se pierde la notificación. Con RabbitMQ, el mensaje queda encolado y se procesa cuando el microservicio vuelva.

**¿Qué pasa si RabbitMQ se cae mientras hay mensajes pendientes?**
Los mensajes son `persistent` y las queues son `durable`. RabbitMQ los persiste a disco. Al reiniciar, los mensajes siguen ahí esperando.

**¿Qué pasa si el microservicio falla al procesar un mensaje?**
Hace NACK con `requeue: true`. El mensaje vuelve a la queue y se reintenta. Esto garantiza que no se pierde.

**¿Cómo se escalaría esto en producción?**
Se levantarían múltiples instancias de `Piedrazul.Notifications`. RabbitMQ distribuye los mensajes entre las instancias automáticamente (round-robin). El `prefetchCount: 10` controla cuántos mensajes procesa cada instancia concurrentemente.

**¿Por qué Topic Exchange y no Direct o Fanout?**
Topic permite filtrar por patrón. Hoy tenemos `appointment.*`. Si mañana se agrega `provider.created`, se puede crear otro binding sin cambiar nada. Fanout no permite filtrar; Direct no permite wildcards.
