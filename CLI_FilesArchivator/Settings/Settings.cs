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
    public Stream _File { get; set; }

    public Settings() { }
    public Settings(Stream settingsFile)
    {
        _File = settingsFile;
    }

    public async Task<SettingsData> ReadSettingsFileAsync()
    {
        if (_File == null || !_File.CanSeek || !_File.CanRead)
            throw new IOException("_File is invalid");

        var streamReader = new StreamReader(_File);
        var readedSettingsData = await streamReader.ReadToEndAsync();
        _File.Seek(0, SeekOrigin.Begin);

        try
        {
            var settingsData = JsonConvert.DeserializeObject<SettingsData>(readedSettingsData);
            if (settingsData == null)
                throw new JsonSerializationException("Settings file is empty");

            return settingsData;
        }
        catch(Exception e)
        {
            throw new JsonSerializationException(e.Message);
        }
        
    }

    public async Task WriteSettingsFileAsync(SettingsData settingsData)
    {
        if (_File == null || !_File.CanSeek || !_File.CanWrite)
            throw new IOException("_File is invalid");

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