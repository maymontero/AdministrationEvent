CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) NOT NULL,
    `ProductVersion` varchar(32) NOT NULL,
    PRIMARY KEY (`MigrationId`)
);

START TRANSACTION;
IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261007232743_InitialInvitations')
BEGIN
    CREATE TABLE `Invitations` (
        `Id` bigint NOT NULL AUTO_INCREMENT,
        `PublicId` char(36) NOT NULL,
        `EventType` varchar(80) NOT NULL,
        `Hosts` varchar(200) NULL,
        `Date` datetime(6) NULL,
        `Message` varchar(2000) NULL,
        `CoverImageUrl` varchar(2000) NULL,
        `Title` varchar(250) NOT NULL,
        `Slug` varchar(250) NOT NULL,
        `IsPublished` tinyint(1) NOT NULL,
        PRIMARY KEY (`Id`)
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261007232743_InitialInvitations')
BEGIN
    CREATE TABLE `Locations` (
        `Id` bigint NOT NULL AUTO_INCREMENT,
        `Name` longtext CHARACTER SET utf8mb4 NULL,
        `Address` longtext CHARACTER SET utf8mb4 NULL,
        `Latitude` decimal(10,7) NULL,
        `Longitude` decimal(10,7) NULL,
        PRIMARY KEY (`Id`)
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261007232743_InitialInvitations')
BEGIN
    CREATE TABLE `InvitationSections` (
        `Id` bigint NOT NULL AUTO_INCREMENT,
        `InvitationId` bigint NOT NULL,
        `Order` int NOT NULL,
        `Enabled` tinyint(1) NOT NULL,
        `Title` varchar(120) NOT NULL,
        `Content` longtext NULL,
        `ImageUrl` varchar(2000) NULL,
        `LinkLabel` varchar(80) NULL,
        `LinkUrl` varchar(2000) NULL,
        PRIMARY KEY (`Id`),
        CONSTRAINT `FK_InvitationSections_Invitations_InvitationId` FOREIGN KEY (`InvitationId`) REFERENCES `Invitations` (`Id`) ON DELETE CASCADE
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261007232743_InitialInvitations')
BEGIN
    CREATE TABLE `InvitationThemes` (
        `Id` bigint NOT NULL AUTO_INCREMENT,
        `CornerRadius` int NOT NULL,
        `FontSize` int NOT NULL,
        `ContentWidth` int NOT NULL,
        `InvitationId` bigint NOT NULL,
        PRIMARY KEY (`Id`),
        CONSTRAINT `FK_InvitationThemes_Invitations_InvitationId` FOREIGN KEY (`InvitationId`) REFERENCES `Invitations` (`Id`) ON DELETE CASCADE
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261007232743_InitialInvitations')
BEGIN
    CREATE TABLE `RSVPConfigurations` (
        `Id` bigint NOT NULL AUTO_INCREMENT,
        `InvitationId` bigint NOT NULL,
        `Enabled` tinyint(1) NOT NULL,
        `Title` longtext CHARACTER SET utf8mb4 NULL,
        `Description` longtext CHARACTER SET utf8mb4 NULL,
        `GoogleFormUrl` longtext CHARACTER SET utf8mb4 NULL,
        `ConfirmationDeadline` datetime(6) NULL,
        PRIMARY KEY (`Id`),
        CONSTRAINT `FK_RSVPConfigurations_Invitations_InvitationId` FOREIGN KEY (`InvitationId`) REFERENCES `Invitations` (`Id`) ON DELETE CASCADE
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261007232743_InitialInvitations')
BEGIN
    CREATE TABLE `Events` (
        `Id` bigint NOT NULL AUTO_INCREMENT,
        `InvitationId` bigint NOT NULL,
        `Title` varchar(250) NULL,
        `DateTime` datetime(6) NULL,
        `DressCode` varchar(500) NULL,
        `Order` int NOT NULL,
        `LocationId` bigint NULL,
        `ParentEventId` bigint NULL,
        PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Events_Invitations_InvitationId` FOREIGN KEY (`InvitationId`) REFERENCES `Invitations` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_Events_Locations_LocationId` FOREIGN KEY (`LocationId`) REFERENCES `Locations` (`Id`) ON DELETE SET NULL,
        CONSTRAINT `FK_Events_Events_ParentEventId` FOREIGN KEY (`ParentEventId`) REFERENCES `Events` (`Id`) ON DELETE RESTRICT
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261007232743_InitialInvitations')
BEGIN
    CREATE TABLE `AudioResources` (
        `Id` bigint NOT NULL AUTO_INCREMENT,
        `InvitationThemeId` bigint NOT NULL,
        `Key` varchar(100) NOT NULL,
        `Url` varchar(2000) NOT NULL,
        `Loop` tinyint(1) NOT NULL,
        PRIMARY KEY (`Id`),
        CONSTRAINT `FK_AudioResources_InvitationThemes_InvitationThemeId` FOREIGN KEY (`InvitationThemeId`) REFERENCES `InvitationThemes` (`Id`) ON DELETE CASCADE
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261007232743_InitialInvitations')
BEGIN
    CREATE TABLE `ColorResources` (
        `Id` bigint NOT NULL AUTO_INCREMENT,
        `InvitationThemeId` bigint NOT NULL,
        `Key` varchar(100) NOT NULL,
        `Value` varchar(50) NOT NULL,
        PRIMARY KEY (`Id`),
        CONSTRAINT `FK_ColorResources_InvitationThemes_InvitationThemeId` FOREIGN KEY (`InvitationThemeId`) REFERENCES `InvitationThemes` (`Id`) ON DELETE CASCADE
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261007232743_InitialInvitations')
BEGIN
    CREATE TABLE `FontResources` (
        `Id` bigint NOT NULL AUTO_INCREMENT,
        `InvitationThemeId` bigint NOT NULL,
        `Key` varchar(100) NOT NULL,
        `FontFamily` varchar(250) NOT NULL,
        `Url` varchar(2000) NULL,
        PRIMARY KEY (`Id`),
        CONSTRAINT `FK_FontResources_InvitationThemes_InvitationThemeId` FOREIGN KEY (`InvitationThemeId`) REFERENCES `InvitationThemes` (`Id`) ON DELETE CASCADE
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261007232743_InitialInvitations')
BEGIN
    CREATE TABLE `ImageResources` (
        `Id` bigint NOT NULL AUTO_INCREMENT,
        `InvitationThemeId` bigint NOT NULL,
        `Key` varchar(100) NOT NULL,
        `Url` varchar(2000) NOT NULL,
        `Alt` longtext CHARACTER SET utf8mb4 NULL,
        PRIMARY KEY (`Id`),
        CONSTRAINT `FK_ImageResources_InvitationThemes_InvitationThemeId` FOREIGN KEY (`InvitationThemeId`) REFERENCES `InvitationThemes` (`Id`) ON DELETE CASCADE
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261007232743_InitialInvitations')
BEGIN
    CREATE UNIQUE INDEX `IX_AudioResources_InvitationThemeId_Key` ON `AudioResources` (`InvitationThemeId`, `Key`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261007232743_InitialInvitations')
BEGIN
    CREATE UNIQUE INDEX `IX_ColorResources_InvitationThemeId_Key` ON `ColorResources` (`InvitationThemeId`, `Key`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261007232743_InitialInvitations')
BEGIN
    CREATE INDEX `IX_Events_LocationId` ON `Events` (`LocationId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261007232743_InitialInvitations')
BEGIN
    CREATE INDEX `IX_Events_ParentEventId` ON `Events` (`ParentEventId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261007232743_InitialInvitations')
BEGIN
    CREATE INDEX `IX_Events_InvitationId_Order` ON `Events` (`InvitationId`, `Order`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261007232743_InitialInvitations')
BEGIN
    CREATE UNIQUE INDEX `IX_FontResources_InvitationThemeId_Key` ON `FontResources` (`InvitationThemeId`, `Key`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261007232743_InitialInvitations')
BEGIN
    CREATE UNIQUE INDEX `IX_ImageResources_InvitationThemeId_Key` ON `ImageResources` (`InvitationThemeId`, `Key`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261007232743_InitialInvitations')
BEGIN
    CREATE UNIQUE INDEX `IX_Invitations_PublicId` ON `Invitations` (`PublicId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261007232743_InitialInvitations')
BEGIN
    CREATE UNIQUE INDEX `IX_Invitations_Slug` ON `Invitations` (`Slug`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261007232743_InitialInvitations')
BEGIN
    CREATE INDEX `IX_InvitationSections_InvitationId_Order` ON `InvitationSections` (`InvitationId`, `Order`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261007232743_InitialInvitations')
BEGIN
    CREATE UNIQUE INDEX `IX_InvitationThemes_InvitationId` ON `InvitationThemes` (`InvitationId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261007232743_InitialInvitations')
BEGIN
    CREATE UNIQUE INDEX `IX_RSVPConfigurations_InvitationId` ON `RSVPConfigurations` (`InvitationId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261007232743_InitialInvitations')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261007232743_InitialInvitations', '10.0.9');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261008220050_Net10ProviderAlignment')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261008220050_Net10ProviderAlignment', '10.0.9');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261008222907_InvitationDesignAndGuestResponses')
BEGIN
    ALTER TABLE `InvitationThemes` ADD `Layout` varchar(20) NOT NULL DEFAULT 'classic';
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261008222907_InvitationDesignAndGuestResponses')
BEGIN
    ALTER TABLE `Invitations` ADD `HasConfirmedTime` tinyint(1) NOT NULL DEFAULT TRUE;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261008222907_InvitationDesignAndGuestResponses')
BEGIN
    ALTER TABLE `Invitations` ADD `RsvpEnabled` tinyint(1) NOT NULL DEFAULT FALSE;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261008222907_InvitationDesignAndGuestResponses')
BEGIN
    ALTER TABLE `Invitations` ADD `ShowCountdown` tinyint(1) NOT NULL DEFAULT FALSE;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261008222907_InvitationDesignAndGuestResponses')
BEGIN
    ALTER TABLE `Invitations` ADD `TimeZone` varchar(100) NOT NULL DEFAULT 'America/Montevideo';
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261008222907_InvitationDesignAndGuestResponses')
BEGIN
    CREATE TABLE `GuestResponses` (
        `Id` bigint NOT NULL AUTO_INCREMENT,
        `InvitationId` bigint NOT NULL,
        `Name` varchar(120) NOT NULL,
        `Contact` varchar(200) NULL,
        `Attending` tinyint(1) NOT NULL,
        `GuestCount` int NOT NULL,
        `Notes` varchar(500) NULL,
        `SubmittedAtUtc` datetime(6) NOT NULL,
        PRIMARY KEY (`Id`),
        CONSTRAINT `FK_GuestResponses_Invitations_InvitationId` FOREIGN KEY (`InvitationId`) REFERENCES `Invitations` (`Id`) ON DELETE CASCADE
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261008222907_InvitationDesignAndGuestResponses')
BEGIN
    CREATE INDEX `IX_GuestResponses_InvitationId_SubmittedAtUtc` ON `GuestResponses` (`InvitationId`, `SubmittedAtUtc`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261008222907_InvitationDesignAndGuestResponses')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261008222907_InvitationDesignAndGuestResponses', '10.0.9');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261009174631_GuestAdministration')
BEGIN
    ALTER TABLE `GuestResponses` ADD `GuestId` bigint NULL;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261009174631_GuestAdministration')
BEGIN
    CREATE TABLE `Guests` (
        `Id` bigint NOT NULL AUTO_INCREMENT,
        `InvitationId` bigint NOT NULL,
        `Name` varchar(120) NOT NULL,
        `Contact` varchar(200) NULL,
        `InvitedCount` int NOT NULL,
        `Notes` varchar(500) NULL,
        `IsArchived` tinyint(1) NOT NULL,
        PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Guests_Invitations_InvitationId` FOREIGN KEY (`InvitationId`) REFERENCES `Invitations` (`Id`) ON DELETE CASCADE
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261009174631_GuestAdministration')
BEGIN
    CREATE INDEX `IX_GuestResponses_GuestId` ON `GuestResponses` (`GuestId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261009174631_GuestAdministration')
BEGIN
    CREATE INDEX `IX_Guests_InvitationId_IsArchived` ON `Guests` (`InvitationId`, `IsArchived`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261009174631_GuestAdministration')
BEGIN
    ALTER TABLE `GuestResponses` ADD CONSTRAINT `FK_GuestResponses_Guests_GuestId` FOREIGN KEY (`GuestId`) REFERENCES `Guests` (`Id`) ON DELETE SET NULL;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261009174631_GuestAdministration')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261009174631_GuestAdministration', '10.0.9');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261009175916_GuestWaitingList')
BEGIN
    ALTER TABLE `Guests` ADD `IsWaitlisted` tinyint(1) NOT NULL DEFAULT FALSE;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261009175916_GuestWaitingList')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261009175916_GuestWaitingList', '10.0.9');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261013223601_GuestTagsAndTables')
BEGIN
    ALTER TABLE `Guests` ADD `TableId` bigint NULL;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261013223601_GuestTagsAndTables')
BEGIN
    CREATE TABLE `GuestTags` (
        `Id` bigint NOT NULL AUTO_INCREMENT,
        `InvitationId` bigint NOT NULL,
        `Name` varchar(80) NOT NULL,
        `Color` varchar(7) NOT NULL,
        PRIMARY KEY (`Id`),
        CONSTRAINT `FK_GuestTags_Invitations_InvitationId` FOREIGN KEY (`InvitationId`) REFERENCES `Invitations` (`Id`) ON DELETE CASCADE
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261013223601_GuestTagsAndTables')
BEGIN
    CREATE TABLE `SeatingTables` (
        `Id` bigint NOT NULL AUTO_INCREMENT,
        `InvitationId` bigint NOT NULL,
        `Name` varchar(80) NOT NULL,
        `Capacity` int NOT NULL,
        PRIMARY KEY (`Id`),
        CONSTRAINT `FK_SeatingTables_Invitations_InvitationId` FOREIGN KEY (`InvitationId`) REFERENCES `Invitations` (`Id`) ON DELETE CASCADE
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261013223601_GuestTagsAndTables')
BEGIN
    CREATE TABLE `GuestTagAssignments` (
        `GuestId` bigint NOT NULL,
        `TagId` bigint NOT NULL,
        PRIMARY KEY (`GuestId`, `TagId`),
        CONSTRAINT `FK_GuestTagAssignments_GuestTags_TagId` FOREIGN KEY (`TagId`) REFERENCES `GuestTags` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_GuestTagAssignments_Guests_GuestId` FOREIGN KEY (`GuestId`) REFERENCES `Guests` (`Id`) ON DELETE CASCADE
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261013223601_GuestTagsAndTables')
BEGIN
    CREATE INDEX `IX_Guests_TableId` ON `Guests` (`TableId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261013223601_GuestTagsAndTables')
BEGIN
    CREATE INDEX `IX_GuestTagAssignments_TagId` ON `GuestTagAssignments` (`TagId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261013223601_GuestTagsAndTables')
BEGIN
    CREATE UNIQUE INDEX `IX_GuestTags_InvitationId_Name` ON `GuestTags` (`InvitationId`, `Name`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261013223601_GuestTagsAndTables')
BEGIN
    CREATE UNIQUE INDEX `IX_SeatingTables_InvitationId_Name` ON `SeatingTables` (`InvitationId`, `Name`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261013223601_GuestTagsAndTables')
BEGIN
    ALTER TABLE `Guests` ADD CONSTRAINT `FK_Guests_SeatingTables_TableId` FOREIGN KEY (`TableId`) REFERENCES `SeatingTables` (`Id`) ON DELETE SET NULL;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261013223601_GuestTagsAndTables')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261013223601_GuestTagsAndTables', '10.0.9');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261014010112_RsvpDietaryAndSong')
BEGIN
    ALTER TABLE `GuestResponses` ADD `DietaryRestrictions` varchar(500) NULL;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261014010112_RsvpDietaryAndSong')
BEGIN
    ALTER TABLE `GuestResponses` ADD `SongRequest` varchar(200) NULL;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261014010112_RsvpDietaryAndSong')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261014010112_RsvpDietaryAndSong', '10.0.9');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261014022815_SectionTitleFontSize')
BEGIN
    ALTER TABLE `InvitationSections` ADD `TitleFontSize` int NULL;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261014022815_SectionTitleFontSize')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261014022815_SectionTitleFontSize', '10.0.9');
END;

COMMIT;

