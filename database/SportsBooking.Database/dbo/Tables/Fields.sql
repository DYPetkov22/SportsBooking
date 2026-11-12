CREATE TABLE dbo.Fields (
    FieldID INT IDENTITY(1,1) CONSTRAINT PK_Fields PRIMARY KEY,
    FieldName NVARCHAR(50) NOT NULL,
    CenterID INT NOT NULL,
    SportID INT NOT NULL,
    PricePerHour DECIMAL(8,2) NOT NULL,
    CONSTRAINT FK_Fields_Centers FOREIGN KEY (CenterID) REFERENCES dbo.SportsCenters(CenterID),
    CONSTRAINT FK_Fields_Sports FOREIGN KEY (SportID) REFERENCES dbo.Sports(SportID),
    CONSTRAINT CK_Fields_Price CHECK (PricePerHour > 0)
);
GO

CREATE INDEX IX_Fields_Center_Sport ON dbo.Fields(CenterID, SportID);
GO
