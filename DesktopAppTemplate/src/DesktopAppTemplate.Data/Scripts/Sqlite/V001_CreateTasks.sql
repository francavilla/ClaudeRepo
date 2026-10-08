CREATE TABLE Tasks (
    Id          TEXT    NOT NULL PRIMARY KEY,
    Title       TEXT    NOT NULL,
    CreatedAt   TEXT    NOT NULL,
    IsCompleted INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX IX_Tasks_CreatedAt ON Tasks (CreatedAt);
