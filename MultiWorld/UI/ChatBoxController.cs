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
    public static ChatBoxController Instance;
    public static GameObject ChatBox;
    public static bool Writing;
    public string AssetName = "ChatBox";
    public string BundleName = "connectbundle";
    public bool IsActive = false;

    private GameObject ChatLog;
    private GameObject Content;
    private GameObject ChatInput;
    private readonly Font font = TextManager.fontDict[ChaosLang.English].font;
    private string command;
    private Coroutine coroutine;
    private readonly bool caseInsensitive = true;
    private Dictionary<string, Action<string[]>> availableCommands;

    public void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);
        else
            Instance = this;
    }

    public void Start()
    {
        var connectPanelAsset = AssetBundleHelper.LoadPrefab(AssetName);
        AddCommands();
        ChatBox = Instantiate(connectPanelAsset);
        ChatLog = GameObject.Find("Canvas/ChatText");
        Content = GameObject.Find("Canvas/ChatText/Viewport/Content");
        CreateInteractivity();
        var texts = GameObject.Find("Canvas").GetComponentsInChildren<Text>();
        foreach (var text in texts)
        {
            text.font = font;
        }
        ChatInput.SetActive(false);
        WriteToChat(MultiWorldPlugin.ChatLines);
        coroutine = StartCoroutine(FadeOut());
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (IsActive)
            {
                SendCommand(command);
                Writing = false;
            }
            else
            {
                ChatInput.SetActive(true);
                ChatInput.GetComponent<InputField>().ActivateInputField();
                Writing = true;
                if (coroutine != null)
                    StopCoroutine(coroutine);
                ChatLog.GetComponent<CanvasGroup>().alpha = 1f;
            }
            IsActive = !IsActive;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SendCommand("");
            IsActive = false;
            Writing = false;
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
            WriteToChat(helpText ?? $"This command takes {amount} parameters.  You passed {parameters.Length}");
        }
        return result;
    }

    private void Help(string[] parameters)
    {
        if (!ValidateParameterList(parameters, 0)) return;

        WriteToChat("Available MULTIWORLD commands:");
        WriteToChat("status: Display connection status");
        WriteToChat("connect URL:PORT NAME [PASSWORD]: Connect to URL:PORT with player name as NAME with optional PASSWORD");
        WriteToChat("disconnect: Disconnect from current server");
        WriteToChat("deathlink: Toggles deathlink on/off");
        WriteToChat("say COMMAND: Sends a text message or command to the server");
    }

    private void Status(string[] parameters)
    {
        if (!ValidateParameterList(parameters, 0)) return;
        var server = MultiWorldPlugin.ArchipelagoManager.ServerAddress;
        if (server == string.Empty)
            WriteToChat("Not connected to server");
        else
            WriteToChat($"Connected to {server}");
    }

    private void Connect(string[] parameters)
    {
        if (MultiWorldPlugin.ArchipelagoManager.Connected)
        {
            WriteToChat("Already connected to server!");
            return;
        }


        if (parameters.Length < 2)
        {
            WriteToChat($"Connect requires 2 or 3 parameters. You passed {parameters.Length}");
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
                WriteToChat("Invalid syntax!");
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
            WriteToChat($"Connect requires 2 or 3 parameters. You passed {parameters.Length}");
            return;
        }

        if (parameters.Length > passwordIndex)
        {
            password = parameters[passwordIndex];
        }

        WriteToChat($"Attempting to connect to {url} as {name}");
        WriteToChat(MultiWorldPlugin.ArchipelagoManager.Connect(url, name, password));
    }

    private void Disconnect(string[] parameters)
    {
        if (!ValidateParameterList(parameters, 0)) return;

        if (MultiWorldPlugin.ArchipelagoManager.Connected)
        {
            var server = MultiWorldPlugin.ArchipelagoManager.ServerAddress;
            MultiWorldPlugin.ArchipelagoManager.Disconnect();
            WriteToChat($"Disconnected from {server}");
        }
        else
            WriteToChat($"Not connected to any server!");

    }

    private void DeathLink(string[] parameters)
    {
        if (!ValidateParameterList(parameters, 0)) return;

        if (MultiWorldPlugin.ArchipelagoManager.Connected)
        {
            bool enabled = MultiWorldPlugin.DeathLinkManager.ToggleDeathLink();
            WriteToChat($"Death link has been {(enabled ? "enabled" : "disabled")}");
        }
        else
            WriteToChat("Not connected to any server");
    }

    private void Say(string[] parameters)
    {
        if (parameters.Length < 1)
        {
            WriteToChat($"This command is requires at least 1 parameter. You passed {parameters.Length}");
            return;
        }

        if (!MultiWorldPlugin.ArchipelagoManager.Connected)
        {
            WriteToChat("Not connected to server!");
            return;
        }

        WriteToChat(string.Join(" ", parameters));
        MultiWorldPlugin.ArchipelagoManager.SendMessage(string.Join(" ", parameters));
    }

    private void CreateInteractivity()
    {
        ChatInput = GameObject.Find("Canvas/ChatInput");
        ChatInput.GetComponent<InputField>().onValueChanged.AddListener(value => command = value);
    }

    private void SendCommand(string input)
    {
        input ??= "";
        ChatInput.SetActive(false);
        var parameters = input.Split(' ');
        var command = caseInsensitive ? parameters[0].ToLower() : parameters[0];
        if (availableCommands.TryGetValue(command, out var action))
            action(parameters.Skip(1).ToArray());
        coroutine = StartCoroutine(FadeOut());
    }

    public void WriteToChat(string input)
    {
        GameObject line = new($"line {MultiWorldPlugin.ChatLines.Count}", typeof(CanvasRenderer), typeof(RectTransform));
        line.transform.SetParent(Content.transform, false);
        var textComponent = line.AddComponent<Text>();
        textComponent.font = font;
        textComponent.text = input;
        if (!MultiWorldPlugin.ChatLines.Contains(input))
            MultiWorldPlugin.ChatLines.Add(input);
    }

    public void WriteToChat(List<string> lines)
    {
        foreach (var line in lines)
            WriteToChat(line);
    }

    private IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(2f);
        var canvasGroup = ChatLog.GetComponent<CanvasGroup>();
        while (!Mathf.Approximately(canvasGroup.alpha, 0))
        {
            canvasGroup.alpha -= 0.05f;
            yield return new WaitForEndOfFrame();
        }
        canvasGroup.alpha = 0;
    }
}
