using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PyxoomInstanceGenerator.Models;

namespace PyxoomInstanceGenerator.Services
{
    public class AppSettingsGenerator
    {
        private readonly string _baseAppSettingsPath;

        public AppSettingsGenerator(string baseAppSettingsPath)
        {
            _baseAppSettingsPath = baseAppSettingsPath;
        }

        public async Task<string> GenerateAppSettingsAsync(InstanceConfiguration instance)
        {
            try
            {
                // Leer el archivo base de appsettings
                var baseAppSettingsJson = await File.ReadAllTextAsync(_baseAppSettingsPath);
                var baseSettings = JObject.Parse(baseAppSettingsJson);

                // Aplicar las configuraciones específicas de la instancia
                ApplyInstanceSettings(baseSettings, instance);

                // Convertir a JSON formateado
                return JsonConvert.SerializeObject(baseSettings, Formatting.Indented);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error generando appsettings para la instancia {instance.InstanceName}: {ex.Message}", ex);
            }
        }

        private void ApplyInstanceSettings(JObject baseSettings, InstanceConfiguration instance)
        {
            // Actualizar ClientId en ExternalServices
            if (baseSettings["ExternalServices"] != null)
            {
                baseSettings["ExternalServices"]["ClientId"] = instance.ClientId;
                baseSettings["ExternalServices"]["PyxoomInteractiveUrl"] = instance.PyxoomInteractiveUrl;
                baseSettings["ExternalServices"]["PyxoomInteractivePublicPrivacy"] = instance.PyxoomInteractivePublicPrivacy;
            }

            // Actualizar ClientId en App
            if (baseSettings["App"] != null)
            {
                baseSettings["App"]["ClientId"] = instance.ClientId;
                baseSettings["App"]["EmpresaId"] = instance.EmpresaId;
            }

            // Actualizar ConnectionStrings
            if (baseSettings["ConnectionStrings"] != null)
            {
                baseSettings["ConnectionStrings"]["Pyxoom42"] = instance.PyxoomConnectionString;
            }

            // Actualizar ExternalPyxoomServices
            if (baseSettings["ExternalPyxoomServices"] != null)
            {
                baseSettings["ExternalPyxoomServices"]["ApiUrl"] = instance.ExternalPyxoomServicesApiUrl;
            }

            // Actualizar RabbitMQ Channel
            if (baseSettings["RabbitMQ"] != null)
            {
                baseSettings["RabbitMQ"]["Channel"] = instance.RabbitMQChannel;
            }

            // Actualizar rutas de logs en Serilog
            UpdateSerilogPaths(baseSettings, instance);

            // Aplicar configuraciones personalizadas
            ApplyCustomSettings(baseSettings, instance);
        }

        private void UpdateSerilogPaths(JObject baseSettings, InstanceConfiguration instance)
        {
            if (baseSettings["Serilog"]?["WriteTo"] is JArray writeToArray)
            {
                foreach (var item in writeToArray)
                {
                    if (item["Args"]?["configure"] is JArray configureArray)
                    {
                        foreach (var config in configureArray)
                        {
                            if (config["Name"]?.ToString() == "File" && config["Args"]?["path"] != null)
                            {
                                var originalPath = config["Args"]["path"]?.ToString();
                                if (!string.IsNullOrEmpty(originalPath))
                                {
                                    // Extraer solo el nombre del archivo de log
                                    var fileName = Path.GetFileName(originalPath);
                                    
                                    // Crear nueva ruta usando el logPath de la instancia
                                    var newPath = Path.Combine(instance.LogPath, fileName).Replace('\\', '/');
                                    
                                    config["Args"]["path"] = newPath;
                                }
                            }
                        }
                    }
                }
            }

            // Actualizar el nombre de la aplicación en Serilog
            if (baseSettings["Serilog"]?["Properties"] != null)
            {
                baseSettings["Serilog"]["Properties"]["Application"] = $"Pyxoom.Analytix.Queue.{instance.InstanceName}";
            }
        }

        private void ApplyCustomSettings(JObject baseSettings, InstanceConfiguration instance)
        {
            if (instance.CustomSettings == null || !instance.CustomSettings.Any())
                return;

            foreach (var customSetting in instance.CustomSettings)
            {
                var pathParts = customSetting.Key.Split(':');
                var current = baseSettings;

                // Navegar hasta el penúltimo nivel
                for (int i = 0; i < pathParts.Length - 1; i++)
                {
                    if (current[pathParts[i]] == null)
                    {
                        current[pathParts[i]] = new JObject();
                    }
                    current = (JObject)current[pathParts[i]]!;
                }

                // Establecer el valor final
                var finalKey = pathParts[pathParts.Length - 1];
                current[finalKey] = customSetting.Value;
            }
        }
    }
}

