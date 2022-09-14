using CLI_FilesArchivator.Settings.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLI_FilesArchivator.Logging;

public class Logger : IAsyncDisposable
{
    public StreamWriter? LogFile { get; set; } = null;

    private IEnumerable<LogOption>? _logFileOptions = null;
    private IEnumerable<LogOption>? _consoleOutOptions = null;


    public Logger() { }

    public Logger ConfigureConsoleOut(IEnumerable<LogOption>? options)
    {
        _consoleOutOptions = options;
        return this;
    }

    public Logger ConfigureConsoleOut()
    {
        return ConfigureConsoleOut(new List<LogOption>
        {
            LogOption.Debug,
            LogOption.Info,
            LogOption.Warning,
            LogOption.Error
        });
    }

    public async Task<Logger> ConfigureLogFile(IEnumerable<LogOption>? options)
    {
        var fullLogFilePath = Path.Combine("Logs", string.Format("log_{0}.txt", DateTime.Now.ToString("yyyyMMddHHmmss")));

        if (Directory.Exists("Logs"))
            Directory.CreateDirectory("Logs");

        // clear if existed file
        await File.WriteAllTextAsync(fullLogFilePath, string.Empty);
        // new stream writer
        var logFile = new StreamWriter(File.Open(fullLogFilePath, FileMode.OpenOrCreate, FileAccess.Write));
        return await ConfigureLogFile(options, logFile);
    }

    public async Task<Logger> ConfigureLogFile(IEnumerable<LogOption>? options, StreamWriter logStream)
    {
        _logFileOptions = options;

        if (LogFile != null && LogFile.BaseStream.CanWrite)
            await LogFile.DisposeAsync();

        if (options != null && options.Any() && options.First() != LogOption.None)
            LogFile = logStream;
        else
            LogFile = null;

        return this;
    }

    public async Task<Logger> WriteLine(string line, LogOption option)
    {
        string stringToWrite = string.Format("{0}: {1}", option.ToString(), line);
        
        // write to console
        if (_consoleOutOptions?.Contains(option) ?? false)
            await Console.Out.WriteLineAsync(stringToWrite);

        // write to log file
        if (LogFile != null && (_logFileOptions?.Contains(option) ?? false))
        {
            await LogFile.WriteLineAsync(stringToWrite);
            await LogFile.FlushAsync();
        }
        return this;
    }

    public async Task<Logger> Info(string line) =>
        await WriteLine(line, LogOption.Info);

    public async Task<Logger> Debug(string line) =>
        await WriteLine(line, LogOption.Debug);

    public async Task<Logger> Warning(string line) =>
        await WriteLine(line, LogOption.Warning);

    public async Task<Logger> Error(string line) =>
        await WriteLine(line, LogOption.Error);


    public async ValueTask DisposeAsync()
    {
        if (LogFile != null && LogFile.BaseStream.CanWrite)
            await LogFile.DisposeAsync();
    }
}
