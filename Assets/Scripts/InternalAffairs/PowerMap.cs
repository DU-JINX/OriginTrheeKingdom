using UnityEngine;
using System.Collections;

public class PowerMap : MonoBehaviour {
	
	public IAController IACtrl;
	public ListController kingList;
	public MapController map;
	public KingInfoController kingInfo;
	
	private int state = 0;
	private float timeTick = 0;
	private Font kingListLabelFont;
	private int kingListLabelFontSize = 48;
	private float kingListLabelCharacterSize = 2.4f;
	
	/// <summary>
	/// 方法说明：初始化内政势力地图控制器。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	void Start () {
		
	}
	
	/// <summary>
	/// 方法说明：打开内政势力地图时刷新君主列表、地图和底部君主信息。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	void OnEnable() {
		state = 0;
		timeTick = 0;
		
		AddKingList();
		kingList.SetSelectItemHandler(OnSelectKingHandler);
		kingList.SetItemSelected(GetCurrentPlayerKingListIndex(), true);
		
		kingList.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.InsertFromLeft);
		map		.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.InsertFromRight);
		kingInfo.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.InsertFromBottom);
		
		map.gameObject.SetActive(true);
	}
	
	/// <summary>
	/// 方法说明：关闭内政势力地图时清理君主列表。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	void OnDisable() {
		kingList.Clear();
	}
	
	/// <summary>
	/// 方法说明：按当前状态处理势力地图返回逻辑。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	void Update () {
		switch (state) {
		case 0:
			OnNormal();
			break;
		case 1:
			OnReturnMain();
			break;
		}
	}
	
	/// <summary>
	/// 方法说明：处理势力地图正常浏览状态。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	void OnNormal() {
		if (Misc.GetBack()) {
			state = 1;
			
			kingList.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.OutToLeft);
			map		.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.OutToRight);
			kingInfo.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.OutToBottom);
		}
	}
	
	/// <summary>
	/// 方法说明：处理势力地图返回内政主菜单的动画收尾。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	void OnReturnMain() {
		timeTick += Time.deltaTime;
		if (timeTick < 0.2f) return;
		
		state = 0;
		timeTick = 0;
		
		gameObject.SetActive(false);
		map.gameObject.SetActive(false);
		
		IACtrl.OnReturnMain();
	}
	
	/// <summary>
	/// 方法说明：填充势力地图左侧君主列表。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	void AddKingList() {
		for (int i=0; i<Informations.Instance.kingNum; i++) {
			if (Informations.Instance.GetKingInfo(i).active == 1) {
				AddKingListItem(i);
			}
		}
	}

	/// <summary>
	/// 方法说明：读取当前玩家君主在势力列表中的行号。
	/// 参数说明：无参数。
	/// 返回说明：找到返回列表行号，找不到返回 0。
	/// </summary>
	int GetCurrentPlayerKingListIndex() {
		int listIndex = 0;
		for (int i = 0; i < Informations.Instance.kingNum; i++) {
			KingInfo kInfo = Informations.Instance.GetKingInfo(i);
			if (kInfo == null || kInfo.active != 1) continue;
			if (i == Controller.kingIndex) return listIndex;
			listIndex++;
		}

		return 0;
	}

	/// <summary>
	/// 方法说明：向势力地图左侧列表添加单个君主项。
	/// 参数说明：kingIdx 为君主索引。
	/// 返回说明：无返回值。
	/// </summary>
	void AddKingListItem(int kingIdx) {
		string name = ZhongWen.Instance.GetKingName(kingIdx);
		ListItem item = kingList.AddItem(name);
		item.SetItemData(kingIdx);
		HideListItemBitmapFont(item);
		CreateKingListLabel(item.transform, name);
	}

	/// <summary>
	/// 方法说明：创建君主列表动态字体标签。
	/// 参数说明：parent 为列表项 Transform，name 为君主名称。
	/// 返回说明：返回创建出的 TextMesh 标签。
	/// </summary>
	TextMesh CreateKingListLabel(Transform parent, string name) {
		GameObject go = new GameObject("PowerMapKingNameLabel");
		go.transform.parent = parent;
		go.transform.localPosition = Vector3.zero;
		go.transform.localScale = Vector3.one;
		go.transform.localRotation = Quaternion.identity;
		go.layer = parent.gameObject.layer;

		TextMesh textMesh = go.AddComponent<TextMesh>();
		textMesh.font = GetKingListLabelFont();
		textMesh.GetComponent<Renderer>().sharedMaterial = textMesh.font.material;
		textMesh.text = name;
		textMesh.fontSize = kingListLabelFontSize;
		textMesh.characterSize = kingListLabelCharacterSize;
		textMesh.anchor = TextAnchor.MiddleLeft;
		textMesh.alignment = TextAlignment.Left;
		textMesh.color = Color.white;
		return textMesh;
	}

	/// <summary>
	/// 方法说明：隐藏旧列表项 bitmap 字体，只保留点击热区。
	/// 参数说明：item 为列表项。
	/// 返回说明：无返回值。
	/// </summary>
	void HideListItemBitmapFont(ListItem item) {
		exSpriteFont font = item.GetComponent<exSpriteFont>();
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
	void MarkFontAsHandledByManualLabel(exSpriteFont font) {
		if (font == null || font.GetComponent<UnifiedGameFontIgnore>() != null) return;

		font.gameObject.AddComponent<UnifiedGameFontIgnore>();
	}

	/// <summary>
	/// 方法说明：读取君主列表动态字体。
	/// 参数说明：无参数。
	/// 返回说明：返回动态字体。
	/// </summary>
	Font GetKingListLabelFont() {
		if (kingListLabelFont == null) {
			kingListLabelFont = UnifiedGameFontController.CreateChineseDynamicFont(kingListLabelFontSize);
		}

		return kingListLabelFont;
	}
	
	/// <summary>
	/// 方法说明：响应势力地图左侧君主选择。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	void OnSelectKingHandler() {
		int kIdx = (int)kingList.GetSelectItem().GetItemData();
		RefreshKingListLabelColors(kIdx);
		
		kingInfo.SetKing(kIdx);
		
		KingInfo kInfo = Informations.Instance.GetKingInfo(kIdx);
		
		map.ClearSelect();
		for (int i=0; i<kInfo.cities.Count; i++) {
			map.SelectCity((int)kInfo.cities[i]);
		}
	}

	/// <summary>
	/// 方法说明：刷新君主列表动态字体颜色，并再次隐藏旧 bitmap 字体。
	/// 参数说明：selectedKingIdx 为当前选中君主索引。
	/// 返回说明：无返回值。
	/// </summary>
	void RefreshKingListLabelColors(int selectedKingIdx) {
		for (int i = 0; i < kingList.GetCount(); i++) {
			ListItem item = kingList.GetListItem(i);
			if (item == null) continue;

			HideListItemBitmapFont(item);
			TextMesh label = item.GetComponentInChildren<TextMesh>();
			if (label == null) continue;

			int kingIdx = (int)item.GetItemData();
			label.color = kingIdx == selectedKingIdx ? new Color(0f, 1f, 0f, 1f) : Color.white;
		}
	}
}
