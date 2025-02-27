#if UNITY_2020_1_OR_NEWER

using System.Collections.Generic;
using System.Text;
using System;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine.Playables;
using System.IO;

namespace Unakin.ProfilerTools
{
    public class UnakinHierarchyFrameRowData
    {
        public int ID;
        public string Name;
        public string Total;
        public string Self;
        public int Calls;
        public string GCAlloc;
        public float TimeMS;
        public float SelfMS;
        public List<string> HeirarchyCallingStack = new List<string>();

        public List<UnakinHierarchyFrameRowData> Children = new List<UnakinHierarchyFrameRowData>();

        public UnakinHierarchyFrameRowData(HierarchyFrameDataView frameData, int ID)
        {
            this.ID = ID;
            this.Name = frameData.GetItemColumnData(ID, HierarchyFrameDataView.columnName);
            this.Total = frameData.GetItemColumnData(ID, HierarchyFrameDataView.columnTotalPercent);
            this.Self = frameData.GetItemColumnData(ID, HierarchyFrameDataView.columnSelfPercent);
            this.Calls = int.Parse(frameData.GetItemColumnData(ID, HierarchyFrameDataView.columnCalls));
            this.GCAlloc = frameData.GetItemColumnData(ID, HierarchyFrameDataView.columnGcMemory);
            this.TimeMS = frameData.GetItemColumnDataAsFloat(ID, HierarchyFrameDataView.columnTotalTime);
            this.SelfMS = frameData.GetItemColumnDataAsFloat(ID, HierarchyFrameDataView.columnSelfTime);
            //this.CallStack = frameData.ResolveItemCallstack(ID);
        }

        public void PopulateChildren(HierarchyFrameDataView frameData, List<UnakinHierarchyFrameRowData> rowDatas)
        {
            List<int> childrenIDs = new List<int>();
            frameData.GetItemChildren(ID, childrenIDs);
            foreach (int childID in childrenIDs)
            {
                UnakinHierarchyFrameRowData childRow = rowDatas.Find(x => x.ID == childID);
                if (childRow != null)
                {
                    Children.Add(childRow);
                }
            }
        }

        public static string GetHeaders()
        {
            return string.Format("Name,Total,Self,Calls,GCAlloc,TimeMS,SelfMS");
        }
        public string ToString(string indent, bool isLast, float durationThreshold)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(indent);
            if (!string.IsNullOrEmpty(indent))
            {
                sb.Append(isLast ? "└─ " : "├─ ");
            }
            sb.Append($"{Name},{Total},{Self},{Calls},{GCAlloc},{TimeMS},{SelfMS}");

            // Process children
            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i].TimeMS > durationThreshold)
                {               
                    sb.AppendLine();
                    bool isLastChild = i == Children.Count - 1;
                    string childIndent = indent + (isLast ? "    " : "│   ");
                    sb.Append(Children[i].ToString(childIndent, isLastChild, durationThreshold));
                }
            }

            return sb.ToString();
        }
    }

    public class UnakinFrameAnalysis
    {
        public List<UnakinHierarchyFrameRowData> frameRowData = new List<UnakinHierarchyFrameRowData>();
        public int frameIndex = -1;
        public float frameTimeMS = 0;
    }

    public class UnakinSingleFrameAnalysis
    {
        public static void OutputSingleFrameToCSV(string filePath, QueryFrameType queryFrameType, string functionFilter, float durationThreshold = 0.01f)
        {
            UnakinFrameAnalysis frameAnalysis = UnakinSingleFrameAnalysis.GetSingleFrameData(queryFrameType);

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                string frameSummary = $"Frame Number: {frameAnalysis.frameIndex}, FrameTime:{frameAnalysis.frameTimeMS}ms, QueryFrameType: {queryFrameType}";
                writer.WriteLine(frameSummary); // Write summary 

                writer.WriteLine(UnakinHierarchyFrameRowData.GetHeaders()); // Write headers to file
                foreach (var data in frameAnalysis.frameRowData)
                {
                    if (!string.IsNullOrEmpty(functionFilter) && !data.Name.Contains(functionFilter))
                    {
                        continue;
                    }

                    if ( data.HeirarchyCallingStack.Count==0 )
                    {
                        writer.WriteLine(data.ToString("", true, durationThreshold)); // Write each row to the file 
                    }
                }
            }
        }

        public enum QueryFrameType
        {
            MedianFrame,
            LongestFrame,
            ShortestFrame,
            LatestFrame,
        }

        private static HierarchyFrameDataView GetFrame(QueryFrameType frameType)
        {
            int unityMainThreadIndex = 0;
            int frameIndex;

            int maxRange = 100;
            int lastFrameIndexTrimmed = ProfilerDriver.lastFrameIndex - 5;
            int firstFrameIndexTrimmed = ProfilerDriver.firstFrameIndex + 5;
            int num_frames = Math.Min(maxRange, lastFrameIndexTrimmed - firstFrameIndexTrimmed);
            int frameIndexFrom = lastFrameIndexTrimmed - num_frames; // limit the search to the last maxRange frames
            int frameIndexTo = lastFrameIndexTrimmed; 
            int range = frameIndexTo - frameIndexFrom;

            switch (frameType)
            {
                case QueryFrameType.MedianFrame:
                    List<Tuple<int, float>> frameData = new List<Tuple<int, float>>();

                    // Collect frame indices and times
                    for (int i = frameIndexFrom; i <= frameIndexTo; i++)
                    {
                        float frameTime = ProfilerDriver.GetHierarchyFrameDataView(
                            i, unityMainThreadIndex, HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,
                            HierarchyFrameDataView.columnTotalTime, false).frameTimeMs;
                        frameData.Add(new Tuple<int, float>(i, frameTime));
                    }

                    // Sort frame data by frame time
                    frameData.Sort((a, b) => a.Item2.CompareTo(b.Item2));

                    // Find the median frame index
                    int medianIndex = frameData.Count / 2;
                    frameIndex = frameData[medianIndex].Item1; // Get the frame index of the median frame time
                    break;


                case QueryFrameType.LatestFrame:
                    frameIndex = ProfilerDriver.lastFrameIndex - 10;
                    break;

                case QueryFrameType.LongestFrame:
                    frameIndex = FindExtremumFrameIndex(true, unityMainThreadIndex, frameIndexFrom, frameIndexTo);  // Implement this method to find the frame index with the longest time
                    break;

                case QueryFrameType.ShortestFrame:
                    frameIndex = FindExtremumFrameIndex(false, unityMainThreadIndex, frameIndexFrom, frameIndexTo); // Implement this method to find the frame index with the shortest time
                    break;

                default:
                    throw new ArgumentException("Unsupported frame type");
            }

            return UnityEditorInternal.ProfilerDriver.GetHierarchyFrameDataView(
                        frameIndex: frameIndex,
                        threadIndex: unityMainThreadIndex,
                        viewMode: HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,
                        sortColumn: HierarchyFrameDataView.columnTotalTime,
                        sortAscending: false);
        }

        // Helper method to find the frame index with the longest or shortest frame time
        private static int FindExtremumFrameIndex(bool longest, int threadIndex, int frameIndexFrom, int frameIndexTo)
        {
            int bestIndex = frameIndexTo;
            float extremumTime = longest ? float.MinValue : float.MaxValue;
            
            for (int i = frameIndexFrom; i <= frameIndexTo; i++)
            {
                var frameTime = ProfilerDriver.GetHierarchyFrameDataView(i, threadIndex, HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName, HierarchyFrameDataView.columnTotalTime, false).frameTimeMs;
                if (longest ? frameTime > extremumTime : frameTime < extremumTime)
                {
                    extremumTime = frameTime;

                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        public static void ProcessFrameData(HierarchyFrameDataView frameData, int parentId, List<UnakinHierarchyFrameRowData> hierarchyFrameRowDatas)
        {
            // Get children of the current parent ID
            List<int> children = new List<int>();
            frameData.GetItemChildren(parentId, children);

            foreach (int childID in children)
            {
                // Create the data object for this child
                UnakinHierarchyFrameRowData hierarchyFrameRowData = new UnakinHierarchyFrameRowData(frameData, childID);

                // Get ancestors and add their data to the hierarchy calling stack
                List<int> ancestors = new List<int>();
                frameData.GetItemAncestors(childID, ancestors);
                foreach (int ancestor in ancestors)
                {
                    string data = frameData.GetItemColumnData(ancestor, 0);
                    hierarchyFrameRowData.HeirarchyCallingStack.Add(data);
                }

                // Add the current child data to the result list
                hierarchyFrameRowDatas.Add(hierarchyFrameRowData);

                // Recursive call for the current childID
                ProcessFrameData(frameData, childID, hierarchyFrameRowDatas);
            }
        }

        public static UnakinFrameAnalysis GetSingleFrameData(QueryFrameType frameType)
        {
            if (ProfilerDriver.lastFrameIndex == -1)
            {
                throw new Exception("No frames available. Run profiler first.");
            }

            UnakinFrameAnalysis frameAnalysis = new UnakinFrameAnalysis();
            
            HierarchyFrameDataView frameData = GetFrame(frameType);
            frameAnalysis.frameIndex = frameData.frameIndex;
            frameAnalysis.frameTimeMS = frameData.frameTimeMs;

            // Initial call to the recursive function
            ProcessFrameData(frameData, 0, frameAnalysis.frameRowData); // Assuming 0 is the root parent ID

            // once all setup, populate the children
            foreach (UnakinHierarchyFrameRowData hierarchyFrameRowData in frameAnalysis.frameRowData)
            {
                hierarchyFrameRowData.PopulateChildren(frameData, frameAnalysis.frameRowData);
            }

            return frameAnalysis;
        }
    }
}

#endif // UNITY_2020_1_OR_NEWER
