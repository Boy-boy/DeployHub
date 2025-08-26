IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [ProjectYamls] (
    [Id] uniqueidentifier NOT NULL,
    [ProjectName] nvarchar(450) NULL,
    [Version] nvarchar(450) NULL,
    [IsCurrent] bit NOT NULL,
    [YamlContent] nvarchar(max) NULL,
    [ChangeDescription] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ProjectYamls] PRIMARY KEY ([Id])
);
GO

CREATE INDEX [IX_ProjectYamls_ProjectName] ON [ProjectYamls] ([ProjectName]);
GO

CREATE UNIQUE INDEX [IX_ProjectYamls_ProjectName_Version] ON [ProjectYamls] ([ProjectName], [Version]) WHERE [ProjectName] IS NOT NULL AND [Version] IS NOT NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250527085728_initialProjectYaml', N'7.0.19');
GO

COMMIT;
GO

