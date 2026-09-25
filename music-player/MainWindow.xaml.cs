using music_player.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using music_player.Models;
using NAudio.Wave;

namespace music_player;

public partial class MainWindow : Window
{
    // ================= PLAYER STATE =================

    private bool _isPlaying;

    private List<Song> _currentQueue = new();

    private int _currentQueueIndex = -1;

    private Song? _currentSong;

    private Playlist? _currentPlaylist;


    // ================= SERVICES =================

    private readonly AudioPlayerService _player;

    private readonly LibraryService _libraryService;

    private readonly MetadataService _metadataService = new();

    private readonly CoverService _coverService = new();

    private readonly PlaylistService _playlistService = new();


    // ================= TIMER =================

    private readonly DispatcherTimer _timer = new();


    // ================= COLLECTIONS =================

    public ObservableCollection<Song> Songs { get; } = new();

    public ObservableCollection<Playlist> Playlists { get; } = new();


    // ================= CONSTRUCTOR =================

    public MainWindow()
    {
        InitializeComponent();

        DataContext = this;

        _player = new AudioPlayerService();

        _player.PlaybackStopped += Player_PlaybackStopped;


        _libraryService = new LibraryService();


        _timer.Interval = TimeSpan.FromMilliseconds(500);

        _timer.Tick += Timer_Tick;


        // Load saved songs

        foreach (Song song in _libraryService.Load())
        {
            Songs.Add(song);
        }


        // Load saved playlists

        foreach (Playlist playlist in _playlistService.Load())
        {
            Playlists.Add(playlist);
        }
    }


    // ============================================================
    // MUSIC LIBRARY
    // ============================================================

    private void AddMusic_Click(
        object sender,
        RoutedEventArgs e)
    {
        OpenFileDialog dialog = new OpenFileDialog();

        dialog.Title = "Select Music";

        dialog.Filter =
            "Audio Files|*.mp3;*.wav;*.flac;*.m4a|All Files|*.*";


        if (dialog.ShowDialog() != true)
            return;


        string selectedFile = dialog.FileName;

        Song song =
            _metadataService.ReadMetadata(selectedFile);


        Songs.Add(song);

        _libraryService.Save(Songs);
    }


    // ============================================================
    // SONG SELECTION
    // ============================================================

    private void SongList_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (SongList.SelectedItem is not Song selectedSong)
            return;


        if (!IsSongFileAvailable(selectedSong))
        {
            MessageBoxResult result =
                MessageBox.Show(
                    $"File not found:\n\n{selectedSong.FilePath}\n\nWould you like to locate it?",
                    "Missing File",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);


            if (result == MessageBoxResult.Yes)
                LocateSongFile(selectedSong);


            return;
        }


        // A normal library selection starts a new queue.

        _currentQueue.Clear();

        _currentQueueIndex = -1;

        _currentPlaylist = null;


        StartSong(selectedSong);
    }


    // ============================================================
    // PLAY / PAUSE
    // ============================================================

    private void PlayButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!_player.IsLoaded)
            return;


        if (_isPlaying)
        {
            _player.Pause();

            _timer.Stop();

            _isPlaying = false;

            PlayButton.Content = "▶";
        }
        else
        {
            _player.Play();

            _timer.Start();

            _isPlaying = true;

            PlayButton.Content = "⏸";
        }
    }


    // ============================================================
    // START SONG
    // ============================================================

    private void StartSong(Song song)
    {
        if (!IsSongFileAvailable(song))
            return;


        _timer.Stop();

        _isPlaying = false;

        PlayButton.Content = "▶";


        // Reset player UI

        ProgressSlider.Value = 0;

        ProgressSlider.Maximum = 1;

        CurrentTimeText.Text = "00:00";

        DurationText.Text = "00:00";


        _currentSong = song;


        // Load new audio

        try
        {
            _player.Load(song.FilePath);

            DurationText.Text =
                _player.Duration.ToString(@"mm\:ss");


            ProgressSlider.Maximum =
                Math.Max(
                    _player.Duration.TotalSeconds,
                    1);


            // Start immediately.

            _player.Play();

            _isPlaying = true;

            PlayButton.Content = "⏸";

            _timer.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not play this file.\n\n{ex.Message}",
                "Playback Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            _isPlaying = false;

            PlayButton.Content = "▶";
        }
    }


    // ============================================================
    // NEXT
    // ============================================================

    private void NextButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        // Playlist queue

        if (_currentQueue.Count > 0)
        {
            int nextIndex =
                _currentQueueIndex + 1;


            if (nextIndex >= _currentQueue.Count)
                return;


            _currentQueueIndex = nextIndex;


            Song nextSong =
                _currentQueue[_currentQueueIndex];


            if (!IsSongFileAvailable(nextSong))
            {
                MessageBox.Show(
                    $"File not found:\n\n{nextSong.FilePath}",
                    "Missing File",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }


            StartSong(nextSong);

            return;
        }


        // Normal library queue

        if (Songs.Count == 0)
            return;


        if (SongList.SelectedItem is not Song currentSong)
            return;


        int currentIndex =
            Songs.IndexOf(currentSong);


        if (currentIndex == -1)
            return;


        int nextLibraryIndex =
            currentIndex + 1;


        if (nextLibraryIndex >= Songs.Count)
            nextLibraryIndex = 0;


        SongList.SelectedIndex =
            nextLibraryIndex;
    }


    // ============================================================
    // PREVIOUS
    // ============================================================

    private void PreviousButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        // Playlist queue

        if (_currentQueue.Count > 0)
        {
            int previousIndex =
                _currentQueueIndex - 1;


            if (previousIndex < 0)
                return;


            _currentQueueIndex =
                previousIndex;


            Song previousSong =
                _currentQueue[_currentQueueIndex];


            if (!IsSongFileAvailable(previousSong))
                return;


            StartSong(previousSong);

            return;
        }


        // Normal library queue

        if (Songs.Count == 0)
            return;


        if (SongList.SelectedItem is not Song currentSong)
            return;


        int currentIndex =
            Songs.IndexOf(currentSong);


        if (currentIndex == -1)
            return;


        int previousLibraryIndex =
            currentIndex - 1;


        if (previousLibraryIndex < 0)
            previousLibraryIndex = Songs.Count - 1;


        SongList.SelectedIndex =
            previousLibraryIndex;
    }


    // ============================================================
    // AUDIO FINISHED
    // ============================================================

    private void Player_PlaybackStopped(object? sender, EventArgs e)
    {
        // PlaybackStopped can happen on a non-UI thread.
        // Move the queue logic to the WPF UI thread.

        Dispatcher.Invoke(() =>
        {
            if (!_isPlaying)
                return;


            _timer.Stop();

            _isPlaying = false;

            PlayButton.Content = "▶";


            // Playlist

            if (_currentQueue.Count > 0)
            {
                int nextIndex =
                    _currentQueueIndex + 1;


                if (nextIndex >= _currentQueue.Count)
                {
                    _currentQueue.Clear();

                    _currentQueueIndex = -1;

                    _currentPlaylist = null;

                    _currentSong = null;


                    ProgressSlider.Value = 0;

                    CurrentTimeText.Text = "00:00";

                    DurationText.Text = "00:00";


                    return;
                }


                _currentQueueIndex =
                    nextIndex;


                Song nextSong =
                    _currentQueue[_currentQueueIndex];


                if (!IsSongFileAvailable(nextSong))
                {
                    MessageBox.Show(
                        $"File not found:\n\n{nextSong.FilePath}",
                        "Missing File",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }


                StartSong(nextSong);

                return;
            }


            // Normal library

            if (Songs.Count == 0)
                return;


            if (SongList.SelectedItem is not Song currentSong)
                return;


            int currentIndex =
                Songs.IndexOf(currentSong);


            if (currentIndex == -1)
                return;


            int nextLibraryIndex =
                currentIndex + 1;


            if (nextLibraryIndex >= Songs.Count)
                nextLibraryIndex = 0;


            SongList.SelectedIndex =
                nextLibraryIndex;
        });
    }


    // ============================================================
    // PROGRESS SLIDER
    // ============================================================

    private void ProgressSlider_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        _timer.Stop();
    }


    private void ProgressSlider_MouseLeftButtonUp(
        object sender,
        MouseButtonEventArgs e)
    {
        if (!_player.IsLoaded)
            return;


        double seconds =
            ProgressSlider.Value;


        _player.Position =
            TimeSpan.FromSeconds(seconds);


        if (_isPlaying)
        {
            _player.Play();

            _timer.Start();
        }
    }


    // ============================================================
    // TIMER
    // ============================================================

    private void Timer_Tick(
        object? sender,
        EventArgs e)
    {
        if (!_isPlaying)
            return;


        if (!_player.IsLoaded)
            return;


        TimeSpan position =
            _player.Position;


        TimeSpan duration =
            _player.Duration;


        if (duration.TotalSeconds <= 0)
            return;


        ProgressSlider.Maximum =
            duration.TotalSeconds;


        ProgressSlider.Value =
            Math.Min(
                position.TotalSeconds,
                duration.TotalSeconds);


        CurrentTimeText.Text =
            position.ToString(@"mm\:ss");


        DurationText.Text =
            duration.ToString(@"mm\:ss");
    }


    // ============================================================
    // REMOVE SONG
    // ============================================================

    private void RemoveSelectedSong()
    {
        if (SongList.SelectedItem is not Song selectedSong)
            return;


        if (_currentSong == selectedSong)
        {
            _player.Stop();

            _timer.Stop();

            _isPlaying = false;

            _currentSong = null;

            _currentQueue.Clear();

            _currentQueueIndex = -1;

            _currentPlaylist = null;
        }


        Songs.Remove(selectedSong);

        _libraryService.Save(Songs);


        PlayButton.Content = "▶";

        ProgressSlider.Value = 0;

        ProgressSlider.Maximum = 1;

        CurrentTimeText.Text = "00:00";

        DurationText.Text = "00:00";
    }


    private void RemoveSong_Click(
        object sender,
        RoutedEventArgs e)
    {
        RemoveSelectedSong();
    }


    // ============================================================
    // MISSING FILE
    // ============================================================

    private bool IsSongFileAvailable(Song song)
    {
        return File.Exists(song.FilePath);
    }


    private void LocateSongFile(Song song)
    {
        OpenFileDialog dialog =
            new OpenFileDialog();


        dialog.Title = "Locate Music File";

        dialog.Filter =
            "Audio Files|*.mp3;*.wav;*.flac;*.m4a|All Files|*.*";


        if (dialog.ShowDialog() != true)
            return;


        string newFilePath =
            dialog.FileName;


        if (!File.Exists(newFilePath))
            return;


        song.FilePath =
            newFilePath;


        _libraryService.Save(Songs);


        _currentSong = song;


        StartSong(song);
    }


    // ============================================================
    // PLAYLIST CREATE
    // ============================================================

    private bool CreatePlaylist(string name)
    {
        Playlist? playlist =
            _playlistService.CreatePlaylist(
                Playlists,
                name);


        if (playlist == null)
            return false;


        Playlists.Add(playlist);

        _playlistService.Save(Playlists);

        return true;
    }


    private void CreatePlaylist_Click(
        object sender,
        RoutedEventArgs e)
    {
        PlaylistNameWindow window =
            new PlaylistNameWindow("New Playlist");


        window.Owner = this;


        if (window.ShowDialog() != true)
            return;


        if (!CreatePlaylist(window.PlaylistName))
        {
            MessageBox.Show(
                "A playlist with this name already exists.",
                "Cannot Create Playlist",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }


    // ============================================================
    // PLAYLIST DELETE
    // ============================================================

    private bool DeletePlaylist(
        Playlist playlist)
    {
        if (!Playlists.Contains(playlist))
            return false;


        _playlistService.DeletePlaylist(
            Playlists,
            playlist);


        _playlistService.Save(Playlists);

        return true;
    }


    private void DeletePlaylist_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (PlaylistList.SelectedItem
            is not Playlist selectedPlaylist)
        {
            MessageBox.Show(
                "Please select a playlist first.",
                "No Playlist Selected",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }


        MessageBoxResult result =
            MessageBox.Show(
                $"Delete playlist \"{selectedPlaylist.Name}\"?",
                "Delete Playlist",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);


        if (result != MessageBoxResult.Yes)
            return;


        if (_currentPlaylist == selectedPlaylist)
        {
            _player.Stop();

            _timer.Stop();

            _isPlaying = false;

            _currentQueue.Clear();

            _currentQueueIndex = -1;

            _currentPlaylist = null;

            _currentSong = null;


            PlayButton.Content = "▶";

            ProgressSlider.Value = 0;

            ProgressSlider.Maximum = 1;

            CurrentTimeText.Text = "00:00";

            DurationText.Text = "00:00";
        }


        DeletePlaylist(selectedPlaylist);
    }


    // ============================================================
    // PLAYLIST RENAME
    // ============================================================

    private bool RenamePlaylist(
        Playlist playlist,
        string newName)
    {
        if (!_playlistService.RenamePlaylist(
                Playlists,
                playlist,
                newName))
        {
            return false;
        }


        _playlistService.Save(Playlists);

        return true;
    }


    private void RenamePlaylist_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (PlaylistList.SelectedItem
            is not Playlist selectedPlaylist)
        {
            MessageBox.Show(
                "Please select a playlist first.",
                "No Playlist Selected",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }


        PlaylistNameWindow window =
            new PlaylistNameWindow(
                "Rename Playlist",
                selectedPlaylist.Name);


        window.Owner = this;


        if (window.ShowDialog() != true)
            return;


        if (!RenamePlaylist(
                selectedPlaylist,
                window.PlaylistName))
        {
            MessageBox.Show(
                "A playlist with this name already exists.",
                "Cannot Rename Playlist",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }


    // ============================================================
    // PLAYLIST ADD SONG
    // ============================================================

    private bool AddSongToPlaylist(
        Playlist playlist,
        Song song)
    {
        if (playlist.SongPaths.Contains(song.FilePath))
            return false;


        _playlistService.AddSong(
            playlist,
            song.FilePath);


        _playlistService.Save(Playlists);

        return true;
    }


    private void AddSongToPlaylist_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (PlaylistList.SelectedItem
            is not Playlist selectedPlaylist)
        {
            MessageBox.Show(
                "Please select a playlist first.",
                "No Playlist Selected",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }


        if (SongList.SelectedItem
            is not Song selectedSong)
        {
            MessageBox.Show(
                "Please select a song first.",
                "No Song Selected",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }


        if (!AddSongToPlaylist(
                selectedPlaylist,
                selectedSong))
        {
            MessageBox.Show(
                "This song is already in the playlist.",
                "Song Already Exists",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }


        MessageBox.Show(
            $"\"{selectedSong.Title}\" added to \"{selectedPlaylist.Name}\".",
            "Song Added",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }


    // ============================================================
    // PLAYLIST REMOVE SONG
    // ============================================================

    private bool RemoveSongFromPlaylist(
        Playlist playlist,
        Song song)
    {
        if (!playlist.SongPaths.Contains(song.FilePath))
            return false;


        _playlistService.RemoveSong(
            playlist,
            song.FilePath);


        _playlistService.Save(Playlists);

        return true;
    }


    // ============================================================
    // FIND SONG
    // ============================================================

    private Song? FindSongByPath(
        string filePath)
    {
        return Songs.FirstOrDefault(
            song =>
                string.Equals(
                    song.FilePath,
                    filePath,
                    StringComparison.OrdinalIgnoreCase));
    }


    private List<Song> GetSongsFromPlaylist(
        Playlist playlist)
    {
        return playlist.SongPaths
            .Select(FindSongByPath)
            .Where(song => song != null)
            .Cast<Song>()
            .ToList();
    }


    // ============================================================
    // PLAY PLAYLIST
    // ============================================================

    private void PlayPlaylist(
        Playlist playlist)
    {
        List<Song> songs =
            GetSongsFromPlaylist(playlist);


        if (songs.Count == 0)
        {
            MessageBox.Show(
                "This playlist has no songs.",
                "Empty Playlist",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }


        _currentQueue = songs;

        _currentQueueIndex = 0;

        _currentPlaylist = playlist;


        Song firstSong =
            _currentQueue[_currentQueueIndex];


        if (!IsSongFileAvailable(firstSong))
        {
            MessageBox.Show(
                $"File not found:\n\n{firstSong.FilePath}",
                "Missing File",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }


        StartSong(firstSong);
    }


    // ============================================================
    // PLAYLIST SELECTION
    // ============================================================

    private void PlaylistList_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (PlaylistList.SelectedItem
            is not Playlist selectedPlaylist)
        {
            return;
        }


        PlayPlaylist(selectedPlaylist);
    }


    // ============================================================
    // FAVORITES
    // ============================================================

    private bool ToggleFavorite(
        Song song)
    {
        if (!Songs.Contains(song))
            return false;


        song.IsFavorite =
            !song.IsFavorite;


        _libraryService.Save(Songs);


        return song.IsFavorite;
    }


    private List<Song> GetFavoriteSongs()
    {
        return Songs
            .Where(song => song.IsFavorite)
            .ToList();
    }


    // ============================================================
    // NAVIGATION
    // ============================================================

    private void HomeButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        PageTitle.Text = "Home";

        SongList.ItemsSource = Songs;
    }


    private void SongsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        PageTitle.Text = "Songs";

        SongList.ItemsSource = Songs;
    }


    private void FavoritesButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        PageTitle.Text = "Favorites";

        SongList.ItemsSource =
            GetFavoriteSongs();
    }


    private void AlbumsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        PageTitle.Text = "Albums";

        SongList.ItemsSource = null;
    }


    private void ArtistsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        PageTitle.Text = "Artists";

        SongList.ItemsSource = null;
    }


    // ============================================================
    // WINDOW CLOSE
    // ============================================================

    protected override void OnClosed(EventArgs e)
    {
        _timer.Stop();

        _player.PlaybackStopped -=
            Player_PlaybackStopped;

        _player.Dispose();

        base.OnClosed(e);
    }
}