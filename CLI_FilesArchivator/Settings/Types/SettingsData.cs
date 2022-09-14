using Newtonsoft.Json;

namespace CLI_FilesArchivator.Settings.Types;

public class SettingsData
{
    [JsonProperty(Required = Required.Always)]
    public IEnumerable<string> Sources { get; set; }

    [JsonProperty(Required = Required.Always)]
    public string DestinationFolder { get; set; }

    [JsonProperty(Required = Required.Always)]
    public string ZipFileName { get; set; }

    // [JsonProperty(Required = Required.AllowNull)]
    public IEnumerable<LogOption>? LogOptions { get; set; }


    public override string ToString() =>
        string.Format("From: [ {0} ] => To: [ {1}/{2}.zip ]; Log Options: [ {3} ]", 
            string.Join(", ", Sources), 
            DestinationFolder,
            ZipFileName,
            string.Join(", ", LogOptions.Select(lo => lo.ToString()))
        );
}
