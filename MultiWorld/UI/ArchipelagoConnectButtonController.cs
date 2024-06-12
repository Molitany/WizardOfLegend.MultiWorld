using MultiWorld.ArchipelagoClient;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace MultiWorld.UI;

public class ArchipelagoConnectButtonController : MonoBehaviour
{
    public static GameObject ConnectPanel;
    public string AssetName = "ConnectPanel";
    public string BundleName = "connectbundle";
    private readonly Font font = TextManager.fontDict[ChaosLang.English].font;
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
        var connectPanelAsset = AssetBundleHelper.LoadPrefab(AssetName);
        ConnectPanel = Instantiate(connectPanelAsset);
        IsOpened = true;
        CreateInteractive();
        var texts = GameObject.Find("Canvas/PanelImage").GetComponentsInChildren<Text>();
        foreach (var text in texts)
        {
            text.font = font;
        }
    }

    private void CreateInteractive()
    {
        var inputSlotName = ConnectPanel.transform.Find("Canvas/PanelImage/SlotNameInput").gameObject;
        inputSlotName.GetComponent<InputField>().onValueChanged.AddListener(value => OnSlotChanged(value));
        inputSlotName.GetComponent<InputField>().text = ArchipelagoManager.SlotName;
        var inputPassword = ConnectPanel.transform.Find("Canvas/PanelImage/PasswordInput/").gameObject;
        inputPassword.GetComponent<InputField>().onValueChanged.AddListener(value => OnPasswordChanged(value));
        inputPassword.GetComponent<InputField>().text = ArchipelagoManager.Password;
        var inputUrl = ConnectPanel.transform.Find("Canvas/PanelImage/UrlInput/").gameObject;
        inputUrl.GetComponent<InputField>().onValueChanged.AddListener(value => OnUrlChanged(value));
        inputUrl.GetComponent<InputField>().text = ArchipelagoManager.Url;
        var inputPort = ConnectPanel.transform.Find("Canvas/PanelImage/PortInput/").gameObject;
        inputPort.GetComponent<InputField>().onValueChanged.AddListener(value => OnPortChanged(value));
        inputPort.GetComponent<InputField>().text = ArchipelagoManager.Port;
        var buttonConnect = ConnectPanel.transform.Find("Canvas/PanelImage/Connect/").gameObject;
        buttonConnect.GetComponent<Button>().onClick.AddListener(() => OnConnectClick());
    }

}