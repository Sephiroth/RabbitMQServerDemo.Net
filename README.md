# RabbitMQServerDemo.Net
By C#

#### 介绍
本库基于RabbitMQ.Client nuget包，旨在封装好RabbitMq使得对初次接触mq的人可以简易使用

#### 软件架构
软件架构说明 .NetFrameWork4.5和.Net Core3.0(无法添加到.NetFx的解决方案里,请到MQLogicLayer.Core文件夹里单独打开) 两个版本

#### 安装教程

#### 使用说明

1.  创建对象 <br>
RabbitMqUtil mqUtil = new RabbitMqUtil("guest", "guest", "TestExchange", "TestQueue1", "routingkey1", false, ExchangeType.Topic, false, "192.168.1.2", 5672);
<br>
<code>
 // 未找到队列而被打回的消息处理
  public void ReturnHandler(object obj, BasicReturnEventArgs args)
        {
            string rs = Encoding.UTF8.GetString(args.Body.ToArray());
            Dispatcher.Invoke(() =>
            {
                tbRcv.Text += $"{rs}发送失败;退货码:{args.ReplyCode};退货说明:{args.ReplyText} \n";
            });
        }
       // 订阅消息处理
        public void ReceiveHandler(object obj, BasicDeliverEventArgs args)
        {
            string rs = Encoding.UTF8.GetString(args.Body.ToArray());
            Dispatcher.Invoke(() =>
            {
                tbRcv.Text += $"{rs}\n";
            });
            mqClient.Ack(args.DeliveryTag);
        }
</code>


// 创建Exchange和Queue并绑定 <br>
mqUtil.InitMqCreateExchangeQueue(ReturnHandler, ReceiveHandler, true, false);<br>

// 只创建exchange用于发布消息 <br>
InitMqCreateExchange(ReturnHandler,false); <br>

// 只创建queue并绑定Exchange，用于客户端订阅消息 <br>
InitMqCreateQueue(ReturnHandler, ReceiveHandler,false,false); <br>


2.  发送消息 <br> 
// 事务发送
SendByTransaction(byte[] data, string routingKey) <br>
// confirm模式发送
SendByConfirm(byte[] data, string routingKey) <br>
// 普通发送
Send(byte[] data, string routingKey)<br>


#### 参与贡献<br>

1.  Fork 本仓库
2.  新建 Feat_xxx 分支
3.  提交代码
4.  新建 Pull Request
