using System.Collections.Generic;
using System.Text;

namespace MySensors.Controllers.Communication
{
    class NetworkMessageDecoder
    {
        #region Fields
        private string buffer = "";
        #endregion

        #region Public methods
        public List<NetworkMessage> Decode(byte[] rawData)
        {
            string s;
            try
            {
                s = new string(Encoding.UTF8.GetChars(rawData));
            }
            catch
            {
                return null;
            }

            return Decode(s);
        }
        public List<NetworkMessage> Decode(string rawData)
        {
            List<NetworkMessage> msgs = new List<NetworkMessage>();

            buffer += rawData;

            string payload = null;
            do
            {
                payload = FindPayload();
                if (payload != null)
                {
                    NetworkMessage msg = NetworkMessage.FromString(payload);
                    if (msg != null)
                        msgs.Add(msg);
                }
            }
            while (payload != null);

            return msgs;
        }
        #endregion

        #region Private methods
        //private string FindPayload()
        //{
        //    int a = buffer.IndexOf(NetworkMessageDelimiters.BOM);
        //    int b = buffer.IndexOf(NetworkMessageDelimiters.EOM);

        //    if (a != -1 && b != -1) // there's a msg inside of s
        //    {
        //        // ccccc<BOM>ccccccccc<EOM>ccccc
        //        //      a             b

        //        string data = buffer.Substring(a + NetworkMessageDelimiters.BOM.Length, b - a - NetworkMessageDelimiters.BOM.Length);
        //        buffer = buffer.Substring(b + NetworkMessageDelimiters.EOM.Length);

        //        return data;
        //    }

        //    return null;
        //}
        private string FindPayload()
        {
            int a = buffer.IndexOf("\n");

            if (a != -1) // there's a msg inside of s
            {
                string data = buffer.Substring(0, a);
                buffer = buffer.Substring(a + 1);

                return data;
            }

            return null;
        }
        #endregion
    }
}
