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

namespace MyApplicationNamespace
{
    class Servidor
    {
        // Mutex para garantir acesso exclusivo ao sistema de ficheiros
        private static Mutex mutex = new Mutex();


        static void Main()
        {
            // Configuração da porta de escuta
            int port = 6000;
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine("[SERVIDOR] Servido ligado!");

            // Thread para aceitar clientes
            Thread acceptThread = new Thread(() =>
            {
                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();
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
                string input = Console.ReadLine();
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
                    // Processamento do comando media para calcular média de um arquivo
                    ProcessMediaCommand(input.Substring(6).Trim());
                }
                else if (input.ToLower().StartsWith("min "))
                {
                    // Processamento do comando media para calcular média de um arquivo
                    ProcessMinCommand(input.Substring(4).Trim());
                }
                else if (input.ToLower().StartsWith("max "))
                {
                    // Processamento do comando media para calcular média de um arquivo
                    ProcessMaxCommand(input.Substring(4).Trim());
                }
                else if (input.ToLower() == "quit")
                {
                    Console.WriteLine("[SERVIDOR] A encerrar servidor...");
                    listener.Stop();
                    break;
                }
                else
                {
                    Console.WriteLine($"[SERVIDOR] Comando inserido: {input}");
                    // Aqui pode adicionar lógica para processar outros comandos do servidor
                }
            }
        }
        static async void ProcessMinCommand(string fileName)
        {
            try
            {
                string dataPath = Path.Combine(@"C:\Users\lucas\source\repos\LFRA7\SD\Servidor\Data", fileName);

                if (!File.Exists(dataPath))
                {
                    Console.WriteLine($"[SERVIDOR] Erro: Arquivo {fileName} não encontrado.");
                    return;
                }

                Regex regex = new Regex(@"^.*?:\s*(\d+)", RegexOptions.Compiled);
                List<double> valores = new List<double>();
                using (StreamReader reader = new StreamReader(dataPath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var match = regex.Match(line);
                        if (match.Success && int.TryParse(match.Groups[1].Value, out int valor))
                        {
                            valores.Add(valor);
                        }
                    }
                }

                if (valores.Count == 0)
                {
                    Console.WriteLine("[SERVIDOR] Erro: Nenhum valor numérico encontrado no arquivo.");
                    return;
                }

                using var channel = GrpcChannel.ForAddress("https://localhost:7220");
                var calculatorClient = new Calculator.CalculatorClient(channel);
                var reply = await calculatorClient.FindMinimumAsync(
                    new CalculateRequest
                    {
                        FileName = fileName,
                        Values = { valores }
                    });

                Console.WriteLine($"[SERVIDOR] Resposta gRPC -> Menor valor: {reply.Value} ({reply.Message})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVIDOR] Erro ao processar comando min: {ex.Message}");
            }
        }

        static async void ProcessMaxCommand(string fileName)
        {
            try
            {
                string dataPath = Path.Combine(@"C:\Users\lucas\source\repos\LFRA7\SD\Servidor\Data", fileName);

                if (!File.Exists(dataPath))
                {
                    Console.WriteLine($"[SERVIDOR] Erro: Arquivo {fileName} não encontrado.");
                    return;
                }

                Regex regex = new Regex(@"^.*?:\s*(\d+)", RegexOptions.Compiled);
                List<double> valores = new List<double>();
                using (StreamReader reader = new StreamReader(dataPath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var match = regex.Match(line);
                        if (match.Success && int.TryParse(match.Groups[1].Value, out int valor))
                        {
                            valores.Add(valor);
                        }
                    }
                }

                if (valores.Count == 0)
                {
                    Console.WriteLine("[SERVIDOR] Erro: Nenhum valor numérico encontrado no arquivo.");
                    return;
                }

                using var channel = GrpcChannel.ForAddress("https://localhost:7220");
                var calculatorClient = new Calculator.CalculatorClient(channel);
                var reply = await calculatorClient.FindMaximumAsync(
                    new CalculateRequest
                    {
                        FileName = fileName,
                        Values = { valores }
                    });

                Console.WriteLine($"[SERVIDOR] Resposta gRPC -> Maior valor: {reply.Value} ({reply.Message})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVIDOR] Erro ao processar comando max: {ex.Message}");
            }
        }
        static async void ProcessMediaCommand(string fileName)
        {
            try
            {
                string dataPath = Path.Combine(@"C:\Users\lucas\source\repos\LFRA7\SD\Servidor\Data", fileName);

                if (!File.Exists(dataPath))
                {
                    Console.WriteLine($"[SERVIDOR] Erro: Arquivo {fileName} não encontrado.");
                    return;
                }

                Console.WriteLine($"[SERVIDOR] Lendo arquivo: {fileName}");

                // Regex para capturar o número inteiro após o primeiro ':'
                Regex regex = new Regex(@"^.*?:\s*(\d+)", RegexOptions.Compiled);

                List<double> valores = new List<double>();
                using (StreamReader reader = new StreamReader(dataPath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var match = regex.Match(line);
                        if (match.Success && int.TryParse(match.Groups[1].Value, out int valor))
                        {
                            valores.Add(valor);
                        }
                    }
                }

                if (valores.Count == 0)
                {
                    Console.WriteLine("[SERVIDOR] Erro: Nenhum valor numérico encontrado no arquivo.");
                    return;
                }

                // Enviar para o serviço gRPC
                using var channel = GrpcChannel.ForAddress("https://localhost:7220");
                var calculatorClient = new Calculator.CalculatorClient(channel);
                var reply = await calculatorClient.CalculateAverageAsync(
                          new CalculateRequest
                          {
                              FileName = fileName,
                              Values = { valores }
                          });

                Console.WriteLine($"[SERVIDOR] Resposta do gRPC: {reply.Message}");
                Console.WriteLine($"[SERVIDOR] Média calculada pelo serviço: {reply.Average}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVIDOR] Erro ao processar comando media: {ex.Message}");
            }
        }

        static async void HandleClient(object obj)
        {
            TcpClient client = (TcpClient)obj;
            using (NetworkStream stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
            {
                try
                {
                    while (true)
                    {
                        string msg = reader.ReadLine();
                        if (string.IsNullOrEmpty(msg)) break;

                        Console.WriteLine($"[SERVIDOR] Recebido: {msg}");

                        // Processamento do comando HELLO para confirmação de conexão
                        if (msg.StartsWith("HELLO:"))
                        {
                            writer.WriteLine("100 OK");
                            Console.WriteLine("[SERVIDOR] Enviado: 100 OK");
                        }
                        // Processamento do comando DATA para receber dados
                        else if (msg.StartsWith("DATA:"))
                        {
                            // Usa um mutex para garantir acesso exclusivo aos ficheiros
                            mutex.WaitOne();

                            try
                            {
                                // Extrai o nome do ficheiro e cria um novo nome com o horário atual
                                string fileName = msg.Substring(5).Trim();
                                string currentDate = DateTime.Now.ToString("dd-MM-yyyy");
                                string currentTime = DateTime.Now.ToString("HH-mm-ss");
                                string newFileName = $"Dados-{currentDate}_{currentTime}-{fileName}";
                                string filePath = Path.Combine(@"C:\Users\lucas\source\repos\LFRA7\SD\Servidor\Data", newFileName);
                                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                                Console.WriteLine($"[SERVIDOR] A Guardar {newFileName}...");

                                // Envia confirmação de que está pronto para receber dados
                                writer.WriteLine("100 OK");
                                Console.WriteLine("[SERVIDOR] Enviado: 100 OK (pronto para receber dados)");

                                // Guarda os dados recebidos no ficheiro
                                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                                {
                                    string fileContent;
                                    while ((fileContent = reader.ReadLine()) != null)
                                    {
                                        if (fileContent == "END") break; // Finaliza quando recebe o marcador END

                                        // Converte o conteúdo para bytes e escreve no ficheiro
                                        byte[] contentBytes = Encoding.UTF8.GetBytes(fileContent + Environment.NewLine);
                                        fileStream.Write(contentBytes, 0, contentBytes.Length);
                                        fileStream.Flush();

                                        Console.WriteLine($"[SERVIDOR] Gravado: {fileContent}");
                                    }
                                }

                                // Confirma que os dados foram guardados com sucesso
                                writer.WriteLine("100 OK");
                                Console.WriteLine($"[SERVIDOR] O Arquivo {newFileName} foi guardado com sucesso.");
                            }
                            finally
                            {
                                // Liberta o mutex após processar os dados
                                mutex.ReleaseMutex();
                            }
                        }
                        // Processamento do comando QUIT para terminar a conexão
                        else if (msg == "QUIT")
                        {
                            writer.WriteLine("400 BYE");
                            Console.WriteLine("[SERVIDOR] Enviado: 400 BYE");
                            break; // Sai do ciclo para encerrar a conexão
                        }
                    }
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"[SERVIDOR] Erro de conexão: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SERVIDOR] Erro inesperado: {ex.Message}");
                }
                finally
                {
                    client.Close(); // Garante que o cliente seja fechado ao sair
                }
            }
        }

    }
}