Topic exchange
Topic exchange is powerful and can behave like other exchanges.

When a queue is bound with "#" (hash) binding key - it will receive all the messages, regardless of the routing key - like in fanout exchange.

When special characters "*" (star) and "#" (hash) aren't used in bindings, the topic exchange will behave just like a direct one.
----------------------------------------------------------------
To receive all the logs:

cd ReceiveLogsTopic
dotnet run "#"
To receive all logs from the facility "kern":

cd ReceiveLogsTopic
dotnet run "kern.*"
Or if you want to hear only about "critical" logs:

cd ReceiveLogsTopic
dotnet run "*.critical"
You can create multiple bindings:

cd ReceiveLogsTopic
dotnet run "kern.*" "*.critical"
And to emit a log with a routing key "kern.critical" type:

cd EmitLogTopic
dotnet run "kern.critical" "A critical kernel error"