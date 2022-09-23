#!/usr/bin/env bash
set -e

if [ "$1" = 'sql' ]; then
  if ! [[ -f /var/opt/mssql/.initialized ]]; 
  then
    ./wait-for-it.sh localhost:1433 -t 30 -- sleep 10 && echo "db is up" 

    echo "Creating $DB_NAME database..."
      
    #run the setup script to create the DB and the schema in the DB
    dotnet /dbex/Company.AppName.Database.dll all

    echo "Database scripts complete"
    touch /var/opt/mssql/.initialized
  fi &
  exec /opt/mssql/bin/sqlservr
fi

exec "$@"