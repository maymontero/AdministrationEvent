START TRANSACTION;
CREATE TABLE `AdminUsers` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `Username` varchar(80) NOT NULL,
    `NormalizedUsername` varchar(80) NOT NULL,
    `DisplayName` varchar(100) NOT NULL,
    `PasswordHash` varchar(500) NOT NULL,
    `IsOwner` tinyint(1) NOT NULL,
    `IsActive` tinyint(1) NOT NULL,
    `FailedAttempts` int NOT NULL,
    `LockedUntilUtc` datetime(6) NULL,
    `SecurityStamp` varchar(36) NOT NULL,
    PRIMARY KEY (`Id`)
);

CREATE UNIQUE INDEX `IX_AdminUsers_NormalizedUsername` ON `AdminUsers` (`NormalizedUsername`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261014184426_AdminAccounts', '10.0.9');

COMMIT;

