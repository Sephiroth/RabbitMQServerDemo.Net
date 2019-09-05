using MQLogicLayer.RabbitMQLayer;
using Org.BouncyCastle.Utilities.Encoders;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

        private void Init()
        {
            mqClient = new RabbitMqUtil("rbuser", "123456", "abc", "aaa", "key", false, false, "localhost", 5672);
            mqClient.HandleRcvData += (BasicDeliverEventArgs e) =>
            {
                string s = Encoding.UTF8.GetString(e.Body);
                Dispatcher.Invoke(() =>
                {
                    tbRcv.Text = string.Format("{0}\r \n{1}", s, tbRcv.Text);//Hex.ToHexString(e.Body)
                });
                if (s.EndsWith("0"))
                {
                    mqClient.NAck(e.DeliveryTag, false, false);
                }
                else
                {
                    mqClient.Ack(e.DeliveryTag);
                }
            };
            mqClient.InitMqCreateExchangeQueue();
            if (mqClient.ConnectState == false)
            {
                string s = mqClient.ErrorInfo;
            }
        }

        private void getDataBtnClick(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 10; i++)
            {
                mqClient.Send(Encoding.UTF8.GetBytes(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")), "key", false);
            }
        }

        private void sendDataBtnClick(object sender, RoutedEventArgs e)
        {
            mqClient.Send(Encoding.UTF8.GetBytes(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")), "key", false);
        }

        private void windowsLoaded(object sender, RoutedEventArgs e)
        {
            Init();
        }
    }
}