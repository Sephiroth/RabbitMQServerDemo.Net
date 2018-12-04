using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.MessagePatterns;
using System;
using System.Text;

namespace RabbitMQDemo
{
    public class RabbitmqServer
    {
        ConnectionFactory factory;

        IConnection conn;

        IModel channel;

        public RabbitmqServer()
        {
            factory = new ConnectionFactory();
            factory.UserName = "guest";
            factory.Password = "guest";
            factory.HostName = "127.0.0.1";
            factory.Port = 5672;
            factory.Protocol = Protocols.AMQP_0_9_1;
            factory.RequestedFrameMax = UInt32.MaxValue;
            factory.RequestedHeartbeat = UInt16.MaxValue;
        }

        public void StartUp()
        {
            try
            {
                conn = factory.CreateConnection();
                if (conn.IsOpen)
                {
                    channel = conn.CreateModel();
                    if (channel.IsOpen)
                    {
                        // 这指示通道不预取超过1个消息
                        channel.BasicQos(0, 1, false);

                        //创建一个新的，持久的交换区
                        channel.ExchangeDeclare("SISOExchange", ExchangeType.Direct, true, false, null);
                        //创建一个新的，持久的队列
                        channel.QueueDeclare("SISOqueue", true, false, false, null);
                        //绑定队列到交换区
                        channel.QueueBind("SISOqueue", "SISOExchange", "optionalRoutingKey");
                        using (var subscription = new Subscription(channel, "SISOqueue", false))
                        {
                            Console.WriteLine("等待消息...");
                            var encoding = new UTF8Encoding();
                            while (channel.IsOpen)
                            {
                                BasicDeliverEventArgs eventArgs;
                                var success = subscription.Next(2000, out eventArgs);
                                if (success == false) continue;
                                var msgBytes = eventArgs.Body;
                                var message = encoding.GetString(msgBytes);
                                Console.WriteLine(message);
                                channel.BasicAck(eventArgs.DeliveryTag, false);
                            }
                        }
                    }
                    else { Console.WriteLine("conn打开失败!"); }
                }
                else { Console.WriteLine("conn打开失败!"); }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.Read();
        }

        private void AckEvent(object obj, BasicAckEventArgs e)
        {
            Console.WriteLine("ChanelNum:" + channel.ChannelNumber);
        }


    }
}
