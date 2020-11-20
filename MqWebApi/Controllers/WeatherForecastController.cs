using EasyNetQ;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MqWebApi.CustomHelper;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static MQLogicLayer.Core.RabbitMQLayer.RabbitMqExtension;

namespace MqWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly IConnection mqConnection;

        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };
        private string connStr = "host=192.168.1.11;virtualHost=/;username=guest;password=guest";

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IRabbitMqHelper rabbitMq)
        {
            mqConnection = rabbitMq.GetConnection();
            _logger = logger;
        }

        //[HttpGet]
        //public IEnumerable<WeatherForecast> Get()
        //{
        //    using var channel = mqConnection.CreateModel();
        //    channel.BasicGet("TestQueue",);

        //}

        [HttpGet]
        public async Task<bool> GetMsg(string msgId)
        {
            IBusControl bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                cfg.Host(new Uri("rabbitmq://myclustername"), settings =>
                {
                    settings.Username("guest");
                    settings.Password("guest");
                    settings.UseCluster(c => { c.Node("192.169.1.11:5676"); });
                });

                cfg.ReceiveEndpoint("order-events-listener", e =>
                {
                    e.Consumer<OrderSubmittedEventConsumer>();
                });
            });


            using var channel = mqConnection.CreateModel();
            return channel.PublishByConfirm(msgId, "TestExchange", "TestKey");
        }
    }

    class OrderSubmittedEventConsumer :
            IConsumer<OrderSubmitted>
    {
        public async Task Consume(ConsumeContext<OrderSubmitted> context)
        {
        }
    }

    public class OrderSubmitted
    {
        public string OrderId;
    }
}
