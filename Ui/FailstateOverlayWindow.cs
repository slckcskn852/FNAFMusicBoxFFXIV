using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures.TextureWraps;
using FNAFMusicBoxFFXIV.Model;
using System.Numerics;

namespace FNAFMusicBoxFFXIV.Ui;

public sealed class FailstateOverlayWindow : Window
{
    private readonly MusicBoxStateMachine stateMachine;
    private readonly IDalamudTextureWrap? failOverlayTexture;

    public FailstateOverlayWindow(MusicBoxStateMachine stateMachine, IDalamudTextureWrap? failOverlayTexture)
        : base("Failstate Overlay")
    {
        this.stateMachine = stateMachine;
        this.failOverlayTexture = failOverlayTexture;

        Flags = ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.NoCollapse
            | ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.NoScrollWithMouse
            | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoResize
            | ImGuiWindowFlags.NoFocusOnAppearing
            | ImGuiWindowFlags.NoBringToFrontOnFocus
            | ImGuiWindowFlags.NoMouseInputs;

        RespectCloseHotkey = false;
        BgAlpha = 0f;
        IsOpen = true;
        Position = Vector2.Zero;
        Size = ImGui.GetIO().DisplaySize;
    }

    public override void PreDraw()
    {
        var io = ImGui.GetIO();
        Position = Vector2.Zero;
        Size = io.DisplaySize;
    }

    public override void Draw()
    {
        if (stateMachine.IsPaused)
            return;

        var ratio = Math.Clamp(stateMachine.ProgressRatio, 0f, 1f);
        var alpha = ratio > 0.5f ? 0f : (0.5f - ratio) / 0.5f;

        if (alpha < 0.001f || failOverlayTexture is null)
            return;

        var drawList = ImGui.GetWindowDrawList();
        var io = ImGui.GetIO();
        var screenSize = io.DisplaySize;
        var screenMin = Vector2.Zero;
        var screenMax = screenSize;

        var overlayColor = ImGui.GetColorU32(new Vector4(1f, 1f, 1f, alpha));
        drawList.AddImage(failOverlayTexture.Handle, screenMin, screenMax, Vector2.Zero, Vector2.One, overlayColor);
    }
}
