using UnityEngine;
using System;
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
    private Dictionary<int, GameObject> visibleKingButtons = new Dictionary<int, GameObject>();
    private Dictionary<int, TextMesh> visibleKingLabels = new Dictionary<int, TextMesh>();
    private List<GameObject> runtimeModButtons = new List<GameObject>();
    private GameObject kingScrollTrack;
    private GameObject kingScrollThumb;
    private Dictionary<int, Material> kingScrollMaterials = new Dictionary<int, Material>();

    private int state = 0;
    private int kingIndex = -1;
    private int kingListScrollIndex = 0;
    private bool isConfirmBoxShow = false;
    private bool kingListDragActive = false;
    private float kingListDragStartY = 0f;

    private Vector3 kingListFirstPos = new Vector3(-240, 140, -4);
    private int kingListRowsPerColumn = 9;
    private int kingListColumnCount = 1;
    private float kingListRowHeight = 26f;
    private float kingListColumnWidth = 0f;
    private Vector3 kingScrollTrackPos = new Vector3(-150, 20, -4);
    private float kingScrollTrackWidth = 14f;
    private float kingScrollTrackHeight = 250f;
    private float kingScrollThumbMinHeight = 48f;
    private Vector3 modListFirstPos = new Vector3(0, 35, 0);
    private float modListRowHeight = 25f;
    private float kingListDragThreshold = 35f;
    private int kingListScrollStep = 3;
    private Font kingListFont = null;
    private int kingListFontSize = 64;
    private float kingListCharacterSize = 3.4f;
    private float selectBackgroundBaseWidth = 640f;
    private float selectBackgroundBaseHeight = 480f;

    /// <summary>
    /// 方法说明：初始化选择界面按钮事件，并按存档状态进入副本或君主选择。
    /// 参数说明：无参数。
    /// 返回说明：无返回值。
    /// </summary>
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
	
    /// <summary>
    /// 方法说明：处理返回键和选择君主列表的手游拖动翻页。
    /// 参数说明：无参数。
    /// 返回说明：无返回值。
    /// </summary>
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

        HandleKingListSwipe();
	}

    /// <summary>
    /// 方法说明：绑定场景内已有的 MOD 选择按钮，运行时只补齐新增副本入口。
    /// 参数说明：无参数。
    /// 返回说明：无返回值。
    /// </summary>
    void SetupMODButtons()
    {
        if (modNames == null || modNames.Length == 0)
        {
            Debug.LogError("副本选择按钮未绑定，无法显示选择时期列表。");
            return;
        }

        int modCount = MODLoadController.Instance.GetMODCount();
        for (int i = 0; i < modNames.Length; i++)
        {
            if (i >= modCount)
            {
                modNames[i].gameObject.SetActive(false);
                continue;
            }

            int modIndex = i;

            modNames[i].SetButtonClickHandler(OnMODButtonClick);
            modNames[i].SetButtonData(modIndex);
            modNames[i].transform.localPosition = GetModButtonPosition(i);

            exSpriteFont font = modNames[i].GetComponent<exSpriteFont>();
            if (font != null)
            {
                font.text = MODLoadController.Instance.GetMODDisplayName(modIndex);
            }

            modNames[i].gameObject.SetActive(true);
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
        BringKingListMenuToFront();

        ClearKingButtons();
        MODLoadController.Instance.LoadMOD(Controller.MODSelect);
        mapCtrl.SetMapDraggingEnabled(true);
        mapCtrl.ResetMapPan();
        kingListScrollIndex = 0;
        CreateKingScrollButtons();
        RebuildKingButtons();

        if (kingIndex == -1)
        {
            OnKingNameSelect(0);
        }

        infoAnim.SetAnim(MenuDisplayAnim.AnimType.InsertFromBottom);
        mapAnim.SetAnim(MenuDisplayAnim.AnimType.InsertFromRight);
        menuAnim.SetAnim(MenuDisplayAnim.AnimType.InsertFromLeft);
        Invoke("FocusSelectedKingMap", 0.25f);
    }

    /// <summary>
    /// 方法说明：把左侧选择君主菜单放到地图和城池标记前面。
    /// 参数说明：无参数。
    /// 返回说明：无返回值。
    /// </summary>
    private void BringKingListMenuToFront()
    {
        SetLocalZ(menuAnim == null ? null : menuAnim.transform, -6f);
        SetLocalZ(kingListRoot == null ? null : kingListRoot.transform, -6f);
    }

    /// <summary>
    /// 方法说明：设置对象本地深度。
    /// 参数说明：target 为目标对象，z 为本地 z 值。
    /// 返回说明：无返回值。
    /// </summary>
    private void SetLocalZ(Transform target, float z)
    {
        if (target == null) return;

        Vector3 position = target.localPosition;
        target.localPosition = new Vector3(position.x, position.y, z);
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
        FitSelectBackgroundToCamera();
        mapCtrl.SetMapDraggingEnabled(false);

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
        if (index < 0 || index >= Informations.Instance.kingNum) return;

        if (kingIndex != index)
        {
            SetVisibleKingButtonState(kingIndex, PushedButton.ButtonState.Normal);
            kingIndex = index;
            SetVisibleKingButtonState(kingIndex, PushedButton.ButtonState.Pressed);
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
        mapCtrl.FocusOnCities(kInfo.cities);

        kingInfoCtrl.SetKing(kingIndex);

        if (!isConfirmBoxShow)
        {
            confirmBox.SetActive(true);
            isConfirmBoxShow = true;
        }
        confirmBox.transform.localPosition = new Vector3(confirmBox.transform.localPosition.x, GetConfirmBoxY(kingIndex), confirmBox.transform.localPosition.z);
    }

    /// <summary>
    /// 方法说明：在选君主入场动画结束后重新聚焦当前势力城池。
    /// 参数说明：无参数。
    /// 返回说明：无返回值。
    /// </summary>
    private void FocusSelectedKingMap()
    {
        if (state != 1 || kingIndex < 0) return;

        KingInfo kInfo = Informations.Instance.GetKingInfo(kingIndex);
        if (kInfo == null) return;

        mapCtrl.FocusOnCities(kInfo.cities);
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

        SetVisibleKingButtonState(kingIndex, PushedButton.ButtonState.Normal);
        kingIndex = -1;
    }

    /// <summary>
    /// 方法说明：清理动态生成的势力按钮和翻页控件。
    /// 参数说明：无参数。
    /// 返回说明：无返回值。
    /// </summary>
    private void ClearKingButtons()
    {
        ClearVisibleKingButtons();
        DestroyKingScrollButtons();
        kingListDragActive = false;
    }

    /// <summary>
    /// 方法说明：清理当前可见的势力按钮。
    /// 参数说明：无参数。
    /// 返回说明：无返回值。
    /// </summary>
    private void ClearVisibleKingButtons()
    {
        for (int i = 0; i < kingNameButtons.Count; i++)
        {
            Destroy(kingNameButtons[i]);
        }
        kingNameButtons.Clear();
        visibleKingButtons.Clear();
        foreach (TextMesh label in visibleKingLabels.Values)
        {
            if (label != null)
            {
                Destroy(label.gameObject);
            }
        }
        visibleKingLabels.Clear();
    }

    /// <summary>
    /// 方法说明：按当前滚动位置重建可见势力按钮。
    /// 参数说明：无参数。
    /// 返回说明：无返回值。
    /// </summary>
    private void RebuildKingButtons()
    {
        // 1. 重新约束滚动索引，避免 MOD 切换后越界。
        ClampKingListScrollIndex();
        ClearVisibleKingButtons();

        // 2. 只创建当前页可见势力，手游界面通过侧边滚动条和滑动翻页。
        int pageSize = GetKingListPageSize();
        int num = Informations.Instance.kingNum;
        int endIndex = Mathf.Min(num, kingListScrollIndex + pageSize);
        for (int i = kingListScrollIndex; i < endIndex; i++)
        {
            GameObject go = (GameObject)Instantiate(pushbuttonPrefab);
            go.transform.parent = kingListRoot.transform;
            go.transform.localPosition = GetKingButtonPosition(i - kingListScrollIndex);
            go.transform.localScale = Vector3.one;
            go.transform.localRotation = Quaternion.identity;
            go.GetComponent<PushedButton>().SetButtonDownHandler(OnKingNameSelect);
            go.GetComponent<PushedButton>().SetButtonData(i);
            go.GetComponent<exSpriteFont>().text = ZhongWen.Instance.GetKingName(i);
            HideKingButtonFont(go);
            kingNameButtons.Add(go);
            visibleKingButtons[i] = go;
            visibleKingLabels[i] = CreateKingNameLabel(ZhongWen.Instance.GetKingName(i), go.transform.localPosition);
        }

        // 3. 当前选中势力如果在可见范围内，恢复按钮高亮。
        if (kingIndex >= kingListScrollIndex && kingIndex < endIndex)
        {
            SetVisibleKingButtonState(kingIndex, PushedButton.ButtonState.Pressed);
        }

        // 4. 刷新侧边滚动条位置，提示当前列表进度。
        RefreshKingScrollButtons();
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
        int startIndex = modNames == null ? 0 : Mathf.Min(modNames.Length, modCount);
        Transform modButtonParent = GetRuntimeModButtonParent();
        for (int i = startIndex; i < modCount; i++)
        {
            GameObject go = CreateRuntimeModButtonObject();
            go.transform.parent = modButtonParent;
            go.name = "RuntimeMOD" + i;
            go.transform.localPosition = GetModButtonPosition(i);
            go.transform.localScale = Vector3.one;
            go.transform.localRotation = Quaternion.identity;
            BindRuntimeModButton(go, i);
            go.GetComponent<exSpriteFont>().text = MODLoadController.Instance.GetMODDisplayName(i);
            runtimeModButtons.Add(go);
        }
    }

    /// <summary>
    /// 方法说明：创建运行时补充剧本按钮对象。
    /// 参数说明：无参数。
    /// 返回说明：返回和场景内已有剧本按钮同模板的按钮对象。
    /// </summary>
    private GameObject CreateRuntimeModButtonObject()
    {
        if (modNames != null && modNames.Length > 0 && modNames[0] != null)
        {
            return (GameObject)Instantiate(modNames[0].gameObject);
        }

        return (GameObject)Instantiate(pushbuttonPrefab);
    }

    /// <summary>
    /// 方法说明：绑定运行时补充剧本按钮点击事件和数据。
    /// 参数说明：buttonObject 为按钮对象，index 为剧本索引。
    /// 返回说明：无返回值。
    /// </summary>
    private void BindRuntimeModButton(GameObject buttonObject, int index)
    {
        Button button = buttonObject.GetComponent<Button>();
        if (button != null)
        {
            button.SetButtonClickHandler(OnMODButtonClick);
            button.SetButtonData(index);
            return;
        }

        PushedButton pushedButton = buttonObject.GetComponent<PushedButton>();
        if (pushedButton != null)
        {
            pushedButton.SetButtonDownHandler(OnMODButtonClick);
            pushedButton.SetButtonData(index);
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
    /// 方法说明：读取运行时补充剧本按钮应挂载的父节点。
    /// 参数说明：无参数。
    /// 返回说明：返回与场景内已有剧本按钮一致的父节点。
    /// </summary>
    private Transform GetRuntimeModButtonParent()
    {
        if (modNames != null && modNames.Length > 0 && modNames[0] != null)
        {
            return modNames[0].transform.parent;
        }

        return selectMOD.transform;
    }

    /// <summary>
    /// 方法说明：把剧本选择页背景按当前相机视野等比铺满，避免宽屏两侧露底。
    /// 参数说明：无参数。
    /// 返回说明：无返回值。
    /// </summary>
    private void FitSelectBackgroundToCamera()
    {
        // 1. 找到选择场景根背景和主摄像机，缺失时保持原状。
        GameObject background = GameObject.Find("Background");
        Camera camera = Camera.main;
        if (background == null || camera == null || !camera.orthographic)
        {
            return;
        }

        // 2. 计算当前相机可视宽高。
        float viewHeight = camera.orthographicSize * 2f;
        float viewWidth = viewHeight * camera.aspect;

        // 3. 等比放大背景，保证宽屏下左右不露出相机底色。
        float scale = Mathf.Max(viewWidth / selectBackgroundBaseWidth, viewHeight / selectBackgroundBaseHeight);
        background.transform.localScale = new Vector3(scale, scale, background.transform.localScale.z);
    }

    /// <summary>
    /// 方法说明：计算副本选择按钮位置。
    /// 参数说明：index 为副本索引。
    /// 返回说明：返回按钮本地坐标。
    /// </summary>
    private Vector3 GetModButtonPosition(int index)
    {
        return new Vector3(modListFirstPos.x, modListFirstPos.y - index * modListRowHeight, 0);
    }

    /// <summary>
    /// 方法说明：计算可见势力按钮位置。
    /// 参数说明：index 为当前页内的可见行索引。
    /// 返回说明：返回按钮本地坐标。
    /// </summary>
    private Vector3 GetKingButtonPosition(int index)
    {
        int column = index / kingListRowsPerColumn;
        int row = index % kingListRowsPerColumn;
        return new Vector3(kingListFirstPos.x + column * kingListColumnWidth, kingListFirstPos.y - row * kingListRowHeight, 0);
    }

    /// <summary>
    /// 方法说明：创建势力列表侧边滚动条。
    /// 参数说明：无参数。
    /// 返回说明：无返回值。
    /// </summary>
    private void CreateKingScrollButtons()
    {
        DestroyKingScrollButtons();
        if (!NeedKingListPaging())
        {
            return;
        }

        float thumbHeight = GetKingScrollThumbHeight();
        kingScrollTrack = CreateKingScrollTrackIcon();
        kingScrollThumb = CreateKingScrollThumbIcon(thumbHeight);
        RefreshKingScrollButtons();
    }

    /// <summary>
    /// 方法说明：创建符合原 UI 画风的势力列表侧边滑轨。
    /// 参数说明：无参数。
    /// 返回说明：返回滑轨根对象。
    /// </summary>
    private GameObject CreateKingScrollTrackIcon()
    {
        GameObject root = CreateKingScrollRoot("KingScrollTrack", kingScrollTrackPos);
        Color dark = new Color(0.10f, 0.04f, 0.04f, 0.90f);
        Color gold = new Color(0.75f, 0.52f, 0.13f, 1f);
        Color highlight = new Color(1f, 0.82f, 0.30f, 1f);

        CreateKingScrollQuad(root.transform, "TrackSlot", Vector3.zero, new Vector2(kingScrollTrackWidth, kingScrollTrackHeight), dark);
        CreateKingScrollQuad(root.transform, "TrackLeftGold", new Vector3(-kingScrollTrackWidth * 0.5f + 1.3f, 0f, -0.05f), new Vector2(2.4f, kingScrollTrackHeight), gold);
        CreateKingScrollQuad(root.transform, "TrackRightGold", new Vector3(kingScrollTrackWidth * 0.5f - 1.3f, 0f, -0.05f), new Vector2(2.4f, kingScrollTrackHeight), gold);
        CreateKingScrollQuad(root.transform, "TrackCenterShade", Vector3.zero, new Vector2(kingScrollTrackWidth - 6f, kingScrollTrackHeight - 8f), new Color(0.04f, 0.02f, 0.03f, 0.75f));
        CreateKingScrollQuad(root.transform, "TrackTopGlint", new Vector3(0f, kingScrollTrackHeight * 0.5f - 2f, -0.08f), new Vector2(kingScrollTrackWidth, 2f), highlight);
        CreateKingScrollQuad(root.transform, "TrackBottomShadow", new Vector3(0f, -kingScrollTrackHeight * 0.5f + 2f, -0.08f), new Vector2(kingScrollTrackWidth, 2f), new Color(0.18f, 0.08f, 0.03f, 1f));
        CreateKingScrollArrowIcon(root.transform, new Vector3(0f, kingScrollTrackHeight * 0.5f - 13f, -0.12f), true);
        CreateKingScrollArrowIcon(root.transform, new Vector3(0f, -kingScrollTrackHeight * 0.5f + 13f, -0.12f), false);
        return root;
    }

    /// <summary>
    /// 方法说明：创建符合原 UI 画风的势力列表滑块。
    /// 参数说明：height 为滑块高度。
    /// 返回说明：返回滑块根对象。
    /// </summary>
    private GameObject CreateKingScrollThumbIcon(float height)
    {
        GameObject root = CreateKingScrollRoot("KingScrollThumb", new Vector3(kingScrollTrackPos.x, GetKingScrollThumbY(height), kingScrollTrackPos.z - 0.25f));
        Color gold = new Color(0.75f, 0.52f, 0.13f, 1f);
        Color brightGold = new Color(1f, 0.78f, 0.20f, 1f);
        Color dark = new Color(0.16f, 0.06f, 0.04f, 0.96f);
        Color red = new Color(0.75f, 0.03f, 0.02f, 1f);

        CreateKingScrollQuad(root.transform, "ThumbShadow", new Vector3(1.5f, -1.5f, 0.08f), new Vector2(kingScrollTrackWidth + 5f, height + 2f), new Color(0f, 0f, 0f, 0.45f));
        CreateKingScrollQuad(root.transform, "ThumbOuter", Vector3.zero, new Vector2(kingScrollTrackWidth + 4f, height), gold);
        CreateKingScrollQuad(root.transform, "ThumbInner", new Vector3(0f, 0f, -0.04f), new Vector2(kingScrollTrackWidth - 1f, Mathf.Max(10f, height - 8f)), dark);
        CreateKingScrollQuad(root.transform, "ThumbTop", new Vector3(0f, height * 0.5f - 3f, -0.08f), new Vector2(kingScrollTrackWidth + 2f, 4f), brightGold);
        CreateKingScrollQuad(root.transform, "ThumbBottom", new Vector3(0f, -height * 0.5f + 3f, -0.08f), new Vector2(kingScrollTrackWidth + 2f, 4f), new Color(0.38f, 0.20f, 0.04f, 1f));
        CreateKingScrollDiamond(root.transform, "ThumbGem", new Vector3(0f, 0f, -0.12f), Mathf.Min(11f, height * 0.28f), red);
        return root;
    }

    /// <summary>
    /// 方法说明：创建滚动条根对象。
    /// 参数说明：name 为对象名称，position 为本地坐标。
    /// 返回说明：返回创建出的根对象。
    /// </summary>
    private GameObject CreateKingScrollRoot(string name, Vector3 position)
    {
        GameObject root = new GameObject(name);
        root.transform.parent = kingListRoot.transform;
        root.transform.localPosition = position;
        root.transform.localScale = Vector3.one;
        root.transform.localRotation = Quaternion.identity;
        root.layer = kingListRoot.layer;
        return root;
    }

    /// <summary>
    /// 方法说明：创建滚动条上下箭头图标。
    /// 参数说明：parent 为父节点，position 为本地坐标，isUp 为 true 时创建向上箭头，否则创建向下箭头。
    /// 返回说明：无返回值。
    /// </summary>
    private void CreateKingScrollArrowIcon(Transform parent, Vector3 position, bool isUp)
    {
        Color gold = new Color(0.70f, 0.48f, 0.12f, 1f);
        Color brightGold = new Color(0.95f, 0.72f, 0.22f, 1f);
        Color red = new Color(0.78f, 0.05f, 0.02f, 1f);

        GameObject root = new GameObject(isUp ? "ScrollUpIcon" : "ScrollDownIcon");
        root.transform.parent = parent;
        root.transform.localPosition = position;
        root.transform.localScale = Vector3.one;
        root.transform.localRotation = Quaternion.identity;
        root.layer = kingListRoot.layer;

        CreateKingScrollQuad(root.transform, "ArrowFrame", Vector3.zero, new Vector2(18f, 18f), gold);
        CreateKingScrollQuad(root.transform, "ArrowFace", new Vector3(0f, 0f, -0.04f), new Vector2(14f, 14f), brightGold);
        CreateKingScrollTriangle(root.transform, "ArrowShape", new Vector3(0f, isUp ? 1f : -1f, -0.1f), 9f, 8f, isUp, red);
    }

    /// <summary>
    /// 方法说明：创建矩形网格图形。
    /// 参数说明：parent 为父节点，name 为对象名，position 为本地坐标，size 为宽高，color 为颜色。
    /// 返回说明：返回创建出的矩形对象。
    /// </summary>
    private GameObject CreateKingScrollQuad(Transform parent, string name, Vector3 position, Vector2 size, Color color)
    {
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-size.x * 0.5f, -size.y * 0.5f, 0f),
            new Vector3(size.x * 0.5f, -size.y * 0.5f, 0f),
            new Vector3(size.x * 0.5f, size.y * 0.5f, 0f),
            new Vector3(-size.x * 0.5f, size.y * 0.5f, 0f)
        };
        return CreateKingScrollMesh(parent, name, position, vertices, new int[] { 0, 2, 1, 0, 3, 2 }, color);
    }

    /// <summary>
    /// 方法说明：创建三角箭头网格图形。
    /// 参数说明：parent 为父节点，name 为对象名，position 为本地坐标，width 为宽度，height 为高度，isUp 为方向，color 为颜色。
    /// 返回说明：返回创建出的三角对象。
    /// </summary>
    private GameObject CreateKingScrollTriangle(Transform parent, string name, Vector3 position, float width, float height, bool isUp, Color color)
    {
        float top = height * 0.5f;
        float bottom = -height * 0.5f;
        Vector3 tip = isUp ? new Vector3(0f, top, 0f) : new Vector3(0f, bottom, 0f);
        Vector3 left = isUp ? new Vector3(-width * 0.5f, bottom, 0f) : new Vector3(-width * 0.5f, top, 0f);
        Vector3 right = isUp ? new Vector3(width * 0.5f, bottom, 0f) : new Vector3(width * 0.5f, top, 0f);
        return CreateKingScrollMesh(parent, name, position, new Vector3[] { tip, left, right }, new int[] { 0, 1, 2 }, color);
    }

    /// <summary>
    /// 方法说明：创建滑块中心菱形图形。
    /// 参数说明：parent 为父节点，name 为对象名，position 为本地坐标，size 为半径尺寸，color 为颜色。
    /// 返回说明：返回创建出的菱形对象。
    /// </summary>
    private GameObject CreateKingScrollDiamond(Transform parent, string name, Vector3 position, float size, Color color)
    {
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(0f, size, 0f),
            new Vector3(size * 0.75f, 0f, 0f),
            new Vector3(0f, -size, 0f),
            new Vector3(-size * 0.75f, 0f, 0f)
        };
        return CreateKingScrollMesh(parent, name, position, vertices, new int[] { 0, 1, 2, 0, 2, 3 }, color);
    }

    /// <summary>
    /// 方法说明：创建滚动条图形网格。
    /// 参数说明：parent 为父节点，name 为对象名，position 为本地坐标，vertices 为顶点数组，triangles 为三角索引，color 为颜色。
    /// 返回说明：返回创建出的网格对象。
    /// </summary>
    private GameObject CreateKingScrollMesh(Transform parent, string name, Vector3 position, Vector3[] vertices, int[] triangles, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.parent = parent;
        go.transform.localPosition = position;
        go.transform.localScale = Vector3.one;
        go.transform.localRotation = Quaternion.identity;
        go.layer = kingListRoot.layer;

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        MeshFilter meshFilter = go.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;
        MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = GetKingScrollMaterial(color);
        return go;
    }

    /// <summary>
    /// 方法说明：读取或创建滚动条纯色材质。
    /// 参数说明：color 为材质颜色。
    /// 返回说明：返回可用于滚动条图形的材质。
    /// </summary>
    private Material GetKingScrollMaterial(Color color)
    {
        int key = ColorToMaterialKey(color);
        Material material;
        if (kingScrollMaterials.TryGetValue(key, out material))
        {
            return material;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        material = new Material(shader);
        material.hideFlags = HideFlags.HideAndDontSave;
        material.color = color;
        kingScrollMaterials[key] = material;
        return material;
    }

    /// <summary>
    /// 方法说明：把颜色转换为材质缓存键。
    /// 参数说明：color 为目标颜色。
    /// 返回说明：返回整数缓存键。
    /// </summary>
    private int ColorToMaterialKey(Color color)
    {
        int r = Mathf.RoundToInt(color.r * 255f);
        int g = Mathf.RoundToInt(color.g * 255f);
        int b = Mathf.RoundToInt(color.b * 255f);
        int a = Mathf.RoundToInt(color.a * 255f);
        return (r << 24) ^ (g << 16) ^ (b << 8) ^ a;
    }

    /// <summary>
    /// 方法说明：销毁势力列表滚动条对象。
    /// 参数说明：无参数。
    /// 返回说明：无返回值。
    /// </summary>
    private void DestroyKingScrollButtons()
    {
        if (kingScrollTrack != null)
        {
            Destroy(kingScrollTrack);
            kingScrollTrack = null;
        }

        if (kingScrollThumb != null)
        {
            Destroy(kingScrollThumb);
            kingScrollThumb = null;
        }

    }

    /// <summary>
    /// 方法说明：滚动势力列表并重建当前页。
    /// 参数说明：offset 为滚动偏移，正数下翻，负数上翻。
    /// 返回说明：无返回值。
    /// </summary>
    private void ScrollKingList(int offset)
    {
        int oldIndex = kingListScrollIndex;
        kingListScrollIndex += offset;
        ClampKingListScrollIndex();
        if (oldIndex == kingListScrollIndex) return;

        RebuildKingButtons();
        if (kingIndex >= 0)
        {
            confirmBox.transform.localPosition = new Vector3(confirmBox.transform.localPosition.x, GetConfirmBoxY(kingIndex), confirmBox.transform.localPosition.z);
        }
    }

    /// <summary>
    /// 方法说明：约束势力列表滚动索引。
    /// 参数说明：无参数。
    /// 返回说明：无返回值。
    /// </summary>
    private void ClampKingListScrollIndex()
    {
        kingListScrollIndex = Mathf.Clamp(kingListScrollIndex, 0, GetKingListMaxScrollIndex());
    }

    /// <summary>
    /// 方法说明：刷新势力列表侧边滚动条位置。
    /// 参数说明：无参数。
    /// 返回说明：无返回值。
    /// </summary>
    private void RefreshKingScrollButtons()
    {
        if (kingScrollThumb == null) return;

        float thumbHeight = GetKingScrollThumbHeight();
        kingScrollThumb.transform.localPosition = new Vector3(kingScrollTrackPos.x, GetKingScrollThumbY(thumbHeight), kingScrollTrackPos.z - 0.2f);
    }

    /// <summary>
    /// 方法说明：计算滚动条滑块高度。
    /// 参数说明：无参数。
    /// 返回说明：返回滑块高度。
    /// </summary>
    private float GetKingScrollThumbHeight()
    {
        if (Informations.Instance.kingNum <= 0) return kingScrollTrackHeight;

        float pageRatio = Mathf.Clamp01(GetKingListPageSize() / (float)Informations.Instance.kingNum);
        return Mathf.Max(kingScrollThumbMinHeight, kingScrollTrackHeight * pageRatio);
    }

    /// <summary>
    /// 方法说明：计算滚动条滑块纵坐标。
    /// 参数说明：thumbHeight 为滑块高度。
    /// 返回说明：返回滑块本地纵坐标。
    /// </summary>
    private float GetKingScrollThumbY(float thumbHeight)
    {
        int maxScrollIndex = GetKingListMaxScrollIndex();
        if (maxScrollIndex <= 0) return kingScrollTrackPos.y;

        float top = kingScrollTrackPos.y + (kingScrollTrackHeight - thumbHeight) * 0.5f;
        float bottom = kingScrollTrackPos.y - (kingScrollTrackHeight - thumbHeight) * 0.5f;
        float t = kingListScrollIndex / (float)maxScrollIndex;
        return Mathf.Lerp(top, bottom, t);
    }

    /// <summary>
    /// 方法说明：设置当前可见势力按钮状态。
    /// 参数说明：index 为势力索引，buttonState 为目标按钮状态。
    /// 返回说明：无返回值。
    /// </summary>
    private void SetVisibleKingButtonState(int index, PushedButton.ButtonState buttonState)
    {
        GameObject button = GetVisibleKingButton(index);
        if (button != null)
        {
            button.GetComponent<PushedButton>().SetButtonState(buttonState);
            HideKingButtonFont(button);
        }

        TextMesh label = GetVisibleKingLabel(index);
        if (label != null)
        {
            label.color = buttonState == PushedButton.ButtonState.Normal ? Color.white : new Color(0f, 1f, 0f, 1f);
        }
    }

    /// <summary>
    /// 方法说明：读取当前可见势力按钮。
    /// 参数说明：index 为势力索引。
    /// 返回说明：找到返回按钮对象，否则返回 null。
    /// </summary>
    private GameObject GetVisibleKingButton(int index)
    {
        GameObject button;
        return visibleKingButtons.TryGetValue(index, out button) ? button : null;
    }

    /// <summary>
    /// 方法说明：读取当前可见势力名称标签。
    /// 参数说明：index 为势力索引。
    /// 返回说明：找到返回标签，否则返回 null。
    /// </summary>
    private TextMesh GetVisibleKingLabel(int index)
    {
        TextMesh label;
        return visibleKingLabels.TryGetValue(index, out label) ? label : null;
    }

    /// <summary>
    /// 方法说明：创建动态字体君主名称标签。
    /// 参数说明：text 为君主名称，position 为本地坐标。
    /// 返回说明：返回创建出的 TextMesh。
    /// </summary>
    private TextMesh CreateKingNameLabel(string text, Vector3 position)
    {
        GameObject go = new GameObject("KingNameLabel");
        go.transform.parent = kingListRoot.transform;
        go.transform.localPosition = new Vector3(position.x, position.y, position.z - 0.2f);
        go.transform.localScale = Vector3.one;
        go.transform.localRotation = Quaternion.identity;
        go.layer = kingListRoot.layer;

        TextMesh textMesh = go.AddComponent<TextMesh>();
        textMesh.font = GetKingListFont();
        textMesh.GetComponent<Renderer>().sharedMaterial = textMesh.font.material;
        textMesh.text = text;
        textMesh.fontSize = kingListFontSize;
        textMesh.characterSize = kingListCharacterSize;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = Color.white;
        return textMesh;
    }

    /// <summary>
    /// 方法说明：读取君主列表动态字体。
    /// 参数说明：无参数。
    /// 返回说明：返回动态字体对象。
    /// </summary>
    private Font GetKingListFont()
    {
        if (kingListFont == null)
        {
			kingListFont = UnifiedGameFontController.CreateChineseDynamicFont(kingListFontSize);
        }

        return kingListFont;
    }

    /// <summary>
    /// 方法说明：隐藏原 bitmap 字体，仅保留按钮点击热区。
    /// 参数说明：button 为按钮对象。
    /// 返回说明：无返回值。
    /// </summary>
    private void HideKingButtonFont(GameObject button)
    {
        exSpriteFont font = button.GetComponent<exSpriteFont>();
        if (font == null) return;

        MarkFontAsHandledByManualLabel(font);
        Color transparent = new Color(1f, 1f, 1f, 0f);
        font.topColor = transparent;
        font.botColor = transparent;
    }

    /// <summary>
    /// 方法说明：标记旧字体已经由当前脚本手工创建动态字体，避免全局字体镜像重复覆盖。
    /// 参数说明：font 为需要跳过全局镜像的旧字体组件。
    /// 返回说明：无返回值。
    /// </summary>
    private void MarkFontAsHandledByManualLabel(exSpriteFont font)
    {
        if (font == null || font.GetComponent<UnifiedGameFontIgnore>() != null) return;

        font.gameObject.AddComponent<UnifiedGameFontIgnore>();
    }

    /// <summary>
    /// 方法说明：计算确认框所在纵坐标。
    /// 参数说明：index 为势力索引。
    /// 返回说明：返回确认框本地纵坐标。
    /// </summary>
    private float GetConfirmBoxY(int index)
    {
        if (index >= kingListScrollIndex && index < kingListScrollIndex + GetKingListPageSize())
        {
            return GetKingButtonPosition(index - kingListScrollIndex).y;
        }

        return 30f;
    }

    /// <summary>
    /// 方法说明：处理手游拖动势力列表翻页。
    /// 参数说明：无参数。
    /// 返回说明：无返回值。
    /// </summary>
    private void HandleKingListSwipe()
    {
        if (state != 1) return;

        if (Input.GetMouseButtonDown(0) && IsPointerInKingListArea())
        {
            kingListDragActive = true;
            kingListDragStartY = Input.mousePosition.y;
        }
        else if (kingListDragActive && Input.GetMouseButtonUp(0))
        {
            float deltaY = Input.mousePosition.y - kingListDragStartY;
            kingListDragActive = false;
            if (Mathf.Abs(deltaY) < kingListDragThreshold) return;

            ScrollKingList(deltaY > 0f ? GetKingListScrollStep() : -GetKingListScrollStep());
        }
    }

    /// <summary>
    /// 方法说明：判断当前触点是否在左侧势力列表区域。
    /// 参数说明：无参数。
    /// 返回说明：在列表区域返回 true，否则返回 false。
    /// </summary>
    private bool IsPointerInKingListArea()
    {
        return Input.mousePosition.x <= Screen.width * 0.32f;
    }

    /// <summary>
    /// 方法说明：计算势力列表单页容量。
    /// 参数说明：无参数。
    /// 返回说明：返回单页可显示势力数量。
    /// </summary>
    private int GetKingListPageSize()
    {
        return kingListRowsPerColumn * kingListColumnCount;
    }

    /// <summary>
    /// 方法说明：计算势力列表最大滚动索引。
    /// 参数说明：无参数。
    /// 返回说明：返回最大滚动起点。
    /// </summary>
    private int GetKingListMaxScrollIndex()
    {
        return Mathf.Max(0, Informations.Instance.kingNum - GetKingListPageSize());
    }

    /// <summary>
    /// 方法说明：计算势力列表单次滑动滚动行数。
    /// 参数说明：无参数。
    /// 返回说明：返回滚动行数。
    /// </summary>
    private int GetKingListScrollStep()
    {
        return Mathf.Max(1, kingListScrollStep);
    }

    /// <summary>
    /// 方法说明：判断势力数量是否超过左侧单列列表容量。
    /// 参数说明：无参数。
    /// 返回说明：超过单页容量返回 true，否则返回 false。
    /// </summary>
    private bool NeedKingListPaging()
    {
        return Informations.Instance.kingNum > GetKingListPageSize();
    }
}
