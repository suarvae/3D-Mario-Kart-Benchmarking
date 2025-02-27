using System;

[Serializable]
internal struct UnakinThreadFrameTime : IComparable<UnakinThreadFrameTime>
{
    public int frameIndex;
    public float ms;
    public float msIdle;

    public UnakinThreadFrameTime(int index, float msTime, float msTimeIdle)
    {
        frameIndex = index;
        ms = msTime;
        msIdle = msTimeIdle;
    }

    public int CompareTo(UnakinThreadFrameTime other)
    {
        return ms.CompareTo(other.ms);
    }
}