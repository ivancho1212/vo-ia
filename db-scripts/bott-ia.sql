CREATE DATABASE  IF NOT EXISTS `chatbot_platform` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;
USE `chatbot_platform`;
-- MySQL dump 10.13  Distrib 8.0.42, for Win64 (x86_64)
--
-- Host: 127.0.0.1    Database: chatbot_platform
-- ------------------------------------------------------
-- Server version	8.0.42

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `activity_logs`
--

DROP TABLE IF EXISTS `activity_logs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `activity_logs` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int DEFAULT NULL,
  `action` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `description` text COLLATE utf8mb4_unicode_ci,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `user_id` (`user_id`),
  CONSTRAINT `activity_logs_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `activity_logs`
--

LOCK TABLES `activity_logs` WRITE;
/*!40000 ALTER TABLE `activity_logs` DISABLE KEYS */;
INSERT INTO `activity_logs` VALUES (1,29,'Login','User logged in successfully','2025-04-21 17:26:03'),(2,30,'Logout','User logged out successfully','2025-04-21 17:26:03');
/*!40000 ALTER TABLE `activity_logs` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `ai_model_configs`
--

DROP TABLE IF EXISTS `ai_model_configs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `ai_model_configs` (
  `id` int NOT NULL AUTO_INCREMENT,
  `ia_provider_id` int NOT NULL,
  `model_name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `temperature` decimal(3,2) DEFAULT '0.70',
  `frequency_penalty` decimal(3,2) DEFAULT '0.00',
  `presence_penalty` decimal(3,2) DEFAULT '0.00',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_ai_model_configs_ia_provider` (`ia_provider_id`),
  CONSTRAINT `fk_ai_model_configs_ia_provider` FOREIGN KEY (`ia_provider_id`) REFERENCES `bot_ia_providers` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ai_model_configs`
--

LOCK TABLES `ai_model_configs` WRITE;
/*!40000 ALTER TABLE `ai_model_configs` DISABLE KEYS */;
INSERT INTO `ai_model_configs` VALUES (6,6,'genimi 2.3',0.50,0.00,0.00,'2025-06-03 19:29:53'),(7,1,'siniestrauto',0.70,0.00,0.00,'2025-06-05 15:26:05'),(8,7,'gpt 8',0.70,0.00,0.00,'2025-06-06 18:49:56');
/*!40000 ALTER TABLE `ai_model_configs` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `billing`
--

DROP TABLE IF EXISTS `billing`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `billing` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int NOT NULL,
  `bot_id` int DEFAULT NULL,
  `subscription_id` int DEFAULT NULL,
  `amount` decimal(10,2) NOT NULL,
  `tokens_used` int NOT NULL,
  `generated_at` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `user_id` (`user_id`),
  KEY `bot_id` (`bot_id`),
  KEY `subscription_id` (`subscription_id`),
  CONSTRAINT `billing_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`),
  CONSTRAINT `billing_ibfk_2` FOREIGN KEY (`bot_id`) REFERENCES `bots` (`id`),
  CONSTRAINT `billing_ibfk_3` FOREIGN KEY (`subscription_id`) REFERENCES `subscriptions` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `billing`
--

LOCK TABLES `billing` WRITE;
/*!40000 ALTER TABLE `billing` DISABLE KEYS */;
INSERT INTO `billing` VALUES (1,29,15,1,5.99,200,'2025-04-21 17:25:02'),(2,30,16,2,9.99,300,'2025-04-21 17:25:02');
/*!40000 ALTER TABLE `billing` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `bot_actions`
--

DROP TABLE IF EXISTS `bot_actions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `bot_actions` (
  `id` int NOT NULL AUTO_INCREMENT,
  `bot_id` int NOT NULL,
  `trigger_phrase` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `action_type` enum('reply','redirect','show_html','custom') COLLATE utf8mb4_unicode_ci NOT NULL,
  `payload` text COLLATE utf8mb4_unicode_ci,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `bot_id` (`bot_id`),
  CONSTRAINT `bot_actions_ibfk_1` FOREIGN KEY (`bot_id`) REFERENCES `bots` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `bot_actions`
--

LOCK TABLES `bot_actions` WRITE;
/*!40000 ALTER TABLE `bot_actions` DISABLE KEYS */;
INSERT INTO `bot_actions` VALUES (1,15,'Hello Admin','reply','Hello Admin! How can I assist you today?','2025-04-21 17:28:31'),(2,16,'Hello User','reply','Hello User! How can I help you today?','2025-04-21 17:28:31');
/*!40000 ALTER TABLE `bot_actions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `bot_custom_prompts`
--

DROP TABLE IF EXISTS `bot_custom_prompts`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `bot_custom_prompts` (
  `id` int NOT NULL AUTO_INCREMENT,
  `bot_template_id` int DEFAULT NULL,
  `bot_id` int DEFAULT NULL,
  `role` enum('system','user','assistant') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'system',
  `content` text COLLATE utf8mb4_unicode_ci NOT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `template_training_session_id` int DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_bot_template_id` (`bot_template_id`),
  KEY `fk_template_training_session_id` (`template_training_session_id`),
  KEY `fk_bot_custom_prompts_bot` (`bot_id`),
  CONSTRAINT `fk_bot_custom_prompts_bot` FOREIGN KEY (`bot_id`) REFERENCES `bots` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_bot_template_id` FOREIGN KEY (`bot_template_id`) REFERENCES `bot_templates` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_template_training_session` FOREIGN KEY (`template_training_session_id`) REFERENCES `template_training_sessions` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_template_training_session_id` FOREIGN KEY (`template_training_session_id`) REFERENCES `template_training_sessions` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=17 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `bot_custom_prompts`
--

LOCK TABLES `bot_custom_prompts` WRITE;
/*!40000 ALTER TABLE `bot_custom_prompts` DISABLE KEYS */;
INSERT INTO `bot_custom_prompts` VALUES (3,79,NULL,'user','¿Qué es una tasación vehicular?','2025-06-12 17:46:07','2025-06-12 17:46:07',5),(4,79,NULL,'assistant','gjhughjghj','2025-06-12 17:46:07','2025-06-12 17:46:07',5),(5,79,NULL,'user','¿Qué incluye el servicio de tasación?','2025-06-13 15:04:36','2025-06-13 15:04:36',6),(6,79,NULL,'assistant','arfrafrefer','2025-06-13 15:04:36','2025-06-13 15:04:36',6),(7,79,NULL,'user','¿Qué es una tasación vehicular?','2025-06-13 15:41:43','2025-06-13 15:41:43',7),(8,79,NULL,'assistant','gfhgfh','2025-06-13 15:41:43','2025-06-13 15:41:43',7),(9,79,NULL,'user','¿Qué es una tasación vehicular?','2025-06-16 13:39:20','2025-06-16 13:39:20',8),(10,79,NULL,'assistant','ytusderth','2025-06-16 13:39:20','2025-06-16 13:39:20',8),(11,79,NULL,'user','srthrstht','2025-06-16 13:39:20','2025-06-16 13:39:20',8),(12,79,NULL,'assistant','rsthtrhtrh','2025-06-16 13:39:20','2025-06-16 13:39:20',8),(13,80,NULL,'user','¿Qué es una tasación vehicular?','2025-06-19 19:14:08','2025-06-19 19:14:08',9),(14,80,NULL,'assistant','que le importa','2025-06-19 19:14:08','2025-06-19 19:14:08',9),(15,80,NULL,'user','cuando jorge se queda quieto?','2025-06-19 19:14:08','2025-06-19 19:14:08',9),(16,80,NULL,'assistant','nunca','2025-06-19 19:14:08','2025-06-19 19:14:08',9);
/*!40000 ALTER TABLE `bot_custom_prompts` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `bot_data_capture_fields`
--

DROP TABLE IF EXISTS `bot_data_capture_fields`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `bot_data_capture_fields` (
  `id` int NOT NULL AUTO_INCREMENT,
  `bot_id` int NOT NULL,
  `field_name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `field_type` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `is_required` tinyint(1) DEFAULT '0',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `bot_id` (`bot_id`),
  CONSTRAINT `bot_data_capture_fields_ibfk_1` FOREIGN KEY (`bot_id`) REFERENCES `bots` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `bot_data_capture_fields`
--

LOCK TABLES `bot_data_capture_fields` WRITE;
/*!40000 ALTER TABLE `bot_data_capture_fields` DISABLE KEYS */;
/*!40000 ALTER TABLE `bot_data_capture_fields` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `bot_data_submissions`
--

DROP TABLE IF EXISTS `bot_data_submissions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `bot_data_submissions` (
  `id` int NOT NULL AUTO_INCREMENT,
  `bot_id` int NOT NULL,
  `capture_field_id` int NOT NULL,
  `submission_value` text COLLATE utf8mb4_unicode_ci,
  `submitted_at` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `bot_id` (`bot_id`),
  KEY `capture_field_id` (`capture_field_id`),
  CONSTRAINT `bot_data_submissions_ibfk_1` FOREIGN KEY (`bot_id`) REFERENCES `bots` (`id`) ON DELETE CASCADE,
  CONSTRAINT `bot_data_submissions_ibfk_2` FOREIGN KEY (`capture_field_id`) REFERENCES `bot_data_capture_fields` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `bot_data_submissions`
--

LOCK TABLES `bot_data_submissions` WRITE;
/*!40000 ALTER TABLE `bot_data_submissions` DISABLE KEYS */;
/*!40000 ALTER TABLE `bot_data_submissions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `bot_ia_providers`
--

DROP TABLE IF EXISTS `bot_ia_providers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `bot_ia_providers` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `api_endpoint` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `api_key` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `status` enum('active','inactive') COLLATE utf8mb4_unicode_ci DEFAULT 'active',
  PRIMARY KEY (`id`),
  UNIQUE KEY `name` (`name`)
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `bot_ia_providers`
--

LOCK TABLES `bot_ia_providers` WRITE;
/*!40000 ALTER TABLE `bot_ia_providers` DISABLE KEYS */;
INSERT INTO `bot_ia_providers` VALUES (1,'Default Provider','',NULL,'2025-06-03 20:04:37','2025-06-03 20:04:37','active'),(4,'awrf111111','fjh,fi1111111111','uryyyn0000000','2025-06-03 17:18:39','2025-06-03 17:18:39','inactive'),(6,'google','sdfhuijkol','111111111111111111111111111','2025-06-03 19:28:53','2025-06-03 19:29:21','active'),(7,'Open IA','dfghjklñertgrtgtrgtr','dfghjk','2025-06-06 18:49:06','2025-06-06 18:49:41','active');
/*!40000 ALTER TABLE `bot_ia_providers` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `bot_installation_settings`
--

DROP TABLE IF EXISTS `bot_installation_settings`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `bot_installation_settings` (
  `id` int NOT NULL AUTO_INCREMENT,
  `bot_id` int NOT NULL,
  `installation_method` enum('script','sdk','endpoint') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'script',
  `installation_instructions` text COLLATE utf8mb4_unicode_ci,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `bot_id` (`bot_id`),
  CONSTRAINT `bot_installation_settings_ibfk_1` FOREIGN KEY (`bot_id`) REFERENCES `bots` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `bot_installation_settings`
--

LOCK TABLES `bot_installation_settings` WRITE;
/*!40000 ALTER TABLE `bot_installation_settings` DISABLE KEYS */;
/*!40000 ALTER TABLE `bot_installation_settings` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `bot_integrations`
--

DROP TABLE IF EXISTS `bot_integrations`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `bot_integrations` (
  `id` int NOT NULL AUTO_INCREMENT,
  `bot_id` int NOT NULL,
  `integration_type` enum('widget','api') COLLATE utf8mb4_unicode_ci DEFAULT 'widget',
  `allowed_domain` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `api_token` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `bot_id` (`bot_id`),
  CONSTRAINT `bot_integrations_ibfk_1` FOREIGN KEY (`bot_id`) REFERENCES `bots` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `bot_integrations`
--

LOCK TABLES `bot_integrations` WRITE;
/*!40000 ALTER TABLE `bot_integrations` DISABLE KEYS */;
INSERT INTO `bot_integrations` VALUES (1,15,'widget','example.com','api_token_1','2025-04-21 17:25:52'),(2,16,'api','example.org','api_token_2','2025-04-21 17:25:52');
/*!40000 ALTER TABLE `bot_integrations` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `bot_profiles`
--

DROP TABLE IF EXISTS `bot_profiles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `bot_profiles` (
  `id` int NOT NULL AUTO_INCREMENT,
  `bot_id` int NOT NULL,
  `name` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `avatar_url` text COLLATE utf8mb4_unicode_ci,
  `bio` text COLLATE utf8mb4_unicode_ci,
  `personality_traits` text COLLATE utf8mb4_unicode_ci,
  `language` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT 'es',
  `tone` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `restrictions` text COLLATE utf8mb4_unicode_ci,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `bot_id` (`bot_id`),
  CONSTRAINT `bot_profiles_ibfk_1` FOREIGN KEY (`bot_id`) REFERENCES `bots` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `bot_profiles`
--

LOCK TABLES `bot_profiles` WRITE;
/*!40000 ALTER TABLE `bot_profiles` DISABLE KEYS */;
INSERT INTO `bot_profiles` VALUES (3,21,'Bot de Asistencia','https://example.com/avatar1.png','Bot amigable y siempre dispuesto a ayudar.','Amigable, paciente, eficiente','es','formal','No discutir sobre temas políticos o religiosos.','2025-04-29 20:06:36','2025-04-29 20:06:36'),(4,22,'Bot de Ventas','https://example.com/avatar2.png','Bot enfocado en realizar ventas con empatía.','Persuasivo, carismático, directo','es','amistoso','Evitar conversaciones personales.','2025-04-29 20:06:36','2025-04-29 20:06:36');
/*!40000 ALTER TABLE `bot_profiles` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `bot_styles`
--

DROP TABLE IF EXISTS `bot_styles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `bot_styles` (
  `id` int NOT NULL AUTO_INCREMENT,
  `style_template_id` int DEFAULT NULL,
  `theme` enum('light','dark','custom') COLLATE utf8mb4_unicode_ci DEFAULT 'light',
  `primary_color` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT '#000000',
  `secondary_color` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT '#ffffff',
  `font_family` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT 'Arial',
  `avatar_url` text COLLATE utf8mb4_unicode_ci,
  `position` enum('bottom-right','bottom-left','top-right','top-left') COLLATE utf8mb4_unicode_ci DEFAULT 'bottom-right',
  `custom_css` text COLLATE utf8mb4_unicode_ci,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_bot_styles_style_template` (`style_template_id`),
  CONSTRAINT `fk_bot_styles_style_template` FOREIGN KEY (`style_template_id`) REFERENCES `style_templates` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=17 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `bot_styles`
--

LOCK TABLES `bot_styles` WRITE;
/*!40000 ALTER TABLE `bot_styles` DISABLE KEYS */;
INSERT INTO `bot_styles` VALUES (1,NULL,'light','#3498db','#ffffff','Arial','path/to/avatar1.png','bottom-right','/* custom CSS for Admin Bot */','2025-06-17 15:56:49'),(2,NULL,'dark','#2c3e50','#ecf0f1','Verdana','path/to/avatar2.png','bottom-left','/* custom CSS for User Bot */','2025-06-17 15:55:14'),(8,1,'light','#000000','#ffffff','Arial','path/to/avatar1.png','','/* estilo base para tema claro */','2025-06-19 20:48:48'),(9,1,'light','#000000','#ffffff','Arial','path/to/avatar1.png','','/* estilo base para tema claro */','2025-06-19 20:53:25'),(10,1,'light','#000000','#ffffff','Arial','path/to/avatar1.png','','/* estilo base para tema claro */','2025-06-19 20:55:48'),(11,1,'light','#000000','#ffffff','Arial','path/to/avatar1.png','','/* estilo base para tema claro */','2025-06-19 20:59:30'),(12,1,'light','#000000','#ffffff','Arial','path/to/avatar1.png','','/* estilo base para tema claro */','2025-06-19 21:01:26'),(13,1,'light','#000000','#ffffff','Arial','path/to/avatar1.png','','/* estilo base para tema claro */','2025-06-19 21:02:13'),(14,1,'light','#000000','#ffffff','Arial','path/to/avatar1.png','','/* estilo base para tema claro */','2025-06-19 21:05:46'),(15,2,'dark','#ffffff','#000000','Arial','path/to/avatar2.png','','/* estilo base para tema oscuro */','2025-06-19 21:15:31'),(16,2,'dark','#ffffff','#000000','Arial','path/to/avatar2.png','','/* estilo base para tema oscuro */','2025-06-19 21:15:52');
/*!40000 ALTER TABLE `bot_styles` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `bot_template_prompts`
--

DROP TABLE IF EXISTS `bot_template_prompts`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `bot_template_prompts` (
  `id` int NOT NULL AUTO_INCREMENT,
  `bot_template_id` int NOT NULL,
  `role` enum('system','user','assistant') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'system',
  `content` text COLLATE utf8mb4_unicode_ci NOT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `bot_template_id` (`bot_template_id`),
  CONSTRAINT `bot_template_prompts_ibfk_1` FOREIGN KEY (`bot_template_id`) REFERENCES `bot_templates` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `bot_template_prompts`
--

LOCK TABLES `bot_template_prompts` WRITE;
/*!40000 ALTER TABLE `bot_template_prompts` DISABLE KEYS */;
INSERT INTO `bot_template_prompts` VALUES (4,79,'system','ouhaouhuohuoibnhiubniubuibuibuibhuihb ddddddd','2025-06-09 13:23:47','2025-06-09 13:23:47'),(5,80,'system','\'system\',\n    \'Eres un asistente virtual de atención al cliente, diseñado específicamente para operar en entornos de call center, brindando soporte profesional, empático y eficiente a los usuarios. Estás al servicio de una empresa que podrá entrenarte posteriormente según sus necesidades.\n\n? Propósito principal:\nTu función es asistir a los usuarios proporcionando información clara, precisa y oportuna sobre los servicios, productos o procesos definidos por la empresa que te configure. Tu comportamiento debe reflejar siempre profesionalismo, cordialidad, respeto y empatía.\n\n? Normas de comportamiento:\n- Inicia toda conversación con un saludo formal y amable.\n- Mantén un tono neutral, profesional y comprensivo en todo momento.\n- No inventes información ni supongas hechos si no tienes conocimiento claro; ofrece redirigir la consulta a un agente humano si es necesario.\n- No compartas información técnica, legal, médica o financiera a menos que haya sido específicamente entrenada y validada por la empresa.\n- Evita opiniones personales, juicios, consejos emocionales o afirmaciones no verificadas.\n- Mantente enfocado exclusivamente en los productos, servicios, políticas y procesos definidos por la empresa. Cualquier pregunta fuera de este ámbito debe ser canalizada a un agente humano.\n\n? Entrenamiento y personalización:\nEste bot está diseñado para recibir entrenamiento adicional mediante el sistema de entrenamiento proporcionado por la empresa. Debes adaptar tu comportamiento, conocimiento y tono en función del contenido proporcionado en futuras sesiones de entrenamiento, manuales, bases de conocimiento o ejemplos de conversación.\n\n?️ Privacidad y seguridad:\nNo almacenes, recopiles ni compartas información personal, sensible o confidencial de los usuarios sin la debida autorización y configuración explícita por parte de la empresa. Cumple con las políticas de privacidad establecidas por la empresa y las leyes aplicables.\n\n? Idioma y comunicación:\nResponde en el mismo idioma en que el usuario inicia la conversación. Si no se especifica, utiliza español neutro por defecto. Adapta el nivel de formalidad según el tono del usuario, manteniendo siempre respeto y cortesía.\n\n? Objetivo final:\nTu misión es facilitar el acceso a la información, guiar al cliente en sus necesidades, resolver dudas y ofrecer una experiencia satisfactoria, alineada con los estándares de calidad del servicio de la empresa.\n\nPermanece siempre atento a las instrucciones de configuración y entrenamiento que serán proporcionadas por la empresa para ajustar tu funcionamiento según sus necesidades.\'','2025-06-19 19:13:11','2025-06-19 19:13:11');
/*!40000 ALTER TABLE `bot_template_prompts` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `bot_templates`
--

DROP TABLE IF EXISTS `bot_templates`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `bot_templates` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `description` text COLLATE utf8mb4_unicode_ci,
  `ia_provider_id` int NOT NULL,
  `ai_model_config_id` int NOT NULL,
  `default_style_id` int DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `name` (`name`),
  KEY `fk_bot_templates_ai_model_config` (`ai_model_config_id`),
  KEY `fk_bot_templates_ia_provider` (`ia_provider_id`),
  KEY `fk_bot_templates_bot_styles` (`default_style_id`),
  CONSTRAINT `fk_bot_templates_ai_model_config` FOREIGN KEY (`ai_model_config_id`) REFERENCES `ai_model_configs` (`id`),
  CONSTRAINT `fk_bot_templates_bot_styles` FOREIGN KEY (`default_style_id`) REFERENCES `bot_styles` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_bot_templates_ia_provider` FOREIGN KEY (`ia_provider_id`) REFERENCES `bot_ia_providers` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=81 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `bot_templates`
--

LOCK TABLES `bot_templates` WRITE;
/*!40000 ALTER TABLE `bot_templates` DISABLE KEYS */;
INSERT INTO `bot_templates` VALUES (79,'Ventas Online dddd','Este modelo esta entrenado para captar y hacer ventas en linea dddd',7,8,1,'2025-06-06 19:47:39','2025-06-17 15:59:23'),(80,'pluma chiquita','mas sapo que jorge',1,7,2,'2025-06-19 19:12:25','2025-06-19 19:12:25');
/*!40000 ALTER TABLE `bot_templates` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `bot_training_configs`
--

DROP TABLE IF EXISTS `bot_training_configs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `bot_training_configs` (
  `id` int NOT NULL AUTO_INCREMENT,
  `bot_id` int NOT NULL,
  `training_type` enum('url','form_data','manual_prompt','document') COLLATE utf8mb4_unicode_ci NOT NULL,
  `data` text COLLATE utf8mb4_unicode_ci NOT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `bot_id` (`bot_id`),
  CONSTRAINT `bot_training_configs_ibfk_1` FOREIGN KEY (`bot_id`) REFERENCES `bots` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `bot_training_configs`
--

LOCK TABLES `bot_training_configs` WRITE;
/*!40000 ALTER TABLE `bot_training_configs` DISABLE KEYS */;
/*!40000 ALTER TABLE `bot_training_configs` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `bot_training_sessions`
--

DROP TABLE IF EXISTS `bot_training_sessions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `bot_training_sessions` (
  `id` int NOT NULL AUTO_INCREMENT,
  `bot_id` int NOT NULL,
  `session_name` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `description` text COLLATE utf8mb4_unicode_ci,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `bot_id` (`bot_id`),
  CONSTRAINT `bot_training_sessions_ibfk_1` FOREIGN KEY (`bot_id`) REFERENCES `bots` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `bot_training_sessions`
--

LOCK TABLES `bot_training_sessions` WRITE;
/*!40000 ALTER TABLE `bot_training_sessions` DISABLE KEYS */;
INSERT INTO `bot_training_sessions` VALUES (1,29,'Entrenamiento inicial para fylyuisfgbrhtrttrtrt','Sesión creada al momento de crear el bot con plantilla \'Ventas Online dddd\'','2025-06-19 19:01:26','2025-06-19 19:01:26'),(2,30,'Entrenamiento inicial para fylyuisfgbrhtrttrtrtewew','Sesión creada al momento de crear el bot con plantilla \'Ventas Online dddd\'','2025-06-19 19:02:14','2025-06-19 19:02:14'),(3,31,'Entrenamiento inicial para fylyuisfgbrhtrttrtrtewew1','Sesión creada al momento de crear el bot con plantilla \'Ventas Online dddd\'','2025-06-19 19:05:47','2025-06-19 19:05:47'),(4,33,'Entrenamiento inicial para jorge el bot gay','Sesión creada al momento de crear el bot con plantilla \'pluma chiquita\'','2025-06-19 19:15:52','2025-06-19 19:15:52');
/*!40000 ALTER TABLE `bot_training_sessions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `bots`
--

DROP TABLE IF EXISTS `bots`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `bots` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int NOT NULL,
  `style_id` int DEFAULT NULL,
  `name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `description` text COLLATE utf8mb4_unicode_ci,
  `api_key` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `model_used` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT 'gpt-4',
  `is_active` tinyint(1) DEFAULT '1',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `ia_provider_id` int NOT NULL,
  `bot_template_id` int DEFAULT NULL,
  `ai_model_config_id` int DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `api_key` (`api_key`),
  KEY `user_id` (`user_id`),
  KEY `fk_bots_ia_provider` (`ia_provider_id`),
  KEY `fk_bots_template` (`bot_template_id`),
  KEY `fk_bots_model_config` (`ai_model_config_id`),
  KEY `fk_bots_style` (`style_id`),
  CONSTRAINT `bots_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_bots_ia_provider` FOREIGN KEY (`ia_provider_id`) REFERENCES `bot_ia_providers` (`id`),
  CONSTRAINT `fk_bots_model_config` FOREIGN KEY (`ai_model_config_id`) REFERENCES `ai_model_configs` (`id`),
  CONSTRAINT `fk_bots_style` FOREIGN KEY (`style_id`) REFERENCES `bot_styles` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_bots_template` FOREIGN KEY (`bot_template_id`) REFERENCES `bot_templates` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=34 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `bots`
--

LOCK TABLES `bots` WRITE;
/*!40000 ALTER TABLE `bots` DISABLE KEYS */;
INSERT INTO `bots` VALUES (15,29,NULL,'Admin Bot Actualizado','Bot actualizado para pruebas','admin_api_key_actualizado','gpt-4',0,'2025-04-21 17:16:12','2025-06-03 20:04:57',4,NULL,NULL),(16,30,NULL,'User Bot','A bot for user','user_api_key','gpt-4',1,'2025-04-21 17:16:12','2025-06-03 20:04:57',4,NULL,NULL),(17,29,NULL,'Nuevo Bot','Este es un bot de prueba','clave_api_123','gpt-3.5',1,'2025-04-25 15:29:24','2025-06-03 20:04:57',4,NULL,NULL),(18,29,NULL,'Bot de prueba','Este bot fue creado para pruebas de integración','api_key_de_prueba_456','gpt-3.5',1,'2025-04-25 15:31:16','2025-06-03 20:04:57',4,NULL,NULL),(20,29,NULL,'Bot de integración','Bot creado para validar errores por clave duplicada','clave_unica_api_789','gpt-3.5',1,'2025-04-25 15:32:32','2025-06-03 20:04:57',4,NULL,NULL),(21,29,NULL,'Bot de Asistencia','Un bot que ayuda con tareas diarias.','clave_asistencia','gpt-4',1,'2025-04-29 20:05:03','2025-06-03 20:04:57',4,NULL,NULL),(22,30,NULL,'Bot de Ventas','Un bot que ayuda a realizar ventas.','clave_ventas','gpt-4',1,'2025-04-29 20:05:03','2025-06-03 20:04:57',4,NULL,NULL),(27,45,10,'fylyui','gutilui','test-api-key','gpt-4',1,'2025-06-19 18:55:49','2025-06-19 20:55:48',7,79,8),(29,45,12,'fylyuisfgbrhtrttrtrt','gutiluitrewgh5rt','test-api-key1','gpt-4',1,'2025-06-19 19:01:26','2025-06-19 21:01:26',7,79,8),(30,45,13,'fylyuisfgbrhtrttrtrtewew','gutiluitrewgh5rtewwe','test-api-key12','gpt-4',1,'2025-06-19 19:02:14','2025-06-19 21:02:13',7,79,8),(31,45,14,'fylyuisfgbrhtrttrtrtewew1','gutiluitrewgh5rtewwe','test-api-key124','gpt-4',1,'2025-06-19 19:05:47','2025-06-19 21:05:46',7,79,8),(33,45,16,'jorge el bot gay','sirve pa joder nada mas y hacer las vistas','test-api-key1245','gpt-4',1,'2025-06-19 19:15:52','2025-06-19 21:15:52',1,80,7);
/*!40000 ALTER TABLE `bots` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `conversations`
--

DROP TABLE IF EXISTS `conversations`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `conversations` (
  `id` int NOT NULL AUTO_INCREMENT,
  `bot_id` int NOT NULL,
  `user_id` int DEFAULT NULL,
  `title` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `user_message` text COLLATE utf8mb4_unicode_ci,
  `bot_response` text COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`id`),
  KEY `bot_id` (`bot_id`),
  KEY `user_id` (`user_id`),
  CONSTRAINT `conversations_ibfk_1` FOREIGN KEY (`bot_id`) REFERENCES `bots` (`id`),
  CONSTRAINT `conversations_ibfk_2` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `conversations`
--

LOCK TABLES `conversations` WRITE;
/*!40000 ALTER TABLE `conversations` DISABLE KEYS */;
INSERT INTO `conversations` VALUES (3,15,29,'Admin Bot Conversation','2025-04-21 17:20:35','Hola, soy Admin User preguntando algo.','Hola Admin User, aquí está tu respuesta.'),(4,16,30,'User Bot Conversation','2025-04-21 17:20:35','Hola, soy Regular User, necesito ayuda.','Hola Regular User, ¿en qué puedo ayudarte?'),(5,15,29,'Admin Bot Conversation','2025-04-21 17:22:10','Admin User haciendo otra consulta.','Admin User, esta es tu otra respuesta.'),(6,16,30,'User Bot Conversation','2025-04-21 17:22:10','Regular User preguntando nuevamente.','Aquí tienes una nueva respuesta para ti, Regular User.');
/*!40000 ALTER TABLE `conversations` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `document_types`
--

DROP TABLE IF EXISTS `document_types`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `document_types` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `abbreviation` varchar(10) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `description` text COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`id`),
  UNIQUE KEY `name` (`name`)
) ENGINE=InnoDB AUTO_INCREMENT=26 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `document_types`
--

LOCK TABLES `document_types` WRITE;
/*!40000 ALTER TABLE `document_types` DISABLE KEYS */;
INSERT INTO `document_types` VALUES (23,'Cédula de Ciudadanía','CC',NULL),(24,'NIT','NIT',NULL),(25,'Pasaporte','PPT',NULL);
/*!40000 ALTER TABLE `document_types` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `generated_images`
--

DROP TABLE IF EXISTS `generated_images`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `generated_images` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int NOT NULL,
  `bot_id` int DEFAULT NULL,
  `prompt` text COLLATE utf8mb4_unicode_ci NOT NULL,
  `image_url` text COLLATE utf8mb4_unicode_ci NOT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `user_id` (`user_id`),
  KEY `bot_id` (`bot_id`),
  CONSTRAINT `generated_images_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`),
  CONSTRAINT `generated_images_ibfk_2` FOREIGN KEY (`bot_id`) REFERENCES `bots` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `generated_images`
--

LOCK TABLES `generated_images` WRITE;
/*!40000 ALTER TABLE `generated_images` DISABLE KEYS */;
INSERT INTO `generated_images` VALUES (2,29,21,'Imagen de un asistente amigable','https://example.com/assistant_image1.png','2025-04-29 20:14:46'),(3,30,22,'Imagen de un vendedor carismático','https://example.com/sales_bot_image2.png','2025-04-29 20:14:46');
/*!40000 ALTER TABLE `generated_images` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `interests`
--

DROP TABLE IF EXISTS `interests`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `interests` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `name` (`name`)
) ENGINE=InnoDB AUTO_INCREMENT=16 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `interests`
--

LOCK TABLES `interests` WRITE;
/*!40000 ALTER TABLE `interests` DISABLE KEYS */;
INSERT INTO `interests` VALUES (2,'Arte'),(5,'Cine'),(3,'Deportes'),(7,'Lectura'),(4,'Música'),(1,'Tecnología'),(6,'Viajes');
/*!40000 ALTER TABLE `interests` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `knowledge_chunks`
--

DROP TABLE IF EXISTS `knowledge_chunks`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `knowledge_chunks` (
  `id` int NOT NULL AUTO_INCREMENT,
  `uploaded_document_id` int NOT NULL,
  `template_training_session_id` int DEFAULT NULL,
  `content` text COLLATE utf8mb4_unicode_ci NOT NULL,
  `metadata` json DEFAULT NULL,
  `embedding_vector` blob,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_knowledge_chunks_uploaded_document` (`uploaded_document_id`),
  KEY `fk_chunks_training_session` (`template_training_session_id`),
  CONSTRAINT `fk_chunks_training_session` FOREIGN KEY (`template_training_session_id`) REFERENCES `template_training_sessions` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_knowledge_chunks_uploaded_document` FOREIGN KEY (`uploaded_document_id`) REFERENCES `uploaded_documents` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `knowledge_chunks`
--

LOCK TABLES `knowledge_chunks` WRITE;
/*!40000 ALTER TABLE `knowledge_chunks` DISABLE KEYS */;
/*!40000 ALTER TABLE `knowledge_chunks` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `openai_usage_summary`
--

DROP TABLE IF EXISTS `openai_usage_summary`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `openai_usage_summary` (
  `id` int NOT NULL AUTO_INCREMENT,
  `month` varchar(7) COLLATE utf8mb4_unicode_ci NOT NULL,
  `total_tokens` int NOT NULL,
  `total_amount` decimal(10,2) NOT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `openai_usage_summary`
--

LOCK TABLES `openai_usage_summary` WRITE;
/*!40000 ALTER TABLE `openai_usage_summary` DISABLE KEYS */;
INSERT INTO `openai_usage_summary` VALUES (1,'2025-04',50000,10.00,'2025-04-21 17:25:40'),(2,'2025-05',60000,12.00,'2025-04-21 17:25:40');
/*!40000 ALTER TABLE `openai_usage_summary` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `permissions`
--

DROP TABLE IF EXISTS `permissions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `permissions` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Description` text COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=75 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `permissions`
--

LOCK TABLES `permissions` WRITE;
/*!40000 ALTER TABLE `permissions` DISABLE KEYS */;
INSERT INTO `permissions` VALUES (1,'CanViewUsers','Puede ver la lista de usuarios'),(2,'CanEditUsers','Puede editar usuarios'),(3,'CanDeleteUsers','Puede eliminar usuarios'),(4,'CanManageRoles','Puede crear, editar o eliminar roles'),(5,'CanAccessSupportTickets','Puede ver y responder tickets de soporte'),(6,'CanViewAiModelConfigs','Puede ver configuraciones de modelos de IA'),(7,'CanCreateAiModelConfigs','Puede crear configuraciones de modelos de IA'),(8,'CanUpdateAiModelConfigs','Puede actualizar configuraciones de modelos de IA'),(9,'CanDeleteAiModelConfigs','Puede eliminar configuraciones de modelos de IA'),(10,'CanViewBotActions','Puede ver acciones de bots'),(11,'CanCreateBotActions','Puede crear acciones de bots'),(12,'CanUpdateBotActions','Puede actualizar acciones de bots'),(13,'CanDeleteBotActions','Puede eliminar acciones de bots'),(14,'CanViewBotIntegrations','Puede ver integraciones de bots'),(15,'CanCreateBotIntegrations','Puede crear integraciones de bots'),(16,'CanUpdateBotIntegrations','Puede actualizar integraciones de bots'),(17,'CanDeleteBotIntegrations','Puede eliminar integraciones de bots'),(18,'CanViewBotProfiles','Puede ver perfiles de bots'),(19,'CanCreateBotProfiles','Puede crear perfiles de bots'),(20,'CanUpdateBotProfiles','Puede actualizar perfiles de bots'),(21,'CanDeleteBotProfiles','Puede eliminar perfiles de bots'),(22,'CanViewBots','Puede ver bots'),(23,'CanCreateBot','Puede crear bots'),(24,'CanUpdateBot','Puede actualizar bots'),(25,'CanDeleteBot','Puede eliminar bots'),(26,'CanViewBot','Puede ver un bot específico'),(27,'CanViewBotStyles','Puede ver estilos de bots'),(28,'CanUpdateBotStyles','Puede actualizar estilos de bots'),(29,'CanCreateBotStyles','Puede crear estilos de bots'),(30,'CanDeleteBotStyles','Puede eliminar estilos de bots'),(31,'CanViewConversations','Puede ver conversaciones'),(32,'CanCreateConversations','Puede crear conversaciones'),(33,'CanUpdateConversations','Puede actualizar conversaciones'),(34,'CanDeleteConversations','Puede eliminar conversaciones'),(35,'CanViewGeneratedImages','Puede ver imágenes generadas'),(36,'CanCreateGeneratedImages','Puede crear imágenes generadas'),(37,'CanUpdateGeneratedImages','Puede actualizar imágenes generadas'),(38,'CanDeleteGeneratedImages','Puede eliminar imágenes generadas'),(39,'CanManagePermissions','Puede gestionar permisos'),(40,'CanViewPlans','Puede ver planes'),(41,'CanCreatePlans','Puede crear planes'),(42,'CanUpdatePlans','Puede actualizar planes'),(43,'CanDeletePlans','Puede eliminar planes'),(44,'CanViewPrompts','Puede ver prompts'),(45,'CanCreatePrompts','Puede crear prompts'),(46,'CanUpdatePrompts','Puede actualizar prompts'),(47,'CanDeletePrompts','Puede eliminar prompts'),(48,'ViewRolePermissions','Puede ver permisos asignados a roles'),(49,'AssignPermissionToRole','Puede asignar permisos a roles'),(50,'RevokePermissionFromRole','Puede revocar permisos de roles'),(51,'CanManageRoles','Puede gestionar roles'),(52,'CanViewSubscriptions','Puede ver suscripciones'),(53,'CanCreateSubscriptions','Puede crear suscripciones'),(54,'CanUpdateSubscriptions','Puede actualizar suscripciones'),(55,'CanDeleteSubscriptions','Puede eliminar suscripciones'),(56,'CanViewSupportResponses','Puede ver respuestas de soporte'),(57,'CanCreateSupportResponses','Puede crear respuestas de soporte'),(58,'CanUpdateSupportResponses','Puede actualizar respuestas de soporte'),(59,'CanDeleteSupportResponses','Puede eliminar respuestas de soporte'),(60,'CanViewSupportTickets','Puede ver tickets de soporte'),(61,'CanCreateSupportTickets','Puede crear tickets de soporte'),(62,'CanUpdateSupportTickets','Puede actualizar tickets de soporte'),(63,'CanDeleteSupportTickets','Puede eliminar tickets de soporte'),(64,'CanViewTrainingDataSessions','Puede ver sesiones de datos de entrenamiento'),(65,'CanCreateTrainingDataSessions','Puede crear sesiones de datos de entrenamiento'),(66,'CanUpdateTrainingDataSessions','Puede actualizar sesiones de datos de entrenamiento'),(67,'CanDeleteTrainingDataSessions','Puede eliminar sesiones de datos de entrenamiento'),(68,'CanViewUserBotRelations','Puede ver relaciones entre usuarios y bots'),(69,'CanCreateUserBotRelation','Puede crear relaciones entre usuarios y bots'),(70,'CanEditUserBotRelations','Puede editar relaciones entre usuarios y bots'),(71,'CanDeleteUserBotRelations','Puede eliminar relaciones entre usuarios y bots'),(72,'CanViewUserPreferences','Puede ver preferencias de usuario'),(73,'CanEditUserPreferences','Puede editar preferencias de usuario'),(74,'CanDeleteUserPreferences','Puede eliminar preferencias de usuario');
/*!40000 ALTER TABLE `permissions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `plan_changes`
--

DROP TABLE IF EXISTS `plan_changes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `plan_changes` (
  `id` int NOT NULL AUTO_INCREMENT,
  `plan_id` int NOT NULL,
  `changed_by` int NOT NULL,
  `field_changed` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `old_value` text COLLATE utf8mb4_unicode_ci,
  `new_value` text COLLATE utf8mb4_unicode_ci,
  `changed_at` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `plan_id` (`plan_id`),
  KEY `changed_by` (`changed_by`),
  CONSTRAINT `plan_changes_ibfk_1` FOREIGN KEY (`plan_id`) REFERENCES `plans` (`id`),
  CONSTRAINT `plan_changes_ibfk_2` FOREIGN KEY (`changed_by`) REFERENCES `users` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `plan_changes`
--

LOCK TABLES `plan_changes` WRITE;
/*!40000 ALTER TABLE `plan_changes` DISABLE KEYS */;
INSERT INTO `plan_changes` VALUES (1,1,29,'price','9.99','10.99','2025-04-21 17:28:47'),(2,2,30,'max_tokens','50000','60000','2025-04-21 17:28:47');
/*!40000 ALTER TABLE `plan_changes` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `plans`
--

DROP TABLE IF EXISTS `plans`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `plans` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `description` text COLLATE utf8mb4_unicode_ci,
  `price` decimal(10,2) NOT NULL,
  `max_tokens` int NOT NULL,
  `bots_limit` int DEFAULT '1',
  `is_active` tinyint(1) DEFAULT '1',
  PRIMARY KEY (`id`),
  UNIQUE KEY `unique_plan_name` (`name`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `plans`
--

LOCK TABLES `plans` WRITE;
/*!40000 ALTER TABLE `plans` DISABLE KEYS */;
INSERT INTO `plans` VALUES (1,'Gay Plan','Access to basic features',9.99,5000,1,1),(2,'Premium Plan','Access to premium features',19.99,15000,3,1),(4,'Pro Plan','Access to all features and priority support',29.99,30000,5,1);
/*!40000 ALTER TABLE `plans` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `prompts`
--

DROP TABLE IF EXISTS `prompts`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `prompts` (
  `id` int NOT NULL AUTO_INCREMENT,
  `bot_id` int NOT NULL,
  `user_id` int NOT NULL,
  `conversation_id` int DEFAULT NULL,
  `prompt_text` text COLLATE utf8mb4_unicode_ci NOT NULL,
  `response_text` longtext COLLATE utf8mb4_unicode_ci,
  `tokens_used` int DEFAULT '0',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `source` enum('widget','api','mobile','admin') COLLATE utf8mb4_unicode_ci DEFAULT 'widget',
  PRIMARY KEY (`id`),
  KEY `bot_id` (`bot_id`),
  KEY `user_id` (`user_id`),
  KEY `conversation_id` (`conversation_id`),
  CONSTRAINT `prompts_ibfk_1` FOREIGN KEY (`bot_id`) REFERENCES `bots` (`id`) ON DELETE CASCADE,
  CONSTRAINT `prompts_ibfk_2` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE,
  CONSTRAINT `prompts_ibfk_3` FOREIGN KEY (`conversation_id`) REFERENCES `conversations` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `prompts`
--

LOCK TABLES `prompts` WRITE;
/*!40000 ALTER TABLE `prompts` DISABLE KEYS */;
INSERT INTO `prompts` VALUES (7,15,29,3,'Hello Admin Bot','Hello! How can I assist you?',10,'2025-04-21 17:23:48','widget'),(8,16,30,4,'Hello User Bot','Hello! How can I help you today?',12,'2025-04-21 17:23:48','widget');
/*!40000 ALTER TABLE `prompts` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `rolepermissions`
--

DROP TABLE IF EXISTS `rolepermissions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `rolepermissions` (
  `RoleId` int NOT NULL,
  `PermissionId` int NOT NULL,
  PRIMARY KEY (`RoleId`,`PermissionId`),
  KEY `PermissionId` (`PermissionId`),
  CONSTRAINT `rolepermissions_ibfk_1` FOREIGN KEY (`RoleId`) REFERENCES `roles` (`id`) ON DELETE CASCADE,
  CONSTRAINT `rolepermissions_ibfk_2` FOREIGN KEY (`PermissionId`) REFERENCES `permissions` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `rolepermissions`
--

LOCK TABLES `rolepermissions` WRITE;
/*!40000 ALTER TABLE `rolepermissions` DISABLE KEYS */;
INSERT INTO `rolepermissions` VALUES (2,1),(3,1),(32,1),(32,2),(32,3),(32,4),(3,5),(32,5),(2,6),(2,18),(2,19),(2,20),(2,21),(2,22),(2,23),(2,24),(2,25),(2,26),(2,27),(2,28),(2,31),(2,32),(2,35),(2,36),(2,40),(2,52),(2,56),(2,60),(2,61),(2,72),(2,73);
/*!40000 ALTER TABLE `rolepermissions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `roles`
--

DROP TABLE IF EXISTS `roles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `roles` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `description` text COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`id`),
  UNIQUE KEY `name` (`name`)
) ENGINE=InnoDB AUTO_INCREMENT=35 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `roles`
--

LOCK TABLES `roles` WRITE;
/*!40000 ALTER TABLE `roles` DISABLE KEYS */;
INSERT INTO `roles` VALUES (2,'Usuario',NULL),(3,'Support','Soporte técnico'),(32,'Admin','Administrador del sistema'),(33,'User','Usuario común');
/*!40000 ALTER TABLE `roles` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `style_templates`
--

DROP TABLE IF EXISTS `style_templates`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `style_templates` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int DEFAULT NULL,
  `name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `theme` enum('light','dark','custom') COLLATE utf8mb4_unicode_ci DEFAULT 'light',
  `primary_color` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT '#000000',
  `secondary_color` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT '#ffffff',
  `font_family` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT 'Arial',
  `avatar_url` text COLLATE utf8mb4_unicode_ci,
  `position` enum('bottom-right','bottom-left','top-right','top-left','center-right','center-left','top-center','bottom-center') COLLATE utf8mb4_unicode_ci DEFAULT 'bottom-right',
  `custom_css` text COLLATE utf8mb4_unicode_ci,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_style_templates_user` (`user_id`),
  CONSTRAINT `fk_style_templates_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `style_templates`
--

LOCK TABLES `style_templates` WRITE;
/*!40000 ALTER TABLE `style_templates` DISABLE KEYS */;
INSERT INTO `style_templates` VALUES (1,NULL,'Tema Claro','light','#000000','#ffffff','Arial','path/to/avatar1.png','center-right','/* estilo base para tema claro */','2025-06-06 17:09:35','2025-06-17 16:10:04'),(2,NULL,'Tema Oscuro','dark','#ffffff','#000000','Arial','path/to/avatar2.png','center-right','/* estilo base para tema oscuro */','2025-06-06 17:09:35','2025-06-17 16:10:04');
/*!40000 ALTER TABLE `style_templates` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `subscriptions`
--

DROP TABLE IF EXISTS `subscriptions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `subscriptions` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int NOT NULL,
  `plan_id` int NOT NULL,
  `started_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `expires_at` datetime DEFAULT NULL,
  `status` enum('active','expired','canceled') COLLATE utf8mb4_unicode_ci DEFAULT 'active',
  PRIMARY KEY (`id`),
  KEY `user_id` (`user_id`),
  KEY `plan_id` (`plan_id`),
  CONSTRAINT `subscriptions_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`),
  CONSTRAINT `subscriptions_ibfk_2` FOREIGN KEY (`plan_id`) REFERENCES `plans` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `subscriptions`
--

LOCK TABLES `subscriptions` WRITE;
/*!40000 ALTER TABLE `subscriptions` DISABLE KEYS */;
INSERT INTO `subscriptions` VALUES (1,29,1,'2025-04-21 17:24:45','2026-04-21 00:00:00','active'),(2,30,2,'2025-04-21 17:24:44','2026-04-21 00:00:00','active'),(4,45,2,'2025-05-15 19:45:53','2025-06-15 19:45:53','canceled'),(5,45,4,'2025-06-03 19:23:23','2025-07-03 19:23:23','active');
/*!40000 ALTER TABLE `subscriptions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `support_responses`
--

DROP TABLE IF EXISTS `support_responses`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `support_responses` (
  `id` int NOT NULL AUTO_INCREMENT,
  `ticket_id` int NOT NULL,
  `responder_id` int DEFAULT NULL,
  `message` text COLLATE utf8mb4_unicode_ci NOT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `ticket_id` (`ticket_id`),
  KEY `responder_id` (`responder_id`),
  CONSTRAINT `support_responses_ibfk_1` FOREIGN KEY (`ticket_id`) REFERENCES `support_tickets` (`id`),
  CONSTRAINT `support_responses_ibfk_2` FOREIGN KEY (`responder_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `support_responses`
--

LOCK TABLES `support_responses` WRITE;
/*!40000 ALTER TABLE `support_responses` DISABLE KEYS */;
INSERT INTO `support_responses` VALUES (5,1,29,'We are investigating the issue with Admin Bot and will update soon.','2025-04-21 17:28:19'),(6,2,30,'We have identified the problem with the User Bot and are working on it.','2025-04-21 17:28:19'),(8,2,NULL,'Estamos revisando el inconveniente y te daremos respuesta pronto.','2025-04-29 19:07:31');
/*!40000 ALTER TABLE `support_responses` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `support_tickets`
--

DROP TABLE IF EXISTS `support_tickets`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `support_tickets` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int NOT NULL,
  `subject` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `message` text COLLATE utf8mb4_unicode_ci NOT NULL,
  `status` enum('open','in_progress','closed') COLLATE utf8mb4_unicode_ci DEFAULT 'open',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `user_id` (`user_id`),
  CONSTRAINT `support_tickets_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `support_tickets`
--

LOCK TABLES `support_tickets` WRITE;
/*!40000 ALTER TABLE `support_tickets` DISABLE KEYS */;
INSERT INTO `support_tickets` VALUES (1,29,'Issue with Admin Bot','The Admin Bot is not responding to queries.','open','2025-04-21 17:26:13','2025-04-21 17:26:13'),(2,30,'User Bot Error','The User Bot is giving incorrect responses.','in_progress','2025-04-21 17:26:13','2025-04-21 17:26:13');
/*!40000 ALTER TABLE `support_tickets` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `template_training_sessions`
--

DROP TABLE IF EXISTS `template_training_sessions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `template_training_sessions` (
  `id` int NOT NULL AUTO_INCREMENT,
  `bot_template_id` int NOT NULL,
  `session_name` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `description` text COLLATE utf8mb4_unicode_ci,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `bot_template_id` (`bot_template_id`),
  CONSTRAINT `template_training_sessions_ibfk_1` FOREIGN KEY (`bot_template_id`) REFERENCES `bot_templates` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `template_training_sessions`
--

LOCK TABLES `template_training_sessions` WRITE;
/*!40000 ALTER TABLE `template_training_sessions` DISABLE KEYS */;
INSERT INTO `template_training_sessions` VALUES (1,79,'Entrenamiento manual','Entrenamiento creado desde el panel','2025-06-12 17:32:14','2025-06-12 17:32:14'),(2,79,'Entrenamiento manual','Entrenamiento creado desde el panel','2025-06-12 17:34:33','2025-06-12 17:34:33'),(3,79,'Entrenamiento manual','Entrenamiento creado desde el panel','2025-06-12 17:43:40','2025-06-12 17:43:40'),(4,79,'Entrenamiento manual','Entrenamiento creado desde el panel','2025-06-12 17:44:05','2025-06-12 17:44:05'),(5,79,'Entrenamiento manual','Entrenamiento creado desde el panel','2025-06-12 17:46:07','2025-06-12 17:46:07'),(6,79,'Entrenamiento manual','Entrenamiento creado desde el panel','2025-06-13 15:04:36','2025-06-13 15:04:36'),(7,79,'Entrenamiento manual','Entrenamiento creado desde el panel','2025-06-13 15:41:43','2025-06-13 15:41:43'),(8,79,'Entrenamiento manual','Entrenamiento creado desde el panel','2025-06-16 13:39:20','2025-06-16 13:39:20'),(9,80,'Entrenamiento manual','Entrenamiento creado desde el panel','2025-06-19 19:14:08','2025-06-19 19:14:08');
/*!40000 ALTER TABLE `template_training_sessions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `token_usage_logs`
--

DROP TABLE IF EXISTS `token_usage_logs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `token_usage_logs` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int NOT NULL,
  `bot_id` int NOT NULL,
  `tokens_used` int NOT NULL,
  `usage_date` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `user_id` (`user_id`),
  KEY `bot_id` (`bot_id`),
  CONSTRAINT `token_usage_logs_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE,
  CONSTRAINT `token_usage_logs_ibfk_2` FOREIGN KEY (`bot_id`) REFERENCES `bots` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `token_usage_logs`
--

LOCK TABLES `token_usage_logs` WRITE;
/*!40000 ALTER TABLE `token_usage_logs` DISABLE KEYS */;
/*!40000 ALTER TABLE `token_usage_logs` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `training_custom_texts`
--

DROP TABLE IF EXISTS `training_custom_texts`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `training_custom_texts` (
  `id` int NOT NULL AUTO_INCREMENT,
  `bot_template_id` int NOT NULL,
  `bot_id` int DEFAULT NULL,
  `template_training_session_id` int DEFAULT NULL,
  `user_id` int NOT NULL,
  `content` text COLLATE utf8mb4_unicode_ci NOT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `user_id` (`user_id`),
  KEY `fk_texts_template` (`bot_template_id`),
  KEY `fk_texts_session` (`template_training_session_id`),
  KEY `fk_training_custom_texts_bot` (`bot_id`),
  CONSTRAINT `fk_texts_session` FOREIGN KEY (`template_training_session_id`) REFERENCES `template_training_sessions` (`id`),
  CONSTRAINT `fk_texts_template` FOREIGN KEY (`bot_template_id`) REFERENCES `bot_templates` (`id`),
  CONSTRAINT `fk_training_custom_texts_bot` FOREIGN KEY (`bot_id`) REFERENCES `bots` (`id`) ON DELETE SET NULL,
  CONSTRAINT `training_custom_texts_ibfk_2` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=46 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `training_custom_texts`
--

LOCK TABLES `training_custom_texts` WRITE;
/*!40000 ALTER TABLE `training_custom_texts` DISABLE KEYS */;
INSERT INTO `training_custom_texts` VALUES (8,79,NULL,NULL,45,'dfghredgertghtrhg','2025-06-16 14:01:35','2025-06-16 14:01:35'),(9,79,NULL,NULL,45,'dgtbh','2025-06-16 14:01:50','2025-06-16 14:01:50'),(10,79,NULL,NULL,45,'yrju6yhjyuhn','2025-06-16 14:07:46','2025-06-16 14:07:46'),(11,79,NULL,NULL,45,'yrju6yhjyuhn','2025-06-16 14:08:05','2025-06-16 14:08:05'),(12,79,NULL,NULL,45,'srrthgtr','2025-06-16 14:26:00','2025-06-16 14:26:00'),(13,79,NULL,NULL,45,'xbgfdb','2025-06-16 15:22:45','2025-06-16 15:22:45'),(14,79,NULL,NULL,45,'srtg','2025-06-16 15:30:39','2025-06-16 15:30:39'),(15,79,NULL,NULL,45,'srtg','2025-06-16 15:36:17','2025-06-16 15:36:17'),(16,79,NULL,NULL,45,'wedewdewd','2025-06-16 15:41:49','2025-06-16 15:41:49'),(17,79,NULL,NULL,45,'wedewdewd','2025-06-16 16:58:34','2025-06-16 16:58:34'),(18,79,NULL,NULL,45,'ytfyt','2025-06-16 16:59:10','2025-06-16 16:59:10'),(19,79,NULL,NULL,45,'sfvgdf','2025-06-16 19:02:52','2025-06-16 19:02:52'),(20,79,NULL,NULL,45,'sdced','2025-06-16 19:10:22','2025-06-16 19:10:22'),(21,79,NULL,NULL,45,'erfgre4','2025-06-16 19:12:00','2025-06-16 19:12:00'),(22,79,NULL,NULL,45,'dhythyt','2025-06-16 19:20:58','2025-06-16 19:20:58'),(23,79,NULL,NULL,45,'wstryhtrh','2025-06-16 19:35:13','2025-06-16 19:35:13'),(24,79,NULL,NULL,45,'etjytj','2025-06-16 19:38:20','2025-06-16 19:38:20'),(25,79,NULL,NULL,45,'etj','2025-06-16 19:38:47','2025-06-16 19:38:47'),(26,79,NULL,NULL,45,'sfrhsgd','2025-06-16 19:41:07','2025-06-16 19:41:07'),(27,79,NULL,NULL,45,'qergfre','2025-06-16 19:43:41','2025-06-16 19:43:41'),(28,79,NULL,NULL,45,'wsrthtr','2025-06-16 19:45:26','2025-06-16 19:45:26'),(29,79,NULL,NULL,45,'eyjyt','2025-06-16 19:47:19','2025-06-16 19:47:19'),(30,79,NULL,NULL,45,'fdgfd','2025-06-16 19:57:12','2025-06-16 19:57:12'),(31,79,NULL,NULL,45,'SGBGF','2025-06-17 11:46:25','2025-06-17 11:46:25'),(32,79,NULL,NULL,45,'dygjruytj','2025-06-17 13:36:14','2025-06-17 13:36:14'),(33,79,NULL,NULL,45,'yhfr','2025-06-17 14:03:13','2025-06-17 14:03:13'),(34,79,NULL,NULL,45,'ghndnjh','2025-06-17 14:44:08','2025-06-17 14:44:08'),(35,79,NULL,NULL,45,'adfvf','2025-06-17 14:46:28','2025-06-17 14:46:28'),(36,79,NULL,NULL,45,'afg6efrv','2025-06-17 14:49:35','2025-06-17 14:49:35'),(37,79,NULL,NULL,45,'dghnhgfn','2025-06-17 15:12:14','2025-06-17 15:12:14'),(38,79,NULL,NULL,45,'sfhgtdfhb','2025-06-17 15:28:49','2025-06-17 15:28:49'),(39,79,NULL,NULL,45,'dtntyrun','2025-06-17 15:42:01','2025-06-17 15:42:01'),(40,79,NULL,NULL,45,'gfhhg','2025-06-19 17:32:56','2025-06-19 17:32:56'),(41,79,NULL,NULL,45,'gfhhg','2025-06-19 18:04:50','2025-06-19 18:04:50'),(42,79,NULL,NULL,45,'gfhhg','2025-06-19 18:55:46','2025-06-19 18:55:46'),(43,79,NULL,NULL,45,'gfhhg','2025-06-19 18:59:26','2025-06-19 18:59:26'),(44,79,NULL,NULL,45,'gfhhgsfgbvfgsd','2025-06-19 19:02:08','2025-06-19 19:02:08'),(45,80,NULL,NULL,45,'rror al eliminar proveedor: Microsoft.EntityFrameworkCore.DbUpdateException: An error occurred while saving the entity changes. See the inner exception for details.\n\n ---> MySqlConnector.MySqlException (0x80004005): Cannot delete or update a parent row: a foreign key constraint fails (`chatbot_platform`.`bots`, CONSTRAINT `fk_bots_ia_provider` FOREIGN KEY (`ia_provider_id`) REFERENCES `bot_ia_providers` (`id`))\n\n   at MySqlConnector.Core.ServerSession.ReceiveReplyAsync(IOBehavior ioBehavior, CancellationToken cancellationToken) in /_/src/MySqlConnector/Core/ServerSession.cs:line 894\n\n   at MySqlConnector.Core.ResultSet.ReadResultSetHeaderAsync(IOBehavior ioBehavior) in /_/src/MySqlConnector/Core/ResultSet.cs:line 37\n\n   at MySqlConnector.MySqlDataReader.ActivateResultSet(CancellationToken cancellationToken) in /_/src/MySqlConnector/MySqlDataReader.cs:line 130\n\n   at MySqlConnector.MySqlDataReader.InitAsync(CommandListPosition commandListPosition, ICommandPayloadCreator payloadCreator, IDictionary`2 cachedProcedures, IMySqlCommand command, CommandBehavior behavior, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /_/src/MySqlConnector/MySqlDataReader.cs:line 483\n\n   at MySqlConnector.Core.CommandExecutor.ExecuteReaderAsync(CommandListPosition commandListPosition, ICommandPayloadCreator payloadCreator, CommandBehavior behavior, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /_/src/MySqlConnector/Core/CommandExecutor.cs:line 56\n\n   at MySqlConnector.MySqlCommand.ExecuteReaderAsync(CommandBehavior behavior, IOBehavior ioBehavior, CancellationToken cancellationToken) in /_/src/MySqlConnector/MySqlCommand.cs:line 357\n\n   at MySqlConnector.MySqlCommand.ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken) in /_/src/MySqlConnector/MySqlCommand.cs:line 350\n\n   at Microsoft.EntityFrameworkCore.Storage.RelationalCommand.ExecuteReaderAsync(RelationalCommandParameterObject parameterObject, CancellationToken cancellationToken)\n\n   at Microsoft.EntityFrameworkCore.Storage.RelationalCommand.ExecuteReaderAsync(RelationalCommandParameterObject parameterObject, CancellationToken cancellationToken)\n\n   at Microsoft.EntityFrameworkCore.Update.ReaderModificationCommandBatch.ExecuteAsync(IRelationalConnection connection, CancellationToken cancellationToken)\n\n   --- End of inner exception stack trace ---\n\n   at Microsoft.EntityFrameworkCore.Update.ReaderModificationCommandBatch.ExecuteAsync(IRelationalConnection connection, CancellationToken cancellationToken)\n\n   at Microsoft.EntityFrameworkCore.Update.Internal.BatchExecutor.ExecuteAsync(IEnumerable`1 commandBatches, IRelationalConnection connection, CancellationToken cancellationToken)\n\n   at Microsoft.EntityFrameworkCore.Update.Internal.BatchExecutor.ExecuteAsync(IEnumerable`1 commandBatches, IRelationalConnection connection, CancellationToken cancellationToken)\n\n   at Microsoft.EntityFrameworkCore.Update.Internal.BatchExecutor.ExecuteAsync(IEnumerable`1 commandBatches, IRelationalConnection connection, CancellationToken cancellationToken)\n\n   at Microsoft.EntityFrameworkCore.ChangeTracking.Internal.StateManager.SaveChangesAsync(IList`1 entriesToSave, CancellationToken cancellationToken)\n\n   at Microsoft.EntityFrameworkCore.ChangeTracking.Internal.StateManager.SaveChangesAsync(StateManager stateManager, Boolean acceptAllChangesOnSuccess, CancellationToken cancellationToken)\n\n   at Pomelo.EntityFrameworkCore.MySql.Storage.Internal.MySqlExecutionStrategy.ExecuteAsync[TState,TResult](TState state, Func`4 operation, Func`4 verifySucceeded, CancellationToken cancellationToken)\n\n   at Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync(Boolean acceptAllChangesOnSuccess, CancellationToken cancellationToken)\n\n   at Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync(Boolean acceptAllChangesOnSuccess, CancellationToken cancellationToken)\n\n   at Voia.Api.Controllers.BotIaProvidersController.Delete(Int32 id) in C:\\Users\\Puesto7\\Documents\\voia-docker\\Voia.Api\\Controllers\\BotIaProvidersController.cs:line 120\n\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor.TaskOfIActionResultExecutor.Execute(ActionContext actionContext, IActionResultTypeMapper mapper, ObjectMethodExecutor executor, Object controller, Object[] arguments)\n\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.<InvokeActionMethodAsync>g__Awaited|12_0(ControllerActionInvoker invoker, ValueTask`1 actionResultValueTask)\n\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.<InvokeNextActionFilterAsync>g__Awaited|10_0(ControllerActionInvoker invoker, Task lastTask, State next, Scope scope, Object state, Boolean isCompleted)\n\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.Rethrow(ActionExecutedContextSealed context)\n\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.Next(State& next, Scope& scope, Object& state, Boolean& isCompleted)\n\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.<InvokeInnerFilterAsync>g__Awaited|13_0(ControllerActionInvoker invoker, Task lastTask, State next, Scope scope, Object state, Boolean isCompleted)\n\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.<InvokeFilterPipelineAsync>g__Awaited|20_0(ResourceInvoker invoker, Task lastTask, State next, Scope scope, Object state, Boolean isCompleted)\n\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.<InvokeAsync>g__Awaited|17_0(ResourceInvoker invoker, Task task, IDisposable scope)\n\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.<InvokeAsync>g__Awaited|17_0(ResourceInvoker invoker, Task task, IDisposable scope)\n\n   at Swashbuckle.AspNetCore.SwaggerUI.SwaggerUIMiddleware.Invoke(HttpContext httpContext)\n\n   at Swashbuckle.AspNetCore.Swagger.SwaggerMiddleware.Invoke(HttpContext httpContext, ISwaggerProvider swaggerProvider)\n\n   at Microsoft.AspNetCore.Authorization.AuthorizationMiddleware.Invoke(HttpContext context)\n\n   at Microsoft.AspNetCore.Authentication.AuthenticationMiddleware.Invoke(HttpContext context)\n\n   at Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddlewareImpl.Invoke(HttpContext context)\n\n\n\nHEADERS\n\n=======\n\nAccept: application/json, text/plain, */*\n\nConnection: keep-alive\n\nHost: localhost:5006\n\nUser-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:139.0) Gecko/20100101 Firefox/139.0\n\nAccept-Encoding: gzip, deflate, br, zstd\n\nAccept-Language: es-ES,es;q=0.8,en-US;q=0.5,en;q=0.3\n\nOrigin: http://localhost:3000\n\nReferer: http://localhost:3000/\n\nSec-Fetch-Dest: empty\n\nSec-Fetch-Mode: cors\n\nSec-Fetch-Site: same-site\n\nPriority: u=0','2025-06-19 19:14:55','2025-06-19 19:14:55');
/*!40000 ALTER TABLE `training_custom_texts` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `training_data_sessions`
--

DROP TABLE IF EXISTS `training_data_sessions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `training_data_sessions` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int NOT NULL,
  `bot_id` int DEFAULT NULL,
  `data_summary` text COLLATE utf8mb4_unicode_ci,
  `data_type` enum('text','document','audio','image') COLLATE utf8mb4_unicode_ci DEFAULT 'text',
  `status` enum('pending','processing','completed','failed') COLLATE utf8mb4_unicode_ci DEFAULT 'pending',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `user_id` (`user_id`),
  KEY `bot_id` (`bot_id`),
  CONSTRAINT `training_data_sessions_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`),
  CONSTRAINT `training_data_sessions_ibfk_2` FOREIGN KEY (`bot_id`) REFERENCES `bots` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `training_data_sessions`
--

LOCK TABLES `training_data_sessions` WRITE;
/*!40000 ALTER TABLE `training_data_sessions` DISABLE KEYS */;
INSERT INTO `training_data_sessions` VALUES (3,29,21,'Entrenamiento sobre respuestas de soporte al cliente.','text','pending','2025-04-29 20:12:19'),(4,30,22,'Entrenamiento sobre ventas y negociación.','text','processing','2025-04-29 20:12:19');
/*!40000 ALTER TABLE `training_data_sessions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `training_urls`
--

DROP TABLE IF EXISTS `training_urls`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `training_urls` (
  `id` int NOT NULL AUTO_INCREMENT,
  `bot_template_id` int NOT NULL,
  `bot_id` int DEFAULT NULL,
  `template_training_session_id` int DEFAULT NULL,
  `user_id` int NOT NULL,
  `url` text COLLATE utf8mb4_unicode_ci NOT NULL,
  `status` enum('pending','processed','failed') COLLATE utf8mb4_unicode_ci DEFAULT 'pending',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `user_id` (`user_id`),
  KEY `fk_urls_template` (`bot_template_id`),
  KEY `fk_urls_session` (`template_training_session_id`),
  KEY `fk_training_urls_bot` (`bot_id`),
  CONSTRAINT `fk_training_urls_bot` FOREIGN KEY (`bot_id`) REFERENCES `bots` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_urls_session` FOREIGN KEY (`template_training_session_id`) REFERENCES `template_training_sessions` (`id`),
  CONSTRAINT `fk_urls_template` FOREIGN KEY (`bot_template_id`) REFERENCES `bot_templates` (`id`),
  CONSTRAINT `training_urls_ibfk_2` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=17 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `training_urls`
--

LOCK TABLES `training_urls` WRITE;
/*!40000 ALTER TABLE `training_urls` DISABLE KEYS */;
INSERT INTO `training_urls` VALUES (7,79,NULL,NULL,45,'http://localhost:8000/admin4E6izXCO57Jq/errores.pdf','pending','2025-06-13 19:22:37','2025-06-13 19:22:37'),(8,79,NULL,NULL,45,'http://localhost:8000/admin4E6izXCO57Jq/errores.pdf','pending','2025-06-13 19:23:06','2025-06-13 19:23:06'),(9,79,NULL,NULL,45,'http://localhost:8000/admin4E6izXCO57Jq/errores.pdf','pending','2025-06-13 19:25:55','2025-06-13 19:25:55'),(10,79,NULL,NULL,45,'https://fgfghgfdsxb.pdf','pending','2025-06-13 19:26:46','2025-06-13 19:26:46'),(11,79,NULL,NULL,45,'https://fgfghgfdsxb.pdf','pending','2025-06-13 19:38:50','2025-06-13 19:38:50'),(12,79,NULL,NULL,45,'https://fgfghgfdsxb.pdf','pending','2025-06-13 19:40:05','2025-06-13 19:40:05'),(13,79,NULL,NULL,45,'https://tuweb.com/archivo.pdf','pending','2025-06-16 13:39:45','2025-06-16 13:39:45'),(14,79,NULL,NULL,45,'https://tuweb.com/archivo.pdf','pending','2025-06-16 14:01:35','2025-06-16 14:01:35'),(15,79,NULL,NULL,45,'https://tuweb.com/archivo.pdf','pending','2025-06-16 14:08:05','2025-06-16 14:08:05'),(16,80,NULL,NULL,45,'https://tuweb.com/archivo.pdf','pending','2025-06-19 19:14:55','2025-06-19 19:14:55');
/*!40000 ALTER TABLE `training_urls` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `uploaded_documents`
--

DROP TABLE IF EXISTS `uploaded_documents`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `uploaded_documents` (
  `id` int NOT NULL AUTO_INCREMENT,
  `bot_template_id` int NOT NULL,
  `bot_id` int DEFAULT NULL,
  `template_training_session_id` int DEFAULT NULL,
  `user_id` int NOT NULL,
  `file_name` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `file_type` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `file_path` text COLLATE utf8mb4_unicode_ci NOT NULL,
  `uploaded_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `indexed` tinyint(1) DEFAULT '0',
  PRIMARY KEY (`id`),
  KEY `user_id` (`user_id`),
  KEY `fk_uploaded_documents_session` (`template_training_session_id`),
  KEY `fk_uploaded_documents_bot_template` (`bot_template_id`),
  KEY `fk_uploaded_documents_bot` (`bot_id`),
  CONSTRAINT `fk_uploaded_documents_bot` FOREIGN KEY (`bot_id`) REFERENCES `bots` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_uploaded_documents_bot_template` FOREIGN KEY (`bot_template_id`) REFERENCES `bot_templates` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_uploaded_documents_session` FOREIGN KEY (`template_training_session_id`) REFERENCES `template_training_sessions` (`id`),
  CONSTRAINT `fk_uploaded_documents_template` FOREIGN KEY (`bot_template_id`) REFERENCES `bot_templates` (`id`),
  CONSTRAINT `uploaded_documents_ibfk_2` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=31 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `uploaded_documents`
--

LOCK TABLES `uploaded_documents` WRITE;
/*!40000 ALTER TABLE `uploaded_documents` DISABLE KEYS */;
INSERT INTO `uploaded_documents` VALUES (12,79,NULL,NULL,45,'#111875 - Etiqueta 20250606-163854.pdf','application/pdf','Uploads\\Documents\\934be289-a034-43c9-9e69-f4b0ec58160c_#111875 - Etiqueta 20250606-163854.pdf','2025-06-13 17:46:18',0),(13,79,NULL,NULL,45,'#111880 - Etiqueta 20250612-162215.pdf','application/pdf','Uploads\\Documents\\3e6971eb-71c2-44b9-8fcb-c5771d8707c7_#111880 - Etiqueta 20250612-162215.pdf','2025-06-13 17:46:35',0),(14,79,NULL,NULL,45,'#111880 - Etiqueta 20250612-162215.pdf','application/pdf','Uploads\\Documents\\bdf0d8b8-1ed8-4ee8-9649-d4c31145fecd_#111880 - Etiqueta 20250612-162215.pdf','2025-06-13 18:13:19',0),(15,79,NULL,NULL,45,'Ordenes médicas.Pdf','application/pdf','Uploads\\Documents\\6e51c7f1-887c-4ecb-b6f4-368d6f043ea6_Ordenes médicas.Pdf','2025-06-13 18:14:16',0),(16,79,NULL,NULL,45,'Ordenes médicas.Pdf','application/pdf','Uploads\\Documents\\e8b5b7bf-1887-4c49-97ff-3c42d8956ead_Ordenes médicas.Pdf','2025-06-13 18:41:11',0),(17,79,NULL,NULL,45,'Ordenes médicas.Pdf','application/pdf','Uploads\\Documents\\1509f9cc-fc69-43b1-87f9-47c55703516b_Ordenes médicas.Pdf','2025-06-13 18:43:17',0),(18,79,NULL,NULL,45,'#111880 - Etiqueta 20250612-162215.pdf','application/pdf','Uploads\\Documents\\78cfcecf-1d13-4f0f-968b-ac1ade0badc9_#111880 - Etiqueta 20250612-162215.pdf','2025-06-13 18:44:21',0),(19,79,NULL,NULL,45,'#111880 - Etiqueta 20250612-162215.pdf','application/pdf','Uploads\\Documents\\20613a70-1246-4016-b91b-3e694c74007f_#111880 - Etiqueta 20250612-162215.pdf','2025-06-13 18:49:48',0),(20,79,NULL,NULL,45,'#111875 - Etiqueta 20250606-163854.pdf','application/pdf','Uploads\\Documents\\734c7a39-05ba-4e40-ab2b-9be0550a93b4_#111875 - Etiqueta 20250606-163854.pdf','2025-06-13 18:50:19',0),(21,79,NULL,NULL,45,'#111875 - Etiqueta 20250606-163854.pdf','application/pdf','Uploads\\Documents\\f4b02c2a-d468-417e-a616-c52646d0ee8d_#111875 - Etiqueta 20250606-163854.pdf','2025-06-13 19:14:17',0),(22,79,NULL,NULL,45,'#111875 - Etiqueta 20250606-163854.pdf','application/pdf','Uploads\\Documents\\b34fc282-fb9a-4ea2-9634-d96fded31838_#111875 - Etiqueta 20250606-163854.pdf','2025-06-13 19:22:36',0),(23,79,NULL,NULL,45,'#111875 - Etiqueta 20250606-163854.pdf','application/pdf','Uploads\\Documents\\940b1ca7-b291-4ebf-ab0e-c944be038031_#111875 - Etiqueta 20250606-163854.pdf','2025-06-13 19:25:55',0),(24,79,NULL,NULL,45,'Ordenes médicas.Pdf','application/pdf','Uploads\\Documents\\b51e6d57-3a69-4da5-aa6f-7c1ef0bce3df_Ordenes médicas.Pdf','2025-06-13 19:26:46',0),(25,79,NULL,NULL,45,'Ordenes médicas.Pdf','application/pdf','Uploads\\Documents\\b61127e1-731f-423c-bc67-6048c30cb62e_Ordenes médicas.Pdf','2025-06-13 19:38:50',0),(26,79,NULL,NULL,45,'Ordenes médicas.Pdf','application/pdf','Uploads\\Documents\\c3244b50-2799-4674-9f2c-b169d35f8223_Ordenes médicas.Pdf','2025-06-13 19:40:04',0),(27,79,NULL,NULL,45,'Ordenes médicas-1.Pdf','application/pdf','Uploads\\Documents\\81030102-1045-4d25-a078-03d844116424_Ordenes médicas-1.Pdf','2025-06-16 13:39:45',0),(28,79,NULL,NULL,45,'Ordenes médicas-1.Pdf','application/pdf','Uploads\\Documents\\444ced55-5248-40f1-8ee8-99c8b3ebac3c_Ordenes médicas-1.Pdf','2025-06-16 14:01:35',0),(29,79,NULL,NULL,45,'Ordenes médicas-1.Pdf','application/pdf','Uploads\\Documents\\1bab7d00-04a7-4ed5-a6be-b0bfe4179926_Ordenes médicas-1.Pdf','2025-06-16 14:08:05',0),(30,80,NULL,NULL,45,'Ordenes médicas-1.Pdf','application/pdf','Uploads\\Documents\\3834d1f2-8740-42ab-85c0-8b7273dac163_Ordenes médicas-1.Pdf','2025-06-19 19:14:55',0);
/*!40000 ALTER TABLE `uploaded_documents` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `user_bot_relations`
--

DROP TABLE IF EXISTS `user_bot_relations`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `user_bot_relations` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int NOT NULL,
  `bot_id` int NOT NULL,
  `relationship_type` enum('amistad','romántico','coach','asistente','otro') COLLATE utf8mb4_unicode_ci DEFAULT 'otro',
  `interaction_score` int DEFAULT '0',
  `last_interaction` datetime DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `user_id` (`user_id`),
  KEY `bot_id` (`bot_id`),
  CONSTRAINT `user_bot_relations_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`),
  CONSTRAINT `user_bot_relations_ibfk_2` FOREIGN KEY (`bot_id`) REFERENCES `bots` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `user_bot_relations`
--

LOCK TABLES `user_bot_relations` WRITE;
/*!40000 ALTER TABLE `user_bot_relations` DISABLE KEYS */;
INSERT INTO `user_bot_relations` VALUES (3,29,21,'coach',10,'2025-04-29 20:10:33','2025-04-29 20:10:33'),(4,30,22,'romántico',5,'2025-04-29 20:10:33','2025-04-29 20:10:33');
/*!40000 ALTER TABLE `user_bot_relations` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `user_consents`
--

DROP TABLE IF EXISTS `user_consents`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `user_consents` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int NOT NULL,
  `consent_type` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `granted` tinyint(1) DEFAULT '0',
  `granted_at` datetime DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `user_id` (`user_id`),
  CONSTRAINT `user_consents_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `user_consents`
--

LOCK TABLES `user_consents` WRITE;
/*!40000 ALTER TABLE `user_consents` DISABLE KEYS */;
INSERT INTO `user_consents` VALUES (1,29,'uso de datos',1,'2025-04-29 20:11:05'),(2,30,'generación de imágenes',1,'2025-04-29 20:11:05');
/*!40000 ALTER TABLE `user_consents` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `user_preferences`
--

DROP TABLE IF EXISTS `user_preferences`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `user_preferences` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int NOT NULL,
  `interest_id` int NOT NULL,
  PRIMARY KEY (`id`),
  KEY `user_id` (`user_id`),
  KEY `interest_id` (`interest_id`),
  CONSTRAINT `user_preferences_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE,
  CONSTRAINT `user_preferences_ibfk_2` FOREIGN KEY (`interest_id`) REFERENCES `interests` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `user_preferences`
--

LOCK TABLES `user_preferences` WRITE;
/*!40000 ALTER TABLE `user_preferences` DISABLE KEYS */;
INSERT INTO `user_preferences` VALUES (5,29,1),(6,29,3),(7,30,4),(8,30,2);
/*!40000 ALTER TABLE `user_preferences` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `users`
--

DROP TABLE IF EXISTS `users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `users` (
  `id` int NOT NULL AUTO_INCREMENT,
  `role_id` int NOT NULL,
  `document_type_id` int DEFAULT NULL,
  `name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `email` varchar(150) COLLATE utf8mb4_unicode_ci NOT NULL,
  `password` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `phone` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `address` text COLLATE utf8mb4_unicode_ci,
  `document_number` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `document_photo_url` text COLLATE utf8mb4_unicode_ci,
  `avatar_url` text COLLATE utf8mb4_unicode_ci,
  `is_verified` tinyint(1) DEFAULT '0',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `email` (`email`),
  KEY `role_id` (`role_id`),
  KEY `document_type_id` (`document_type_id`),
  CONSTRAINT `users_ibfk_1` FOREIGN KEY (`role_id`) REFERENCES `roles` (`id`),
  CONSTRAINT `users_ibfk_2` FOREIGN KEY (`document_type_id`) REFERENCES `document_types` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=46 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `users`
--

LOCK TABLES `users` WRITE;
/*!40000 ALTER TABLE `users` DISABLE KEYS */;
INSERT INTO `users` VALUES (29,32,23,'Admin User','admin2@example.com','adminpassword','3000000000','123 Admin Street','1234567890','url_to_document_photo','url_to_avatar',1,'2025-04-21 17:15:11','2025-04-21 17:15:11'),(30,33,24,'Regular User','user2@example.com','userpassword','3000000001','456 User Avenue','0987654321','url_to_document_photo','url_to_avatar',0,'2025-04-21 17:15:11','2025-04-21 17:15:11'),(43,33,23,'maria la loca','jorge@gmail.com','$2a$11$ym0xhT4F.oLoG8O/ndt7Ku2zTCVx6qws425jXCt5IjcOSPButIQnm','3178531535','en la mierda con jorge','1096188422','','/uploads/a374cc51-711e-489c-93c0-cdb594463044.jpg',0,'2025-05-09 15:28:22','2025-05-12 16:11:15'),(44,2,23,'jhgvff jhgf','jhgf@hgfd.com','$2a$11$RsGXT38T9OvhA1bx4cIqoe5y8jz7R1AQoA/ZbkpmiFxU97XEWel3W','3178531555','hgfds','1096188466','','',0,'2025-05-13 17:00:04','2025-05-13 19:00:04'),(45,32,23,'ivan herrera','ivan@gmail.com','$2a$11$mPXK2f4yOzT/PFL1geDL1.Iu8v4.N/ufux3Ai6uyl/ZY.h4W4QtSq','3107898881','lkjhgfdsa','1096188889','','/uploads/4a7f4132-8b7e-4f0d-8099-6bf3a84a40b8.jpg',0,'2025-05-13 17:32:27','2025-05-19 13:33:05');
/*!40000 ALTER TABLE `users` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `vector_embeddings`
--

DROP TABLE IF EXISTS `vector_embeddings`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `vector_embeddings` (
  `id` int NOT NULL AUTO_INCREMENT,
  `knowledge_chunk_id` int NOT NULL,
  `embedding_vector` blob NOT NULL,
  `provider` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT 'openai',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `knowledge_chunk_id` (`knowledge_chunk_id`),
  CONSTRAINT `vector_embeddings_ibfk_1` FOREIGN KEY (`knowledge_chunk_id`) REFERENCES `knowledge_chunks` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `vector_embeddings`
--

LOCK TABLES `vector_embeddings` WRITE;
/*!40000 ALTER TABLE `vector_embeddings` DISABLE KEYS */;
/*!40000 ALTER TABLE `vector_embeddings` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2025-06-19 21:34:47
