﻿using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace MySensors.Core
{
    public class Sensor : ObservableObject
    {
        #region Fields
        private byte nodeID;
        private byte id;
        private SensorType type;
        private string protocolVersion = "";
        private ObservableCollection<SensorValue> values = new ObservableCollection<SensorValue>();
        #endregion

        #region Properties
        public byte NodeID
        {
            get { return nodeID; }
            private set
            {
                if (nodeID != value)
                {
                    nodeID = value;
                    NotifyPropertyChanged("NodeID");
                }
            }
        }
        public byte ID
        {
            get { return id; }
            private set
            {
                if (id != value)
                {
                    id = value;
                    NotifyPropertyChanged("ID");
                }
            }
        }
        public SensorType Type
        {
            get { return type; }
            set
            {
                if (type != value)
                {
                    type = value;
                    NotifyPropertyChanged("Type");
                }
            }
        }
        public string ProtocolVersion // library version
        {
            get { return protocolVersion; }
            set
            {
                if (protocolVersion != value)
                {
                    protocolVersion = value;
                    NotifyPropertyChanged("ProtocolVersion");
                }
            }
        }
        public ObservableCollection<SensorValue> Values
        {
            get { return values; }
        }

        public SensorValue Value
        {
            get { return values != null && values.Count != 0 ? values.Where(v => v.Time == values.Select(vv => vv.Time).Max()).FirstOrDefault() : null; }
        }
        #endregion

        #region Constructors
        public Sensor(byte nodeID, byte id)
        {
            NodeID = nodeID;
            ID = id;

            values.CollectionChanged += values_CollectionChanged;
        }
        public Sensor(byte nodeID, byte id, SensorType type, string protocolVersion)
        {
            NodeID = nodeID;
            ID = id;
            Type = type;
            ProtocolVersion = protocolVersion;

            values.CollectionChanged += values_CollectionChanged;
        }
        #endregion

        #region Event handlers
        private void values_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("Value");
        }
        #endregion
    }
}