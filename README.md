Use Neovim as your Unity code editor.

This package:
1. Opens file & line in Nvim from the Unity editor (requires [nvr](https://github.com/mhinz/neovim-remote))
2. Generates csproj. and sln files (same way as the [old vscode package](https://github.com/Unity-Technologies/com.unity.ide.vscode))

### OS & Terminal Support
Works out of the box on Windows Terminal (Windows) and Kitty (Linux). Currently, there are no configuration files to set your terminal. Refer to ```TermDispatch.cs``` for more details on the relevant commands.

### Install
1. Open the Package Manager from Window > Package Manager
2. "+" button > Add package from git URL
```
https://github.com/dssste/NvimNvrEditor.git?path=/Assets/NvimNvr
```

### Setting Up Unity
Go to Edit > Preferences > External Tools and select any nvim excutable. This is just to tell the editor this package will be responsible for handling code editors. Under the hood we run the nvr command, so make sure the command is accessible.

### Setting Up Nvim
There is not much to set up as long as you get the LSP stuff working. Omnisharp works fine for me. Highly recommand deselecting all in ```Generate .csproj files for``` to keep your sln small and let [omnisharp-extended-lsp.nvim](https://github.com/Hoffs/omnisharp-extended-lsp.nvim) cover external locations.
