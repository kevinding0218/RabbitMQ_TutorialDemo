using System;
using RabbitMQ.Client;
using System.Text;
using System.Threading;

class NewTask
{
    public static void Main(string[] args)
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            // When RabbitMQ quits or crashes it will forget the queues and messages unless you tell it not to. 
            // Two things are required to make sure that messages aren't lost: we need to mark both the queue and messages as durable.
            channel.QueueDeclare(queue: "task_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);

            // var message = GetMessage(args);
            foreach (var arg in args)
            {
                Random rnd = new Random();
                int taskLength = rnd.Next(1, 11);
                string taskLengthPresent = ".".Repeat(taskLength);
                string message = $"{arg}{taskLengthPresent}";

                var body = Encoding.UTF8.GetBytes(message);

                // At this point we're sure that the task_queue queue won't be lost even if RabbitMQ restarts. 
                // Now we need to mark our messages as persistent - by setting IBasicProperties.SetPersistent to true
                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;

                // Marking messages as persistent doesn't fully guarantee that a message won't be lost.
                // Although it tells RabbitMQ to save the message to disk, there is still a short time window when RabbitMQ has accepted a message and hasn't saved it yet.
                // Also, RabbitMQ doesn't do fsync(2) for every message -- it may be just saved to cache and not really written to the disk.
                //  The persistence guarantees aren't strong, but it's more than enough for our simple task queue.
                // If you need a stronger guarantee then you can use publisher confirms.

                channel.BasicPublish(exchange: "", routingKey: "task_queue", basicProperties: properties, body: body);
                Console.WriteLine(" [x] Sent {0}", $"{arg} with {taskLength} seconds");
                Thread.Sleep(1000);
            }
        }

        Console.WriteLine(" Press [enter] to exit.");
        Console.ReadLine();
    }

    private static string GetMessage(string[] args)
    {
        return ((args.Length > 0) ? string.Join(" ", args) : "Hello World!");
    }
}