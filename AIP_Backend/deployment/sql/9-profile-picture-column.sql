-- Ensure ProfilePicture column supports large base64 images (~1.5MB)
-- Run this if profile picture save returns 500 (e.g. string overflow)
-- Idempotent: safe to run multiple times

IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'ProfilePicture')
BEGIN
    DECLARE @CurrentType NVARCHAR(128)
    SELECT @CurrentType = DATA_TYPE + 
        CASE WHEN CHARACTER_MAXIMUM_LENGTH = -1 THEN '(max)' 
             WHEN CHARACTER_MAXIMUM_LENGTH IS NOT NULL THEN '(' + CAST(CHARACTER_MAXIMUM_LENGTH AS VARCHAR) + ')' 
             ELSE '' END
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'ProfilePicture'
    
    IF @CurrentType <> 'nvarchar(max)'
    BEGIN
        ALTER TABLE AspNetUsers ALTER COLUMN ProfilePicture NVARCHAR(MAX) NULL
        PRINT 'ProfilePicture column altered to nvarchar(max)'
    END
    ELSE
        PRINT 'ProfilePicture already nvarchar(max)'
END
ELSE
    PRINT 'ProfilePicture column does not exist - run EF migrations first'
