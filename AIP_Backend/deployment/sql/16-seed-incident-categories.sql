-- Migration 16: Seed IncidentCategories into LookupTables
-- Idempotent: safe to re-run.
-- Behavior:
--   1) Inserts missing categories
--   2) Reactivates and updates existing categories
--   3) Optionally deactivates old categories no longer in the list

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
	BEGIN TRANSACTION;

	DECLARE @seedUserId NVARCHAR(450);
	DECLARE @category NVARCHAR(50) = 'IncidentCategories';
	DECLARE @deactivateRemoved BIT = 0; -- Set to 1 to deactivate records not in the desired list

	SELECT TOP 1 @seedUserId = Id
	FROM AspNetUsers
	ORDER BY Id;

	IF @seedUserId IS NULL
	BEGIN
		RAISERROR('No users found in AspNetUsers. Seed at least one user before running this script.', 16, 1);
		ROLLBACK TRANSACTION;
		RETURN;
	END;

	DECLARE @incidentCategories TABLE (
		Value NVARCHAR(100) NOT NULL,
		Description NVARCHAR(255) NOT NULL,
		Code NVARCHAR(50) NOT NULL,
		SortOrder INT NOT NULL
	);

	INSERT INTO @incidentCategories (Value, Description, Code, SortOrder)
	VALUES
		('Shoplifting / Theft', 'Incident Category: Shoplifting / Theft', 'SHOPLIFTING_THEFT', 0),
		('Fraud (Refund / Price Switching / Barcode Abuse)', 'Incident Category: Fraud (Refund / Price Switching / Barcode Abuse)', 'FRAUD_REFUND_PRICE_BARCODE', 1),
		('Abusive Behaviour', 'Incident Category: Abusive Behaviour', 'ABUSIVE_BEHAVIOUR', 2),
		('Threats and Intimidation', 'Incident Category: Threats and Intimidation', 'THREATS_INTIMIDATION', 3),
		('Violent Behaviour', 'Incident Category: Violent Behaviour', 'VIOLENT_BEHAVIOUR', 4),
		('Ban from Store / Trespassing', 'Incident Category: Ban from Store / Trespassing', 'BAN_TRESPASSING', 5),
		('Self-Scan / Scan & Go Misuse', 'Incident Category: Self-Scan / Scan & Go Misuse', 'SELFSCAN_SCANGO_MISUSE', 6),
		('Police Related (Attended / Failed to Attend)', 'Incident Category: Police Related (Attended / Failed to Attend)', 'POLICE_RELATED', 7),
		('Internal Theft', 'Incident Category: Internal Theft', 'INTERNAL_THEFT', 8),
		('Injury / Medical Emergency', 'Incident Category: Injury / Medical Emergency', 'INJURY_MEDICAL_EMERGENCY', 9),
		('Others', 'Incident Category: Others', 'OTHERS', 10);

	;MERGE LookupTables AS target
	USING (
		SELECT
			@category AS Category,
			Value,
			Description,
			Code,
			SortOrder
		FROM @incidentCategories
	) AS source
	ON target.Category = source.Category
	   AND target.Value = source.Value
	WHEN MATCHED THEN
		UPDATE SET
			target.Description = source.Description,
			target.Code = source.Code,
			target.SortOrder = source.SortOrder,
			target.IsActive = 1,
			target.RecordIsDeletedYN = 0,
			target.UpdatedBy = @seedUserId,
			target.UpdatedAt = GETUTCDATE()
	WHEN NOT MATCHED BY TARGET THEN
		INSERT (
			Category,
			Value,
			Description,
			Code,
			SortOrder,
			IsActive,
			RecordIsDeletedYN,
			CreatedBy,
			CreatedAt,
			UpdatedBy,
			UpdatedAt
		)
		VALUES (
			source.Category,
			source.Value,
			source.Description,
			source.Code,
			source.SortOrder,
			1,
			0,
			@seedUserId,
			GETUTCDATE(),
			@seedUserId,
			GETUTCDATE()
		);

	IF (@deactivateRemoved = 1)
	BEGIN
		UPDATE lt
		SET
			lt.IsActive = 0,
			lt.RecordIsDeletedYN = 1,
			lt.UpdatedBy = @seedUserId,
			lt.UpdatedAt = GETUTCDATE()
		FROM LookupTables lt
		WHERE lt.Category = @category
		  AND NOT EXISTS (
			  SELECT 1
			  FROM @incidentCategories c
			  WHERE c.Value = lt.Value
		  );
	END;

	COMMIT TRANSACTION;

	PRINT 'IncidentCategories seeded successfully.';
	PRINT 'Set @deactivateRemoved = 1 if you also want to deactivate old categories not in the new list.';
END TRY
BEGIN CATCH
	IF @@TRANCOUNT > 0
		ROLLBACK TRANSACTION;

	DECLARE @errorMessage NVARCHAR(4000) = ERROR_MESSAGE();
	DECLARE @errorNumber INT = ERROR_NUMBER();
	DECLARE @errorLine INT = ERROR_LINE();

	RAISERROR('Migration 16 failed (Error %d, Line %d): %s', 16, 1, @errorNumber, @errorLine, @errorMessage);
END CATCH;

