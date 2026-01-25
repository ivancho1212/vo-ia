-- Script para eliminar bots huérfanos y sus dependencias
-- Ejecutar en orden

-- Desactivar safe mode temporalmente
SET SQL_SAFE_UPDATES = 0;

-- 1. Primero eliminar mensajes (no tienen foreign keys que bloqueen)
DELETE m FROM `chatbot_platform`.`messages` m
INNER JOIN `chatbot_platform`.`conversations` c ON m.conversation_id = c.id
WHERE c.bot_id IN (1, 2);

-- 2. Eliminar conversaciones (antes de public_users porque dependen de ellos)
DELETE FROM `chatbot_platform`.`conversations` WHERE `bot_id` IN (1, 2);

-- 3. Ahora sí eliminar public_users
DELETE FROM `chatbot_platform`.`public_users` WHERE `bot_id` IN (1, 2);

-- 3. Eliminar mensajes de bienvenida
DELETE FROM `chatbot_platform`.`bot_welcome_messages` WHERE `bot_id` IN (1, 2);

-- 4. Eliminar campos de captura de datos
DELETE FROM `chatbot_platform`.`bot_data_capture_fields` WHERE `bot_id` IN (1, 2);

-- 5. Eliminar integraciones
DELETE FROM `chatbot_platform`.`bot_integrations` WHERE `bot_id` IN (1, 2);

-- 6. Eliminar fases
DELETE FROM `chatbot_platform`.`bot_phases` WHERE `bot_id` IN (1, 2);

-- 7. Eliminar configuraciones de entrenamiento
DELETE FROM `chatbot_platform`.`bot_training_configs` WHERE `bot_id` IN (1, 2);

-- 8. Eliminar sesiones de entrenamiento
DELETE FROM `chatbot_platform`.`bot_training_sessions` WHERE `bot_id` IN (1, 2);

-- 9. Eliminar custom prompts
DELETE FROM `chatbot_platform`.`bot_custom_prompts` WHERE `bot_id` IN (1, 2);

-- 10. Eliminar acciones de bot
DELETE FROM `chatbot_platform`.`bot_actions` WHERE `bot_id` IN (1, 2);

-- 11. Eliminar URLs de entrenamiento
DELETE FROM `chatbot_platform`.`training_urls` WHERE `bot_id` IN (1, 2);

-- 12. Eliminar documentos subidos
DELETE FROM `chatbot_platform`.`uploaded_documents` WHERE `bot_id` IN (1, 2);

-- 13. Eliminar textos personalizados de entrenamiento
DELETE FROM `chatbot_platform`.`training_custom_texts` WHERE `bot_id` IN (1, 2);

-- 14. Eliminar sesiones de datos de entrenamiento
DELETE FROM `chatbot_platform`.`training_data_sessions` WHERE `bot_id` IN (1, 2);

-- 15. Eliminar imágenes generadas
DELETE FROM `chatbot_platform`.`generated_images` WHERE `bot_id` IN (1, 2);

-- 16. Finalmente eliminar los bots
DELETE FROM `chatbot_platform`.`bots` WHERE `id` IN (1, 2);

-- Reactivar safe mode
SET SQL_SAFE_UPDATES = 1;

-- Verificar que se eliminaron
SELECT COUNT(*) as remaining_bots FROM `chatbot_platform`.`bots` WHERE `id` IN (1, 2);
