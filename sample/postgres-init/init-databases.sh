#!/bin/bash
set -e

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    -- Create databases for OfX services
    CREATE DATABASE "OfXTestService1";
    CREATE DATABASE "OfXTestService2";
    CREATE DATABASE "OfXTestService3";
    CREATE DATABASE "OfXTestOtherService1";

    -- Grant privileges
    GRANT ALL PRIVILEGES ON DATABASE "OfXTestService1" TO $POSTGRES_USER;
    GRANT ALL PRIVILEGES ON DATABASE "OfXTestService2" TO $POSTGRES_USER;
    GRANT ALL PRIVILEGES ON DATABASE "OfXTestService3" TO $POSTGRES_USER;
    GRANT ALL PRIVILEGES ON DATABASE "OfXTestOtherService1" TO $POSTGRES_USER;
EOSQL

echo "All databases created successfully"
