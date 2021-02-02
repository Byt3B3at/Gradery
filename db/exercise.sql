DROP TABLE IF EXISTS Schulnote;
DROP TABLE IF EXISTS Kurs;
DROP TABLE IF EXISTS Schueler;
DROP TABLE IF EXISTS Zeitraum;
DROP TABLE IF EXISTS Lehrer;
DROP TABLE IF EXISTS Person;
DROP TABLE IF EXISTS Termin;
DROP TABLE IF EXISTS Schulnotenart;
DROP TABLE IF EXISTS Schulfach;


CREATE TABLE Schulfach (
  SchulfachID INTEGER  NOT NULL  ,
  Schulfachname TEXT    ,
  Schulfachtyp TEXT      ,
PRIMARY KEY(SchulfachID));

CREATE TABLE Schulnotenart (
  SchulnotenartID INTEGER  NOT NULL  ,
  Schulnotentyp TEXT      ,
PRIMARY KEY(SchulnotenartID));

CREATE TABLE Termin (
  TerminID INTEGER  NOT NULL  ,
  Termintyp TEXT    ,
  Zeitpunkt DATE      ,
PRIMARY KEY(TerminID));

CREATE TABLE Person (
  PersonID INTEGER  NOT NULL  ,
  Vorname TEXT    ,
  Nachname TEXT      ,
PRIMARY KEY(PersonID));

CREATE TABLE Lehrer (
  LehrerID INTEGER  NOT NULL  ,
  PersonID INTEGER  NOT NULL    ,
PRIMARY KEY(LehrerID),
  FOREIGN KEY(PersonID)
    REFERENCES Person(PersonID)
      ON DELETE NO ACTION
      ON UPDATE NO ACTION);

CREATE TABLE Zeitraum (
  ZeitraumID INTEGER  NOT NULL  ,
  TerminID INTEGER  NOT NULL  ,
  Zeitraumendpunkt DATE      ,
PRIMARY KEY(ZeitraumID),
  FOREIGN KEY(TerminID)
    REFERENCES Termin(TerminID)
      ON DELETE NO ACTION
      ON UPDATE NO ACTION);

CREATE TABLE Schueler (
  SchuelerID INTEGER  NOT NULL  ,
  PersonID INTEGER  NOT NULL    ,
PRIMARY KEY(SchuelerID),
  FOREIGN KEY(PersonID)
    REFERENCES Person(PersonID)
      ON DELETE NO ACTION
      ON UPDATE NO ACTION);

CREATE TABLE Kurs (
  LehrerID INTEGER  NOT NULL  ,
  SchulfachID INTEGER  NOT NULL  ,
  ZeitraumID INTEGER  NOT NULL  ,
  TerminID INTEGER  NOT NULL    ,
PRIMARY KEY(LehrerID, SchulfachID),
  FOREIGN KEY(LehrerID)
    REFERENCES Lehrer(LehrerID)
      ON DELETE NO ACTION
      ON UPDATE NO ACTION,
  FOREIGN KEY(SchulfachID)
    REFERENCES Schulfach(SchulfachID)
      ON DELETE NO ACTION
      ON UPDATE NO ACTION,
  FOREIGN KEY(ZeitraumID)
    REFERENCES Zeitraum(ZeitraumID)
      ON DELETE NO ACTION
      ON UPDATE NO ACTION,
  FOREIGN KEY(TerminID)
    REFERENCES Termin(TerminID)
      ON DELETE NO ACTION
      ON UPDATE NO ACTION);

CREATE TABLE Schulnote (
  SchulnoteID INTEGER  NOT NULL  ,
  SchulfachID INTEGER  NOT NULL  ,
  LehrerID INTEGER  NOT NULL  ,
  SchuelerID INTEGER  NOT NULL  ,
  SchulnotenartID INTEGER  NOT NULL  ,
  Schulnotenwert TEXT      ,
PRIMARY KEY(SchulnoteID),
  FOREIGN KEY(SchulnotenartID)
    REFERENCES Schulnotenart(SchulnotenartID)
      ON DELETE NO ACTION
      ON UPDATE NO ACTION,
  FOREIGN KEY(SchuelerID)
    REFERENCES Schueler(SchuelerID)
      ON DELETE NO ACTION
      ON UPDATE NO ACTION,
  FOREIGN KEY(LehrerID, SchulfachID)
    REFERENCES Kurs(LehrerID, SchulfachID)
      ON DELETE NO ACTION
      ON UPDATE NO ACTION);


--Schulnotenart: SchulnotenartID, Schulnotentyp
INSERT INTO Schulnotenart VALUES(null, 'Klausur');
INSERT INTO Schulnotenart VALUES(null, 'SoMi');

--Termin: TerminID, Termintyp, Zeitpunkt
INSERT INTO Termin VALUES(null, 'Semesterbeginn', date('2020-02-01'));
INSERT INTO Termin VALUES(null, 'Einschreibefrist', date('2020-01-01'));
INSERT INTO Termin VALUES(null, 'Klausur', date('2020-04-23'));

--Schulfach: SchulfachID, Schulfachname, Schulfachtyp
INSERT INTO Schulfach VALUES(null, 'AWE', 'Pflichtfach');
INSERT INTO Schulfach VALUES(null, 'DB', 'Pflichtfach');
INSERT INTO Schulfach VALUES(null, 'DK', 'Pflichtfach');
INSERT INTO Schulfach VALUES(null, 'ENG', 'Pflichtfach');
INSERT INTO Schulfach VALUES(null, 'PG', 'Pflichtfach');
INSERT INTO Schulfach VALUES(null, 'WGP', 'Pflichtfach');

--Person: PersonID, Vorname, Nachname
INSERT INTO Person VALUES(null, 'Max', 'Mustermann');
INSERT INTO Person VALUES(null, 'Eva', 'Musterfrau');
INSERT INTO Person VALUES(null, 'Frau', 'KAM');
INSERT INTO Person VALUES(null, 'Herr', 'SAR');

--Zeitraum: ZeitraumID, TerminID, Zeitraumendpunkt
INSERT INTO Semester VALUES(null, 1, date('2020-07-31'));

--Lehrer: LehrerID, PersonID
INSERT INTO Lehrer VALUES(null, 3);
INSERT INTO Lehrer VALUES(null, 4);

--Schueler: SchuelerID, PersonID
INSERT INTO Schueler VALUES(null, 1);
INSERT INTO Schueler VALUES(null, 2);

--Kurs: LehrerID, SchulfachID, SemesterID, TerminID
INSERT INTO Kurs VALUES(1, 6, 1, 1);
--Die Verdrahtung einer evtl. Klausur-Terminierung würde "übers Ziel hinausschießen".
--INSERT INTO Kurs VALUES(1, 6, 1, 3);
INSERT INTO Kurs VALUES(2, 1, 1, 2);

--Schulnote: SchulnoteID, SchulfachID, LehrerID, SchuelerID, SchulnotenartID, Schulnotenwert
INSERT INTO Schulnote VALUES(null, 1, 1, 1, 1, '1');
INSERT INTO Schulnote VALUES(null, 1, 1, 1, 1, '2');
INSERT INTO Schulnote VALUES(null, 2, 6, 2, 2, '3+');

--SELECT Zeitpunkt, Termintyp, Schulfachname, Schulfachtyp
--FROM   Unterrichtsfach NATURAL JOIN Lehrer NATURAL JOIN Schulfach NATURAL JOIN Semester NATURAL JOIN Termin;

SELECT Vorname, Nachname, Schulfachname, Schulnotentyp, Schulnotenwert
FROM   Schueler NATURAL JOIN Person NATURAL JOIN Schulfach NATURAL JOIN Schulnote NATURAL JOIN Schulnotenart;