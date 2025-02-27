using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[Serializable]
internal class UnakinThreadData
{
    public string threadNameWithIndex;
    public int threadGroupIndex;
    public string threadGroupName;
    public int threadsInGroup;
    public List<UnakinThreadFrameTime> frames = new List<UnakinThreadFrameTime>();

    public float msMedian;
    public float msLowerQuartile;
    public float msUpperQuartile;
    public float msMin;
    public float msMax;

    public int medianFrameIndex;
    public int minFrameIndex;
    public int maxFrameIndex;

    public UnakinThreadData(string _threadName)
    {
        threadNameWithIndex = _threadName;

        var info = threadNameWithIndex.Split(':');
        threadGroupIndex = int.Parse(info[0]);
        threadGroupName = info[1];
        threadsInGroup = 1;

        msMedian = 0.0f;
        msLowerQuartile = 0.0f;
        msUpperQuartile = 0.0f;
        msMin = 0.0f;
        msMax = 0.0f;

        medianFrameIndex = -1;
        minFrameIndex = -1;
        maxFrameIndex = -1;
    }

    struct ThreadFrameTimeFrameComparer : IComparer<UnakinThreadFrameTime>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(UnakinThreadFrameTime x, UnakinThreadFrameTime y)
        {
            if (x.frameIndex == y.frameIndex)
                return 0;
            return x.frameIndex > y.frameIndex ? 1 : -1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnakinThreadFrameTime? GetFrame(int frameIndex)
    {
        var index = frames.BinarySearch(new UnakinThreadFrameTime(frameIndex, 0, 0), new ThreadFrameTimeFrameComparer());
        return index >= 0 ? (UnakinThreadFrameTime?)frames[index] : null;
    }
}
