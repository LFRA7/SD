// Servidor/Program.cs
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Servidor
{
    static void Main()
    {
        int port = 6000;
        TcpListener listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine("[SERVIDOR] A escutar na porta " + port);

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

                    if (msg.StartsWith("HELLO:"))
                    {
                        writer.WriteLine("100 OK");
                        Console.WriteLine("[SERVIDOR] Enviado: 100 OK");
                    }
                    else if (msg.StartsWith("DATA:"))
                    {
                        string fileName = msg.Substring(5).Trim();
                        string filePath = Path.Combine(@"C:\Users\lucas\source\repos\LFRA7\SD\Servidor\Data", fileName);
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                        writer.WriteLine("100 OK");
                        Console.WriteLine($"[SERVIDOR] Preparando para salvar {fileName}...");

                        using (var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write))
                        using (var fileWriter = new StreamWriter(fileStream, Encoding.UTF8))
                        {
                            string fileContent;
                            while ((fileContent = reader.ReadLine()) != null)
                            {
                                if (fileContent == "END") break;
                                fileWriter.WriteLine(fileContent);
                                fileWriter.Flush();
                                Console.WriteLine($"[SERVIDOR] Gravado: {fileContent}");
                            }
                        }

                        writer.WriteLine("100 OK");
                        Console.WriteLine($"[SERVIDOR] Arquivo {fileName} salvo com sucesso.");
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
                client.Close();
                Console.WriteLine("[SERVIDOR] Cliente desconectado.");
            }
        }
    }
}