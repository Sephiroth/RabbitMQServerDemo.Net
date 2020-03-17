using log4net;
using MQLogicLayer.Util;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.MessagePatterns;
using System;
using System.Reflection;
using System.Text;
using System.Threading;

namespace MQLogicLayer.RabbitMQLayer
{
    public class RabbitMqUtil
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private ConnectionFactory factory;
        private IConnection conn;
        public IModel Channel;
        private EventingBasicConsumer consumer;
        private IBasicProperties properties;

        public string username { get; private set; }
        public string pwd { get; private set; }
        public int port { get; private set; }
        public string host { get; private set; }
        public bool UseConfirm { get; private set; }
        public bool AutoAck { get; private set; }

        public string Exchange { get; private set; }

        public string Queue { get; private set; }

        public string RoutingKey { get; private set; }

        /// <summary>
        /// 处理从MQ接收到的数据
        /// </summary>
        public Action<BasicDeliverEventArgs> HandleRcvData;

        public string ErrorInfo { get; private set; }

        public bool ConnectState { get; private set; }

        /// <summary>
        /// 构造器初始化参数
        /// </summary>
        /// <param name="username"></param>
        /// <param name="pwd"></param>
        /// <param name="exchangeName"></param>
        /// <param name="queueName"></param>
        /// <param name="routingKey"></param>
        /// <param name="autoAck">队列自动确认消息</param>
        /// <param name="useConfirm"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public RabbitMqUtil(string username, string pwd, string exchangeName, string queueName, string routingKey, bool autoAck, bool useConfirm, string host = "localhost", int port = 5672)
        {
            this.username = username;
            this.pwd = pwd;
            this.host = host;
            this.port = port;
            this.Exchange = exchangeName;
            this.Queue = queueName;
            this.RoutingKey = routingKey;
            this.UseConfirm = useConfirm;
            AutoAck = autoAck;
            ConnectState = false;
        }

        /// <summary>
        /// 创建Exchange和Queue并绑定
        /// </summary>
        public void InitMqCreateExchangeQueue()
        {
            if (factory != null) { return; }
            try
            {
                this.factory = new ConnectionFactory()
                {
                    HostName = host,
                    Port = port,
                    UserName = username,
                    Password = pwd,
                    Protocol = Protocols.DefaultProtocol,
                    AutomaticRecoveryEnabled = true, //自动重连
                    RequestedFrameMax = UInt32.MaxValue,
                    RequestedHeartbeat = UInt16.MaxValue //心跳超时时间
                };
                this.conn = factory.CreateConnection();
                if (this.conn.IsOpen)
                {
                    this.Channel = conn.CreateModel();
                    if (Channel.IsOpen)
                    {
                        Channel.ExchangeDeclare(Exchange, ExchangeType.Direct, false, true, null);
                        Channel.QueueDeclare(Queue, false, false, true, null);
                        Channel.QueueBind(Queue, Exchange, RoutingKey);
                        if (UseConfirm) { Channel.ConfirmSelect(); }
                        this.properties = this.Channel.CreateBasicProperties();
                        this.properties.DeliveryMode = 2; //消息是持久的，存在并不会受服务器重启影响 
                        consumer = new EventingBasicConsumer(Channel);
                        Channel.BasicConsume(Queue, AutoAck, consumer);
                        consumer.Received += MqMsgHandler;
                        #region Subscription订阅队列
                        //Thread thread = new Thread(new ThreadStart(() =>
                        //{
                        //    Subscription subscription = new Subscription(Channel, Queue);
                        //    while (true)
                        //    {
                        //        bool getRs = subscription.Next(2000, out BasicDeliverEventArgs args);
                        //        if (!getRs) { continue; }
                        //        byte[] data = args.Body;
                        //    }
                        //}));
                        #endregion
                        this.ConnectState = true;
                    }
                    else
                    {
                        this.ErrorInfo = "Channel打开失败";
                    }
                }
                else
                {
                    this.ErrorInfo = "conn打开失败";
                }
            }
            catch (Exception exp)
            {
                this.ErrorInfo = exp.Message;
                Logger.Error(exp.Message);
            }
        }

        /// <summary>
        /// 只创建exchange
        /// </summary>
        public void InitMqCreateExchange()
        {
            if (factory != null) { return; }
            try
            {
                this.factory = new ConnectionFactory()
                {
                    HostName = host,
                    Port = port,
                    UserName = username,
                    Password = pwd,
                    Protocol = Protocols.DefaultProtocol,
                    AutomaticRecoveryEnabled = true, //自动重连
                    RequestedFrameMax = UInt32.MaxValue,
                    RequestedHeartbeat = UInt16.MaxValue //心跳超时时间
                };
                this.conn = factory.CreateConnection();
                if (this.conn.IsOpen)
                {
                    this.Channel = conn.CreateModel();
                    if (Channel.IsOpen)
                    {
                        // 设置消息属性
                        Channel.ExchangeDeclare(Exchange, ExchangeType.Direct, true, false, null);
                        if (UseConfirm) { Channel.ConfirmSelect(); }
                        this.properties = this.Channel.CreateBasicProperties();
                        this.properties.DeliveryMode = 2; //消息是持久的，存在并不会受服务器重启影响 
                        this.ConnectState = true;
                    }
                    else
                    {
                        this.ErrorInfo = "Channel打开失败";
                    }
                }
                else
                {
                    this.ErrorInfo = "conn打开失败";
                }
            }
            catch (Exception exp)
            {
                this.ErrorInfo = exp.Message;
                Logger.Error(exp.Message);
            }
        }

        /// <summary>
        /// 只创建queue并绑定
        /// </summary>
        public void InitMqCreateQueue()
        {
            if (factory != null) { return; }
            try
            {
                this.factory = new ConnectionFactory()
                {
                    HostName = host,
                    Port = port,
                    UserName = username,
                    Password = pwd,
                    Protocol = Protocols.DefaultProtocol,
                    AutomaticRecoveryEnabled = true, //自动重连
                    RequestedFrameMax = UInt32.MaxValue,
                    RequestedHeartbeat = UInt16.MaxValue //心跳超时时间
                };
                this.conn = factory.CreateConnection();
                if (this.conn.IsOpen)
                {
                    this.Channel = conn.CreateModel();
                    if (Channel.IsOpen)
                    {
                        // 设置消息属性
                        Channel.QueueDeclare(Queue, false, false, true, null);//
                        Channel.QueueBind(Queue, Exchange, RoutingKey);
                        if (UseConfirm) { Channel.ConfirmSelect(); }
                        this.properties = this.Channel.CreateBasicProperties();
                        this.properties.DeliveryMode = 2; //消息是持久的，存在并不会受服务器重启影响 
                        consumer = new EventingBasicConsumer(Channel);
                        Channel.BasicConsume(Queue, AutoAck, consumer);
                        consumer.Received += MqMsgHandler;
                        this.ConnectState = true;
                    }
                    else
                    {
                        this.ErrorInfo = "Channel打开失败";
                    }
                }
                else
                {
                    this.ErrorInfo = "conn打开失败";
                }
            }
            catch (Exception exp)
            {
                this.ErrorInfo = exp.Message;
                Logger.Error(exp.Message);
            }
        }

        /// <summary>
        /// 只绑定exchange和queue,不创建
        /// </summary>
        public void InitMqNoCreate()
        {
            if (factory != null) { return; }
            try
            {
                this.factory = new ConnectionFactory()
                {
                    HostName = host,
                    Port = port,
                    UserName = username,
                    Password = pwd,
                    Protocol = Protocols.DefaultProtocol,
                    AutomaticRecoveryEnabled = true, //自动重连
                    RequestedFrameMax = UInt32.MaxValue,
                    RequestedHeartbeat = UInt16.MaxValue //心跳超时时间
                };
                this.conn = factory.CreateConnection();
                if (this.conn.IsOpen)
                {
                    this.Channel = conn.CreateModel();
                    if (Channel.IsOpen)
                    {
                        // 设置消息属性
                        this.properties = this.Channel.CreateBasicProperties();
                        this.properties.DeliveryMode = 2; //消息是持久的，存在并不会受服务器重启影响 
                        this.ConnectState = true;
                    }
                    else
                    {
                        this.ErrorInfo = "Channel打开失败";
                    }
                }
                else
                {
                    this.ErrorInfo = "conn打开失败";
                }
            }
            catch (Exception exp)
            {
                this.ErrorInfo = exp.Message;
                Logger.Error(exp.Message);
            }
        }

        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="d"></param>
        /// <param name="useConfirmOnce">是否使用事务</param>
        /// <returns></returns>
        public bool Send(byte[] d, string routingKey, bool useConfirmOnce)
        {
            bool rs = false;
            if (conn == null)
            {
                this.InitMqCreateExchange();
            }
            try
            {
                if (useConfirmOnce && !UseConfirm) { Channel.TxSelect(); }
                this.Channel.BasicPublish(Exchange, routingKey, properties, d);
                if (useConfirmOnce && !UseConfirm) { Channel.TxCommit(); }
                rs = true;
            }
            catch (Exception exp)
            {
                if (useConfirmOnce && !UseConfirm) { Channel.TxRollback(); }
                this.ErrorInfo = exp.Message;
                Logger.Error(exp.Message);
            }
            return rs;
        }

        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="frameId"></param>
        /// <param name="d"></param>
        /// <param name="useConfirmOnce">是否使用事务</param>
        /// <returns></returns>
        public bool Send(uint frameId, byte[] d, string routingKey, bool useConfirmOnce)
        {
            byte[] data = DataConvert.HandleData(frameId, d);
            return this.Send(data, routingKey, useConfirmOnce);
        }

        /// <summary>
        /// 收到通知已成功接收处理信息
        /// </summary>
        /// <param name="delivertTag">交付标志</param>
        /// <param name="multiple">是否多条消息</param>
        public void Ack(ulong delivertTag, bool multiple = false)
        {
            Channel.BasicAck(delivertTag, multiple);
        }

        /// <summary>
        /// 拒绝消息并重新排队
        /// </summary>
        /// <param name="delivertTag">交付标志</param>
        /// <param name="multiple">是否多条消息</param>
        /// <param name="requeue">是否重新排队</param>
        public void NAck(ulong delivertTag, bool multiple = false, bool requeue = true)
        {
            //Channel.BasicNack(delivertTag, multiple, requeue);
            Channel.BasicReject(delivertTag, requeue);
        }

        private void MqMsgHandler(object obj, BasicDeliverEventArgs e)
        {
            HandleRcvData?.Invoke(e);
        }
    }
}