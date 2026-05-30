
CREATE TABLE IF NOT EXISTS "Personnel" (
    "Id"                   SERIAL          PRIMARY KEY,
    "Name"                 VARCHAR(100)    NOT NULL,
    "ICNumber"             VARCHAR(20),
    "BirthDate"            DATE,
    "Email"                VARCHAR(150)    NOT NULL,
    "ResidentialAddress"   TEXT,
    "ResidentialPhone"     VARCHAR(20),
    "OfficePhone"          VARCHAR(20),
    "Designation"          VARCHAR(100),
    "JobDescription"       TEXT,
    "Department"           VARCHAR(100),
    "IsStoresIncharge"     BOOLEAN         NOT NULL DEFAULT FALSE,
    "Building"             VARCHAR(100),
    "ReportingOfficer"     VARCHAR(100),
    "IdCardNumber"         VARCHAR(30),
    "IdCardExpiryDate"     DATE,
    "PhotoPath"            VARCHAR(500),
    "CreatedAt"            TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "UpdatedAt"            TIMESTAMPTZ
);


CREATE UNIQUE INDEX IF NOT EXISTS "IX_Personnel_Email"
    ON "Personnel" ("Email");


CREATE INDEX IF NOT EXISTS "IX_Personnel_CreatedAt"
    ON "Personnel" ("CreatedAt" DESC);

CREATE INDEX IF NOT EXISTS "IX_Personnel_Department"
    ON "Personnel" ("Department");


