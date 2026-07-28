@echo off
setlocal EnableDelayedExpansion
cd /d "%~dp0"

echo ============================================
echo  EMS - Start Docker stack
echo ============================================
echo  Starts: SQL Server (EMSDevDB + UserManagementDb),
echo           MongoDB, Redis, Mongo Express,
echo           usermanagement-api, ems-api, ems-web
echo.
echo  Then open: http://localhost:3000
echo ============================================
echo.

docker compose up -d --build
if errorlevel 1 (
  echo.
  echo ERROR: docker compose failed. Is Docker Desktop running?
  exit /b 1
)

echo.
echo Done. Containers are up.
echo   Web UI:              http://localhost:3000
echo   EMS.API:             http://localhost:5246
echo   User Management API: http://localhost:5137
echo   Mongo UI:            http://localhost:8081
echo   SQL Server:          localhost,1433  (sa / see docker-compose.yml)
echo.
pause
