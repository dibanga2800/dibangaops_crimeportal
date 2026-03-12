-- Add offender verification fields to Incidents table
-- Safe to run once; adjust schema name if needed

IF COL_LENGTH('Incidents', 'OffenderDetailsVerified') IS NULL
BEGIN
	ALTER TABLE Incidents
	ADD OffenderDetailsVerified BIT NOT NULL CONSTRAINT DF_Incidents_OffenderDetailsVerified DEFAULT(0);
END

IF COL_LENGTH('Incidents', 'VerificationMethod') IS NULL
BEGIN
	ALTER TABLE Incidents
	ADD VerificationMethod NVARCHAR(100) NULL;
END

IF COL_LENGTH('Incidents', 'VerificationEvidenceImage') IS NULL
BEGIN
	ALTER TABLE Incidents
	ADD VerificationEvidenceImage NVARCHAR(MAX) NULL;
END
