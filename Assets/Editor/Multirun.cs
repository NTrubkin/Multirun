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
		private const string ServerRunLabel = "Autorun Server";
		private const string ClientRunLabel = "Autorun Clients";
		private const string EditorRunLabel = "Run in Editor";
		private const string EditorIsServerLabel = "Editor is Server";
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
		private const string ConnectMsgPattern = "Server: {0}; Clients: {1}; Сonnect to: IP: {2}; Port {3}; ";
		private const string BuildMsg = "Build and Run";
		private const string RunMsg = "Run";
		private const string WithEditorMsg = "with editor";

		#endregion

		#region Error Messages

		private const string ArgHandlerErrorMessage = "ArgumentHandler not found!";
		private const string OSMsg = "Your OS doesn't supported";

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

		[SerializeField] private string buildPath = "Build\\"; // Помни, что Linux использует "/", а Windows "\"

		[SerializeField] private bool runServer;
		[SerializeField] private bool runClients;
		[SerializeField] private bool runInEditor;
		[SerializeField] private bool runEditorAsServer;
		[SerializeField] private bool showOtherConfiguration;

		#endregion

		private Vector2 scrollPos; // Память для вертикального скролла
		private string buildName;
		private bool isServerEnabled;
		private bool runEditorAsClient = false; // todo проверить правильность имени, если нет, вернуть имя isCus. Попытаться упразднить

		private bool isError = false;
		//private ArgumentHandler handler;


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
			// Поиск обработчика аргументов и запуск в редакторе Unity в качестве сервера
			if (runEditorAsServer && EditorApplication.isPlaying)
			{
				if (isServerEnabled)
				{
					if (FindArgHandlerObj())
					{
						ArgumentHandler.Singleton.EditorEvent("server", ip, port);
						isServerEnabled = false;
					}
				}
			}

			// Поиск обработчика аргументов и запуск в редакторе Unity в качестве клиента
			if (runEditorAsClient && EditorApplication.isPlaying)
			{
				if (FindArgHandlerObj())
				{
					runEditorAsClient = false;
					ArgumentHandler.Singleton.EditorEvent("client", ip, port);
				}
			}

			scrollPos = EditorGUILayout.BeginScrollView(scrollPos); // Память для вертикального скролла

			EditorGUILayout.Space();
			countOfInstances = EditorGUILayout.IntField(CountOfInstancesLabel, countOfInstances);
			runServer = EditorGUILayout.Toggle(ServerRunLabel, runServer);

			// Логика поведения чекбоксов
			if (countOfInstances != 1 && runServer)
			{
				// чекбокс на автозапуск клиента
				runClients = EditorGUILayout.Toggle(ClientRunLabel, runClients);
			}
			else
			{
				runClients = false;
			}

			// чекбокс на автозапуск приложения в редакторе 
			runInEditor = EditorGUILayout.Toggle(EditorRunLabel, runInEditor);

			// проверка на принудительный запуск сервера в редакторе, когда выбран один экземпляр приложения и запуск выполняется в unity
			if ((countOfInstances == 1) && runServer && runInEditor)
			{
				runEditorAsServer = true;
			}

			// отображение чекбокса "runEditorAsServer", когда это возможно 
			if (runInEditor && runServer)
			{
				EditorGUI.indentLevel = 1;
				runEditorAsServer = EditorGUILayout.Toggle(EditorIsServerLabel, runEditorAsServer);
				EditorGUI.indentLevel = 0;
			}
			else runEditorAsServer = false;
			// конец логики чекбоксов

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

		// Поиск компонента ArgumentHandler
		private bool FindArgHandlerObj()
		{
			if (!isError)
			{
				//Поиск через singleton
				if (ArgumentHandler.Singleton == null)
				{
					Debug.LogError(ArgHandlerErrorMessage);
					isError = true;
					return false;
				}
				else return true; // вернем истину, если объект найден
			}
			else return false; // возвращает ложь,если была ранее выдана ошибка
		}

		private static string FindSceneLocation(string name)
		{
			var dirs = Directory.GetFiles("Assets", name, SearchOption.AllDirectories);

			return dirs.Length > 0 ? dirs[0] : null;
		}

		private void Run()
		{
			isError = false;
			isServerEnabled = true;


			var proc = new Process();
			proc.StartInfo.FileName = buildPath + Application.productName + ExecutionExtension;

			for (var i = 0; i < countOfInstances; i++)
			{
				if (runServer && (i == 0))
				{
					proc.StartInfo.Arguments = "server " + port;
					if (runEditorAsServer)
					{
						EditorApplication.isPlaying = true; // Старт в редакторе Unity
					}
					else proc.Start();
				}

				if (runClients && (i != 0))
				{
					proc.StartInfo.Arguments = "client " + ip + " " + port;
					if (runInEditor && (i == countOfInstances - 1) && !runEditorAsServer)
					{
						runEditorAsClient = true;
						EditorApplication.isPlaying = true;
					}
					else proc.Start();
				}

				if ((!runClients && !runServer) || (!runClients && runServer && i != 0))
				{
					proc.StartInfo.Arguments = "";
					if (runInEditor && (i == countOfInstances - 1) && (!runEditorAsServer))
					{
						EditorApplication.isPlaying = true;
					}
					else proc.Start();
				}
			}

			// Тут был бип
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
					Debug.LogError("Build stop! scene#" + i + " is NULL");
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
			// todo переписать
			var reportMsg = new StringBuilder(string.Format(RequestMsgPattern,
				withBuild ? BuildMsg : RunMsg,
				buildPath + Application.productName + ExecutionExtension,
				countOfInstances,
				runInEditor ? WithEditorMsg : ""
			));
			
			if (runServer || runClients)
			{
				var countOfClients = 0;

				if (runClients) countOfClients = countOfInstances;
				if (runServer) countOfClients--;
				
				reportMsg.Append(string.Format(ConnectMsgPattern,
					runServer ? 1 : 0,
					countOfClients,
					ip,
					port));
			}

			Debug.Log(reportMsg);
		}
	}
}