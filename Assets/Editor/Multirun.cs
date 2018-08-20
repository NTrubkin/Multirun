using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

[Serializable]
public class Multirun : EditorWindow
{
    // Список ОС, поддерживаемых плагином
    private enum OperatingSystem
    {
        Linux = 0,
        Windows = 1
    }

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
    private const string CountOfScenesLabel = "Count of Scenes";
    private const string SceneLabel = "Scene #";
    private const string OsLabel = "Operating System";

    // Buttons
    private const string RunButtonLabel = "Run";
    private const string BuildRunButtonLabel = "Build and Run";

    #endregion

    #region Error Messages

    private const string TagErrorMessage = "Tag not found and ArgumentHandler!";
    private const string ArgHandlerErrorMessage = "ArgumentHandler not found!";
    private const string OsErrorMessage = "Unknown OS \"{0}\"";

    #endregion

    #region Constants

    private const int MaxCountOfScenes = 10;
    private const int MinCountOfScenes = 1;

    private const int MaxCountOfInstances = 5;
    private const int MinCountOfInstances = 1;

    private const string ScenesPath = "Assets/Scenes/"; //Путь до место расположения самих сцен 

    #endregion

    // Variables that are used for store data from visual fields in Multirun tab
    #region TabCache

    [SerializeField] private SceneAsset[] scenes;

    [SerializeField] private int countOfScenes = 1; // Для куска кода, который следит за изменением количества сцен
    [SerializeField] private int oldCountOfScenes = 0;
    [SerializeField] private int countOfInstances = 1; // Instance - это экземпляр игры (новое окно или внутри редактора)

    [SerializeField] private string ip = "127.0.0.1";
    [SerializeField] private int port = 7777;

    [SerializeField] private string argHandlerTag = "Handler";

    [SerializeField] private string buildPath = "Build\\"; // Помни, что Linux использует "/", а Windows "\"

    [SerializeField] private string[] sceneNames; // todo упразднить

    [SerializeField] private bool runServer;
    [SerializeField] private bool runClients;
    [SerializeField] private bool runInEditor;
    [SerializeField] private bool runEditorAsServer;
    [SerializeField] private bool showOtherConfigs;

    [SerializeField] private OperatingSystem system = OperatingSystem.Windows;

    #endregion

    private Vector2 scrollPos; // Память для вертикального скролла
    private string buildName;
    private string nameExtension; // todo упразднить (спрятать в методы)
    private bool isServerEnabled;
    private bool runEditorAsClient = false; // todo проверить правильность имени, если нет, вернуть имя isCus. Попытаться упразднить
    private bool isError = false;
    private ArgumentHandler handler;


    [MenuItem("Window/Multirun")]
    public static void ShowWindow()
    {
        GetWindow<Multirun>("Multirun");
    }

    // Поиск объекта с тегом, а также компонента ArgumentHandler
    private bool FindArgHandlerObj()
    {
        //Проверка на повторный запуск скрипта, так как находится в OnGUI и межет быть вызван более 1 раза 
        if (!isError)
        {
            var argHandlerObj = GameObject.FindGameObjectWithTag(argHandlerTag);
            if (argHandlerObj == null)
            {
                // Если объект не найден, дальнейшая работа скрипта невозможна
                Debug.LogError(TagErrorMessage);
                isError = true;
                return false;
            }
            else handler = argHandlerObj.GetComponentInParent<ArgumentHandler>();

            if (handler == null)
            {
                // Если компонент не найден, дальнейшая работа скрипта невозможна
                Debug.LogError(ArgHandlerErrorMessage);
                isError = true;
                return false;
            }
            else return true; // вернем истину, если объект найден
        }
        else return false; // возвращает ложь,если была ранее выдана ошибка
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
                    handler.EditorEvent("server", ip, port);
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
                handler.EditorEvent("client", ip, port);
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
        showOtherConfigs = EditorGUILayout.Toggle(OtherConfigLabel, showOtherConfigs);
        if (showOtherConfigs)
        {
            EditorGUI.indentLevel = 1;
            argHandlerTag = EditorGUILayout.TextField(TagNameLabel, argHandlerTag);
            ip = EditorGUILayout.TextField(IpLabel, ip);
            port = EditorGUILayout.IntField(PortLabel, port);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(BuildConfigLabel);

            countOfScenes = EditorGUILayout.IntField(CountOfScenesLabel, countOfScenes);
            if (countOfScenes > MaxCountOfScenes)
            {
                countOfScenes = MaxCountOfScenes;
            }
            else if (countOfScenes < MinCountOfScenes)
            {
                countOfScenes = MinCountOfScenes;
            }
            else
            {
                if ((countOfScenes != oldCountOfScenes)) // Проверка состояния изменения сцены
                {
                    sceneNames = new string[countOfScenes]; // Выделить новый массив с новым количеством сцен
                    scenes = new SceneAsset[countOfScenes];
                    oldCountOfScenes = countOfScenes; // Указываем новое значение для дальнейших проверок
                }

                for (int i = 0; i < countOfScenes; i++) // Отрисовываем и записываем значения полей в массив scenes
                {
                    scenes[i] = EditorGUILayout.ObjectField(SceneLabel + i, scenes[i], typeof(SceneAsset), false) as SceneAsset;
                }
            }

            system = (OperatingSystem) EditorGUILayout.EnumPopup(OsLabel, system); // Отрисовка селектора операционной системы
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
            Run();
        }

        if (GUILayout.Button(BuildRunButtonLabel))
        {
            BuildAndRun();

            // Костыль! После билда юнити забывает о том, что разметка перешла в горизонталь
            EditorGUILayout.BeginHorizontal();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();
    }

    private void Run()
    {
        isError = false;
        isServerEnabled = true;
        switch (system)
        {
            case OperatingSystem.Linux:
                // todo узнать, что такое "правиви" (смотри коммент ниже)
                // Вызывается скрипт bash для выставления прав на запуск в linux в папке с проектом необходим скрипт npfs.sh так же с правиви исполнения 
                var chmod = new Process();
                chmod.StartInfo.FileName = "./npfs.sh"; // Установка имени исполняемого файла 
                chmod.StartInfo.Arguments = buildName;  // Установка аргументов
                chmod.Start();                          // запуск самого приложения в данном случае скрипта 
                nameExtension = "";
                break;
            case OperatingSystem.Windows:
                nameExtension = ".exe";
                break;
            default:
                throw new NotImplementedException(string.Format(OsErrorMessage, system));
        }

        var proc = new Process();
        proc.StartInfo.FileName = buildPath + buildName;

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
        buildName = Application.productName + nameExtension;
        for (var i = 0; i < countOfScenes; i++)
        {
            if (scenes[i] != null)
            {
                sceneNames[i] = ScenesPath + scenes[i].name + ".unity";
            }
            else
            {
                Debug.LogError("Build stop! scene#" + i + " is NULL");
                return false;
                // todo доработать скрипт, чтобы можно было указывать свой путь к сценам и чтоб не было ошибок при добавление из другой папки 
            }
        }

        // Билд проекта
        var buildPlayerOptions = new BuildPlayerOptions();
        // todo избавиться от велосипеда - найти конструктор массива с копией существующего
        buildPlayerOptions.scenes = new string[countOfScenes]; // Создаем массив с количеством сцен
        for (var i = 0; i < countOfScenes; i++)                // Заполняем имена сцен и добавляем все в buildPlayerOptions.scenes 
        {
            buildPlayerOptions.scenes[i] = sceneNames[i]; // Собственно добавляем 
        }

        buildPlayerOptions.locationPathName = buildPath + buildName; // Полный путь до билда с именем исполняемого файла
        switch (system)
        {
            case OperatingSystem.Linux:
                buildPlayerOptions.target = BuildTarget.StandaloneLinux64;
                break;
            case OperatingSystem.Windows:
                buildPlayerOptions.target = BuildTarget.StandaloneWindows;
                break;
            default:
                throw new NotImplementedException(string.Format(OsErrorMessage, system));
        }

        buildPlayerOptions.options = BuildOptions.None;
        BuildPipeline.BuildPlayer(buildPlayerOptions);
        return true;
    }

    private void BuildAndRun()
    {
        if (Build()) Run();
    }

    private void ShowRunReport()
    {
        // todo implement
        throw new NotImplementedException("not implemented yet");
    }
}