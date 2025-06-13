using Grpc.Core;
using GrpcGreeterClient2;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;
using System.Threading.Tasks;

namespace Trab1
{
    public class CalculatorService : Calculator.CalculatorBase
    {
        public override Task<CalculateReply> CalculateAverage(CalculateRequest request, ServerCallContext context)
        {
            if (!request.Values.Any())
            {
                return Task.FromResult(new CalculateReply
                {
                    Average = 0,
                    Message = "Nenhum valor para calcular a média."
                });
            }
            double average = request.Values.Average();
            return Task.FromResult(new CalculateReply
            {
                Average = average,
                Message = $"Média calculada com sucesso: {average}"
            });
        }

        public override Task<MinMaxReply> FindMinimum(CalculateRequest request, ServerCallContext context)
        {
            if (!request.Values.Any())
            {
                return Task.FromResult(new MinMaxReply
                {
                    Value = 0,
                    Message = "Nenhum valor para encontrar o mínimo."
                });
            }
            double min = request.Values.Min();
            return Task.FromResult(new MinMaxReply
            {
                Value = min,
                Message = $"Mínimo encontrado com sucesso: {min}"
            });
        }

        public override Task<MinMaxReply> FindMaximum(CalculateRequest request, ServerCallContext context)
        {
            if (!request.Values.Any())
            {
                return Task.FromResult(new MinMaxReply
                {
                    Value = 0,
                    Message = "Nenhum valor para encontrar o máximo."
                });
            }
            double max = request.Values.Max();
            return Task.FromResult(new MinMaxReply
            {
                Value = max,
                Message = $"Máximo encontrado com sucesso: {max}"
            });
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure Kestrel to listen on HTTPS port 7220
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenLocalhost(7220, o => o.UseHttps());
            });

            // Add services to the container.
            builder.Services.AddGrpc();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.MapGrpcService<CalculatorService>();
            app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

            app.Run();
        }
    }
}
