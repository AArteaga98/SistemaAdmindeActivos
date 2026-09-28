--CREATE DATABASE PRUEBAQUOM
--USE PRUEBAQUOM

CREATE TABLE dbo.AppUsers (
 Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppUsers PRIMARY KEY,
 UserName nvarchar(100) NOT NULL,
 PasswordHash nvarchar(500) NOT NULL,
 RoleName varchar(20) NOT NULL,
 IsActive bit NOT NULL CONSTRAINT DF_AppUsers_Active DEFAULT (1),
 CreatedAt datetime2(3) NOT NULL CONSTRAINT DF_AppUsers_Created DEFAULT SYSUTCDATETIME(),
 CONSTRAINT CK_AppUsers_Role CHECK (RoleName IN ('Administrador','Operador')),
 CONSTRAINT CK_AppUsers_UserName CHECK (LEN(TRIM(UserName)) > 0),
 CONSTRAINT CK_AppUsers_Hash CHECK (LEN(TRIM(PasswordHash)) > 0)
);
CREATE UNIQUE INDEX UX_AppUsers_UserName ON dbo.AppUsers(UserName);
GO

CREATE TABLE dbo.Suppliers (
 Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Suppliers PRIMARY KEY,
 Name nvarchar(160) NOT NULL,
 ContactEmail nvarchar(254) NULL,
 IsActive bit NOT NULL CONSTRAINT DF_Suppliers_Active DEFAULT (1),
 CreatedAt datetime2(3) NOT NULL CONSTRAINT DF_Suppliers_Created DEFAULT SYSUTCDATETIME(),
 CONSTRAINT CK_Suppliers_Name CHECK (LEN(TRIM(Name)) > 0)
);
GO

CREATE TABLE dbo.SupplierServices (
 SupplierId bigint NOT NULL,
 ServiceType varchar(20) NOT NULL,
 CONSTRAINT PK_SupplierServices PRIMARY KEY (SupplierId,ServiceType),
 CONSTRAINT FK_SupplierServices_Supplier FOREIGN KEY (SupplierId) REFERENCES dbo.Suppliers(Id),
 CONSTRAINT CK_SupplierServices_Type CHECK (ServiceType IN ('Compra','Mantenimiento','Arrendamiento'))
);
GO
CREATE TABLE dbo.Employees (
 Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Employees PRIMARY KEY,
 EmployeeNumber nvarchar(40) NOT NULL,
 FullName nvarchar(160) NOT NULL,
 Email nvarchar(254) NOT NULL,
 IsActive bit NOT NULL CONSTRAINT DF_Employees_Active DEFAULT (1),
 CreatedAt datetime2(3) NOT NULL CONSTRAINT DF_Employees_Created DEFAULT SYSUTCDATETIME(),
 CONSTRAINT CK_Employees_Number CHECK (LEN(TRIM(EmployeeNumber)) > 0),
 CONSTRAINT CK_Employees_Name CHECK (LEN(TRIM(FullName)) > 0)
);
CREATE UNIQUE INDEX UX_Employees_Number ON dbo.Employees(EmployeeNumber);
GO
CREATE TABLE dbo.Assets (
 Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Assets PRIMARY KEY,
 AssetCode nvarchar(50) NOT NULL,
 SerialNumber nvarchar(100) NULL,
 Category nvarchar(60) NOT NULL,
 Brand nvarchar(80) NOT NULL,
 Model nvarchar(100) NOT NULL,
 OwnershipType varchar(20) NOT NULL,
 SupplierId bigint NULL,
 Status varchar(20) NOT NULL CONSTRAINT DF_Assets_Status DEFAULT ('Disponible'),
 CurrentLocation nvarchar(160) NOT NULL,
 PurchaseDate date NULL,
 RentalEndDate date NULL,
 CreatedAt datetime2(3) NOT NULL CONSTRAINT DF_Assets_Created DEFAULT SYSUTCDATETIME(),
 UpdatedAt datetime2(3) NOT NULL CONSTRAINT DF_Assets_Updated DEFAULT SYSUTCDATETIME(),
 CONSTRAINT FK_Assets_Supplier FOREIGN KEY (SupplierId) REFERENCES dbo.Suppliers(Id),
 CONSTRAINT CK_Assets_Code CHECK (LEN(TRIM(AssetCode)) > 0),
 CONSTRAINT CK_Assets_Category CHECK (LEN(TRIM(Category)) > 0),
 CONSTRAINT CK_Assets_Brand CHECK (LEN(TRIM(Brand)) > 0),
 CONSTRAINT CK_Assets_Model CHECK (LEN(TRIM(Model)) > 0),
 CONSTRAINT CK_Assets_Location CHECK (LEN(TRIM(CurrentLocation)) > 0),
 CONSTRAINT CK_Assets_Ownership CHECK (OwnershipType IN ('Propio','Arrendado')),
 CONSTRAINT CK_Assets_Status CHECK (Status IN ('Disponible','Asignado','Mantenimiento','Retirado')),
 CONSTRAINT CK_Assets_RentedSupplier CHECK (OwnershipType <> 'Arrendado' OR SupplierId IS NOT NULL),
 CONSTRAINT CK_Assets_RentalDate CHECK (RentalEndDate IS NULL OR OwnershipType = 'Arrendado')
);
CREATE UNIQUE INDEX UX_Assets_Code ON dbo.Assets(AssetCode);
CREATE UNIQUE INDEX UX_Assets_Serial ON dbo.Assets(SerialNumber) WHERE SerialNumber IS NOT NULL;
CREATE INDEX IX_Assets_Filters ON dbo.Assets(Status,Category,Id);
GO
CREATE TABLE dbo.AssetAssignments (
 Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_AssetAssignments PRIMARY KEY,
 AssetId bigint NOT NULL,
 EmployeeId bigint NOT NULL,
 AssignedAt datetime2(3) NOT NULL CONSTRAINT DF_Assignments_Assigned DEFAULT SYSUTCDATETIME(),
 AssignedByUserId bigint NOT NULL,
 ReturnedAt datetime2(3) NULL,
 ReturnCondition nvarchar(300) NULL,
 ReturnedByUserId bigint NULL,
 CONSTRAINT FK_Assignments_Asset FOREIGN KEY (AssetId) REFERENCES dbo.Assets(Id),
 CONSTRAINT FK_Assignments_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(Id),
 CONSTRAINT FK_Assignments_AssignedBy FOREIGN KEY (AssignedByUserId) REFERENCES dbo.AppUsers(Id),
 CONSTRAINT FK_Assignments_ReturnedBy FOREIGN KEY (ReturnedByUserId) REFERENCES dbo.AppUsers(Id),
CONSTRAINT CK_Assignments_Return CHECK ((ReturnedAt IS NULL AND ReturnCondition IS NULL AND ReturnedByUserId IS NULL) OR (ReturnedAt IS NOT NULL AND ReturnCondition IS NOT NULL AND ReturnedByUserId IS NOT NULL AND ReturnedAt >= AssignedAt))
);
CREATE UNIQUE INDEX UX_Assignments_OneActive ON dbo.AssetAssignments(AssetId) WHERE ReturnedAt IS NULL;
CREATE INDEX IX_Assignments_Employee ON dbo.AssetAssignments(EmployeeId,ReturnedAt);
GO

CREATE TABLE dbo.AssetMovements (
 Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_AssetMovements PRIMARY KEY,
 AssetId bigint NOT NULL,
 AssignmentId bigint NULL,
 MovementType varchar(20) NOT NULL,
 PreviousStatus varchar(20) NULL,
 NewStatus varchar(20) NULL,
 PreviousLocation nvarchar(160) NULL,
 NewLocation nvarchar(160) NULL,
 PerformedByUserId bigint NOT NULL,
 OccurredAt datetime2(3) NOT NULL CONSTRAINT DF_Movements_At DEFAULT SYSUTCDATETIME(),
 Observations nvarchar(500) NULL,
 CONSTRAINT FK_Movements_Asset FOREIGN KEY (AssetId) REFERENCES dbo.Assets(Id),
 CONSTRAINT FK_Movements_Assignment FOREIGN KEY (AssignmentId) REFERENCES dbo.AssetAssignments(Id),
 CONSTRAINT FK_Movements_User FOREIGN KEY (PerformedByUserId) REFERENCES dbo.AppUsers(Id),
 CONSTRAINT CK_Movements_Type CHECK (MovementType IN ('Alta','Asignacion','Devolucion','CambioEstado','CambioUbicacion'))
);
CREATE INDEX IX_Movements_AssetAt ON dbo.AssetMovements(AssetId,OccurredAt DESC,Id DESC);
GO