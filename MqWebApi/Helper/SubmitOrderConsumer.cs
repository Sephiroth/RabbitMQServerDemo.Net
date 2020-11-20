using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MqWebApi.Helper
{
    public class SubmitOrderConsumer<T> :  IConsumer<T> where T : class, IMsg
    {
        public async Task Consume(ConsumeContext<T> context)
        {
            await context.Publish<T>(new { context.Message.OrderId });
        }
    }
}