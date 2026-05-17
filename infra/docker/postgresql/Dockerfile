FROM postgres:16-alpine

# SQL scripts in this folder run automatically on first container start.
COPY init/ /docker-entrypoint-initdb.d/
