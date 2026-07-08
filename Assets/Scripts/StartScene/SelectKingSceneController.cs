using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SelectKingSceneController : MonoBehaviour {

    public GameObject selectKing;
    public GameObject selectMOD;

    public GameObject pushbuttonPrefab;
    public GameObject kingListRoot;

    public GameObject confirmBox;
    public Button btnOK;
    public Button btnCancel;

    public MapController mapCtrl;
    public KingInfoController kingInfoCtrl;

    public MenuDisplayAnim infoAnim;
    public MenuDisplayAnim mapAnim;
    public MenuDisplayAnim menuAnim;

    public Button[] modNames;

    private List<GameObject> kingNameButtons = new List<GameObject>();
    private List<GameObject> runtimeModButtons = new List<GameObject>();

    private int state = 0;
    private int kingIndex = -1;
    private bool isConfirmBoxShow = false;

    private Vector3 kingListFirstPos = new Vector3(-260, 150, 0);
    private int kingListRowsPerColumn = 11;
    private float kingListColumnWidth = 180f;
    private Vector3 modListFirstPos = new Vector3(-210, 120, 0);

	// Use this for initialization
	void Start () 
    {
        SetupMODButtons();

        if (PlayerPrefs.HasKey("GamePass"))
        {
            SetSelectMOD();
        }
        else
        {
            SetSelectKing();
        }
        confirmBox.SetActive(false);
        btnOK.SetButtonClickHandler(OnOKButton);
        btnCancel.SetButtonClickHandler(OnCancelButton);

        OnCancelButton();
	}
	
	// Update is called once per frame
	void Update () 
    {
        if (Misc.GetBack())
        {
            if (state == 0)
            {
                Misc.LoadLevel("StartScene");
                GameObject.Destroy(GameObject.Find("MouseTrack"));
            }
            else if (state == 1)
            {
                if (PlayerPrefs.HasKey("GamePass"))
                {
                    if (isConfirmBoxShow)
                    {
                        OnCancelButton();
                    }
                    else
                    {
                        SetSelectMOD();
                    }
                }
                else
                {
                    Misc.LoadLevel("StartScene");
                    GameObject.Destroy(GameObject.Find("MouseTrack"));
                }
            }
        }
	}

    /// <summary>
    /// 方法说明：绑定 MOD 选择按钮，按钮不足时把最后一个入口映射到威力加强版。
    /// 参数说明：无参数。
    /// 返回说明：无返回值。
    /// </summary>
    void SetupMODButtons()
    {
        int modCount = MODLoadController.Instance.GetMODCount();
        for (int i = 0; i < modNames.Length; i++)
        {
            int modIndex = i;
            if (i == modNames.Length - 1 && modNames.Length < modCount)
            {
                modIndex = modCount - 1;
            }

            modNames[i].SetButtonClickHandler(OnMODButtonClick);
            modNames[i].SetButtonData(modIndex);

            exSpriteFont font = modNames[i].GetComponent<exSpriteFont>();
            if (font != null && modIndex == modCount - 1)
            {
                font.text = MODLoadController.Instance.GetMODDisplayName(modIndex);
            }

            modNames[i].gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 方法说明：切换到选君主界面。
    /// 参数说明：无参数。
    /// 返回说明：无返回值。
    /// </summary>
    void SetSelectKing()
    {
        if (selectMOD.activeSelf)
        {
            selectMOD.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.OutToLeft);

            Invoke("SelectKingEnter", 0.2f);
        }
        else
        {
            SelectKingEnter();
        }
    }

    /// <summary>
    /// 方法说明：进入选君主界面并按当前 MOD 动态生成势力按钮。
    /// 参数说明：无参数。
    /// 返回说明：无返回值。
    /// </summary>
    void SelectKingEnter()
    {
        state = 1;

        selectMOD.SetActive(false);
        selectKing.SetActive(true);

        ClearKingButtons();
        MODLoadController.Instance.LoadMOD(Controller.MODSelect);
        int num = Informations.Instance.kingNum;
        for (int i = 0; i < num; i++)
        {
            GameObject go = (GameObject)Instantiate(pushbuttonPrefab);
            go.transform.parent = kingListRoot.transform;
            go.transform.localPosition = GetKingButtonPosition(i);
            go.GetComponent<PushedButton>().SetButtonDownHandler(OnKingNameSelect);
            go.GetComponent<PushedButton>().SetButtonData(i);
            go.GetComponent<exSpriteFont>().text = ZhongWen.Instance.GetKingName(i);
            kingNameButtons.Add(go);
        }

        if (kingIndex == -1)
        {
            OnKingNameSelect(0);
        }

        infoAnim.SetAnim(MenuDisplayAnim.AnimType.InsertFromBottom);
        mapAnim.SetAnim(MenuDisplayAnim.AnimType.InsertFromRight);
        menuAnim.SetAnim(MenuDisplayAnim.AnimType.InsertFromLeft);
    }

    /// <summary>
    /// 方法说明：切换到 MOD 选择界面。
    /// 参数说明：无参数。
    /// 返回说明：无返回值。
    /// </summary>
    public void SetSelectMOD()
    {
        if (selectKing.activeSelf)
        {
            infoAnim.SetAnim(MenuDisplayAnim.AnimType.OutToBottom);
            mapAnim.SetAnim(MenuDisplayAnim.AnimType.OutToRight);
            menuAnim.SetAnim(MenuDisplayAnim.AnimType.OutToLeft);

            Invoke("SelectMODEnter", 0.2f);
        }
        else
        {
            SelectMODEnter();
        }
    }

    /// <summary>
    /// 方法说明：进入 MOD 选择界面并清理已生成的势力按钮。
    /// 参数说明：无参数。
    /// 返回说明：无返回值。
    /// </summary>
    void SelectMODEnter()
    {
        state = 0;

        selectMOD.SetActive(true);
        selectKing.SetActive(false);

        ClearKingButtons();
        CreateRuntimeModButtons();

        selectMOD.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.InsertFromRight);
    }

    /// <summary>
    /// 方法说明：响应 MOD 按钮点击并加载对应 MOD。
    /// 参数说明：data 为按钮绑定的 MOD 索引。
    /// 返回说明：无返回值。
    /// </summary>
    void OnMODButtonClick(object data)
    {
        int index = (int)data;

        Informations.Reset();
        MODLoadController.Instance.LoadMOD(index);
        kingIndex = -1;
        ClearRuntimeModButtons();
        SetSelectKing();
    }

    /// <summary>
    /// 方法说明：响应势力按钮选择，并刷新地图高亮与势力信息。
    /// 参数说明：data 为势力索引。
    /// 返回说明：无返回值。
    /// </summary>
    void OnKingNameSelect(object data)
    {
        int index = (int)data;
        if (kingIndex != index)
        {
            if (kingIndex != -1)
                kingNameButtons[kingIndex].GetComponent<PushedButton>().SetButtonState(PushedButton.ButtonState.Normal);
            kingIndex = index;
            kingNameButtons[kingIndex].GetComponent<PushedButton>().SetButtonState(PushedButton.ButtonState.Pressed);
        }
        else
        {
            return;
        }

        mapCtrl.ClearSelect();
        KingInfo kInfo = Informations.Instance.GetKingInfo(kingIndex);
        if (kInfo == null) return;
        for (int i = 0; i < kInfo.cities.Count; i++)
        {
            mapCtrl.SelectCity(kInfo.cities[i]);
        }

        kingInfoCtrl.SetKing(kingIndex);

        if (!isConfirmBoxShow)
        {
            confirmBox.SetActive(true);
            isConfirmBoxShow = true;
        }
        Vector3 buttonPos = GetKingButtonPosition(kingIndex);
        confirmBox.transform.localPosition = new Vector3(confirmBox.transform.localPosition.x, buttonPos.y, confirmBox.transform.localPosition.z);
    }

    /// <summary>
    /// 方法说明：确认选中势力并进入内政场景。
    /// 参数说明：无参数。
    /// 返回说明：无返回值。
    /// </summary>
    void OnOKButton()
    {
        if (kingIndex < 0) return;

        Controller.kingIndex = kingIndex;

        StrategyController.isFirstEnter = true;
        Misc.LoadLevel("InternalAffairs");
    }

    /// <summary>
    /// 方法说明：取消当前势力确认框。
    /// 参数说明：无参数。
    /// 返回说明：无返回值。
    /// </summary>
    void OnCancelButton()
    {
        isConfirmBoxShow = false;
        confirmBox.SetActive(false);

        btnCancel.SetButtonState(Button.ButtonState.Normal);

        if (kingIndex >= 0 && kingIndex < kingNameButtons.Count)
        {
            kingNameButtons[kingIndex].GetComponent<PushedButton>().SetButtonState(PushedButton.ButtonState.Normal);
        }
        kingIndex = -1;
    }

    /// <summary>
    /// 方法说明：清理动态生成的势力按钮。
    /// 参数说明：无参数。
    /// 返回说明：无返回值。
    /// </summary>
    private void ClearKingButtons()
    {
        for (int i = 0; i < kingNameButtons.Count; i++)
        {
            Destroy(kingNameButtons[i]);
        }
        kingNameButtons.Clear();
    }

    /// <summary>
    /// 方法说明：创建副本选择按钮，避免旧场景按钮被背景遮挡或缺失时无法进入威力加强版。
    /// 参数说明：无参数。
    /// 返回说明：无返回值。
    /// </summary>
    private void CreateRuntimeModButtons()
    {
        ClearRuntimeModButtons();

        int modCount = MODLoadController.Instance.GetMODCount();
        for (int i = 0; i < modCount; i++)
        {
            GameObject go = (GameObject)Instantiate(pushbuttonPrefab);
            go.transform.parent = selectMOD.transform;
            go.transform.localPosition = GetModButtonPosition(i);
            go.transform.localScale = Vector3.one;
            go.transform.localRotation = Quaternion.identity;
            go.GetComponent<PushedButton>().SetButtonDownHandler(OnMODButtonClick);
            go.GetComponent<PushedButton>().SetButtonData(i);
            go.GetComponent<exSpriteFont>().text = MODLoadController.Instance.GetMODDisplayName(i);
            runtimeModButtons.Add(go);
        }
    }

    /// <summary>
    /// 方法说明：清理运行时创建的副本选择按钮。
    /// 参数说明：无参数。
    /// 返回说明：无返回值。
    /// </summary>
    private void ClearRuntimeModButtons()
    {
        for (int i = 0; i < runtimeModButtons.Count; i++)
        {
            Destroy(runtimeModButtons[i]);
        }

        runtimeModButtons.Clear();
    }

    /// <summary>
    /// 方法说明：计算副本选择按钮位置。
    /// 参数说明：index 为副本索引。
    /// 返回说明：返回按钮本地坐标。
    /// </summary>
    private Vector3 GetModButtonPosition(int index)
    {
        return new Vector3(modListFirstPos.x, modListFirstPos.y - index * 42, 0);
    }

    /// <summary>
    /// 方法说明：计算势力按钮位置，超过一列时自动换列。
    /// 参数说明：index 为势力索引。
    /// 返回说明：返回按钮本地坐标。
    /// </summary>
    private Vector3 GetKingButtonPosition(int index)
    {
        int column = index / kingListRowsPerColumn;
        int row = index % kingListRowsPerColumn;
        return new Vector3(kingListFirstPos.x + column * kingListColumnWidth, kingListFirstPos.y - row * 30, 0);
    }
}
