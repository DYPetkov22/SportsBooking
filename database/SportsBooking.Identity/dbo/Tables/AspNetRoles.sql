CREATE TABLE [dbo].[AspNetRoles]
(
    [Id] NVARCHAR(450) NOT NULL,
    [Name] NVARCHAR(256) NULL,
    [NormalizedName] NVARCHAR(256) NULL,
    [ConcurrencyStamp] NVARCHAR(MAX) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);
GO

CREATE UNIQUE INDEX [RoleNameIndex]
    ON [dbo].[AspNetRoles] ([NormalizedName])
    WHERE [NormalizedName] IS NOT NULL;
GO
