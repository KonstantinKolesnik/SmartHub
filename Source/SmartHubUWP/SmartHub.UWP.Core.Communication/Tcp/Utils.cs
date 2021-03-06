﻿using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace SmartHub.UWP.Core.Communication.Tcp
{
    static class Utils
    {
        private const int MAX_BUFFER_SIZE_KB = 1024;

        #region Public methods
        public static async Task<bool> ConnectAsync(StreamSocket socket, string hostName, string serviceName, int timeOut = 10000)
        {
            try
            {
                var cts = new CancellationTokenSource();
                cts.CancelAfter(timeOut);

                await socket.ConnectAsync(new HostName(hostName), serviceName).AsTask(cts.Token);
                return true;
            }
            catch (TaskCanceledException ex)
            {
            }
            catch (Exception ex)
            {
                // If this is an unknown status it means that the error is fatal and retry will likely fail.
                if (SocketError.GetStatus(ex.HResult) == SocketErrorStatus.Unknown)
                    throw;
            }

            return false;
        }
        public static async Task<string> ReceiveAsync(StreamSocket socket)
        {
            string result = null;

            if (socket != null)
                using (var reader = new DataReader(socket.InputStream))
                {
                    reader.InputStreamOptions = InputStreamOptions.Partial;

                    try
                    {
                        uint sizeFieldLength = await reader.LoadAsync(sizeof(uint));
                        if (sizeFieldLength == sizeof(uint))
                        {
                            uint dataLength = reader.ReadUInt32();

                            uint loadedDataLength = 0;
                            var sb = new StringBuilder();
                            while (loadedDataLength < dataLength)
                            {
                                var read = await reader.LoadAsync(MAX_BUFFER_SIZE_KB * 1024);
                                sb.Append(reader.ReadString(read));
                                loadedDataLength += read;
                            }

                            result = sb.ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        // If this is an unknown status it means that the error if fatal and retry will likely fail.
                        //if (SocketError.GetStatus(ex.HResult) == SocketErrorStatus.Unknown)
                        //    throw;
                    }

                    reader.DetachStream();
                }

            return result;
        }
        public static async Task<bool> SendAsync(StreamSocket socket, string data)
        {
            if (socket != null && !string.IsNullOrEmpty(data))
                using (var writer = new DataWriter(socket.OutputStream))
                {
                    try
                    {
                        writer.WriteUInt32(writer.MeasureString(data));
                        writer.WriteString(data);

                        await writer.StoreAsync();
                        await writer.FlushAsync();

                        return true;
                    }
                    catch (Exception ex)
                    {
                        // If this is an unknown status it means that the error if fatal and retry will likely fail.
                        //if (SocketError.GetStatus(ex.HResult) == SocketErrorStatus.Unknown)
                        //    throw;
                    }

                    writer.DetachStream();
                }

            return false;
        }
        #endregion
    }
}
