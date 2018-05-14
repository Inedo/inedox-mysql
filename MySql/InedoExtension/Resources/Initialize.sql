CREATE TABLE __BuildMaster_DbSchemaChanges (
  Numeric_Release_Number BIGINT NOT NULL,
  Script_Id INT NOT NULL,
  Script_Name VARCHAR(50) CHARACTER SET UTF8 NOT NULL,
  Executed_Date DATETIME NOT NULL,
  Success_Indicator CHAR(1) NOT NULL,

  CONSTRAINT __BuildMaster_DbSchemaChangesPK
	PRIMARY KEY (Numeric_Release_Number, Script_Id)
)
;

INSERT INTO __BuildMaster_DbSchemaChanges
	(Numeric_Release_Number, Script_Id, Script_Name, Executed_Date, Success_Indicator)
VALUES
	(0, 0, 'CREATE TABLE __BuildMaster_DbSchemaChanges', NOW(), 'Y')
;