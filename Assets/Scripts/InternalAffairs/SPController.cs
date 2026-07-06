using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SPController : MonoBehaviour {
	private const int StateSurrenderAllResult = 9;
	
	public IAController IACtrl;
	
	public ListController prisonerList;
	public DialogueController kingDialogue;
	public DialogueController prisonerDialogue;
	
	private int state = -1;
	private bool havePrison = false;
	
	private int prisonIdx;
	private int dialogueIdx;
	
	private float timeTick = 0;
	private GUIStyle surrenderAllButtonStyle = null;
	
	// Use this for initialization
	void Start () {
		prisonerList.SetSelectItemHandler(OnSelectPrisonHandler);
	}
	
	void OnEnable() {
		
		if (havePrison) {
			state = 2;
			
			prisonerList.gameObject.SetActive(true);
			
			prisonerList.gameObject.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.InsertFromLeft);
		} else {
			state = 0;
			
			prisonerList.gameObject.SetActive(false);
		}
	}
	
	void OnDisable() {
		havePrison = false;
		prisonerList.Clear();
	}
	
	// Update is called once per frame
	void Update () {
		switch (state) {
		case 0:
			OnNoPrisonDialogueInsertHandler();
			break;
		case 1:
			OnNoPrisonDialogueOutHandler();
			break;
		case 2:
			OnSelectPrisonModeHandler();
			break;
		case 3:
			OnKingAskModeHandler();
			break;
		case 4:
			OnPrisonAnswerModeHandler();
			break;
		case 5:
			OnKingAsnwerModeHandler();
			break;
		case 6:
			OnPrisonNoInCityHandler();
			break;
		case 7:
			OnDialogueOverHandler();
			break;
		case 8:
			OnReturnMainModeHandler();
			break;
		case StateSurrenderAllResult:
			OnSurrenderAllResultModeHandler();
			break;
		}
	}

	// 方法说明：绘制年度内政招降的一键招降触屏按钮。
	// 参数说明：无。
	// 返回说明：无返回值。
	void OnGUI() {
		if (state != 2 || !HasSelectablePrisoners()) {
			return;
		}

		float scale = Mathf.Min(Screen.width / 640f, Screen.height / 480f);
		Rect buttonRect = new Rect(Screen.width - 142f * scale,
		                           Screen.height - 70f * scale,
		                           118f * scale,
		                           42f * scale);

		if (GUI.Button(buttonRect, ZhongWen.Instance.zhaoxiang_all, GetSurrenderAllButtonStyle(scale))) {
			OnSurrenderAllButton();
		}
	}
	
	void OnNoPrisonDialogueInsertHandler() {
		if (!kingDialogue.IsShowingText() && Input.GetMouseButtonUp(0)) {
			state = 1;
			kingDialogue.SetDialogueOut(MenuDisplayAnim.AnimType.OutToBottom);
		}
	}
	
	void OnNoPrisonDialogueOutHandler() {
		if (kingDialogue.gameObject.activeSelf == false) {
			gameObject.SetActive(false);
			IACtrl.ResetState();
		}
	}
	
	void OnSelectPrisonModeHandler() {
		if (Misc.GetBack()) {
			state = 8;
			
			prisonerList.gameObject.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.OutToLeft);
		}
	}
	
	void OnSelectPrisonHandler() {
		if (state != 2)	return;
		
		prisonIdx = (int)prisonerList.GetSelectItem().GetItemData();
		string msg = "";
		
		if (Informations.Instance.GetGeneralInfo(prisonIdx).city != -1) {
			state = 3;
			
			if (Informations.Instance.GetGeneralInfo(prisonIdx).king == Controller.kingIndex) {
				msg = ZhongWen.Instance.GetGeneralName(prisonIdx) + ZhongWen.Instance.zhaoxiang_guilai;
			} else {
				msg = ZhongWen.Instance.GetGeneralName(prisonIdx) + ZhongWen.Instance.zhaoxiang_ask;
			}
		} else {
			state = 6;
			Informations.Instance.GetGeneralInfo(prisonIdx).active = 0;
			msg = ZhongWen.Instance.GetGeneralName(prisonIdx) + ZhongWen.Instance.zhaoxiang_buzai;
		}
		
		prisonerList.enabled = false;
		
		kingDialogue.SetDialogue(Informations.Instance.GetKingInfo(Controller.kingIndex).generalIdx, msg, MenuDisplayAnim.AnimType.InsertFromBottom);
	}
	
	void OnKingAskModeHandler() {
		if (!kingDialogue.IsShowingText()) {
			if (Input.GetMouseButtonUp(0)) {
				state = 4;
				bool isSuccess = ApplySurrenderResultForGeneral(prisonIdx, out dialogueIdx);
				if (isSuccess) {
					SoundController.Instance.PlaySound("00045");
				} else {
					SoundController.Instance.PlaySound("00057");
				}
				
				string msg = ZhongWen.Instance.zhaoxiang_wenda[dialogueIdx * 2];
				
				prisonerDialogue.SetDialogue(prisonIdx, msg, MenuDisplayAnim.AnimType.InsertFromTop);
			}
		}
	}
	
	void OnPrisonAnswerModeHandler() {
		if (!prisonerDialogue.IsShowingText()) {
			if (Input.GetMouseButtonUp(0)) {
				state = 5;
				
				Input.ResetInputAxes();
				
				kingDialogue.SetText(ZhongWen.Instance.zhaoxiang_wenda[dialogueIdx * 2 + 1]);
			}
		}
	}
	
	void OnKingAsnwerModeHandler() {
		if (!kingDialogue.IsShowingText()) {
			if (Input.GetMouseButtonUp(0)) {
				state = 7;
				
				kingDialogue.SetDialogueOut(MenuDisplayAnim.AnimType.OutToBottom);
				prisonerDialogue.SetDialogueOut(MenuDisplayAnim.AnimType.OutToTop);
				
				prisonerList.GetSelectItem().SetSelectEnable(false);
				prisonerList.SetItemSelected(-1, false);
			}
		}
	}
	
	void OnPrisonNoInCityHandler() {
		if (!kingDialogue.IsShowingText()) {
			if (Input.GetMouseButtonUp(0)) {
				state = 7;
				
				kingDialogue.SetDialogueOut(MenuDisplayAnim.AnimType.OutToBottom);
				
				prisonerList.GetSelectItem().SetSelectEnable(false);
				prisonerList.SetItemSelected(-1, false);
			}
		}
	}

	// 方法说明：处理年度内政一键招降结果提示后的点击。
	// 参数说明：无。
	// 返回说明：无返回值。
	void OnSurrenderAllResultModeHandler() {
		if (!kingDialogue.IsShowingText()) {
			if (Input.GetMouseButtonUp(0)) {
				state = 7;
				Input.ResetInputAxes();
				kingDialogue.SetDialogueOut(MenuDisplayAnim.AnimType.OutToBottom);
				prisonerList.SetItemSelected(-1, false);
			}
		}
	}
	
	void OnDialogueOverHandler() {
		if (!kingDialogue.gameObject.activeSelf && !prisonerDialogue.gameObject.activeSelf) {
			state = 2;
			
			prisonerList.enabled = true;
		}
	}

	// 方法说明：处理年度内政一键招降按钮点击。
	// 参数说明：无。
	// 返回说明：无返回值。
	void OnSurrenderAllButton() {
		if (state != 2 || !HasSelectablePrisoners()) {
			return;
		}

		ApplySurrenderAllResult();
	}

	// 方法说明：批量结算年度内政招降列表中本年度仍可招降的俘虏。
	// 参数说明：无。
	// 返回说明：无返回值。
	void ApplySurrenderAllResult() {
		Input.ResetInputAxes();
		prisonerList.enabled = false;
		int successCount = 0;
		int returnCount = 0;
		int failCount = 0;
		int notInCityCount = 0;
		List<int> successGenerals = new List<int>();
		List<int> returnGenerals = new List<int>();
		List<int> failGenerals = new List<int>();
		List<int> notInCityGenerals = new List<int>();

		// 1. 遍历当前年度招降列表，只处理仍可选的俘虏，避免重复消耗已行动武将。
		for (int i=0; i<prisonerList.GetCount(); i++) {
			ListItem item = prisonerList.GetListItem(i);
			if (item == null || !item.GetSelectEnable()) {
				continue;
			}

			int gIdx = (int)item.GetItemData();
			GeneralInfo gInfo = Informations.Instance.GetGeneralInfo(gIdx);
			bool isOwnGeneralReturn = gInfo.king == Controller.kingIndex;

			// 2. 不在城中的俘虏沿用原逻辑：本年度标为已处理，但不强行登用。
			if (gInfo.city == -1) {
				gInfo.active = 0;
				item.SetSelectEnable(false);
				notInCityCount++;
				notInCityGenerals.Add(gIdx);
				continue;
			}

			// 3. 在城中的俘虏按原年度招降概率逐个结算。
			int resultDialogueIdx;
			bool isSuccess = ApplySurrenderResultForGeneral(gIdx, out resultDialogueIdx);
			item.SetSelectEnable(false);
			if (isSuccess && isOwnGeneralReturn) {
				returnCount++;
				returnGenerals.Add(gIdx);
			} else if (isSuccess) {
				successCount++;
				successGenerals.Add(gIdx);
			} else {
				failCount++;
				failGenerals.Add(gIdx);
			}
		}

		// 4. 汇总显示本次一键招降结果，并回到招降列表。
		if (successCount + returnCount > 0) {
			SoundController.Instance.PlaySound("00045");
		} else {
			SoundController.Instance.PlaySound("00057");
		}

		state = StateSurrenderAllResult;
		prisonerList.SetItemSelected(-1, false);
		string msg = GetSurrenderAllResultMessage(successCount,
		                                          returnCount,
		                                          failCount,
		                                          notInCityCount,
		                                          successGenerals,
		                                          returnGenerals,
		                                          failGenerals,
		                                          notInCityGenerals);
		kingDialogue.SetDialogue(Informations.Instance.GetKingInfo(Controller.kingIndex).generalIdx,
		                          msg,
		                          MenuDisplayAnim.AnimType.InsertFromBottom);
	}

	// 方法说明：生成年度内政一键招降的结果文案。
	// 参数说明：successCount 为敌将归降数，returnCount 为旧将回归数，failCount 为失败数，notInCityCount 为不在城中数，后四个列表为对应武将编号名单。
	// 返回说明：返回用于君主对话框展示的结果文案。
	string GetSurrenderAllResultMessage(int successCount,
	                                    int returnCount,
	                                    int failCount,
	                                    int notInCityCount,
	                                    List<int> successGenerals,
	                                    List<int> returnGenerals,
	                                    List<int> failGenerals,
	                                    List<int> notInCityGenerals) {
		string msg = string.Format(ZhongWen.Instance.zhaoxiang_all_result,
		                           successCount,
		                           returnCount,
		                           failCount);
		if (notInCityCount > 0) {
			msg += string.Format(ZhongWen.Instance.zhaoxiang_all_result_buzai, notInCityCount);
		}

		msg += GetSurrenderAllNameDetail(ZhongWen.Instance.zhaoxiang_all_success_label, successGenerals);
		msg += GetSurrenderAllNameDetail(ZhongWen.Instance.zhaoxiang_all_return_label, returnGenerals);
		msg += GetSurrenderAllNameDetail(ZhongWen.Instance.zhaoxiang_all_fail_label, failGenerals);
		msg += GetSurrenderAllNameDetail(ZhongWen.Instance.zhaoxiang_all_not_in_city_label, notInCityGenerals);

		return msg;
	}

	// 方法说明：生成一键招降某类结果的武将名单文案。
	// 参数说明：label 为名单标题，generals 为武将编号列表。
	// 返回说明：存在名单时返回标题和武将名，不存在名单时返回空字符串。
	string GetSurrenderAllNameDetail(string label, List<int> generals) {
		if (generals.Count == 0) {
			return "";
		}

		return label + ZhongWen.Instance.maohao + GetGeneralNamesText(generals) + ZhongWen.Instance.juhao;
	}

	// 方法说明：把武将编号列表转换为顿号分隔的武将名称。
	// 参数说明：generals 为武将编号列表。
	// 返回说明：返回可显示在一键招降结果中的武将名称字符串。
	string GetGeneralNamesText(List<int> generals) {
		List<string> names = new List<string>();
		for (int i=0; i<generals.Count; i++) {
			names.Add(ZhongWen.Instance.GetGeneralName(generals[i]));
		}

		return string.Join(ZhongWen.Instance.dunhao, names.ToArray());
	}

	// 方法说明：按年度招降概率结算指定俘虏。
	// 参数说明：gIdx 为武将编号，resultDialogueIdx 返回原单人对话索引。
	// 返回说明：招降成功或旧将回归返回 true，失败返回 false。
	bool ApplySurrenderResultForGeneral(int gIdx, out int resultDialogueIdx) {
		GeneralInfo gInfo = Informations.Instance.GetGeneralInfo(gIdx);
		bool isSuccess = false;
		resultDialogueIdx = 0;

		gInfo.active = 0;

		if (gInfo.king == Controller.kingIndex) {
			resultDialogueIdx = 5;
			isSuccess = true;
		} else if (Random.Range(0, 100) < (100 - gInfo.loyalty) / 2) {
			resultDialogueIdx = 0;
			isSuccess = true;
		} else {
			resultDialogueIdx = (100 - gInfo.loyalty) / 10 + 1;
			resultDialogueIdx = Mathf.Clamp(resultDialogueIdx, 0, 4);
			LowerSurrenderLoyalty(gInfo);
		}

		if (isSuccess) {
			ApplySurrenderSuccess(gInfo, gIdx);
		}

		return isSuccess;
	}

	// 方法说明：处理年度招降失败后的忠诚变化。
	// 参数说明：gInfo 为招降失败的俘虏武将数据。
	// 返回说明：无返回值。
	void LowerSurrenderLoyalty(GeneralInfo gInfo) {
		gInfo.loyalty -= Random.Range(5, 20);
		gInfo.loyalty = Mathf.Clamp(gInfo.loyalty, 0, 100);
	}

	// 方法说明：把年度招降成功的俘虏加入所在城市和当前君主武将列表。
	// 参数说明：gInfo 为成功招降的武将数据，gIdx 为武将编号。
	// 返回说明：无返回值。
	void ApplySurrenderSuccess(GeneralInfo gInfo, int gIdx) {
		gInfo.loyalty = 90;
		gInfo.king = Controller.kingIndex;
		gInfo.prisonerIdx = -1;
		gInfo.soldierCur = gInfo.soldierMax;
		gInfo.knightCur = gInfo.knightMax;

		CityInfo cInfo = Informations.Instance.GetCityInfo(gInfo.city);
		cInfo.prisons.Remove(gIdx);
		AddGeneralUnique(cInfo.generals, gIdx);
		AddGeneralUnique(Informations.Instance.GetKingInfo(Controller.kingIndex).generals, gIdx);
	}

	// 方法说明：向武将编号列表追加不存在的武将，避免一键招降造成重复归属。
	// 参数说明：generals 为武将编号列表，gIdx 为武将编号。
	// 返回说明：无返回值。
	void AddGeneralUnique(List<int> generals, int gIdx) {
		if (!generals.Contains(gIdx)) {
			generals.Add(gIdx);
		}
	}

	// 方法说明：判断当前年度招降列表中是否还有可操作俘虏。
	// 参数说明：无。
	// 返回说明：存在可操作俘虏返回 true，否则返回 false。
	bool HasSelectablePrisoners() {
		if (prisonerList == null) {
			return false;
		}

		for (int i=0; i<prisonerList.GetCount(); i++) {
			ListItem item = prisonerList.GetListItem(i);
			if (item != null && item.GetSelectEnable()) {
				return true;
			}
		}

		return false;
	}

	// 方法说明：取得年度内政一键招降按钮样式。
	// 参数说明：scale 为屏幕缩放系数。
	// 返回说明：返回当前帧使用的一键招降按钮样式。
	GUIStyle GetSurrenderAllButtonStyle(float scale) {
		if (surrenderAllButtonStyle == null) {
			surrenderAllButtonStyle = new GUIStyle(GUI.skin.button);
			surrenderAllButtonStyle.alignment = TextAnchor.MiddleCenter;
			surrenderAllButtonStyle.fontStyle = FontStyle.Bold;
			surrenderAllButtonStyle.wordWrap = false;
			surrenderAllButtonStyle.clipping = TextClipping.Overflow;
		}

		surrenderAllButtonStyle.fontSize = Mathf.RoundToInt(16f * scale);
		return surrenderAllButtonStyle;
	}
	
	void OnReturnMainModeHandler() {
		timeTick += Time.deltaTime;
		if (timeTick >= 0.2f) {
			timeTick = 0;
			
			gameObject.SetActive(false);
			IACtrl.OnReturnMain();
		}
	}
	
	public bool AddPrisonsList() {
		
		int king = Controller.kingIndex;
		
		for (int i=0; i<Informations.Instance.generalNum; i++) {
			
			GeneralInfo gInfo = Informations.Instance.GetGeneralInfo(i);
			
			if (gInfo.prisonerIdx == king) {
				
				havePrison = true;
				
				ListItem li = prisonerList.AddItem(ZhongWen.Instance.GetGeneralName1(i));
				li.SetItemData(i);
				if (gInfo.active == 0) {
					li.SetSelectEnable(false);
				}
			}
		}
		
		if (!havePrison) {
			kingDialogue.SetDialogue(Informations.Instance.GetKingInfo(king).generalIdx, ZhongWen.Instance.zhaoxiang_no, MenuDisplayAnim.AnimType.InsertFromBottom);
		}
		
		return havePrison;
	}
}
