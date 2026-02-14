# 📧 Configuración del Sistema de Notificaciones por Email

## ✅ Estado de la Implementación

El sistema de notificaciones por email ha sido completamente implementado y está listo para usar. Los siguientes componentes están instalados:

### Archivos Creados
1. ✅ **Services/EmailService.cs** - Servicio de envío de emails
2. ✅ **Workers/EmailNotificationWorker.cs** - Worker para notificaciones en batch
3. ✅ **Program.cs** - Configuración y registro de servicios
4. ✅ **appsettings.json** - Configuración de SMTP
5. ✅ **Hubs/ChatHub.cs** - Integración con SignalR para detección de admins offline
6. ✅ **Models/conversations/Conversation.cs** - Campos agregados: `UnreadAdminMessages`, `AssignedUserId`
7. ✅ **Migrations** - Migración aplicada exitosamente a la base de datos

### Cambios en Base de Datos
```sql
ALTER TABLE Conversations ADD unread_admin_messages INT NOT NULL DEFAULT 0;
ALTER TABLE Conversations ADD assigned_user_id INT NULL;
CREATE INDEX IX_Conversations_assigned_user_id ON Conversations (assigned_user_id);
ALTER TABLE Conversations ADD CONSTRAINT FK_Conversations_AspNetUsers_assigned_user_id 
    FOREIGN KEY (assigned_user_id) REFERENCES AspNetUsers (Id);
```

---

## 🔧 Configuración Requerida

### Paso 1: Configurar Credenciales SMTP

Debes configurar las credenciales SMTP en tu archivo `.env` o variables de entorno. Las notificaciones están **DESHABILITADAS por defecto** para evitar errores.

#### Opción A: Usar Gmail (Recomendado para desarrollo)

1. **Crear App Password de Gmail**:
   - Ve a tu cuenta de Google: https://myaccount.google.com/security
   - Busca "Contraseñas de aplicaciones" (App Passwords)
   - Selecciona "Correo" y "Otro dispositivo personalizado"
   - Copia la contraseña de 16 caracteres generada

2. **Configurar en `.env`**:
```env
SMTP_USERNAME=tu-email@gmail.com
SMTP_PASSWORD=xxxx xxxx xxxx xxxx  # App Password (16 caracteres)
SMTP_FROM_EMAIL=tu-email@gmail.com
```

#### Opción B: Usar SendGrid (Recomendado para producción)

1. **Crear cuenta en SendGrid**: https://sendgrid.com/
2. **Crear API Key** en Settings → API Keys
3. **Configurar en `.env`**:
```env
SMTP_USERNAME=apikey
SMTP_PASSWORD=SG.xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx  # SendGrid API Key
SMTP_FROM_EMAIL=noreply@tu-dominio.com
```

4. **Modificar appsettings.json**:
```json
"EmailSettings": {
  "SmtpHost": "smtp.sendgrid.net",
  "SmtpPort": 587,
  ...
}
```

### Paso 2: Habilitar Notificaciones

Una vez configuradas las credenciales SMTP, habilita las notificaciones editando `appsettings.json`:

```json
"EmailSettings": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": 587,
  "SmtpUsername": "${SMTP_USERNAME}",
  "SmtpPassword": "${SMTP_PASSWORD}",
  "FromEmail": "${SMTP_FROM_EMAIL}",
  "FromName": "VOIA Notifications",
  "EnableSsl": true,
  "EnableNotifications": true,  // ⬅️ CAMBIAR A true
  "DashboardUrl": "http://localhost:3000"  // URL del dashboard para el botón del email
}
```

### Paso 3: Configurar URL de Producción (Opcional)

Para producción, actualiza la URL del dashboard:

```json
"DashboardUrl": "https://tu-dominio.com/admin"
```

---

## 📝 Cómo Funciona

### Estrategia: Notificación Inmediata + Batch cada 15 minutos

El sistema implementa un enfoque **híbrido**:

1. **Notificación Inmediata (ChatHub)**:
   - Cuando un usuario envía un mensaje, el sistema verifica si hay admins online
   - Se considera "online" cualquier admin que haya tenido actividad en los últimos 5 minutos
   - Si **NO hay admins online**:
     * Se incrementa el contador `UnreadAdminMessages`
     * Se envía un email inmediato al admin asignado (o a cualquier admin si no hay asignación)
   - Si **SÍ hay admins online**:
     * No se envía email (se asume que el admin verá el mensaje en el dashboard)

2. **Notificación por Lote (EmailNotificationWorker)**:
   - Cada **15 minutos**, el worker verifica todas las conversaciones con mensajes sin leer
   - Agrupa conversaciones por admin asignado
   - Envía un **email de resumen** con el total de mensajes sin leer por conversación
   - Solo envía emails a admins que están **offline** (sin actividad en 5 minutos)

### Detección de Admins Online

El sistema consulta la tabla `ActivityLogs` para determinar si un admin está activo:

```csharp
var recentlyActiveAdminIds = await dbContext.ActivityLogs
    .Where(log => log.CreatedAt >= DateTime.UtcNow.AddMinutes(-5))
    .Select(log => log.UserId)
    .Distinct()
    .ToListAsync();
```

Si un admin ha registrado actividad en los últimos 5 minutos, se considera **online**.

---

## 🎨 Plantillas de Email

### Email de Notificación Inmediata
- **Asunto**: "Nuevo mensaje en Sesión {conversationId}"
- **Contenido**: 
  * Número de mensajes sin leer
  * Preview del último mensaje (truncado a 150 caracteres)
  * Botón para acceder directamente a la conversación

### Email de Resumen por Lote
- **Asunto**: "Tienes {totalUnread} mensajes sin leer en {conversationCount} conversaciones"
- **Contenido**:
  * Lista de conversaciones con cantidad de mensajes sin leer
  * Botón para acceder al dashboard de conversaciones

Ambos emails están diseñados con HTML responsive y colores del tema VOIA (#17a2b8).

---

## 🧪 Pruebas

### Test 1: Email Inmediato (Admin Offline)
1. **Deslogueate** del dashboard admin
2. Espera **5 minutos** (para que expire tu actividad)
3. Envía un mensaje desde el widget como usuario
4. Verifica que llegue un email inmediato al admin

### Test 2: Sin Email (Admin Online)
1. **Logueate** en el dashboard admin
2. Envía un mensaje desde el widget como usuario
3. **NO debería llegar email** (el admin está online)

### Test 3: Email por Lote
1. Deslogueate del dashboard
2. Envía **varios mensajes** desde diferentes conversaciones
3. Espera **hasta 15 minutos**
4. Verifica que llegue un email de resumen agrupando todas las conversaciones

### Logs de Diagnóstico

Revisa los logs de la aplicación para depurar:

```bash
# Ver logs en tiempo real
tail -f Logs/voia-api-*.txt | grep "📧"

# Buscar emails enviados exitosamente
grep "✅ Email enviado exitosamente" Logs/voia-api-*.txt

# Buscar errores de email
grep "❌ Error al enviar email" Logs/voia-api-errors-*.txt
```

Emojis de diagnóstico:
- `📧` - Operaciones de email
- `✅` - Email enviado correctamente
- `❌` - Error al enviar email
- `⚠️` - Advertencia (admin sin email configurado)
- `👤` - Admin está online, no enviar email

---

## 🔐 Seguridad

### Contraseñas Seguras
- **NUNCA** commites las credenciales SMTP al repositorio
- Usa **variables de entorno** o **Azure Key Vault** en producción
- Gmail App Passwords son más seguras que usar la contraseña real

### Rate Limiting
- Gmail: 500 emails/día (límite gratuito)
- SendGrid: 100 emails/día (plan gratuito), hasta 100,000/día (planes pagos)
- El worker corre cada 15 minutos para evitar spam

### Privacidad
- Los emails contienen solo un **preview truncado** del mensaje (150 caracteres)
- No se incluye información sensible del usuario
- Email marcado como "no responder"

---

## 🚀 Próximas Mejoras (Opcional)

Si deseas extender el sistema, considera:

1. **Preferencias de Notificación por Admin**:
   - Panel en el frontend para que cada admin configure:
     * Habilitar/deshabilitar notificaciones
     * Frecuencia de emails por lote
     * Notificaciones solo para conversaciones asignadas

2. **Plantillas Personalizables**:
   - Migrar las plantillas HTML a archivos `.cshtml` o `.html`
   - Permitir personalización desde el dashboard

3. **Notificaciones Push**:
   - Integrar con Firebase Cloud Messaging
   - Enviar push notifications a móviles

4. **Estadísticas**:
   - Tracking de emails abiertos (open rate)
   - Clicks en el botón "Ver conversación"

---

## ❓ Solución de Problemas

### Problema: "No se envían emails"
- ✅ Verifica que `EnableNotifications: true` en appsettings.json
- ✅ Verifica que las credenciales SMTP estén correctas en `.env`
- ✅ Revisa logs: `grep "❌" Logs/voia-api-errors-*.txt`
- ✅ Prueba con un script simple: https://www.c-sharpcorner.com/article/send-email-in-asp-net-core/

### Problema: "Emails llegan a spam"
- ✅ Configura SPF/DKIM en tu dominio (SendGrid)
- ✅ Usa un email `noreply@tu-dominio.com` verificado
- ✅ Evita palabras spam ("GRATIS", "URGENTE", etc.)

### Problema: "Emails se envían aunque el admin esté online"
- ✅ Verifica que `ActivityLogs` se esté poblando correctamente
- ✅ Revisa logs: `grep "👤 Hay admins online" Logs/voia-api-*.txt`
- ✅ Confirma que el admin tenga actividad reciente (últimos 5 min)

---

## 📚 Referencias

- [SendGrid Docs](https://docs.sendgrid.com/)
- [Gmail App Passwords](https://support.google.com/accounts/answer/185833)
- [System.Net.Mail Docs](https://learn.microsoft.com/en-us/dotnet/api/system.net.mail)
- [BackgroundService Docs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services)

---

**🎉 ¡Sistema de notificaciones por email completamente implementado!**

Solo falta configurar las credenciales SMTP y habilitar las notificaciones. Una vez hecho, el sistema comenzará a enviar emails automáticamente cuando los admins estén offline.
