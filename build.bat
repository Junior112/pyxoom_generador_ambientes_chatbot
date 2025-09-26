@echo off
echo Compilando PyxoomInstanceGenerator...

dotnet build --configuration Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ Compilación exitosa!
    echo.
    echo Para ejecutar el generador:
    echo   PyxoomInstanceGenerator\bin\Release\net8.0\PyxoomInstanceGenerator.exe
    echo.
    echo O ejecuta directamente:
    echo   dotnet run --project PyxoomInstanceGenerator
) else (
    echo.
    echo ❌ Error en la compilación
)

pause

