START TRANSACTION;
CREATE TABLE `InvitationAccesses` (
    `UserId` bigint NOT NULL,
    `InvitationId` bigint NOT NULL,
    `AvailableFromUtc` datetime(6) NULL,
    PRIMARY KEY (`UserId`, `InvitationId`),
    CONSTRAINT `FK_InvitationAccesses_AdminUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AdminUsers` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_InvitationAccesses_Invitations_InvitationId` FOREIGN KEY (`InvitationId`) REFERENCES `Invitations` (`Id`) ON DELETE CASCADE
);

CREATE INDEX `IX_InvitationAccesses_InvitationId` ON `InvitationAccesses` (`InvitationId`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261017010409_InvitationPermissions', '10.0.9');

COMMIT;

