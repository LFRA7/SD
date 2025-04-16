using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

class Wavy
{
    private TcpClient client;
    private NetworkStream stream;

    static void Main(string[] args)
    {
        new Wavy().Run();
    }

    public void Run()
    {
        string aggregatorIP = "127.0.0.1";
        int aggregatorPort = 5000;
        string wavyId;

        Console.Write("Digite o ID do Wavy: ");
        wavyId = Console.ReadLine();

        try
        {
            client = new TcpClient(aggregatorIP, aggregatorPort);
            stream = client.GetStream();
            Console.WriteLine("[WAVY] Conexão estabelecida com o Agregador.");

            // Envia HELLO
            SendMessage($"HELLO:{wavyId}");
            Console.WriteLine($"[WAVY] Enviado: HELLO:{wavyId}");

            // Lê resposta do Agregador
            string response = ReceiveMessage();
            Console.WriteLine($"[WAVY] Recebido: {response}");

            // Loop de comandos
            string command;
            do
            {
                Console.Write("Digite o comando (ou QUIT para sair): ");
                command = Console.ReadLine();

                if (command.StartsWith("DATA "))
                {
                    string fileName = command.Substring(5).Trim().Trim('"').Trim('\'');
                    string filePath = Path.Combine(@"C:\\Users\\lucas\\source\\repos\\LFRA7\\SD\\Wavy\\Data", fileName);

                    if (File.Exists(filePath))
                    {
                        SendMessage($"DATA:{fileName}");
                        Console.WriteLine($"[WAVY] Enviado: DATA:{fileName}");

                        foreach (string line in File.ReadLines(filePath))
                        {
                            SendMessage(line);
                        }
                        SendMessage("END");
                        Console.WriteLine($"[WAVY] Enviado conteúdo do arquivo: {fileName}");

                        string aggResponse = ReceiveMessage();
                        Console.WriteLine($"[WAVY] Resposta do Agregador: {aggResponse}");
                    }
                    else
                    {
                        Console.WriteLine($"[WAVY] Arquivo não encontrado: {fileName}");
                    }
                }
                else if (command != "QUIT")
                {
                    SendMessage(command);
                    Console.WriteLine($"[WAVY] Enviado: {command}");
                    response = ReceiveMessage();
                    Console.WriteLine($"[WAVY] Recebido: {response}");
                }
            } while (command != "QUIT");

            // Envia QUIT
            SendMessage("QUIT");
            Console.WriteLine("[WAVY] Enviado: QUIT");

            // Aguarda a resposta final
            while (true)
            {
                response = ReceiveMessage();
                Console.WriteLine($"[WAVY] Recebido: {response}");
                if (response == "400 BYE")
                {
                    break;
                }
            }

            stream.Close();
            client.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WAVY] Erro: {ex.Message}");
        }
    }

    private void SendMessage(string message)
    {
        if (stream == null)
            throw new InvalidOperationException("Conexão não estabelecida");

        byte[] data = Encoding.UTF8.GetBytes(message + "\n");
        stream.Write(data, 0, data.Length);
    }

    private string ReceiveMessage()
    {
        if (stream == null)
            throw new InvalidOperationException("Conexão não estabelecida");

        using (var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true))
        {
            return reader.ReadLine()?.Trim() ?? "";
        }
    }
}
