# Assorted Plugins for paint.net
I use VS Code for these plugins, so the `.csproj` and `.sln` files
may not immediately be compatible with Visual Studio.

## Associated VS Code Plugins
I use these plugins in VS Code for C# and project management purposes. These
links will open in VS Code (using the `vscode` protocol).

- [C#](vscode:extension/ms-vscode.csharp)
- [C# Extensions](vscode:extension/jchannon.csharpextensions)
  (New C# Class/Interface in context menu)
- [Auto Close Tag](vscode:extension/formulahendry.auto-close-tag) (XML)
- [XML Tools](vscode:extension/dotjoshjohnson.xml) (XML format)

## Debug Setup
As far as I know, paint.net doesn't support sideloading plugins, so the easiest
debug setup is to install the plugin as a symlink.

```bash
cd PluginFolder
# In Bash
ln -s "bin/Debug/Effect.dll" "/c/Program Files/paint.net/Effects/Plugin.dll"
# In CMD
mklink "C:\Program Files\paint.net\Effects\Effect.dll" "bin\Debug\Plugin.dll"
```

Since the plugin is installed, just use paint.net as the debug program, for
example in VS Code `launch.json`:
```json
"program": "${env:PROGRAMFILES}\\paint.net\\PaintDotNet.exe"
```

If your paint.net is installed somewhere else, you'll have to adjust the
locations of the DLLs in the `.csproj` file.

## Template
1. Copy the `Template` folder and rename it and the `.csproj` file
2. Rename the `.cs` files and update the `.csproj` file
3. Update the information in `Properties\AssemblyInfo.cs`
3. Update `icon.png` for the menu icon
4. Add the project to the solution using `dotnet sln add PluginFolder`
