using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;
using FNAFMusicBoxFFXIV.Model;
using FNAFMusicBoxFFXIV.Audio;
using System.Numerics;

namespace FNAFMusicBoxFFXIV.Ui;

public sealed class MusicBoxWindow : Window
{
    private const float MeterPadding = 14f;
    private const float BaseWidth = 430f;
    private const float BaseHeight = 190f;
    private const float WindButtonTopOffset = 31f;
    private const float WindButtonHeight = 86f;
    private const float SettingsPopupWidth = 230f;

    private readonly PluginConfiguration configuration;
    private readonly MusicBoxStateMachine stateMachine;
    private readonly IDalamudTextureWrap? windButtonTexture;
    private readonly MusicBoxAudioController audioController;

    public event Action? RequestSave;

    public MusicBoxWindow(PluginConfiguration configuration, MusicBoxStateMachine stateMachine, IDalamudTextureWrap? windButtonTexture, MusicBoxAudioController audioController)
        : base("Music Box")
    {
        this.configuration = configuration;
        this.stateMachine = stateMachine;
        this.windButtonTexture = windButtonTexture;
        this.audioController = audioController;

        Flags = ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.NoCollapse
            | ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.NoScrollWithMouse
            | ImGuiWindowFlags.NoFocusOnAppearing
            | ImGuiWindowFlags.NoResize;

        RespectCloseHotkey = false;
        BgAlpha = configuration.Opacity;
        Position = configuration.WindowPosition;
        Size = configuration.WindowSize;
        PositionCondition = ImGuiCond.FirstUseEver;
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        var deltaSeconds = ImGui.GetIO().DeltaTime;
        stateMachine.Update(deltaSeconds);

        UpdateWindowBehavior();

        var drawList = ImGui.GetWindowDrawList();
        var windowPos = ImGui.GetWindowPos();
        var panelSize = GetScaledPanelSize();
        var scale = MathF.Max(0.75f, configuration.UiScale);
        var contentTop = MathF.Max(MeterPadding, ((panelSize.Y - (WindButtonHeight * scale)) * 0.5f) - (WindButtonTopOffset * scale));
        var contentPos = windowPos + new Vector2(MeterPadding, contentTop);

        DrawBackdrop(drawList, windowPos, panelSize);
        DrawPrompt(drawList, contentPos, panelSize);
        DrawWindButton(drawList, contentPos, panelSize);
        DrawSettingsPopup();

        UpdatePersistedTransform();
    }

    private void UpdateWindowBehavior()
    {
        Flags = ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.NoCollapse
            | ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.NoScrollWithMouse
            | ImGuiWindowFlags.NoFocusOnAppearing
            | ImGuiWindowFlags.NoResize
            | (configuration.LockPosition ? ImGuiWindowFlags.NoMove : ImGuiWindowFlags.None);

        BgAlpha = configuration.Opacity;
    }

    private Vector2 GetScaledPanelSize()
    {
        var width = MathF.Max(BaseWidth, configuration.WindowSize.X) * MathF.Max(0.75f, configuration.UiScale);
        var height = MathF.Max(BaseHeight, configuration.WindowSize.Y) * MathF.Max(0.75f, configuration.UiScale);

        return new Vector2(width, height);
    }

    private void DrawBackdrop(ImDrawListPtr drawList, Vector2 windowPos, Vector2 panelSize)
    {
        var panelMax = windowPos + panelSize;
        var panelMin = windowPos;
        var fill = ImGui.GetColorU32(new Vector4(0.05f, 0.05f, 0.07f, configuration.Opacity));
        var border = ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.12f));

        drawList.AddRectFilled(panelMin, panelMax, fill, 2f);
        drawList.AddRect(panelMin, panelMax, border, 2f, ImDrawFlags.None, 1f);

        var time = ImGui.GetTime();
        const int scanlineCount = 36;
        for (var i = 0; i < scanlineCount; i++)
        {
            var y = panelMin.Y + ((i + 0.5f) * panelSize.Y / scanlineCount);
            var wave = MathF.Sin((float)time * 12f + (i * 0.9f));
            var alpha = 0.03f + ((wave + 1f) * 0.015f);
            var lineColor = ImGui.GetColorU32(new Vector4(1f, 1f, 1f, alpha));
            drawList.AddLine(new Vector2(panelMin.X, y), new Vector2(panelMax.X, y), lineColor, 1f);
        }
    }

    private void DrawPrompt(ImDrawListPtr drawList, Vector2 origin, Vector2 panelSize)
    {
        var scale = MathF.Max(0.75f, configuration.UiScale);
        var center = origin + new Vector2(62f * scale, 54f * scale);
        var radius = 34f * scale;
        var ratio = Math.Clamp(stateMachine.ProgressRatio, 0f, 1f);

        // Dark background circle
        drawList.AddCircleFilled(center, radius, ImGui.GetColorU32(new Vector4(0.08f, 0.08f, 0.1f, 0.95f)), 48);
        
        // Outer white border
        drawList.AddCircle(center, radius, ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.42f)), 48, 2.2f);

        // Full pie-fill from center to edge that depletes clockwise
        var sweepAngle = ratio * MathF.Tau;
        if (sweepAngle > 0.001f)
        {
            var startAngle = -MathF.PI / 2f;
            var segments = Math.Max(12, (int)(48f * ratio));
            var fillColor = ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.94f));

            drawList.PathClear();
            drawList.PathLineTo(center);

            for (var i = 0; i <= segments; i++)
            {
                var angle = startAngle + (sweepAngle * i / segments);
                drawList.PathLineTo(center + (new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius));
            }

            drawList.PathFillConvex(fillColor);
        }
    }

    private void DrawWindButton(ImDrawListPtr drawList, Vector2 origin, Vector2 panelSize)
    {
        var scale = MathF.Max(0.75f, configuration.UiScale);
        var boxMin = origin + new Vector2(130f * scale, 14f * scale);
        var boxSize = new Vector2(248f * scale, 86f * scale);
        var boxMax = boxMin + boxSize;

        ImGui.SetCursorScreenPos(boxMin);
        ImGui.InvisibleButton("##wind-button", boxSize);

        var hovered = ImGui.IsItemHovered();
        var active = ImGui.IsItemActive();

        stateMachine.SetWinding(active);

        if (windButtonTexture is not null)
        {
            drawList.AddImage(windButtonTexture.Handle, boxMin, boxMax);

            if (active)
            {
                drawList.AddRectFilled(boxMin, boxMax, ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.16f)));
            }
            else if (hovered)
            {
                drawList.AddRectFilled(boxMin, boxMax, ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.12f)));
            }
        }
        else
        {
            var fallbackFill = active
                ? new Vector4(0.2f, 0.24f, 0.36f, 0.96f)
                : hovered
                    ? new Vector4(0.16f, 0.2f, 0.31f, 0.94f)
                    : new Vector4(0.14f, 0.17f, 0.28f, 0.9f);

            drawList.AddRectFilled(boxMin, boxMax, ImGui.GetColorU32(fallbackFill), 0f);
            drawList.AddRect(boxMin, boxMax, ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.9f)), 0f, ImDrawFlags.None, 3f);
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            ImGui.OpenPopup("musicbox-settings");
        }
    }

    private void DrawSettingsPopup()
    {
        ImGui.SetNextWindowSize(new Vector2(SettingsPopupWidth, 0f), ImGuiCond.Appearing);

        if (!ImGui.BeginPopup("musicbox-settings"))
        {
            return;
        }

        var changed = false;
        var lockPosition = configuration.LockPosition;
        var opacity = configuration.Opacity;
        var windSpeed = configuration.WindSpeed;
        var decaySpeed = configuration.DecaySpeed;
        var volume = configuration.Volume;
        var isPaused = stateMachine.IsPaused;

        ImGui.TextUnformatted("Overlay Settings");
        ImGui.Separator();

        changed |= ImGui.Checkbox("Lock position", ref lockPosition);
        changed |= ImGui.Checkbox("Pause", ref isPaused);
        changed |= ImGui.SliderFloat("Opacity", ref opacity, 0.2f, 1.0f, "%.2f");
        changed |= ImGui.SliderFloat("Wind speed", ref windSpeed, 10f, 80f, "%.0f");
        changed |= ImGui.SliderFloat("Decay speed", ref decaySpeed, 4f, 30f, "%.0f");

        ImGui.Spacing();
        ImGui.TextUnformatted("Audio");
        ImGui.Separator();
        changed |= ImGui.SliderFloat("Volume", ref volume, 0.0f, 1.0f, "%.2f");

        if (ImGui.Button("Reset meter"))
        {
            stateMachine.Reset();
            changed = true;
        }

        ImGui.SameLine();

        if (ImGui.Button(configuration.IsVisible ? "Hide overlay" : "Show overlay"))
        {
            configuration.IsVisible = !configuration.IsVisible;
            changed = true;
        }

        configuration.LockPosition = lockPosition;
        configuration.Opacity = opacity;
        configuration.WindSpeed = windSpeed;
        configuration.DecaySpeed = decaySpeed;
        configuration.Volume = volume;
        if (isPaused != stateMachine.IsPaused)
        {
            stateMachine.SetPaused(isPaused);
            if (isPaused)
            {
                audioController.Pause();
            }
            else
            {
                audioController.Resume();
            }
        }

        if (changed)
        {
            RequestSave?.Invoke();
        }

        ImGui.EndPopup();
    }

    private void UpdatePersistedTransform()
    {
        var currentPosition = ImGui.GetWindowPos();
        var currentSize = ImGui.GetWindowSize();

        if (Vector2.DistanceSquared(configuration.WindowPosition, currentPosition) > 0.25f)
        {
            configuration.WindowPosition = currentPosition;
            RequestSave?.Invoke();
        }

        if (Vector2.DistanceSquared(configuration.WindowSize, currentSize) > 0.25f)
        {
            configuration.WindowSize = currentSize;
            RequestSave?.Invoke();
        }
    }
}