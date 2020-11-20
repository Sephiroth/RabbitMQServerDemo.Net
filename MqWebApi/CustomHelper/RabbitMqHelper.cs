using MQLogicLayer.Core.RabbitMQLayer;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace MqWebApi.CustomHelper
{
    public class RabbitMqHelper : IRabbitMqHelper
    {
        private List<IConnection> connections;
        private int LastIndex;
        ConnectionFactory Factory;

        public RabbitMqHelper()
        {
            LastIndex = 0;
            connections = new List<IConnection>(4);
            Factory = new ConnectionFactory
            {
                HostName = "192.168.1.11",
                Port = 5676,
                VirtualHost = "/",
                UserName = "guest",
                Password = "guest",
                AmqpUriSslProtocols = SslProtocols.Tls,
                AutomaticRecoveryEnabled = true, //自动重连
                RequestedFrameMax = UInt32.MaxValue,
                RequestedHeartbeat = TimeSpan.FromSeconds(UInt16.MaxValue) //心跳超时时间
            };
        }

        public IConnection GetConnection()
        {
            IConnection curConn = null;
            if (connections.Count < LastIndex + 1 || connections[LastIndex] == null)
            {
                RabbitMqUtil mqUtil = new RabbitMqUtil();
                IConnection connection = mqUtil.CreateConnection(Factory);
                connections.Add(connection);
                curConn = connection;
            }
            else
            {
                curConn = connections[LastIndex];
            }
            var channel = curConn.CreateModel();
            var instance = channel.CreateBasicPublishBatch();
            return curConn;
        }
    }

    public interface IRabbitMqHelper
    {
        IConnection GetConnection();
    }
}