using PyxoomInstanceGenerator.Models;
using PyxoomInstanceGenerator.Services;
using Newtonsoft.Json;

namespace PyxoomInstanceGenerator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("=== Generador de Instancias Pyxoom-Rabbit ===");
                Console.WriteLine("Iniciando proceso de generación...\n");

                // Determinar archivo de configuración
                var configFile = args.Length > 0 ? args[0] : "instances-config.json";
                
                if (!File.Exists(configFile))
                {
                    Console.WriteLine($"❌ Error: No se encontró el archivo de configuración: {configFile}");
                    Console.WriteLine("\nUso:");
                    Console.WriteLine("  PyxoomInstanceGenerator.exe [archivo-config.json]");
                    Console.WriteLine("\nSi no se especifica archivo, se buscará 'instances-config.json'");
                    
                    // Crear archivo de ejemplo si no existe
                    await CreateExampleConfigAsync();
                    return;
                }

                // Cargar configuración
                var configJson = await File.ReadAllTextAsync(configFile);
                var config = JsonConvert.DeserializeObject<GeneratorConfig>(configJson);

                if (config == null)
                {
                    Console.WriteLine($"❌ Error: No se pudo deserializar el archivo de configuración: {configFile}");
                    return;
                }

                // Generar instancias
                var generator = new InstanceGenerator(config);
                await generator.GenerateInstancesAsync();

                Console.WriteLine("\n✅ Proceso completado exitosamente!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Error fatal: {ex.Message}");
                Console.WriteLine($"Detalles: {ex}");
                Environment.Exit(1);
            }
        }

        private static async Task CreateExampleConfigAsync()
        {
            var exampleConfig = new GeneratorConfig
            {
                SourcePath = @"C:\Pyxoom-Rabbit\publish",
                OutputBasePath = @"C:\InstanciasPyxoom",
                BaseAppSettingsPath = @"C:\ruta\a\Pyxoom-Rabbit\appsettings.json",
                CopyAdditionalFiles = new List<string>
                {
                    "Content",
                    "start-app.bat",
                    "start-app.ps1"
                },
                ExcludeFiles = new List<string>
                {
                    "appsettings.json",
                    "appsettings.Development.json"
                },
                Instances = new List<InstanceConfiguration>
                {
                    new InstanceConfiguration
                    {
                        InstanceName = "Cliente 1 - Producción",
                        FolderName = "cliente1-prod",
                        ClientId = "1",
                        LogPath = @"C:\PyxoomLogs\Cliente1",
                        PyxoomInteractiveUrl = "https://cliente1.pyxoomdemo.com/Interactive42/Home/SignInToken",
                        PyxoomInteractivePublicPrivacy = "https://cliente1.pyxoomdemo.com/Interactive42/PublicPrivacy/Index",
                        PyxoomConnectionString = "Data Source=servidor1;Database=Pyxoom42_Cliente1;User ID=user1;Password=pass1;",
                        ExternalPyxoomServicesApiUrl = "https://api-cliente1.pyxoomdemo.com/",
                        RabbitMQChannel = "pyxoom_cliente1",
                        EmpresaId = 1,
                        Description = "Instancia de producción para Cliente 1",
                        CustomSettings = new Dictionary<string, string>
                        {
                            ["RabbitMQ:HostName"] = "rabbit-cliente1.example.com",
                            ["ExternalServices:NodeAppUrl"] = "http://node-cliente1:8001/"
                        }
                    },
                    new InstanceConfiguration
                    {
                        InstanceName = "Cliente 2 - Desarrollo",
                        FolderName = "cliente2-dev",
                        ClientId = "2",
                        LogPath = @"C:\PyxoomLogs\Cliente2",
                        PyxoomInteractiveUrl = "https://dev-cliente2.pyxoomdemo.com/Interactive42/Home/SignInToken",
                        PyxoomInteractivePublicPrivacy = "https://dev-cliente2.pyxoomdemo.com/Interactive42/PublicPrivacy/Index",
                        PyxoomConnectionString = "Data Source=servidor2;Database=Pyxoom42_Cliente2_Dev;User ID=user2;Password=pass2;",
                        ExternalPyxoomServicesApiUrl = "https://dev-api-cliente2.pyxoomdemo.com/",
                        RabbitMQChannel = "pyxoom_cliente2_dev",
                        EmpresaId = 2,
                        Description = "Instancia de desarrollo para Cliente 2"
                    }
                }
            };

            var configJson = JsonConvert.SerializeObject(exampleConfig, Formatting.Indented);
            await File.WriteAllTextAsync("instances-config.json", configJson);

            Console.WriteLine("✅ Se ha creado un archivo de ejemplo: instances-config.json");
            Console.WriteLine("📝 Edita este archivo con tus configuraciones y ejecuta nuevamente el programa.");
        }
    }
}

