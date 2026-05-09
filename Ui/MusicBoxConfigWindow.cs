using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using FNAFMusicBoxFFXIV.Model;
using System.Numerics;

namespace FNAFMusicBoxFFXIV.Ui;

public sealed class MusicBoxConfigWindow : Window
{
    private readonly PluginConfiguration configuration;
    private readonly MusicBoxStateMachine stateMachine;

    public event Action? RequestSave;

    public MusicBoxConfigWindow(PluginConfiguration configuration, MusicBoxStateMachine stateMachine)
        : base("Music Box Settings")
    {
        this.configuration = configuration;
        this.stateMachine = stateMachine;

        // Tell ImGui to dynamically scale the window to fit everything inside it
        Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize;
        
        // You can actually delete these two lines completely now!
        // Size = new Vector2(320f, 300f); 
        // SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        var changed = false;
        var showOverlay = configuration.IsVisible;
        var lockPosition = configuration.LockPosition;
        var opacity = configuration.Opacity;
        var scale = configuration.UiScale;
        var windSpeed = configuration.WindSpeed;
        var decaySpeed = configuration.DecaySpeed;

        ImGui.TextUnformatted("Overlay");
        ImGui.Separator();
        changed |= ImGui.Checkbox("Show overlay", ref showOverlay);
        changed |= ImGui.Checkbox("Lock position", ref lockPosition);
        changed |= ImGui.SliderFloat("Opacity", ref opacity, 0.2f, 1.0f, "%.2f");
        changed |= ImGui.SliderFloat("Scale", ref scale, 0.75f, 1.5f, "%.2f");

        ImGui.Spacing();
        ImGui.TextUnformatted("Behavior");
        ImGui.Separator();
        changed |= ImGui.SliderFloat("Wind speed", ref windSpeed, 10f, 80f, "%.0f");
        changed |= ImGui.SliderFloat("Decay speed", ref decaySpeed, 4f, 30f, "%.0f");

        ImGui.Spacing();
        ImGui.TextUnformatted($"State: {stateMachine.State}");
        ImGui.TextUnformatted($"Progress: {MathF.Round(stateMachine.ProgressRatio * 100f):0}%");

        if (ImGui.Button("Reset meter"))
        {
            stateMachine.Reset();
            changed = true;
        }

        if (ImGui.Button("Save now"))
        {
            RequestSave?.Invoke();
        }

        configuration.IsVisible = showOverlay;
        configuration.LockPosition = lockPosition;
        configuration.Opacity = opacity;
        configuration.UiScale = scale;
        configuration.WindSpeed = windSpeed;
        configuration.DecaySpeed = decaySpeed;

        if (changed)
        {
            RequestSave?.Invoke();
        }
    }
}