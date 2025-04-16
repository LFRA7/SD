// Servidor/Program.cs
using System;
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
        using (var reader = new System.IO.StreamReader(stream, Encoding.UTF8))
        using (var writer = new System.IO.StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
        {
            try
            {
                string msg = reader.ReadLine();
                Console.WriteLine($"[SERVIDOR] Recebido: {msg}");

                if (msg.StartsWith("HELLO:"))
                {
                    writer.WriteLine("100 OK");
                    Console.WriteLine("[SERVIDOR] Enviado: 100 OK");
                }
                else if (msg == "QUIT")
                {
                    writer.WriteLine("400 BYE");
                    Console.WriteLine("[SERVIDOR] Enviado: 400 BYE");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVIDOR] Erro: {ex.Message}");
            }
        }
    }
}