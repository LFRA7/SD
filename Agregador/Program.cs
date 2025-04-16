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
        using (var wavyReader = new System.IO.StreamReader(wavyStream, Encoding.UTF8))
        using (var wavyWriter = new System.IO.StreamWriter(wavyStream, Encoding.UTF8) { AutoFlush = true })
        {
            try
            {
                string msg = wavyReader.ReadLine();
                Console.WriteLine($"[AGREGADOR] Recebido do WAVY: {msg}");

                if (msg.StartsWith("HELLO:"))
                {
                    // Enviar HELLO ao servidor
                    string id = msg.Split(':')[1];
                    string serverResponse = SendToServer($"HELLO:{id}");
                    wavyWriter.WriteLine(serverResponse);
                }

                msg = wavyReader.ReadLine();
                if (msg == "QUIT")
                {
                    string serverResponse = SendToServer("QUIT");
                    wavyWriter.WriteLine(serverResponse);
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
            using (var reader = new System.IO.StreamReader(serverStream, Encoding.UTF8))
            using (var writer = new System.IO.StreamWriter(serverStream, Encoding.UTF8) { AutoFlush = true })
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