using Newtonsoft.Json;

namespace PyxoomInstanceGenerator.Models
{
    public class InstanceConfiguration
    {
        [JsonProperty("instanceName")]
        public string InstanceName { get; set; } = string.Empty;

        [JsonProperty("folderName")]
        public string FolderName { get; set; } = string.Empty;

        [JsonProperty("clientId")]
        public string ClientId { get; set; } = string.Empty;

        [JsonProperty("logPath")]
        public string LogPath { get; set; } = string.Empty;

        [JsonProperty("pyxoomInteractiveUrl")]
        public string PyxoomInteractiveUrl { get; set; } = string.Empty;

        [JsonProperty("pyxoomInteractivePublicPrivacy")]
        public string PyxoomInteractivePublicPrivacy { get; set; } = string.Empty;

        [JsonProperty("pyxoomConnectionString")]
        public string PyxoomConnectionString { get; set; } = string.Empty;

        [JsonProperty("externalPyxoomServicesApiUrl")]
        public string ExternalPyxoomServicesApiUrl { get; set; } = string.Empty;

        [JsonProperty("rabbitMQChannel")]
        public string RabbitMQChannel { get; set; } = string.Empty;

        [JsonProperty("empresaId")]
        public int EmpresaId { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        // Configuraciones adicionales opcionales
        [JsonProperty("customSettings")]
        public Dictionary<string, string> CustomSettings { get; set; } = new Dictionary<string, string>();
    }
}




