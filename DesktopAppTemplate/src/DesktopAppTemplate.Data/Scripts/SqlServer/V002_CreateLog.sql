CREATE TABLE dbo.Log (
    Id          BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Log PRIMARY KEY,
    LoggedAt    NVARCHAR(40)  NOT NULL,
    Level       NVARCHAR(20)  NOT NULL,
    Category    NVARCHAR(256) NOT NULL,
    Message     NVARCHAR(MAX) NOT NULL,
    Exception   NVARCHAR(MAX) NULL,
    UserName    NVARCHAR(256) NULL,
    MachineName NVARCHAR(256) NULL
);

CREATE INDEX IX_Log_LoggedAt ON dbo.Log (LoggedAt);
