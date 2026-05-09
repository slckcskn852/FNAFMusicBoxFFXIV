using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FNAFMusicBoxFFXIV.Audio;
using System.IO;

namespace FNAFMusicBoxFFXIV;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/musicbox";

    private readonly IDalamudPluginInterface pluginInterface;
    private readonly ICommandManager commandManager;
    private readonly IPluginLog pluginLog;
    private readonly ITextureProvider textureProvider;
    private readonly WindowSystem windowSystem = new("FNAFMusicBoxFFXIV");
    private readonly PluginConfiguration configuration;
    private readonly Model.MusicBoxStateMachine stateMachine = new();
    private readonly MusicBoxAudioController audioController;
    private readonly IDalamudTextureWrap? windButtonTexture;
    private readonly IDalamudTextureWrap? failOverlayTexture;
    private readonly Ui.MusicBoxWindow musicBoxWindow;
    private readonly Ui.FailstateOverlayWindow failstateOverlayWindow;
    private readonly Ui.MusicBoxConfigWindow configWindow;
    private bool disposed;

    public string Name => "FNAF Music Box";

    public Plugin(IDalamudPluginInterface pluginInterface, ICommandManager commandManager, IPluginLog pluginLog, ITextureProvider textureProvider)
    {
        this.pluginInterface = pluginInterface;
        this.commandManager = commandManager;
        this.pluginLog = pluginLog;
        this.textureProvider = textureProvider;

        configuration = pluginInterface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();
        
        // Get the actual directory where the plugin DLL and assets are located
        var pluginDirectory = pluginInterface.AssemblyLocation.DirectoryName ?? string.Empty;
        
        // Start the music box fully wound up
        stateMachine.SetFullyWound();
        
        // Pass the directory into the new audio controller constructor
        audioController = new MusicBoxAudioController(pluginDirectory, pluginLog);

        var buttonPath = Path.Combine(pluginDirectory, "windupmusicbutton.png");
        if (File.Exists(buttonPath))
        {
            var bytes = File.ReadAllBytes(buttonPath);
            windButtonTexture = textureProvider.CreateFromImageAsync(bytes, "windupmusicbutton.png").GetAwaiter().GetResult();
        }
        else
        {
            pluginLog.Warning("windupmusicbutton.png could not be found at {Path}", buttonPath);
        }

        var failPath = Path.Combine(pluginDirectory, "fail.png");
        if (File.Exists(failPath))
        {
            var bytes = File.ReadAllBytes(failPath);
            failOverlayTexture = textureProvider.CreateFromImageAsync(bytes, "fail.png").GetAwaiter().GetResult();
        }
        else
        {
            pluginLog.Warning("fail.png could not be found at {Path}", failPath);
        }

        musicBoxWindow = new Ui.MusicBoxWindow(configuration, stateMachine, windButtonTexture, audioController)
        {
            IsOpen = configuration.IsVisible,
        };
        musicBoxWindow.RequestSave += SaveConfiguration;

        failstateOverlayWindow = new Ui.FailstateOverlayWindow(stateMachine, failOverlayTexture);

        configWindow = new Ui.MusicBoxConfigWindow(configuration, stateMachine)
        {
            IsOpen = false,
        };
        configWindow.RequestSave += SaveConfiguration;

        windowSystem.AddWindow(musicBoxWindow);
        windowSystem.AddWindow(failstateOverlayWindow);
        windowSystem.AddWindow(configWindow);

        commandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggle the music box overlay, or use 'lock', 'show', 'hide', 'reset', or 'config'.",
        });

        pluginInterface.UiBuilder.Draw += Draw;
        pluginInterface.UiBuilder.OpenMainUi += ToggleOverlay;
    }

    private void Draw()
{
    musicBoxWindow.IsOpen = configuration.IsVisible;
    stateMachine.DecayPerSecond = configuration.DecaySpeed;
    stateMachine.WindPerSecond = configuration.WindSpeed;

    // ---> ADD THIS LINE: Pass the volume from config to the audio controller
    audioController.SetVolume(configuration.Volume);

    windowSystem.Draw();
    audioController.Update(stateMachine.Progress, stateMachine.IsWinding);
}

    private void ToggleOverlay()
    {
        configuration.IsVisible = !configuration.IsVisible;
        musicBoxWindow.IsOpen = configuration.IsVisible;
        SaveConfiguration();
    }

    private void OnCommand(string command, string args)
    {
        var normalized = args.Trim().ToLowerInvariant();

        switch (normalized)
        {
            case "show":
                configuration.IsVisible = true;
                break;
            case "hide":
                configuration.IsVisible = false;
                break;
            case "lock":
                configuration.LockPosition = !configuration.LockPosition;
                break;
            case "reset":
                stateMachine.Reset();
                break;
            case "config":
                configWindow.IsOpen = !configWindow.IsOpen;
                break;
            default:
                configuration.IsVisible = !configuration.IsVisible;
                break;
        }

        musicBoxWindow.IsOpen = configuration.IsVisible;
        SaveConfiguration();
    }

    private void SaveConfiguration()
    {
        pluginInterface.SavePluginConfig(configuration);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        pluginInterface.UiBuilder.Draw -= Draw;
        pluginInterface.UiBuilder.OpenMainUi -= ToggleOverlay;
        commandManager.RemoveHandler(CommandName);

        musicBoxWindow.RequestSave -= SaveConfiguration;
        configWindow.RequestSave -= SaveConfiguration;
        windButtonTexture?.Dispose();
        failOverlayTexture?.Dispose();
        audioController.Dispose();
        SaveConfiguration();
    }
}