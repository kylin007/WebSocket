using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace WebSocket_kylin
{
    // Server助手 负责：1 握手 2 请求转换 3 响应转换  
    class ServerHelper
    {
        /// <summary>  
        /// 输出连接头信息  
        /// </summary>  
        public static string ResponseHeader(string requestHeader)
        {
            Hashtable table = new Hashtable();

            // 拆分成键值对，保存到哈希表  
            string[] rows = requestHeader.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string row in rows)
            {
                int splitIndex = row.IndexOf(':');
                if (splitIndex > 0)
                {
                    table.Add(row.Substring(0, splitIndex).Trim(), row.Substring(splitIndex + 1).Trim());
                }
            }

            StringBuilder header = new StringBuilder();
            header.Append("HTTP/1.1 101 Web Socket Protocol Handshake\r\n");
            header.AppendFormat("Upgrade: {0}\r\n", table.ContainsKey("Upgrade") ? table["Upgrade"].ToString() : string.Empty);
            header.AppendFormat("Connection: {0}\r\n", table.ContainsKey("Connection") ? table["Connection"].ToString() : string.Empty);
            header.AppendFormat("WebSocket-Origin: {0}\r\n", table.ContainsKey("Sec-WebSocket-Origin") ? table["Sec-WebSocket-Origin"].ToString() : string.Empty);
            header.AppendFormat("WebSocket-Location: {0}\r\n", table.ContainsKey("Host") ? table["Host"].ToString() : string.Empty);

            string key = table.ContainsKey("Sec-WebSocket-Key") ? table["Sec-WebSocket-Key"].ToString() : string.Empty;
            string magic = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            header.AppendFormat("Sec-WebSocket-Accept: {0}\r\n", Convert.ToBase64String(SHA1.Create().ComputeHash(Encoding.ASCII.GetBytes(key + magic))));

            header.Append("\r\n");

            return header.ToString();
        }

        /// <summary>  
        /// 解码请求内容  
        /// </summary>  
        public static string DecodeMsg(Byte[] buffer, int len)
        {
            if (buffer[0] != 0x81
                || (buffer[0] & 0x80) != 0x80
                || (buffer[1] & 0x80) != 0x80)
            {
                return null;
            }
            Byte[] mask = new Byte[4];
            int beginIndex = 0;
            int payload_len = buffer[1] & 0x7F;
            if (payload_len == 0x7E)
            {
                Array.Copy(buffer, 4, mask, 0, 4);
                payload_len = payload_len & 0x00000000;
                payload_len = payload_len | buffer[2];
                payload_len = (payload_len << 8) | buffer[3];
                beginIndex = 8;
            }
            else if (payload_len != 0x7F)
            {
                Array.Copy(buffer, 2, mask, 0, 4);
                beginIndex = 6;
            }

            for (int i = 0; i < payload_len; i++)
            {
                buffer[i + beginIndex] = (byte)(buffer[i + beginIndex] ^ mask[i % 4]);
            }
            return Encoding.UTF8.GetString(buffer, beginIndex, payload_len);
        }

        /// <summary>  
        /// 编码响应内容  
        /// </summary>  
        public static byte[] EncodeMsg(string content)
        {
            byte[] bts = null;
            byte[] temp = Encoding.UTF8.GetBytes(content); //System.Text.Encoding.Default.GetBytes(content);//Encoding.UTF8.GetBytes(content);
            if (temp.Length < 126)
            {
                bts = new byte[temp.Length + 2];
                bts[0] = 0x81;
                bts[1] = (byte)temp.Length;
                Array.Copy(temp, 0, bts, 2, temp.Length);
            }
            else if (temp.Length < 0xFFFF)
            {
                bts = new byte[temp.Length + 4];
                bts[0] = 0x81;
                bts[1] = 126;
                bts[2] = (byte)(temp.Length & 0xFF);
                bts[3] = (byte)(temp.Length >> 8 & 0xFF);
                Array.Copy(temp, 0, bts, 4, temp.Length);
            }
            else
            {
                bts = System.Text.Encoding.Default.GetBytes(string.Format("暂不处理超长内容").ToCharArray());
            }
            string str = System.Text.Encoding.Default.GetString(bts);
            return bts;
        }  
    }
}
