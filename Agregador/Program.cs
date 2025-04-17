// Aggregator/Program.cs
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;


class Agregador
{
    // Mutex para garantir acesso exclusivo aos recursos partilhados
    private static Mutex mutex = new Mutex();

    static void Main()
    {
        // Configuração da porta de escuta
        int listenPort = 5000;
        TcpListener listener = new TcpListener(IPAddress.Any, listenPort);
        listener.Start();
        Console.WriteLine("[AGREGADOR] Agregador ligado!");

        // Criação de Threads para lidar com várias Wavys
        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            Thread t = new Thread(HandleClient);
            t.Start(client);
        }
    }


    static void HandleClient(object obj)
    {
        // Varável para armazenar o ID da Wavy atual
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

                    // Processamento do comando HELLO para autenticação
                    if (msg.StartsWith("HELLO:"))
                    {
                        currentWavyId = msg.Substring(6).Trim();

                        // Verifica se o ID já está em uso
                        bool idJaAssociado = VerificarWavyIdAssociado(currentWavyId);

                        if (idJaAssociado)
                        {
                            // Envia mensagem de ID em uso
                            wavyWriter.WriteLine("401 ID_IN_USE");
                            wavyWriter.Flush();
                            Console.WriteLine($"[AGREGADOR] Enviado ao WAVY: 401 ID_IN_USE - ID {currentWavyId} já está associado");

                            // Coloca o ID como unknown
                            currentWavyId = "unknown";
                        }
                        else
                        {
                            // Atualiza o estado do Wavy para Associado
                            UpdateWavyStatus(currentWavyId, "Associado");

                            // Envia confirmação de sucesso ao WAVY
                            wavyWriter.WriteLine("200 READY");
                            wavyWriter.Flush();
                            Console.WriteLine("[AGREGADOR] Enviado ao WAVY: 200 READY");

                            // Envia o HELLO para o SERVIDOR
                            try
                            {
                                TcpClient serverClient = new TcpClient("127.0.0.1", 6000);
                                NetworkStream serverStream = serverClient.GetStream();
                                StreamReader serverReader = new StreamReader(serverStream, Encoding.UTF8);
                                StreamWriter serverWriter = new StreamWriter(serverStream, Encoding.UTF8) { AutoFlush = true };

                                serverWriter.WriteLine($"HELLO:{currentWavyId}");
                                Console.WriteLine($"[AGREGADOR] Enviado ao SERVIDOR: HELLO:{currentWavyId}");

                                string serverResponse = serverReader.ReadLine();
                                Console.WriteLine($"[AGREGADOR] Resposta do SERVIDOR: {serverResponse}");

                                if (serverResponse != "200 READY")
                                {
                                    Console.WriteLine("[AGREGADOR] Erro: O servidor não aceitou o HELLO ID.");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[AGREGADOR] Erro ao comunicar com o SERVIDOR: {ex.Message}");
                            }
                        }
                    }
                    // Processamento do comando SEND para receber dados
                    else if (msg.StartsWith("SEND:"))
                    {
                        // Usa um mutex para garantir acesso exclusivo aos ficheiros
                        mutex.WaitOne();

                        try
                        {
                            string fileName = msg.Substring(5).Trim('"');

                            // Caminho para o ficheiro correspondente (Alterar o caminho para o local desejado)
                            string filePath = Path.Combine(@"C:\Users\lucas\source\repos\LFRA7\SD\Agregador\Data", $"Dados-{fileName}");

                            // Garante que a pasta de dados existe (Alterar o caminho para o local desejado)
                            Directory.CreateDirectory(@"C:\Users\lucas\source\repos\LFRA7\SD\Agregador\Data");

                            bool shouldSendToServer = false;

                            // Guarda o conteúdo recebido no ficheiro
                            using (var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write))
                            using (var fileWriter = new StreamWriter(fileStream, Encoding.UTF8))
                            {
                                string fileContent;
                                while ((fileContent = wavyReader.ReadLine()) != null && fileContent != "END")
                                {
                                    fileWriter.WriteLine(fileContent);
                                }
                            }

                            // Confirma a receção dos dados
                            wavyWriter.WriteLine("200 READY");
                            Console.WriteLine($"[AGREGADOR] Enviado ao WAVY: 200 READY");

                            Console.WriteLine($"[AGREGADOR] O arquivo {fileName} foi atualizado com novos dados.");

                            // Verifica se o ficheiro atingiu o limite para envio ao servidor
                            int lineCount = File.ReadLines(filePath).Count();
                            if (lineCount >= 20)
                            {
                                shouldSendToServer = true;
                            }

                            // Processo de envio para o servidor quando necessário
                            if (shouldSendToServer)
                            {
                                Console.WriteLine($"[AGREGADOR] O arquivo atingiu {lineCount} linhas. A enviar dados para o Servidor...");

                                // Estabelece conexão com o servidor
                                TcpClient serverClient = new TcpClient("127.0.0.1", 6000);
                                NetworkStream serverStream = serverClient.GetStream();
                                StreamReader serverReader = new StreamReader(serverStream, Encoding.UTF8);
                                StreamWriter serverWriter = new StreamWriter(serverStream, Encoding.UTF8) { AutoFlush = true };

                                try
                                {
                                    // Informa o servidor sobre o envio de dados
                                    serverWriter.WriteLine($"SEND:{fileName}");
                                    Console.WriteLine($"[AGREGADOR] Enviado ao SERVIDOR: SEND:{fileName}");

                                    string response = serverReader.ReadLine();
                                    Console.WriteLine($"[AGREGADOR] Resposta do SERVIDOR: {response}");

                                    if (response == "200 READY")
                                    {
                                        // Envia o conteúdo do ficheiro para o servidor
                                        using (StreamReader fileReader = new StreamReader(filePath))
                                        {
                                            string line;
                                            while ((line = fileReader.ReadLine()) != null)
                                            {
                                                serverWriter.WriteLine(line);
                                                Console.WriteLine($"[AGREGADOR] Enviado ao SERVIDOR: {line}");
                                            }
                                        }

                                        // Marca o fim do envio
                                        serverWriter.WriteLine("END");
                                        Console.WriteLine($"[AGREGADOR] Enviado ao SERVIDOR: END");

                                        // Recebe confirmação do servidor
                                        response = serverReader.ReadLine();
                                        Console.WriteLine($"[AGREGADOR] Resposta do SERVIDOR: {response}");

                                        if (response != null && response.Trim() == "200 READY")
                                        {
                                            try
                                            {
                                                // Liberta recursos para evitar bloqueios de ficheiros
                                                GC.Collect();
                                                GC.WaitForPendingFinalizers();

                                                // Limpa o ficheiro após envio bem-sucedido
                                                using (FileStream fs = new FileStream(filePath, FileMode.Truncate, FileAccess.Write))
                                                {
                                                    fs.SetLength(0);
                                                }

                                                Console.WriteLine($"[AGREGADOR] O arquivo {fileName} foi limpo após o envio ao servidor.");
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
                                    // Liberta recursos da conexão com o servidor
                                    serverReader.Close();
                                    serverWriter.Close();
                                    serverStream.Close();
                                    serverClient.Close();
                                }
                            }
                        }
                        finally
                        {
                            // Liberta o mutex após processar os dados
                            mutex.ReleaseMutex();
                        }
                    }
                    // Comando para atualizar o estado do Wavy para "Operação" ou "Manutenção"
                    else if (msg.StartsWith("ESTADO "))
                    {
                        string estado = msg.Substring(7).Trim();
                        if (estado == "OPERACAO")
                        {
                            UpdateWavyStatus(currentWavyId, "Operação"); // Atualiza o estado para Operação
                        }
                        else if (estado == "MANUTENCAO")
                        {
                            UpdateWavyStatus(currentWavyId, "Manutenção"); // Atualiza o estado para Manutenção
                        }


                        wavyWriter.WriteLine("200 READY");
                        wavyWriter.Flush();
                        //Informa o Wavy que o estado foi atualizado
                        Console.WriteLine($"[AGREGADOR] Enviado ao WAVY: 200 READY - Estado atualizado para {estado}");
                    }
                    // Processamento do comando QUIT para terminar a conexão
                    else if (msg == "QUIT")
                    {
                        // Envia confirmação de encerramento
                        wavyWriter.WriteLine("410 CLOSED");
                        Console.WriteLine("[AGREGADOR] Enviado ao WAVY: 410 CLOSED");
                        Console.WriteLine("[AGREGADOR] A aguardar resposta do servidor");

                        // Notifica o servidor sobre a desconexão
                        string serverResponse = SendToServer("QUIT");
                        Console.WriteLine($"[AGREGADOR] Resposta do SERVIDOR: {serverResponse}");

                        // Atualiza o estado do Wavy para Desativado
                        UpdateWavyStatus(currentWavyId, "Desativado");

                        if (serverResponse == "410 CLOSED")
                        {
                            break;
                        }
                    }
                    // Processamento de comandos desconhecidos
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
            finally
            {
                // Garante que o estado do Wavy é atualizado mesmo em caso de erro
                if (currentWavyId != "unknown")
                {
                    UpdateWavyStatus(currentWavyId, "Desativado");
                }
            }
        }
    }


    // Função para enviar Ficheiros para o servidor
    static string SendToServer(string message, TcpClient existingClient = null,
                          StreamReader existingReader = null,
                          StreamWriter existingWriter = null,
                          bool keepConnection = false)
    {
        // Configuração do IP e porta do servidor
        string serverIP = "127.0.0.1";
        int serverPort = 6000;
        bool ownsConnection = existingClient == null;

        TcpClient serverClient = null;
        NetworkStream serverStream = null;
        StreamReader reader = null;
        StreamWriter writer = null;

        try
        {
            // Usa a conexão existente ou cria uma nova
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

            // Envia a mensagem
            writer.WriteLine(message);
            Console.WriteLine($"[AGREGADOR] Enviado ao SERVIDOR: {message}");

            // Aguarda resposta apenas para comandos específicos
            string response = "NO_RESPONSE";
            if (message.StartsWith("SEND:") || message.StartsWith("HELLO:") || message == "QUIT" || message == "END")
            {
                response = reader.ReadLine();
                Console.WriteLine($"[AGREGADOR] Resposta do SERVIDOR: {response}");
            }

            // Mantém a conexão se solicitado
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
            // Fecha os recursos se for o proprietário da conexão e não for solicitado para manter aberta
            if (ownsConnection && !keepConnection)
            {
                reader?.Close();
                writer?.Close();
                serverStream?.Close();
                serverClient?.Close();
            }
        }
    }


    // Função para atualizar o estado do Wavy
    static void UpdateWavyStatus(string wavyId, string status)
    {
        string wavysFilePath = Path.Combine(@"C:\Users\lucas\source\repos\LFRA7\SD\Agregador\Data", "Wavys.csv");

        // Garante que a pasta existe
        Directory.CreateDirectory(Path.GetDirectoryName(wavysFilePath));

        string lastSync = DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss");

        string wavyEntry = $"{wavyId}:{status}:{lastSync}";

        Dictionary<string, string> wavyRecords = new Dictionary<string, string>();

        // Lê os registos existentes
        if (File.Exists(wavysFilePath))
        {
            foreach (string line in File.ReadLines(wavysFilePath))
            {
                string[] parts = line.Split(':', 2);
                if (parts.Length >= 2)
                {
                    string id = parts[0];
                    wavyRecords[id] = line;
                }
            }
        }

        // Atualiza o registo do Wavy atual
        wavyRecords[wavyId] = wavyEntry;

        // Escreve todos os registos de volta ao ficheiro
        using (StreamWriter writer = new StreamWriter(wavysFilePath, false))
        {
            foreach (string record in wavyRecords.Values)
            {
                writer.WriteLine(record);
            }
        }

        Console.WriteLine($"[AGREGADOR] Atualizado status do Wavy {wavyId}: {status}");
    }


    //Função para verificar se o ID da Wavy está associado
    static bool VerificarWavyIdAssociado(string wavyId)
    {
        string wavysFilePath = Path.Combine(@"C:\Users\lucas\source\repos\LFRA7\SD\Agregador\Data", "Wavys.csv");

        // Se o ficheiro não existir, o ID não está associado
        if (!File.Exists(wavysFilePath))
            return false;

        // Verifica cada linha do ficheiro
        foreach (string line in File.ReadLines(wavysFilePath))
        {
            string[] parts = line.Split(':');
            if (parts.Length >= 3 && parts[0] == wavyId && parts[1] == "Associado")
            {
                return true; // ID encontrado e associado
            }
        }

        return false; // ID não encontrado ou não associado
    }
}