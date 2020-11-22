using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;


namespace MQLogicLayer.Core.RabbitMQLayer
{
    public class RabbitMqUtil : IDisposable
    {
        public static readonly string DEAD_LETTER_EXCHANGE = "x-dead-letter-exchange";
        public static readonly string MESSAGE_TTL = "x-message-ttl";
        public static readonly string DEAD_LETTER_KEY = "x-dead-letter-routing-key";

        /// <summary>
        /// 是否完成销毁
        /// </summary>
        private bool _disposed;
        private bool UseInit;
        private List<EventingBasicConsumer> consumerList;

        private ConnectionFactory Factory;
        private IConnection Connect;
        public IModel Channel { get; private set; }
        private IBasicProperties Properties;

        #region RabbitMQ基本配置信息
        public string UserName { get; private set; }
        public string Pwd { get; private set; }
        public string Host { get; private set; }
        public int Port { get; private set; }
        public bool OpenConfirm { get; private set; }
        public bool AutoAck { get; private set; }
        /// <summary>
        /// =true时队列独占，不论是否设置持久化，断开连接时自动删除
        /// </summary>
        public bool QueueExclusive { get; set; } = false;

        public string Exchange { get; private set; }
        /// <summary>
        /// 交换器类型
        /// </summary>
        public string ExchType { get; private set; }
        public string Queue { get; private set; }
        public string RoutingKey { get; private set; }
        #endregion

        public string ErrorInfo { get; private set; }
        public bool ConnectState { get; private set; }

        #region 初始化方法
        public RabbitMqUtil()
        {
            consumerList = new List<EventingBasicConsumer>(4);
        }

        /// <summary>
        /// 1.构造器初始化参数
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
        public RabbitMqUtil(string username,
            string pwd,
            string exchangeName,
            string queueName,
            string routingKey,
            bool autoAck,
            string exchangeType,
            bool openConfirm,
            string host = "localhost",
            int port = 5672) : this()
        {
            this.UserName = username;
            this.Pwd = pwd;
            this.Host = host;
            this.Port = port;
            this.Exchange = exchangeName;
            this.Queue = queueName;
            this.RoutingKey = routingKey ?? "RoutingKey";
            this.OpenConfirm = openConfirm;
            this.ExchType = exchangeType;
            AutoAck = autoAck;
            ConnectState = false;
        }

        /// <summary>
        /// 2.连接到服务器
        /// </summary>
        /// <param name="factory">如果为空，则使用默认配置</param>
        /// <returns></returns>
        public IConnection CreateConnection(ConnectionFactory factory = null)
        {
            ConnectionFactory currentFactory = factory ?? Factory;
            currentFactory = currentFactory ?? new ConnectionFactory
            {
                HostName = Host,
                Port = Port,
                UserName = UserName,
                Password = Pwd,
                VirtualHost = "/",
                //Protocol = Protocols.DefaultProtocol,
                AmqpUriSslProtocols = SslProtocols.Tls,
                AutomaticRecoveryEnabled = true, //自动重连
                RequestedFrameMax = UInt32.MaxValue,
                RequestedHeartbeat = TimeSpan.FromSeconds(UInt16.MaxValue) //心跳超时时间
            };
            IConnection connection = currentFactory.CreateConnection();
            if (connection.IsOpen == false) { throw new Exception("conn打开失败"); }
            Factory = currentFactory;
            return connection;
        }

        public IModel CreateModel(IConnection connection = null)
        {
            connection = connection ?? Connect;
            if (connection.IsOpen == false) { throw new Exception("connection未连接到服务器"); }
            IModel channel = connection.CreateModel();
            if (channel.IsOpen == false) { throw new Exception("Channel打开失败"); }
            return channel;
        }

        /// <summary>
        /// 3.声明交换器
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="durable"></param>
        /// <param name="autoDelete"></param>
        /// <param name="arguments"></param>
        public void DeclareExchange(string exchangeName, string type, bool durable, bool autoDelete, IDictionary<string, object> arguments = null)
        {
            Channel.ExchangeDeclare(exchangeName, type, durable, autoDelete, arguments);
        }

        /// <summary>
        /// 4.声明队列
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="exchangeName">如果为空，则绑定到默认交换器</param>
        /// <param name="routingKey"></param>
        /// <param name="durable"></param>
        /// <param name="exclusive">=true时队列独占，不论是否设置持久化，断开连接时自动删除</param>
        /// <param name="autoDelete"></param>
        /// <param name="arguments"></param>
        public void DeclareQueue(string queueName, string exchangeName, string routingKey, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments = null)
        {
            Channel.QueueDeclare(queueName, durable, exclusive, autoDelete, arguments);
            if (string.IsNullOrEmpty(exchangeName)) { exchangeName = Exchange; }
            Channel.QueueBind(queueName, exchangeName, routingKey);
        }

        /// <summary>
        /// 6.添加消费者
        /// </summary>
        /// <param name="eventHandler"></param>
        /// <returns></returns>
        public string[] AddConsumer(EventHandler<BasicDeliverEventArgs> eventHandler)
        {
            EventingBasicConsumer consumer = new EventingBasicConsumer(Channel);
            consumer.Received += eventHandler;
            string consumerStr = Channel.BasicConsume(Queue, AutoAck, consumer);
            consumerList.Add(consumer);
            return consumer.ConsumerTags;
        }

        /// <summary>
        /// 删除消费者
        /// </summary>
        /// <param name="consumerTag"></param>
        /// <param name="eventHandler"></param>
        /// <returns></returns>
        public bool RemoveConsumer(string consumerTag, EventHandler<BasicDeliverEventArgs> eventHandler)
        {
            EventingBasicConsumer current = consumerList.FirstOrDefault(s => s.ConsumerTags.Contains(consumerTag));
            if (current == null) { return false; }
            Channel.BasicCancel(consumerTag);
            current.Received -= eventHandler;
            consumerList.Remove(current);
            return true;
        }

        /// <summary>
        /// 5.处理因找不到队列而被遣回的消息
        /// </summary>
        /// <param name="eventHandler"></param>
        public void AddReturnHandler(EventHandler<BasicReturnEventArgs> eventHandler)
        {
            Channel.BasicReturn += eventHandler;
        }

        public void RemoveReturnHandle(EventHandler<BasicReturnEventArgs> eventHandler)
        {
            Channel.BasicReturn -= eventHandler;
        }

        public void AddDeadLetterQueue(string deadExchangeName = "deadExchange", string deadQueueName = "deadQueue", string routingKey = "routingKey", uint queueRetry = 0, IModel model = null)
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            //args.TryAdd(DEAD_LETTER_EXCHANGE, deadExchangeName);
            //if (queueRetry > 0) { args.TryAdd(DEAD_LETTER_EXCHANGE, deadExchangeName); }
            //args.Add(DEAD_LETTER_KEY, "routingkey");
            IModel currentChannel = model ?? Channel;
            currentChannel.ExchangeDeclare(deadExchangeName, ExchangeType.Topic, true, false, args);
            currentChannel.QueueDeclare(deadQueueName, true, false, false, args);
            currentChannel.QueueBind(deadQueueName, deadExchangeName, routingKey);
        }

        /// <summary>
        /// 创建Exchange和Queue并绑定
        /// </summary>
        /// <param name="exchangeDurable">交换器持久化</param>
        /// <param name="exchangeAutoDelete">断开时是否自动删除</param>
        /// <param name="queueDurable">队列持久化</param>
        /// <param name="queueAutoDelete">断开时是否自动删除</param>
        public void InitMqCreateExchangeQueue(
            EventHandler<BasicReturnEventArgs> returnHandler,
            EventHandler<BasicDeliverEventArgs> receiveHandler,
            bool exchangeDurable = true,
            bool exchangeAutoDelete = false,
            bool queueDurable = false,
            bool queueAutoDelete = false,
            IDictionary<string, object> queueArguments = null)
        {
            if (UseInit) { throw new Exception("Connect和Channel已被初始化"); }
            Connect = Connect ?? CreateConnection();
            Channel = Channel ?? CreateModel(Connect);
            DeclareExchange(Exchange, ExchType, exchangeDurable, exchangeAutoDelete, null);
            DeclareQueue(Queue, Exchange, RoutingKey, queueDurable, QueueExclusive, queueAutoDelete, queueArguments);
            Properties = Channel.CreateBasicProperties();
            Properties.DeliveryMode = 2; //消息是持久的，存在并不会受服务器重启影响 
            AddReturnHandler(returnHandler);
            AddConsumer(receiveHandler);
            AddDeadLetterQueue();
            UseInit = true;
        }

        /// <summary>
        /// 只创建exchange
        /// </summary>
        /// <param name="exchangeDurable">交换区是否持久化</param>
        public void InitMqCreateExchange(EventHandler<BasicReturnEventArgs> returnHandler, bool exchangeDurable = false)
        {
            if (UseInit) { throw new Exception("Connect和Channel已被初始化"); }
            Connect = Connect ?? CreateConnection();
            Channel = Channel ?? CreateModel(Connect);
            // 设置消息属性
            DeclareExchange(Exchange, ExchType, exchangeDurable, true, null);
            Properties = Channel.CreateBasicProperties();
            Properties.DeliveryMode = 2; //消息是持久的，存在并不会受服务器重启影响 
            AddReturnHandler(returnHandler);
            UseInit = true;
        }

        /// <summary>
        /// 只创建queue并绑定
        /// </summary>
        /// <param name="queueDurable">队列是否持久化</param>
        /// <param name="queueAutoDelete">断开时是否自动删除</param>
        public void InitMqCreateQueue(
            EventHandler<BasicReturnEventArgs> returnHandler,
            EventHandler<BasicDeliverEventArgs> receiveHandler,
            bool queueDurable = false,
            bool queueAutoDelete = true,
            IDictionary<string, object> queueArguments = null)
        {
            if (UseInit) { throw new Exception("Connect和Channel已被初始化"); }
            Connect = Connect ?? CreateConnection();
            Channel = Channel ?? CreateModel(Connect);
            DeclareQueue(Queue, Exchange, RoutingKey, queueDurable, QueueExclusive, queueAutoDelete, queueArguments);
            // 设置消息属性
            Properties = Channel.CreateBasicProperties();
            Properties.DeliveryMode = 2; //消息是持久的，存在并不会受服务器重启影响 
            AddReturnHandler(returnHandler);
            AddConsumer(receiveHandler);
            UseInit = true;
        }

        /// <summary>
        /// 只绑定exchange和queue,不创建
        /// </summary>
        public void InitMqNoCreate(EventHandler<BasicReturnEventArgs> returnHandler)
        {
            if (UseInit) { throw new Exception("Connect和Channel已被初始化"); }
            Connect = Connect ?? CreateConnection();
            Channel = Channel ?? CreateModel(Connect);
            // 设置消息属性
            Properties = Channel.CreateBasicProperties();
            Properties.DeliveryMode = 2; //消息是持久的，存在并不会受服务器重启影响 
            AddReturnHandler(returnHandler);
            UseInit = true;
        }

        #endregion

        public void ExchangeDelete(string exchangeName, bool ifUnused = false)
        {
            Channel.ExchangeDelete(exchangeName, ifUnused);
        }

        public uint QueueDelete(string exchangeName, bool ifUnused = false, bool ifEmpty = false)
        {
            return Channel.QueueDelete(exchangeName, ifUnused, ifEmpty);
        }

        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="d"></param>
        /// <param name="useTransaction">是否使用事务</param>
        /// <returns></returns>
        private bool SendData(byte[] data, string routingKey, bool useConfirm = false, bool useTransaction = false)
        {
            if (useConfirm && useTransaction) { throw new Exception("不能同时开启Transaction模式和Confirm"); }
            bool rs = false;
            try
            {
                if (useTransaction) { Channel.TxSelect(); }
                if (useConfirm) { Channel.ConfirmSelect(); }

                Channel.BasicPublish(Exchange, routingKey, false, Properties, data);

                if (useTransaction)
                {
                    Channel.TxCommit();
                    rs = true;
                }
                if (useConfirm) { rs = Channel.WaitForConfirms(); }
                else { rs = true; }
            }
            catch (Exception exp)
            {
                if (useTransaction) { Channel.TxRollback(); }
                ErrorInfo = exp.Message;
            }
            return rs;
        }

        /// <summary>
        /// 事务发送
        /// </summary>
        /// <param name="data"></param>
        /// <param name="routingKey"></param>
        /// <returns></returns>
        public bool SendByTransaction(byte[] data, string routingKey)
        {
            return SendData(data, routingKey, false, true);
        }

        /// <summary>
        /// confirm模式发送
        /// </summary>
        /// <param name="data"></param>
        /// <param name="routingKey"></param>
        /// <returns></returns>
        public bool SendByConfirm(byte[] data, string routingKey)
        {
            return SendData(data, routingKey, true);
        }

        /// <summary>
        /// 普通发送
        /// </summary>
        /// <param name="data"></param>
        /// <param name="routingKey"></param>
        /// <returns></returns>
        public bool Send(byte[] data, string routingKey)
        {
            return SendData(data, routingKey, false, false);
        }

        /// <summary>
        /// 收到通知已成功接收处理信息
        /// </summary>
        /// <param name="delivertTag">交付标志</param>
        /// <param name="multiple">是否多条消息</param>
        public void Ack(ulong delivertTag, bool multiple = false) => Channel.BasicAck(delivertTag, multiple);

        /// <summary>
        /// 拒绝消息并重新排队
        /// </summary>
        /// <param name="delivertTag">交付标志</param>
        /// <param name="multiple">是否多条消息</param>
        /// <param name="requeue">是否重新排队</param>
        public void NAck(ulong delivertTag, bool requeue = true)//bool multiple = false,
        {
            //Channel.BasicNack(delivertTag, multiple, requeue);
            Channel.BasicReject(delivertTag, requeue);
        }

        #region 实现IDisposable接口
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) { return; }
            if (disposing)
            {
                Channel?.Dispose();
            }
            Channel = null;
            Factory = null;
            _disposed = true;
        }
        #endregion
    }

    public static class RabbitMqExtension
    {
        public static readonly string DEAD_LETTER_EXCHANGE = "x-dead-letter-exchange";
        public static readonly string MESSAGE_TTL = "x-message-ttl";
        public static readonly string DEAD_LETTER_KEY = "x-dead-letter-routing-key";

        public static IModel CreateChannel(this IConnection connection)
        {
            if (connection.IsOpen == false) { throw new Exception("connection未连接到服务器"); }
            IModel channel = connection.CreateModel();
            if (channel.IsOpen == false) { throw new Exception("Channel打开失败"); }
            return channel;
        }

        /// <summary>
        /// 声明交换区
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="exchangeName"></param>
        /// <param name="type"></param>
        /// <param name="durable"></param>
        /// <param name="autoDelete"></param>
        /// <param name="arguments"></param>
        public static void DeclareExchange(this IModel channel, string exchangeName, string type, bool durable, bool autoDelete, IDictionary<string, object> arguments = null)
        {
            channel.ExchangeDeclare(exchangeName, type, durable, autoDelete, arguments);
        }

        /// <summary>
        /// 声明队列并绑定到交换区
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="queueName"></param>
        /// <param name="exchangeName">如果为空，则绑定到默认交换器</param>
        /// <param name="routingKey"></param>
        /// <param name="durable"></param>
        /// <param name="exclusive">=true时队列独占，不论是否设置持久化，断开连接时自动删除</param>
        /// <param name="autoDelete"></param>
        /// <param name="arguments"></param>
        public static void DeclareQueue(this IModel channel, string queueName, string exchangeName, string routingKey, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments = null)
        {
            channel.QueueDeclare(queueName, durable, exclusive, autoDelete, arguments);
            channel.QueueBind(queueName, exchangeName, routingKey);
        }

        /// <summary>
        /// 添加消费者
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="queue"></param>
        /// <param name="eventHandler"></param>
        /// <param name="autoAck"></param>
        /// <returns></returns>
        public static EventingBasicConsumer AddConsumer(this IModel channel, string queue, EventHandler<BasicDeliverEventArgs> eventHandler, bool autoAck = true)
        {
            EventingBasicConsumer consumer = new EventingBasicConsumer(channel);
            consumer.Received += eventHandler;
            string consumerStr = channel.BasicConsume(queue, autoAck, consumer);
            return consumer;
        }

        /// <summary>
        /// 删除消费者
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="consumer"></param>
        /// <param name="eventHandler"></param>
        /// <returns></returns>
        public static bool RemoveConsumer(this IModel channel, EventingBasicConsumer consumer, EventHandler<BasicDeliverEventArgs> eventHandler)
        {
            string consumerTag = consumer.ConsumerTags.First();
            channel.BasicCancel(consumerTag);
            consumer.Received -= eventHandler;
            return true;
        }

        /// <summary>
        /// 处理因找不到队列而被遣回的消息
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="eventHandler"></param>
        public static void AddReturnHandler(this IModel channel, EventHandler<BasicReturnEventArgs> eventHandler)
        {
            channel.BasicReturn += eventHandler;
        }

        /// <summary>
        /// 移除方法（处理因找不到队列而被遣回的消息）
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="eventHandler"></param>
        public static void RemoveReturnHandle(this IModel channel, EventHandler<BasicReturnEventArgs> eventHandler)
        {
            channel.BasicReturn -= eventHandler;
        }

        public static void AddDeadLetterQueue(this IModel channel, string deadExchangeName = "deadExchange", string deadQueueName = "deadQueue", string routingKey = "routingKey", uint queueRetry = 0)
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            args.TryAdd(DEAD_LETTER_EXCHANGE, deadExchangeName);
            if (queueRetry > 0) { args.TryAdd(DEAD_LETTER_EXCHANGE, deadExchangeName); }
            //args.Add(DEAD_LETTER_KEY, "routingkey");
            channel.ExchangeDeclare(deadExchangeName, ExchangeType.Topic, true, false, args);
            channel.QueueDeclare(deadQueueName, true, false, false, args);
            channel.QueueBind(deadQueueName, deadExchangeName, routingKey);
        }

        /// <summary>
        /// 收到通知已成功接收处理信息
        /// </summary>
        /// <param name="delivertTag">交付标志</param>
        /// <param name="multiple">是否多条消息</param>
        public static void Ack(this IModel channel, ulong delivertTag, bool multiple = false) => channel.BasicAck(delivertTag, multiple);

        /// <summary>
        /// 拒绝消息并重新排队
        /// </summary>
        /// <param name="delivertTag">交付标志</param>
        /// <param name="multiple">是否多条消息</param>
        /// <param name="requeue">是否重新排队</param>
        public static void NAck(this IModel channel, ulong delivertTag, bool requeue = true)//bool multiple = false,
        {
            //Channel.BasicNack(delivertTag, multiple, requeue);
            channel.BasicReject(delivertTag, requeue);
        }

        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="d"></param>
        /// <param name="useTransaction">是否使用事务</param>
        /// <returns></returns>
        private static bool SendData(this IModel channel, byte[] data, string routingKey, string exchange, bool useConfirm = false, bool useTransaction = false)
        {
            if (useConfirm && useTransaction) { throw new Exception("不能同时开启Transaction模式和Confirm"); }
            bool rs = false;
            if (useTransaction) { channel.TxSelect(); }
            if (useConfirm) { channel.ConfirmSelect(); }

            channel.BasicPublish(exchange, routingKey, true, null, data);

            if (useTransaction)
            {
                channel.TxCommit();
                rs = true;
            }
            if (useConfirm) { rs = channel.WaitForConfirms(); }
            else { rs = true; }
            return rs;
        }

        /// <summary>
        /// 事务发送
        /// </summary>
        /// <param name="data"></param>
        /// <param name="routingKey"></param>
        /// <returns></returns>
        public static bool SendByTransaction(this IModel channel, byte[] data, string routingKey, string exchange)
        {
            return channel.SendData(data, routingKey, exchange, false, true);
        }

        /// <summary>
        /// confirm模式发送
        /// </summary>
        /// <param name="data"></param>
        /// <param name="routingKey"></param>
        /// <returns></returns>
        public static bool SendByConfirm(this IModel channel, byte[] data, string routingKey, string exchange)
        {
            return channel.SendData(data, routingKey, exchange, true);
        }

        public static bool PublishByConfirm(this IModel channel, object body, string exchange, string routingKey)
        {
            try
            {
                byte[] bytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(body);// .SerializeObject(body);
                channel.ConfirmSelect();
                channel.BasicPublish(exchange, routingKey, true, null, bytes);
                return channel.WaitForConfirms();
            }
            catch
            {
            }
            return false;
        }
    }

}