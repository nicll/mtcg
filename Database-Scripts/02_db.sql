-- Database: mtcg

-- DROP DATABASE mtcg;

CREATE DATABASE mtcg
    WITH 
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'German_Austria.1252'
    LC_CTYPE = 'German_Austria.1252'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1;

GRANT ALL ON DATABASE mtcg TO postgres;

GRANT TEMPORARY, CONNECT ON DATABASE mtcg TO PUBLIC;

GRANT CONNECT ON DATABASE mtcg TO mtcg_user;
