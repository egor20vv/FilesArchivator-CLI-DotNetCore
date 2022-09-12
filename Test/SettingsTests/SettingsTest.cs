using CLI_FilesArchivator.Settings;
using CLI_FilesArchivator.Settings.Types;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.SettingsTests;

public  class SettingsTest
{
    [Fact]
    public async Task ConstructorIsEmptyTest()
    {
        var settings = new Settings();
        var exception = await Record.ExceptionAsync(async () => await settings.DisposeAsync());

        Assert.Null(exception);
    }

    // Read settings file
    [Theory]
    [InlineData(new[] { "path/to/source/file", "path/to/source/folder" }, "path/to/destination/folder", "zipName", new[] { LogOption.None })]
    [InlineData(new[] { "path/to/source/folder" }, "path/to/destination/folder", "zipName", new[] { LogOption.Info, LogOption.Debug, LogOption.Error })]
    public async Task ReadSettingsFileTest(IEnumerable<string> sources, string destination, string zipName, IEnumerable<LogOption> logOptions)
    {
        // prepare
        var settingsData = new SettingsData
        {
            Sources = sources,
            DestinationFolder = destination,
            ZipFileName = zipName,
            LogOptions = logOptions
        };
        await using var fakeStream = GetFakeStream(settingsData);

        // actions
        await using var settings = new Settings();
        settings._File = fakeStream;
        var result = await settings.ReadSettingsFileAsync();

        // asserts
        Assert.NotNull(result);
        Assert.Equal(settingsData!, result, new SettingsDataComparer());
    }

    [Theory]
    [InlineData(new[] {"path"}, "dest", "name", null)]
    [InlineData(new[] {"path"}, "dest", null, new[] {LogOption.None})]
    [InlineData(new[] { "path" }, null, "name", new[] { LogOption.None })]
    [InlineData(null, "dest", "name", new[] { LogOption.None })]
    [InlineData(null, null, null, null)]
    public async Task ReadSettingsFileWithCompletenessErrorTest(IEnumerable<string> sources, string destination, string zipName, IEnumerable<LogOption> logOptions)
    {
        // prepare
        var settingsData = new SettingsData
        {
            Sources = sources,
            DestinationFolder = destination,
            ZipFileName = zipName,
            LogOptions = logOptions
        };
        await using var fakeStream = GetFakeStream(settingsData);

        // actions
        await using var settings = new Settings();
        settings._File = fakeStream;

        // asserts
        await Assert.ThrowsAsync<JsonSerializationException>(async () => await settings.ReadSettingsFileAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("{}")]
    public async Task ReadSettingsFileIsEmptyTest(string fakeFileContent)
    {
        // preparations
        await using var fakeStream = new MemoryStream(Encoding.UTF8.GetBytes(fakeFileContent));

        // actions
        await using var settings = new Settings();
        settings._File = fakeStream;

        // asserts
        await Assert.ThrowsAsync<JsonSerializationException>(async () => await settings.ReadSettingsFileAsync());
    }


    // TODO Write settings file
    [Theory]
    [InlineData(new[] { "path/to/source/file", "path/to/source/folder" }, "path/to/destination/folder", "zipName", new[] { LogOption.None })]
    [InlineData(new[] { "path/to/source/folder" }, "path/to/destination/folder", "zipName", new[] { LogOption.Info, LogOption.Debug, LogOption.Error })]
    public async Task WriteSettingsFileTest(IEnumerable<string> sources, string destination, string zipName, IEnumerable<LogOption> logOptions)
    {
        // preparations
        var settingsData = new SettingsData
        {
            Sources = sources,
            DestinationFolder = destination,
            ZipFileName = zipName,
            LogOptions = logOptions
        };

        await using var expectedStream = GetFakeStream(settingsData);
        await using var expectedSettings = new Settings();
        expectedSettings._File = expectedStream;

        await using var fakeStream = new MemoryStream(new byte[256], true);
        fakeStream.Seek(0, SeekOrigin.Begin);

        // actions
        await using var settings = new Settings();
        settings._File = fakeStream;
        await settings.WriteSettingsFileAsync(settingsData);

        // asserts
        Assert.Equal
        (
            await expectedSettings.ReadSettingsFileAsync(),
            await settings.ReadSettingsFileAsync(),
            new SettingsDataComparer()
        );
        
    }

    [Theory]
    [InlineData(new[] { "path" }, "dest", "name", null)]
    [InlineData(new[] { "path" }, "dest", null, new[] { LogOption.None })]
    [InlineData(new[] { "path" }, null, "name", new[] { LogOption.None })]
    [InlineData(null, "dest", "name", new[] { LogOption.None })]
    [InlineData(null, null, null, null)]
    public async Task WriteSettingsFileWithNotCompleteDataTest(IEnumerable<string> sources, string destination, string zipName, IEnumerable<LogOption> logOptions)
    {
        // preparations
        var settingsData = new SettingsData
        {
            Sources = sources,
            DestinationFolder = destination,
            ZipFileName = zipName,
            LogOptions = logOptions
        };
        await using var fakeStream = new MemoryStream(new byte[256], true);

        await using var settings = new Settings();
        settings._File = fakeStream;

        await Assert.ThrowsAsync<JsonSerializationException>(async () => await settings.WriteSettingsFileAsync(settingsData));

    }


    [Fact]
    public async Task WriteThenReadWithNoExceptionTest()
    {
        // preparations
        var settingsData = new SettingsData
        {
            Sources = new[] { "src" },
            DestinationFolder = "dst",
            ZipFileName = "name",
            LogOptions = new[] { LogOption.None }
        };
        await using var fakeStream = new MemoryStream(new byte[256], true);

        // actions
        await using var settings = new Settings();
        settings._File = fakeStream;
        await settings.WriteSettingsFileAsync(settingsData);

        // assert
        SettingsData? settingsDataResult = null;
        var exception = await Record.ExceptionAsync(async () => settingsDataResult = await settings.ReadSettingsFileAsync());
        Assert.Null(exception);
        Assert.NotNull(settingsDataResult);
        Assert.Equal(settingsData, settingsDataResult!, new SettingsDataComparer());
    }

    [Fact]
    public async Task ReadThenWriteWithNoExceptionsTest()
    {
        // preparations
        var settingsData = new SettingsData
        {
            Sources = new[] { "src" },
            DestinationFolder = "dst",
            ZipFileName = "name",
            LogOptions = new[] { LogOption.None }
        };
        await using var fakeStream = GetFakeStream(settingsData);

        // actions
        await using var settings = new Settings();
        settings._File = fakeStream;
        //var settingsDataReaded = await settings.ReadSettingsFileAsync();

        // assert
        var exception = await Record.ExceptionAsync(async () => await settings.WriteSettingsFileAsync(settingsData));
        Assert.Null(exception);
        Assert.Equal(settingsData, await settings.ReadSettingsFileAsync(), new SettingsDataComparer());
    }

    private Stream GetFakeStream(SettingsData settingsData)
    {
        var options = new JsonSerializerSettings();
        options.Error += (sender, e) => e.ErrorContext.Handled = true;

        var settingsDataString = JsonConvert.SerializeObject(settingsData, options);
        var fakeStream = new MemoryStream();
        
        var writer = new StreamWriter(fakeStream);
        writer.Write(settingsDataString);
        writer.Flush();

        fakeStream.Seek(0, SeekOrigin.Begin);
        return fakeStream;
    }

    private class SettingsDataComparer : IEqualityComparer<SettingsData>
    {
        public bool Equals(SettingsData? x, SettingsData? y)
        {   
            return x != null && y != null &&
                x.Sources.SequenceEqual(y.Sources) &&
                x.DestinationFolder.Equals(y.DestinationFolder) &&
                x.ZipFileName.Equals(y.ZipFileName) &&
                x.LogOptions.SequenceEqual(y.LogOptions);
        }

        public int GetHashCode([DisallowNull] SettingsData obj) => obj.GetHashCode();
    }
}
