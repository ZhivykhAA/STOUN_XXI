DROP PROCEDURE IF EXISTS proc1;

DELIMITER $$

CREATE PROCEDURE proc1(fname varchar(45), fdate date)
READS SQL DATA 
BEGIN
 SET @dat = CURDATE();
 IF @dat < fdate THEN
  SELECT "The required day has not yet arrived!";
 ELSE 
  SET @fl = 0;
  SELECT COUNT(fdate) INTO @fl FROM rate WHERE date = fdate;
  IF @fl = 1 THEN
   SET @fid = "";
   SELECT ID INTO @fid FROM guide WHERE name = fname;
   SET @q = CONCAT("select ", @fid, " from rate where date = \'", fdate,"\'");
   PREPARE st FROM @q;
   EXECUTE st;
   DEALLOCATE PREPARE st;
  ELSEIF @fl = 0 THEN
   SELECT "There is no such date in the database!";
  END IF;
 END IF;
END $$

DELIMITER ;

CALL proc1('Болгарский лев', "2021-02-11");

