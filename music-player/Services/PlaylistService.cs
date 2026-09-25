using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using music_player.Models;

namespace music_player.Services;

public class PlaylistService
{
    private readonly string _filePath;

    public PlaylistService()
    {
        string appFolder = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "NexPlayer"
        );

        Directory.CreateDirectory(appFolder);

        _filePath = Path.Combine(appFolder, "playlists.json");
    }

    public void Save(IEnumerable<Playlist> playlists)
    {
        string json = JsonSerializer.Serialize(
            playlists,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        File.WriteAllText(_filePath, json);
    }

    public List<Playlist> Load()
    {
        if (!File.Exists(_filePath))
            return new List<Playlist>();

        string json = File.ReadAllText(_filePath);

        return JsonSerializer.Deserialize<List<Playlist>>(json)
               ?? new List<Playlist>();
    }

    
    public Playlist? CreatePlaylist(
        IEnumerable<Playlist> playlists,
        string name)
    {
        name = name.Trim();

        if (string.IsNullOrWhiteSpace(name))
            return null;

        if (playlists.Any(p =>
                string.Equals(
                    p.Name,
                    name,
                    StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        return new Playlist
        {
            Name = name
        };
    }
    
    
    public void AddSong(Playlist playlist, string songPath)
    {
        if (playlist.SongPaths.Contains(songPath))
            return;

        playlist.SongPaths.Add(songPath);
    }

    public void RemoveSong(Playlist playlist, string songPath)
    {
        playlist.SongPaths.Remove(songPath);
    }

    public void DeletePlaylist(
        ICollection<Playlist> playlists,
        Playlist playlist)
    {
        playlists.Remove(playlist);
    }
    
    
    public bool RenamePlaylist(
        IEnumerable<Playlist> playlists,
        Playlist playlist,
        string newName)
    {
        newName = newName.Trim();

        if (string.IsNullOrWhiteSpace(newName))
            return false;

        if (playlists.Any(p =>
                p != playlist &&
                string.Equals(
                    p.Name,
                    newName,
                    StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        playlist.Name = newName;

        return true;
    }
}