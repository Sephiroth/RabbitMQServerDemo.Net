using System;

namespace MQLogicLayer.Util
{
    public class DataConvert
    {
        /// <summary>
        /// 把报文和帧id拼接在一起
        /// </summary>
        /// <param name="frameID"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] HandleData(uint frameID, byte[] data)
        {
            byte[] cache = new byte[data.Length + 1];
            Array.Copy(data, 0, cache, 0, data.Length);
            cache[cache.Length - 1] = Convert.ToByte(frameID);
            return cache;
        }

        /// <summary>
        /// 把queue数据转成can报文
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Tuple<byte, byte[]> DataToCan(byte[] data)
        {
            if (data == null) { return null; }
            if (data.Length != 9) { return null; }
            byte frameID = data[data.Length - 1];
            byte[] cmd = new byte[8];
            Array.Copy(data, 0, cmd, 0, 8);
            return Tuple.Create<byte, byte[]>(frameID, cmd);
        }
    }
}
