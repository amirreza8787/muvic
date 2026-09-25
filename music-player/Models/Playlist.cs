using System.Collections.Generic;

namespace music_player.Models;

public class Playlist
{
    public string Name { get; set; } = "";
    public List<string> SongPaths { get; set; } = new();
}