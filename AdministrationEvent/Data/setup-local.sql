-- Ejecutar como administrador de MySQL. Reemplazar la contraseña antes de ejecutar.
CREATE DATABASE IF NOT EXISTS invitations CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER IF NOT EXISTS 'invitations_app'@'localhost' IDENTIFIED BY 'REEMPLAZAR_CON_UNA_CONTRASENA_PROPIA';
-- Permisos de desarrollo para aplicar migraciones. Producción: separar usuario de migraciones.
GRANT ALL PRIVILEGES ON invitations.* TO 'invitations_app'@'localhost';
