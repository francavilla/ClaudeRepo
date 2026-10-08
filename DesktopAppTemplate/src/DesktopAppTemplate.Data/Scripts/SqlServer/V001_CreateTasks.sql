CREATE TABLE dbo.Tasks (
    Id          NVARCHAR(36)  NOT NULL CONSTRAINT PK_Tasks PRIMARY KEY,
    Title       NVARCHAR(200) NOT NULL,
    CreatedAt   NVARCHAR(40)  NOT NULL,
    IsCompleted BIGINT        NOT NULL CONSTRAINT DF_Tasks_IsCompleted DEFAULT 0
);

CREATE INDEX IX_Tasks_CreatedAt ON dbo.Tasks (CreatedAt);
