SLN := ServUO.sln
APP_PROJ := Application/ServUO.csproj
EXE := ServUO.exe

.PHONY: default build debug release run run-mono clean restore

# По умолчанию: сборка в Release и запуск
default: run

# Восстановление NuGet-зависимостей
restore:
	dotnet restore "$(SLN)"

# Сборка в режиме Release (как в CI/CD)
build:
	dotnet build "$(SLN)" --configuration Release

# Сборка в режиме Debug (с отладочной информацией)
debug:
	dotnet build "$(SLN)" --configuration Debug

# Запуск сервера через .NET (для проектов под .NET 8 / .NET 10)
run: build
	dotnet run --project "$(APP_PROJ)" --configuration Release --no-build

# Альтернативный запуск через Mono (если ServUO.csproj нацелен на net472/net48)
run-mono: build
	mono "$(EXE)"

# Полная очистка артефактов сборки
clean:
	dotnet clean "$(SLN)" --configuration Release
	dotnet clean "$(SLN)" --configuration Debug
	rm -f Settings.dll Server.dll Scripts.dll "$(EXE)" "$(EXE).mdb" *.pdb
