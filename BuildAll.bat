dotnet build .\Rampastring.XNAUI.csproj -c WindowsDXRelease
dotnet build .\Rampastring.XNAUI.csproj -c UniversalGLRelease
dotnet build .\Rampastring.XNAUI.csproj -c WindowsGLRelease
dotnet build .\Rampastring.XNAUI.csproj -c WindowsXNARelease --runtime win-x86 --no-self-contained

dotnet build .\Rampastring.XNAUI.csproj -c WindowsDXDebug
dotnet build .\Rampastring.XNAUI.csproj -c UniversalGLDebug
dotnet build .\Rampastring.XNAUI.csproj -c WindowsGLDebug
dotnet build .\Rampastring.XNAUI.csproj -c WindowsXNADebug --runtime win-x86 --no-self-contained