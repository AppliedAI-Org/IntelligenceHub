﻿CREATE TABLE Properties (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    [Name] NVARCHAR(64) NOT NULL,
    [Type] NVARCHAR(255) NOT NULL,
    [Description] NVARCHAR(200) NOT NULL,
    ToolId INT NOT NULL
);
