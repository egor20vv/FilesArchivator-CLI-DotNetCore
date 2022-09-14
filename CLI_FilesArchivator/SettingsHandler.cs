using CLI_FilesArchivator.Logging;
using CLI_FilesArchivator.Settings;
using CLI_FilesArchivator.Settings.Types;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLI_FilesArchivator;

public static class SettingsHandler
{
    public static async Task<SettingsData?> GetSettingsData(string settingsFileName, Logger logger)
    {
        try
        {
            await using var settingsFile = File.Open(settingsFileName, FileMode.Open, FileAccess.ReadWrite);
            await logger.Debug("Settings file opened");

            await using var settings = new Settings.Settings(settingsFile);
            var settingsData = await settings.ReadSettingsFileAsync();
            await logger.Debug("Settings readed successfully");

            return settingsData;
        }
        catch (FileNotFoundException)
        {
            // create new defualt file
            await logger.Error("Settings file was not found, new will be created");

            await CreateDefaultSettingsDataFile(settingsFileName);
            await logger.Debug("New settings file was created");

            //Environment.Exit(1);
            return null;
        }
        catch (IOException)
        {
            // delete preview, create new default file
            await logger.Error("Settings file can't to be opened");
            //Environment.Exit(1);
            return null;
        }
        catch (JsonSerializationException e)
        {
            // reserve preview, create new default file
            await logger.Error(string.Format("Settings file deserialization was unsuccessful: {0}", e.Message));
            if (YesNoIssue(string.Format("Do you want to reserve old to \"{0}\" and create a new settings file?", "old_" + settingsFileName)))
            {
                await File.WriteAllBytesAsync("old_" + settingsFileName, await File.ReadAllBytesAsync(settingsFileName));
                await CreateDefaultSettingsDataFile(settingsFileName);
                await logger.Debug("Actual settings copied to \"old_settings.txt\" file, new one has been created");
            }

            //Environment.Exit(1);
            return null;
        }
    }

    public static async Task<SettingsData?> ActualSettingsData(SettingsData settingsData, Logger logger)
    {
        if (settingsData == null)
            return null;

        // check destination
        if (!Directory.Exists(settingsData.DestinationFolder))
        {
            await logger.Error(string.Format("Destination folder \"{0}\" was not found", settingsData.DestinationFolder));
            //Environment.Exit(1);
            return null;
        }
        await logger.Debug("Destination dirctory was found");

        // get only found sources
        var actualSources = settingsData.Sources.Where(src => Directory.Exists(src) || File.Exists(src));

        // save found sources 
        if (actualSources.Any())
        {
            // report on not found sources
            foreach (var src in settingsData.Sources.Except(actualSources))
                await logger.Warning(string.Format("Source file \"{0}\" was not found", src));
            settingsData.Sources = actualSources;

            await logger.Debug(string.Format("Approved sources: \n\t{0}", string.Join("\n\t", actualSources)));
        }
        else
        {
            await logger.Error("Sources was not found");
            //Environment.Exit(1);
            return null;
        }

        // check zip file
        var zipFullName = Path.Combine(settingsData.DestinationFolder, settingsData.ZipFileName + ".zip");

        await logger.Debug("Zip file full name: " + zipFullName);

        if (File.Exists(zipFullName))
        {
            if (YesNoIssue(string.Format("The zip file \"{0}\" is already exists. Do you want rewrite it?", settingsData.ZipFileName)))
            {
                File.Delete(zipFullName);
                await logger.Debug(string.Format("{0} was deleted", zipFullName));
            }
            else
            {
                var zipFileIndex = Directory.GetFiles(settingsData.DestinationFolder, string.Format("{0}*", settingsData.ZipFileName)).Count();
                settingsData.ZipFileName += string.Format("({0})", zipFileIndex);
                await logger.Debug(string.Format("{0} name was approved", settingsData.ZipFileName));
            }
        }
        return settingsData;
    }


    #region Private Methods

    private static async Task CreateDefaultSettingsDataFile(string settingsFileName)
    {
        await File.WriteAllTextAsync(settingsFileName, string.Empty);
        await using var settings = new Settings.Settings
        (
            File.Open(settingsFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite)
        );
        await settings.WriteSettingsFileAsync(GetDefaultSettingsData());
    }

    private static SettingsData GetDefaultSettingsData() =>
        new SettingsData
        {
            Sources = new[] { "path/to/source", "path/to/source" },
            DestinationFolder = "path/to/destination/folder",
            ZipFileName = "name_of_zip_file",
            LogOptions = new[] { LogOption.Info, LogOption.Debug, LogOption.Error, LogOption.Warning }
        };

    private static bool YesNoIssue(string issue)
    {
        Console.Write("{0} [y/n]: ", issue);
        var answer = Console.ReadLine()?.ToLower();
        return answer?.StartsWith("y") ?? false;
    }

    #endregion
}
