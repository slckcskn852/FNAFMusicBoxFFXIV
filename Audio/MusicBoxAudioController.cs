using NAudio.Wave;
using NAudio.CoreAudioApi;
using Dalamud.Plugin.Services;
using System;
using System.IO;

namespace FNAFMusicBoxFFXIV.Audio;

public sealed class MusicBoxAudioController : IDisposable
{
    private enum PlaybackMode
    {
        None,
        MusicLoop,
        FailState,
    }

    private readonly string pluginDirectory;
    private readonly string? musicPath;
    private readonly string? failStatePath;
    private readonly string? windupPath;
    private readonly IPluginLog logger;

    private IWavePlayer? musicOutput;
    private AudioFileReader? musicReader;
    private LoopStream? musicLoop;

    private IWavePlayer? failOutput;
    private AudioFileReader? failReader;

    private IWavePlayer? windupOutput;
    private AudioFileReader? windupReader;
    private LoopStream? windupLoop;

    private PlaybackMode playbackMode = PlaybackMode.None;
    private bool disposed;
    private bool hasLoggedMissingMusic;
    private bool hasLoggedMissingFailstate;
    private bool hasLoggedMissingWindup;
    
    // Tracks the current volume
    private float currentVolume = 1.0f;

    public MusicBoxAudioController(string pluginDirectory, IPluginLog logger)
    {
        this.pluginDirectory = pluginDirectory;
        this.logger = logger;

        musicPath = ResolveAssetPath("musicbox.mp3");
        failStatePath = ResolveAssetPath("failstate.mp3");
        windupPath = ResolveAssetPath("windup.mp3");

        logger.Information("MusicBox audio paths => music: {MusicPath}, fail: {FailPath}, windup: {WindupPath}", musicPath ?? "<missing>", failStatePath ?? "<missing>", windupPath ?? "<missing>");
    }

    // New method to handle volume changes dynamically
    public void SetVolume(float volume)
    {
        // Clamp between 0.0 (mute) and 1.0 (max)
        currentVolume = Math.Clamp(volume, 0.0f, 1.0f);

        if (musicReader is not null) musicReader.Volume = currentVolume;
        if (failReader is not null) failReader.Volume = currentVolume;
        if (windupReader is not null) windupReader.Volume = currentVolume;
    }

    public void Pause()
    {
        musicOutput?.Pause();
        failOutput?.Pause();
        windupOutput?.Pause();
    }

    public void Resume()
    {
        if (musicOutput?.PlaybackState == PlaybackState.Paused)
        {
            musicOutput.Play();
        }

        if (failOutput?.PlaybackState == PlaybackState.Paused)
        {
            failOutput.Play();
        }

        if (windupOutput?.PlaybackState == PlaybackState.Paused)
        {
            windupOutput.Play();
        }
    }

    public void Update(float progress, bool isWinding)
    {
        if (disposed)
        {
            return;
        }

        if (isWinding)
        {
            StartWindupLoop();
        }
        else
        {
            StopWindupLoop();
        }

        var nextMode = progress > 0f ? PlaybackMode.MusicLoop : PlaybackMode.FailState;

        if (nextMode == playbackMode)
        {
            return;
        }

        switch (nextMode)
        {
            case PlaybackMode.MusicLoop:
                StopFailState();
                StartMusicLoop();
                break;
            case PlaybackMode.FailState:
                StopMusicLoop();
                StartFailState();
                break;
            default:
                StopMusicLoop();
                StopFailState();
                break;
        }

        playbackMode = nextMode;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        StopMusicLoop();
        StopFailState();
        StopWindupLoop();

        musicLoop?.Dispose();
        musicReader?.Dispose();
        musicOutput?.Dispose();

        failReader?.Dispose();
        failOutput?.Dispose();

        windupLoop?.Dispose();
        windupReader?.Dispose();
        windupOutput?.Dispose();
    }

    private void StartMusicLoop()
    {
        if (musicPath is null)
        {
            if (!hasLoggedMissingMusic)
            {
                hasLoggedMissingMusic = true;
                logger.Warning("musicbox.mp3 could not be found. No music loop will play.");
            }

            return;
        }

        try
        {
            if (musicOutput is null)
            {
                musicReader = new AudioFileReader(musicPath) { Volume = currentVolume };
                musicLoop = new LoopStream(musicReader);
                musicOutput = CreateOutputDevice();
                musicOutput.Init(musicLoop);
            }

            musicReader!.Position = 0;
            musicOutput.Play();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to start musicbox.mp3 loop playback.");
        }
    }

    private void StopMusicLoop()
    {
        musicOutput?.Stop();
    }

    private void StartFailState()
    {
        if (failStatePath is null)
        {
            if (!hasLoggedMissingFailstate)
            {
                hasLoggedMissingFailstate = true;
                logger.Warning("failstate.mp3 could not be found. No failstate audio will play.");
            }

            return;
        }

        try
        {
            if (failOutput is null)
            {
                failReader = new AudioFileReader(failStatePath) { Volume = currentVolume };
                failOutput = CreateOutputDevice();
                failOutput.Init(failReader);
            }

            failReader!.Position = 0;
            failOutput.Play();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to start failstate.mp3 playback.");
        }
    }

    private void StopFailState()
    {
        if (failOutput is null)
        {
            return;
        }

        failOutput.Stop();

        if (failReader is not null)
        {
            failReader.Position = 0;
        }
    }

    private void StartWindupLoop()
    {
        if (windupPath is null)
        {
            if (!hasLoggedMissingWindup)
            {
                hasLoggedMissingWindup = true;
                logger.Warning("windup.mp3 could not be found. No windup audio will play.");
            }

            return;
        }

        try
        {
            if (windupOutput is null)
            {
                windupReader = new AudioFileReader(windupPath) { Volume = currentVolume };
                windupLoop = new LoopStream(windupReader);
                windupOutput = CreateOutputDevice();
                windupOutput.Init(windupLoop);
            }

            if (windupOutput.PlaybackState != PlaybackState.Playing)
            {
                windupReader!.Position = 0;
                windupOutput.Play();
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to start windup.mp3 loop playback.");
        }
    }

    private void StopWindupLoop()
    {
        if (windupOutput is null)
        {
            return;
        }

        windupOutput.Stop();

        if (windupReader is not null)
        {
            windupReader.Position = 0;
        }
    }

    private string? ResolveAssetPath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(pluginDirectory))
        {
            return null;
        }

        var candidate = Path.Combine(pluginDirectory, fileName);
        if (File.Exists(candidate))
        {
            return candidate;
        }

        return null;
    }

    private IWavePlayer CreateOutputDevice()
    {
        try
        {
            return new WasapiOut(AudioClientShareMode.Shared, false, 100);
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "WasapiOut initialization failed. Falling back to WaveOutEvent.");
            return new WaveOutEvent();
        }
    }
}