-- Insertar roles
INSERT IGNORE INTO roles (name, description) VALUES
  ('Admin', 'Administrador del sistema'),
  ('User', 'Usuario común');

-- Insertar tipos de documento
INSERT IGNORE INTO document_types (name, abbreviation) VALUES
  ('Cédula de Ciudadanía', 'CC'),
  ('NIT', 'NIT'),
  ('Pasaporte', 'PPT');

-- Insertar intereses
INSERT IGNORE INTO interests (name) VALUES 
('Tecnología'), 
('Deportes'), 
('Música'), 
('Cine'), 
('Arte'),
('Literatura');

-- Obtener IDs de soporte
SET @admin_role_id = (SELECT id FROM roles WHERE name = 'Admin');
SET @user_role_id = (SELECT id FROM roles WHERE name = 'User');
SET @cc_type_id = (SELECT id FROM document_types WHERE abbreviation = 'CC');
SET @nit_type_id = (SELECT id FROM document_types WHERE abbreviation = 'NIT');

-- Insertar usuarios
INSERT INTO users (
  role_id, document_type_id, name, email, password, phone, address,
  document_number, document_photo_url, avatar_url, is_verified
) VALUES
  (@admin_role_id, @cc_type_id, 'Admin User', 'admin2@example.com', 'adminpassword', '3000000000',
   '123 Admin Street', '1234567890', 'url_to_document_photo', 'url_to_avatar', 1),
  (@user_role_id, @nit_type_id, 'Regular User', 'user2@example.com', 'userpassword', '3000000001',
   '456 User Avenue', '0987654321', 'url_to_document_photo', 'url_to_avatar', 0);

-- Obtener IDs de usuarios
SET @admin_user_id = (SELECT id FROM users WHERE email = 'admin2@example.com');
SET @regular_user_id = (SELECT id FROM users WHERE email = 'user2@example.com');

-- Insertar bots
INSERT INTO bots (user_id, name, description, api_key, model_used, is_active) 
VALUES 
  (@admin_user_id, 'Admin Bot', 'A bot for admin', 'admin_api_key', 'gpt-4', 1),
  (@regular_user_id, 'User Bot', 'A bot for user', 'user_api_key', 'gpt-4', 1);

-- Obtener IDs de bots
SET @admin_bot_id = (SELECT id FROM bots WHERE name = 'Admin Bot' AND user_id = @admin_user_id);
SET @user_bot_id = (SELECT id FROM bots WHERE name = 'User Bot' AND user_id = @regular_user_id);

-- Insertar perfiles de bots
INSERT INTO bot_profiles (bot_id, name, avatar_url, bio, personality_traits, language, tone, restrictions)
VALUES
  (@admin_bot_id, 'Bot de Asistencia', 'https://example.com/avatar1.png', 'Bot amigable y siempre dispuesto a ayudar.', 'Amigable, paciente, eficiente', 'es', 'formal', 'No discutir sobre temas políticos o religiosos.'),
  (@user_bot_id, 'Bot de Ventas', 'https://example.com/avatar2.png', 'Bot enfocado en realizar ventas con empatía.', 'Persuasivo, carismático, directo', 'es', 'amistoso', 'Evitar conversaciones personales.');

-- Insertar integraciones de bots
INSERT INTO bot_integrations (bot_id, integration_type, allowed_domain, api_token)
VALUES 
  (@admin_bot_id, 'widget', 'example.com', 'api_token_1'),
  (@user_bot_id, 'api', 'example.org', 'api_token_2');

-- Insertar documentos subidos
INSERT INTO uploaded_documents (bot_id, user_id, file_name, file_type, file_path, indexed) 
VALUES 
  (@admin_bot_id, @admin_user_id, 'document1.pdf', 'pdf', 'path/to/document1.pdf', TRUE),
  (@user_bot_id, @regular_user_id, 'document2.docx', 'docx', 'path/to/document2.docx', FALSE);

-- Insertar estilos del widget
INSERT INTO bot_styles (bot_id, theme, primary_color, secondary_color, font_family, avatar_url, position, custom_css)
VALUES
  (@admin_bot_id, 'light', '#3498db', '#ffffff', 'Arial', 'path/to/avatar1.png', 'bottom-right', '/* custom CSS for Admin Bot */'),
  (@user_bot_id, 'dark', '#2c3e50', '#ecf0f1', 'Verdana', 'path/to/avatar2.png', 'bottom-left', '/* custom CSS for User Bot */');

-- Insertar acciones de bots
INSERT INTO bot_actions (bot_id, trigger_phrase, action_type, payload)
VALUES 
  (@admin_bot_id, 'Hello Admin', 'reply', 'Hello Admin! How can I assist you today?'),
  (@user_bot_id, 'Hello User', 'reply', 'Hello User! How can I help you today?');

-- Insertar preferencias de usuario
-- Obtener IDs de intereses
SET @tecnologia_id = (SELECT id FROM interests WHERE name = 'Tecnología');
SET @musica_id = (SELECT id FROM interests WHERE name = 'Música');
SET @cine_id = (SELECT id FROM interests WHERE name = 'Cine');
SET @deportes_id = (SELECT id FROM interests WHERE name = 'Deportes');

INSERT INTO user_preferences (user_id, interest_id) VALUES 
(@admin_user_id, @tecnologia_id),
(@admin_user_id, @musica_id),
(@regular_user_id, @cine_id),
(@regular_user_id, @deportes_id);

-- Insertar relaciones usuario-bot
INSERT INTO user_bot_relations (user_id, bot_id, relationship_type, interaction_score, last_interaction)
VALUES
  (@admin_user_id, @admin_bot_id, 'coach', 10, NOW()),
  (@regular_user_id, @user_bot_id, 'romántico', 5, NOW());

-- Insertar conversaciones
INSERT INTO conversations (bot_id, user_id, title)
VALUES
  (@admin_bot_id, @admin_user_id, 'Admin Bot Conversation'),
  (@user_bot_id, @regular_user_id, 'User Bot Conversation');

-- Obtener conversaciones
SET @admin_convo_id = (SELECT id FROM conversations WHERE bot_id = @admin_bot_id AND user_id = @admin_user_id ORDER BY id DESC LIMIT 1);
SET @user_convo_id = (SELECT id FROM conversations WHERE bot_id = @user_bot_id AND user_id = @regular_user_id ORDER BY id DESC LIMIT 1);

-- Insertar prompts
INSERT INTO prompts (bot_id, user_id, conversation_id, prompt_text, response_text, tokens_used) 
VALUES 
  (@admin_bot_id, @admin_user_id, @admin_convo_id, 'Hola Admin Bot', '¡Hola! ¿En qué puedo ayudarte?', 10),
  (@user_bot_id, @regular_user_id, @user_convo_id, 'Hola User Bot', '¡Hola! ¿En qué puedo ayudarte hoy?', 12);

-- Insertar planes
INSERT INTO plans (name, description, price, max_tokens, bots_limit)
VALUES
  ('Basic Plan', 'Access to basic features', 9.99, 5000, 1),
  ('Premium Plan', 'Access to premium features', 19.99, 10000, 5);

-- Insertar cambios de plan
INSERT INTO plan_changes (plan_id, changed_by, field_changed, old_value, new_value)
VALUES 
  (1, @admin_user_id, 'price', '9.99', '10.99'),
  (2, @regular_user_id, 'max_tokens', '50000', '60000');

-- Insertar resumen de uso de OpenAI
INSERT INTO openai_usage_summary (month, total_tokens, total_amount)
VALUES 
  ('2025-04', 50000, 10.00),
  ('2025-05', 60000, 12.00);

-- Insertar logs de actividad
INSERT INTO activity_logs (user_id, action, description)
VALUES 
  (@admin_user_id, 'Login', 'User logged in successfully'),
  (@regular_user_id, 'Logout', 'User logged out successfully');

-- Insertar tickets de soporte
INSERT INTO support_tickets (user_id, subject, message, status)
VALUES 
  (@admin_user_id, 'Issue with Admin Bot', 'The Admin Bot is not responding to queries.', 'open'),
  (@regular_user_id, 'User Bot Error', 'The User Bot is giving incorrect responses.', 'in_progress');

-- Insertar respuestas a tickets
INSERT INTO support_responses (ticket_id, responder_id, message) 
VALUES 
  (1, @admin_user_id, 'We are investigating the issue with Admin Bot and will update soon.'),
  (2, @regular_user_id, 'We have identified the problem with the User Bot and are working on it.');

INSERT INTO Permissions (Name, Description) VALUES
('CanViewUsers', 'Puede ver la lista de usuarios'),
('CanEditUsers', 'Puede editar usuarios'),
('CanDeleteUsers', 'Puede eliminar usuarios'),
('CanManageRoles', 'Puede crear, editar o eliminar roles'),
('CanAccessSupportTickets', 'Puede ver y responder tickets de soporte');

INSERT INTO roles (id, name, description)
VALUES (3, 'Support', 'Soporte técnico');


-- Ejemplo: El rol Admin tiene todos los permisos
INSERT INTO RolePermissions (RoleId, PermissionId)
SELECT 32, Id FROM Permissions;

-- El rol Support solo tiene acceso a tickets
INSERT INTO RolePermissions (RoleId, PermissionId)
VALUES (3, (SELECT Id FROM Permissions WHERE Name = 'CanAccessSupportTickets'));

INSERT INTO RolePermissions (RoleId, PermissionId) VALUES (3, 1);
INSERT INTO RolePermissions (RoleId, PermissionId) VALUES (5, 1); -- CanViewUsers

INSERT INTO RolePermissions (RoleId, PermissionId)
SELECT 32, Id FROM Permissions
ON DUPLICATE KEY UPDATE RoleId = RoleId;

SELECT * FROM RolePermissions WHERE RoleId = 32;
