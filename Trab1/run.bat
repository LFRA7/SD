@echo off
echo Iniciando o Servidor...
start cmd /k "dotnet run --project C:\Users\lucas\source\repos\LFRA7\SD\Servidor\Servidor.csproj"
timeout /t 1 /nobreak >nul

echo Iniciando o Agregador...
start cmd /k "dotnet run --project C:\Users\lucas\source\repos\LFRA7\SD\Agregador\Agregador.csproj"
timeout /t 1 /nobreak >nul

echo Iniciando 3 instâncias do Wavy...
start cmd /k "dotnet run --project C:\Users\lucas\source\repos\LFRA7\SD\Wavy\Wavy.csproj"
start cmd /k "dotnet run --project C:\Users\lucas\source\repos\LFRA7\SD\Wavy\Wavy.csproj"

echo Tudo iniciado com sucesso!