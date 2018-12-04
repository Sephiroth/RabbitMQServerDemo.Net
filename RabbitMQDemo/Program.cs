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
            RabbitmqServer serv = new RabbitmqServer();
            serv.StartUp();

        }
    }
}
