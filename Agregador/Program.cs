// Aggregator/Program.cs
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Agregador
{
    static void Main()
    {
        int listenPort = 5000;
        TcpListener listener = new TcpListener(IPAddress.Any, listenPort);
        listener.Start();
        Console.WriteLine("[AGREGADOR] A escutar na porta " + listenPort);

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            Thread t = new Thread(HandleClient);
            t.Start(client);
        }
    }

    static void HandleClient(object obj)
    {
        TcpClient wavyClient = (TcpClient)obj;
        using (NetworkStream wavyStream = wavyClient.GetStream())
        using (var wavyReader = new StreamReader(wavyStream, Encoding.UTF8))
        using (var wavyWriter = new StreamWriter(wavyStream, Encoding.UTF8) { AutoFlush = true })
        {
            try
            {
                string msg;
                while ((msg = wavyReader.ReadLine()) != null)
                {
                    Console.WriteLine($"[AGREGADOR] Recebido do WAVY: {msg}");
                    if (msg.StartsWith("HELLO:"))
                    {
                        // Responder diretamente ao WAVY
                        wavyWriter.WriteLine("100 OK");
                        wavyWriter.Flush();
                        Console.WriteLine("[AGREGADOR] Enviado ao WAVY: 100 OK");
                    }
                    else if (msg.StartsWith("DATA:"))
                    {
                        string fileName = msg.Substring(5).Trim('"');

                        string filePath = Path.Combine(@"C:\Users\lucas\source\repos\LFRA7\SD\Agregador\Data", $"Dados-{fileName}");

                        // Certifique-se de que a pasta Data existe
                        Directory.CreateDirectory(@"C:\Users\lucas\source\repos\LFRA7\SD\Agregador\Data");

                        bool shouldSendToServer = false;

                        using (var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write))
                        using (var fileWriter = new StreamWriter(fileStream, Encoding.UTF8))
                        {
                            string fileContent;
                            while ((fileContent = wavyReader.ReadLine()) != null && fileContent != "END")
                            {
                                // Escreve todos as linhas no ficheiro
                                fileWriter.WriteLine(fileContent);
                            }
                        }
                        wavyWriter.WriteLine("100 OK");
                        Console.WriteLine($"[AGREGADOR] Enviado ao WAVY: 100 OK");

                        Console.WriteLine($"[AGREGADOR] Arquivo {fileName} atualizado com novos dados.");

                        // Verifica se o arquivo tem 10 ou mais linhas
                        int lineCount = File.ReadLines(filePath).Count();
                        if (lineCount >= 10)
                        {
                            shouldSendToServer = true;
                        }

                        if (shouldSendToServer)
                        {
                            Console.WriteLine($"[AGREGADOR] Arquivo atingiu {lineCount} linhas. Enviando para o Servidor...");

                            TcpClient serverClient = new TcpClient("127.0.0.1", 6000);
                            NetworkStream serverStream = serverClient.GetStream();
                            StreamReader serverReader = new StreamReader(serverStream, Encoding.UTF8);
                            StreamWriter serverWriter = new StreamWriter(serverStream, Encoding.UTF8) { AutoFlush = true };

                            try
                            {
                                serverWriter.WriteLine($"DATA:{fileName}");
                                Console.WriteLine($"[AGREGADOR] Enviado ao SERVIDOR: DATA:{fileName}");

                                string response = serverReader.ReadLine();
                                Console.WriteLine($"[AGREGADOR] Resposta do SERVIDOR: {response}");

                                if (response == "100 OK")
                                {
                                    using (StreamReader fileReader = new StreamReader(filePath))
                                    {
                                        string line;
                                        while ((line = fileReader.ReadLine()) != null)
                                        {
                                            serverWriter.WriteLine(line);
                                            Console.WriteLine($"[AGREGADOR] Enviado ao SERVIDOR: {line}");
                                        }
                                    }
                                    serverWriter.WriteLine("END");
                                    Console.WriteLine($"[AGREGADOR] Enviado ao SERVIDOR: END");

                                    // Get final confirmation
                                    response = serverReader.ReadLine();
                                    Console.WriteLine($"[AGREGADOR] Resposta do SERVIDOR: {response}");

                                    //// Inform WAVY
                                    //wavyWriter.WriteLine("SEND_COMPLETE");
                                    //wavyWriter.Flush();
                                }
                            }
                            finally
                            {
                                // Clean up
                                serverReader.Close();
                                serverWriter.Close();
                                serverStream.Close();
                                serverClient.Close();

                            }
                        }
                    }
                    else if (msg == "QUIT")
                    {

                        // Enviar resposta 400 BYE ao WAVY
                        wavyWriter.WriteLine("400 BYE");
                        Console.WriteLine("[AGREGADOR] Enviado ao WAVY: 400 BYE");
                        Console.WriteLine("[AGREGADOR] Aguardando resposta do servidor");


                        // Enviar QUIT ao servidor
                        string serverResponse = SendToServer("QUIT");
                        Console.WriteLine($"[AGREGADOR] Resposta do SERVIDOR: {serverResponse}");

                        // Encerrar a conexão após receber 400 BYE do servidor
                        if (serverResponse == "400 BYE")
                        {
                            break;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[AGREGADOR] Comando desconhecido: {msg}");
                        wavyWriter.WriteLine("500 ERROR");
                        Console.WriteLine("[AGREGADOR] Enviado ao WAVY: 500 ERROR");
                    }
                  }
                }
            catch (Exception ex)
            {
                Console.WriteLine($"[AGREGADOR] Erro: {ex.Message}");
            }
        }
    }

    static string SendToServer(string message, TcpClient existingClient = null,
                           StreamReader existingReader = null,
                           StreamWriter existingWriter = null,
                           bool keepConnection = false)
    {
        string serverIP = "127.0.0.1";
        int serverPort = 6000;
        bool ownsConnection = existingClient == null;

        TcpClient serverClient = null;
        NetworkStream serverStream = null;
        StreamReader reader = null;
        StreamWriter writer = null;

        try
        {
            if (existingClient == null)
            {
                serverClient = new TcpClient(serverIP, serverPort);
                serverStream = serverClient.GetStream();
                reader = new StreamReader(serverStream, Encoding.UTF8);
                writer = new StreamWriter(serverStream, Encoding.UTF8) { AutoFlush = true };
            }
            else
            {
                serverClient = existingClient;
                reader = existingReader;
                writer = existingWriter;
            }

            writer.WriteLine(message);
            Console.WriteLine($"[AGREGADOR] Enviado ao SERVIDOR: {message}");
            string response = "NO_RESPONSE";
            if (message.StartsWith("DATA:") || message.StartsWith("HELLO:") || message == "QUIT" || message == "END")
            {
                response = reader.ReadLine();
                Console.WriteLine($"[AGREGADOR] Resposta do SERVIDOR: {response}");
            }
            if (keepConnection && ownsConnection)
            {
                return response;
            }
            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AGREGADOR] Erro ao comunicar com o servidor: {ex.Message}");
            return "500 ERROR";
        }
        finally
        {
            // Only close the connection if we created it and don't need to keep it
            if (ownsConnection && !keepConnection)
            {
                reader?.Close();
                writer?.Close();
                serverStream?.Close();
                serverClient?.Close();
            }
        }
    }
}