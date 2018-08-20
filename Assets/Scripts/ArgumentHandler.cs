using System;
using UnityEngine;
using UnityEngine.Networking;

public class ArgumentHandler : MonoBehaviour
{
    // todo решить вопрос с синглтоном
    public static ArgumentHandler singleton;
    private int countArgs;
    private int port = 7777;
    private string args;
    private string ip = "127.0.0.1";
    private bool isStartInUnity = false;

    private void Start()
    {
        countArgs = Environment.GetCommandLineArgs().Length;
        if (countArgs != 1)
        {
            switch (Environment.GetCommandLineArgs()[1])
            {
                case "server":
                    if (countArgs == 2)
                    {
                        SetPort(port);
                    }
                    else
                    {
                        SetPort(Convert.ToInt32(Environment.GetCommandLineArgs()[2]));
                    }

                    StartHost();
                    break;
                case "client":
                    ip = Environment.GetCommandLineArgs()[2];
                    port = Convert.ToInt32(Environment.GetCommandLineArgs()[3]);
                    Connect(ip, port);
                    break;
                default:
                    throw new ArgumentException("Wrong argument " + Environment.GetCommandLineArgs()[1]);
            }
        }
    }

    private void Update()
    {
        if (!isStartInUnity) return;

        switch (args)
        {
            case "server":
                SetPort(port);
                StartHost();
                break;
            case "client":
                Connect(ip, port);
                break;
            default:
                throw new ArgumentException("Wrong argument " + args);
        }

        isStartInUnity = false;
    }

    private static void Connect(string ip, int port)
    {
        SetIpAddr(ip);
        SetPort(port);

        if (NetworkManager.singleton == null)
        {
            Debug.LogError("NetworkManager not found");
        }
        else
        {
            NetworkManager.singleton.StartClient();
        }
    }

    private static void SetIpAddr(string ip)
    {
        if (NetworkManager.singleton == null)
        {
            Debug.LogError("NetworkManager not found");
        }
        else
        {
            NetworkManager.singleton.networkAddress = ip;
        }
    }

    private static void SetPort(int port)
    {
        if (NetworkManager.singleton == null)
        {
            Debug.LogError("NetworkManager not found");
        }
        else
        {
            NetworkManager.singleton.networkPort = port;
        }
    }

    private static void StartHost()
    {
        if (NetworkManager.singleton == null)
        {
            Debug.LogError("NetworkManager not found");
        }
        else
        {
            NetworkManager.singleton.StartHost();
        }
    }

    public void EditorEvent(string args, string ip, int port)
    {
        this.args = args;
        this.ip = ip;
        this.port = port;
        isStartInUnity = true;
    }
}