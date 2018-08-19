using System;
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
    private const string SceneLabel = "Scene #";
    private const string OsLabel = "Operation System";

    // Buttons
    private const string RunButtonLabel = "Run";
    private const string BuildRunButtonLabel = "Build and Run";

    #endregion

    #region Error Messages

    private const string TagErrorMessage = "Tag not found and ArgumentHandler!";
    private const string ArgHandlerErrorMessage = "ArgumentHandler not found!";
    private const string OsErrorMessage = "Unknown OS \"{0}\"";

    #endregion


    private Vector2 scrollPos;                       // Для вертикальной полосы
    [SerializeField] private SceneAsset[] scenesObj; // Сами сцены 

    private const int MaxCountScenes = 10; // Максимальное количество сцен для редактирования 
    private const int MinCountScenes = 1;         // Минимальное количество сцен 
    [SerializeField] private int countScenes = 1; // Для куска кода, который следит за изменением количества сцен
    [SerializeField] private int oldCountScene = 0;
    [SerializeField] private int countStartScenes = 1;       // Количество сцен требуемые для разового запуска 
    private const int MaxCountStartScenes = 5;               // максимальное количество сцен для запуска
    private const int MinCountStartScenes = 1;               // Минимальное количество сцен для запуска
    [SerializeField] private string ipAddress = "127.0.0.1"; // Сервер, база
    [SerializeField] private int port = 7777;

    private string buildName; // Имя окончательного билда
    [SerializeField] private string tagName = "Handler";

    [SerializeField] private string pathBuild = "Build\\"; // Путь окончательного билда (В windows обратный \) редактировать в unity

    private const string PathScene = "Assets/Scenes/"; //Путь до место расположения самих сцен 
    private string nameExtension;                      //доп переменная для определения расширения
    [SerializeField] private string[] scenes;          //Полное имя сцен

    [SerializeField] private bool isServer; //Выбор запуска сервера или только клиента false - только клиенты
    [SerializeField] private bool isConnect;
    [SerializeField] private bool isPlayUnity;
    [SerializeField] private bool isServerInUnity;
    [SerializeField] private bool isShowOptions;
    private bool isEnableServer;
    private bool isCus = false;
    private bool isError = false;
    [SerializeField] private GameObject obj;
    private ArgumentHandler handler;
    [SerializeField] private DefaultSystem defaultSystem = DefaultSystem.Windows;

    [MenuItem("Window/Multirun")]
    public static void ShowWindow()
    {
        GetWindow<Multirun>("Multirun");
    }

    private bool FindArgHandlerObj() //Поиск объекта с тегом, а также компонента ArgumentHandler
    {
        if (!isError) //Проверка на повторный запуск скрипта, так как находится в OnGUI и межет быть вызван более 1 раза 
        {
            obj = GameObject.FindGameObjectWithTag(tagName); //Поиск объекта с тегом
            if (obj == null)                                 // Если объект не найден, то выдать исключение, в этом случае в debug log
            {
                Debug.LogError(TagErrorMessage);
                isError = true;
                return false;
            }
            else handler = obj.GetComponentInParent<ArgumentHandler>(); //поиск компонента в объекте

            if (handler == null) //Обработчик исключений
            {
                Debug.LogError(ArgHandlerErrorMessage);
                isError = true;
                return false;
            }
            else return true; //вернем истину, если объект найден
        }
        else return false; //возвращает ложь,если была ранее выдана ошибка
    }

    private void OnGUI()
    {
        if (isServerInUnity && EditorApplication.isPlaying) //Поиск и запуск в редакторе Unity в качестве сервера
        {
            if (isEnableServer)
            {
                if (FindArgHandlerObj()) //Обработчик исключений, делает независимым скрипт Multirun от ArgumentHandler
                {
                    handler.EditorEvent("server", ipAddress, port);
                    isEnableServer = false;
                }
            }
        }

        if (isCus && EditorApplication.isPlaying) //Поиск и запуск в редакторе Unity в качестве клиента
        {
            if (FindArgHandlerObj()) //Обработчик исключений, делает независимым скрипт Multirun от ArgumentHandler
            {
                isCus = false;
                handler.EditorEvent("client", ipAddress, port);
            }
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos); //Вертикальная полоса прокрутки 

        EditorGUILayout.Space();
        countStartScenes = EditorGUILayout.IntField(CountOfInstancesLabel, countStartScenes); //Количество экземпляров приложения
        isServer = EditorGUILayout.Toggle(ServerRunLabel, isServer);                          //Чекбокс на запуск сервера, написать чтобы при выборе 
        //Логика поведения чекбоксов, для запуска режима
        if (countStartScenes != 1 && isServer)
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
            if (countScenes > MaxCountScenes) //Проверка на количество сцен
            {
                countScenes = MaxCountScenes;
            }
            else if (countScenes < MinCountScenes)
            {
                countScenes = MinCountScenes;
            }
            else
            {
                if ((countScenes != oldCountScene)) //Проверка состояния изменения сцены
                {
                    scenes = new string[countScenes]; //Выделить новый массив с новым количеством сцен
                    scenesObj = new SceneAsset[countScenes];
                    oldCountScene = countScenes; //Указываем новое значение для дальнейших проверок
                }

                for (int i = 0; i < countScenes; i++) //Отрисовываем и записываем значения полей в массив scenes
                {
                    scenesObj[i] = EditorGUILayout.ObjectField(SceneLabel + i, scenesObj[i], typeof(SceneAsset), false) as SceneAsset;
                }
            }

            defaultSystem = (DefaultSystem) EditorGUILayout.EnumPopup(OsLabel, defaultSystem); //Выбор дефолтной системы по умолчанию GUI
            pathBuild = EditorGUILayout.TextField(BuildPathLabel, pathBuild);

            EditorGUI.indentLevel = 0;
        }

        if (countStartScenes < MinCountStartScenes) //ограничения устанавливаемых сцен
        {
            countStartScenes = MinCountStartScenes;
        }
        else if (countStartScenes > MaxCountStartScenes)
        {
            countStartScenes = MaxCountStartScenes;
        }

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(RunButtonLabel))
        {
            Run(); //Запуск билда в режиме defaultSystem
        }

        if (GUILayout.Button(BuildRunButtonLabel))
        {
            BuildAndRun(); //Компиляция билда и запуск в режиме defaultSystem

            // Костыль! После билда юнити забывает о том, что разметка перешла в горизонталь
            EditorGUILayout.BeginHorizontal();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView(); //конец отрисовки полосы прокрутки
    }

    private void Run()
    {
        isError = false;
        isEnableServer = true;
        switch (defaultSystem) //определение ОС
        {
            case DefaultSystem.Linux:
                //Вызывается скрипт bash для выставления прав на запуск в linux в папке с проектом необходим скрипт npfs.sh так же с правиви исполнения 
                var chmod = new Process();
                chmod.StartInfo.FileName = "./npfs.sh"; //Установка имени исполняемого файла 
                chmod.StartInfo.Arguments = buildName;  //Установка аргументов
                chmod.Start();                          //запуск самого приложения в данном случае скрипта 
                nameExtension = "";
                break;
            case DefaultSystem.Windows:
                nameExtension = ".exe";
                break;
            default:
                throw new NotImplementedException(string.Format(OsErrorMessage, defaultSystem));
        }

        var proc = new Process();                    //Запуск самой программы
        proc.StartInfo.FileName = pathBuild + buildName; //Выбор билда и последующий запуск, сделать запуск по аргументам для сервера

        for (var i = 0; i < countStartScenes; i++)
        {
            if (isServer && (i == 0))
            {
                proc.StartInfo.Arguments = "server " + port;
                if (isServerInUnity)
                {
                    EditorApplication.isPlaying = true; //Старт в unity
                }
                else proc.Start();
            }

            if (isConnect && (i != 0))
            {
                proc.StartInfo.Arguments = "client " + ipAddress + " " + port; //Установка аргументов
                if (isPlayUnity && (i == countStartScenes - 1) && !isServerInUnity)
                {
                    isCus = true;
                    EditorApplication.isPlaying = true;
                }
                else proc.Start();
            }

            if ((!isConnect && !isServer) || (!isConnect && isServer && i != 0))
            {
                proc.StartInfo.Arguments = "";
                if (isPlayUnity && (i == countStartScenes - 1) && (!isServerInUnity))
                {
                    EditorApplication.isPlaying = true;
                }
                else proc.Start();
            }
        }

        //Тут был бип
    }

    // Returns true if build was success
    private bool Build()
    {
        buildName = Application.productName + nameExtension;
        for (var i = 0; i < countScenes; i++)
        {
            if (scenesObj[i] != null)
            {
                scenes[i] = PathScene + scenesObj[i].name + ".unity";
            }
            else
            {
                Debug.LogError("Build stop! scene#" + i + " is NULL");
                return false;
                // todo доработать скрипт, чтобы можно было указывать свой путь к сценам и чтоб не было ошибок при добавление из другой папки 
            }
        }

        //Билд проекта
        var buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new string[countScenes]; //Создаем массив с количеством сцен
        for (var i = 0; i < countScenes; i++)                //Заполняем имена сцен и добавляем все в buildPlayerOptions.scenes 
        {
            buildPlayerOptions.scenes[i] = scenes[i]; //Собственно добавляем 
        }

        buildPlayerOptions.locationPathName = pathBuild + buildName; //Путь до билда
        switch (defaultSystem)                                              //определение ОС
        {
            case DefaultSystem.Linux: //Команды для компиляции в GNU/LINUX
                buildPlayerOptions.target = BuildTarget.StandaloneLinux64; //Компилировать по GNU/Linux
                break;
            case DefaultSystem.Windows: //В окнах
                buildPlayerOptions.target = BuildTarget.StandaloneWindows; //Windows
                break;
            default:
                throw new NotImplementedException(string.Format(OsErrorMessage, defaultSystem));
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