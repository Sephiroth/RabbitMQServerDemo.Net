using MQLogicLayer.RabbitMQLayer;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Linq;
using System.Text;
using System.Windows;

namespace RabbitMQClient_Test
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private RabbitMqUtil mqClient;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void windowsLoaded(object sender, RoutedEventArgs e)
        {
            mqClient = new RabbitMqUtil("guest", "guest", "TestExchange", "TestQueue1", "routingkey1", false, ExchangeType.Topic, false, "192.168.194.128", 5673);
            mqClient.InitMqCreateExchangeQueue(ReturnHandler, ReceiveHandler, true, false);
        }

        private void sendDataBtnClick(object sender, RoutedEventArgs e)
        {
            mqClient.SendByConfirm(Encoding.UTF8.GetBytes(DateTime.Now.ToString()), "routingkey1");
        }

        private void getDataBtnClick(object sender, RoutedEventArgs e)
        {

        }

        public void ReturnHandler(object obj, BasicReturnEventArgs args)
        {
            string rs = Encoding.UTF8.GetString(args.Body.ToArray());
            Dispatcher.Invoke(() =>
            {
                tbRcv.Text += $"{rs}发送失败;退货码:{args.ReplyCode};退货说明:{args.ReplyText} \n";
            });
        }

        public void ReceiveHandler(object obj, BasicDeliverEventArgs args)
        {
            string rs = Encoding.UTF8.GetString(args.Body.ToArray());
            Dispatcher.Invoke(() =>
            {
                tbRcv.Text += $"{rs}\n";
            });
            mqClient.Ack(args.DeliveryTag);
        }
    }
}