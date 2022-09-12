using CLI_FilesArchivator.Settings.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.SettingsTests;

public class SettingsDataTest
{
    [Fact]
    public async Task ToStringTest()
    {
        var settingsData = new SettingsData
        {
            Sources = new[] { "path/to/folder", "path/to/file" },
            DestinationFolder = "destination",
            ZipFileName = "new archive",
            LogOptions = new[] { LogOption.Debug, LogOption.Info, LogOption.Error }
        };

        Assert.Equal
        (
            "From: [ path/to/folder, path/to/file ] => To: [ destination/new archive.zip ]; Log Options: [ Debug, Info, Error ]", 
            await Task.FromResult(settingsData.ToString())
        );
    }
}
