Use Neovim as your Unity code editor.

This package:
1. Opens file & line in Nvim from the Unity editor (requires [nvr](https://github.com/mhinz/neovim-remote))
2. Generates csproj. and sln files (same way as the [old vscode package](https://github.com/Unity-Technologies/com.unity.ide.vscode))

### OS & Terminal Support
Windows:
- Tested on Windows 10 with Windows Terminal.
- **Terminal selection**: Only WT is supported.
- **Neovim ```-c``` support**: Can’t pass commands yet, but this is easy to add.
- **Bring to front**: Focuses the first WT window. Can hit the wrong one, and does not handle tabs.

Linux:
- Tested on NixOS with Cinnamon and Kitty.
- **Terminal selection**: Can change the terminal emulator in Edit > Preferences > External Tools.
- **Neovim ```-c``` support**: Allows passing one command to Neovim on startup. The command can be set in Preferences.
- **Bring to front**: Precisely finds the window using pid. Requires ```wmctrl``` with some heuristics made during implementation.

Check ```TermDispatch.cs``` for more details.

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
