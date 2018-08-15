using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
public class ArgumentHandler : MonoBehaviour
{
    public static ArgumentHandler singleton;
    private int countArgs;
    private int port = 7777;
    private string args;
    private string ip = "127.0.0.1";
    private bool isStartInUnity = false;
    // Use this for initialization
    void Start()
    {
        countArgs = System.Environment.GetCommandLineArgs().Length;
        if (countArgs != 1)
        {
            switch (System.Environment.GetCommandLineArgs()[1])
            {
                case "server":
                    if (countArgs == 2)
                    {
                        SetPort(port);
                    }
                    else
                    {
                        SetPort(ConvertStrToInt(System.Environment.GetCommandLineArgs()[2]));
                    }
                    StartHost();
                    break;
                case "client":
                    ip = System.Environment.GetCommandLineArgs()[2];
                    port = ConvertStrToInt(System.Environment.GetCommandLineArgs()[3]);
                    Connect(ip, port);
                    break;
                default:
                    break;
            }
        }
    }
    int ConvertStrToInt(string str)
    {
        return System.Convert.ToInt32(str);
    }
    void Connect(string ip, int port)
    {
        SetIpAddr(ip);
        SetPort(port);
        try
        {
            NetworkManager.singleton.StartClient();
        }
        catch
        {
            Debug.Log("Start client stop! NetworkManager not fond");
        }
    }
    void SetIpAddr(string ip)
    {
        try
        {
            NetworkManager.singleton.networkAddress = ip;
        }
        catch
        {
            Debug.Log("Error set ip! NetworkManager not fond");
        }
    }
    void SetPort(int port)
    {
        try
        {
            NetworkManager.singleton.networkPort = port;
        }
        catch
        {
            Debug.Log("Error set port! NetworkManager not fond");
        }
    }
    void StartHost()
    {
        try
        {
            NetworkManager.singleton.StartHost();
        }
        catch
        {
            Debug.Log("Start host stop! NetworkManager not fond");
        }
    }
    public void EditorEvent(string args, string ip, int port)
    {
        this.args = args;
        this.ip = ip;
        this.port = port;
        isStartInUnity = true;
    }
    void Update()
    {
        if (isStartInUnity)
        {
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
                    break;
            }
            isStartInUnity = false;
        }
    }
}
