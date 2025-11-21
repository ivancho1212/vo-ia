-- Expand activity_logs table for comprehensive audit trail
-- This migration adds fields needed for complete change tracking

ALTER TABLE activity_logs 
ADD COLUMN entity_type VARCHAR(100) NULL AFTER `action`,
ADD COLUMN entity_id INT NULL AFTER entity_type,
ADD COLUMN old_values LONGTEXT NULL AFTER entity_id,
ADD COLUMN new_values LONGTEXT NULL AFTER old_values,
ADD COLUMN ip_address VARCHAR(45) NULL AFTER new_values,
ADD COLUMN user_agent VARCHAR(500) NULL AFTER ip_address,
ADD COLUMN request_id VARCHAR(50) NULL AFTER user_agent;

-- Create indexes for better query performance
CREATE INDEX idx_activity_logs_user_id ON activity_logs(user_id);
CREATE INDEX idx_activity_logs_entity ON activity_logs(entity_type, entity_id);
CREATE INDEX idx_activity_logs_created ON activity_logs(created_at);
CREATE INDEX idx_activity_logs_request ON activity_logs(request_id);

-- Update the comment on the table
ALTER TABLE activity_logs COMMENT = 'Comprehensive audit trail for all system activities and data changes';
