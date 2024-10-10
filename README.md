Use Neovim as your Unity code editor.

This package:
1. Opens file & line in Nvim from the Unity editor (requires [nvr](https://github.com/mhinz/neovim-remote))
2. Generates csproj. and sln files (same way as the [old vscode package](https://github.com/Unity-Technologies/com.unity.ide.vscode))

### OS & Terminal Support
Works out of the box with Windows Terminal (Windows) and Kitty (Linux).

Limitations on Windows:
1. Does not allow setting alternative terminal emulators (should be easy to impelement)
2. Does not allow passing command to Neovim by ```-c``` (should be easy to impelement)

On Linux:
1. Does not bring the terminal window to front when clicking files from within the Unity Editor (haven't look at how to do this yet)

Refer to ```TermDispatch.cs``` for more details on the relevant commands and implementations.

### Install
1. Open the Package Manager from Window > Package Manager
2. "+" button > Add package from git URL
```
https://github.com/dssste/NvimNvrEditor.git?path=/Assets/NvimNvr
```

### Setting Up Unity
Go to Edit > Preferences > External Tools and select any ```nvim``` executable. This file is not invoked; it's simply used to inform the editor that our package will handle code editors. If locating the actual ```nvim``` executable is troublesome, you can create an empty file called ```nvim``` and point the editor to it. Under the hood, we run the nvr command, so make sure that the command is accessible.

### Setting Up Nvim
There is not much to set up as long as you get the LSP stuff working. Omnisharp works fine for me. Highly recommand deselecting all in ```Generate .csproj files for``` to keep your sln small and let [omnisharp-extended-lsp.nvim](https://github.com/Hoffs/omnisharp-extended-lsp.nvim) cover external locations.
