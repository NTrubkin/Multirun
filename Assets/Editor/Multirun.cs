using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
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
		private const string CountOfInstancesLabel = "Count of Game Instances";
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

		private const string OSMsg = "Your OS doesn't supported";
		private const string SceneNullMsg = "Build stop! scene#{0} is NULL";

		#endregion

		#region Constants

		private const int MaxCountOfInstances = 5;
		private const int MinCountOfInstances = 1;

		private const string ExecutionExtension = ".exe";
		private const string SceneExtension = ".unity";

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
		
		[SerializeField] private bool autoConnect;
		[SerializeField] private bool editorIsServer;
		[SerializeField] private bool editorIsClient;

		private Vector2 scrollPos; // Память для вертикального скролла
		
		#endregion

		[MenuItem("Window/Multirun")]
		public static void ShowWindow()
		{
#if UNITY_EDITOR_WIN
			GetWindow<Multirun>("Multirun");
#else
		Debug.LogError(OSMsg);
#endif
		}

		private void OnGUI()
		{
			scrollPos = EditorGUILayout.BeginScrollView(scrollPos); // Память для вертикального скролла

			EditorGUILayout.Space();
			countOfInstances = EditorGUILayout.IntField(CountOfInstancesLabel, countOfInstances);

			autoConnect = EditorGUILayout.Toggle(AutoConnectLabel, autoConnect);

			if (autoConnect)
			{
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
			}
			
			EditorGUILayout.Space();

			// Отображение прочих опций
			showOtherConfiguration = EditorGUILayout.Foldout(showOtherConfiguration, OtherConfigLabel);
			if (showOtherConfiguration)
			{
				EditorGUI.indentLevel = 1;
				argHandlerTag = EditorGUILayout.TextField(TagNameLabel, argHandlerTag);
				ip = EditorGUILayout.TextField(IpLabel, ip);
				port = EditorGUILayout.IntField(PortLabel, port);

				EditorGUILayout.Space();
				EditorGUILayout.LabelField(BuildConfigLabel);

				EditorGui.DrawList(ScenesLabel, scenes, false);

				buildPath = EditorGUILayout.TextField(BuildPathLabel, buildPath);

				EditorGUI.indentLevel = 0;
			}

			if (countOfInstances < MinCountOfInstances)
			{
				countOfInstances = MinCountOfInstances;
			}
			else if (countOfInstances > MaxCountOfInstances)
			{
				countOfInstances = MaxCountOfInstances;
			}

			EditorGUILayout.Space();

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button(RunButtonLabel))
			{
				ShowRunReport(false);
				Run();
			}

			if (GUILayout.Button(BuildRunButtonLabel))
			{
				ShowRunReport(true);
				BuildAndRun();
				// Костыль! После билда юнити забывает о том, что разметка перешла в горизонталь
				EditorGUILayout.BeginHorizontal();
			}

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndScrollView();
		}

		private static string FindSceneLocation(string name)
		{
			var dirs = Directory.GetFiles("Assets", name, SearchOption.AllDirectories);

			return dirs.Length > 0 ? dirs[0] : null;
		}

		private void Run()
		{
			var proc = new Process();
			proc.StartInfo.FileName = buildPath + Application.productName + ExecutionExtension;

			var i = 0;
			if (editorIsServer || editorIsClient)
			{
				EditorApplication.isPlaying = true; // Старт в редакторе Unity
				i++;
			}

			for (; i < countOfInstances; i++)
			{
				proc.Start();
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

			buildPlayerOptions.locationPathName = buildPath + Application.productName + ExecutionExtension; // Полный путь до билда с именем исполняемого файла
			buildPlayerOptions.target = BuildTarget.StandaloneWindows;
			buildPlayerOptions.options = BuildOptions.None;
			BuildPipeline.BuildPlayer(buildPlayerOptions);
			return true;
		}

		private void BuildAndRun()
		{
			if (Build()) Run();
		}

		private void ShowRunReport(bool withBuild)
		{
			var reportMsg = new StringBuilder(string.Format(RequestMsgPattern,
				withBuild ? BuildMsg : RunMsg,
				buildPath + Application.productName + ExecutionExtension,
				countOfInstances,
				editorIsServer || editorIsClient ? WithEditorMsg : ""
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