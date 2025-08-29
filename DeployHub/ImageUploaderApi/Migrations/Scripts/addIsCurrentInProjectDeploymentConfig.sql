BEGIN TRANSACTION;
ALTER TABLE [ProjectDeploymentConfigs] ADD [IsCurrent] bit NOT NULL DEFAULT CAST(0 AS bit);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250829052534_addIsCurrentInProjectDeploymentConfig', N'9.0.0');

COMMIT;
GO

