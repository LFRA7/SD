// Aggregator/Program.cs
using Grpc.Net.Client;
using GrpcGreeterClient;
using Microsoft.AspNetCore.Connections;
using Microsoft.EntityFrameworkCore;
using Models;
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
    private static ApplicationDbContext dbContext;

    static async Task Main(string[] args)
    {
        // Configure database context
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=SensorDataNew;Trusted_Connection=True;MultipleActiveResultSets=true");
        dbContext = new ApplicationDbContext(optionsBuilder.Options);
        await dbContext.Database.EnsureCreatedAsync();

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

            // Parse message
            var parts = message.Split(':');
            if (parts.Length != 2 || !double.TryParse(parts[1], out double value))
            {
                Console.WriteLine($"[AGREGADOR] Formato de mensagem inválido: {message}");
                return;
            }

            // Save to database
            var sensorData = new SensorData
            {
                WavyId = parts[0],
                Topic = routingKey,
                Value = value,
                Timestamp = DateTime.UtcNow,
                Processed = false
            };

            await dbContext.SensorData.AddAsync(sensorData);
            await dbContext.SaveChangesAsync();

            // Check if we have 5 unprocessed records for this topic
            var unprocessedCount = await dbContext.SensorData
                .CountAsync(s => s.Topic == routingKey && !s.Processed);

            if (unprocessedCount >= 5)
            {
                await ProcessTopicData(routingKey);
            }
        };

        await channel.BasicConsumeAsync(queue: queueName, autoAck: true, consumer: consumer);

        Console.WriteLine("A receber mensagens... Pressione Enter para sair.");
        Console.ReadLine();
    }

    static async Task ProcessTopicData(string topic)
    {
        try
        {
            // Get 5 unprocessed records
            var records = await dbContext.SensorData
                .Where(s => s.Topic == topic && !s.Processed)
                .OrderBy(s => s.Timestamp)
                .Take(5)
                .ToListAsync();

            if (records.Count < 5) return;

            // Mark records as processed
            foreach (var record in records)
            {
                record.Processed = true;
            }
            await dbContext.SaveChangesAsync();

            // Send to server
            using (var client = new TcpClient("127.0.0.1", 6000))
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
            {
                // Send DATA command
                writer.WriteLine($"DATA:{topic}");
                Console.WriteLine($"[AGREGADOR] Enviado ao SERVIDOR: DATA:{topic}");

                // Wait for server response
                var response = reader.ReadLine();
                Console.WriteLine($"[AGREGADOR] Resposta do SERVIDOR: {response}");

                // Send each record
                foreach (var record in records)
                {
                    string message = $"{record.WavyId}:{record.Value}";
                    writer.WriteLine(message);
                    Console.WriteLine($"[AGREGADOR] Enviado ao SERVIDOR: {message}");
                }

                // Send END
                writer.WriteLine("END");
                Console.WriteLine($"[AGREGADOR] Enviado ao SERVIDOR: END");

                // Wait for final confirmation
                response = reader.ReadLine();
                Console.WriteLine($"[AGREGADOR] Resposta do SERVIDOR: {response}");

                // If successfully sent, delete the records from the aggregator's database
                if (response == "100 OK") // Assuming server sends "100 OK" upon successful data reception
                {
                    dbContext.SensorData.RemoveRange(records);
                    await dbContext.SaveChangesAsync();
                    Console.WriteLine($"[AGREGADOR] Dados processados para o tópico {topic} removidos da base de dados.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AGREGADOR] Erro ao processar dados do tópico {topic}: {ex.Message}");
        }
    }
}