using System.Linq;
using System.Net.NetworkInformation;
using System.IO;
using UnityEditor;
using UnityEngine;

#if (UNITY_EDITOR_WIN)

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.CodeEditor;

#elif (UNITY_EDITOR_LINUX)

using System.Diagnostics;
using Unity.CodeEditor;

#endif

public class TermDispatch{
		private const string nvim_address = "127.0.0.1";
		private const int nvim_port = 25884;

		private static bool isNvimRunning => IPGlobalProperties
			.GetIPGlobalProperties()
			.GetActiveTcpConnections()
			.Any(tcpi => tcpi.LocalEndPoint.Port == nvim_port && tcpi.LocalEndPoint.Address.ToString() == nvim_address);

		public static void CommandFields(){
			CommandFieldsByPlatform();
		}

		public static bool Open(string projectPath, string filePath, int line, int column){
			projectPath = Path.Join(projectPath, "Assets");
			return OpenByPlatform(projectPath, filePath, line, column);
		}

#if(UNITY_EDITOR_WIN)


		private const string script_editor_process_name = "WindowsTerminal";
		private static readonly string nvr_argument = $"-s --servername {nvim_address}:{nvim_port} --nostart $(File) +$(Line)";
		private static readonly string script_editor_start_argument = $"start wt -w 0 nt nvim $(File) +$(Line) -c \"cd {{0}}\" --listen {nvim_address}:{nvim_port}";
		[DllImport("user32.dll")]
		private static extern bool SetForegroundWindow(IntPtr hWnd);

		private static void CommandFieldsByPlatform(){
		}

		private static bool OpenByPlatform(string projectPath, string filePath, int line, int column){
			if(isNvimRunning){
				Process
					.Start(new ProcessStartInfo{
						FileName = "nvr",
						Arguments = CodeEditor.ParseArgument(nvr_argument, filePath, line, column),
						CreateNoWindow = true,
						UseShellExecute = false,
					})
					.WaitForExit();
			}else{
				var arg = string.Format(script_editor_start_argument, projectPath);
				arg = CodeEditor.ParseArgument(arg, filePath, line, column);
				Process
					.Start(new ProcessStartInfo{
						FileName = "cmd.exe",
						Arguments = "/c " + arg,
						CreateNoWindow = true,
						UseShellExecute = false,
					})
					.WaitForExit();
			}
			var process = Process.GetProcessesByName(script_editor_process_name).FirstOrDefault();
			if(process != null){
				var scriptEditorWindowHandle = process.MainWindowHandle;
				SetForegroundWindow(scriptEditorWindowHandle);
			}
			return true;
		}


#elif(UNITY_EDITOR_LINUX)


		private const string default_term_emulator = "kitty";
		private static readonly string term_start_args = $"nvim $(File) +$(Line) -c \"{{0}}\" --listen {nvim_address}:{nvim_port}";
		private static readonly string nvr_args = $"-s --servername {nvim_address}:{nvim_port} --nostart $(File) +$(Line)";

		private static string term_emulator{
			get => EditorPrefs.GetString("nvim_nvr_term_emulator", default_term_emulator);
			set => EditorPrefs.SetString("nvim_nvr_term_emulator", value);
		}

		private static string extra_dash_c{
			get => EditorPrefs.GetString("nvim_nvr_extra_dash_c_string", "");
			set => EditorPrefs.SetString("nvim_nvr_extra_dash_c_string", value);
		}

		private static void CommandFieldsByPlatform(){
			term_emulator = EditorGUILayout.TextField(new GUIContent("terminal: "), term_emulator);
			extra_dash_c = EditorGUILayout.TextField(new GUIContent("nvim -c: "), extra_dash_c);
		}

		private static bool OpenByPlatform(string projectPath, string filePath, int line, int column){
			if(isNvimRunning){
				var arg = CodeEditor.ParseArgument(nvr_args, filePath, line, column);
				Process.Start(new ProcessStartInfo{
					FileName = "nvr",
					Arguments = arg,
					CreateNoWindow = true,
					UseShellExecute = false,
				});
			}else{
				var dash_c = $"cd {projectPath}";
				if(!string.IsNullOrWhiteSpace(extra_dash_c)){
					dash_c = dash_c + " | " + extra_dash_c;
				}
				var arg = string.Format(term_start_args, dash_c);
				arg = CodeEditor.ParseArgument(arg, filePath, line, column);
				Process.Start(new ProcessStartInfo{
					FileName = term_emulator,
					Arguments = arg,
					CreateNoWindow = true,
					UseShellExecute = false,
				});
			}
			return true;
		}


#endif
}
