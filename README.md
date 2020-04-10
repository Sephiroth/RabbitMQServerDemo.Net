# RabbitMQServerDemo.Net
By C#

#### 介绍
本库基于RabbitMQ.Client nuget包，旨在封装好RabbitMq使得对初次接触mq的人可以简易使用

#### 软件架构
软件架构说明 .NetFrameWork4.5和.Net Core3.0(无法添加到.NetFx的解决方案里,请到MQLogicLayer.Core文件夹里单独打开) 两个版本

#### 安装教程

#### 使用说明

1.  创建对象 <br>
RabbitMqUtil mqUtil = new RabbitMqUtil(string username, string pwd, string exchangeName, string queueName, string routingKey, bool autoAck, string exchangeType, bool useConfirm, string host = "localhost", int port = 5672);
<br>

// 创建Exchange和Queue并绑定 <br>
InitMqCreateExchangeQueue(); <br>

// 只创建exchange用于发布消息 <br>
InitMqCreateExchange(); <br>

// 只创建queue并绑定Exchange，用于客户端订阅消息 <br>
InitMqCreateQueue(); <br>


2.  发送消息 <br> 
// d:发生的消息 <br>
// routingKey:路由 <br>
// useConfirmOnce:是否使用事务确认 <br>
 mqUtil.Send(byte[] d, string routingKey, bool useConfirmOnce) <br>

3.  订阅 <br>
mqUtil.HandleRcvData += (BasicDeliverEventArgs e)=>{ <br>
    // 处理数据 e.Body  <br>
}; <br>

#### 参与贡献<br>

1.  Fork 本仓库
2.  新建 Feat_xxx 分支
3.  提交代码
4.  新建 Pull Request
