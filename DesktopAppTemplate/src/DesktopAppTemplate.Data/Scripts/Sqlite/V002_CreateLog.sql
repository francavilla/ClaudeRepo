CREATE TABLE Log (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    LoggedAt    TEXT NOT NULL,
    Level       TEXT NOT NULL,
    Category    TEXT NOT NULL,
    Message     TEXT NOT NULL,
    Exception   TEXT NULL,
    UserName    TEXT NULL,
    MachineName TEXT NULL
);

CREATE INDEX IX_Log_LoggedAt ON Log (LoggedAt);
