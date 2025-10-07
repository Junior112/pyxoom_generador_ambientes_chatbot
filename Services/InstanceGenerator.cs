using PyxoomInstanceGenerator.Models;

namespace PyxoomInstanceGenerator.Services
{
    public class InstanceGenerator
    {
        private readonly AppSettingsGenerator _appSettingsGenerator;
        private readonly FileManager _fileManager;
        private readonly GeneratorConfig _config;

        public InstanceGenerator(GeneratorConfig config)
        {
            _config = config;
            _appSettingsGenerator = new AppSettingsGenerator(config.BaseAppSettingsPath);
            _fileManager = new FileManager(config.SourcePath, config.OutputBasePath);
        }

        public async Task GenerateInstancesAsync()
        {
            Console.WriteLine("=== Generador de Instancias Pyxoom-Rabbit ===");
            Console.WriteLine($"Ruta fuente: {_config.SourcePath}");
            Console.WriteLine($"Ruta destino: {_config.OutputBasePath}");
            Console.WriteLine($"Total de instancias a generar: {_config.Instances.Count}");
            Console.WriteLine();

            // Validar configuración
            ValidateConfiguration();

            // Crear directorio base si no existe
            Directory.CreateDirectory(_config.OutputBasePath);

            var successCount = 0;
            var errorCount = 0;

            foreach (var instance in _config.Instances)
            {
                try
                {
                    Console.WriteLine($"\n--- Generando instancia: {instance.InstanceName} ---");
                    
                    // Copiar archivos compilados
                    await _fileManager.CopyCompiledFilesAsync(
                        instance, 
                        _config.CopyAdditionalFiles, 
                        _config.ExcludeFiles);

                    // Generar appsettings.json personalizado
                    var appSettingsContent = await _appSettingsGenerator.GenerateAppSettingsAsync(instance);
                    var instancePath = Path.Combine(_config.OutputBasePath, instance.FolderName);
                    await _fileManager.SaveAppSettingsAsync(instancePath, appSettingsContent);

                    // Crear scripts de inicio
                    await _fileManager.CreateStartupScriptsAsync(instancePath, instance);

                    Console.WriteLine($"✓ Instancia '{instance.InstanceName}' generada exitosamente");
                    successCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error generando instancia '{instance.InstanceName}': {ex.Message}");
                    errorCount++;
                }
            }

            // Generar archivo con comandos PM2 por instancia
            try
            {
                await GeneratePm2CommandsAsync();
                Console.WriteLine($"\n✓ Archivo de comandos PM2 generado en: {_config.OutputBasePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n⚠ No se pudieron generar los comandos PM2: {ex.Message}");
            }

            // Resumen final
            Console.WriteLine("\n=== Resumen de Generación ===");
            Console.WriteLine($"Instancias generadas exitosamente: {successCount}");
            Console.WriteLine($"Errores: {errorCount}");
            Console.WriteLine($"Total procesadas: {_config.Instances.Count}");

            if (successCount > 0)
            {
                Console.WriteLine($"\nInstancias disponibles en: {_config.OutputBasePath}");
                Console.WriteLine("\nPara ejecutar una instancia:");
                Console.WriteLine("  1. Navega a la carpeta de la instancia");
                Console.WriteLine("  2. Ejecuta el script de inicio correspondiente");
                Console.WriteLine("     - start-[nombre].bat (Windows CMD)");
                Console.WriteLine("     - start-[nombre].ps1 (PowerShell)");
            }
        }

        private async Task GeneratePm2CommandsAsync()
        {
            Directory.CreateDirectory(_config.OutputBasePath);

            var txtPath = Path.Combine(_config.OutputBasePath, "pm2-commands.txt");
            var batPath = Path.Combine(_config.OutputBasePath, "pm2-start-all.bat");
            var ps1Path = Path.Combine(_config.OutputBasePath, "pm2-start-all.ps1");

            var txtLines = new List<string>();
            var batLines = new List<string>();
            var ps1Lines = new List<string>();

            batLines.Add("@echo off");
            batLines.Add("echo Iniciando procesos con pm2...");
            batLines.Add("");
            batLines.Add("REM Verificar que PM2 esté instalado");
            batLines.Add("pm2 --version >nul 2>&1");
            batLines.Add("if errorlevel 1 (");
            batLines.Add("    echo ERROR: PM2 no está instalado o no está en el PATH");
            batLines.Add("    echo Instala PM2 con: npm install -g pm2");
            batLines.Add("    pause");
            batLines.Add("    exit /b 1");
            batLines.Add(")");
            batLines.Add("");

            ps1Lines.Add("# Inicia procesos con pm2 para cada instancia");
            ps1Lines.Add("Write-Host \"Iniciando procesos con pm2...\" -ForegroundColor Green");
            ps1Lines.Add("");

            foreach (var instance in _config.Instances)
            {
                var instancePath = Path.Combine(_config.OutputBasePath, instance.FolderName);
                // Usar ruta absoluta y normalizar para evitar mezcla de "./" con "\\"
                var exePathAbs = Path.GetFullPath(Path.Combine(instancePath, "Pyxoom-Rabbit.exe"));
                // pm2 acepta ambos separadores; mantenemos backslash en Windows para consistencia
                var exePathNormalized = exePathAbs.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

                // Envolver en comillas para rutas con espacios
                var exeQuoted = $"\"{exePathNormalized}\"";
                var nameQuoted = $"\"{instance.InstanceName}\"";

                var stopCmd = $"pm2 stop {nameQuoted}";
                var deleteCmd = $"pm2 delete {nameQuoted}";
                var startCmd = $"pm2 start {exeQuoted} --name {nameQuoted} --instances 4";

                // Archivo TXT (legible)
                txtLines.Add(stopCmd);
                txtLines.Add(deleteCmd);
                txtLines.Add(startCmd);

                // BAT: suprime salida de stop/delete para no ensuciar, pero muestra errores de start
                batLines.Add($"echo Procesando instancia: {instance.InstanceName}");
                batLines.Add($"{stopCmd} 1>nul 2>nul");
                batLines.Add($"{deleteCmd} 1>nul 2>nul");
                batLines.Add($"if not exist \"{exePathNormalized}\" (");
                batLines.Add($"    echo ERROR: No se encontró el ejecutable: {exePathNormalized}");
                batLines.Add($"    echo Verifica que la ruta sea correcta");
                batLines.Add($") else (");
                batLines.Add($"    {startCmd}");
                batLines.Add($")");
                batLines.Add("");

                // PS1: envía a Out-Null stop/delete
                ps1Lines.Add($"{stopCmd} | Out-Null");
                ps1Lines.Add($"{deleteCmd} | Out-Null");
                ps1Lines.Add(startCmd);
            }

            batLines.Add("");
            batLines.Add("echo.");
            batLines.Add("echo Proceso completado. Verifica el estado con: pm2 list");
            batLines.Add("echo.");
            batLines.Add("pause");

            ps1Lines.Add("");
            ps1Lines.Add("Write-Host \"Hecho.\" -ForegroundColor Cyan");

            await File.WriteAllLinesAsync(txtPath, txtLines);
            await File.WriteAllLinesAsync(batPath, batLines);
            await File.WriteAllLinesAsync(ps1Path, ps1Lines);
        }

        private void ValidateConfiguration()
        {
            if (string.IsNullOrEmpty(_config.SourcePath))
                throw new InvalidOperationException("SourcePath no puede estar vacío");

            if (string.IsNullOrEmpty(_config.OutputBasePath))
                throw new InvalidOperationException("OutputBasePath no puede estar vacío");

            if (string.IsNullOrEmpty(_config.BaseAppSettingsPath))
                throw new InvalidOperationException("BaseAppSettingsPath no puede estar vacío");

            if (!Directory.Exists(_config.SourcePath))
                throw new InvalidOperationException($"El directorio fuente no existe: {_config.SourcePath}");

            if (!File.Exists(_config.BaseAppSettingsPath))
                throw new InvalidOperationException($"El archivo base appsettings no existe: {_config.BaseAppSettingsPath}");

            if (_config.Instances == null || !_config.Instances.Any())
                throw new InvalidOperationException("No se han definido instancias para generar");

            // Validar instancias individuales
            var folderNames = new HashSet<string>();
            foreach (var instance in _config.Instances)
            {
                if (string.IsNullOrEmpty(instance.InstanceName))
                    throw new InvalidOperationException("InstanceName no puede estar vacío");

                if (string.IsNullOrEmpty(instance.FolderName))
                    throw new InvalidOperationException($"FolderName no puede estar vacío para la instancia: {instance.InstanceName}");

                if (folderNames.Contains(instance.FolderName))
                    throw new InvalidOperationException($"FolderName duplicado: {instance.FolderName}");

                folderNames.Add(instance.FolderName);
            }
        }
    }
}
