using System.Collections.Generic;
using System;
using UnityEngine;
using System.Reflection;
using static UnakinBridgeServer;

public class CommandFactory
{
    private Dictionary<string, Type> _commandTypes = new Dictionary<string, Type>();

    
    public CommandFactory()
    {   
        // Register command types using reflection
        Assembly assembly = Assembly.GetAssembly(typeof(CommandBase));
        foreach (Type type in assembly.GetTypes())
        {
            if (type.IsSubclassOf(typeof(CommandBase)))
            {
                string commandName = type.Name;
                _commandTypes.Add(commandName, type);
            }
        }
    }

    [System.Serializable]
    class PeekCommandName
    {
        public string command;
        public string Id;
    }

    public CommandBase CreateCommandFromJson(string jsonData)
    {
        UnakinBridgeServer.DebugLogMessage("Entering CreateCommandFromJson");

        PeekCommandName peekCommandName = JsonUtility.FromJson<PeekCommandName>(jsonData);
        UnakinBridgeServer.DebugLogMessage("Parsed command: " + peekCommandName.command);

        if (_commandTypes.TryGetValue(peekCommandName.command, out Type commandType))
        {
            UnakinBridgeServer.DebugLogMessage("Found command type: " + commandType.FullName);
            CommandBase command = (CommandBase)Activator.CreateInstance(commandType);
            command.Id = peekCommandName.Id;
            command.InitializeFromJson(jsonData);

            if (command.IsValid)
            {
                UnakinBridgeServer.DebugLogMessage("Command is valid.");
                return command;
            }
            else
            {
                string expectedJsonData = JsonUtility.ToJson(command);
                UnakinBridgeServer.DebugLogMessage("Command is invalid. Expected JSON: " + expectedJsonData);
                throw new Exception("Required JSon data not found: " + expectedJsonData);
            }
        }
        else
        {
            UnakinBridgeServer.DebugLogMessage("Command type not found for: " + peekCommandName.command);
            foreach (var kvp in _commandTypes)
            {
                UnakinBridgeServer.DebugLogMessage("Registered command: " + kvp.Key + " => " + kvp.Value.FullName);
            }
        }
        return null;
    }
}
