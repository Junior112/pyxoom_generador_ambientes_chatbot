@echo off
echo === Ejecutando Generador de Instancias Pyxoom-Rabbit ===
echo.

cd /d "%~dp0"

if not exist "instances-config.json" (
    echo ⚠️  No se encontró instances-config.json
    echo Se creará un archivo de ejemplo...
    echo.
)

echo Ejecutando generador...
echo.

dotnet run --project PyxoomInstanceGenerator

echo.
echo Presiona cualquier tecla para salir...
pause > nul






