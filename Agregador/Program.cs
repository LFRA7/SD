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
        string currentWavyId = "unknown";

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
                        currentWavyId = msg.Substring(6).Trim();

                        // Verificar se já existe um Wavy com este ID que esteja ativo
                        bool idJaAssociado = VerificarWavyIdAssociado(currentWavyId);

                        if (idJaAssociado)
                        {
                            // ID já está em uso, enviar erro
                            wavyWriter.WriteLine("401 ID_IN_USE");
                            wavyWriter.Flush();
                            Console.WriteLine($"[AGREGADOR] Enviado ao WAVY: 401 ID_IN_USE - ID {currentWavyId} já está associado");

                            // Resetar o ID atual pois a associação falhou
                            currentWavyId = "unknown";
                        }
                        else
                        {
                            // Update the Wavy status file
                            UpdateWavyStatus(currentWavyId, "Associado");

                            // Respond to WAVY
                            wavyWriter.WriteLine("100 OK");
                            wavyWriter.Flush();
                            Console.WriteLine("[AGREGADOR] Enviado ao WAVY: 100 OK");
                        }
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
                        if (lineCount >= 20)
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

                                    if (response != null && response.Trim() == "100 OK")
                                    {

                                        try
                                        {
                                            // Fechar qualquer handle aberto para o arquivo
                                            GC.Collect();
                                            GC.WaitForPendingFinalizers();

                                            // Apagar o conteúdo do arquivo
                                            using (FileStream fs = new FileStream(filePath, FileMode.Truncate, FileAccess.Write))
                                            {
                                                // O modo Truncate reduz o tamanho para zero
                                                fs.SetLength(0);
                                            }

                                            Console.WriteLine($"[AGREGADOR] Arquivo {fileName} limpo após envio ao servidor.");
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"[AGREGADOR] Erro ao limpar o arquivo: {ex.Message}");
                                        }
                                    }
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

                        UpdateWavyStatus(currentWavyId, "Desativado");

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

    static void UpdateWavyStatus(string wavyId, string status)
    {
        string wavysFilePath = Path.Combine(@"C:\\Users\\lucas\\source\\repos\\LFRA7\\SD\\Agregador\\Data", "Wavys.csv");

        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(wavysFilePath));

        // Current timestamp for last_sync
        string lastSync = DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss");

        // Format the new line to add
        string wavyEntry = $"{wavyId}:{status}:{lastSync}";

        // Dictionary to hold updated records
        Dictionary<string, string> wavyRecords = new Dictionary<string, string>();

        // Read existing records if file exists
        if (File.Exists(wavysFilePath))
        {
            foreach (string line in File.ReadLines(wavysFilePath))
            {
                string[] parts = line.Split(':', 2); // Split only at first colon to get ID
                if (parts.Length >= 2)
                {
                    string id = parts[0];
                    wavyRecords[id] = line; // Store the full line
                }
            }
        }

        // Update or add the new record
        wavyRecords[wavyId] = wavyEntry;

        // Write all records back to the file
        using (StreamWriter writer = new StreamWriter(wavysFilePath, false))
        {
            foreach (string record in wavyRecords.Values)
            {
                writer.WriteLine(record);
            }
        }

        Console.WriteLine($"[AGREGADOR] Atualizado status do Wavy {wavyId}: {status}");
    }

    static bool VerificarWavyIdAssociado(string wavyId)
    {
        string wavysFilePath = Path.Combine(@"C:\\Users\\lucas\\source\\repos\\LFRA7\\SD\\Agregador\\Data", "Wavys.csv");

        // Se o arquivo não existir, não há Wavys associados
        if (!File.Exists(wavysFilePath))
            return false;

        // Ler o arquivo e verificar se há um Wavy com o mesmo ID e status "Associado"
        foreach (string line in File.ReadLines(wavysFilePath))
        {
            string[] parts = line.Split(':');
            if (parts.Length >= 3 && parts[0] == wavyId && parts[1] == "Associado")
            {
                return true; // ID já está associado
            }
        }

        return false; // ID não está associado ou tem outro status
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