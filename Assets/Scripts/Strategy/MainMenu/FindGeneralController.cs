using UnityEngine;
using System.Collections;

public class FindGeneralController : MonoBehaviour {
	
	public StrategyController strCtrl;
	public ListController generalsList;
	public MapController map;
	public CityInfoController cityInfo;
	public ChoiceTargetController choiceTarget;
	
	public GameObject confirmBox;
	public Button okButton;
	public Button cancelButton;
	
	private int state = -1;
	private int generalSelected;
	private float timeTick;
	
	// Use this for initialization
	void Start () {
		generalsList.SetSelectItemHandler(OnSelectGeneralHandler);
	}
	
	void OnEnable() {
		state = 0;
		generalsList.Clear();
		
		map.gameObject.SetActive(true);
		ApplyRecoveredFindGeneralMapLayout();
		
		generalsList.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.InsertFromLeft);
		map			.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.InsertFromRight);
		cityInfo	.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.InsertFromBottom);
		
		AddGeneralsList();
	}

	// 方法说明：修正 MOD06 寻找武将地图预览位置和尺寸，避免宽图压住底部城池信息栏。
	// 参数说明：无。
	// 返回说明：无返回值。
	void ApplyRecoveredFindGeneralMapLayout() {
		if (!MODLoadController.IsRestoredSango2Index(Controller.MODSelect) || map == null) return;

		Vector3 mapPosition = map.transform.localPosition;
		mapPosition.x = 430f;
		mapPosition.y = 225f;
		map.transform.localPosition = mapPosition;
		map.transform.localScale = new Vector3(0.65f, 0.65f, map.transform.localScale.z);

		MenuDisplayAnim mapAnim = map.GetComponent<MenuDisplayAnim>();
		if (mapAnim == null) return;

		Vector3 originalPosition = mapAnim.GetOriginalPosition();
		originalPosition.x = mapPosition.x;
		originalPosition.y = mapPosition.y;
		mapAnim.SetOriginalPosition(originalPosition);
	}
	
	void OnDisable() {
		generalsList.Clear();
	}
	
	// Update is called once per frame
	void Update () {
		switch (state) {
		case 0:
			OnNormalModeHandler();
			break;
		case 1:
			OnConfirmModeController();
			break;
		case 2:
			OnGoToGeneralController();
			break;
		case 3:
			OnReturnMainModeHandler();
			break;
		}
	}
	
	void OnNormalModeHandler() {
		if (Misc.GetBack()) {
			state = 3;
			
			generalsList.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.OutToLeft);
			map			.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.OutToRight);
			cityInfo	.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.OutToBottom);
		}
	}
	
	void OnSelectGeneralHandler() {
		
		if (state != 0)	return;
		
		state = 1;
		
		int idx = (int)generalsList.GetSelectItem().GetItemData();
		generalSelected = idx;
		
		GeneralInfo gInfo = Informations.Instance.GetGeneralInfo(idx);
		
		cityInfo.SetCity(gInfo.city);
		map.ClearSelect();
		map.SelectCity(gInfo.city);
		
		confirmBox.SetActive(true);
		confirmBox.transform.position = new Vector3(confirmBox.transform.position.x, 
			generalsList.GetSelectItem().transform.position.y, confirmBox.transform.position.z);
		
		generalsList.enabled = false;
	}
	
	void OnConfirmModeController() {
		
		if (Misc.GetBack()) {
			state = 0;
			
			confirmBox.SetActive(false);
			generalsList.enabled = true;
			
			return;
		}
		
		if (okButton.GetButtonState() == Button.ButtonState.Clicked) {
			
			state = 2;
			
			cancelButton.SetButtonState(Button.ButtonState.Normal);
			confirmBox.SetActive(false);
			generalsList.enabled = true;
			
			generalsList.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.OutToLeft);
			map			.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.OutToRight);
			cityInfo	.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.OutToBottom);
			
		} else if (cancelButton.GetButtonState() == Button.ButtonState.Clicked) {
			
			state = 0;
			
			cancelButton.SetButtonState(Button.ButtonState.Normal);
			confirmBox.SetActive(false);
			generalsList.enabled = true;
		}
	}
	
	void OnGoToGeneralController() {
		timeTick += Time.deltaTime;
		
		if (timeTick >= 0.2f) {
			timeTick = 0;
			
			gameObject.SetActive(false);
			
			GeneralInfo gInfo = Informations.Instance.GetGeneralInfo(generalSelected);
			if (gInfo.city != -1) {
				choiceTarget.AddCityTarget(gInfo.city);
				choiceTarget.Show();
			} else {
				
				foreach (ArmyInfo armyInfo in Informations.Instance.armys) {
					foreach(int gIdx in armyInfo.generals) {
						
						if (gIdx == generalSelected) {
							choiceTarget.AddArmyTarget(armyInfo);
							choiceTarget.Show();
							return;
						}
					}
				}
			}
		}
	}
	
	void OnReturnMainModeHandler() {
		
		timeTick += Time.deltaTime;
		
		if (timeTick >= 0.2f) {
			timeTick = 0;
			
			gameObject.SetActive(false);
			
			//StrategyController.state = StrategyController.State.Normal;
			strCtrl.ReturnMainMode();
		}
	}
	
	void AddGeneralsList() {
		
		int kingIdx = Informations.Instance.GetKingInfo(Controller.kingIndex).generalIdx;
		AddGeneralListItem(kingIdx);
		
		GeneralInfo gInfo = Informations.Instance.GetGeneralInfo(kingIdx);
		
		cityInfo.SetCity(gInfo.city);
		map.ClearSelect();
		map.SelectCity(gInfo.city);
		
		KingInfo kInfo = Informations.Instance.GetKingInfo(Controller.kingIndex);
		
		for (int i=0; i<kInfo.generals.Count; i++) {
			
			int gIdx = (int)kInfo.generals[i];
			
			if (gIdx != kInfo.generalIdx) {
				AddGeneralListItem(gIdx);
			}
		}
		
	}

	// 方法说明：添加寻找武将列表项，由全局字体镜像统一处理显示和选中颜色。
	// 参数说明：generalIdx 为武将编号。
	// 返回说明：返回创建出的列表项。
	ListItem AddGeneralListItem(int generalIdx) {
		string name = ZhongWen.Instance.GetGeneralName(generalIdx);
		ListItem item = generalsList.AddItem(name);
		item.SetItemData(generalIdx);
		return item;
	}
}
