-- Eliminar la base de datos si ya existe
DROP DATABASE IF EXISTS chatbot_platform;

-- Crear la base de datos
CREATE DATABASE chatbot_platform CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE chatbot_platform;

-- Tabla de roles
CREATE TABLE roles (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE,
    description TEXT
);

-- Tabla de tipos de documento
CREATE TABLE document_types (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE,
    abbreviation VARCHAR(10)
);

-- Tabla de usuarios
CREATE TABLE users (
    id INT AUTO_INCREMENT PRIMARY KEY,
    role_id INT NOT NULL,
    document_type_id INT,
    name VARCHAR(100) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    password VARCHAR(255) NOT NULL,
    phone VARCHAR(20),
    address TEXT,
    document_number VARCHAR(50),
    document_photo_url TEXT,
    avatar_url TEXT,
    is_verified BOOLEAN DEFAULT FALSE,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (role_id) REFERENCES roles(id),
    FOREIGN KEY (document_type_id) REFERENCES document_types(id)
);

-- Tabla de bots
CREATE TABLE bots (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    api_key VARCHAR(255) NOT NULL UNIQUE,
    model_used VARCHAR(50) DEFAULT 'gpt-4',
    is_active BOOLEAN DEFAULT TRUE,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

-- Tabla de documentos subidos
CREATE TABLE uploaded_documents (
    id INT AUTO_INCREMENT PRIMARY KEY,
    bot_id INT NOT NULL,
    user_id INT NOT NULL,
    file_name VARCHAR(255) NOT NULL,
    file_type VARCHAR(20),
    file_path TEXT NOT NULL,
    uploaded_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    indexed BOOLEAN DEFAULT FALSE,
    FOREIGN KEY (bot_id) REFERENCES bots(id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

-- Tabla de estilos personalizados del widget
CREATE TABLE bot_styles (
    id INT AUTO_INCREMENT PRIMARY KEY,
    bot_id INT NOT NULL,
    theme ENUM('light', 'dark', 'custom') DEFAULT 'light',
    primary_color VARCHAR(20) DEFAULT '#000000',
    secondary_color VARCHAR(20) DEFAULT '#ffffff',
    font_family VARCHAR(100) DEFAULT 'Arial',
    avatar_url TEXT,
    position ENUM('bottom-right', 'bottom-left', 'top-right', 'top-left') DEFAULT 'bottom-right',
    custom_css TEXT,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (bot_id) REFERENCES bots(id) ON DELETE CASCADE
);

-- Tabla de conversaciones
CREATE TABLE conversations (
    id INT AUTO_INCREMENT PRIMARY KEY,
    bot_id INT NOT NULL,
    user_id INT,
    title VARCHAR(255),
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (bot_id) REFERENCES bots(id),
    FOREIGN KEY (user_id) REFERENCES users(id)
);

ALTER TABLE conversations
ADD COLUMN user_message TEXT NULL,
ADD COLUMN bot_response TEXT NULL;

-- Tabla de prompts (interacciones)
CREATE TABLE prompts (
    id INT AUTO_INCREMENT PRIMARY KEY,
    bot_id INT NOT NULL,
    user_id INT NOT NULL,
    conversation_id INT,
    prompt_text TEXT NOT NULL,
    response_text LONGTEXT,
    tokens_used INT DEFAULT 0,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (bot_id) REFERENCES bots(id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (conversation_id) REFERENCES conversations(id)
);

-- Tabla de planes
CREATE TABLE plans (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    price DECIMAL(10, 2) NOT NULL,
    max_tokens INT NOT NULL,
    bots_limit INT DEFAULT 1,
    is_active BOOLEAN DEFAULT TRUE
);

-- Suscripciones de usuarios a planes
CREATE TABLE subscriptions (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    plan_id INT NOT NULL,
    started_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    expires_at DATETIME,
    status ENUM('active', 'expired', 'canceled') DEFAULT 'active',
    FOREIGN KEY (user_id) REFERENCES users(id),
    FOREIGN KEY (plan_id) REFERENCES plans(id)
);

-- Tabla de facturación por uso
CREATE TABLE billing (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    bot_id INT,
    subscription_id INT,
    amount DECIMAL(10, 2) NOT NULL,
    tokens_used INT NOT NULL,
    generated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id),
    FOREIGN KEY (bot_id) REFERENCES bots(id),
    FOREIGN KEY (subscription_id) REFERENCES subscriptions(id)
);

-- Resumen de uso mensual para OpenAI
CREATE TABLE openai_usage_summary (
    id INT AUTO_INCREMENT PRIMARY KEY,
    month VARCHAR(7) NOT NULL, -- formato YYYY-MM
    total_tokens INT NOT NULL,
    total_amount DECIMAL(10, 2) NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Tabla de integraciones (widgets o API)
CREATE TABLE bot_integrations (
    id INT AUTO_INCREMENT PRIMARY KEY,
    bot_id INT NOT NULL,
    integration_type ENUM('widget', 'api') DEFAULT 'widget',
    allowed_domain VARCHAR(255),
    api_token VARCHAR(255),
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (bot_id) REFERENCES bots(id)
);

-- Historial de actividad del usuario
CREATE TABLE activity_logs (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT,
    action VARCHAR(100),
    description TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id)
);

-- Tabla de soporte (tickets)
CREATE TABLE support_tickets (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    subject VARCHAR(255) NOT NULL,
    message TEXT NOT NULL,
    status ENUM('open', 'in_progress', 'closed') DEFAULT 'open',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id)
);

-- Respuestas del soporte
CREATE TABLE support_responses (
    id INT AUTO_INCREMENT PRIMARY KEY,
    ticket_id INT NOT NULL,
    responder_id INT,
    message TEXT NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (ticket_id) REFERENCES support_tickets(id),
    FOREIGN KEY (responder_id) REFERENCES users(id)
);
ALTER TABLE prompts ADD COLUMN source ENUM('widget', 'api', 'mobile', 'admin') DEFAULT 'widget';
CREATE TABLE bot_actions (
    id INT AUTO_INCREMENT PRIMARY KEY,
    bot_id INT NOT NULL,
    trigger_phrase VARCHAR(255),
    action_type ENUM('reply', 'redirect', 'show_html', 'custom') NOT NULL,
    payload TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (bot_id) REFERENCES bots(id)
);
ALTER TABLE plans ADD CONSTRAINT unique_plan_name UNIQUE (name);
CREATE TABLE plan_changes (
    id INT AUTO_INCREMENT PRIMARY KEY,
    plan_id INT NOT NULL,
    changed_by INT NOT NULL,
    field_changed VARCHAR(100),
    old_value TEXT,
    new_value TEXT,
    changed_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (plan_id) REFERENCES plans(id),
    FOREIGN KEY (changed_by) REFERENCES users(id)
);

ALTER USER 'root'@'localhost' IDENTIFIED BY '1234';
FLUSH PRIVILEGES;
-- Tabla para extender el perfil de los bots

CREATE TABLE bot_profiles (
    id INT AUTO_INCREMENT PRIMARY KEY,
    bot_id INT NOT NULL,
    name VARCHAR(100),
    avatar_url TEXT,
    bio TEXT,
    personality_traits TEXT,
    language VARCHAR(20) DEFAULT 'es',
    tone VARCHAR(50), -- Ej: formal, amistoso, romántico
    restrictions TEXT, -- Reglas de comportamiento
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (bot_id) REFERENCES bots(id) ON DELETE CASCADE
);

-- Tabla para guardar los intereses de los usuarios
CREATE TABLE interests (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) UNIQUE NOT NULL
);

-- Tabla intermedia: intereses de cada usuario
CREATE TABLE user_preferences (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    interest_id INT NOT NULL,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (interest_id) REFERENCES interests(id) ON DELETE CASCADE
);

-- Tabla para relacionar usuarios con sus bots personalizados
CREATE TABLE user_bot_relations (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    bot_id INT NOT NULL,
    relationship_type ENUM('amistad', 'romántico', 'coach', 'asistente', 'otro') DEFAULT 'otro',
    interaction_score INT DEFAULT 0,
    last_interaction DATETIME,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id),
    FOREIGN KEY (bot_id) REFERENCES bots(id)
);

-- Tabla para consentimiento del usuario sobre el uso de datos
CREATE TABLE user_consents (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    consent_type VARCHAR(100) NOT NULL, -- Ej: uso de datos, generación de imágenes, fine-tune
    granted BOOLEAN DEFAULT FALSE,
    granted_at DATETIME,
    FOREIGN KEY (user_id) REFERENCES users(id)
);

-- Tabla para registrar sesiones de entrenamiento (fine-tuning)
CREATE TABLE training_data_sessions (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    bot_id INT,
    data_summary TEXT,
    data_type ENUM('text', 'document', 'audio', 'image') DEFAULT 'text',
    status ENUM('pending', 'processing', 'completed', 'failed') DEFAULT 'pending',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id),
    FOREIGN KEY (bot_id) REFERENCES bots(id)
);

-- Tabla para guardar configuraciones personalizadas por modelo
CREATE TABLE ai_model_configs (
    id INT AUTO_INCREMENT PRIMARY KEY,
    bot_id INT NOT NULL,
    model_name VARCHAR(100) NOT NULL, -- Ej: gpt-4, mistral, claude
    temperature DECIMAL(3,2) DEFAULT 0.7,
    max_tokens INT DEFAULT 512,
    frequency_penalty DECIMAL(3,2) DEFAULT 0,
    presence_penalty DECIMAL(3,2) DEFAULT 0,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (bot_id) REFERENCES bots(id)
);

-- Tabla para guardar imágenes generadas (opcional)
CREATE TABLE generated_images (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    bot_id INT,
    prompt TEXT NOT NULL,
    image_url TEXT NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id),
    FOREIGN KEY (bot_id) REFERENCES bots(id)
);

-- Tabla de permisos
CREATE TABLE permissions (
    id INT PRIMARY KEY AUTO_INCREMENT,
    name VARCHAR(100) NOT NULL,
    description TEXT
);

-- Tabla intermedia roles-permisos
CREATE TABLE rolepermissions (
    role_id INT,
    permission_id INT,
    PRIMARY KEY (role_id, permission_id),
    FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE CASCADE,
    FOREIGN KEY (permission_id) REFERENCES permissions(id) ON DELETE CASCADE
);

ALTER TABLE plan_changes
DROP FOREIGN KEY plan_changes_ibfk_1;

ALTER TABLE plan_changes
ADD CONSTRAINT plan_changes_ibfk_1
FOREIGN KEY (plan_id) REFERENCES plans(id)
ON DELETE CASCADE;

CREATE TABLE bot_training_configs (
  id INT AUTO_INCREMENT PRIMARY KEY,
  bot_id INT NOT NULL,
  training_type ENUM('url', 'form_data', 'manual_prompt', 'document') NOT NULL,
  data TEXT NOT NULL, -- puede ser texto plano, JSON con preguntas/respuestas, URL, etc.
  created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (bot_id) REFERENCES bots(id)
);

CREATE TABLE knowledge_chunks (
  id INT AUTO_INCREMENT PRIMARY KEY,
  bot_id INT NOT NULL,
  content TEXT NOT NULL,
  metadata JSON,
  embedding_vector BLOB, -- si usarás búsqueda semántica por vectores (con FAISS, Pinecone, etc.)
  created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (bot_id) REFERENCES bots(id)
);

-- 1. bot_templates: para bots preconfigurados (plantillas)
CREATE TABLE IF NOT EXISTS bot_templates (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    description TEXT,
    ia_provider_id INT NOT NULL, -- referencia a proveedor IA (bot_ia_providers)
    default_style_id INT,         -- referencia a estilo por defecto (bot_styles si tienes)
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- 2. bot_data_capture_fields: campos que un bot puede capturar (formularios, datos)
CREATE TABLE IF NOT EXISTS bot_data_capture_fields (
    id INT AUTO_INCREMENT PRIMARY KEY,
    bot_id INT NOT NULL,                 -- bot que usa el campo
    field_name VARCHAR(100) NOT NULL,   -- nombre del campo (ej: "email", "telefono")
    field_type VARCHAR(50) NOT NULL,    -- tipo (texto, número, email, fecha, etc)
    is_required BOOLEAN DEFAULT FALSE,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (bot_id) REFERENCES bots(id) ON DELETE CASCADE
);

-- 3. bot_data_submissions: datos capturados por el bot, enviados por usuarios
CREATE TABLE IF NOT EXISTS bot_data_submissions (
    id INT AUTO_INCREMENT PRIMARY KEY,
    bot_id INT NOT NULL,
    capture_field_id INT NOT NULL,
    submission_value TEXT,
    submitted_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (bot_id) REFERENCES bots(id) ON DELETE CASCADE,
    FOREIGN KEY (capture_field_id) REFERENCES bot_data_capture_fields(id) ON DELETE CASCADE
);

-- 4. bot_installation_settings: configuración para instalar el bot en plataformas externas
CREATE TABLE IF NOT EXISTS bot_installation_settings (
    id INT AUTO_INCREMENT PRIMARY KEY,
    bot_id INT NOT NULL,
    installation_method ENUM('script', 'sdk', 'endpoint') NOT NULL DEFAULT 'script',
    installation_instructions TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (bot_id) REFERENCES bots(id) ON DELETE CASCADE
);

-- 5. bot_ia_providers: proveedores de IA a los que un bot puede estar asociado
CREATE TABLE IF NOT EXISTS bot_ia_providers (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,  -- ej: "OpenAI", "Google Gemini", "DeepSeek"
    api_endpoint VARCHAR(255) NOT NULL,
    api_key VARCHAR(255),
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- 6. token_usage_logs: para registrar uso de tokens por usuario y bot
CREATE TABLE IF NOT EXISTS token_usage_logs (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    bot_id INT NOT NULL,
    tokens_used INT NOT NULL,
    usage_date DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (bot_id) REFERENCES bots(id) ON DELETE CASCADE
);
CREATE TABLE bot_template_prompts (
    id INT AUTO_INCREMENT PRIMARY KEY,
    bot_template_id INT NOT NULL,
    role ENUM('system', 'user', 'assistant') NOT NULL DEFAULT 'system',
    content TEXT NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (bot_template_id) REFERENCES bot_templates(id) ON DELETE CASCADE
);

CREATE TABLE bot_custom_prompts (
    id INT AUTO_INCREMENT PRIMARY KEY,
    bot_id INT NOT NULL,                         -- referencia al bot creado (tabla bots)
    role ENUM('system', 'user', 'assistant') NOT NULL DEFAULT 'system',
    content TEXT NOT NULL,                        -- prompt o ejemplo de conversación
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (bot_id) REFERENCES bots(id) ON DELETE CASCADE
);

CREATE TABLE bot_training_sessions (
    id INT AUTO_INCREMENT PRIMARY KEY,
    bot_id INT NOT NULL,
    session_name VARCHAR(255),
    description TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (bot_id) REFERENCES bots(id) ON DELETE CASCADE
);

ALTER TABLE bot_custom_prompts
ADD COLUMN training_session_id INT NULL,
ADD FOREIGN KEY (training_session_id) REFERENCES bot_training_sessions(id) ON DELETE SET NULL;

CREATE TABLE style_templates (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL, -- Relaciona el estilo con el usuario creador
    name VARCHAR(100) NOT NULL, -- Nombre del estilo personalizado (ej. "Estilo Verde Claro")
    theme ENUM('light', 'dark', 'custom') DEFAULT 'light',
    primary_color VARCHAR(20) DEFAULT '#000000',
    secondary_color VARCHAR(20) DEFAULT '#ffffff',
    font_family VARCHAR(100) DEFAULT 'Arial',
    avatar_url TEXT,
    position ENUM(
        'bottom-right', 'bottom-left', 'top-right', 'top-left',
        'center-right', 'center-left', 'top-center', 'bottom-center'
    ) DEFAULT 'bottom-right',
    custom_css TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    CONSTRAINT fk_style_templates_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

ALTER TABLE bot_styles
ADD COLUMN style_template_id INT DEFAULT NULL AFTER bot_id,
ADD CONSTRAINT fk_bot_styles_style_template
FOREIGN KEY (style_template_id) REFERENCES style_templates(id)
ON DELETE SET NULL;

