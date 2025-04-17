// Servidor/Program.cs
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;


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
        Console.WriteLine("[SERVIDOR] Servidor ligado!");

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
                        writer.WriteLine("200 READY");
                        Console.WriteLine("[SERVIDOR] Enviado: 200 READY");
                    }
                    // Processamento do comando DATA para receber dados
                    else if (msg.StartsWith("SEND:"))
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
                            string filePath = Path.Combine(@"C:\\Users\\lucas\\source\\repos\\LFRA7\\SD\\Servidor\\Data", newFileName);
                            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                            Console.WriteLine($"[SERVIDOR] A Guardar {newFileName}...");

                            // Envia confirmação de que está pronto para receber dados
                            writer.WriteLine("200 READY");
                            Console.WriteLine("[SERVIDOR] Enviado: 200 READY (pronto para receber dados)");

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
                            writer.WriteLine("200 READY");
                            Console.WriteLine($"[SERVIDOR] O Arquivo {newFileName} foi guardado com sucesso.");
                        }
                        finally
                        {
                            // Liberta o mutex após processar os dados
                            mutex.ReleaseMutex();
                        }
                    }
                    // Processamento do comando DISCONNECT para terminar a conexão
                    else if (msg == "DISCONNECT")
                    {
                        writer.WriteLine("410 CLOSED");
                        Console.WriteLine("[SERVIDOR] Enviado: 410 CLOSED");
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