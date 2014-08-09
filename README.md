DepthFirstSearchGame
====================

NOTE ABOUT DATABASE
(This information is available only in ServerClassLibrary/Database.cs and is duplicated here for documentation purposes)

The recommended database connector is available at the following link: http://dev.mysql.com/downloads/file.php?id=450594

Use of this game requires a MySQL database to be installed and accessible either on the development system or a separate server.
TODO: add database creation SQL script to the project repository.

When running the server for the first time, a config.ini file will be automatically created and populated with default values. The MySQL server name, a user account name, and a password are required at this time. This information is used to connect to the database. THe server will not start if the database connection can not be established.

The client will not launch if it is unable to connect to the server (hard-coded value in Game/AnglerGame.cs at this time)
