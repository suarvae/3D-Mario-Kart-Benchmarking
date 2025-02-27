

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

#if UNITY_2020_1_OR_NEWER
using static Unakin.ProfilerTools.UnakinSingleFrameAnalysis;
#endif // UNITY_2020_1_OR_NEWER

[System.Serializable]
public class CollectProfileData : CommandBase
{
    public string outputAggregateData;
    public string outputSingleFrameMedian;

    public override void InitializeFromJson(string jsonData)
    {
        JsonUtility.FromJsonOverwrite(jsonData, this);

        // true by default
        IsValid = true;

        // flag as invalid if the required data is missing
        if (outputAggregateData == null)
            IsValid = false;

    }

    public override Task ExecuteAsync(CancellationToken cancellationToken)
    {
#if UNITY_2020_1_OR_NEWER
        ProfileDataExporter profileDataExporter = new ProfileDataExporter();
        profileDataExporter.ExportFrameDataToCSV(outputAggregateData);

        Unakin.ProfilerTools.UnakinSingleFrameAnalysis.OutputSingleFrameToCSV(outputSingleFrameMedian, QueryFrameType.MedianFrame, "PlayerLoop");
#else
        throw new Exception("CollectProfileData command is only available in Unity 2020.1 or newer");
#endif // UNITY_2020_1_OR_NEWER

        return Task.CompletedTask;
    }
}