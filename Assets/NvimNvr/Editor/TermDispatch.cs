using System.Diagnostics;
using System.IO;
using Unity.CodeEditor;
using UnityEditor;
using UnityEngine;

#if (UNITY_EDITOR_WIN)

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

#elif (UNITY_EDITOR_LINUX)

using System.Text.RegularExpressions;

#elif (UNITY_EDITOR_OSX)

using System.Collections.Generic;
using System.Linq;

#endif

public class TermDispatch {
	private const string nvim_address = "127.0.0.1";
	private const int nvim_port = 25884;

	public static void CommandFields() {
		CommandFieldsByPlatform();
	}

	public static bool Open(string projectPath, string filePath, int line, int column) {
		return OpenByPlatform(Path.Join(projectPath, "Assets"), Path.GetFullPath(filePath), line, column);
	}

#if (UNITY_EDITOR_WIN)


		private const string script_editor_process_name = "WindowsTerminal";
		private static readonly string nvr_argument = $"-s --servername {nvim_address}:{nvim_port} --nostart $(File) +$(Line)";
		private static readonly string script_editor_start_argument = $"start wt -w 0 nt nvim $(File) +$(Line) -c \"cd {{0}}\" --listen {nvim_address}:{nvim_port}";

		private static bool isNvimRunning => IPGlobalProperties
			.GetIPGlobalProperties()
			.GetActiveTcpConnections()
			.Any(tcpi => tcpi.LocalEndPoint.Port == nvim_port && tcpi.LocalEndPoint.Address.ToString() == nvim_address);

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


#elif (UNITY_EDITOR_LINUX)


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

		private static bool TryGetNvimPid(out int pid){
			var process = Process.Start(new ProcessStartInfo{
				FileName = "bash",
				Arguments = $"-c \"ss -tulpn | grep {nvim_address}:{nvim_port}\"",
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true
			});
			var match = Regex.Match(process.StandardOutput.ReadToEnd(), @"pid=(\d+)");
			if(match.Success){
				return int.TryParse(match.Groups[1].Value, out pid);
			}else{
				pid = -1;
				return false;
			}
		}

		private static bool TryGetTermPid(int pid, out int ppid){
			ppid = -1;
			for(int i = 0; i < 12; i++){
				var process = Process.Start(new ProcessStartInfo{
					FileName = "bash",
					Arguments = $"-c \"ps -o ppid=,comm= -p {pid}\"",
					RedirectStandardOutput = true,
					UseShellExecute = false,
					CreateNoWindow = true
				});
				var output = process.StandardOutput.ReadToEnd().Trim();
				if(string.IsNullOrWhiteSpace(output)){
					return false;
				}
				var splits = output.Split(' ');
				if(splits.Length < 2){
					return true;
				}
				if(splits[1].Contains(term_emulator)){
					ppid = pid;
					return true;
				}else{
					pid = int.Parse(splits[0]);
				}
			}
			return false;
		}

		private static bool TryGetWindowId(int pid, out string wid){
			var process = Process.Start(new ProcessStartInfo{
				FileName = "bash",
				Arguments = $"-c \"wmctrl -lp | grep {pid}\"",
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true
			});
			var output = process.StandardOutput.ReadToEnd();
			if(output.Contains(pid.ToString())){
				wid = output.Split(' ')[0];
				return true;
			}else{
				wid = "";
				return false;
			}
		}

		private static void CommandFieldsByPlatform(){
			term_emulator = EditorGUILayout.TextField(new GUIContent("terminal: "), term_emulator);
			extra_dash_c = EditorGUILayout.TextField(new GUIContent("nvim -c: "), extra_dash_c);
		}

		private static bool OpenByPlatform(string projectPath, string filePath, int line, int column){
			if(TryGetNvimPid(out var pid)){
				var arg = CodeEditor.ParseArgument(nvr_args, filePath, line, column);
				Process.Start(new ProcessStartInfo{
					FileName = "nvr",
					Arguments = arg,
					CreateNoWindow = true,
					UseShellExecute = false,
				});

				if(TryGetTermPid(pid, out var ppid) && TryGetWindowId(ppid, out var wid)){
					Process.Start(new ProcessStartInfo{
						FileName = "wmctrl",
						Arguments = "-ia " + wid,
						CreateNoWindow = true,
						UseShellExecute = false,
					});
				}
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


#elif (UNITY_EDITOR_OSX)



	private static readonly List<string> term_emulator_options = new() { "kitty", "ghostty" };
	private const string default_term_emulator = "kitty";
	private static readonly string term_start_args = $"nvim $(File) +$(Line) -c '{{0}}' --listen {nvim_address}:{nvim_port}";
	private static readonly string nvr_args = $"-s --servername {nvim_address}:{nvim_port} --nostart $(File) +$(Line)";

	private static string term_emulator {
		get => EditorPrefs.GetString("nvim_nvr_term_emulator", default_term_emulator);
		set => EditorPrefs.SetString("nvim_nvr_term_emulator", value);
	}

	private static string extra_dash_c {
		get => EditorPrefs.GetString("nvim_nvr_extra_dash_c_string", "");
		set => EditorPrefs.SetString("nvim_nvr_extra_dash_c_string", value);
	}

	private static void CommandFieldsByPlatform() {
		var selectedIndex = 0;
		if (term_emulator_options.Contains(term_emulator)) {
			selectedIndex = term_emulator_options.IndexOf(term_emulator);
		}
		selectedIndex = EditorGUILayout.Popup(new GUIContent("terminal: "), selectedIndex, term_emulator_options.ToArray());
		term_emulator = term_emulator_options[selectedIndex];
		extra_dash_c = EditorGUILayout.TextField(new GUIContent("nvim -c: "), extra_dash_c);
	}

	private static bool TryGetNvimPid(out object pid) {
		var psi = new ProcessStartInfo {
			FileName = "zsh",
			Arguments = $"-c 'netstat -an | grep LISTEN | grep {nvim_address}.{nvim_port}'",
			RedirectStandardOutput = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};
		using (var process = Process.Start(psi)) {
			process.WaitForExit();
			var output = process.StandardOutput.ReadToEnd();
			if (string.IsNullOrWhiteSpace(output)) {
				pid = -1;
				return false;
			} else {
				pid = 0;
				return true;
			}
		}
	}

	private static bool OpenByPlatform(string projectPath, string filePath, int line, int column) {
		if (term_emulator == "kitty") {
			return OpenKitty(projectPath, filePath, line, column);
		} else if (term_emulator == "ghostty") {
			return OpenGhostty(projectPath, filePath, line, column);
		}
		return false;
	}

	private static bool TryGetKittySocket(out string socket) {
		var tmpDir = new DirectoryInfo("/tmp");
		var kittySocket = tmpDir.GetFiles("mykitty-*")
			.OrderByDescending(f => f.LastWriteTime)
			.FirstOrDefault();
		if (kittySocket == null) {
			socket = "";
			return false;
		} else {
			socket = kittySocket.FullName;
			return true;
		}
	}

	private static bool OpenKitty(string projectPath, string filePath, int line, int column) {
		if (TryGetKittySocket(out var socket)) {
			if (TryGetNvimPid(out var pid)) {
				var arg = CodeEditor.ParseArgument(nvr_args, filePath, line, column);
				var psi = new ProcessStartInfo {
					FileName = "zsh",
					Arguments = $"-lic 'nvr {arg}'",
					CreateNoWindow = true,
					UseShellExecute = false,
				};
				using (var process = Process.Start(psi)) {
					if (process == null) return false;
				}
			} else {
				var arg = string.Format(term_start_args, extra_dash_c);
				arg = CodeEditor.ParseArgument(arg, filePath, line, column);
				var psi = new ProcessStartInfo {
					FileName = term_emulator,
					Arguments = $"@ --to unix:{socket} launch --type=os-window --cwd={projectPath} zsh -lic '{arg}'",
					CreateNoWindow = true,
					UseShellExecute = false,
				};
				using (var process = Process.Start(psi)) {
					if (process == null) return false;
				}
			}
			using (var focusProcess = Process.Start(new ProcessStartInfo {
				FileName = term_emulator,
				Arguments = $"@ --to unix:{socket} focus-window",
				CreateNoWindow = true,
				UseShellExecute = false,
			})) { }
		} else {
			var dash_c = $"cd {projectPath}";
			if (!string.IsNullOrWhiteSpace(extra_dash_c)) {
				dash_c = dash_c + " | " + extra_dash_c;
			}
			var arg = string.Format(term_start_args, dash_c);
			arg = CodeEditor.ParseArgument(arg, filePath, line, column);
			var psi = new ProcessStartInfo {
				FileName = "zsh",
				Arguments = $"-lic '{term_emulator} -d ~/ {arg}'",
				CreateNoWindow = true,
				UseShellExecute = false,
			};
			using (var process = Process.Start(psi)) {
				if (process == null) return false;
			}
		}
		return true;
	}

	private static bool OpenGhostty(string projectPath, string filePath, int line, int column) {
		if (TryGetNvimPid(out var pid)) {
			var arg = CodeEditor.ParseArgument(nvr_args, filePath, line, column);
			var psi = new ProcessStartInfo {
				FileName = "zsh",
				Arguments = $"-lic 'nvr {arg}'",
				CreateNoWindow = true,
				UseShellExecute = false,
			};
			using (var process = Process.Start(psi)) {
				if (process == null) return false;

				using (var focusProcess = Process.Start(new ProcessStartInfo {
					FileName = "zsh",
					Arguments = $"-lic 'hs -c \"GhosttyFocus()\"'",
					CreateNoWindow = true,
					UseShellExecute = false,
				})) { }
			}
		} else {
			var dash_c = $"cd {projectPath}";
			if (!string.IsNullOrWhiteSpace(extra_dash_c)) {
				dash_c = dash_c + " | " + extra_dash_c;
			}
			var arg = string.Format(term_start_args, dash_c);
			arg = CodeEditor.ParseArgument(arg, filePath, line, column);
			var pbpsi = new ProcessStartInfo {
				FileName = "pbcopy",
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardInput = true,
			};
			using (var pbProcess = Process.Start(pbpsi)) {
				if (pbProcess == null) return false;

				pbProcess.StandardInput.Write(arg);
				pbProcess.StandardInput.Close();
				pbProcess.WaitForExit();

				using (var hsProcess = Process.Start(new ProcessStartInfo {
					FileName = "zsh",
					Arguments = $"-lic 'hs -c \"GhosttyNewWindow()\"'",
					CreateNoWindow = true,
					UseShellExecute = false,
				})) { }
			}
		}
		return true;
	}


#endif
}
