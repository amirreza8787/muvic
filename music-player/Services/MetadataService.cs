using System;
using System.IO;
using music_player.Models;
using TagLib;

namespace music_player.Services;

public class MetadataService
{
    public Song ReadMetadata(string filePath)
    {
        var file = TagLib.File.Create(filePath);

        byte[]? coverData = null;

        if (file.Tag.Pictures.Length > 0)
        {
            coverData = file.Tag.Pictures[0].Data.Data;
        }

        return new Song
        {
            Title = string.IsNullOrWhiteSpace(file.Tag.Title)
                ? Path.GetFileNameWithoutExtension(filePath)
                : file.Tag.Title,

            Artist = string.IsNullOrWhiteSpace(file.Tag.FirstPerformer)
                ? "Unknown Artist"
                : file.Tag.FirstPerformer,

            Album = string.IsNullOrWhiteSpace(file.Tag.Album)
                ? "Unknown Album"
                : file.Tag.Album,

            Genre = string.IsNullOrWhiteSpace(file.Tag.FirstGenre)
                ? "Unknown Genre"
                : file.Tag.FirstGenre,
            
            
            FilePath = filePath,

            Duration = file.Properties.Duration,

            
        };
    }   
}