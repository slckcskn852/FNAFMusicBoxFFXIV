namespace FNAFMusicBoxFFXIV.Model;

public enum MusicBoxState
{
    Idle,
    Winding,
    Decaying,
    Depleted,
}

public sealed class MusicBoxStateMachine
{
    public const float MaximumProgress = 100f;

    public float Progress { get; private set; }

    public float WindPerSecond { get; set; } = 38f;

    public float DecayPerSecond { get; set; } = 12f;

    public bool IsWinding { get; private set; }

    public bool IsPaused { get; private set; }

    public MusicBoxState State { get; private set; } = MusicBoxState.Idle;

    public float ProgressRatio => Progress / MaximumProgress;

    public bool IsFull => Progress >= MaximumProgress;

    public bool IsEmpty => Progress <= 0f;

    public void SetWinding(bool isWinding)
    {
        if (IsWinding == isWinding)
        {
            return;
        }

        IsWinding = isWinding;

        if (!isWinding && Progress > 0f && Progress < MaximumProgress)
        {
            State = MusicBoxState.Decaying;
        }
    }

    public void Reset()
    {
        Progress = 0f;
        IsWinding = false;
        State = MusicBoxState.Idle;
    }

    public void SetFullyWound()
    {
        Progress = MaximumProgress;
        IsWinding = false;
        State = MusicBoxState.Idle;
    }

    public void SetPaused(bool paused)
    {
        IsPaused = paused;
    }

    public void Update(float deltaSeconds)
    {
        if (IsPaused || deltaSeconds <= 0f)
        {
            return;
        }

        if (IsWinding)
        {
            Progress = MathF.Min(MaximumProgress, Progress + (WindPerSecond * deltaSeconds));
            State = IsFull ? MusicBoxState.Idle : MusicBoxState.Winding;
            return;
        }

        if (IsEmpty)
        {
            Progress = 0f;
            State = MusicBoxState.Depleted;
            return;
        }

        Progress = MathF.Max(0f, Progress - (DecayPerSecond * deltaSeconds));
        State = IsEmpty ? MusicBoxState.Depleted : MusicBoxState.Decaying;
    }
}