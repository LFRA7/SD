using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Connections;
using Microsoft.EntityFrameworkCore;
using Models;
using RabbitMQ.Client;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

class Wavy
{
    // Global ID variable
    private static string? wavyId;

    static async Task Main(string[] args)
    {
        // Get Wavy ID from user
        Console.Write("Digite o ID da Wavy: ");
        wavyId = Console.ReadLine();

        if (string.IsNullOrEmpty(wavyId))
        {
            Console.WriteLine("ID da Wavy não pode ser vazio. Saindo.");
            return;
        }

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

        Console.WriteLine("Digite os dados no formato 'temperatura valor' (ex: temperatura 20) ou 'sair' para terminar:");
        
        while (true)
        {
            string? input = Console.ReadLine();
            if (string.IsNullOrEmpty(input) || input.ToLower() == "sair")
                break;

            string[] parts = input.Split(' ');
            if (parts.Length != 2 || !double.TryParse(parts[1], out double value))
            {
                Console.WriteLine("Formato inválido. Use 'temperatura valor' (ex: temperatura 20)");
                continue;
            }

            string topic = parts[0];
            string messageWithId = $"{wavyId}:{value}";
            
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
