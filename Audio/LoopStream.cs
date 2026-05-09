using NAudio.Wave;

namespace FNAFMusicBoxFFXIV.Audio;

internal sealed class LoopStream : WaveStream
{
    private readonly WaveStream sourceStream;

    public LoopStream(WaveStream sourceStream)
    {
        this.sourceStream = sourceStream;
    }

    public override WaveFormat WaveFormat => sourceStream.WaveFormat;

    public override long Length => sourceStream.Length;

    public override long Position
    {
        get => sourceStream.Position;
        set => sourceStream.Position = value;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var totalBytesRead = 0;

        while (totalBytesRead < count)
        {
            var bytesRead = sourceStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);

            if (bytesRead == 0)
            {
                sourceStream.Position = 0;
                continue;
            }

            totalBytesRead += bytesRead;
        }

        return totalBytesRead;
    }
}