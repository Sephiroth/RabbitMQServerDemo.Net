using log4net;
using MQLogicLayer.Util;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Reflection;
using System.Text;

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

        public string username;
        public string pwd;
        public int port;
        public string host;

        private UTF8Encoding encoding;

        public string Exchange { get; private set; }

        public string Queue { get; private set; }

        public string RoutingKey { get; set; }

        /// <summary>
        /// 处理从MQ接收到的数据
        /// </summary>
        public event EventHandler<byte[]> HandleRcvData;

        public string ErrorInfo { get; private set; }

        public bool ConnectState { get; private set; }

        /// <summary>
        /// 构造器初始化参数
        /// </summary>
        /// <param name="username"></param>
        /// <param name="pwd"></param>
        /// <param name="exchangeName"></param>
        /// <param name="queueName"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public RabbitMqUtil(string username, string pwd, string exchangeName, string queueName,string routingKey, string host = "localhost", int port = 5672)
        {
            this.username = username;
            this.pwd = pwd;
            this.host = host;
            this.port = port;
            this.Exchange = exchangeName;
            this.Queue = queueName;
            this.encoding = new UTF8Encoding();
            this.RoutingKey = routingKey;
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
                        Channel.BasicAck(2, true);
                        consumer = new EventingBasicConsumer(Channel);
                        Channel.BasicConsume(Queue, true, consumer);
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
                        Channel.BasicAck(2, true);
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
                        Channel.BasicAck(2, true);
                        consumer = new EventingBasicConsumer(Channel);
                        Channel.BasicConsume(Queue, true, consumer);
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

        public bool Send(byte[] d)
        {
            bool rs = false;
            if (conn == null)
            {
                this.InitMqCreateExchange();
            }
            try
            {
                this.Channel.BasicPublish(Exchange, RoutingKey, properties, d);
                rs = true;
            }
            catch (Exception exp)
            {
                this.ErrorInfo = exp.Message;
                Logger.Error(exp.Message);
            }
            return rs;
        }

        public bool Send(uint frameId, byte[] d)
        {
            byte[] data = DataConvert.HandleData(frameId, d);
            return this.Send(data);
        }

        private void MqMsgHandler(object obj, BasicDeliverEventArgs e)
        {
            HandleRcvData?.Invoke(obj, e.Body);
        }
    }
}