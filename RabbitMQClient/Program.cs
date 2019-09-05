using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQClient
{
    class Program
    {
        static void Main(string[] args)
        {
            //建立RabbitMQ连接和通道
            var connectionFactory = new ConnectionFactory
            {
                HostName = "127.0.0.1",
                Port = 5672,
                UserName = "rbuser",
                Password = "123456",
                Protocol = Protocols.DefaultProtocol,
                AutomaticRecoveryEnabled = true, //自动重连
                RequestedFrameMax = UInt32.MaxValue,
                RequestedHeartbeat = UInt16.MaxValue //心跳超时时间
            };
            try
            {
                using (var connection = connectionFactory.CreateConnection())
                {
                    using (var channel = connection.CreateModel())
                    {
                        //创建一个新的，持久的交换区
                        channel.ExchangeDeclare("SISOExchange", ExchangeType.Direct, true, false, null);
                        //创建一个新的，持久的队列, 没有排他性，与不自动删除
                        channel.QueueDeclare("SISOqueue", true, false, false, null);
                        // 绑定队列到交换区
                        channel.QueueBind("SISOqueue", "SISOExchange", "optionalRoutingKey");

                        // 设置消息属性
                        var properties = channel.CreateBasicProperties();
                        properties.DeliveryMode = 2; //消息是持久的，存在并不会受服务器重启影响 

                        //准备开始推送
                        //发布的消息可以是任何一个(可以被序列化的)字节数组，如序列化对象，一个实体的ID，或只是一个字符串
                        var encoding = new UTF8Encoding();
                        for (var i = 0; i < 10; i++)
                        {
                            var msg = string.Format("Message(FromClient): #{0}", i + 1);
                            var msgBytes = encoding.GetBytes(msg);
                            //RabbitMQ消息模型的核心思想就是，生产者不把消息直接发送给队列。实际上，生产者在很多情况下都不知道消息是否会被发送到一个队列中。取而代之的是，生产者将消息发送到交换区。交换区是一个非常简单的东西，它一端接受生产者的消息，另一端将他们推送到队列中。交换区必须要明确的指导如何处理它接受到的消息。是放到一个队列中，还是放到多个队列中，亦或是被丢弃。这些规则可以通过交换区的类型来定义。
                            //可用的交换区类型有：direct，topic，headers，fanout。
                            //Exchange：用于接收消息生产者发送的消息，有三种类型的exchange：direct, fanout,topic，不同类型实现了不同的路由算法；
                            //RoutingKey：是RabbitMQ实现路由分发到各个队列的规则，并结合Binging提供于Exchange使用将消息推送入队列；
                            //Queue：是消息队列，可以根据需要定义多个队列，设置队列的属性，比如：消息移除、消息缓存、回调机制等设置，实现与Consumer通信；
                            channel.BasicPublish("SISOExchange", "optionalRoutingKey", properties, msgBytes);
                        }
                        channel.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("消息发布!");
            Console.ReadKey(true);
        }
    }
}
