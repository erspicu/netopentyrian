using OpenTyrian.Platform;

namespace OpenTyrian.Core;

public sealed class DemoPlaybackController
{
    private readonly DemoPlaybackInfo _info;
    private int _segmentIndex;
    private int _framesRemaining;
    private byte _currentKeys;

    public DemoPlaybackController(DemoPlaybackInfo info)
    {
        _info = info;
        _segmentIndex = 0;
        _framesRemaining = 0;
        _currentKeys = 0;
    }

    public string DisplayLabel
    {
        get { return string.Format("Demo {0}: {1}", _info.DemoNumber, _info.LevelName); }
    }

    public int? MusicTrackIndex
    {
        get { return _info.MusicTrackIndex; }
    }

    public bool TryAdvance(out InputSnapshot input)
    {
        while (_framesRemaining <= 0)
        {
            if (_segmentIndex >= _info.Segments.Count)
            {
                input = default;
                return false;
            }

            DemoInputSegment segment = _info.Segments[_segmentIndex++];
            _currentKeys = segment.Keys;
            _framesRemaining = segment.Frames;
        }

        _framesRemaining--;
        input = CreateInputSnapshot(_currentKeys);
        return true;
    }

    private static InputSnapshot CreateInputSnapshot(byte keys)
    {
        bool confirm = (keys & 0xF0) != 0;
        return new InputSnapshot(
            Up: (keys & (1 << 0)) != 0,
            Down: (keys & (1 << 1)) != 0,
            Left: (keys & (1 << 2)) != 0,
            Right: (keys & (1 << 3)) != 0,
            Confirm: confirm,
            Cancel: false);
    }
}
