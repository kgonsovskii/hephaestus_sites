-- psql -U postgres -d postgres -v ON_ERROR_STOP=1 -f setup-postgres.sql
-- Database: sites  Role: tss / 123
-- Deploy wipes all tracking data.

DO $$
BEGIN
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'tss') THEN
    CREATE ROLE tss LOGIN PASSWORD '123' CREATEDB;
  ELSE
    ALTER ROLE tss WITH PASSWORD '123';
  END IF;
END $$;

SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE datname = 'sites'
  AND pid <> pg_backend_pid();

DROP DATABASE IF EXISTS sites;

CREATE DATABASE sites
  OWNER tss
  ENCODING 'UTF8'
  TEMPLATE template0;

\connect sites

SET client_min_messages = WARNING;

CREATE TABLE track_visit (
  day        DATE NOT NULL,
  ip         VARCHAR(45) NOT NULL,
  flow       VARCHAR(100) NOT NULL DEFAULT '',
  site       VARCHAR(200) NOT NULL DEFAULT '',
  target1    VARCHAR(200) NOT NULL DEFAULT '',
  target2    VARCHAR(200) NOT NULL DEFAULT '',
  hit        BOOLEAN NOT NULL DEFAULT FALSE,
  video      BOOLEAN NOT NULL DEFAULT FALSE,
  play       BOOLEAN NOT NULL DEFAULT FALSE,
  goal       BOOLEAN NOT NULL DEFAULT FALSE,
  first_seen TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
  last_seen  TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
  goal_at    TIMESTAMP WITHOUT TIME ZONE,
  PRIMARY KEY (day, ip, flow, site)
);

ALTER SCHEMA public OWNER TO tss;
ALTER TABLE track_visit OWNER TO tss;
