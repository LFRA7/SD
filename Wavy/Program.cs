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

        string wavyId = "";
        string response = "";
        bool idAccepted = false;

        try
        {
            client = new TcpClient(aggregatorIP, aggregatorPort);
            stream = client.GetStream();
            Console.WriteLine("[WAVY] Conexão estabelecida com o Agregador.");

            while (!idAccepted)
            {
                Console.Write("Digite o ID do Wavy: ");
                wavyId = Console.ReadLine();

                // Envia HELLO
                SendMessage($"HELLO:{wavyId}");
                Console.WriteLine($"[WAVY] Enviado: HELLO:{wavyId}");

                // Lê resposta do Agregador
                response = ReceiveMessage();
                Console.WriteLine($"[WAVY] Recebido: {response}");

                if (response == "100 OK")
                {
                    idAccepted = true;
                    Console.WriteLine("[WAVY] ID aceito pelo Agregador.");
                }
                else if (response == "401 ID_IN_USE")
                {
                    Console.WriteLine("[WAVY] Este ID já está em uso. Por favor, escolha outro ID.");
                    // Continua no loop para solicitar novo ID
                }
                else
                {
                    Console.WriteLine($"[WAVY] Resposta inesperada do Agregador: {response}");
                    // Para outros erros, também solicitamos um novo ID
                }
            }

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
                            // Adiciona o wavyId no início de cada linha
                            string modifiedLine = $"{wavyId}:{line}";
                            SendMessage(modifiedLine);
                            Console.WriteLine($"[WAVY] Enviado: {modifiedLine}");
                        }
                        SendMessage("END");
                        Console.WriteLine($"[WAVY] Enviado conteúdo do arquivo: {fileName}");

                        string aggResponse = ReceiveMessage();
                        Console.WriteLine($"[WAVY] Recebido: {aggResponse}");
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

                    //if (response == "SEND_COMPLETE")
                    //{
                    //    Console.WriteLine("[WAVY] O Agregador terminou de enviar os dados ao servidor.");
                    //}
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
