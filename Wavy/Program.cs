using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Connections;
using RabbitMQ.Client;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

class Wavy
{
    // Global ID variable
    private static string wavyId;

    static async Task Main(string[] args)
    {
        // Get Wavy ID from user
        Console.Write("Digite o ID da Wavy: ");
        wavyId = Console.ReadLine();

        Console.Write("Digite o nome do ficheiro de dados (ex: Vento.csv): ");
        string fileName = Console.ReadLine();
        string filePath = Path.Combine(@"C:\Users\lucas\source\repos\LFRA7\SD\Wavy\Data", fileName);

        if (!File.Exists(filePath))
        {
            Console.WriteLine("Ficheiro não encontrado.");
            return;
        }

        string topic = Path.GetFileNameWithoutExtension(fileName);

        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            Port = 5672,
            UserName = "guest",
            Password = "guest"
        };

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(exchange: "sensores", type: ExchangeType.Topic);

        foreach (var line in File.ReadLines(filePath))
        {
            // Prepend the Wavy ID to the message
            string messageWithId = $"{wavyId}:{line}";
            var body = Encoding.UTF8.GetBytes(messageWithId);
            await channel.BasicPublishAsync(
                exchange: "sensores",
                routingKey: topic,
                body: body
            );
            Console.WriteLine($"[WAVY {wavyId}] Publicado em '{topic}': {messageWithId}");
        }

        Console.WriteLine("Envio concluído. Pressione Enter para sair.");
        Console.ReadLine();
    }
}
