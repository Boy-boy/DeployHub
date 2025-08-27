BEGIN TRANSACTION;
CREATE TABLE [Projects] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CurrentDeploymentConfigId] uniqueidentifier NULL,
    CONSTRAINT [PK_Projects] PRIMARY KEY ([Id])
);

CREATE TABLE [ProjectDeploymentConfigs] (
    [Id] uniqueidentifier NOT NULL,
    [ProjectId] uniqueidentifier NOT NULL,
    [Tag] nvarchar(100) NOT NULL,
    [YamlContent] nvarchar(max) NOT NULL,
    [Description] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_ProjectDeploymentConfigs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProjectDeploymentConfigs_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_ProjectDeploymentConfigs_ProjectId_Tag] ON [ProjectDeploymentConfigs] ([ProjectId], [Tag]);

CREATE UNIQUE INDEX [IX_Projects_Name] ON [Projects] ([Name]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250818010413_addProject', N'9.0.0');

COMMIT;
GO

