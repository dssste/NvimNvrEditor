using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Unity.CodeEditor;

public class TermDispatch{
		private const string nvim_address = "127.0.0.1";
		private const int nvim_port = 25884;

		private static bool isNvimRunning => IPGlobalProperties
			.GetIPGlobalProperties()
			.GetActiveTcpConnections()
			.Any(tcpi => tcpi.LocalEndPoint.Port == nvim_port && tcpi.LocalEndPoint.Address.ToString() == nvim_address);


#if(UNITY_EDITOR_WIN)


		private const string script_editor_process_name = "WindowsTerminal";
		private static readonly string nvr_argument = $"-s --servername {nvim_address}:{nvim_port} --nostart $(File) +$(Line)";
		private static readonly string script_editor_start_argument = $"start wt -w 0 nt nvim $(File) +$(Line) -c \"Proj {{0}}\" --listen {nvim_address}:{nvim_port}";
		[DllImport("user32.dll")]
		private static extern bool SetForegroundWindow(IntPtr hWnd);

		public static bool Open(string projectPath, string filePath, int line, int column){
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


		private const string script_editor_process_name = "WindowsTerminal";
		private static readonly string nvr_argument = $"-s --servername {nvim_address}:{nvim_port} --nostart $(File) +$(Line)";
		private static readonly string script_editor_start_argument = $"start wt -w 0 nt nvim $(File) +$(Line) -c \"Proj {{0}}\" --listen {nvim_address}:{nvim_port}";

		public static bool Open(string projectPath, string filePath, int line, int column){
			if(isNvimRunning){
				UnityEngine.Debug.Log("Nvim is running");
			}else{
				UnityEngine.Debug.Log("Nvim is not running");
			}
			return true;
		}


#endif
}
