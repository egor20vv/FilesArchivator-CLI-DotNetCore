using CLI_FilesArchivator.Logging;
using System.IO.Compression;

namespace CLI_FilesArchivator.Zipper;

public class Zipper : IAsyncDisposable
{
    private readonly Stream _stream;

    private readonly Logger _logger;

    public Zipper(Stream stream, Logger logger)
    {
        _stream = stream;
        _logger = logger;
    }

    public async Task WriteArchive(IEnumerable<string> sourcePaths)
    {
        if (!_stream.CanRead || !_stream.CanSeek || !_stream.CanWrite)
            throw new IOException("_stream is invalid");
        
        using var archive = new ZipArchive(_stream, ZipArchiveMode.Update);
        await WriteSourcesToArchive(sourcePaths, archive);
        await _logger.Info("Archivation is complete");
    }

    private async Task WriteSourcesToArchive(IEnumerable<string> filesList, ZipArchive archive, string? dirFrom = null)
    {
        foreach (var source in filesList)
        {
            if (File.Exists(source))
            {
                using var sourceReader = await TryOpenStream(source);
                
                if (sourceReader == null)
                {
                    await _logger.Warning(string.Format("Source \"{0}\" is unreachable", source));
                    continue;
                }

                var entryName = dirFrom == null ?
                    Path.GetFileName(source) :
                    Path.GetRelativePath(Directory.GetParent(dirFrom)!.FullName, source);

                var entry = archive.CreateEntryFromFile(source, entryName);

                await _logger.Info(string.Format("File \"...{0}{1}\" archieved", Path.DirectorySeparatorChar, entryName));
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

    private async Task<StreamReader?> TryOpenStream(string source)
    {
        try
        {
            var stream = File.OpenRead(source);
            if (!stream.CanSeek || !stream.CanRead)
                return null;
            return new StreamReader(stream);
        } 
        catch (Exception)
        {
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _stream.DisposeAsync();
    }
}
