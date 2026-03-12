namespace OpenTyrian.Core;

public sealed class SpriteTable
{
    private readonly SpriteFrame?[] _frames;

    public SpriteTable(SpriteFrame?[] frames)
    {
        _frames = frames;
    }

    public int Count => _frames.Length;

    public bool Exists(int index)
    {
        return index >= 0 && index < _frames.Length && _frames[index] is not null;
    }

    public SpriteFrame GetFrame(int index)
    {
        if (!Exists(index))
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Sprite frame {index} does not exist.");
        }

        return _frames[index]!;
    }
}
