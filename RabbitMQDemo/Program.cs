using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;

namespace RabbitMQDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            MQLogicLayer.RabbitMQLayer.RabbitMqUtil mq = new MQLogicLayer.RabbitMQLayer.RabbitMqUtil("rbuser", "123456", "abc", "aaa", "key", true, false, "localhost", 5672);
            mq.HandleRcvData += (BasicDeliverEventArgs e) =>
            {
                Console.WriteLine(Encoding.UTF8.GetString(e.Body));
            };
            mq.InitMqNoCreate();
            if (mq.ConnectState == false)
            {
                string s = mq.ErrorInfo;
            }
        }
    }
}