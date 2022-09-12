using CLI_FilesArchivator.Settings.Types;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLI_FilesArchivator.Settings;



public partial class Settings: IAsyncDisposable
{
    #region Properties

    public Stream _File { get; set; }

    #endregion

    public Settings(string settingsFileFullName)
    {
        if (settingsFileFullName == null)
            throw new ArgumentNullException(nameof(settingsFileFullName));

        if (!File.Exists(settingsFileFullName))
            throw new FileNotFoundException("Settings file is not found", settingsFileFullName);

        try
        {
            _File = File.Open(settingsFileFullName, FileMode.Open, FileAccess.ReadWrite);
        } 
        catch (Exception e)
        {
            throw new IOException("Settings file can't be opened", e);
        }
    }

    public async Task<SettingsData> ReadSettingsFileAsync()
    {
        var streamReader = new StreamReader(_File);
        var readedSettingsData = await streamReader.ReadToEndAsync();
        _File.Seek(0, SeekOrigin.Begin);

        var settingsData = JsonConvert.DeserializeObject<SettingsData>(readedSettingsData);
        if (settingsData == null)
            throw new JsonSerializationException("Settings file is empty");
        
        return settingsData;
    }

    public async Task WriteSettingsFileAsync(SettingsData settingsData)
    {
        var settingsDataString = JsonConvert.SerializeObject(settingsData, Formatting.Indented);

        var writer = new StreamWriter(_File);
        await writer.WriteAsync(settingsDataString);
        await writer.FlushAsync();

        _File.Seek(0, SeekOrigin.Begin);
    }

    #region Rest Methods

    public async ValueTask DisposeAsync()
    {
        if (_File != null)
            await _File.DisposeAsync();
    }

    #endregion
}


// tests
public partial class Settings
{
    public Settings() { }
}