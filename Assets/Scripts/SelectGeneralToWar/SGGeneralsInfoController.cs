using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SGGeneralsInfoController : MonoBehaviour {
	
	public SelectGeneralToWarController sgCtrl;
	
	public ListController 	generalsList;
	
	public GameObject		information;
	public GIInformation 	generalInfo;
	public GIFormation 		formation;
	public GIArms 			arms;
	public GIMagic 			magic;
	public GIEquipment 		equipment;
	
	public MenuDisplayAnim 	arrow;
	public ImageButton 		leftArrow;
	public ImageButton 		rightArrow;
	
	private int state = 0;
	private int selectIdx = 0;
	private int selectGeneralIdx = 0;
	
	private float timeTick;
	
	// Use this for initialization
	void Start () {
		
		generalsList.SetSelectItemHandler(OnSelectGeneral);
		
		leftArrow.SetButtonClickHandler(OnLeftArrowClickHandler);
		rightArrow.SetButtonClickHandler(OnRightArrowClickHandler);
	}
	
	void OnEnable() {
		state = 0;
		
		information.SetActive(false);
		SetGeneralsListVisible(true);
		generalsList.gameObject.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.InsertFromRight);
	}
	
	void OnDisable() {
		generalsList.Clear();
	}
	
	// Update is called once per frame
	void Update () {
		switch (state) {
		case 0:
			OnSelectGeneralModeHandler();
			break;
		case 1:
			OnGeneralInformationModeHandler();
			break;
		case 2:
			OnChangeToInformationModeHandler();
			break;
		case 3:
			OnChangeToGeneralsListModeHandler();
			break;
		case 4:
			OnReturnMainHandler();
			break;
		}
	}
	
	void OnSelectGeneralModeHandler() {
		if (Misc.GetBack()) {
			state = 4;
			generalsList.gameObject.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.OutToRight);
			return;
		}
	}
	
	void OnGeneralInformationModeHandler() {
		if (Misc.GetBack()) {
			state = 3;
			
			generalInfo	.gameObject.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.OutToLeft);
			formation	.gameObject.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.OutToTop);
			arms		.gameObject.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.OutToRight);
			magic		.gameObject.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.OutToBottom);
			equipment	.gameObject.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.OutToRight);
			arrow.SetAnim(MenuDisplayAnim.AnimType.OutToLeft);
			
			return;
		}
	}
	
	void OnChangeToInformationModeHandler() {
		timeTick += Time.deltaTime;
		if (timeTick >= 0.2f) {
			timeTick = 0;
			state = 1;
			
			SetGeneralsListVisible(false);
			information.SetActive(true);
			
			generalInfo	.gameObject.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.InsertFromLeft);
			formation	.gameObject.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.InsertFromTop);
			arms		.gameObject.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.InsertFromRight);
			magic		.gameObject.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.InsertFromBottom);
			equipment	.gameObject.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.InsertFromRight);
			arrow.SetAnim(MenuDisplayAnim.AnimType.InsertFromLeft);
		}
	}
	
	void OnChangeToGeneralsListModeHandler() {
		timeTick += Time.deltaTime;
		if (timeTick >= 0.2f) {
			timeTick = 0;
			state = 0;
			
			SetGeneralsListVisible(true);
			generalsList.gameObject.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.InsertFromRight);
			
			information.SetActive(false);
		}
	}
	
	void OnReturnMainHandler() {
		timeTick += Time.deltaTime;
		if (timeTick >= 0.2f) {
			timeTick = 0;
			
			gameObject.SetActive(false);
			
			sgCtrl.OnReturnMain();
		}
	}
	
	void OnSelectGeneral() {
		if (state != 0) return;
		
		state = 2;
				
		selectIdx = generalsList.GetSelectIndex();
		selectGeneralIdx = (int)generalsList.GetSelectItem().GetItemData();
		
		generalsList.gameObject.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.OutToRight);
		
		OnChangeGeneralInfo();
	}
	
	void OnLeftArrowClickHandler() {
		if (selectIdx > 0) {
			selectIdx--;
			selectGeneralIdx = (int)generalsList.GetListItem(selectIdx).GetItemData();
			
			OnChangeGeneralInfo();
		}
	}
	
	void OnRightArrowClickHandler() {
		if (selectIdx < generalsList.GetCount() - 1) {
			selectIdx++;
			selectGeneralIdx = (int)generalsList.GetListItem(selectIdx).GetItemData();
			
			OnChangeGeneralInfo();
		}
	}
	
	void OnChangeGeneralInfo() {
		generalInfo	.SetGeneral(selectGeneralIdx);
		formation	.SetGeneral(selectGeneralIdx);
		arms		.SetGeneral(selectGeneralIdx);
		magic		.SetGeneral(selectGeneralIdx);
		equipment	.SetGeneral(selectGeneralIdx);
	}

	// 方法说明：统一切换武将资料名单及其滚动条背景，避免进入详情页后名单地图层残留遮挡资料。
	// 参数说明：visible 为 true 时显示名单，为 false 时隐藏名单整组控件。
	// 返回说明：无返回值。
	void SetGeneralsListVisible(bool visible) {
		if (generalsList == null) return;

		SetGeneralsListLayerVisible(visible);
		generalsList.gameObject.SetActive(visible);
		if (generalsList.slider != null && generalsList.slider.parent != null) {
			generalsList.slider.parent.gameObject.SetActive(visible);
		}

		MapController[] mapBackgrounds = GetComponentsInChildren<MapController>(true);
		for (int i = 0; i < mapBackgrounds.Length; i++) {
			mapBackgrounds[i].gameObject.SetActive(visible);
		}
	}

	// 方法说明：按武将资料控制器直接子节点切换名单层，详情页只保留资料面板和左右切换箭头。
	// 参数说明：visible 为 true 时显示名单层，为 false 时隐藏名单层。
	// 返回说明：无返回值。
	void SetGeneralsListLayerVisible(bool visible) {
		for (int i = 0; i < transform.childCount; i++) {
			Transform child = transform.GetChild(i);
			if (IsInformationDetailNode(child)) continue;

			child.gameObject.SetActive(visible);
		}
	}

	// 方法说明：判断节点是否属于武将资料详情页需要保留的对象。
	// 参数说明：child 为待判断的直接子节点。
	// 返回说明：需要保留返回 true，否则返回 false。
	bool IsInformationDetailNode(Transform child) {
		if (child == null) return false;
		if (information != null && child == information.transform) return true;
		if (arrow != null && child == arrow.transform) return true;
		if (leftArrow != null && child == leftArrow.transform) return true;
		if (rightArrow != null && child == rightArrow.transform) return true;

		return false;
	}
	
	public void AddGeneralsList(List<int> generals) {
		
		gameObject.SetActive(true);
		
		for (int i=0; i<generals.Count; i++) {
			
			int gIdx = generals[i];
			generalsList.AddItem(ZhongWen.Instance.GetGeneralName1(gIdx)).SetItemData(gIdx);
		}
	}
	
}
