using Newtonsoft.Json;

namespace PyxoomInstanceGenerator.Models
{
    public class GeneratorConfig
    {
        [JsonProperty("sourcePath")]
        public string SourcePath { get; set; } = string.Empty;

        [JsonProperty("outputBasePath")]
        public string OutputBasePath { get; set; } = string.Empty;

        [JsonProperty("baseAppSettingsPath")]
        public string BaseAppSettingsPath { get; set; } = string.Empty;

        [JsonProperty("instances")]
        public List<InstanceConfiguration> Instances { get; set; } = new List<InstanceConfiguration>();

        [JsonProperty("copyAdditionalFiles")]
        public List<string> CopyAdditionalFiles { get; set; } = new List<string>();

        [JsonProperty("excludeFiles")]
        public List<string> ExcludeFiles { get; set; } = new List<string>();
    }
}

