dotnet build ../Haru.sln /p:Configuration=Release --no-incremental
dotnet build nuget.proj -t:Prepare
dotnet build nuget.proj -t:NuSpec,NuPack
