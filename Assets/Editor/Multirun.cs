using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;
using Trubkin.Util;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Trubkin.Multirun
{
	[Serializable]
	public class Multirun : EditorWindow
	{
		// This labels are using for fields in Multirun tab in Unity Editor

		#region Labels

		// Main configs
		private const string CountOfInstancesLabel = "Count of Instances";
		private const string AutoConnectLabel = "Auto Connect";
		private const string EditorIsServerLabel = "Editor is Server";
		private const string EditorIsClientLabel = "Editor is Client";
		private const string OtherConfigLabel = "Other Configuration";

		// Other configs
		private const string TagNameLabel = "Tag of ArgHandler"; // todo rename
		private const string BuildPathLabel = "Build Path";
		private const string IpLabel = "IP";
		private const string PortLabel = "Port";
		private const string BuildConfigLabel = "Build Configuration";
		private const string ScenesLabel = "Scenes";

		// Buttons
		private const string RunButtonLabel = "Run";
		private const string BuildRunButtonLabel = "Build and Run";

		#endregion

		#region Report Messages

		private const string RequestMsgPattern = "[Multirun] \"{0}\" requested; Path \"{1}\"; Instances: {2} {3}; ";
		private const string ConnectMsgPattern = "Сonnect to: IP: {0}; Port {1}; ";
		private const string BuildMsg = "Build and Run";
		private const string RunMsg = "Run";
		private const string WithEditorMsg = "with editor";

		#endregion

		#region Error Messages

		private const string OsMsg = "Your OS doesn't supported";
		private const string SceneNullMsg = "Build stop! scene#{0} is NULL";
		private const string RunFailMsg = "Can't start process. {0}: {1}";

		#endregion

		#region Constants

		private const int MaxCountOfInstances = 5;
		private const int MinCountOfInstances = 1;

		private const string ExecutionExtension = ".exe";
		private const string SceneExtension = ".unity";

		// Костыль (см. Update())! количество фреймов задержки между остановкой и запуском игры в редакторе
		// Количество подбирается индивидуально опытным путем
		private const int ExecutionDelay = 60;

		#endregion

		// Variables that are used for store data from visual fields in Multirun tab

		#region TabCache

		[SerializeField] private List<SceneAsset> scenes;

		[SerializeField] private int countOfInstances = 1; // Instance - это экземпляр игры (новое окно или внутри редактора)

		[SerializeField] private string ip = "127.0.0.1";
		[SerializeField] private int port = 7777;

		[SerializeField] private string argHandlerTag = "Handler";

		[SerializeField] private string buildPath = "Build\\";

		[SerializeField] private bool showOtherConfiguration;

		[SerializeField] private bool runEditor;
		[SerializeField] private bool autoConnect;
		[SerializeField] private bool editorIsServer;
		[SerializeField] private bool editorIsClient;

		private Vector2 scrollPos; // Память для вертикального скролла

		#endregion

		private string FullExecutablePath
		{
			get { return buildPath + "/" + Application.productName + ExecutionExtension; }
		}

		[SerializeField] private List<int> processIds = new List<int>();
		private int exeDelayTimer = 0; // Костыль (см. Update())! оставшееся количество фреймов до запуска игры в редакторе

		[MenuItem("Window/Multirun")]
		public static void ShowWindow()
		{
#if UNITY_EDITOR_WIN
			GetWindow<Multirun>("Multirun");
#else
		Debug.LogError(OSMsg);
#endif
		}

		private void Update()
		{
			// Костыль! Unity не позволяет остановить и снова запустить игру в редакторе сразу, требуется задержка в несколько фреймов
			// поэтому рестарт переносится. Для этого и нужен таймер exeDelayTimer
			if (exeDelayTimer <= 0) return;
			exeDelayTimer--;
			if (exeDelayTimer <= 0)
			{
				Run();
			}
		}

		private void OnGUI()
		{
			scrollPos = EditorGUILayout.BeginScrollView(scrollPos); // Память для вертикального скролла

			EditorGUILayout.Space();
			countOfInstances = EditorGUILayout.IntField(CountOfInstancesLabel, countOfInstances);
			if (countOfInstances < MinCountOfInstances)
			{
				countOfInstances = MinCountOfInstances;
			}
			else if (countOfInstances > MaxCountOfInstances)
			{
				countOfInstances = MaxCountOfInstances;
			}

			runEditor = EditorGUILayout.Toggle("Run in Editor", runEditor);
			autoConnect = EditorGUILayout.Toggle(AutoConnectLabel, autoConnect);

			EditorGUI.BeginDisabledGroup(!autoConnect || !runEditor);

			// Блок radio toggle

			if (editorIsServer != EditorGUILayout.Toggle(EditorIsServerLabel, editorIsServer, EditorStyles.radioButton))
			{
				editorIsClient = false;
				editorIsServer = !editorIsServer;
			}

			if (editorIsClient != EditorGUILayout.Toggle(EditorIsClientLabel, editorIsClient, EditorStyles.radioButton))
			{
				editorIsServer = false;
				editorIsClient = !editorIsClient;
			}

			EditorGUI.EndDisabledGroup();

			EditorGUILayout.Space();

			// Отображение прочих опций
			showOtherConfiguration = EditorGUILayout.Foldout(showOtherConfiguration, OtherConfigLabel);
			if (showOtherConfiguration)
			{
				EditorGUI.indentLevel++;
				argHandlerTag = EditorGUILayout.TextField(TagNameLabel, argHandlerTag);
				ip = EditorGUILayout.TextField(IpLabel, ip);
				port = EditorGUILayout.IntField(PortLabel, port);

				EditorGUILayout.Space();
				EditorGUILayout.LabelField(BuildConfigLabel);

				EditorGui.DrawList(ScenesLabel, scenes, false);

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.TextField(BuildPathLabel, buildPath);
				if (GUILayout.Button("browse", EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
				{
					buildPath = EditorUtility.OpenFolderPanel("Build Path", buildPath, "");
				}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUI.indentLevel--;

			EditorGUILayout.Space();

			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button(RunButtonLabel))
			{
				ShowRunReport(false);
				Stop();
				DelayRun();
			}

			if (GUILayout.Button(BuildRunButtonLabel))
			{
				ShowRunReport(true);
				BuildAndRun();
				// Костыль! После билда юнити забывает о том, что разметка перешла в горизонталь
				EditorGUILayout.BeginHorizontal();
			}

			if (GUILayout.Button("Stop"))
			{
				Stop();
			}

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndScrollView();
		}

		private static string FindSceneLocation(string name)
		{
			var dirs = Directory.GetFiles("Assets", name, SearchOption.AllDirectories);

			return dirs.Length > 0 ? dirs[0] : null;
		}

		private void DelayRun()
		{
			exeDelayTimer = ExecutionDelay;
		}

		private void Run()
		{
			var i = 0;
			if (runEditor)
			{
				EditorApplication.isPlaying = true; // Старт в редакторе Unity
				i++;
			}

			for (; i < countOfInstances; i++)
			{
				var proc = new Process();
				proc.StartInfo.FileName = FullExecutablePath;
				try
				{
					proc.Start();
					processIds.Add(proc.Id);
				}
				catch (Win32Exception e)
				{
					Debug.LogWarning(string.Format(RunFailMsg, e.GetType().Name, e.Message));
				}
			}
		}

		// Returns true if build was success
		private bool Build()
		{
			var buildPlayerOptions = new BuildPlayerOptions();

			buildPlayerOptions.scenes = new string[scenes.Count];
			for (var i = 0; i < scenes.Count; i++)
			{
				if (scenes[i] != null)
				{
					buildPlayerOptions.scenes[i] = FindSceneLocation(scenes[i].name + SceneExtension);
				}
				else
				{
					Debug.LogError(string.Format(SceneNullMsg, i));
					return false;
				}
			}

			buildPlayerOptions.locationPathName = FullExecutablePath; // Полный путь до билда с именем исполняемого файла
			buildPlayerOptions.target = BuildTarget.StandaloneWindows;
			buildPlayerOptions.options = BuildOptions.None;
			BuildPipeline.BuildPlayer(buildPlayerOptions);
			return true;
		}

		private void BuildAndRun()
		{
			Stop();
			if (Build()) Run();
		}

		private void Stop()
		{
			EditorApplication.isPlaying = false;
			foreach (var procId in processIds)
			{
				try
				{
					var proc = Process.GetProcessById(procId);
					proc.Kill();
				}
				catch (InvalidOperationException)
				{
					// just ignore
				}
				catch (ArgumentException)
				{
					// just ignore
				}
			}

			processIds.Clear();
		}

		private void ShowRunReport(bool withBuild)
		{
			var reportMsg = new StringBuilder(string.Format(RequestMsgPattern,
				withBuild ? BuildMsg : RunMsg,
				FullExecutablePath,
				countOfInstances,
				runEditor ? WithEditorMsg : ""
			));

			if (autoConnect)
			{
				reportMsg.Append(string.Format(ConnectMsgPattern,
					ip,
					port));
			}

			Debug.Log(reportMsg);
		}
	}
}