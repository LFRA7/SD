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
                        Directory.CreateDirectory(@"C:\Users\barba\source\repos\SD-PL1-GP5\Agregador\Data");

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

                            string response = SendToServer($"DATA:{fileName}");
                            if (response == "100 OK")
                            {
                                using (StreamReader reader = new StreamReader(filePath))
                                {
                                    string line;
                                    while ((line = reader.ReadLine()) != null)
                                    {
                                        SendToServer(line);  // Envia cada linha do arquivo para o servidor
                                    }
                                }
                                SendToServer("END");  // Finaliza o envio para o servidor

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

    static string SendToServer(string message)
    {
        string serverIP = "127.0.0.1";
        int serverPort = 6000;

        try
        {
            using (TcpClient serverClient = new TcpClient(serverIP, serverPort))
            using (NetworkStream serverStream = serverClient.GetStream())
            using (var reader = new StreamReader(serverStream, Encoding.UTF8))
            using (var writer = new StreamWriter(serverStream, Encoding.UTF8) { AutoFlush = true })
            {
                writer.WriteLine(message);
                Console.WriteLine($"[AGREGADOR] Enviado ao SERVIDOR: {message}");
                string response = reader.ReadLine();
                Console.WriteLine($"[AGREGADOR] Resposta do SERVIDOR: {response}");
                return response;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AGREGADOR] Erro ao comunicar com o servidor: {ex.Message}");
            return "500 ERROR";
        }
    }
}