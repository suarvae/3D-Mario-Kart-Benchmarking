using System;

[Serializable]
/// <summary>
/// Metrics related to an individual frame
/// </summary>
struct UnakinFrameTime : IComparable<UnakinFrameTime>
{
    /// <summary>Duration in the frame in milliseconds</summary>
    public float ms;
    /// <summary>Index of which frame this time duration occured on. A zero based frame index</summary>
    public int frameIndex;
    /// <summary>Number of occurrences</summary>
    public int count;

    /// <summary>Initialise UnakinFrameTime</summary>
    /// <param name="index"> The frame index</param>
    /// <param name="msTime"> The duration of the frame in milliseconds</param>
    /// <param name="_count"> The number of occurrences</param>
    public UnakinFrameTime(int index, float msTime, int _count)
    {
        frameIndex = index;
        ms = msTime;
        count = _count;
    }

    /// <summary>Initialise from another UnakinFrameTime</summary>
    /// <param name="t"> The UnakinFrameTime to assign</param>
    public UnakinFrameTime(UnakinFrameTime t)
    {
        frameIndex = t.frameIndex;
        ms = t.ms;
        count = t.count;
    }

    /// <summary>Compare the time duration between the frames. Used for sorting in ascending order</summary>
    /// <param name="other"> The other UnakinFrameTime to compare </param>
    /// <returns>-1 if this is smaller, 0 if the same, 1 if this is larger</returns>
    public int CompareTo(UnakinFrameTime other)
    {
        if (ms == other.ms)
        {
            // secondary sort by frame index order
            return frameIndex.CompareTo(other.frameIndex);
        }

        return ms.CompareTo(other.ms);
    }

    /// <summary>Compare the time duration between two FrameTimes. Used for sorting in ascending order</summary>
    /// <param name="a"> The first UnakinFrameTime to compare </param>
    /// <param name="b"> The second UnakinFrameTime to compare </param>
    /// <returns>-1 if a is smaller, 0 if the same, 1 if a is larger</returns>
    public static int CompareMs(UnakinFrameTime a, UnakinFrameTime b)
    {
        if (a.ms == b.ms)
        {
            // secondary sort by frame index order
            return a.frameIndex.CompareTo(b.frameIndex);
        }

        return a.ms.CompareTo(b.ms);
    }

    /// <summary>Compare the instance count between two FrameTimes. Used for sorting in ascending order</summary>
    /// <param name="a"> The first UnakinFrameTime to compare </param>
    /// <param name="b"> The second UnakinFrameTime to compare </param>
    /// <returns>-1 if a is smaller, 0 if the same, 1 if a is larger</returns>
    public static int CompareCount(UnakinFrameTime a, UnakinFrameTime b)
    {
        if (a.count == b.count)
        {
            // secondary sort by frame index order
            return a.frameIndex.CompareTo(b.frameIndex);
        }

        return a.count.CompareTo(b.count);
    }

    /// <summary>Compare the time duration between two FrameTimes. Used for sorting in descending order</summary>
    /// <param name="a"> The first UnakinFrameTime to compare </param>
    /// <param name="b"> The second UnakinFrameTime to compare </param>
    /// <returns>-1 if a is larger, 0 if the same, 1 if a is smaller</returns>
    public static int CompareMsDescending(UnakinFrameTime a, UnakinFrameTime b)
    {
        if (a.ms == b.ms)
        {
            // secondary sort by frame index order
            return a.frameIndex.CompareTo(b.frameIndex);
        }

        return -a.ms.CompareTo(b.ms);
    }

    /// <summary>Compare the instance count between two FrameTimes. Used for sorting in descending order</summary>
    /// <param name="a"> The first UnakinFrameTime to compare </param>
    /// <param name="b"> The second UnakinFrameTime to compare </param>
    /// <returns>-1 if a is larger, 0 if the same, 1 if a is smaller</returns>
    public static int CompareCountDescending(UnakinFrameTime a, UnakinFrameTime b)
    {
        if (a.count == b.count)
        {
            // secondary sort by frame index order
            return a.frameIndex.CompareTo(b.frameIndex);
        }

        return -a.count.CompareTo(b.count);
    }
}
