-- Migration: Add RefreshToken table
-- Date: 2025-11-13
-- Purpose: Store refresh tokens for JWT authentication

CREATE TABLE IF NOT EXISTS refresh_tokens (
    id INT PRIMARY KEY AUTO_INCREMENT,
    user_id VARCHAR(255) NOT NULL,
    token LONGTEXT NOT NULL,
    token_jti VARCHAR(255),
    expiry_date DATETIME NOT NULL,
    is_revoked BOOLEAN NOT NULL DEFAULT 0,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ip_address VARCHAR(45),
    user_agent LONGTEXT,
    last_used_at DATETIME,

    FOREIGN KEY (user_id) REFERENCES aspnetusers(id) ON DELETE CASCADE,
    
    INDEX idx_user_id (user_id),
    INDEX idx_token (token(255)),
    INDEX idx_expiry_date (expiry_date),
    INDEX idx_is_revoked (is_revoked)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Almacena refresh tokens para autenticación JWT';
