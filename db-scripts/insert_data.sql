-- Insertar roles
INSERT IGNORE INTO roles (name, description) VALUES
  ('Admin', 'Administrador del sistema'),
  ('User', 'Usuario común');

-- Insertar tipos de documento
INSERT IGNORE INTO document_types (name, abbreviation) VALUES
  ('Cédula de Ciudadanía', 'CC'),
  ('NIT', 'NIT'),
  ('Pasaporte', 'PPT');

-- Insertar usuarios con los IDs correctos de roles y tipos de documento
INSERT INTO users (
  role_id, document_type_id, name, email, password, phone, address,
  document_number, document_photo_url, avatar_url, is_verified
) VALUES
  (32, 23, 'Admin User', 'admin2@example.com', 'adminpassword', '3000000000',
   '123 Admin Street', '1234567890', 'url_to_document_photo', 'url_to_avatar', 1),
  (33, 24, 'Regular User', 'user2@example.com', 'userpassword', '3000000001',
   '456 User Avenue', '0987654321', 'url_to_document_photo', 'url_to_avatar', 0);

-- Insertar bots asociados a los usuarios (usando los IDs correctos)
INSERT INTO bots (user_id, name, description, api_key, model_used, is_active) 
VALUES 
(29, 'Admin Bot', 'A bot for admin', 'admin_api_key', 'gpt-4', 1),
(30, 'User Bot', 'A bot for user', 'user_api_key', 'gpt-4', 1);


-- Insertar documentos subidos (usando los IDs correctos de bots y usuarios)
INSERT INTO uploaded_documents (bot_id, user_id, file_name, file_type, file_path, indexed) 
VALUES 
(15, 29, 'document1.pdf', 'pdf', 'path/to/document1.pdf', TRUE),  -- Admin Bot
(16, 30, 'document2.docx', 'docx', 'path/to/document2.docx', FALSE);  -- User Bot

-- Insertar estilos personalizados del widget (usando los IDs correctos de bots)
INSERT INTO bot_styles (bot_id, theme, primary_color, secondary_color, font_family, avatar_url, position, custom_css)
VALUES
  (15, 'light', '#3498db', '#ffffff', 'Arial', 'path/to/avatar1.png', 'bottom-right', '/* custom CSS for Admin Bot */'),  -- Admin Bot
  (16, 'dark', '#2c3e50', '#ecf0f1', 'Verdana', 'path/to/avatar2.png', 'bottom-left', '/* custom CSS for User Bot */');  -- User Bot

-- Insertar conversaciones (usando los IDs correctos de los bots)
INSERT INTO conversations (bot_id, user_id, title)
VALUES
  (15, 29, 'Admin Bot Conversation'),  -- Conversación con el Admin Bot
  (16, 30, 'User Bot Conversation');    -- Conversación con el User Bot

-- Actualizar la conversación con id = 3
UPDATE conversations
SET user_message = 'Hola, soy Admin User preguntando algo.',
    bot_response = 'Hola Admin User, aquí está tu respuesta.'
WHERE id = 3;

-- Actualizar la conversación con id = 4
UPDATE conversations
SET user_message = 'Hola, soy Regular User, necesito ayuda.',
    bot_response = 'Hola Regular User, ¿en qué puedo ayudarte?'
WHERE id = 4;

-- Actualizar la conversación con id = 5
UPDATE conversations
SET user_message = 'Admin User haciendo otra consulta.',
    bot_response = 'Admin User, esta es tu otra respuesta.'
WHERE id = 5;

-- Actualizar la conversación con id = 6
UPDATE conversations
SET user_message = 'Regular User preguntando nuevamente.',
    bot_response = 'Aquí tienes una nueva respuesta para ti, Regular User.'
WHERE id = 6;


-- Insertar conversaciones
INSERT INTO conversations (bot_id, user_id, title)
VALUES
  (15, 29, 'Admin Bot Conversation'),  -- Conversación con el Admin Bot
  (16, 30, 'User Bot Conversation');    -- Conversación con el User Bot
-- Insertar interacciones (prompts)
-- Insertar interacciones (prompts)
INSERT INTO prompts (bot_id, user_id, conversation_id, prompt_text, response_text, tokens_used) 
VALUES 
  (15, 29, 3, 'Hello Admin Bot', 'Hello! How can I assist you?', 10),  -- Interacción con Admin Bot 
  (16, 30, 4, 'Hello User Bot', 'Hello! How can I help you today?', 12);  -- Interacción con User Bot

-- Insertar planes
INSERT INTO plans (name, description, price, max_tokens, bots_limit)
VALUES
  ('Basic Plan', 'Access to basic features', 9.99, 5000, 1),
  ('Premium Plan', 'Access to premium features', 19.99, 15000, 3);

-- Insertar suscripciones
INSERT INTO subscriptions (user_id, plan_id, expires_at, status)
VALUES
  (29, 1, '2026-04-21 00:00:00', 'active'),  -- Suscripción para Admin User (Basic Plan)
  (30, 2, '2026-04-21 00:00:00', 'active');  -- Suscripción para Regular User (Premium Plan)

-- Insertar facturación por uso
INSERT INTO billing (user_id, bot_id, subscription_id, amount, tokens_used)
VALUES
  (29, 15, 1, 5.99, 200),  -- Facturación para Admin User con Admin Bot
  (30, 16, 2, 9.99, 300);  -- Facturación para Regular User con User Bot


INSERT INTO openai_usage_summary (month, total_tokens, total_amount)
VALUES 
  ('2025-04', 50000, 10.00),  -- Resumen de abril 2025
  ('2025-05', 60000, 12.00);  -- Resumen de mayo 2025


INSERT INTO bot_integrations (bot_id, integration_type, allowed_domain, api_token)
VALUES 
  (15, 'widget', 'example.com', 'api_token_1'),  -- Integración para Admin Bot
  (16, 'api', 'example.org', 'api_token_2');    -- Integración para User Bot


INSERT INTO activity_logs (user_id, action, description)
VALUES 
  (29, 'Login', 'User logged in successfully'),  -- Actividad de Admin User
  (30, 'Logout', 'User logged out successfully');  -- Actividad de Regular User

INSERT INTO support_tickets (user_id, subject, message, status)
VALUES 
  (29, 'Issue with Admin Bot', 'The Admin Bot is not responding to queries.', 'open'),  -- Ticket de Admin User
  (30, 'User Bot Error', 'The User Bot is giving incorrect responses.', 'in_progress');  -- Ticket de Regular User

INSERT INTO support_responses (ticket_id, responder_id, message) 
VALUES 
  (1, 29, 'We are investigating the issue with Admin Bot and will update soon.'),  -- Respuesta al ticket del Admin Bot
  (2, 30, 'We have identified the problem with the User Bot and are working on it.');  -- Respuesta al ticket del User Bot

INSERT INTO bot_actions (bot_id, trigger_phrase, action_type, payload)
VALUES 
  (15, 'Hello Admin', 'reply', 'Hello Admin! How can I assist you today?'),  -- Acción para Admin Bot
  (16, 'Hello User', 'reply', 'Hello User! How can I help you today?');  -- Acción para User Bot

INSERT INTO plan_changes (plan_id, changed_by, field_changed, old_value, new_value)
VALUES 
  (1, 29, 'price', '9.99', '10.99'),  -- Cambio de precio para el plan 1 hecho por el usuario 29
  (2, 30, 'max_tokens', '50000', '60000');  -- Cambio en los tokens máximos para el plan 2 hecho por el usuario 30
