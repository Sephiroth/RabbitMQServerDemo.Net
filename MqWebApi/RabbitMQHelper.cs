using MassTransit;
using MassTransit.RabbitMqTransport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MqWebApi
{
    public class RabbitMQHelper
    {
        /// <summary>
        /// 创建连接对象
        /// 不对外公开
        /// </summary>
        //private static IBusControl CreateBus(Action<IRabbitMqBusFactoryConfigurator, IRabbitMqHost> registrationAction = null)
        //{
        //    //通过MassTransit创建MQ联接工厂
        //    Bus.Factory.CreateUsingRabbitMq(cfg =>
        //    {
        //        cfg.ReceiveEndpoint("order-service", e =>
        //        {
        //            e.Consumer<SubmitOrderConsumer>();
        //        });
        //    });
        //}
    }
}