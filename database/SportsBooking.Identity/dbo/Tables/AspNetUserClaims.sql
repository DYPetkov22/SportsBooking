CREATE TABLE [dbo].[AspNetUserClaims]
(
    [Id] INT NOT NULL IDENTITY(1,1),
    [UserId] NVARCHAR(450) NOT NULL,
    [ClaimType] NVARCHAR(MAX) NULL,
    [ClaimValue] NVARCHAR(MAX) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId]
        FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_AspNetUserClaims_UserId]
    ON [dbo].[AspNetUserClaims] ([UserId]);
GO
