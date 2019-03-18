using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading;

class Worker
{
    public static void Main()
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        using(var connection = factory.CreateConnection())
        using(var channel = connection.CreateModel())
        {
            // When RabbitMQ quits or crashes it will forget the queues and messages unless you tell it not to. 
            // Two things are required to make sure that messages aren't lost: we need to mark both the queue and messages as durable.
            channel.QueueDeclare(queue: "task_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);

            // You might have noticed that the dispatching still doesn't work exactly as we want.
            // For example in a situation with two workers, when all odd messages are heavy and even messages are light,
            // one worker will be constantly busy and the other one will do hardly any work.
            // Well, RabbitMQ doesn't know anything about that and will still dispatch messages evenly.
            // This happens because RabbitMQ just dispatches a message when the message enters the queue.
            // It doesn't look at the number of unacknowledged messages for a consumer. It just blindly dispatches every n-th message to the n-th consumer.
            // In order to change this behavior we can use the BasicQos method with the prefetchCount = 1 setting.
            // This tells RabbitMQ not to give more than one message to a worker at a time.
            // Or, in other words, don't dispatch a new message to a worker until it has processed and acknowledged the previous one.
            // Instead, it will dispatch it to the next worker that is not still busy.
            channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            Console.WriteLine(" [*] Waiting for messages.");

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body;
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine(" [x] Received {0}", message);

                int dots = message.Split('.').Length - 1;
                Thread.Sleep(dots * 1000);

                Console.WriteLine(" [x] Done in {0}", $"{dots} seconds");

                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            };
            channel.BasicConsume(queue: "task_queue", autoAck: false, consumer: consumer);

            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        }
    }
}