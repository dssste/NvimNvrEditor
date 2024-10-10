using System;
using System.IO;
using System.Linq;
using Unity.CodeEditor;
using UnityEditor;
using UnityEngine;

namespace dss.editor.nvimnvr{
	[InitializeOnLoad]
	public class NvimNvrScriptEditor: IExternalCodeEditor{
		private ProjectGeneration projectGeneration;

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
			TermDispatch.CommandFields();
			EditorGUILayout.LabelField("Generate .csproj files for:");
			EditorGUI.indentLevel++;
			SettingsButton(ProjectGenerationFlag.Embedded, "Embedded packages", "");
			SettingsButton(ProjectGenerationFlag.Local, "Local packages", "");
			SettingsButton(ProjectGenerationFlag.Registry, "Registry packages", "");
			SettingsButton(ProjectGenerationFlag.Git, "Git packages", "");
			SettingsButton(ProjectGenerationFlag.BuiltIn, "Built-in packages", "");
			SettingsButton(ProjectGenerationFlag.LocalTarBall, "Local tarball", "");
			SettingsButton(ProjectGenerationFlag.Unknown, "Packages from unknown sources", "");
			var rect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(new GUILayoutOption[] { }));
			rect.width = 252;
			if(GUI.Button(rect, "Regenerate project files")){
				projectGeneration.Sync();
			}
			EditorGUI.indentLevel--;
			HandledExtensionsString = EditorGUILayout.TextField(new GUIContent("Extensions handled: "), HandledExtensionsString);
		}

		private void SettingsButton(ProjectGenerationFlag preference, string guiMessage, string toolTip){
			var prevValue = projectGeneration.AssemblyNameProvider.ProjectGenerationFlag.HasFlag(preference);
			var newValue = EditorGUILayout.Toggle(new GUIContent(guiMessage, toolTip), prevValue);
			if(newValue != prevValue){
				projectGeneration.AssemblyNameProvider.ToggleProjectGeneration(preference);
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

		public bool OpenProject(string path, int line, int column){
			var extension = Path.GetExtension(path);
			if(string.IsNullOrEmpty(extension)) return false;
			if(!HandledExtensions.Contains(extension.TrimStart('.'))) return false;
			if(!File.Exists(path)) return false;

			if(line == -1) line = 1;
			if(column == -1) column = 0;
			return TermDispatch.Open(projectGeneration.ProjectDirectory, path, line, column);
		}

		static NvimNvrScriptEditor(){
			var editor = new NvimNvrScriptEditor();
			editor.projectGeneration = new ProjectGeneration(Directory.GetParent(Application.dataPath).FullName);
			CodeEditor.Register(editor);
			if(!editor.projectGeneration.SolutionExists()){
				editor.projectGeneration.Sync();
			}
		}

		public void Initialize(string editorInstallationPath){}
	}
}
