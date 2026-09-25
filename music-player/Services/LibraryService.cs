using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using music_player.Models;

namespace music_player.Services;

public class LibraryService
{
    private readonly string _filePath;

    public LibraryService()
    {
        string appFolder = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "NexPlayer"
        );

        Directory.CreateDirectory(appFolder);

        _filePath = Path.Combine(appFolder, "library.json");
    }

    public void Save(IEnumerable<Song> songs)
    {
        string json = JsonSerializer.Serialize(
            songs,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        File.WriteAllText(_filePath, json);
    }

    public List<Song> Load()
    {
        if (!File.Exists(_filePath))
            return new List<Song>();

        string json = File.ReadAllText(_filePath);

        return JsonSerializer.Deserialize<List<Song>>(json)
               ?? new List<Song>();
    }
}