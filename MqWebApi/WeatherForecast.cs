using MqWebApi.Helper;
using System;
using System.Threading.Tasks;

namespace MqWebApi
{
    public class WeatherForecast : SubmitOrderConsumer<IMsg>
    {
        public DateTime Date { get; set; }

        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        public string Summary { get; set; }
        public string OrderId { get; set; }

    }
}
