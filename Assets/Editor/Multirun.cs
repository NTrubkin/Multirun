using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

[Serializable]
public class Multirun : EditorWindow
{
    // Список перечислений ОС
    public enum DefaultSystem
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
    private const string SceneLabel = "Scene #{0}";
    private const string OsLabel = "Operation System";

    // Buttons
    private const string RunButtonLabel = "Run";
    private const string BuildRunButtonLabel = "Build and Run";

    #endregion


    private Vector2 scrollPos;                       // Для вертикальной полосы
    [SerializeField] private SceneAsset[] scenesObj; // Сами сцены 
    [SerializeField] private List<SceneAsset> sceneAssets = new List<SceneAsset>();

    private int maxCountScenes = 10;              // Максимальное количество сцен для редактирования 
    private const int minCountScenes = 1;         // Минимальное количество сцен 
    [SerializeField] private int countScenes = 1; // Для куска кода, который следит за изменением количества сцен
    [SerializeField] private int oldCountScene = 0;
    [SerializeField] private int countStartScenes = 1;       // Количество сцен требуемые для разового запуска 
    private const int maxCountStartScenes = 5;               // максимальное количество сцен для запуска
    private const int minCountStartScenes = 1;               // Минимальное количество сцен для запуска
    [SerializeField] private string ipAddress = "127.0.0.1"; // Сервер, база
    [SerializeField] private int port = 7777;
    [SerializeField] private int serverPort = 7777;

    private string buildName; // Имя окончательного билда
    [SerializeField] private string tagName = "Handler";

    [SerializeField] private string pathBuild = "Build\\"; // Путь окончательного билда (В windows обратный \) редактировать в unity

    private const string pathScene = "Assets/Scenes/"; //Путь до место расположения самих сцен 
    private string nameExtension;                      //доп переменная для определения расширения
    [SerializeField] private string[] scenes;          //Полное имя сцен

    [SerializeField] private bool isBuild;  //Проверка на выбранные сцены
    [SerializeField] private bool isServer; //Выбор запуска сервера или только клиента false - только клиенты
    [SerializeField] private bool isConnect;
    [SerializeField] private bool isPlayUnity;
    [SerializeField] private bool isServerInUnity;
    [SerializeField] private bool isShowOptions;
    private bool isEnableServer;
    private bool isCUS = false;
    private bool isError = false;
    [SerializeField] private GameObject obj;
    private ArgumentHandler handler;
    public DefaultSystem defaultSystem = DefaultSystem.Windows;

    [MenuItem("Window/Multirun")]
    public static void ShowWindow()
    {
        GetWindow<Multirun>("Multirun");
    }

    bool FindArgHandlerObj() //Поиск объекта с тегом, а также компонента ArgumentHandler
    {
        if (!isError) //Проверка на повторный запуск скрипта, так как находится в OnGUI и межет быть вызван более 1 раза 
        {
            obj = GameObject.FindGameObjectWithTag(tagName); //Поиск объекта с тегом
            if (obj == null)                                 // Если объект не найден, то выдать исключение, в этом случае в debug log
            {
                ShowError(0);
                isError = true;
                return false;
            }
            else handler = obj.GetComponentInParent<ArgumentHandler>(); //поиск компонента в объекте

            if (handler == null) //Обработчик исключений
            {
                ShowError(1);
                isError = true;
                return false;
            }
            else return true; //вернем истину, если объект найден
        }
        else return false; //возвращает ложь,если была ранее выдана ошибка
    }

    void ShowError(int exc) //Вывод сообщения
    {
        switch (exc)
        {
            case 0:
                Debug.Log("Tag not found and ArgumentHandler!");
                break;
            case 1:
                Debug.Log("ArgumentHandler not found!");
                break;
            default:
                break;
        }
    }

    void OnGUI()
    {
        if (isServerInUnity && EditorApplication.isPlaying) //Поиск и запуск в редакторе Unity в качестве сервера
        {
            if (isEnableServer)
            {
                if (FindArgHandlerObj()) //Обработчик исключений, делает независимым скрипт Multirun от ArgumentHandler
                {
                    handler.EditorEvent("server", ipAddress, port);
                    isEnableServer = false;
                    //Debug.Log("SERVER STARTING!");
                }
            }
        }

        if (isCUS && EditorApplication.isPlaying) //Поиск и запуск в редакторе Unity в качестве клиента
        {
            if (FindArgHandlerObj()) //Обработчик исключений, делает независимым скрипт Multirun от ArgumentHandler
            {
                isCUS = false;
                handler.EditorEvent("client", ipAddress, port);
                //Debug.Log("CLIENT STARTING!");
            }
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos); //Вертикальная полоса прокрутки 

        EditorGUILayout.Space();
        countStartScenes = EditorGUILayout.IntField(CountOfInstancesLabel, countStartScenes); //Количество экземпляров приложения
        isServer = EditorGUILayout.Toggle(ServerRunLabel, isServer);                          //Чекбокс на запуск сервера, написать чтобы при выборе 
        //Логика поведения чекбоксов, для запуска режима
        if (countStartScenes != 1 && isServer == true)
        {
            isConnect = EditorGUILayout.Toggle(ClientRunLabel, isConnect); //чекбокс на автозапуск клиента
        }
        else
        {
            isConnect = false;
        }

        isPlayUnity = EditorGUILayout.Toggle(EditorRunLabel, isPlayUnity); //чекбокс на автозапуск приложения в редакторе 

        if ((countStartScenes == 1) && isServer && isPlayUnity) //проверка на принудительный запуск сервера в редакторе, когда выбран один экземпляр приложения и запуск выполняется в unity
        {
            isServerInUnity = true;
        }

        if (isPlayUnity && isServer) //отображение чекбокса "Server launching", когда это возможно 
        {
            EditorGUI.indentLevel = 1;
            isServerInUnity = EditorGUILayout.Toggle(EditorIsServerLabel, isServerInUnity);
            EditorGUI.indentLevel = 0;
        }
        else isServerInUnity = false;

        //конец
        EditorGUILayout.Space();
        //Отображение опций
        isShowOptions = EditorGUILayout.Toggle(OtherConfigLabel, isShowOptions);
        if (isShowOptions)
        {
            EditorGUI.indentLevel = 1;
            tagName = EditorGUILayout.TextField(TagNameLabel, tagName);
            ipAddress = EditorGUILayout.TextField(IpLabel, ipAddress);
            port = EditorGUILayout.IntField(PortLabel, port);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(BuildConfigLabel);

            countScenes = EditorGUILayout.IntField(CountOfScenesLabel, countScenes);
            if (countScenes > maxCountScenes) //Проверка на количество сцен
            {
                countScenes = maxCountScenes;
            }
            else if (countScenes < minCountScenes)
            {
                countScenes = minCountScenes;
            }
            else
            {
                if ((countScenes != oldCountScene)) //Проверка состояния изменения сцены
                {
                    scenes = new string[countScenes]; //Выделить новый массив с новым количеством сцен
                    //Debug.Log("count scenes: " + scenes.Length);
                    scenesObj = new SceneAsset[countScenes];
                    oldCountScene = countScenes; //Указываем новое значение для дальнейших проверок
                }

                for (int i = 0; i < countScenes; i++) //Отрисовываем и записываем значения полей в массив scenes
                {
                    scenesObj[i] =
                        EditorGUILayout.ObjectField(string.Format(SceneLabel, i), scenesObj[i], typeof(SceneAsset),
                                false) as
                            SceneAsset;
                }
            }

            defaultSystem = (DefaultSystem) EditorGUILayout.EnumPopup(OsLabel, defaultSystem); //Выбор дефолтной системы по умолчанию GUI
            pathBuild = EditorGUILayout.TextField(BuildPathLabel, pathBuild);

            EditorGUI.indentLevel = 0;
        }

        if (countStartScenes < minCountStartScenes) //ограничения устанавливаемых сцен
        {
            countStartScenes = minCountStartScenes;
        }
        else if (countStartScenes > maxCountStartScenes)
        {
            countStartScenes = maxCountStartScenes;
        }

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(RunButtonLabel))
        {
            Run(defaultSystem); //Запуск билда в режиме defaultSystem
        }

        if (GUILayout.Button(BuildRunButtonLabel))
        {
            BuildAndRun(defaultSystem); //Компиляция билда и запуск в режиме defaultSystem

            // Костыль! После билда юнити забывает о том, что разметка перешла в горизонталь
            EditorGUILayout.BeginHorizontal();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView(); //конец отрисовки полосы прокрутки
    }

    private void Run(DefaultSystem system)
    {
        isError = false;
        isEnableServer = true;
        //Debug.Log("path: " + pathBuild + buildName);
        switch (system) //определение ОС
        {
            case DefaultSystem.Linux:           //Вызывается скрипт bash для выставления прав на запуск в linux в папке с проектом необходим скрипт npfs.sh так же с правиви исполнения 
                Debug.Log("Select: GNU/Linux"); //только для UNIX!!!
                Process chmod = new Process();

                chmod.StartInfo.FileName = "./npfs.sh"; //Установка имени исполняемого файла 
                chmod.StartInfo.Arguments = buildName;  //Установка аргументов
                chmod.Start();                          //запуск самого приложения в данном случае скрипта 
                nameExtension = "";
                break;
            case DefaultSystem.Windows:
                Debug.Log("Select Windows"); //В окнах нет безопасности, поэтому нам пофигу 
                nameExtension = ".exe";
                break;
            default:
                Debug.LogError("System error, not valid authorization data"); //Заглушка, вдруг что-то будет не то 
                break;
        }

        Process proc = new Process();                    //Запуск самой программы
        proc.StartInfo.FileName = pathBuild + buildName; //Выбор билда и последующий запуск, сделать запуск по аргументам для сервера

        for (int i = 0; i < countStartScenes; i++)
        {
            if (isServer && (i == 0))
            {
                proc.StartInfo.Arguments = "server " + port;
                if (isServerInUnity)
                {
                    Debug.Log("Start server in Unity: " + "server");
                    EditorApplication.isPlaying = true; //Старт в unity
                }
                else proc.Start();
            }

            if (isConnect && (i != 0))
            {
                proc.StartInfo.Arguments = "client " + ipAddress + " " + port; //Установка аргументов
                if (isPlayUnity && (i == countStartScenes - 1) && !isServerInUnity)
                {
                    isCUS = true;
                    Debug.Log("Start client in Unity: " + "client " + ipAddress + " " + port);
                    EditorApplication.isPlaying = true;
                }
                else proc.Start();
            }

            if ((!isConnect && !isServer) || (!isConnect && isServer && i != 0))
            {
                proc.StartInfo.Arguments = "";
                if (isPlayUnity && (i == countStartScenes - 1) && (!isServerInUnity))
                {
                    Debug.Log("Start client in Unity: " + "default");
                    EditorApplication.isPlaying = true;
                }
                else proc.Start();
            }
        }

        //Тут был бип
    }

    // Returns true if build was success
    private bool Build(DefaultSystem system)
    {
        Debug.Log(scenes.Length);
        buildName = Application.productName + nameExtension;
        for (int i = 0; i < countScenes; i++)
        {
            if (scenesObj[i] != null)
            {
                scenes[i] = pathScene + scenesObj[i].name + ".unity";
            }
            else
            {
                Debug.Log("scene #" + i + " is NULL");
                Debug.LogError("Build stop! scene is NULL");
                return false;
                // todo доработать скрипт, чтобы можно было указывать свой путь к сценам и чтоб не было ошибок при добавление из другой папки 
            }
        }

        //Билд проекта
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new string[countScenes]; //Создаем массив с количеством сцен
        for (int i = 0; i < countScenes; i++)                //Заполняем имена сцен и добавляем все в buildPlayerOptions.scenes 
        {
            Debug.Log("Scene: count - " + i + " name - " + scenes[i]);
            buildPlayerOptions.scenes[i] = scenes[i]; //Собственно добавляем 
        }

        buildPlayerOptions.locationPathName = pathBuild + buildName; //Путь до билда
        switch (system)                                              //определение ОС
        {
            case DefaultSystem.Linux: //Команды для компиляции в GNU/LINUX
                Debug.Log("Select: GNU/Linux");
                buildPlayerOptions.target = BuildTarget.StandaloneLinux64; //Компилировать по GNU/Linux
                break;
            case DefaultSystem.Windows: //В окнах
                Debug.Log("Select Windows");
                buildPlayerOptions.target = BuildTarget.StandaloneWindows; //Windows
                break;
            default:
                Debug.LogError("System error, not valid authorization data");
                return false;
        }

        buildPlayerOptions.options = BuildOptions.None;
        BuildPipeline.BuildPlayer(buildPlayerOptions);
        return true;
    }

    private void BuildAndRun(DefaultSystem system)
    {
        if (Build(system)) Run(system);
    }
}