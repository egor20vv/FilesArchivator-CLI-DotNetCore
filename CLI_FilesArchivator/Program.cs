using CLI_FilesArchivator;
using CLI_FilesArchivator.Logging;
using CLI_FilesArchivator.Zipper;

// // Main // //

// set logger
await using var logger = new Logger().ConfigureConsoleOut();

// get settings
var settingsData = await SettingsHandler.GetSettingsData("settings.json", logger);

// configure logger for a log file
await logger.ConfigureLogFile(settingsData?.LogOptions);
 
// check settings file
settingsData = await SettingsHandler.ActualSettingsData(settingsData, logger);

// zip
if (settingsData != null)
{
    var zipFileFullName = Path.Combine(settingsData!.DestinationFolder, settingsData.ZipFileName + ".zip");
    await using var zipFile = File.Open(zipFileFullName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
    await using var zipper = new Zipper(zipFile, logger);
    await zipper.WriteArchive(settingsData!.Sources);

    Console.WriteLine("\nPress any key to close this window . . .");
}
else
{
    Console.WriteLine("Program exits with code error 1. \n\nPress any key to close this window . . .");
}

Console.ReadLine();