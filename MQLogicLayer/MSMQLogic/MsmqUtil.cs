using MQLogicLayer.Util;
using System;
using System.Messaging;

namespace MQLogicLayer.MSMQLogic
{
    public class MsmqUtil
    {
        public MessageQueue MSMQ;

        /// <summary>
        /// 全限定名称
        /// </summary>
        private string queueName;

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorInfo { get; private set; }

        /// <summary>
        /// MsmqUtil构造器
        /// </summary>
        /// <param name="queueName">队列全限定名称</param>
        /// <param name="isRemote">是否是远程队列</param>
        /// <param name="isCreate">删除还是重新创建</param>
        public MsmqUtil(string queueName, bool isRemote, bool isCreate = false)
        {
            this.queueName = queueName;
            if (isRemote) { isCreate = false; } // 如果是远程队列，不允许删除重新创建
            this.InitMq(isRemote, isCreate);
        }

        /// <summary>
        /// 初始化msmq对象
        /// </summary>
        /// <param name="isRemote">是否是远程队列</param>
        /// <param name="isCreate">删除还是重新创建</param>
        public void InitMq(bool isRemote, bool isCreate)
        {
            #region 远程队列
            if (isRemote)
            {
                MSMQ = new MessageQueue(queueName);
                return;
            }
            #endregion
            #region 本地队列
            else
            {
                MessageQueue[] mqList = MessageQueue.GetPrivateQueuesByMachine(System.Environment.MachineName);
                if (mqList.Length > 0)
                {
                    foreach (var mq in mqList)
                    {
                        if (mq.FormatName.Contains(queueName))
                        {
                            if (isCreate)
                            {
                                MessageQueue.Delete(queueName);
                                MSMQ = MessageQueue.Create(queueName, false);
                            }
                            else { MSMQ = new MessageQueue(queueName); }
                            return;
                        }
                    }
                    MSMQ = MessageQueue.Create(queueName, false);
                }
                else { MSMQ = MessageQueue.Create(queueName, false); }
            }
            #endregion
        }

        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool Send(object obj)
        {
            bool rs = false;
            try
            {
                if (obj != null)
                {
                    MSMQ.Send(obj);
                    rs = true;
                    ErrorInfo = null;
                }
            }
            catch (Exception e) { ErrorInfo = e.Message; }
            return rs;
        }

        public bool Send(uint frameID, byte[] d)
        {
            bool rs = false;
            try
            {
                MSMQ.Send(DataConvert.HandleData(frameID, d));
                rs = true;
            }
            catch { }
            return rs;
        }

        public object Receive()
        {
            object rs = null;
            Message msg = MSMQ.Receive();
            if (msg != null)
            {
                rs = msg.Body;
            }
            return rs;
        }
    }
}
