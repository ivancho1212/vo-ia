-- Script para agregar columna ExpiresAt a la tabla conversations (MySQL)

ALTER TABLE conversations
ADD COLUMN expires_at DATETIME NULL;

-- Crear índice para mejorar búsquedas por expiración
CREATE INDEX IX_conversations_expires_at
ON conversations(expires_at);
