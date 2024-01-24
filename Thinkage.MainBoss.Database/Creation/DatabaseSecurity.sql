-- We use the single database principal called MainBoss (which will have been created at time of database creation)
-- to which we grant the necessary permissions to do MainBoss functions
-- A database administrator can add appropriate users/groups to this principal to allow them to use mainboss [in addition to whatever
-- they need to do to let them connect to the database
grant alter any schema, create function, create procedure, select, insert, delete, update, backup database, execute to [MainBoss]