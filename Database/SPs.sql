
CREATE PROCEDURE dbo.usp_Users_Create
 @UserName nvarchar(100), @PasswordHash nvarchar(500), @RoleName varchar(20), @Id bigint OUTPUT
AS
BEGIN
 SET NOCOUNT ON;
 SET @UserName=NULLIF(TRIM(@UserName),'');

 IF @UserName IS NULL OR NULLIF(TRIM(@PasswordHash),'') IS NULL OR @RoleName NOT IN ('Administrador','Operador')

  THROW 51001,'Usuario, hash o rol invalido.',1;

 INSERT dbo.AppUsers(UserName,PasswordHash,RoleName) VALUES (@UserName,@PasswordHash,@RoleName);

 SET @Id=CONVERT(bigint,SCOPE_IDENTITY());
END;
GO

CREATE PROCEDURE dbo.usp_Users_GetCredentialsByUserName
@UserName nvarchar(100)
AS
BEGIN
 SET NOCOUNT ON;
 SELECT Id,UserName,PasswordHash,RoleName,IsActive FROM dbo.AppUsers WHERE UserName=@UserName;
END;
GO

CREATE PROCEDURE dbo.usp_Suppliers_Create
 @Name nvarchar(160), @ContactEmail nvarchar(254)=NULL,
 @Purchase bit=0, @Maintenance bit=0, @Rental bit=0, @Id bigint OUTPUT
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 SET @Name=NULLIF(TRIM(@Name),''); SET @ContactEmail=NULLIF(TRIM(@ContactEmail),'');
 IF @Name IS NULL OR (@Purchase=0 AND @Maintenance=0 AND @Rental=0)
  THROW 51002,'Nombre y al menos un servicio son obligatorios.',1;
 BEGIN TRY
  BEGIN TRAN;
  INSERT dbo.Suppliers(Name,ContactEmail) VALUES (@Name,@ContactEmail);
  SET @Id=CONVERT(bigint,SCOPE_IDENTITY());
  IF @Purchase=1 INSERT dbo.SupplierServices VALUES (@Id,'Compra');
  IF @Maintenance=1 INSERT dbo.SupplierServices VALUES (@Id,'Mantenimiento');
  IF @Rental=1 INSERT dbo.SupplierServices VALUES (@Id,'Arrendamiento');
  COMMIT;
 END TRY BEGIN CATCH
  IF @@TRANCOUNT>0 ROLLBACK;
  THROW;
 END CATCH
END;
GO

CREATE PROCEDURE dbo.usp_Suppliers_List
AS
BEGIN
 SET NOCOUNT ON;
 SELECT s.Id, s.Name, s.ContactEmail, s.IsActive, s.CreatedAt,
  CAST(CASE WHEN EXISTS(SELECT 1 FROM dbo.SupplierServices x WHERE x.SupplierId=s.Id AND x.ServiceType='Compra') THEN 1 ELSE 0 END AS bit) AS [Purchase],
  CAST(CASE WHEN EXISTS(SELECT 1 FROM dbo.SupplierServices x WHERE x.SupplierId=s.Id AND x.ServiceType='Mantenimiento') THEN 1 ELSE 0 END AS bit) AS [Maintenance],
  CAST(CASE WHEN EXISTS(SELECT 1 FROM dbo.SupplierServices x WHERE x.SupplierId=s.Id AND x.ServiceType='Arrendamiento') THEN 1 ELSE 0 END AS bit) AS [Rental]
 FROM dbo.Suppliers s ORDER BY s.Id;
END;
GO

CREATE PROCEDURE dbo.usp_Employees_Create
 @EmployeeNumber nvarchar(40),@FullName nvarchar(160),@Email nvarchar(254),@Id bigint OUTPUT
AS
BEGIN
 SET NOCOUNT ON;
 SET @EmployeeNumber=NULLIF(TRIM(@EmployeeNumber),'');
 SET @FullName=NULLIF(TRIM(@FullName),''); SET @Email=NULLIF(TRIM(@Email),'');
 IF @EmployeeNumber IS NULL OR @FullName IS NULL OR @Email IS NULL
  THROW 51003,'Numero, nombre y correo son obligatorios.',1;
 IF @Email NOT LIKE '%_@_%._%' OR @Email LIKE '% %'
  THROW 51004,'Correo invalido.',1;
 INSERT dbo.Employees(EmployeeNumber,FullName,Email) VALUES (@EmployeeNumber,@FullName,@Email);
 SET @Id=CONVERT(bigint,SCOPE_IDENTITY());
END;
GO

CREATE PROCEDURE dbo.usp_Employees_List
AS
BEGIN
 SET NOCOUNT ON;
 SELECT Id,EmployeeNumber,FullName,Email,IsActive,CreatedAt FROM dbo.Employees ORDER BY Id;
END;
GO

CREATE PROCEDURE dbo.usp_Assets_Create
 @AssetCode nvarchar(50),@SerialNumber nvarchar(100)=NULL,@Category nvarchar(60),
 @Brand nvarchar(80),@Model nvarchar(100),@OwnershipType varchar(20),
 @SupplierId bigint=NULL,@CurrentLocation nvarchar(160),
 @PurchaseDate date=NULL,@RentalEndDate date=NULL,@ActorUserId bigint,
 @Observations nvarchar(500)=NULL,@Id bigint OUTPUT
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 SET @AssetCode=NULLIF(TRIM(@AssetCode),''); SET @SerialNumber=NULLIF(TRIM(@SerialNumber),'');
 SET @Category=NULLIF(TRIM(@Category),''); SET @Brand=NULLIF(TRIM(@Brand),'');
 SET @Model=NULLIF(TRIM(@Model),''); SET @CurrentLocation=NULLIF(TRIM(@CurrentLocation),'');

 IF @AssetCode IS NULL OR @Category IS NULL OR @Brand IS NULL OR @Model IS NULL OR @CurrentLocation IS NULL
  THROW 51005,'Faltan datos obligatorios del activo.',1;

 IF @OwnershipType NOT IN ('Propio','Arrendado') OR (@OwnershipType='Arrendado' AND @SupplierId IS NULL)
  THROW 51006,'Tipo de propiedad o proveedor invalido.',1;

 IF @RentalEndDate IS NOT NULL AND @OwnershipType<>'Arrendado'
  THROW 51007,'Fecha de fin de arrendamiento invalida.',1;

 BEGIN TRY
  BEGIN TRAN;
  IF NOT EXISTS(SELECT 1 FROM dbo.AppUsers WHERE Id=@ActorUserId AND IsActive=1) 
  THROW 51008,'Usuario invalido.',1;

  IF @SupplierId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.Suppliers WHERE Id=@SupplierId AND IsActive=1)
   THROW 51009,'Proveedor invalido.',1;

  IF @OwnershipType='Arrendado' AND NOT EXISTS(SELECT 1 FROM dbo.SupplierServices WHERE SupplierId=@SupplierId AND ServiceType='Arrendamiento')
   THROW 51010,'El proveedor no ofrece arrendamiento.',1;

  INSERT dbo.Assets(AssetCode,SerialNumber,Category,Brand,Model,OwnershipType,SupplierId,CurrentLocation,PurchaseDate,RentalEndDate)
   VALUES (@AssetCode,@SerialNumber,@Category,@Brand,@Model,@OwnershipType,@SupplierId,@CurrentLocation,@PurchaseDate,@RentalEndDate);
  SET @Id=CONVERT(bigint,SCOPE_IDENTITY());

  INSERT dbo.AssetMovements(AssetId,MovementType,NewStatus,NewLocation,PerformedByUserId,Observations)
   VALUES (@Id,'Alta','Disponible',@CurrentLocation,@ActorUserId,@Observations);

  COMMIT;
 END TRY 
 
 BEGIN CATCH
  IF @@TRANCOUNT>0 ROLLBACK;
  THROW;
 END CATCH

END;
GO

CREATE PROCEDURE dbo.usp_Assets_Update
 @AssetId bigint,@Category nvarchar(60),@Brand nvarchar(80),@Model nvarchar(100),
 @OwnershipType varchar(20),@SupplierId bigint=NULL,@PurchaseDate date=NULL,
 @RentalEndDate date=NULL,@CurrentLocation nvarchar(160),@Status varchar(20),
 @ActorUserId bigint,@Observations nvarchar(500)=NULL
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 SET @Category=NULLIF(TRIM(@Category),''); SET @Brand=NULLIF(TRIM(@Brand),'');
 SET @Model=NULLIF(TRIM(@Model),''); SET @CurrentLocation=NULLIF(TRIM(@CurrentLocation),'');

 IF @Category IS NULL OR @Brand IS NULL OR @Model IS NULL OR @CurrentLocation IS NULL
  THROW 51011,'Faltan datos obligatorios.',1;

 IF @OwnershipType NOT IN ('Propio','Arrendado') OR (@OwnershipType='Arrendado' AND @SupplierId IS NULL)
  OR (@RentalEndDate IS NOT NULL AND @OwnershipType<>'Arrendado')
  THROW 51012,'Propiedad o arrendamiento invalido.',1

 IF @Status NOT IN ('Disponible','Asignado','Mantenimiento','Retirado') THROW 51013,'Estado invalido.',1;

 DECLARE @OldStatus varchar(20),
 @OldLocation nvarchar(160),
 @Active bit;

 BEGIN TRY
  BEGIN TRAN;
  
  SELECT @OldStatus=Status,@OldLocation=CurrentLocation FROM dbo.Assets WITH(UPDLOCK,HOLDLOCK) WHERE Id=@AssetId;

  IF @OldStatus IS NULL THROW 51014,'Activo inexistente.',1;

  IF @OldStatus='Retirado' AND @Status<>'Retirado' THROW 51015,'El retiro es definitivo.',1;

  IF NOT EXISTS(SELECT 1 FROM dbo.AppUsers WHERE Id=@ActorUserId AND IsActive=1) THROW 51008,'Usuario invalido.',1;

  IF @SupplierId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.Suppliers WHERE Id=@SupplierId AND IsActive=1) THROW 51009,'Proveedor invalido.',1;

  IF @OwnershipType='Arrendado' AND NOT EXISTS(SELECT 1 FROM dbo.SupplierServices WHERE SupplierId=@SupplierId AND ServiceType='Arrendamiento')
   THROW 51010,'El proveedor no ofrece arrendamiento.',1;

  SET @Active=CASE WHEN EXISTS(SELECT 1 FROM dbo.AssetAssignments WHERE AssetId=@AssetId AND ReturnedAt IS NULL) THEN 1 ELSE 0 END;
  IF (@Active=1 AND @Status<>'Asignado') OR (@Active=0 AND @Status='Asignado')
   THROW 51016,'Estado y asignacion activa incompatibles.',1;

  UPDATE dbo.Assets SET Category=@Category,Brand=@Brand,Model=@Model,
   OwnershipType=@OwnershipType,SupplierId=@SupplierId,PurchaseDate=@PurchaseDate,
   RentalEndDate=@RentalEndDate,CurrentLocation=@CurrentLocation,Status=@Status,
   UpdatedAt=SYSUTCDATETIME() WHERE Id=@AssetId;
  IF @OldStatus <> @Status
   INSERT dbo.AssetMovements(AssetId,MovementType,PreviousStatus,NewStatus,PerformedByUserId,Observations)
    VALUES (@AssetId,'CambioEstado',@OldStatus,@Status,@ActorUserId,@Observations);
  IF @OldLocation<>@CurrentLocation
   INSERT dbo.AssetMovements(AssetId,MovementType,PreviousLocation,NewLocation,PerformedByUserId,Observations)
    VALUES (@AssetId,'CambioUbicacion',@OldLocation,@CurrentLocation,@ActorUserId,@Observations);
  COMMIT;
 END TRY BEGIN CATCH
  IF @@TRANCOUNT>0 ROLLBACK;
  THROW;
 END CATCH
END;
GO

CREATE PROCEDURE dbo.usp_Assets_Assign
 @AssetId bigint,@EmployeeId bigint,@ActorUserId bigint,
 @Observations nvarchar(500)=NULL,@AssignmentId bigint OUTPUT
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 DECLARE @Status varchar(20),@Now datetime2(3);
 BEGIN TRY
  BEGIN TRAN;
  SELECT @Status=Status FROM dbo.Assets WITH(UPDLOCK,HOLDLOCK) WHERE Id=@AssetId;
  IF @Status IS NULL THROW 51020,'Activo inexistente.',1;
  IF @Status<>'Disponible' THROW 51021,'Activo no disponible.',1;
  IF NOT EXISTS(SELECT 1 FROM dbo.Employees WHERE Id=@EmployeeId AND IsActive=1)
   THROW 51022,'Colaborador inexistente o inactivo.',1;
  IF NOT EXISTS(SELECT 1 FROM dbo.AppUsers WHERE Id=@ActorUserId AND IsActive=1)
   THROW 51008,'Usuario invalido.',1;
  IF EXISTS(SELECT 1 FROM dbo.AssetAssignments WHERE AssetId=@AssetId AND ReturnedAt IS NULL)
   THROW 51023,'El activo ya tiene asignacion activa.',1;
  SET @Now=SYSUTCDATETIME();
  INSERT dbo.AssetAssignments(AssetId,EmployeeId,AssignedAt,AssignedByUserId)
   VALUES (@AssetId,@EmployeeId,@Now,@ActorUserId);
  SET @AssignmentId=CONVERT(bigint,SCOPE_IDENTITY());
  UPDATE dbo.Assets SET Status='Asignado',UpdatedAt=@Now WHERE Id=@AssetId;
  INSERT dbo.AssetMovements(AssetId,AssignmentId,MovementType,PreviousStatus,NewStatus,PerformedByUserId,OccurredAt,Observations)
   VALUES (@AssetId,@AssignmentId,'Asignacion','Disponible','Asignado',@ActorUserId,@Now,@Observations);
  COMMIT;
 END TRY BEGIN CATCH
  IF @@TRANCOUNT>0 ROLLBACK;
  THROW;
 END CATCH
END;
GO

CREATE PROCEDURE dbo.usp_Assets_Return
 @AssetId bigint,@ReturnCondition nvarchar(300),@ActorUserId bigint,
 @Observations nvarchar(500)=NULL,@AssignmentId bigint OUTPUT
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 DECLARE @Status varchar(20),@Now datetime2(3);
 SET @ReturnCondition=NULLIF(TRIM(@ReturnCondition),'');
 IF @ReturnCondition IS NULL THROW 51024,'Condicion de devolucion obligatoria.',1;
 BEGIN TRY
  BEGIN TRAN;
  SELECT @Status=Status FROM dbo.Assets WITH(UPDLOCK,HOLDLOCK) WHERE Id=@AssetId;
  IF @Status IS NULL THROW 51020,'Activo inexistente.',1;
  SELECT @AssignmentId=Id FROM dbo.AssetAssignments WHERE AssetId=@AssetId AND ReturnedAt IS NULL;
  IF @AssignmentId IS NULL OR @Status<>'Asignado' THROW 51025,'No existe asignacion activa valida.',1;
  IF NOT EXISTS(SELECT 1 FROM dbo.AppUsers WHERE Id=@ActorUserId AND IsActive=1) THROW 51008,'Usuario invalido.',1;
  SET @Now=SYSUTCDATETIME();
  UPDATE dbo.AssetAssignments SET ReturnedAt=@Now,ReturnCondition=@ReturnCondition,ReturnedByUserId=@ActorUserId WHERE Id=@AssignmentId;
  UPDATE dbo.Assets SET Status='Disponible',UpdatedAt=@Now WHERE Id=@AssetId;
  INSERT dbo.AssetMovements(AssetId,AssignmentId,MovementType,PreviousStatus,NewStatus,PerformedByUserId,OccurredAt,Observations)
   VALUES (@AssetId,@AssignmentId,'Devolucion','Asignado','Disponible',@ActorUserId,@Now,@Observations);
  COMMIT;
 END TRY BEGIN CATCH
  IF @@TRANCOUNT>0 ROLLBACK;
  THROW;
 END CATCH
END;
GO

CREATE PROCEDURE dbo.usp_Assets_List
 @Search nvarchar(100)=NULL,@Status varchar(20)=NULL,@Category nvarchar(60)=NULL,
 @Page int=1,@PageSize int=20
AS
BEGIN
 SET NOCOUNT ON;
 IF @Page<1 OR @PageSize<1 OR @PageSize>100 OR @Page IS NULL OR @PageSize IS NULL
  THROW 51030,'Paginacion invalida.',1;
 IF @Status IS NOT NULL AND @Status NOT IN ('Disponible','Asignado','Mantenimiento','Retirado') THROW 51013,'Estado invalido.',1;
 SET @Search=NULLIF(TRIM(@Search),''); SET @Category=NULLIF(TRIM(@Category),'');
 IF @Search IS NOT NULL AND LEN(@Search)>100 THROW 51031,'Busqueda demasiado larga.',1;
 IF @Category IS NOT NULL AND LEN(@Category)>60 THROW 51032,'Categoria demasiado larga.',1;
 DECLARE @Offset bigint=(CONVERT(bigint,@Page)-1)*@PageSize;
 SELECT a.Id,a.AssetCode,a.SerialNumber,a.Category,a.Brand,a.Model,a.OwnershipType,
  a.SupplierId,a.Status,a.CurrentLocation,a.PurchaseDate,a.RentalEndDate,a.CreatedAt,a.UpdatedAt,
  aa.EmployeeId AS AssignedEmployeeId,e.FullName AS AssignedEmployeeName
 FROM dbo.Assets a
 LEFT JOIN dbo.AssetAssignments aa ON aa.AssetId=a.Id AND aa.ReturnedAt IS NULL
 LEFT JOIN dbo.Employees e ON e.Id=aa.EmployeeId
 WHERE (@Status IS NULL OR a.Status=@Status) AND (@Category IS NULL OR a.Category=@Category)
  AND (@Search IS NULL OR a.AssetCode LIKE '%'+@Search+'%' OR a.SerialNumber LIKE '%'+@Search+'%'
   OR a.Brand LIKE '%'+@Search+'%' OR a.Model LIKE '%'+@Search+'%')
 ORDER BY a.Id OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
 SELECT COUNT_BIG(*) AS TotalCount FROM dbo.Assets a
 WHERE (@Status IS NULL OR a.Status=@Status) AND (@Category IS NULL OR a.Category=@Category)
  AND (@Search IS NULL OR a.AssetCode LIKE '%'+@Search+'%' OR a.SerialNumber LIKE '%'+@Search+'%'
   OR a.Brand LIKE '%'+@Search+'%' OR a.Model LIKE '%'+@Search+'%');
END;
GO

CREATE PROCEDURE dbo.usp_Assets_History @AssetId bigint
AS
BEGIN
 SET NOCOUNT ON;
 IF NOT EXISTS(SELECT 1 FROM dbo.Assets WHERE Id=@AssetId) THROW 51020,'Activo inexistente.',1;

 SELECT m.Id,m.AssetId,m.AssignmentId,m.MovementType,m.PreviousStatus,m.NewStatus,
  m.PreviousLocation,m.NewLocation,m.PerformedByUserId,u.UserName,m.OccurredAt,m.Observations,
  aa.EmployeeId,aa.ReturnCondition
 FROM dbo.AssetMovements m
 JOIN dbo.AppUsers u ON u.Id=m.PerformedByUserId
 LEFT JOIN dbo.AssetAssignments aa ON aa.Id=m.AssignmentId
 WHERE m.AssetId=@AssetId ORDER BY m.OccurredAt,m.Id;
END;
GO
