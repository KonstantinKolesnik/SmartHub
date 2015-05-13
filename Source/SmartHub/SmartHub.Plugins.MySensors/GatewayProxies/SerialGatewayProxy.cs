﻿using SmartHub.Plugins.MySensors.Core;
using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace SmartHub.Plugins.MySensors.GatewayProxies
{
    class SerialGatewayProxy : IGatewayProxy
    {
        #region Fields
        private SerialPort serialPort;
        private Thread thread;
        //private System.Timers.Timer timer;
        #endregion

        #region Properties
        public bool IsStarted
        {
            get { return serialPort.IsOpen; }
        }
        #endregion

        #region Events
        public event EventHandler Connected;
        public event SensorMessageEventHandler MessageReceived;
        public event EventHandler Disconnected;
        #endregion

        #region Constructor
        public SerialGatewayProxy()
        {
            serialPort = new SerialPort();
            serialPort.BaudRate = 115200;
            serialPort.DtrEnable = true;
            serialPort.Encoding = Encoding.ASCII;
            serialPort.NewLine = "\n";
            //serialPort.ReadTimeout = 10000; // !!! to let gateway initialize befero it sens a first message

            thread = new Thread(() =>
            {
                while (!IsStarted && !FindAndConnect()) ;
            });
            //thread.Priority = ThreadPriority.AboveNormal;

            //timer = new System.Timers.Timer(2000);
            //timer.AutoReset = false;
            //timer.Elapsed += (object source, ElapsedEventArgs e) =>
            //{
            //    timer.Stop();
            //    Debug.WriteLine("Timer elapsed");
            //    Debug.WriteLine("Timer disabled");

            //    if (!FindAndConnect())
            //    {
            //        Debug.WriteLine("Find failed");
            //        timer.Start();
            //        Debug.WriteLine("Timer enabled");
            //    }
            //    else
            //        Debug.WriteLine("Find OK");
            //};
        }
        #endregion

        #region Public methods
        public void Start()
        {
            thread.Start();
            //timer.Start();
        }
        public void Stop()
        {
            if (thread.IsAlive)
                thread.Join();
            //if (timer.Enabled)
            //    timer.Enabled = false;

            if (IsStarted)
            {
                serialPort.DataReceived -= serialPort_DataReceived;
                serialPort.Close();

                if (Disconnected != null)
                    Disconnected(this, EventArgs.Empty);
            }
        }

        public void Send(SensorMessage message)
        {
            if (IsStarted && message != null)
            {
                try
                {
                    serialPort.WriteLine(message.ToRawMessage());
                    Thread.Sleep(100);
                }
                catch (Exception) { }
            }
        }
        #endregion

        #region Event handlers
        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string str = null;
                while (IsStarted && !string.IsNullOrEmpty(str = serialPort.ReadLine()))
                {
                    SensorMessage msg = SensorMessage.FromRawMessage(str);
                    if (msg != null && MessageReceived != null)
                        MessageReceived(this, new SensorMessageEventArgs(msg));
                }
            }
            catch (TimeoutException) { }
            catch (IOException) { }
            catch (Exception) { }
        }
        #endregion

        #region Private methods
        private bool FindAndConnect()
        {
            if (!IsStarted)
            {
                var names = SerialPort.GetPortNames();

                foreach (string portName in names)
                {
                    serialPort.PortName = portName;

                    try
                    {
                        serialPort.Open();

                        if (IsStarted)
                        {
                            Thread.Sleep(2000); // let hardware gateway initialize

                            try
                            {
                                string str = serialPort.ReadLine();
                                SensorMessage msg = SensorMessage.FromRawMessage(str);
                                if (msg != null && msg.Type == SensorMessageType.Internal && (InternalValueType)msg.SubType == InternalValueType.GatewayReady)
                                {
                                    serialPort.DataReceived += serialPort_DataReceived;

                                    if (MessageReceived != null)
                                        MessageReceived(this, new SensorMessageEventArgs(msg));

                                    if (Connected != null)
                                        Connected(this, EventArgs.Empty);

                                    return true;
                                }
                            }
                            catch (TimeoutException)
                            {
                            }

                            serialPort.Close();
                        }
                    }
                    catch (Exception)
                    {
                        serialPort.Close();
                    }
                }

                return false;
            }
            else
                return true;
        }
        #endregion
    }
}
