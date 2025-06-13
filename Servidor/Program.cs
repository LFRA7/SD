// Servidor/Program.cs
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Grpc.Net.Client;
using GrpcGreeterClient2;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Models;

namespace MyApplicationNamespace
{
    class Servidor
    {
        private static ApplicationDbContext? dbContext;

        static async Task Main()
        {
            // Configure database context
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=SensorDataNew;Trusted_Connection=True;MultipleActiveResultSets=true");
            dbContext = new ApplicationDbContext(optionsBuilder.Options);
            await dbContext.Database.EnsureCreatedAsync();

            // Configuração da porta de escuta
            int port = 6000;
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine("[SERVIDOR] Servidor ligado!");

            // Thread para aceitar clientes
            Thread acceptThread = new Thread(() =>
            {
                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    // Pass the client object to HandleClient
                    Thread t = new Thread(HandleClient);
                    t.Start(client);
                }
            });
            acceptThread.IsBackground = true;
            acceptThread.Start();

            // Permite escrever comandos na consola do servidor
            Console.WriteLine("[SERVIDOR] Digite comandos aqui (digite 'exit' para sair):");
            while (true)
            {
                string? input = Console.ReadLine();
                if (string.IsNullOrEmpty(input))
                    continue;

                if (input.ToLower() == "exit")
                {
                    Console.WriteLine("[SERVIDOR] A encerrar servidor...");
                    listener.Stop();
                    break;
                }
                else if (input.ToLower().StartsWith("media "))
                {
                    await ProcessMediaCommand(input.Substring(6).Trim());
                }
                else if (input.ToLower().StartsWith("min "))
                {
                    await ProcessMinCommand(input.Substring(4).Trim());
                }
                else if (input.ToLower().StartsWith("max "))
                {
                    await ProcessMaxCommand(input.Substring(4).Trim());
                }
                else
                {
                    Console.WriteLine($"[SERVIDOR] Comando desconhecido: {input}");
                }
            }
        }

        static async Task ProcessMediaCommand(string topic)
        {
            try
            {
                if (dbContext == null)
                {
                    Console.WriteLine("[SERVIDOR] Erro: Contexto da base de dados não inicializado.");
                    return;
                }

                var values = await dbContext.SensorDataProcessed
                    .Where(s => s.Topic == topic)
                    .Select(s => s.Value)
                    .ToListAsync();

                if (!values.Any())
                {
                    Console.WriteLine($"[SERVIDOR] Nenhum dado encontrado para o tópico {topic}");
                    return;
                }

                using var channel = GrpcChannel.ForAddress("https://localhost:7220");
                var calculatorClient = new Calculator.CalculatorClient(channel);
                var reply = await calculatorClient.CalculateAverageAsync(
                    new CalculateRequest
                    {
                        FileName = topic,
                        Values = { values }
                    });

                Console.WriteLine($"[SERVIDOR] Média para {topic}: {reply.Average}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVIDOR] Erro ao processar comando media: {ex.Message}");
            }
        }

        static async Task ProcessMinCommand(string topic)
        {
            try
            {
                if (dbContext == null)
                {
                    Console.WriteLine("[SERVIDOR] Erro: Contexto da base de dados não inicializado.");
                    return;
                }

                var values = await dbContext.SensorDataProcessed
                    .Where(s => s.Topic == topic)
                    .Select(s => s.Value)
                    .ToListAsync();

                if (!values.Any())
                {
                    Console.WriteLine($"[SERVIDOR] Nenhum dado encontrado para o tópico {topic}");
                    return;
                }

                using var channel = GrpcChannel.ForAddress("https://localhost:7220");
                var calculatorClient = new Calculator.CalculatorClient(channel);
                var reply = await calculatorClient.FindMinimumAsync(
                    new CalculateRequest
                    {
                        FileName = topic,
                        Values = { values }
                    });

                Console.WriteLine($"[SERVIDOR] Mínimo para {topic}: {reply.Value}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVIDOR] Erro ao processar comando min: {ex.Message}");
            }
        }

        static async Task ProcessMaxCommand(string topic)
        {
            try
            {
                if (dbContext == null)
                {
                    Console.WriteLine("[SERVIDOR] Erro: Base de dados não inicializada.");
                    return;
                }

                var values = await dbContext.SensorDataProcessed
                    .Where(s => s.Topic == topic)
                    .Select(s => s.Value)
                    .ToListAsync();

                if (!values.Any())
                {
                    Console.WriteLine($"[SERVIDOR] Nenhum dado encontrado para o tópico {topic}");
                    return;
                }

                using var channel = GrpcChannel.ForAddress("https://localhost:7220");
                var calculatorClient = new Calculator.CalculatorClient(channel);
                var reply = await calculatorClient.FindMaximumAsync(
                    new CalculateRequest
                    {
                        FileName = topic,
                        Values = { values }
                    });

                Console.WriteLine($"[SERVIDOR] Máximo para {topic}: {reply.Value}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVIDOR] Erro ao processar comando max: {ex.Message}");
            }
        }

        static async void HandleClient(object? obj)
        {
            if (obj is not TcpClient client)
            {
                Console.WriteLine("[SERVIDOR] Erro: Objeto cliente inválido.");
                return;
            }

            using (NetworkStream stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
            {
                try
                {
                    while (true)
                    {
                        string? msg = reader.ReadLine();
                        if (string.IsNullOrEmpty(msg)) break;

                        Console.WriteLine($"[SERVIDOR] Recebido: {msg}");

                        if (msg.StartsWith("DATA:"))
                        {
                            string topic = msg.Substring(5).Trim();
                            writer.WriteLine("100 OK");

                            while (true)
                            {
                                string? data = reader.ReadLine();
                                if (data == "END") break;
                                if (string.IsNullOrEmpty(data))
                                {
                                    continue; // Skip empty lines
                                }

                                var parts = data.Split(':');
                                if (parts.Length == 2 && double.TryParse(parts[1], out double value))
                                {
                                    if (dbContext == null)
                                    {
                                        Console.WriteLine("[SERVIDOR] Erro: Contexto da base de dados não inicializado ao receber dados.");
                                        break;
                                    }

                                    var sensorDataProcessed = new SensorDataProcessed
                                    {
                                        WavyId = parts[0],
                                        Topic = topic,
                                        Value = value,
                                        Timestamp = DateTime.UtcNow
                                    };

                                    await dbContext.SensorDataProcessed.AddAsync(sensorDataProcessed);
                                }
                                else
                                {
                                    Console.WriteLine($"[SERVIDOR] Formato de dados inválido recebido: {data}");
                                }
                            }

                            if (dbContext != null)
                            {
                                await dbContext.SaveChangesAsync();
                            }
                            writer.WriteLine("100 OK");
                        }
                        else if (msg == "QUIT")
                        {
                            writer.WriteLine("400 BYE");
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SERVIDOR] Erro: {ex.Message}");
                }
                finally
                {
                    client.Close();
                }
            }
        }
    }
}