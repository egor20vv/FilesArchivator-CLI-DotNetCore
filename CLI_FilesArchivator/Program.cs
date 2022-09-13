using CLI_FilesArchivator.Settings;
using CLI_FilesArchivator.Settings.Types;
using Newtonsoft.Json;
using System.IO.Compression;

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

//enum RewriteIssueAnswer
//{
//    NoActions, DeleteCreateNew, ReserveCreateNew
//};

static async Task<bool> FileAlreadyExistsRewriteIssue(string fileKind, string fileName)
{
    Console.WriteLine("The {0} file \"{1}\" is already exists. Do you want rewrite it? [y/n]", fileKind, fileName);
    var answer = Console.ReadLine()?.ToLower();
    return answer?.StartsWith("y") ?? false;
}




// // Main // //

// get settings
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
        Environment.Exit(1);
        return null;
    }
    catch (IOException e)
    {
        // delete preview, create new default file
        File.Delete(settingsFileName);
        await CreateDefaultSettingsDataFile(settingsFileName);

        Console.Error.WriteLine("Settings file can't to be opened, new will be created");
        Environment.Exit(1);
        return null;
    }
    catch (JsonSerializationException e)
    {
        // reserve preview, create new default file
        File.Delete("old_" + settingsFileName);
        File.Move(settingsFileName, "old_" + settingsFileName);
        await CreateDefaultSettingsDataFile(settingsFileName);

        Console.Error.WriteLine("Settings file deserialization was unsuccessful, new file will be created, old got a name ({0})", "old_" + settingsFileName);
        Environment.Exit(1);
        return null;
    }
}).GetAwaiter().GetResult();

// check settings file
Task.Run(() =>
{
    // check destination
    if (!Directory.Exists(settingsData!.DestinationFolder))
        throw new DirectoryNotFoundException(settingsData!.DestinationFolder);

    // check sources
    var existingSources = new List<string>();
    foreach (var source in settingsData!.Sources)
    {
        if (Directory.Exists(source) || File.Exists(source))
            existingSources.Add(source);
    }

    if (existingSources.Count() == 0)
        throw new Exception("source files was not found");

    settingsData!.Sources = existingSources;

}).GetAwaiter().GetResult();


// zip
static async Task WriteSourcesToArchive(IEnumerable<string> filesList, ZipArchive archive, string? dirFrom = null)
{
    foreach (var source in filesList)
    {
        if (File.Exists(source))
        {
            using var sourceReader = new StreamReader(File.OpenRead(source));

            var entryName = dirFrom == null ?
                Path.GetFileName(source) :
                Path.GetRelativePath(Directory.GetParent(dirFrom)!.FullName, source);

            var entry = archive.CreateEntryFromFile(source, entryName);

            Console.WriteLine("Info: File \"{0}\" archieved", source);
        }
        else if (Directory.Exists(source))
        {
            var entryName = dirFrom == null ?
               Path.GetFileName(source) + '\\' :
               Path.Combine(Path.GetRelativePath(Directory.GetParent(dirFrom)!.FullName, source));

            await WriteSourcesToArchive(Directory.GetFiles(source), archive, dirFrom ?? source);
            await WriteSourcesToArchive(Directory.GetDirectories(source), archive, dirFrom ?? source);
        }
    }
}

Task.Run(async () =>
{
    var zipFileFullName = Path.Combine(settingsData!.DestinationFolder, settingsData.ZipFileName + ".zip");

    using var archive = new ZipArchive(File.Open(zipFileFullName, FileMode.OpenOrCreate, FileAccess.ReadWrite), ZipArchiveMode.Update);

    await WriteSourcesToArchive(settingsData!.Sources, archive);

}).GetAwaiter().GetResult();

// // Log out // //

// Console.WriteLine(settingsData);