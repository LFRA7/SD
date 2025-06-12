// Aggregator/Program.cs
using Grpc.Net.Client;
using GrpcGreeterClient;
using Microsoft.AspNetCore.Connections;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


class Agregador
{
    // Mutex para garantir acesso exclusivo aos recursos partilhados
    private static Mutex mutex = new Mutex();
    private NetworkStream stream;

    static void Main()
    {
        // Configuração da porta de escuta
        int listenPort = 5000;
        TcpListener listener = new TcpListener(IPAddress.Any, listenPort);
        listener.Start();
        Console.WriteLine("[AGREGADOR] Agregador ligado!");


        Subscribe(Environment.GetCommandLineArgs()).GetAwaiter().GetResult();

    }



    static async Task Subscribe(string[] args)
    {
        Console.Write("Digite o(s) tópico(s) a subscrever (separados por vírgula, ex: temperatura,humidade): ");
        string input = Console.ReadLine();
        var topics = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            Port = 5672,
            UserName = "guest",
            Password = "guest"
        };

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        string exchangeName = "sensores";
        await channel.ExchangeDeclareAsync(exchange: exchangeName, type: ExchangeType.Topic);

        var queueName = await channel.QueueDeclareAsync().ContinueWith(t => t.Result.QueueName);

        // Liga a queue aos tópicos
        foreach (var topic in topics)
        {
            await channel.QueueBindAsync(queue: queueName, exchange: exchangeName, routingKey: topic);
            Console.WriteLine($"[AGREGADOR] Subscrito ao tópico: {topic}");
        }

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var routingKey = ea.RoutingKey;

            Console.WriteLine($"[AGREGADOR] Recebido de '{routingKey}': {message}");

            // Enviar para o gRPC
            using var grpcChannel = GrpcChannel.ForAddress("https://localhost:7190");
            var grpcClient = new Greeter.GreeterClient(grpcChannel);
            var grpcReply = await grpcClient.SendSensorDataAsync(
                new SensorDataRequest
                {
                    Topic = routingKey,
                    Data = message
                }
            );
            Console.WriteLine($"[AGREGADOR][gRPC] Resposta: {grpcReply.Message}");

            // Guardar a resposta do RPC no ficheiro do canal
            string filePath = Path.Combine(@"C:\Users\lucas\source\repos\LFRA7\SD\Agregador\Data", $"Dados-{routingKey}.csv");
            Directory.CreateDirectory("Data");
            await File.AppendAllTextAsync(filePath, grpcReply.Message + Environment.NewLine);

            // Verificar se o ficheiro tem 40 ou mais linhas
            int lineCount = File.ReadLines(filePath).Count();
            if (lineCount >= 40)
            {
                Console.WriteLine($"[AGREGADOR] O arquivo {filePath} atingiu {lineCount} linhas. A enviar para o servidor...");

                // Lê o conteúdo do ficheiro
                var fileLines = await File.ReadAllLinesAsync(filePath);

                // Abre uma única conexão para todo o envio
                using (var client = new TcpClient("127.0.0.1", 6000))
                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                {
                    // Envia o comando DATA
                    writer.WriteLine($"DATA:{routingKey}.csv");
                    Console.WriteLine($"[AGREGADOR] Enviado ao SERVIDOR: DATA:{routingKey}.csv");

                    // Aguarda resposta do servidor
                    var response = reader.ReadLine();
                    Console.WriteLine($"[AGREGADOR] Resposta do SERVIDOR: {response}");

                    // Envia cada linha do ficheiro
                    foreach (var line in fileLines)
                    {
                        writer.WriteLine(line);
                        Console.WriteLine($"[AGREGADOR] Enviado ao SERVIDOR: {line}");
                    }

                    // Envia END
                    writer.WriteLine("END");
                    Console.WriteLine($"[AGREGADOR] Enviado ao SERVIDOR: END");

                    // Aguarda confirmação final
                    response = reader.ReadLine();
                    Console.WriteLine($"[AGREGADOR] Resposta do SERVIDOR: {response}");
                }

                // Limpa o ficheiro após envio
                File.WriteAllText(filePath, string.Empty);
            }
        };

        await channel.BasicConsumeAsync(queue: queueName, autoAck: true, consumer: consumer);

        Console.WriteLine("A receber mensagens... Pressione Enter para sair.");
        Console.ReadLine();
    }

}