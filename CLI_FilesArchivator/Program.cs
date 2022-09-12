using CLI_FilesArchivator.Settings;
using CLI_FilesArchivator.Settings.Types;
using Newtonsoft.Json;

// // Methods // //

static SettingsData GetDefaultSettingsData() =>
    new SettingsData
    {
        Sources = new[] { "path/to/source", "path/to/source" },
        DestinationFolder = "path/to/destination/folder",
        ZipFileName = "name_of_zip_file",
        LogOptions = new[] { LogOption.Info, LogOption.Debug, LogOption.Error }
    };

static async Task CreateDefaultSettingsDataFile(string settingsFileName)
{
    await using var settings = new Settings();
    settings._File = File.Open(settingsFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
    await settings.WriteSettingsFileAsync(GetDefaultSettingsData());
}


// // Main // //

var settingsData = Task.Run(async () =>
{
    var settingsFileName = "settings.json";

    try
    {
        await using var settings = new Settings(settingsFileName);
        return await settings.ReadSettingsFileAsync();
    }
    catch (FileNotFoundException e)
    {
        // create new defualt file
        await CreateDefaultSettingsDataFile(settingsFileName);

        Console.Error.WriteLine("Settings file was not found, new will be created");
        return null;
    }
    catch (IOException e)
    {
        // delete preview, create new default file
        File.Delete(settingsFileName);
        await CreateDefaultSettingsDataFile(settingsFileName);

        Console.Error.WriteLine("Settings file can't to be opened, new will be created");
        return null;
    }
    catch (JsonSerializationException e)
    {
        // reserve preview, create new default file
        File.Delete("old_" + settingsFileName);
        File.Move(settingsFileName, "old_" + settingsFileName);
        await CreateDefaultSettingsDataFile(settingsFileName);

        Console.Error.WriteLine("Settings file deserialization was unsuccessful, new file will be created, old got a name ({0})", "old_" + settingsFileName);
        return null;
    }
}).GetAwaiter().GetResult();


// // Log out // //

Console.WriteLine(settingsData);