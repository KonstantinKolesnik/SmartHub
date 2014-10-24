﻿
namespace MySensors.Core.Nodes
{
    public class Node
    {
        public byte NodeID { get; private set; }
        public SensorType Type { get; internal set; } // ArduinoNode / ArduinoRelay
        public string Name { get; private set; }
        public string Version { get; private set; }
        public bool IsAckNeeded { get; private set; }

        public Node(byte nodeID)
        {
            NodeID = nodeID;
        }
        public Node(byte nodeID, SensorType type)
        {
            NodeID = nodeID;
            Type = type;
        }

        //public Node(byte nodeID, string name, string version, bool isAckNeeded)
        //{
        //    NodeID = nodeID;
        //    Name = name;
        //    Version = version;
        //    IsAckNeeded = isAckNeeded;
        //}
    }
}
