using System;
using UnityEngine;
using UnityEngine.UI;

namespace MultiWorld.UI;

public class ArchipelagoConnectButtonController : MonoBehaviour
{
    public static GameObject connectPanel;
    public string assetName = "ConnectPanel";
    public string bundleName = "connectbundle";
    public GameObject chat;
    public GameObject ConnectPanel;
    public GameObject MinimizePanel;
    private string minimizeText = "-";
    private TextManager.FontOptions font = TextManager.fontDict[ChaosLang.English];
    public static bool IsOpened;

    public delegate string SlotChanged(string newValue);
    public static SlotChanged OnSlotChanged;
    public delegate string PasswordChanged(string newValue);
    public static PasswordChanged OnPasswordChanged;
    public delegate string UrlChanged(string newValue);
    public static UrlChanged OnUrlChanged;
    public delegate string PortChanged(string newValue);
    public static PortChanged OnPortChanged;
    public delegate void ConnectClicked();
    public static ConnectClicked OnConnectClick;

    public void Start()
    {
        var connectPanelAsset = AssetBundleHelper.LoadPrefab(assetName);
        connectPanel = Instantiate(connectPanelAsset);
        IsOpened = true;
        CreateInteractive();
    }

    public void Awake()
    {

    }

    private void CreateInteractive()
    {
        var inputSlotName = connectPanel.transform.Find("Canvas/PanelImage/SlotNameInput").gameObject;
        inputSlotName.GetComponent<InputField>().onValueChanged.AddListener((string value) => OnSlotChanged(value));
        var inputPassword = connectPanel.transform.Find("Canvas/PanelImage/PasswordInput/").gameObject;
        inputPassword.GetComponent<InputField>().onValueChanged.AddListener((string value) => OnPasswordChanged(value));
        var inputUrl = connectPanel.transform.Find("Canvas/PanelImage/UrlInput/").gameObject;
        inputUrl.GetComponent<InputField>().onValueChanged.AddListener((string value) => OnUrlChanged(value));
        var inputPort = connectPanel.transform.Find("Canvas/PanelImage/PortInput/").gameObject;
        inputPort.GetComponent<InputField>().onValueChanged.AddListener((string value) => OnPortChanged(value));
        var buttonConnect = connectPanel.transform.Find("Canvas/PanelImage/Connect/").gameObject;
        buttonConnect.GetComponent<Button>().onClick.AddListener(() => OnConnectClick());
    }

}