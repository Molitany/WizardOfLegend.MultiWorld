using MultiWorld.ArchipelagoClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MultiWorld.UI;
public class ChatBoxController : MonoBehaviour
{
    public static GameObject ChatBox;
    private GameObject ChatLog;
    private Text ChatLogContent;
    private GameObject ChatInput;
    public string AssetName = "ChatBox";
    public string BundleName = "connectbundle";
    public bool IsActive = true;

    private readonly Font font = TextManager.fontDict[ChaosLang.English].font;
    private string command;
    private Coroutine coroutine;
    private bool caseInsensitive = true;
    private Dictionary<string, Action<string[]>> availableCommands;

    public void Start()
    {
        var connectPanelAsset = AssetBundleHelper.LoadPrefab(AssetName);
        AddCommands();
        ChatBox = Instantiate(connectPanelAsset);
        ChatLog = GameObject.Find("Canvas/ChatText");
        ChatLogContent = GameObject.Find("Canvas/ChatText/Viewport/Content").GetComponent<Text>();
        CreateInteractivity();
        var texts = GameObject.Find("Canvas").GetComponentsInChildren<Text>();
        foreach (var text in texts)
        {
            text.font = font;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (IsActive)
            {
                SendCommand(command);
            }
            else
            {
                ChatInput.SetActive(true);
                ChatInput.GetComponent<InputField>().ActivateInputField();
                StopCoroutine(coroutine);
                ChatLog.GetComponent<CanvasGroup>().alpha = 1f;
            }
            IsActive = !IsActive;
        }
    }

    private void AddCommands()
    {
        availableCommands = new()
        {
            { "help", Help },
            { "status", Status },
            { "connect", Connect },
            { "disconnect", Disconnect },
            { "deathlink", DeathLink },
            { "say", Say },
        };
    }

    private bool ValidateParameterList(string[] parameters, int amount, string helpText = null)
    {
        bool result = amount == parameters.Length;
        if (!result)
        {
            WriteToChatLog(helpText ?? $"This command takes {amount} parameters.  You passed {parameters.Length}");
        }
        return result;
    }

    private void Help(string[] parameters)
    {
        if (!ValidateParameterList(parameters, 0)) return;

        WriteToChatLog("Available MULTIWORLD commands:");
        WriteToChatLog("status: Display connection status");
        WriteToChatLog("connect URL:PORT NAME [PASSWORD]: Connect to URL:PORT with player name as NAME with optional PASSWORD");
        WriteToChatLog("disconnect: Disconnect from current server");
        WriteToChatLog("deathlink: Toggles deathlink on/off");
        WriteToChatLog("say COMMAND: Sends a text message or command to the server");
    }

    private void Status(string[] parameters)
    {
        if (!ValidateParameterList(parameters, 0)) return;
        var server = MultiWorldPlugin.ArchipelagoManager.ServerAddress;
        if (server == string.Empty)
            WriteToChatLog("Not connected to server");
        else
            WriteToChatLog($"Connected to {server}");
    }

    private void Connect(string[] parameters)
    {
        if (MultiWorldPlugin.ArchipelagoManager.Connected)
        {
            WriteToChatLog("Already connected to server!");
            return;
        }


        if (parameters.Length < 2)
        {
            WriteToChatLog($"Connect requires 2 or 3 parameters. You passed {parameters.Length}");
            return;
        }

        string url = parameters[0];
        string name = "";
        string password = null;
        int passwordIndex = -1;

        if (parameters[1].StartsWith("\""))
        {
            for (int i = parameters.Length - 1; i >= 1; i--)
            {
                if (parameters[i].EndsWith("\""))
                {
                    passwordIndex = i + 1;
                    break;
                }
            }

            if (passwordIndex == -1)
            {
                WriteToChatLog("Invalid syntax!");
                return;
            }

            for (int i = 1; i < passwordIndex; i++)
            {
                name += $"{parameters[i]} ";
            }
            name = name.Substring(1, name.Length - 3);
        }
        else
        {
            name = parameters[1];
            passwordIndex = 2;
        }

        if (parameters.Length > passwordIndex + 1)
        {
            WriteToChatLog($"Connect requires 2 or 3 parameters. You passed {parameters.Length}");
            return;
        }

        if (parameters.Length > passwordIndex)
        {
            password = parameters[passwordIndex];
        }

        WriteToChatLog($"Attempting to connect to {url} as {name}");
        WriteToChatLog(MultiWorldPlugin.ArchipelagoManager.Connect(url, name, password));
    }

    private void Disconnect(string[] parameters)
    {
        if (!ValidateParameterList(parameters, 0)) return;

        if (MultiWorldPlugin.ArchipelagoManager.Connected)
        {
            var server = MultiWorldPlugin.ArchipelagoManager.ServerAddress;
            MultiWorldPlugin.ArchipelagoManager.Disconnect();
            WriteToChatLog($"Disconnected from {server}");
        }
        else
            WriteToChatLog($"Not connected to any server!");

    }

    private void DeathLink(string[] parameters)
    {
        if (!ValidateParameterList(parameters, 0)) return;

        if (MultiWorldPlugin.ArchipelagoManager.Connected)
        {
            bool enabled = MultiWorldPlugin.DeathLinkManager.ToggleDeathLink();
            WriteToChatLog($"Death link has been {(enabled ? "enabled" : "disabled")}");
        }
        else
            WriteToChatLog("Not connected to any server");
    }

    private void Say(string[] parameters)
    {
        if (parameters.Length < 1)
        {
            WriteToChatLog($"This command is requires at least 1 parameter. You passed {parameters.Length}");
            return;
        }

        if (!MultiWorldPlugin.ArchipelagoManager.Connected)
        {
            WriteToChatLog("Not connected to server!");
            return;
        }

        MultiWorldPlugin.ArchipelagoManager.SendMessage(string.Concat(parameters.Skip(1).ToArray()));
    }

    private void CreateInteractivity()
    {
        ChatInput = GameObject.Find("Canvas/ChatInput");
        ChatInput.GetComponent<InputField>().onValueChanged.AddListener(value => command = value);
    }

    private void SendCommand(string command)
    {
        ChatInput.SetActive(false);
        var parameters = command.Split(' ');
        availableCommands[caseInsensitive ? parameters[0].ToLower() : parameters[0]](parameters.Skip(1).ToArray());
        coroutine = StartCoroutine(FadeOut());
    }

    private void WriteToChatLog(string command)
    {
        ChatLogContent.text += $"{command}\n";
    }

    private IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(2f);
        var canvasGroup = ChatLog.GetComponent<CanvasGroup>();
        while (!Mathf.Approximately(canvasGroup.alpha, 0))
        {
            canvasGroup.alpha -= 0.1f;
            yield return new WaitForEndOfFrame();
        }
        canvasGroup.alpha = 0;
    }
}
