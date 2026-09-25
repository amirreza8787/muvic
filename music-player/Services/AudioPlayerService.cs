using System;
using NAudio.Wave;

namespace music_player.Services;

public class AudioPlayerService : IDisposable
{
    private WaveOut? _outputDevice;
    private AudioFileReader? _audioFile;

    public event EventHandler? PlaybackStopped;


    public bool IsLoaded =>
        _audioFile != null;


    public bool IsPlaying =>
        _outputDevice?.PlaybackState == PlaybackState.Playing;


    public TimeSpan Position
    {
        get =>
            _audioFile?.CurrentTime
            ?? TimeSpan.Zero;

        set
        {
            if (_audioFile == null)
                return;

            if (value < TimeSpan.Zero)
                value = TimeSpan.Zero;

            if (value > _audioFile.TotalTime)
                value = _audioFile.TotalTime;

            _audioFile.CurrentTime = value;
        }
    }


    public TimeSpan Duration =>
        _audioFile?.TotalTime
        ?? TimeSpan.Zero;


    public void Load(string filePath)
    {
        Stop();

        DisposeAudio();


        _audioFile =
            new AudioFileReader(filePath);


        _outputDevice =
            new WaveOut();


        _outputDevice.PlaybackStopped +=
            OutputDevice_PlaybackStopped;


        _outputDevice.Init(_audioFile);
    }


    public void Play()
    {
        _outputDevice?.Play();
    }


    public void Pause()
    {
        _outputDevice?.Pause();
    }


    public void Stop()
    {
        _outputDevice?.Stop();

        if (_audioFile != null)
        {
            _audioFile.CurrentTime =
                TimeSpan.Zero;
        }
    }


    public void SetVolume(float volume)
    {
        if (_audioFile == null)
            return;

        volume =
            Math.Clamp(volume, 0f, 1f);

        _audioFile.Volume =
            volume;
    }


    private void OutputDevice_PlaybackStopped(
        object? sender,
        StoppedEventArgs e)
    {
        PlaybackStopped?.Invoke(
            this,
            EventArgs.Empty);
    }


    private void DisposeAudio()
    {
        if (_outputDevice != null)
        {
            _outputDevice.PlaybackStopped -=
                OutputDevice_PlaybackStopped;

            _outputDevice.Dispose();

            _outputDevice = null;
        }


        _audioFile?.Dispose();

        _audioFile = null;
    }


    public void Dispose()
    {
        Stop();

        DisposeAudio();
    }
}