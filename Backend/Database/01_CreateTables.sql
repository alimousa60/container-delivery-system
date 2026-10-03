-- =============================================
-- Container Vehicle Delivery Management System
-- SQL Server Database Schema
-- =============================================

CREATE DATABASE ContainerDeliveryDB;
GO

USE ContainerDeliveryDB;
GO

-- =============================================
-- USERS TABLE
-- =============================================
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(256) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(512) NOT NULL,
    FullName NVARCHAR(200) NOT NULL,
    PhoneNumber NVARCHAR(20) NULL,
    IsMfaEnabled BIT NOT NULL DEFAULT 0,
    MfaSecret NVARCHAR(32) NULL,
    MfaRecoveryCodes NVARCHAR(MAX) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    LastLoginAt DATETIME2 NULL,
    FailedLoginAttempts INT NOT NULL DEFAULT 0,
    LockedOutUntil DATETIME2 NULL
);
GO

CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_Users_IsActive ON Users(IsActive);
GO

-- =============================================
-- ROLES TABLE
-- =============================================
CREATE TABLE Roles (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(500) NULL
);
GO

INSERT INTO Roles (Name, Description) VALUES 
('Admin', 'Full system access including user management, audit logs, and container deletion'),
('DeliveryUser', 'Can view assigned containers, confirm deliveries, and generate reports');
GO

-- =============================================
-- USER_ROLES TABLE (Many-to-Many)
-- =============================================
CREATE TABLE UserRoles (
    UserId INT NOT NULL,
    RoleId INT NOT NULL,
    AssignedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    AssignedByUserId INT NOT NULL,
    PRIMARY KEY (UserId, RoleId),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE,
    FOREIGN KEY (AssignedByUserId) REFERENCES Users(Id)
);
GO

-- =============================================
-- CONTAINERS TABLE
-- =============================================
CREATE TABLE Containers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ContainerNumber NVARCHAR(50) NOT NULL UNIQUE,
    TotalVehicles INT NOT NULL DEFAULT 0,
    DeliveredVehicles INT NOT NULL DEFAULT 0,
    Status NVARCHAR(20) NOT NULL DEFAULT 'NotStarted', -- NotStarted, InProgress, FullyDelivered
    CreatedByUserId INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    StartedAt DATETIME2 NULL,
    CompletedAt DATETIME2 NULL,
    Notes NVARCHAR(MAX) NULL,
    FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id)
);
GO

CREATE INDEX IX_Containers_ContainerNumber ON Containers(ContainerNumber);
CREATE INDEX IX_Containers_Status ON Containers(Status);
CREATE INDEX IX_Containers_CreatedAt ON Containers(CreatedAt);
GO

-- =============================================
-- VEHICLES TABLE
-- =============================================
CREATE TABLE Vehicles (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Vin NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(500) NOT NULL,
    ContainerId INT NOT NULL,
    IsDelivered BIT NOT NULL DEFAULT 0,
    DeliveredAt DATETIME2 NULL,
    DeliveredByUserId INT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    FOREIGN KEY (ContainerId) REFERENCES Containers(Id) ON DELETE CASCADE,
    FOREIGN KEY (DeliveredByUserId) REFERENCES Users(Id)
);
GO

CREATE INDEX IX_Vehicles_VIN ON Vehicles(Vin);
CREATE INDEX IX_Vehicles_ContainerId ON Vehicles(ContainerId);
CREATE INDEX IX_Vehicles_IsDelivered ON Vehicles(IsDelivered);
CREATE INDEX IX_Vehicles_Description ON Vehicles(Description);
GO

-- =============================================
-- DELIVERY_RECORDS TABLE
-- =============================================
CREATE TABLE DeliveryRecords (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    VehicleId INT NOT NULL,
    ContainerId INT NOT NULL,
    DeliveredByUserId INT NOT NULL,
    DeliveredAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    DeliveryMethod NVARCHAR(20) NOT NULL, -- BarcodeScan, Manual
    Notes NVARCHAR(MAX) NULL,
    ScannedVin NVARCHAR(50) NULL,
    FOREIGN KEY (VehicleId) REFERENCES Vehicles(Id) ON DELETE CASCADE,
    FOREIGN KEY (ContainerId) REFERENCES Containers(Id) ON DELETE CASCADE,
    FOREIGN KEY (DeliveredByUserId) REFERENCES Users(Id)
);
GO

CREATE INDEX IX_DeliveryRecords_VehicleId ON DeliveryRecords(VehicleId);
CREATE INDEX IX_DeliveryRecords_ContainerId ON DeliveryRecords(ContainerId);
CREATE INDEX IX_DeliveryRecords_DeliveredAt ON DeliveryRecords(DeliveredAt);
GO

-- =============================================
-- AUDIT_LOGS TABLE
-- =============================================
CREATE TABLE AuditLogs (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NULL,
    Action NVARCHAR(100) NOT NULL, -- Create, Update, Delete, Login, Logout, Import, Export, Delivery
    EntityType NVARCHAR(100) NOT NULL, -- User, Container, Vehicle, Report, ImportBatch
    EntityId INT NULL,
    OldValues NVARCHAR(MAX) NULL, -- JSON
    NewValues NVARCHAR(MAX) NULL, -- JSON
    IpAddress NVARCHAR(45) NULL, -- IPv6 compatible
    UserAgent NVARCHAR(500) NULL,
    Timestamp DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL
);
GO

CREATE INDEX IX_AuditLogs_UserId ON AuditLogs(UserId);
CREATE INDEX IX_AuditLogs_EntityType_EntityId ON AuditLogs(EntityType, EntityId);
CREATE INDEX IX_AuditLogs_Timestamp ON AuditLogs(Timestamp);
CREATE INDEX IX_AuditLogs_Action ON AuditLogs(Action);
GO

-- =============================================
-- CONTAINER_REPORTS TABLE
-- =============================================
CREATE TABLE ContainerReports (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ContainerId INT NOT NULL,
    GeneratedByUserId INT NOT NULL,
    FilePath NVARCHAR(500) NOT NULL,
    FileName NVARCHAR(255) NOT NULL,
    GeneratedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    TotalVehicles INT NOT NULL,
    DeliveredVehicles INT NOT NULL,
    UndeliveredVehicles INT NOT NULL,
    CompletionPercentage DECIMAL(5,2) NOT NULL,
    FOREIGN KEY (ContainerId) REFERENCES Containers(Id) ON DELETE CASCADE,
    FOREIGN KEY (GeneratedByUserId) REFERENCES Users(Id)
);
GO

CREATE INDEX IX_ContainerReports_ContainerId ON ContainerReports(ContainerId);
CREATE INDEX IX_ContainerReports_GeneratedAt ON ContainerReports(GeneratedAt);
GO

-- =============================================
-- IMPORT_BATCHES TABLE
-- =============================================
CREATE TABLE ImportBatches (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FileName NVARCHAR(255) NOT NULL,
    FilePath NVARCHAR(500) NOT NULL,
    TotalRecords INT NOT NULL DEFAULT 0,
    SuccessfulRecords INT NOT NULL DEFAULT 0,
    FailedRecords INT NOT NULL DEFAULT 0,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending, Processing, Completed, Failed
    ErrorDetails NVARCHAR(MAX) NULL, -- JSON array of errors
    ImportedByUserId INT NOT NULL,
    ImportedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CompletedAt DATETIME2 NULL,
    FOREIGN KEY (ImportedByUserId) REFERENCES Users(Id)
);
GO

CREATE INDEX IX_ImportBatches_ImportedAt ON ImportBatches(ImportedAt);
CREATE INDEX IX_ImportBatches_Status ON ImportBatches(Status);
GO

-- =============================================
-- FULL-TEXT SEARCH SETUP
-- =============================================
CREATE FULLTEXT CATALOG VehicleCatalog AS DEFAULT;
GO

CREATE FULLTEXT INDEX ON Vehicles
(
    Vin LANGUAGE 1033,
    Description LANGUAGE 1033
)
KEY INDEX PK_Vehicles
ON VehicleCatalog
WITH CHANGE_TRACKING AUTO;
GO

CREATE FULLTEXT INDEX ON Containers
(
    ContainerNumber LANGUAGE 1033,
    Notes LANGUAGE 1033
)
KEY INDEX PK_Containers
ON VehicleCatalog
WITH CHANGE_TRACKING AUTO;
GO

-- =============================================
-- STORED PROCEDURES
-- =============================================

-- Update Container Status based on vehicle delivery status
CREATE PROCEDURE UpdateContainerStatus
    @ContainerId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE Containers
    SET 
        DeliveredVehicles = (
            SELECT COUNT(*) FROM Vehicles WHERE ContainerId = @ContainerId AND IsDelivered = 1
        ),
        Status = CASE 
            WHEN (SELECT COUNT(*) FROM Vehicles WHERE ContainerId = @ContainerId AND IsDelivered = 1) = 0 
                THEN 'NotStarted'
            WHEN (SELECT COUNT(*) FROM Vehicles WHERE ContainerId = @ContainerId AND IsDelivered = 1) 
                 = (SELECT TotalVehicles FROM Containers WHERE Id = @ContainerId)
                THEN 'FullyDelivered'
            ELSE 'InProgress'
        END,
        CompletedAt = CASE 
            WHEN (SELECT COUNT(*) FROM Vehicles WHERE ContainerId = @ContainerId AND IsDelivered = 1) 
                 = (SELECT TotalVehicles FROM Containers WHERE Id = @ContainerId)
                THEN SYSUTCDATETIME()
            ELSE CompletedAt
        END
    WHERE Id = @ContainerId;
END;
GO

-- Get Dashboard Statistics
CREATE PROCEDURE GetDashboardStatistics
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        (SELECT COUNT(*) FROM Containers) AS TotalContainers,
        (SELECT COUNT(*) FROM Vehicles) AS TotalVehicles,
        (SELECT COUNT(*) FROM Containers WHERE Status = 'NotStarted') AS PendingContainers,
        (SELECT COUNT(*) FROM Containers WHERE Status = 'FullyDelivered') AS CompletedContainers,
        (SELECT COUNT(*) FROM Vehicles WHERE IsDelivered = 1) AS DeliveredVehicles,
        (SELECT COUNT(*) FROM Vehicles WHERE IsDelivered = 0) AS UndeliveredVehicles;
END;
GO