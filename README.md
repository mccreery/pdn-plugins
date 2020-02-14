# Assorted Plugins for paint.net
~~I use VS Code~~ The solution and project files are made with Visual Studio
2019. The VS Code information below is kept in case anyone wants to contribute
to the project using VS Code. Extra care would have to be taken to avoid
mangling the `.csproj` and `.sln` files.

## Visual Studio

Each plugin copies its built DLL into the paint.net plugins folder on build, so
Visual Studio needs to be run as administrator for that action to work.
Otherwise you will have to copy the files yourself each time you build.

### Template
There is a folder containing a `.vstemplate` file in the `PluginTemplate`
directory. If you drop a directory junction in the Visual Studio templates
folder, it will be picked up and updated if the repo ever updates it:

```
mklink /D "%USERPROFILE%\Documents\Visual Studio 2019\Templates\ProjectTemplates\PluginTemplate" "C:\Path\To\Repo\PluginTemplate"
```

## VS Code (Legacy)

**Note**: Some information in this section may be out of date. I use Visual
Studio now.

### Associated VS Code Plugins
I use these plugins in VS Code for C# and project management purposes. These
links will open in VS Code (using the `vscode` protocol).

- [C#](vscode:extension/ms-vscode.csharp)
- [C# Extensions](vscode:extension/jchannon.csharpextensions)
  (New C# Class/Interface in context menu)
- [Auto Close Tag](vscode:extension/formulahendry.auto-close-tag) (XML)
- [XML Tools](vscode:extension/dotjoshjohnson.xml) (XML format)

### Debug Setup
As far as I know, paint.net doesn't support sideloading plugins, so the easiest
debug setup is to install the plugin as a symlink:

```bat
mklink "C:\Program Files\paint.net\Effects\PluginName.dll" "PluginName\bin\Debug\PluginName.dll"
```

You can also use [Link Shell Extension](https://schinagl.priv.at/nt/hardlinkshellext/linkshellextension.html)
to make this process less painful. I found it much easier to pin the Effects
folder in Explorer and drag-and-drop the DLLs while holding right click, which
gives a "Drop Here... Symbolic Link" option.

Since the plugin is installed, just use paint.net as the debug program, for
example in VS Code `launch.json`:
```json
"program": "${env:PROGRAMFILES}\\paint.net\\PaintDotNet.exe"
```

If your paint.net is installed somewhere else, you'll have to adjust the
locations of the DLLs in the `.csproj` file.

### Template
1. Copy the `Template` folder and rename it and the `.csproj` file
2. Rename the `.cs` files and update the `.csproj` file
3. Update the information in `Properties\AssemblyInfo.cs`
3. Update `icon.png` for the menu icon
4. Add the project to the solution using `dotnet sln add PluginFolder`
