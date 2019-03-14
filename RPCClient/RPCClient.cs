using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class RpcClient
{
    private const string QUEUE_NAME = "rpc_queue";
    // We establish a connection and channel
    // and declare an exclusive 'callback' queue for replies.
    private readonly IConnection connection;
    private readonly IModel channel;
    private readonly string replyQueueName;
    private readonly EventingBasicConsumer consumer;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> callbackMapper =
                new ConcurrentDictionary<string, TaskCompletionSource<string>>();

    public RpcClient()
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };

        connection = factory.CreateConnection();
        channel = connection.CreateModel();
        replyQueueName = channel.QueueDeclare().QueueName;
        consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            if (!callbackMapper.TryRemove(
                    ea.BasicProperties.CorrelationId, 
                    out TaskCompletionSource<string> tcs))
                return;
            var body = ea.Body;
            var response = Encoding.UTF8.GetString(body);
            tcs.TrySetResult(response);
        };
    }

    // We establish a connection and channel and declare an exclusive 'callback' queue for replies.
    // Our Call method makes the actual RPC request.
    public Task<string> CallAsync(string message, CancellationToken cancellationToken = default(CancellationToken))
    {
      IBasicProperties props = channel.CreateBasicProperties();
      // we first generate a unique CorrelationId number and save it
      // the while loop will use this value to catch the appropriate response.
      var correlationId = Guid.NewGuid().ToString();
      // Next, we publish the request message, with two properties: ReplyTo and CorrelationId.
      props.CorrelationId = correlationId;
      props.ReplyTo = replyQueueName;
      var messageBytes = Encoding.UTF8.GetBytes(message);
      var tcs = new TaskCompletionSource<string>();
      callbackMapper.TryAdd(correlationId, tcs);

      channel.BasicPublish(
          exchange: "",
          routingKey: QUEUE_NAME,
          basicProperties: props,
          body: messageBytes);

      channel.BasicConsume(
          consumer: consumer,
          queue: replyQueueName,
          autoAck: true);

      cancellationToken.Register(() => callbackMapper.TryRemove(correlationId, out var tmp));
      return tcs.Task;
    }

    public void Close()
    {
        connection.Close();
    }
}

public class Rpc
{
  public static void Main(string[] args)
  {
      Console.WriteLine("RPC Client");
      string n = args.Length > 0 ? args[0] : "15";
      Task t = InvokeAsync(n);
      t.Wait();

      Console.WriteLine(" Press [enter] to exit.");
      Console.ReadLine();
  }

  private static async Task InvokeAsync(string n)
  {
      var rnd = new Random(Guid.NewGuid().GetHashCode());
      var rpcClient = new RpcClient();

      Console.WriteLine(" [x] Requesting fib({0})", n);
      var response = await rpcClient.CallAsync(n.ToString());
      Console.WriteLine(" [.] Got '{0}'", response);

      rpcClient.Close();
  }
}