using MassTransit;
using System.Threading.Tasks;

namespace MqWebApi.Helper
{
    public interface IConsumer<in TMessage> : IConsumer where TMessage : class, IMsg
    {
        Task Consume(ConsumeContext<TMessage> context);
    }

    public interface IMsg
    {
        string OrderId { get; set; }
    }
}