Bindings
In previous examples we were already creating bindings. You may recall code like:

channel.QueueBind(queue: queueName,
                  exchange: "logs",
                  routingKey: "");
A binding is a relationship between an exchange and a queue. This can be simply read as: the queue is interested in messages from this exchange.

Bindings can take an extra routingKey parameter. To avoid the confusion with a BasicPublish parameter we're going to call it a binding key. This is how we could create a binding with a key:

channel.QueueBind(queue: queueName,
                  exchange: "direct_logs",
                  routingKey: "black");
The meaning of a binding key depends on the exchange type. The fanout exchanges, which we used previously, simply ignored its value.

