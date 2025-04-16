// Wavy/Program.cs
using System;
using System.Net.Sockets;
using System.Text;

class Wavy
{
    static void Main(string[] args)
    {
        string aggregatorIP = "127.0.0.1";
        int aggregatorPort = 5000;
        string wavyId = "WAVY001";

        try
        {
            using (TcpClient client = new TcpClient(aggregatorIP, aggregatorPort))
            using (NetworkStream stream = client.GetStream())
            using (var reader = new System.IO.StreamReader(stream, Encoding.UTF8))
            using (var writer = new System.IO.StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
            {
                // Envia HELLO
                writer.WriteLine($"HELLO:{wavyId}");
                Console.WriteLine($"[WAVY] Enviado: HELLO:{wavyId}");

                // Lê resposta
                string response = reader.ReadLine();
                Console.WriteLine($"[WAVY] Recebido: {response}");

                // Envia QUIT
                writer.WriteLine("QUIT");
                Console.WriteLine("[WAVY] Enviado: QUIT");

                // Lê resposta final
                response = reader.ReadLine();
                Console.WriteLine($"[WAVY] Recebido: {response}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WAVY] Erro: {ex.Message}");
        }
    }
}
