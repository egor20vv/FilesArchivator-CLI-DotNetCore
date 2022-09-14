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
            throw new IOException("stream is invalid");
        
        using var archive = new ZipArchive(_stream, ZipArchiveMode.Update);
        await WriteSourcesToArchive(sourcePaths, archive);
    }

    private async Task WriteSourcesToArchive(IEnumerable<string> filesList, ZipArchive archive, string? dirFrom = null)
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

                await _logger.Info(string.Format("File \"...{0}{1}\" archieved", Path.PathSeparator, entryName));
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

    public async ValueTask DisposeAsync()
    {
        await _stream.DisposeAsync();
    }
}
