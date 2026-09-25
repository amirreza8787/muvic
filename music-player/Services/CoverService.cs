using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;
using TagLib;

namespace music_player.Services;

public class CoverService
{
    private readonly Dictionary<string, BitmapImage?> _cache = new();

    public BitmapImage? GetCover(string filePath)
    {
        if (_cache.TryGetValue(filePath, out BitmapImage? cachedCover))
            return cachedCover;

        BitmapImage? cover = ReadCover(filePath);

        _cache[filePath] = cover;

        return cover;
    }

    private BitmapImage? ReadCover(string filePath)
    {
        if (!System.IO.File.Exists(filePath))
            return null;

        var file = TagLib.File.Create(filePath);

        if (file.Tag.Pictures.Length == 0)
            return null;

        byte[] data = file.Tag.Pictures[0].Data.Data;

        using var stream = new MemoryStream(data);

        var image = new BitmapImage();

        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();

        return image;
    }
}