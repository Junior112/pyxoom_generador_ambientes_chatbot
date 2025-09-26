using PyxoomInstanceGenerator.Models;

namespace PyxoomInstanceGenerator.Services
{
    public class FileManager
    {
        private readonly string _sourcePath;
        private readonly string _outputBasePath;

        public FileManager(string sourcePath, string outputBasePath)
        {
            _sourcePath = sourcePath;
            _outputBasePath = outputBasePath;
        }

        public async Task CopyCompiledFilesAsync(InstanceConfiguration instance, List<string> additionalFiles, List<string> excludeFiles)
        {
            var instancePath = Path.Combine(_outputBasePath, instance.FolderName);
            
            // Crear directorio de la instancia si no existe
            Directory.CreateDirectory(instancePath);

            Console.WriteLine($"Copiando archivos para la instancia: {instance.InstanceName}");

            try
            {
                // Copiar todo el contenido de la carpeta fuente respetando exclusiones
                await CopyAllFromSourceAsync(instancePath, excludeFiles);

                Console.WriteLine($"Archivos copiados exitosamente para {instance.InstanceName}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error copiando archivos para la instancia {instance.InstanceName}: {ex.Message}", ex);
            }
        }

        private async Task CopyAllFromSourceAsync(string instancePath, List<string> excludeFiles)
        {
            if (!Directory.Exists(_sourcePath))
            {
                Console.WriteLine($"  ⚠ Directorio fuente no existe: {_sourcePath}");
                return;
            }

            // Copiar todos los archivos en la raíz del source
            var files = Directory.GetFiles(_sourcePath, "*", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                var relative = Path.GetFileName(file);
                if (ShouldExclude(relative, excludeFiles))
                {
                    Console.WriteLine($"  ⏭ Excluido: {relative}");
                    continue;
                }

                var destFile = Path.Combine(instancePath, relative);
                await CopyFileAsync(file, destFile);
                Console.WriteLine($"  ✓ Copiado: {relative}");
            }

            // Copiar directorios recursivamente
            var directories = Directory.GetDirectories(_sourcePath, "*", SearchOption.TopDirectoryOnly);
            foreach (var dir in directories)
            {
                var dirName = Path.GetFileName(dir);
                if (ShouldExclude(dirName, excludeFiles))
                {
                    Console.WriteLine($"  ⏭ Excluido directorio: {dirName}");
                    continue;
                }

                var destSubDir = Path.Combine(instancePath, dirName);
                await CopyDirectoryWithExclusionsAsync(dir, destSubDir, excludeFiles);
                Console.WriteLine($"  ✓ Copiado directorio: {dirName}/");
            }
        }

        private async Task CopyDirectoryWithExclusionsAsync(string sourceDir, string destDir, List<string> excludeFiles)
        {
            Directory.CreateDirectory(destDir);

            // Copiar archivos de este nivel
            var files = Directory.GetFiles(sourceDir, "*", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                var relative = Path.GetFileName(file);
                if (ShouldExclude(relative, excludeFiles))
                {
                    continue;
                }
                var destFile = Path.Combine(destDir, relative);
                await CopyFileAsync(file, destFile);
            }

            // Recurse en subdirectorios
            var directories = Directory.GetDirectories(sourceDir, "*", SearchOption.TopDirectoryOnly);
            foreach (var dir in directories)
            {
                var dirName = Path.GetFileName(dir);
                if (ShouldExclude(dirName, excludeFiles))
                {
                    continue;
                }
                var destSubDir = Path.Combine(destDir, dirName);
                await CopyDirectoryWithExclusionsAsync(dir, destSubDir, excludeFiles);
            }
        }

        private bool ShouldExclude(string relativePathOrName, List<string> excludeFiles)
        {
            if (excludeFiles == null || excludeFiles.Count == 0)
                return false;

            var normalized = relativePathOrName.Replace('\\', '/');
            foreach (var pattern in excludeFiles)
            {
                var p = (pattern ?? string.Empty).Replace('\\', '/');
                if (string.IsNullOrWhiteSpace(p)) continue;

                // Coincidencia por nombre exacto
                if (string.Equals(Path.GetFileName(normalized), Path.GetFileName(p), StringComparison.OrdinalIgnoreCase))
                    return true;

                // Coincidencia por sufijo de ruta
                if (normalized.EndsWith(p, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private async Task CopyAdditionalFilesAsync(string instancePath, List<string> additionalFiles)
        {
            if (additionalFiles == null || !additionalFiles.Any())
                return;

            foreach (var file in additionalFiles)
            {
                var sourceFile = Path.Combine(_sourcePath, file);
                var destFile = Path.Combine(instancePath, file);

                if (File.Exists(sourceFile))
                {
                    await CopyFileAsync(sourceFile, destFile);
                    Console.WriteLine($"  ✓ Copiado adicional: {file}");
                }
                else if (Directory.Exists(sourceFile))
                {
                    var destDir = Path.Combine(instancePath, Path.GetFileName(file));
                    await CopyDirectoryAsync(sourceFile, destDir);
                    Console.WriteLine($"  ✓ Copiado directorio adicional: {file}");
                }
                else
                {
                    Console.WriteLine($"  ⚠ No encontrado (adicional): {file}");
                }
            }
        }

        private async Task CopyFileAsync(string sourceFile, string destFile)
        {
            var destDir = Path.GetDirectoryName(destFile);
            if (!string.IsNullOrEmpty(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            // Usar File.Copy para archivos binarios y File.ReadAllTextAsync para archivos de texto
            var extension = Path.GetExtension(sourceFile).ToLower();
            
            if (extension == ".json" || extension == ".txt" || extension == ".htm" || extension == ".html" || extension == ".bat" || extension == ".ps1")
            {
                // Archivos de texto - usar async
                var content = await File.ReadAllTextAsync(sourceFile);
                await File.WriteAllTextAsync(destFile, content);
            }
            else
            {
                // Archivos binarios - usar File.Copy
                File.Copy(sourceFile, destFile, overwrite: true);
            }
        }

        private async Task CopyDirectoryAsync(string sourceDir, string destDir)
        {
            if (!Directory.Exists(sourceDir))
                return;

            Directory.CreateDirectory(destDir);

            var files = Directory.GetFiles(sourceDir);
            var directories = Directory.GetDirectories(sourceDir);

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(destDir, fileName);
                await CopyFileAsync(file, destFile);
            }

            foreach (var dir in directories)
            {
                var dirName = Path.GetFileName(dir);
                var destSubDir = Path.Combine(destDir, dirName);
                await CopyDirectoryAsync(dir, destSubDir);
            }
        }

        public async Task SaveAppSettingsAsync(string instancePath, string appSettingsContent, string fileName = "appsettings.json")
        {
            var appSettingsPath = Path.Combine(instancePath, fileName);
            await File.WriteAllTextAsync(appSettingsPath, appSettingsContent);
            Console.WriteLine($"  ✓ Generado: {fileName}");
        }

        public async Task CreateStartupScriptsAsync(string instancePath, InstanceConfiguration instance)
        {
            // Crear script de inicio personalizado para la instancia
            var startScript = $@"@echo off
echo Iniciando Pyxoom-Rabbit - Instancia: {instance.InstanceName}
echo Descripción: {instance.Description}
echo.
echo Configuración:
echo - Client ID: {instance.ClientId}
echo - Empresa ID: {instance.EmpresaId}
echo - RabbitMQ Channel: {instance.RabbitMQChannel}
echo - Log Path: {instance.LogPath}
echo.
echo Presiona cualquier tecla para continuar...
pause > nul

set QUEUE_NAME={instance.RabbitMQChannel}
Pyxoom-Rabbit.exe

echo.
echo La aplicación se ha cerrado.
pause
";

            var startScriptPath = Path.Combine(instancePath, $"start-{instance.FolderName}.bat");
            await File.WriteAllTextAsync(startScriptPath, startScript);
            Console.WriteLine($"  ✓ Generado script de inicio: start-{instance.FolderName}.bat");

            // Crear script de PowerShell también
            var psScript = $@"# Script de inicio para Pyxoom-Rabbit - Instancia: {instance.InstanceName}
# Descripción: {instance.Description}

Write-Host ""Iniciando Pyxoom-Rabbit - Instancia: {instance.InstanceName}"" -ForegroundColor Green
Write-Host ""Descripción: {instance.Description}"" -ForegroundColor Yellow
Write-Host """"
Write-Host ""Configuración:"" -ForegroundColor Cyan
Write-Host ""- Client ID: {instance.ClientId}"" -ForegroundColor White
Write-Host ""- Empresa ID: {instance.EmpresaId}"" -ForegroundColor White
Write-Host ""- RabbitMQ Channel: {instance.RabbitMQChannel}"" -ForegroundColor White
Write-Host ""- Log Path: {instance.LogPath}"" -ForegroundColor White
Write-Host """"

$env:QUEUE_NAME = ""{instance.RabbitMQChannel}""
& "".\Pyxoom-Rabbit.exe""

Write-Host """"
Write-Host ""La aplicación se ha cerrado."" -ForegroundColor Red
Read-Host ""Presiona Enter para salir""
";

            var psScriptPath = Path.Combine(instancePath, $"start-{instance.FolderName}.ps1");
            await File.WriteAllTextAsync(psScriptPath, psScript);
            Console.WriteLine($"  ✓ Generado script PowerShell: start-{instance.FolderName}.ps1");
        }
    }
}
