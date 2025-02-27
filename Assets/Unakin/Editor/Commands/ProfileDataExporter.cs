#if UNITY_2020_1_OR_NEWER

using System;
using System.IO;
using System.Threading;

using UnityEditor;
using UnityEditorInternal;

using UnityEditor.Profiling;
using System.Collections;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.Globalization;
using UnityEngine.UIElements;
using UnityEngine.Profiling;
using System.Diagnostics;
using UnityEngine;
using System.Text;
using UnityEngine.Analytics;
using System.Reflection;

// create an ordered (by elapsedNanoseconds) list of Recorder objects
class ProfileMetric
{
    public string name;
    public Recorder recorder;
};

public class ProfileDataExporter
{
    public void ExportFrameDataToCSV(string filepath)
    {
        UnakinProfileAnalysis analysis = PullFromProfiler(Path.ChangeExtension(filepath, "pdata"));

        List<string> headers = new List<string>();
        headers.Add("name");
        headers.Add("msTotal");
        headers.Add("count");
        headers.Add("msMean");
        headers.Add("countMean");
        headers.Add("minDepth");
        headers.Add("maxDepth");
        OutputToCSV(filepath, analysis, headers);
    }

    UnakinProfileAnalysis PullFromProfiler(string pdata_path)
    {
        //OpenProfilerOrUseExisting();

        Unakin.ProfilerWindowInterface m_ProfilerWindowInterface = new Unakin.ProfilerWindowInterface();

        // this gets the window handle, required to pull data from
        m_ProfilerWindowInterface.OpenProfilerOrUseExisting();

        int first;
        int last;
        m_ProfilerWindowInterface.GetFrameRangeFromProfiler(out first, out last);
        if(last==0)
        {
            throw new Exception("Profiler has no data to collect. Run profiler first.");
        }

        int available_frames = last - first;
        int num_frames_to_analyse = Math.Min(available_frames, 50);

        int firstFrameDisplayIndex = last - num_frames_to_analyse;
        int lastFrameDisplayIndex = last;

        int trim_depth = 3;

        // if deep profiling is enabled then the first and last frames seem skewed, we want to omit those from our profiling data
        firstFrameDisplayIndex = firstFrameDisplayIndex + trim_depth;
        lastFrameDisplayIndex = lastFrameDisplayIndex - trim_depth;

        UnakinProfileData newProfileData = m_ProfilerWindowInterface.PullFromProfiler(firstFrameDisplayIndex, lastFrameDisplayIndex, pdata_path);

        UnakinProfileAnalyzer profileAnalyzer = new UnakinProfileAnalyzer();

        List<int> selectionIndices = new List<int>( );
        for (int i = num_frames_to_analyse; i>=0; --i)
        {
            selectionIndices.Add(last - i);
        }

        List<string> threadFilters = new List<string>();
        threadFilters.Add("1:Main Thread");
        int depthFilter = UnakinProfileAnalyzer.kDepthAll;
        bool selfTimes = true;
        string parentMarker = null;
        float timeScaleMax = 0;
        string removeMarker = null;

        UnakinProfileAnalysis analysis = profileAnalyzer.Analyze(newProfileData, selectionIndices, threadFilters, depthFilter, selfTimes, parentMarker, timeScaleMax, removeMarker);
        return analysis;
    }

    void OutputAllToCSV(string filepath, UnakinProfileAnalysis analysis)
    {
        
        List<UnakinMarkerData> markers = analysis.GetMarkers();

        // create a csv dump of the markers, including the header description

        StringBuilder csvContent = new StringBuilder();
        // CSV Header
        csvContent.AppendLine("Name,NameLowerCaseHash,MsTotal,Count,CountMin,CountMax,CountMean,CountMedian,CountLowerQuartile,CountUpperQuartile," +
                              "CountStandardDeviation,LastFrame,PresentOnFrameCount,FirstFrameIndex,MsMean,MsMedian,MsLowerQuartile,MsUpperQuartile," +
                              "MsMin,MsMax,MsStandardDeviation,MinIndividualFrameIndex,MaxIndividualFrameIndex,MsMinIndividual,MsMaxIndividual,MsAtMedian," +
                              "MedianFrameIndex,MinFrameIndex,MaxFrameIndex,MinDepth,MaxDepth,Threads");

        // Add data for each marker
        foreach (UnakinMarkerData marker in markers)
        {
            csvContent.AppendLine($"{marker.name},{marker.nameLowerCaseHash},{marker.msTotal},{marker.count},{marker.countMin},{marker.countMax}," +
                                  $"{marker.countMean},{marker.countMedian},{marker.countLowerQuartile},{marker.countUpperQuartile},{marker.countStandardDeviation}," +
                                  $"{marker.lastFrame},{marker.presentOnFrameCount},{marker.firstFrameIndex},{marker.msMean},{marker.msMedian},{marker.msLowerQuartile}," +
                                  $"{marker.msUpperQuartile},{marker.msMin},{marker.msMax},{marker.msStandardDeviation},{marker.minIndividualFrameIndex}," +
                                  $"{marker.maxIndividualFrameIndex},{marker.msMinIndividual},{marker.msMaxIndividual},{marker.msAtMedian},{marker.medianFrameIndex}," +
                                  $"{marker.minFrameIndex},{marker.maxFrameIndex},{marker.minDepth},{marker.maxDepth},\"{string.Join(",", marker.threads)}\"");
        }


        filepath = Path.ChangeExtension(filepath, "csv");
        // Write to file
        File.WriteAllText(filepath, csvContent.ToString());
    }

    void OutputToCSV(string filepath, UnakinProfileAnalysis analysis, List<string> headers, int max_rows = 30)
    {
        List<UnakinMarkerData> markers = analysis.GetMarkers();

        markers.Sort((a, b) => b.msTotal.CompareTo(a.msTotal));

        StringBuilder csvContent = new StringBuilder();
        // Dynamically create CSV header based on provided headers
        csvContent.AppendLine(string.Join(",", headers));

        int row_count = 1;
        // Add data for each marker based on the headers
        foreach (UnakinMarkerData marker in markers)
        {
            List<string> rowData = new List<string>();
            foreach (string header in headers)
            {
                // Retrieve the field using reflection
                var fieldInfo = marker.GetType().GetField(header, BindingFlags.Public | BindingFlags.Instance);
                if (fieldInfo != null)
                {
                    var value = fieldInfo.GetValue(marker);
                    // Handle the case for special types, for example, arrays or lists
                    if (value is IEnumerable<object> && !(value is string))
                    {
                        rowData.Add("\"" + string.Join(",", (IEnumerable<object>)value) + "\"");
                    }
                    else
                    {
                        rowData.Add(value?.ToString());
                    }
                }
                else
                {
                    // Optionally handle or log the case where no field is found
                    rowData.Add("N/A"); // Placeholder or handle appropriately
                }
            }
            csvContent.AppendLine(string.Join(",", rowData));

            if (row_count >= max_rows)
            {
                break;
            }
            ++row_count;
        }

        filepath = Path.ChangeExtension(filepath, "csv");
        // Write to file
        File.WriteAllText(filepath, csvContent.ToString());
    }

}

#endif // UNITY_2020_1_OR_NEWER
