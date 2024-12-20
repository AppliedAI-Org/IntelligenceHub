CREATE TABLE Tools (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    [Name] NVARCHAR(255) UNIQUE NOT NULL,
    [Description] NVARCHAR(255) NOT NULL,
    [Required] NVARCHAR(255) NOT NULL,
    ExecutionUrl NVARCHAR(255),
    ExecutionMethod NVARCHAR(255),
    ExecutionBase64Key NVARCHAR(255)
);
