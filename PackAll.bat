dotnet pack .\Rampastring.XNAUI.csproj -c WindowsDXRelease
dotnet pack .\Rampastring.XNAUI.csproj -c WindowsGLRelease
dotnet pack .\Rampastring.XNAUI.csproj -c XNARelease -p:PlatformTarget=x86

dotnet pack .\Rampastring.XNAUI.csproj -c WindowsDXDebug
dotnet pack .\Rampastring.XNAUI.csproj -c WindowsGLDebug
dotnet pack .\Rampastring.XNAUI.csproj -c XNADebug -p:PlatformTarget=x86