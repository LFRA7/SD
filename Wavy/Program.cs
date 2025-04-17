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
        // Definição do IP e porta do Agregador
        string aggregatorIP = "127.0.0.1";
        int aggregatorPort = 5000;

        // Variáveis para gestão da identidade do Wavy
        string wavyId = "";
        string response = "";
        bool idAccepted = false;

        try
        {
            // Estabelece conexão com o Agregador
            client = new TcpClient(aggregatorIP, aggregatorPort);
            stream = client.GetStream();
            Console.WriteLine("[WAVY] Conexão estabelecida com o Agregador.");

            // Ciclo para autenticação do ID da Wavy
            while (!idAccepted)
            {
                Console.Write("Digite o ID do Wavy: ");
                wavyId = Console.ReadLine();

                // Envia mensagem de autenticação
                SendMessage($"HELLO:{wavyId}");
                Console.WriteLine($"[WAVY] Enviado: HELLO:{wavyId}");

                // Recebe resposta do Agregador
                response = ReceiveMessage();
                Console.WriteLine($"[WAVY] Recebido: {response}");

                // Verifica a resposta do Agregador
                if (response == "200 READY")
                {
                    idAccepted = true;
                }
                else if (response == "401 ID_IN_USE")
                {
                    Console.WriteLine("[WAVY] Este ID já está em uso. Por favor, introduza outro ID.");
                }
                else
                {
                    Console.WriteLine($"[WAVY] Resposta inesperada do Agregador: {response}");
                }
            }


            string command;
            do
            {
                Console.Write("Digite o comando (ou QUIT para sair): ");
                command = Console.ReadLine();

                // Comando DATA para envio de ficheiros
                if (command.StartsWith("DATA "))
                {
                    string fileName = command.Substring(5).Trim().Trim('"').Trim('\'');
                    string filePath = Path.Combine(@"C:\\Users\\lucas\\source\\repos\\LFRA7\\SD\\Wavy\\Data", fileName);

                    if (File.Exists(filePath))
                    {
                        // Informa o Agregador que irá enviar dados
                        SendMessage($"DATA:{fileName}");
                        Console.WriteLine($"[WAVY] Enviado: DATA:{fileName}");

                        // Envia cada linha do ficheiro com o seu ID no início
                        foreach (string line in File.ReadLines(filePath))
                        {
                            string modifiedLine = $"{wavyId}:{line}";
                            SendMessage(modifiedLine);
                            Console.WriteLine($"[WAVY] Enviado: {modifiedLine}");
                        }
                        // Marca o fim do envio dos dados
                        SendMessage("END");
                        Console.WriteLine($"[WAVY] Enviado conteúdo do arquivo: {fileName}");

                        // Recebe resposta do Agregador
                        string aggResponse = ReceiveMessage();
                        Console.WriteLine($"[WAVY] Recebido: {aggResponse}");
                    }
                    else
                    {
                        Console.WriteLine($"[WAVY] Arquivo não encontrado: {fileName}");
                    }
                }

                else if (command.StartsWith("ESTADO "))
                {
                    string estadoCommand = command.Substring(7).Trim().ToUpper();
                    if (estadoCommand == "OPERACAO" || estadoCommand == "MANUTENCAO")
                    {
                        SendMessage($"ESTADO {estadoCommand}");
                        Console.WriteLine($"[WAVY] Enviado: ESTADO {estadoCommand}");
                        response = ReceiveMessage();
                        Console.WriteLine($"[WAVY] Recebido: {response}");
                    }
                    else
                    {
                        Console.WriteLine("[WAVY] Comando de estado inválido. Use 'ESTADO OPERAÇÃO' ou 'ESTADO MANUTENÇÃO'.");
                    }

                }
                // Comando QUIT para encerrar a conexão
                else if (command != "QUIT")
                {
                    SendMessage(command);
                    Console.WriteLine($"[WAVY] Enviado: {command}");
                    response = ReceiveMessage();
                    Console.WriteLine($"[WAVY] Recebido: {response}");
                }

            } while (command != "QUIT");

            // Processo de encerramento da conexão
            SendMessage("QUIT");
            Console.WriteLine("[WAVY] Enviado: QUIT");

            // Aguarda a confirmação de desconexão do Agregador
            while (true)
            {
                response = ReceiveMessage();

                Console.WriteLine($"[WAVY] Recebido: {response}");
                if (response == "400 CLOSED")
                {
                    break; // Sai do ciclo quando recebe a confirmação de desconexão
                }
            }

            // Fecha os recursos de rede
            stream.Close();
            client.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WAVY] Erro: {ex.Message}");
        }
    }

    // Função para enviar mensagens ao Agregador
    private void SendMessage(string message)
    {
        if (stream == null)
            throw new InvalidOperationException("Conexão não estabelecida");

        byte[] data = Encoding.UTF8.GetBytes(message + "\n");
        stream.Write(data, 0, data.Length);
    }


    // Função para receber mensagens do Agregador
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
