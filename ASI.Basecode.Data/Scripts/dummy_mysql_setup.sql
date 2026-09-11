-- Dummy local MySQL database for the MVC sample (React -> MovieController -> MovieService -> MovieRepository -> MySQL)
-- Run with:  mysql -u root -p < ASI.Basecode.Data/Scripts/dummy_mysql_setup.sql

CREATE DATABASE IF NOT EXISTS asibasecodedb
  CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;

USE asibasecodedb;

CREATE TABLE IF NOT EXISTS Movies (
  Id          INT AUTO_INCREMENT PRIMARY KEY,
  Title       VARCHAR(150) NOT NULL,
  Genre       VARCHAR(50)  NULL,
  ReleaseYear INT          NOT NULL DEFAULT 0,
  Watched     TINYINT(1)   NOT NULL DEFAULT 0,
  CreatedTime DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UpdatedTime DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS Users (
  Id          INT AUTO_INCREMENT PRIMARY KEY,
  UserId      VARCHAR(50) NOT NULL,
  Name        VARCHAR(50) NOT NULL,
  Password    VARCHAR(50) NOT NULL,
  CreatedBy   VARCHAR(50) NOT NULL,
  CreatedTime DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UpdatedBy   VARCHAR(50) NOT NULL,
  UpdatedTime DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE KEY UQ__Users__1788CC4D5F4A160F (UserId)
);

INSERT INTO Movies (Title, Genre, ReleaseYear, Watched) VALUES
  ('Inception',    'Sci-Fi',    2010, 1),
  ('The Matrix',   'Sci-Fi',    1999, 1),
  ('Parasite',     'Thriller',  2019, 0),
  ('Spirited Away','Animation', 2001, 0);
