﻿using NAudio.Wave;
//using NLog;

namespace SmartHub.UWP.Plugins.Audio
{
    /// <summary>
    /// Stream for looping playback
    /// </summary>
    public class LoopStream : WaveStream, IPlayback
    {
        #region Fields
        //private readonly Logger log;
        private readonly WaveStream sourceStream;
        private bool stop;
        #endregion

        #region Properties
        public int Loop
        {
            get; private set;
        }
        public override WaveFormat WaveFormat
        {
            get { return sourceStream.WaveFormat; }
        }
        public override long Length
        {
            get { return sourceStream.Length; }
        }
        public override long Position
        {
            get { return sourceStream.Position; }
            set { sourceStream.Position = value; }
        }
        #endregion

        #region Constructor
        public LoopStream(/*Logger log, */WaveStream sourceStream, int loop = 1)
        {
            //this.log = log;
            this.sourceStream = sourceStream;
            Loop = loop;
        }
        #endregion

        #region Public methods
        public void Stop()
        {
            //log.Debug("Stop sound request");
            stop = true;
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;

            while (totalBytesRead < count && Loop > 0)
            {
                if (stop)
                {
                    //log.Debug("Stop sound");
                    return 0;
                }

                int bytesRead = sourceStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);

                if (bytesRead == 0)
                {
                    if (sourceStream.Position == 0 && Loop <= 0)
                    {
                        //log.Debug("End of file");
                        break;
                    }

                    // loop
                    Loop--;
                    sourceStream.Position = 0;
                }

                totalBytesRead += bytesRead;
            }

            return totalBytesRead;
        }
        #endregion
    }
}
