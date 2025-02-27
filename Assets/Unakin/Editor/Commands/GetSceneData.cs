#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Reflection;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

[System.Serializable]
public class GetSceneData : CommandBase
{
    public string outputFile = null;
    private RootNode rootNode;

    public override void InitializeFromJson(string jsonData)
    {
        JsonUtility.FromJsonOverwrite(jsonData, this);

        // true by default
        IsValid = true;

        // flag as invalid if the required data is missing
        if (outputFile == null)
            IsValid = false;
    }

    public override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew(); // Start the timer

        rootNode = new RootNode();
        rootNode.gameObjects = new List<GameObjectNode>();

        UnakinBridgeServer.DebugLogMessage("GetSceneData Start");

        foreach (var rootObj in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (rootObj == null)
                continue;

            //string name = rootObj.name ?? "Unnamed";
            //UnakinBridgeServer.DebugLogMessage($"GetSceneData LogGameObject {name}");

            var gameObjectNode = LogGameObject(rootObj);
            rootNode.gameObjects.Add(gameObjectNode);
        }

        UnakinBridgeServer.DebugLogMessage("GetSceneData Create Json Output");

        // Serialize the root node to JSON
        string jsonOutput = JsonUtility.ToJson(rootNode, true);

        UnakinBridgeServer.DebugLogMessage("GetSceneData Write Output to file");

        // Write the JSON to the output file
        //using (StreamWriter writer = new StreamWriter(outputFile, false))
        //{
        //    await writer.WriteAsync(jsonOutput);
        //    await writer.FlushAsync();
        //}
        // can't use async methods here, this can block execution
        using (StreamWriter writer = new StreamWriter(outputFile, false))
        {
            writer.Write(jsonOutput);
            writer.Flush();
        }

        UnakinBridgeServer.DebugLogMessage($"GetSceneData: Finished writing output file: {outputFile}");

        stopwatch.Stop(); // Stop the timer
        Debug.Log($"Scene data saved to {outputFile} in {stopwatch.ElapsedMilliseconds} ms");

        await Task.CompletedTask;
    }

    // Data Structures
    [Serializable]
    public class RootNode
    {
        public List<GameObjectNode> gameObjects;
    }

    [Serializable]
    public class GameObjectNode
    {
        public string id; // Add this line for the GameObject ID
        public string type; // "GameObject" or "Prefab"
        public string name;
        public List<ComponentNode> components;
        public List<GameObjectNode> children;
    }

    [Serializable]
    public class ComponentNode
    {
        public string type; // "Component"
        public string name;
        public List<MemberData> members;
    }

    [Serializable]
    public class MemberData
    {
        public string name;
        public string type;
        public string value;
    }

    private GameObjectNode LogGameObject(GameObject obj)
    {
        if (obj == null)
            return null;

        var components = LogComponents(obj) ?? new List<ComponentNode>();

        var gameObjectNode = new GameObjectNode
        {
            id = obj.GetInstanceID().ToString(),
            type = "GameObject",
            name = obj.name ?? "Unnamed",
            components = components,
            children = new List<GameObjectNode>()
        };

        if (PrefabUtility.IsAnyPrefabInstanceRoot(obj))
        {
            var prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(obj);
            string assetPath = prefabAsset != null ? AssetDatabase.GetAssetPath(prefabAsset) : string.Empty;

            if (!string.IsNullOrEmpty(assetPath) &&
                Path.GetExtension(assetPath).Equals(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                GameObject prefabContents = PrefabUtility.LoadPrefabContents(assetPath);
                if (prefabContents != null)
                {
                    var prefabNode = LogPrefabContents(prefabContents);
                    if (prefabNode != null)
                        gameObjectNode.children.Add(prefabNode);
                    PrefabUtility.UnloadPrefabContents(prefabContents);
                }
            }
            else
            {
                // Handle FBX or other non-prefab assets
                gameObjectNode.type = "Asset";
                gameObjectNode.name = (obj.name ?? "Unnamed") + " (Non-Prefab Asset)";
                gameObjectNode.components.Add(new ComponentNode
                {
                    type = "Asset",
                    name = prefabAsset != null ? prefabAsset.name : "Unknown Asset",
                    members = new List<MemberData>
                {
                    new MemberData
                    {
                        name = "Asset Path",
                        type = "String",
                        value = assetPath
                    }
                }
                });
            }
        }
        else
        {
            if (obj.transform != null)
            {
                foreach (Transform child in obj.transform)
                {
                    if (child == null)
                        continue;

                    var childNode = LogGameObject(child.gameObject);
                    if (childNode != null)
                        gameObjectNode.children.Add(childNode);
                }
            }
        }

        return gameObjectNode;
    }



    private List<ComponentNode> LogComponents(GameObject obj)
    {
        var componentNodes = new List<ComponentNode>();

        foreach (var component in obj.GetComponents<MonoBehaviour>())
        {
            if (component != null)
            {
                var componentNode = new ComponentNode
                {
                    type = "Component",
                    name = component.GetType().Name,
                    members = LogComponentMembers(component)
                };
                componentNodes.Add(componentNode);
            }
        }

        return componentNodes;
    }

    private List<MemberData> LogComponentMembers(MonoBehaviour component)
    {
        var membersList = new List<MemberData>();

        // Log public fields
        FieldInfo[] fields = component.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (var field in fields)
        {
            try
            {
                object value = field.GetValue(component);
                membersList.Add(new MemberData
                {
                    name = field.Name,
                    type = field.FieldType.Name,
                    value = ConvertToString(value)
                });
            }
            catch (Exception ex) when (ex is TargetInvocationException || ex is NotSupportedException)
            {
                // Handle exceptions if necessary
            }
        }

        // Log public properties
        PropertyInfo[] properties = component.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in properties)
        {
            if (property.CanRead)
            {
                try
                {
                    object value = property.GetValue(component);
                    membersList.Add(new MemberData
                    {
                        name = property.Name,
                        type = property.PropertyType.Name,
                        value = ConvertToString(value)
                    });
                }
                catch (Exception ex) when (ex is TargetInvocationException || ex is NotSupportedException)
                {
                    // Handle exceptions if necessary
                }
            }
        }

        return membersList;
    }

    private GameObjectNode LogPrefabContents(GameObject prefabRoot)
    {
        var prefabNode = new GameObjectNode
        {
            type = "Prefab",
            name = prefabRoot.name,
            components = LogComponents(prefabRoot),
            children = new List<GameObjectNode>()
        };

        // Log each child object recursively
        foreach (Transform child in prefabRoot.transform)
        {
            var childNode = LogGameObject(child.gameObject);
            prefabNode.children.Add(childNode);
        }

        return prefabNode;
    }

    private string ConvertToString(object obj)
    {
        if (obj == null)
            return "null";

        // Handle basic types directly
        if (obj is string || obj.GetType().IsPrimitive)
            return obj.ToString();

        // Handle Unity types
        if (obj is UnityEngine.Object unityObject)
        {
            if (unityObject == null) // Check if the Unity object is unassigned
                return "Unassigned Unity Object";

            // Return the name and instance ID of the Unity object
            return $"{unityObject.GetType().Name} (Name: {unityObject.name}, ID: {unityObject.GetInstanceID()})";
        }

        if (obj is Vector3 vector3)
            return vector3.ToString();
        if (obj is Vector2 vector2)
            return vector2.ToString();
        if (obj is Quaternion quaternion)
            return quaternion.ToString();
        if (obj is Color color)
            return color.ToString();

        // For other types, return the type name
        return obj.GetType().Name;
    }


}
#endif
