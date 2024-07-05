using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Unity.CodeEditor;
using UnityEditor;
using UnityEngine;

namespace dss.editor.nvimnvr{
	[InitializeOnLoad]
	public class NvimNvrScriptEditor: IExternalCodeEditor{
		private const string nvim_address = "127.0.0.1";
		private const int nvim_port = 25884;
		private const string script_editor_process_name = "WindowsTerminal";
		private static readonly string nvr_argument = $"-s --servername {nvim_address}:{nvim_port} --nostart $(File) +$(Line)";
		private static readonly string script_editor_start_argument = $"start wt -w 0 nt nvim $(File) +$(Line) -c \"Proj {{0}}\" --listen {nvim_address}:{nvim_port}";

		private ProjectGeneration projectGeneration;

		private IntPtr scriptEditorWindowHandle{
			get{
				var process = Process.GetProcessesByName(script_editor_process_name).FirstOrDefault();
				return process == null
					? IntPtr.Zero
					: process.MainWindowHandle;
			}
		}

		static string[] defaultExtensions => EditorSettings.projectGenerationBuiltinExtensions
			.Concat(EditorSettings.projectGenerationUserExtensions)
			.Concat(new[]{"json", "asmdef", "log"})
			.Distinct()
			.ToArray();

		static string[] HandledExtensions => HandledExtensionsString
			.Split(";", StringSplitOptions.RemoveEmptyEntries)
			.Select(s => s.TrimStart('.', '*'))
			.ToArray();

		static string HandledExtensionsString{
			get => EditorPrefs.GetString("nvim_nvr_user_extensions", string.Join(";", defaultExtensions));
			set => EditorPrefs.SetString("nvim_nvr_user_extensions", value);
		}

		public CodeEditor.Installation[] Installations => new CodeEditor.Installation[0];

		public bool TryGetInstallationForPath(string path, out CodeEditor.Installation result){
			// we are only responsible when the path is pointing to a nvim executable
			if(System.IO.Path.GetFileNameWithoutExtension(path) == "nvim"){
				result = new(){
					Name = "nvr",
					Path = path,
				};
				return true;
			}else{
				result = default;
				return false;
			}
		}

		public void OnGUI(){
			EditorGUILayout.LabelField("Generate .csproj files for:");
			EditorGUI.indentLevel++;
			SettingsButton(ProjectGenerationFlag.Embedded, "Embedded packages", "");
			SettingsButton(ProjectGenerationFlag.Local, "Local packages", "");
			SettingsButton(ProjectGenerationFlag.Registry, "Registry packages", "");
			SettingsButton(ProjectGenerationFlag.Git, "Git packages", "");
			SettingsButton(ProjectGenerationFlag.BuiltIn, "Built-in packages", "");
			SettingsButton(ProjectGenerationFlag.LocalTarBall, "Local tarball", "");
			SettingsButton(ProjectGenerationFlag.Unknown, "Packages from unknown sources", "");
			RegenerateProjectFiles();
			EditorGUI.indentLevel--;

			HandledExtensionsString = EditorGUILayout.TextField(new GUIContent("Extensions handled: "), HandledExtensionsString);
		}

		private void RegenerateProjectFiles(){
			var rect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(new GUILayoutOption[] { }));
			rect.width = 252;
			if(GUI.Button(rect, "Regenerate project files")){
				projectGeneration.Sync();
			}
		}

		private void SettingsButton(ProjectGenerationFlag preference, string guiMessage, string toolTip){
			var prevValue = projectGeneration.AssemblyNameProvider.ProjectGenerationFlag.HasFlag(preference);
			var newValue = EditorGUILayout.Toggle(new GUIContent(guiMessage, toolTip), prevValue);
			if(newValue != prevValue){
				projectGeneration.AssemblyNameProvider.ToggleProjectGeneration(preference);
			}
		}

		public void CreateIfDoesntExist(){
			if(!projectGeneration.SolutionExists()){
				projectGeneration.Sync();
			}
		}

		public void SyncIfNeeded(string[] addedFiles, string[] deletedFiles, string[] movedFiles, string[] movedFromFiles, string[] importedFiles){
			(projectGeneration.AssemblyNameProvider as IPackageInfoCache)?.ResetPackageInfoCache();
			projectGeneration.SyncIfNeeded(addedFiles.Union(deletedFiles).Union(movedFiles).Union(movedFromFiles).ToList(), importedFiles);
		}

		public void SyncAll(){
			(projectGeneration.AssemblyNameProvider as IPackageInfoCache)?.ResetPackageInfoCache();
			AssetDatabase.Refresh();
			projectGeneration.Sync();
		}

		[DllImport("user32.dll")]
		private static extern bool SetForegroundWindow(IntPtr hWnd);

		private bool isNvimRunning => IPGlobalProperties
			.GetIPGlobalProperties()
			.GetActiveTcpConnections()
			.Any(tcpi => tcpi.LocalEndPoint.Port == nvim_port && tcpi.LocalEndPoint.Address.ToString() == nvim_address);

		public bool OpenProject(string path, int line, int column){
			if(path != "" && (!SupportsExtension(path) || !File.Exists(path))) return false;
			if(line == -1) line = 1;
			if(column == -1) column = 0;
			if(isNvimRunning){
				Process
					.Start(new ProcessStartInfo{
						FileName = "nvr",
						Arguments = CodeEditor.ParseArgument(nvr_argument, path, line, column),
						CreateNoWindow = true,
						UseShellExecute = false,
					})
					.WaitForExit();
			}else{
				var arg = string.Format(script_editor_start_argument, projectGeneration.ProjectDirectory);
				arg = CodeEditor.ParseArgument(arg, path, line, column);
				Process
					.Start(new ProcessStartInfo{
						FileName = "cmd.exe",
						Arguments = "/c " + arg,
						CreateNoWindow = true,
						UseShellExecute = false,
					})
					.WaitForExit();
			}
			SetForegroundWindow(scriptEditorWindowHandle);
			return true;
		}

		static bool SupportsExtension(string path){
			var extension = Path.GetExtension(path);
			if(string.IsNullOrEmpty(extension)) return false;

			return HandledExtensions.Contains(extension.TrimStart('.'));
		}

		static NvimNvrScriptEditor(){
			var editor = new NvimNvrScriptEditor();
			editor.projectGeneration = new ProjectGeneration(Directory.GetParent(Application.dataPath).FullName);
			CodeEditor.Register(editor);
			editor.CreateIfDoesntExist();
		}

		public void Initialize(string editorInstallationPath){}
	}
}
