using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CLI_FilesArchivator.Settings.Types;

[JsonConverter(typeof(StringEnumConverter))]
public enum LogOption
{
    None = 0, Info = 1, Debug = 2, Error = 4
}
