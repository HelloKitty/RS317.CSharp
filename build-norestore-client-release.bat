dotnet publish src/Client/Rs317.Client.Unity/Rs317.Client.Unity.csproj -c Release

if not exist "build\client\release\unity" mkdir build\client\release\unity"

xcopy src\Client\Rs317.Client.Unity\bin\Release\netstandard2.0\publish build\client\release\unity /Y /q /EXCLUDE:BuildExclude.txt
xcopy build\client\release\unity "C:\Users\Glader\Documents\Unity Projects\Rs317.Unity\Assets\DLLs" /Y /q /EXCLUDE:BuildExclude.txt