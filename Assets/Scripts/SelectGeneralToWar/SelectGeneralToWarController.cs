using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SelectGeneralToWarController : MonoBehaviour {
	
	public TopBarInfo leftBar;
	public TopBarInfo rightBar;
	
	public SGSelectGeneralInfo leftGeneralInfo;
	public SGSelectGeneralInfo rightGeneralInfo;
	
	public SGGeneralsListController generalListCtrl;
	
	public SGSelectFormation selectFormation;
	public SGGeneralsInfoController generalsInfo;
	public GameObject generalPos;
	public GameObject retreatConfirm;
	
	public DialogueController dialogCtrl;
	
	public Button[] menus;
	
	public static int mode;
	public static int cityAttacked;
	public static ArmyInfo mine;
	public static ArmyInfo enemy;
	
	public static bool isWarBegin = true;
	public static int warResult;
	
	private static int leftSelectIdx;
	private static int rightSelectIdx;
	private static int leftKing;
	private static int rightKing;
	private static int leftDefense;
	private static int rightDefense;
	private static int leftExperience;
	private static int rightExperience;
	private static int mineCommander;
	
	private static List<int> leftGenerals;
	private static List<int> rightGenerals;
	
	private static bool[] leftFailFlag;
	private static bool[] rightFailFlag;
	private static int[] gPos; //0:back 1:front

	private const int StatePostBattleSurrenderAsk = 6;
	private const int StatePostBattleSurrenderPrisonerAnswer = 7;
	private const int StatePostBattleSurrenderKingAnswer = 8;
	private const int StateQuickBattleConfirm = 9;
	private const string PostBattleSurrenderBackgroundResource = "PostBattleSurrenderBackground";

	private enum QuickBattlePriority {
		LowStrength,
		HighStrength
	}
		
	private int state;
	private int menuSelectIdx = -1;
	private bool isPrisoned;
	private bool isWarOver;
	private int prisonCheckIdx;
	private List<int> postBattleSurrenderPrisons = new List<int>();
	private int postBattleSurrenderIdx;
	private int postBattleSurrenderDialogueIdx;
	private int postBattleSurrenderCityIdx = -1;
	private CityInfo postBattleSurrenderCityInfo = null;
	private ArmyInfo postBattleSurrenderArmyInfo = null;
	private QuickBattlePriority quickBattlePriority = QuickBattlePriority.LowStrength;
	private Texture2D postBattleSurrenderBackgroundTexture = null;
	private bool isPostBattleSurrenderStageActive = false;
	private bool isPostBattleSurrenderGuiButtonClicked = false;
	private float postBattleSurrenderBackgroundAlpha = 0f;
	private string postBattleSurrenderText = "";
	private GUIStyle quickBattlePanelStyle = null;
	private GUIStyle quickBattleQuestionStyle = null;
	private GUIStyle quickBattleButtonStyle = null;
	private GUIStyle quickBattleCancelButtonStyle = null;
	private GUIStyle postBattleSurrenderTitleStyle = null;
	private GUIStyle postBattleSurrenderTextStyle = null;
	private GUIStyle postBattleSurrenderHintStyle = null;
	private GUIStyle postBattleSurrenderButtonStyle = null;
	
	// Use this for initialization
	void Start () {
		//test
		/*
		if (isWarBegin) {
			mine = new ArmyInfo();
			mine.king = 0;
			mine.cityFrom = 0;
			mine.cityTo = 1;
			mine.commander = 98;
			mine.generals.Add(98);
			for (int i=0; i<20; i++) {
				int gIdx = Random.Range(0, 90);
				if (Informations.Instance.GetGeneralInfo(gIdx).king != -1) {
					mine.generals.Add(gIdx);
				}
			}
			enemy = new ArmyInfo();
			enemy.king = 1;
			enemy.cityFrom = 1;
			enemy.cityTo = 0;
			
			for (int i=0; i<5; i++) {
				int gIdx = Random.Range(99, 200);
				if (Informations.Instance.GetGeneralInfo(gIdx).king != -1) {
					enemy.generals.Add(gIdx);
				}
			}
		}
		*/
		InitScene();
		InitMenuAction();
	}
	
	// Update is called once per frame
	void Update () {

		UpdatePostBattleSurrenderStageAnimation();
		
		switch (state) {
		case 1:
			if (!dialogCtrl.IsShowingText() && Input.GetMouseButtonUp(0)) {
				dialogCtrl.SetDialogueOut(MenuDisplayAnim.AnimType.OutToBottom);
				
				state = 0;
				Invoke("OnReturnMain", 0.5f);
			}
			break;
		case 2:
			if (!dialogCtrl.IsShowingText() && Input.GetMouseButtonUp(0)) {
				dialogCtrl.SetDialogueOut(MenuDisplayAnim.AnimType.OutToBottom);
				
				state = 0;
				Invoke("WarOverResult", 0.5f);
			}
			break;
		case 3:
			if (!dialogCtrl.IsShowingText() && Input.GetMouseButtonUp(0)) {
				Input.ResetInputAxes();
				if (warResult == 0) {
					bool flag = false;
					for (int i=0; i<rightGenerals.Count; i++) {
						GeneralInfo gInfo = Informations.Instance.GetGeneralInfo(rightGenerals[i]);
						if (gInfo.prisonerIdx != -1) {
							gInfo.prisonerIdx = -1;
							flag= true;
						}
					}
					
					if (flag) {
						state = 4;
						dialogCtrl.SetText(ZhongWen.Instance.womengbeifude);
					} else {
						flag = false;
						for (int i=0; i<leftGenerals.Count; i++) {
							GeneralInfo gInfo = Informations.Instance.GetGeneralInfo(leftGenerals[i]);
							if (gInfo.prisonerIdx != -1) {
								flag= true;
								prisonCheckIdx = i;
								break;
							}
						}
						
						if (!flag) {
							dialogCtrl.SetDialogueOut(MenuDisplayAnim.AnimType.OutToBottom);
				
							state = 0;
							Invoke("WarOverResult", 0.5f);
						} else {
							state = 5;
							dialogCtrl.SetText(ZhongWen.Instance.womenfulule + ZhongWen.Instance.GetGeneralName(leftGenerals[prisonCheckIdx++]) + ZhongWen.Instance.tanhao);
						}
					}
				} else {
					bool flag = false;
					for (int i=0; i<leftGenerals.Count; i++) {
						GeneralInfo gInfo = Informations.Instance.GetGeneralInfo(leftGenerals[i]);
						if (gInfo.prisonerIdx != -1) {
							gInfo.prisonerIdx = -1;
							flag= true;
						}
					}
					
					if (flag) {
						state = 4;
						dialogCtrl.SetText(ZhongWen.Instance.fuludewujiang);
					} else {
						flag = false;
						for (int i=0; i<rightGenerals.Count; i++) {
							GeneralInfo gInfo = Informations.Instance.GetGeneralInfo(rightGenerals[i]);
							if (gInfo.prisonerIdx != -1) {
								flag= true;
								prisonCheckIdx = i;
								break;
							}
						}
						
						if (!flag) {
							dialogCtrl.SetDialogueOut(MenuDisplayAnim.AnimType.OutToBottom);
				
							state = 0;
							Invoke("WarOverResult", 0.5f);
						} else {
							state = 5;
							dialogCtrl.SetText(ZhongWen.Instance.GetGeneralName(rightGenerals[prisonCheckIdx++]) + ZhongWen.Instance.beifulule + ZhongWen.Instance.tanhao);
						}
					}
				}
			}
			break;
		case 4:
			if (!dialogCtrl.IsShowingText() && Input.GetMouseButtonUp(0)) {
				Input.ResetInputAxes();
				if (warResult == 0) {
					bool flag = false;
					for (int i=0; i<leftGenerals.Count; i++) {
						GeneralInfo gInfo = Informations.Instance.GetGeneralInfo(leftGenerals[i]);
						if (gInfo.prisonerIdx != -1) {
							flag= true;
							prisonCheckIdx = i;
							break;
						}
					}
					
					if (!flag) {
						dialogCtrl.SetDialogueOut(MenuDisplayAnim.AnimType.OutToBottom);
			
						state = 0;
						Invoke("WarOverResult", 0.5f);
					} else {
						state = 5;
						dialogCtrl.SetText(ZhongWen.Instance.womenfulule + ZhongWen.Instance.GetGeneralName(leftGenerals[prisonCheckIdx++]) + ZhongWen.Instance.tanhao);
					}
				} else {
					bool flag = false;
					for (int i=0; i<rightGenerals.Count; i++) {
						GeneralInfo gInfo = Informations.Instance.GetGeneralInfo(rightGenerals[i]);
						if (gInfo.prisonerIdx != -1) {
							flag= true;
							prisonCheckIdx = i;
							break;
						}
					}
					
					if (!flag) {
						dialogCtrl.SetDialogueOut(MenuDisplayAnim.AnimType.OutToBottom);
			
						state = 0;
						Invoke("WarOverResult", 0.5f);
					} else {
						state = 5;
						dialogCtrl.SetText(ZhongWen.Instance.GetGeneralName(rightGenerals[prisonCheckIdx++]) + ZhongWen.Instance.beifulule + ZhongWen.Instance.tanhao);
					}
				}
			}
			break;
			case 5:
				if (!dialogCtrl.IsShowingText() && Input.GetMouseButtonUp(0)) {
					Input.ResetInputAxes();
					if (warResult == 0) {
					bool flag = false;
					for (int i=prisonCheckIdx; i<leftGenerals.Count; i++) {
						GeneralInfo gInfo = Informations.Instance.GetGeneralInfo(leftGenerals[i]);
						if (gInfo.prisonerIdx != -1) {
							flag = true;
							prisonCheckIdx = i;
							break;
						}
					}
					
					if (!flag) {
						dialogCtrl.SetDialogueOut(MenuDisplayAnim.AnimType.OutToBottom);
			
						state = 0;
						Invoke("WarOverResult", 0.5f);
					} else {
						dialogCtrl.SetText(ZhongWen.Instance.womenfulule + ZhongWen.Instance.GetGeneralName(leftGenerals[prisonCheckIdx++]) + ZhongWen.Instance.tanhao);
					}
				} else {
					bool flag = false;
					for (int i=prisonCheckIdx; i<rightGenerals.Count; i++) {
						GeneralInfo gInfo = Informations.Instance.GetGeneralInfo(rightGenerals[i]);
						if (gInfo.prisonerIdx != -1) {
							flag = true;
							prisonCheckIdx = i;
							break;
						}
					}
					
					if (!flag) {
						dialogCtrl.SetDialogueOut(MenuDisplayAnim.AnimType.OutToBottom);
			
						state = 0;
						Invoke("WarOverResult", 0.5f);
					} else {
						dialogCtrl.SetText(ZhongWen.Instance.GetGeneralName(rightGenerals[prisonCheckIdx++]) + ZhongWen.Instance.beifulule + ZhongWen.Instance.tanhao);
					}
					}
				}
				break;
			case StatePostBattleSurrenderAsk:
				OnPostBattleSurrenderAskModeHandler();
				break;
			case StatePostBattleSurrenderPrisonerAnswer:
				OnPostBattleSurrenderPrisonerAnswerModeHandler();
				break;
			case StatePostBattleSurrenderKingAnswer:
				OnPostBattleSurrenderKingAnswerModeHandler();
				break;
			case StateQuickBattleConfirm:
				OnQuickBattleConfirmModeHandler();
				break;
			}
		}
	
	void LateUpdate() {
		
		if (menuSelectIdx != -1) {
			menus[menuSelectIdx].GetComponent<exSpriteFont>().topColor = new Color(1, 0, 0, 1);
			menus[menuSelectIdx].GetComponent<exSpriteFont>().botColor = new Color(1, 0, 0, 1);
		}
	}

	// 方法说明：绘制战后招降专用背景和底部对话框。
	// 参数说明：无。
	// 返回说明：无返回值。
	void OnGUI() {
		if (state == StateQuickBattleConfirm) {
			DrawQuickBattlePriorityPanel();
		}

		if (isPostBattleSurrenderStageActive) {
			DrawPostBattleSurrenderBackground();
			DrawPostBattleSurrenderDialogue();
		}
	}

	// 方法说明：绘制快速战斗优先级选择面板，使用大面积触屏按钮避免文字裁切。
	// 参数说明：无。
	// 返回说明：无返回值。
	void DrawQuickBattlePriorityPanel() {
		float scale = Mathf.Min(Screen.width / 640f, Screen.height / 480f);
		float buttonWidth = 118f * scale;
		float buttonHeight = 42f * scale;
		float gap = 10f * scale;
		float padding = 12f * scale;
		float titleHeight = 26f * scale;
		float panelWidth = buttonWidth * 3f + gap * 2f + padding * 2f;
		float panelHeight = titleHeight + buttonHeight + padding * 3f;
		Rect panelRect = new Rect((Screen.width - panelWidth) / 2f,
		                          Screen.height * 0.52f,
		                          panelWidth,
		                          panelHeight);
		Rect titleRect = new Rect(panelRect.x + padding,
		                          panelRect.y + padding,
		                          panelRect.width - padding * 2f,
		                          titleHeight);
		Rect highRect = new Rect(panelRect.x + padding,
		                         titleRect.yMax + padding,
		                         buttonWidth,
		                         buttonHeight);
		Rect lowRect = new Rect(highRect.xMax + gap,
		                        highRect.y,
		                        buttonWidth,
		                        buttonHeight);
		Rect noNeedRect = new Rect(lowRect.xMax + gap,
		                           highRect.y,
		                           buttonWidth,
		                           buttonHeight);

		Color color = GUI.color;
		GUI.color = new Color(0f, 0f, 0f, 0.72f);
		GUI.Box(panelRect, "", GetQuickBattlePanelStyle(scale));
		GUI.color = new Color(1f, 0.86f, 0.48f, 1f);
		GUI.Label(titleRect, ZhongWen.Instance.quickBattleConfirm, GetQuickBattleQuestionStyle(scale));
		GUI.color = Color.white;

		if (GUI.Button(highRect, ZhongWen.Instance.quickBattleHighStrength, GetQuickBattleButtonStyle(scale))) {
			OnQuickBattleHighStrengthButton();
		}

		if (GUI.Button(lowRect, ZhongWen.Instance.quickBattleLowStrength, GetQuickBattleButtonStyle(scale))) {
			OnQuickBattleLowStrengthButton();
		}

		if (GUI.Button(noNeedRect, ZhongWen.Instance.quickBattleNoNeed, GetQuickBattleCancelButtonStyle(scale))) {
			OnQuickBattleNoNeedButton();
		}

		GUI.color = color;
	}

	// 方法说明：取得快速战斗面板背景样式。
	// 参数说明：scale 为屏幕缩放系数。
	// 返回说明：返回当前帧使用的面板样式。
	GUIStyle GetQuickBattlePanelStyle(float scale) {
		if (quickBattlePanelStyle == null) {
			quickBattlePanelStyle = new GUIStyle(GUI.skin.box);
		}

		return quickBattlePanelStyle;
	}

	// 方法说明：取得快速战斗问题文本样式。
	// 参数说明：scale 为屏幕缩放系数。
	// 返回说明：返回当前帧使用的问题文本样式。
	GUIStyle GetQuickBattleQuestionStyle(float scale) {
		if (quickBattleQuestionStyle == null) {
			quickBattleQuestionStyle = new GUIStyle(GUI.skin.label);
			quickBattleQuestionStyle.alignment = TextAnchor.MiddleCenter;
			quickBattleQuestionStyle.fontStyle = FontStyle.Bold;
			quickBattleQuestionStyle.wordWrap = false;
			quickBattleQuestionStyle.clipping = TextClipping.Overflow;
		}

		quickBattleQuestionStyle.fontSize = Mathf.RoundToInt(18f * scale);
		return quickBattleQuestionStyle;
	}

	// 方法说明：取得快速战斗优先级按钮样式。
	// 参数说明：scale 为屏幕缩放系数。
	// 返回说明：返回当前帧使用的优先级按钮样式。
	GUIStyle GetQuickBattleButtonStyle(float scale) {
		if (quickBattleButtonStyle == null) {
			quickBattleButtonStyle = new GUIStyle(GUI.skin.button);
			quickBattleButtonStyle.alignment = TextAnchor.MiddleCenter;
			quickBattleButtonStyle.fontStyle = FontStyle.Bold;
			quickBattleButtonStyle.wordWrap = false;
			quickBattleButtonStyle.clipping = TextClipping.Overflow;
		}

		quickBattleButtonStyle.fontSize = Mathf.RoundToInt(18f * scale);
		return quickBattleButtonStyle;
	}

	// 方法说明：取得快速战斗取消按钮样式。
	// 参数说明：scale 为屏幕缩放系数。
	// 返回说明：返回当前帧使用的取消按钮样式。
	GUIStyle GetQuickBattleCancelButtonStyle(float scale) {
		if (quickBattleCancelButtonStyle == null) {
			quickBattleCancelButtonStyle = new GUIStyle(GetQuickBattleButtonStyle(scale));
		}

		quickBattleCancelButtonStyle.fontSize = Mathf.RoundToInt(18f * scale);
		return quickBattleCancelButtonStyle;
	}
	
	// 方法说明：初始化战斗选将场景，并在玩家战斗开始时弹出快速战斗确认。
	// 参数说明：无。
	// 返回说明：无返回值。
	void InitScene() {
		
		bool shouldShowQuickBattleConfirm = isWarBegin;
		InitData();
		InitTopBarInformation();
		if (shouldShowQuickBattleConfirm && IsPlayerBattle()) {
			ShowQuickBattleConfirm();
		}
		//InitGeneralInfo();
	}
	
	void InitMenuAction() {
		
		menus[0].SetButtonClickHandler(OnToWar);
		menus[1].SetButtonClickHandler(OnSelectFormation);
		menus[2].SetButtonClickHandler(OnGeneralPosition);
		menus[3].SetButtonClickHandler(OnGeneralInformation);
		menus[4].SetButtonClickHandler(OnEscape);
	}
	
	void InitTopBarInformation() {
		
		int sNum1 = 0;
		for (int i=0; i<leftGenerals.Count; i++) {
			sNum1 += Informations.Instance.GetGeneralInfo(leftGenerals[i]).soldierCur;
			sNum1 += Informations.Instance.GetGeneralInfo(leftGenerals[i]).knightCur;
		}
		
		int sNum2 = 0;
		for (int i=0; i<rightGenerals.Count; i++) {
			sNum2 += Informations.Instance.GetGeneralInfo(rightGenerals[i]).soldierCur;
			sNum2 += Informations.Instance.GetGeneralInfo(rightGenerals[i]).knightCur;
		}
		
		leftBar.SetInformation(leftKing, leftGenerals.Count, sNum1);
		rightBar.SetInformation(rightKing, rightGenerals.Count, sNum2);
	}
	
	void InitGeneralInfo() {
		
		leftGeneralInfo.SetGeneralInformation(leftGenerals[leftSelectIdx], leftDefense, 0);
		rightGeneralInfo.SetGeneralInformation(rightGenerals[rightSelectIdx], rightDefense, gPos[rightSelectIdx]);
	}

	// 方法说明：判断当前战斗是否有玩家势力参与。
	// 参数说明：无。
	// 返回说明：玩家在左右任意一方时返回 true，否则返回 false。
	bool IsPlayerBattle() {
		return leftKing == Controller.kingIndex || rightKing == Controller.kingIndex;
	}

	// 方法说明：显示快速战斗确认对话，并暂时关闭手动战斗菜单。
	// 参数说明：无。
	// 返回说明：无返回值。
	void ShowQuickBattleConfirm() {
		state = StateQuickBattleConfirm;
		OnSubMenu();
		dialogCtrl.SetDialogue(Informations.Instance.GetKingInfo(Controller.kingIndex).generalIdx,
		                       ZhongWen.Instance.quickBattleConfirm,
		                       MenuDisplayAnim.AnimType.InsertFromBottom);
	}

	// 方法说明：处理快速战斗确认输入，设备返回键等同于选择“不需要”。
	// 参数说明：无。
	// 返回说明：无返回值。
	void OnQuickBattleConfirmModeHandler() {
		if (Misc.GetBack()) {
			OnQuickBattleNoNeedButton();
		}
	}

	// 方法说明：点击“高武力”后按武力高优先执行快速战斗并进入战后结算。
	// 参数说明：无。
	// 返回说明：无返回值。
	void OnQuickBattleHighStrengthButton() {
		if (state != StateQuickBattleConfirm) {
			return;
		}

		StartQuickBattleWithPriority(QuickBattlePriority.HighStrength);
	}

	// 方法说明：点击“低武力”后按武力低优先执行快速战斗并进入战后结算。
	// 参数说明：无。
	// 返回说明：无返回值。
	void OnQuickBattleLowStrengthButton() {
		if (state != StateQuickBattleConfirm) {
			return;
		}

		StartQuickBattleWithPriority(QuickBattlePriority.LowStrength);
	}

	// 方法说明：按指定武力优先级执行快速战斗并进入战后结算。
	// 参数说明：priority 为快速战斗出战武将选择优先级。
	// 返回说明：无返回值。
	void StartQuickBattleWithPriority(QuickBattlePriority priority) {
		Input.ResetInputAxes();
		quickBattlePriority = priority;
		ExecuteQuickBattle();
		dialogCtrl.SetDialogueOut(MenuDisplayAnim.AnimType.OutToBottom);
		state = 0;
		Invoke("WarOverResult", 0.5f);
	}

	// 方法说明：点击“不需要”后关闭快速战斗确认并恢复手动战斗菜单。
	// 参数说明：无。
	// 返回说明：无返回值。
	void OnQuickBattleNoNeedButton() {
		if (state != StateQuickBattleConfirm) {
			return;
		}

		Input.ResetInputAxes();
		dialogCtrl.SetDialogueOut(MenuDisplayAnim.AnimType.OutToBottom);
		state = 0;
		Invoke("OnReturnMain", 0.5f);
	}

	// 方法说明：执行整场快速战斗自动结算。
	// 参数说明：无。
	// 返回说明：无返回值。
	void ExecuteQuickBattle() {
		// 1. 初始化快速战斗依赖，确保技能表可用且本场不是撤退战。
		MagicManager.Instance.LoadConfig();
		WarSceneController.isEscape = false;
		isPrisoned = false;
		isWarOver = false;

		// 2. 循环按玩家选择的武力优先级派出双方当前未败武将。
		while (!isWarOver) {
			leftSelectIdx = GetQuickBattleGeneralIndex(leftGenerals, leftFailFlag, quickBattlePriority);
			rightSelectIdx = GetQuickBattleGeneralIndex(rightGenerals, rightFailFlag, quickBattlePriority);

			if (leftSelectIdx == -1 || rightSelectIdx == -1) {
				Debug.LogError("Quick battle cannot find available general!");
				CheckWarOver();
				break;
			}

			GeneralInfo left = Informations.Instance.GetGeneralInfo(leftGenerals[leftSelectIdx]);
			GeneralInfo right = Informations.Instance.GetGeneralInfo(rightGenerals[rightSelectIdx]);

			// 3. 按真实入场规则恢复体力技力，并自动释放可用技能。
			RecoverGeneralForQuickBattle(left);
			RecoverGeneralForQuickBattle(right);
			int leftMagicPower = UseQuickBattleMagic(left);
			int rightMagicPower = UseQuickBattleMagic(right);
			int leftPower = GetQuickBattlePower(left, leftDefense) + leftMagicPower;
			int rightPower = GetQuickBattlePower(right, rightDefense) + rightMagicPower;

			// 4. 按本轮战力结算败退，再检查整场是否结束。
			if (leftPower > rightPower) {
				rightFailFlag[rightSelectIdx] = true;
				ApplyQuickBattleDuelResult(left, right, rightPower);
			} else {
				leftFailFlag[leftSelectIdx] = true;
				ApplyQuickBattleDuelResult(right, left, leftPower);
			}

			CheckWarOver();
		}
	}

	// 方法说明：按快速战斗优先级选择当前未败武将的列表索引。
	// 参数说明：generals 为武将编号列表，failFlags 为败退标记数组，priority 为武力高或低优先。
	// 返回说明：找到返回列表索引，找不到返回 -1。
	int GetQuickBattleGeneralIndex(List<int> generals, bool[] failFlags, QuickBattlePriority priority) {
		int selectedIndex = -1;
		int selectedStrength = priority == QuickBattlePriority.LowStrength ? int.MaxValue : int.MinValue;

		for (int i=0; i<generals.Count; i++) {
			if (failFlags[i]) {
				continue;
			}

			GeneralInfo gInfo = Informations.Instance.GetGeneralInfo(generals[i]);
			if (IsBetterQuickBattleGeneral(gInfo.strength, selectedStrength, priority)) {
				selectedIndex = i;
				selectedStrength = gInfo.strength;
			}
		}

		return selectedIndex;
	}

	// 方法说明：判断当前武力值是否比已选武力值更符合快速战斗优先级。
	// 参数说明：strength 为当前武将武力，selectedStrength 为已选武力，priority 为武力高或低优先。
	// 返回说明：当前武将更符合优先级时返回 true，否则返回 false。
	bool IsBetterQuickBattleGeneral(int strength, int selectedStrength, QuickBattlePriority priority) {
		if (priority == QuickBattlePriority.HighStrength) {
			return strength > selectedStrength;
		}

		return strength < selectedStrength;
	}

	// 方法说明：按真实战斗入场规则恢复武将体力和技力。
	// 参数说明：gInfo 为出战武将数据。
	// 返回说明：无返回值。
	void RecoverGeneralForQuickBattle(GeneralInfo gInfo) {
		gInfo.healthCur += 10;
		gInfo.healthCur = Mathf.Clamp(gInfo.healthCur, 0, gInfo.healthMax);
		gInfo.manaCur += 5;
		gInfo.manaCur = Mathf.Clamp(gInfo.manaCur, 0, gInfo.manaMax);
	}

	// 方法说明：计算快速战斗中的基础战力。
	// 参数说明：gInfo 为武将数据，defense 为城市防御修正。
	// 返回说明：基础战力数值。
	int GetQuickBattlePower(GeneralInfo gInfo, int defense) {
		return gInfo.level * 5
			+ gInfo.knightCur * 6
			+ gInfo.soldierCur * 3
			+ gInfo.strength * 2
			+ gInfo.healthCur
			+ gInfo.manaCur / 2
			+ defense / 10
			+ Random.Range(0, 20);
	}

	// 方法说明：自动释放快速战斗可用技能，并返回本轮战力加成。
	// 参数说明：gInfo 为出战武将数据。
	// 返回说明：技能带来的本轮战力加成；治疗技能返回 0 但会恢复体力。
	int UseQuickBattleMagic(GeneralInfo gInfo) {
		MagicDataInfo info = GetQuickBattleMagicInfo(gInfo);
		if (info == null) {
			return 0;
		}

		gInfo.manaCur -= info.MP;
		if (info.ATTRIB == "补血") {
			gInfo.healthCur += info.ATTACK;
			gInfo.healthCur = Mathf.Clamp(gInfo.healthCur, 0, gInfo.healthMax);
			return 0;
		}

		return info.ATTACK;
	}

	// 方法说明：获取快速战斗中可释放的优先技能。
	// 参数说明：gInfo 为出战武将数据。
	// 返回说明：可释放技能数据；没有可用技能返回 null。
	MagicDataInfo GetQuickBattleMagicInfo(GeneralInfo gInfo) {
		for (int i=3; i>=0; i--) {
			if (gInfo.magic[i] == -1) {
				continue;
			}

			MagicDataInfo info = MagicManager.Instance.GetMagicDataInfo(gInfo.magic[i]);
			if (info != null && gInfo.manaCur >= info.MP) {
				return info;
			}
		}

		return null;
	}

	// 方法说明：套用快速战斗单轮胜负后的武将消耗、经验和俘虏结果。
	// 参数说明：winner 为本轮胜者，loser 为本轮败者，damager 为败者战力值。
	// 返回说明：无返回值。
	void ApplyQuickBattleDuelResult(GeneralInfo winner, GeneralInfo loser, int damager) {
		loser.knightCur = 0;
		loser.soldierCur = 0;
		loser.healthCur = 2;
		loser.manaCur = 2;

		int horse = 0;
		if (loser.equipment == 27 || loser.equipment == 28
		    || loser.equipment == 29 || loser.equipment == 30) {
			horse = 1;
		}

		if (loser == Informations.Instance.GetGeneralInfo(Informations.Instance.GetKingInfo(loser.king).generalIdx)) {
			if (Informations.Instance.GetKingInfo(loser.king).cities.Count > 0) {
				horse += 3;
			}
		}

		if (Random.Range(0, 100) > 50 + horse * 25 - loser.escape * 25) {
			loser.prisonerIdx = winner.king;
			isPrisoned = true;
			loser.escape = 0;
		} else {
			isPrisoned = false;
			loser.escape++;
		}

		winner.experience += (Misc.GetLevelExperience(winner.level + 1) - Misc.GetLevelExperience(winner.level)) / 2;
		CheckLevelUp(winner);

		damager -= winner.strength;
		if (damager <= 0) {
			return;
		}

		if (damager > winner.knightCur * 6) {
			damager -= winner.knightCur * 6;
			winner.knightCur = 0;
		} else {
			winner.knightCur -= damager / 6;
			return;
		}

		if (damager > winner.soldierCur * 3) {
			damager -= winner.soldierCur * 3;
			winner.soldierCur = 0;
		} else {
			winner.soldierCur -= damager / 3;
			return;
		}

		if (damager > winner.manaCur / 2) {
			damager -= winner.manaCur / 2;
			winner.manaCur = 0;
		} else {
			winner.manaCur -= damager * 2;
			return;
		}

		if (winner.healthCur - damager < 15) {
			winner.healthCur = 15;
		} else {
			winner.healthCur = winner.healthCur - damager;
		}
	}
	
	void InitData() {
		
		if (isWarBegin) {
			isWarBegin = false;
			
			switch(mode) {
			case 0:
			{
				leftGenerals = new List<int>();
				for (int i=0; i<enemy.generals.Count; i++) {
					int gIdx = enemy.generals[i];
					if (gIdx == enemy.commander) {
						leftGenerals.Insert(0, gIdx);
					} else {
						leftGenerals.Add(gIdx);
					}
				}
				
				rightGenerals = new List<int>();
				for (int i=0; i<mine.generals.Count; i++) {
					int gIdx = mine.generals[i];
					if (gIdx == mine.commander) {
						rightGenerals.Insert(0, gIdx);
					} else {
						rightGenerals.Add(gIdx);
					}
				}
				
				leftKing = enemy.king;
				rightKing = mine.king;
				leftDefense = 0;
				rightDefense = 0;
				mineCommander = mine.commander;
			}
				break;
			case 1:
			{
				CityInfo cInfo = Informations.Instance.GetCityInfo(cityAttacked);
				
				leftGenerals = new List<int>();
				for (int i=0; i<enemy.generals.Count; i++) {
					int gIdx = enemy.generals[i];
					if (gIdx == enemy.commander) {
						leftGenerals.Insert(0, gIdx);
					} else {
						leftGenerals.Add(gIdx);
					}
				}
				
				rightGenerals = new List<int>();
				for (int i=0; i<cInfo.generals.Count; i++) {
					int gIdx = cInfo.generals[i];
					if (gIdx == cInfo.prefect) {
						rightGenerals.Insert(0, gIdx);
					} else {
						rightGenerals.Add(gIdx);
					}
				}
				
				leftKing = enemy.king;
				rightKing = cInfo.king;
				leftDefense = 0;
				rightDefense = cInfo.defense;
				mineCommander = cInfo.prefect;
			}
				break;
			case 2:
			{
				CityInfo cInfo = Informations.Instance.GetCityInfo(cityAttacked);
				
				leftGenerals = new List<int>();
				for (int i=0; i<cInfo.generals.Count; i++) {
					int gIdx = cInfo.generals[i];
					if (gIdx == cInfo.prefect) {
						leftGenerals.Insert(0, gIdx);
					} else {
						leftGenerals.Add(gIdx);
					}
				}
				
				rightGenerals = new List<int>();
				for (int i=0; i<mine.generals.Count; i++) {
					int gIdx = mine.generals[i];
					if (gIdx == mine.commander) {
						rightGenerals.Insert(0, gIdx);
					} else {
						rightGenerals.Add(gIdx);
					}
				}
				
				leftKing = cInfo.king;
				rightKing = mine.king;
				leftDefense = cInfo.defense;
				rightDefense = 0;
				mineCommander = mine.commander;
			}
				break;
			}
			
			leftSelectIdx = 0;
			rightSelectIdx = 0;
			leftFailFlag = new bool[leftGenerals.Count];
			rightFailFlag = new bool[rightGenerals.Count];
			gPos = new int[rightGenerals.Count];
		} else {
			
			GeneralInfo g1 = Informations.Instance.GetGeneralInfo(leftGenerals[leftSelectIdx]);
			GeneralInfo g2 = Informations.Instance.GetGeneralInfo(rightGenerals[rightSelectIdx]);
			
			if (warResult == 0) {
				
				leftFailFlag[leftSelectIdx] = true;
				
				WarResult(g2, g1, leftExperience);
				if (CheckWarOver()) {
					OnWarOver();
				} else {
					SetWarResultDialogue();
				}

				OnSubMenu();
			} else if (warResult == 1) {
				
				rightFailFlag[rightSelectIdx] = true;
				
				WarResult(g1, g2, rightExperience);
				if (CheckWarOver()) {
					OnWarOver();
				}
				 else {
					SetWarResultDialogue();
				}

				OnSubMenu();
			} else if (warResult == 2) {
				/*
				leftFailFlag[leftSelectIdx] = true;
				rightFailFlag[rightSelectIdx] = true;
				
				WarResult(g1, g2, rightExperience);
				WarResult(g2, g1, leftExperience);
				if (CheckWarOver()) {
					OnWarOver();
				}
				*/
			}
		}
		
		if (!isWarOver) {
			do {
				leftSelectIdx = Random.Range(0, leftGenerals.Count);
			} while(leftFailFlag[leftSelectIdx]);
		}
		
		leftGeneralInfo.SetGeneralInformation(leftGenerals[leftSelectIdx], leftDefense, 0);
		OnRightGeneralSelected(rightSelectIdx);
		
		generalListCtrl.SetGeneralsList(leftGenerals, rightGenerals, leftFailFlag, rightFailFlag, leftSelectIdx, rightSelectIdx, isWarOver);
	}
	
	void WarResult(GeneralInfo winner, GeneralInfo loser, int experience) {
		
		//winner.experience += experience;
		winner.experience += (Misc.GetLevelExperience(winner.level + 1) - Misc.GetLevelExperience(winner.level)) / 2;
		CheckLevelUp(winner);
		
		int hourse = 0;
		if (loser.equipment == 27 || loser.equipment == 28
		    || loser.equipment == 29 || loser.equipment == 30) {
			hourse = 1;
		}
		
		if (loser == Informations.Instance.GetGeneralInfo(Informations.Instance.GetKingInfo(loser.king).generalIdx)) {
			if (Informations.Instance.GetKingInfo(loser.king).cities.Count > 0) {
				hourse += 3;
			}
		}
		
		if (!WarSceneController.isEscape && Random.Range(0, 100) > 50 + hourse * 25 - loser.escape * 25) {
			loser.prisonerIdx = winner.king;
			isPrisoned = true;
			loser.escape = 0;
		} else {
			isPrisoned = false;
			loser.escape++;
		}
	}
	
	void CheckLevelUp(GeneralInfo gInfo) {
		
		Misc.CheckIsLevelUp(gInfo);
	}
	
	bool CheckWarOver() {
		
		isWarOver = true;
		bool flag = true;
		for (int i=0; i<leftFailFlag.Length; i++) {
			if (leftFailFlag[i] == false) {
				flag = false;
			}
		}
		
		if (flag) {
			warResult = 0;
			return true;
		}
		
		flag = true;
		for (int i=0; i<rightFailFlag.Length; i++) {
			if (rightFailFlag[i] == false) {
				flag = false;
			}
		}
		
		if (flag) {
			warResult = 1;
			return true;
		}
		
		isWarOver = false;
		
		return false;
	}
	
	void SetWarResultDialogue() {
		
		if (warResult == 0) {
			
			state = 1;
			OnSubMenu();
			
			string msg = "";
			if (isPrisoned) {
				msg = ZhongWen.Instance.wojundasheng + ZhongWen.Instance.GetGeneralName(leftGenerals[leftSelectIdx]) + ZhongWen.Instance.tanhao;
			} else {
				msg = ZhongWen.Instance.wojunshengli;
			}
			dialogCtrl.SetDialogue(mineCommander, msg, MenuDisplayAnim.AnimType.InsertFromBottom);
			
		} else if (warResult == 1) {
			
			state = 1;
			OnSubMenu();
			
			string msg = "";
			if (isPrisoned) {
				msg = ZhongWen.Instance.wozhanbai;
			} else {
				msg = ZhongWen.Instance.henyihan;
			}
			dialogCtrl.SetDialogue(rightGenerals[rightSelectIdx], msg, MenuDisplayAnim.AnimType.InsertFromBottom);
		}
	}
	
	void OnWarOver() {
		
		state = 3;
		string msg = "";
		
		if (warResult == 0) {
			msg = ZhongWen.Instance.wojundahuoquansheng;
		} else {
			msg = ZhongWen.Instance.wojunzhanbai;
		}
		
		dialogCtrl.SetDialogue(mineCommander, msg, MenuDisplayAnim.AnimType.InsertFromBottom);
	}
	
	// 方法说明：结算战斗结果，并在回到主地图前触发玩家战后招降流程。
	// 参数说明：无。
	// 返回说明：无返回值。
	void WarOverResult() {
		
		ResetPostBattleSurrenderData();
		
		switch (mode) {
		case 0:
		{
			if (warResult == 0) {
				PreparePostBattleSurrenderFromArmyWinner(mine, enemy);
				WarOverResultArmyToArmy(mine, enemy);
			} else if (warResult == 1) {
				PreparePostBattleSurrenderFromArmyWinner(enemy, mine);
				WarOverResultArmyToArmy(enemy, mine);
			}
		}
			break;
		case 1:
		{
			if (warResult == 0) {
				PreparePostBattleSurrenderFromCityBattle(enemy, cityAttacked, false);
				WarOverResultArmyToCity(enemy, cityAttacked, false);
			} else if (warResult == 1) {
				PreparePostBattleSurrenderFromCityBattle(enemy, cityAttacked, true);
				WarOverResultArmyToCity(enemy, cityAttacked, true);
			}
		}
			break;
		case 2:
		{
			if (warResult == 0) {
				PreparePostBattleSurrenderFromCityBattle(mine, cityAttacked, true);
				WarOverResultArmyToCity(mine, cityAttacked, true);
			} else if (warResult == 1) {
				PreparePostBattleSurrenderFromCityBattle(mine, cityAttacked, false);
				WarOverResultArmyToCity(mine, cityAttacked, false);
			}
		}
			break;
		}
		
		BeginPostBattleSurrender();
	}

	// 方法说明：清空上一场战斗遗留的战后招降数据。
	// 参数说明：无。
	// 返回说明：无返回值。
	void ResetPostBattleSurrenderData() {
		postBattleSurrenderPrisons.Clear();
		postBattleSurrenderIdx = 0;
		postBattleSurrenderDialogueIdx = 0;
		postBattleSurrenderCityIdx = -1;
		postBattleSurrenderCityInfo = null;
		postBattleSurrenderArmyInfo = null;
	}

	// 方法说明：从部队对部队的战后结果中收集玩家本场新增俘虏。
	// 参数说明：winner 为胜利部队，loser 为失败部队。
	// 返回说明：无返回值。
	void PreparePostBattleSurrenderFromArmyWinner(ArmyInfo winner, ArmyInfo loser) {
		if (winner.king != Controller.kingIndex) {
			return;
		}

		postBattleSurrenderArmyInfo = winner;
		postBattleSurrenderCityInfo = null;
		postBattleSurrenderCityIdx = -1;

		int capturedGeneralCount = 0;
		for (int i=0; i<loser.generals.Count; i++) {
			int gIdx = loser.generals[i];
			if (Informations.Instance.GetGeneralInfo(gIdx).prisonerIdx == winner.king) {
				AddPostBattleSurrenderPrison(gIdx);
				capturedGeneralCount++;
			}
		}

		if (capturedGeneralCount == loser.generals.Count) {
			AddPostBattleSurrenderPrisons(loser.prisons);
		}
	}

	// 方法说明：从部队攻城或守城战后结果中收集玩家本场新增俘虏。
	// 参数说明：armyInfo 为参战部队，cIdx 为参战城市编号，isWin 表示部队是否攻城成功。
	// 返回说明：无返回值。
	void PreparePostBattleSurrenderFromCityBattle(ArmyInfo armyInfo, int cIdx, bool isWin) {
		CityInfo cInfo = Informations.Instance.GetCityInfo(cIdx);

		if (isWin) {
			if (armyInfo.king != Controller.kingIndex) {
				return;
			}

			postBattleSurrenderCityIdx = cIdx;
			postBattleSurrenderCityInfo = cInfo;
			postBattleSurrenderArmyInfo = null;

			for (int i=0; i<cInfo.generals.Count; i++) {
				int gIdx = cInfo.generals[i];
				if (Informations.Instance.GetGeneralInfo(gIdx).prisonerIdx == armyInfo.king) {
					AddPostBattleSurrenderPrison(gIdx);
				}
			}

			AddPostBattleSurrenderPrisons(cInfo.prisons);
			return;
		}

		if (cInfo.king != Controller.kingIndex) {
			return;
		}

		postBattleSurrenderCityIdx = cIdx;
		postBattleSurrenderCityInfo = cInfo;
		postBattleSurrenderArmyInfo = null;

		int capturedGeneralCount = 0;
		for (int i=0; i<armyInfo.generals.Count; i++) {
			int gIdx = armyInfo.generals[i];
			if (Informations.Instance.GetGeneralInfo(gIdx).prisonerIdx == cInfo.king) {
				AddPostBattleSurrenderPrison(gIdx);
				capturedGeneralCount++;
			}
		}

		if (capturedGeneralCount == armyInfo.generals.Count) {
			AddPostBattleSurrenderPrisons(armyInfo.prisons);
		}
	}

	// 方法说明：追加一组待战后招降的俘虏编号。
	// 参数说明：prisons 为待追加的俘虏编号列表。
	// 返回说明：无返回值。
	void AddPostBattleSurrenderPrisons(List<int> prisons) {
		for (int i=0; i<prisons.Count; i++) {
			AddPostBattleSurrenderPrison(prisons[i]);
		}
	}

	// 方法说明：追加单个待战后招降的俘虏编号，并避免重复。
	// 参数说明：gIdx 为武将编号。
	// 返回说明：无返回值。
	void AddPostBattleSurrenderPrison(int gIdx) {
		if (!postBattleSurrenderPrisons.Contains(gIdx)) {
			postBattleSurrenderPrisons.Add(gIdx);
		}
	}

	// 方法说明：开始战后招降；没有可招降俘虏时直接返回主地图。
	// 参数说明：无。
	// 返回说明：无返回值。
	void BeginPostBattleSurrender() {
		if (postBattleSurrenderPrisons.Count == 0) {
			LoadStrategyAfterWarOver();
			return;
		}

		if (postBattleSurrenderCityInfo == null && postBattleSurrenderArmyInfo == null) {
			Debug.LogError("Post battle surrender target cannot found!");
			LoadStrategyAfterWarOver();
			return;
		}

		postBattleSurrenderIdx = 0;
		ShowPostBattleSurrenderStage();
		ShowPostBattleSurrenderAsk();
	}

	// 方法说明：显示战后招降专用背景，并隐藏战斗选将界面元素。
	// 参数说明：无。
	// 返回说明：无返回值。
	void ShowPostBattleSurrenderStage() {
		SetPostBattleSurrenderControlsVisible(false);
		EnsurePostBattleSurrenderBackground();
		isPostBattleSurrenderStageActive = true;
		postBattleSurrenderBackgroundAlpha = 0f;
		if (dialogCtrl != null) {
			dialogCtrl.gameObject.SetActive(false);
		}
	}

	// 方法说明：创建并显示战后招降背景图。
	// 参数说明：无。
	// 返回说明：创建或显示成功返回 true，资源缺失返回 false。
	bool EnsurePostBattleSurrenderBackground() {
		if (postBattleSurrenderBackgroundTexture != null) {
			return true;
		}

		postBattleSurrenderBackgroundTexture = Resources.Load<Texture2D>(PostBattleSurrenderBackgroundResource);
		if (postBattleSurrenderBackgroundTexture == null) {
			Debug.LogError("Post battle surrender background cannot found!");
			return false;
		}

		return true;
	}

	// 方法说明：更新战后招降背景淡入动画。
	// 参数说明：无。
	// 返回说明：无返回值。
	void UpdatePostBattleSurrenderStageAnimation() {
		if (!isPostBattleSurrenderStageActive) {
			return;
		}

		postBattleSurrenderBackgroundAlpha = Mathf.MoveTowards(postBattleSurrenderBackgroundAlpha, 1f, Time.unscaledDeltaTime * 2.5f);
	}

	// 方法说明：绘制战后招降全屏背景图。
	// 参数说明：无。
	// 返回说明：无返回值。
	void DrawPostBattleSurrenderBackground() {
		Color color = GUI.color;
		GUI.color = new Color(1f, 1f, 1f, postBattleSurrenderBackgroundAlpha);

		if (postBattleSurrenderBackgroundTexture != null) {
			GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height),
			                postBattleSurrenderBackgroundTexture,
			                ScaleMode.ScaleAndCrop);
		} else {
			GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height),
			                Texture2D.blackTexture,
			                ScaleMode.StretchToFill);
		}

		GUI.color = color;
	}

	// 方法说明：绘制战后招降底部文字框。
	// 参数说明：无。
	// 返回说明：无返回值。
	void DrawPostBattleSurrenderDialogue() {
		float scale = Mathf.Min(Screen.width / 640f, Screen.height / 480f);
		Rect boxRect = new Rect(Screen.width * 0.08f,
		                        Screen.height - 132f * scale,
		                        Screen.width * 0.84f,
		                        108f * scale);
		Rect titleRect = new Rect(boxRect.x + 18f * scale,
		                          boxRect.y + 10f * scale,
		                          boxRect.width - 150f * scale,
		                          28f * scale);
		Rect allButtonRect = new Rect(boxRect.xMax - 126f * scale,
		                              boxRect.y + 10f * scale,
		                              108f * scale,
		                              30f * scale);
		Rect textRect = new Rect(boxRect.x + 18f * scale,
		                         boxRect.y + 40f * scale,
		                         boxRect.width - 36f * scale,
		                         44f * scale);
		Rect hintRect = new Rect(boxRect.x + 18f * scale,
		                         boxRect.y + 80f * scale,
		                         boxRect.width - 36f * scale,
		                         22f * scale);

		Color color = GUI.color;
		GUI.color = new Color(0f, 0f, 0f, 0.68f * postBattleSurrenderBackgroundAlpha);
		GUI.Box(boxRect, "");
		GUI.color = new Color(1f, 0.86f, 0.48f, postBattleSurrenderBackgroundAlpha);
		GUI.Label(titleRect, ZhongWen.Instance.postBattleSurrenderTitle, GetPostBattleSurrenderTitleStyle(scale));
		DrawPostBattleSurrenderAllButton(allButtonRect, scale);
		GUI.color = new Color(1f, 1f, 1f, postBattleSurrenderBackgroundAlpha);
		GUI.Label(textRect, postBattleSurrenderText, GetPostBattleSurrenderTextStyle(scale));
		GUI.color = new Color(0.9f, 0.9f, 0.9f, postBattleSurrenderBackgroundAlpha);
		GUI.Label(hintRect, ZhongWen.Instance.postBattleSurrenderContinue, GetPostBattleSurrenderHintStyle(scale));
		GUI.color = color;
	}

	// 方法说明：绘制战后招降的一键招降按钮，并在点击时批量处理剩余俘虏。
	// 参数说明：buttonRect 为按钮区域，scale 为屏幕缩放系数。
	// 返回说明：无返回值。
	void DrawPostBattleSurrenderAllButton(Rect buttonRect, float scale) {
		if (state != StatePostBattleSurrenderAsk) {
			return;
		}

		bool enabled = GUI.enabled;
		GUI.enabled = postBattleSurrenderBackgroundAlpha >= 0.95f;
		GUI.color = new Color(1f, 1f, 1f, postBattleSurrenderBackgroundAlpha);
		if (GUI.Button(buttonRect, ZhongWen.Instance.postBattleSurrenderAll, GetPostBattleSurrenderButtonStyle(scale))) {
			isPostBattleSurrenderGuiButtonClicked = true;
			ApplyPostBattleSurrenderAllResult();
		}

		GUI.enabled = enabled;
	}

	// 方法说明：取得战后招降标题样式。
	// 参数说明：scale 为屏幕缩放系数。
	// 返回说明：返回当前帧使用的标题样式。
	GUIStyle GetPostBattleSurrenderTitleStyle(float scale) {
		if (postBattleSurrenderTitleStyle == null) {
			postBattleSurrenderTitleStyle = new GUIStyle(GUI.skin.label);
			postBattleSurrenderTitleStyle.alignment = TextAnchor.MiddleLeft;
			postBattleSurrenderTitleStyle.fontStyle = FontStyle.Bold;
		}

		postBattleSurrenderTitleStyle.fontSize = Mathf.RoundToInt(22f * scale);
		return postBattleSurrenderTitleStyle;
	}

	// 方法说明：取得战后招降正文样式。
	// 参数说明：scale 为屏幕缩放系数。
	// 返回说明：返回当前帧使用的正文样式。
	GUIStyle GetPostBattleSurrenderTextStyle(float scale) {
		if (postBattleSurrenderTextStyle == null) {
			postBattleSurrenderTextStyle = new GUIStyle(GUI.skin.label);
			postBattleSurrenderTextStyle.alignment = TextAnchor.MiddleLeft;
			postBattleSurrenderTextStyle.wordWrap = true;
		}

		postBattleSurrenderTextStyle.fontSize = Mathf.RoundToInt(20f * scale);
		return postBattleSurrenderTextStyle;
	}

	// 方法说明：取得战后招降继续提示样式。
	// 参数说明：scale 为屏幕缩放系数。
	// 返回说明：返回当前帧使用的继续提示样式。
	GUIStyle GetPostBattleSurrenderHintStyle(float scale) {
		if (postBattleSurrenderHintStyle == null) {
			postBattleSurrenderHintStyle = new GUIStyle(GUI.skin.label);
			postBattleSurrenderHintStyle.alignment = TextAnchor.MiddleRight;
		}

		postBattleSurrenderHintStyle.fontSize = Mathf.RoundToInt(14f * scale);
		return postBattleSurrenderHintStyle;
	}

	// 方法说明：取得战后招降按钮样式。
	// 参数说明：scale 为屏幕缩放系数。
	// 返回说明：返回当前帧使用的一键招降按钮样式。
	GUIStyle GetPostBattleSurrenderButtonStyle(float scale) {
		if (postBattleSurrenderButtonStyle == null) {
			postBattleSurrenderButtonStyle = new GUIStyle(GUI.skin.button);
			postBattleSurrenderButtonStyle.alignment = TextAnchor.MiddleCenter;
			postBattleSurrenderButtonStyle.fontStyle = FontStyle.Bold;
			postBattleSurrenderButtonStyle.wordWrap = false;
			postBattleSurrenderButtonStyle.clipping = TextClipping.Overflow;
		}

		postBattleSurrenderButtonStyle.fontSize = Mathf.RoundToInt(14f * scale);
		return postBattleSurrenderButtonStyle;
	}

	// 方法说明：设置战后招降期间选将界面元素是否可见。
	// 参数说明：visible 为 true 时显示选将界面元素，为 false 时隐藏。
	// 返回说明：无返回值。
	void SetPostBattleSurrenderControlsVisible(bool visible) {
		if (leftBar != null) leftBar.gameObject.SetActive(visible);
		if (rightBar != null) rightBar.gameObject.SetActive(visible);
		if (leftGeneralInfo != null) leftGeneralInfo.gameObject.SetActive(visible);
		if (rightGeneralInfo != null) rightGeneralInfo.gameObject.SetActive(visible);
		if (generalListCtrl != null) generalListCtrl.gameObject.SetActive(visible);

		for (int i=0; i<menus.Length; i++) {
			if (menus[i] != null) {
				menus[i].gameObject.SetActive(visible);
			}
		}

		if (selectFormation != null) selectFormation.gameObject.SetActive(false);
		if (generalsInfo != null) generalsInfo.gameObject.SetActive(false);
		if (generalPos != null) generalPos.SetActive(false);
		if (retreatConfirm != null) retreatConfirm.SetActive(false);
	}

	// 方法说明：显示当前俘虏的战后招降询问文本。
	// 参数说明：无。
	// 返回说明：无返回值。
	void ShowPostBattleSurrenderAsk() {
		int gIdx = postBattleSurrenderPrisons[postBattleSurrenderIdx];
		GeneralInfo gInfo = Informations.Instance.GetGeneralInfo(gIdx);
		string msg = "";

		if (gInfo.king == Controller.kingIndex) {
			msg = ZhongWen.Instance.GetGeneralName(gIdx) + ZhongWen.Instance.zhaoxiang_guilai;
		} else {
			msg = ZhongWen.Instance.GetGeneralName(gIdx) + ZhongWen.Instance.zhaoxiang_ask;
		}

		state = StatePostBattleSurrenderAsk;
		SetPostBattleSurrenderText(msg);
	}

	// 方法说明：处理战后招降询问后的点击，并显示俘虏答复。
	// 参数说明：无。
	// 返回说明：无返回值。
	void OnPostBattleSurrenderAskModeHandler() {
		if (IsPostBattleSurrenderContinueClicked()) {
			Input.ResetInputAxes();
			ApplyPostBattleSurrenderResult();
			state = StatePostBattleSurrenderPrisonerAnswer;
			SetPostBattleSurrenderText(ZhongWen.Instance.zhaoxiang_wenda[postBattleSurrenderDialogueIdx * 2]);
		}
	}

	// 方法说明：处理俘虏答复后的点击，并显示君主回应。
	// 参数说明：无。
	// 返回说明：无返回值。
	void OnPostBattleSurrenderPrisonerAnswerModeHandler() {
		if (IsPostBattleSurrenderContinueClicked()) {
			Input.ResetInputAxes();
			state = StatePostBattleSurrenderKingAnswer;
			SetPostBattleSurrenderText(ZhongWen.Instance.zhaoxiang_wenda[postBattleSurrenderDialogueIdx * 2 + 1]);
		}
	}

	// 方法说明：处理君主回应后的点击，并继续下一个俘虏或返回主地图。
	// 参数说明：无。
	// 返回说明：无返回值。
	void OnPostBattleSurrenderKingAnswerModeHandler() {
		if (IsPostBattleSurrenderContinueClicked()) {
			Input.ResetInputAxes();
			postBattleSurrenderIdx++;

			if (postBattleSurrenderIdx >= postBattleSurrenderPrisons.Count) {
				HidePostBattleSurrenderStage();
				state = 0;
				Invoke("LoadStrategyAfterWarOver", 0.5f);
				return;
			}

			ShowPostBattleSurrenderAsk();
		}
	}

	// 方法说明：设置战后招降文字框内容。
	// 参数说明：text 为当前阶段需要展示的招降文本。
	// 返回说明：无返回值。
	void SetPostBattleSurrenderText(string text) {
		postBattleSurrenderText = text;
		Input.ResetInputAxes();
	}

	// 方法说明：判断战后招降文字框是否被点击继续。
	// 参数说明：无。
	// 返回说明：背景淡入后点击屏幕返回 true，否则返回 false。
	bool IsPostBattleSurrenderContinueClicked() {
		if (isPostBattleSurrenderGuiButtonClicked) {
			isPostBattleSurrenderGuiButtonClicked = false;
			return false;
		}

		if (postBattleSurrenderBackgroundAlpha < 0.95f) {
			return false;
		}

		return Input.GetMouseButtonUp(0);
	}

	// 方法说明：隐藏战后招降专用背景和文字框。
	// 参数说明：无。
	// 返回说明：无返回值。
	void HidePostBattleSurrenderStage() {
		isPostBattleSurrenderStageActive = false;
		postBattleSurrenderText = "";
	}

	// 方法说明：按年度招降同一套概率结算当前俘虏的战后招降结果。
	// 参数说明：无。
	// 返回说明：无返回值。
	void ApplyPostBattleSurrenderResult() {
		int gIdx = postBattleSurrenderPrisons[postBattleSurrenderIdx];
		int dialogueIdx;
		bool isSuccess = ApplyPostBattleSurrenderResultForGeneral(gIdx, out dialogueIdx);

		postBattleSurrenderDialogueIdx = dialogueIdx;
		if (isSuccess) {
			SoundController.Instance.PlaySound("00045");
		} else {
			SoundController.Instance.PlaySound("00057");
		}
	}

	// 方法说明：批量结算当前尚未处理的战后俘虏，并显示一键招降统计。
	// 参数说明：无。
	// 返回说明：无返回值。
	void ApplyPostBattleSurrenderAllResult() {
		if (state != StatePostBattleSurrenderAsk || postBattleSurrenderBackgroundAlpha < 0.95f) {
			return;
		}

		Input.ResetInputAxes();
		int successCount = 0;
		int returnCount = 0;
		int failCount = 0;

		// 1. 从当前俘虏开始，逐个按原年度招降概率结算剩余名单。
		for (int i=postBattleSurrenderIdx; i<postBattleSurrenderPrisons.Count; i++) {
			int gIdx = postBattleSurrenderPrisons[i];
			GeneralInfo gInfo = Informations.Instance.GetGeneralInfo(gIdx);
			bool isOwnGeneralReturn = gInfo.king == Controller.kingIndex;
			int dialogueIdx;
			bool isSuccess = ApplyPostBattleSurrenderResultForGeneral(gIdx, out dialogueIdx);

			// 2. 区分本方旧将回归、敌将归降和拒绝招降，方便玩家看懂批量结果。
			if (isSuccess && isOwnGeneralReturn) {
				returnCount++;
			} else if (isSuccess) {
				successCount++;
			} else {
				failCount++;
			}
		}

		// 3. 播放一次结算音效，避免批量处理时连续叠音。
		if (successCount + returnCount > 0) {
			SoundController.Instance.PlaySound("00045");
		} else {
			SoundController.Instance.PlaySound("00057");
		}

		// 4. 跳到战后招降结束阶段，点击继续后回到主地图。
		postBattleSurrenderIdx = postBattleSurrenderPrisons.Count;
		state = StatePostBattleSurrenderKingAnswer;
		SetPostBattleSurrenderText(string.Format(ZhongWen.Instance.postBattleSurrenderAllResult,
		                                         successCount,
		                                         returnCount,
		                                         failCount));
	}

	// 方法说明：按年度招降同一套概率结算指定俘虏的战后招降结果。
	// 参数说明：gIdx 为武将编号，dialogueIdx 返回单人招降答复索引。
	// 返回说明：招降成功或本方旧将回归返回 true，招降失败返回 false。
	bool ApplyPostBattleSurrenderResultForGeneral(int gIdx, out int dialogueIdx) {
		GeneralInfo gInfo = Informations.Instance.GetGeneralInfo(gIdx);
		bool isSuccess = false;
		dialogueIdx = 0;

		gInfo.active = 0;

		if (gInfo.king == Controller.kingIndex) {
			dialogueIdx = 5;
			isSuccess = true;
		} else if (Random.Range(0, 100) < (100 - gInfo.loyalty) / 2) {
			dialogueIdx = 0;
			isSuccess = true;
		} else {
			dialogueIdx = (100 - gInfo.loyalty) / 10 + 1;
			dialogueIdx = Mathf.Clamp(dialogueIdx, 0, 4);
			LowerPostBattleSurrenderLoyalty(gInfo);
		}

		if (isSuccess) {
			ApplyPostBattleSurrenderSuccess(gInfo, gIdx);
		}

		return isSuccess;
	}

	// 方法说明：处理战后招降失败后的忠诚变化。
	// 参数说明：gInfo 为招降失败的俘虏武将数据。
	// 返回说明：无返回值。
	void LowerPostBattleSurrenderLoyalty(GeneralInfo gInfo) {
		gInfo.loyalty -= Random.Range(5, 20);
		gInfo.loyalty = Mathf.Clamp(gInfo.loyalty, 0, 100);
	}

	// 方法说明：把战后招降成功的俘虏加入当前战果归属的城市或部队。
	// 参数说明：gInfo 为成功招降的武将数据，gIdx 为武将编号。
	// 返回说明：无返回值。
	void ApplyPostBattleSurrenderSuccess(GeneralInfo gInfo, int gIdx) {
		gInfo.loyalty = 90;
		gInfo.king = Controller.kingIndex;
		gInfo.prisonerIdx = -1;
		gInfo.soldierCur = gInfo.soldierMax;
		gInfo.knightCur = gInfo.knightMax;

		if (postBattleSurrenderCityInfo != null) {
			gInfo.city = postBattleSurrenderCityIdx;
			postBattleSurrenderCityInfo.prisons.Remove(gIdx);
			AddGeneralUnique(postBattleSurrenderCityInfo.generals, gIdx);
		} else if (postBattleSurrenderArmyInfo != null) {
			gInfo.city = -1;
			postBattleSurrenderArmyInfo.prisons.Remove(gIdx);
			AddGeneralUnique(postBattleSurrenderArmyInfo.generals, gIdx);
		} else {
			Debug.LogError("Post battle surrender target cannot found!");
		}

		AddGeneralUnique(Informations.Instance.GetKingInfo(Controller.kingIndex).generals, gIdx);
	}

	// 方法说明：在武将编号列表中追加不存在的武将，避免重复归属。
	// 参数说明：generals 为武将编号列表，gIdx 为武将编号。
	// 返回说明：无返回值。
	void AddGeneralUnique(List<int> generals, int gIdx) {
		if (!generals.Contains(gIdx)) {
			generals.Add(gIdx);
		}
	}

	// 方法说明：战后招降流程结束后加载主地图。
	// 参数说明：无。
	// 返回说明：无返回值。
	void LoadStrategyAfterWarOver() {
		Misc.LoadLevel("Strategy");
	}
		
	void WarOverResultArmyToArmy(ArmyInfo winner, ArmyInfo loser) {
		
		winner.state = (int)ArmyController.ArmyState.Victory;
		for (int i=0; i<winner.generals.Count; i++) {
			GeneralInfo wgInfo = Informations.Instance.GetGeneralInfo(winner.generals[i]);
			wgInfo.prisonerIdx = -1;
			
			if (wgInfo.level > 4) {
				//wgInfo.experience += 137 * (wgInfo.level - 4) / 2;
				wgInfo.experience += (Misc.GetLevelExperience(wgInfo.level + 1) - Misc.GetLevelExperience(wgInfo.level)) / 2;
			} else {
				wgInfo.experience += 50;
			}
			CheckLevelUp(wgInfo);
		}
		
		int count = loser.generals.Count;
		for (int i=count-1; i>=0; i--) {
			
			int gIdx = loser.generals[i];
			if (Informations.Instance.GetGeneralInfo(gIdx).prisonerIdx == winner.king) {
				winner.prisons.Add(gIdx);
				
				Informations.Instance.GetKingInfo(loser.king).generals.Remove(gIdx);
				loser.generals.RemoveAt(i);
				
				if (gIdx == Informations.Instance.GetKingInfo(loser.king).generalIdx) {
					SetKingOver(loser.king);
				}
			}
		}
		
		if (loser.generals.Count == 0) {
			
			for (int i=0; i<loser.prisons.Count; i++) {
				Informations.Instance.GetGeneralInfo(loser.prisons[i]).prisonerIdx = winner.king;
			}
			
			winner.prisons.AddRange(loser.prisons);
			loser.prisons.Clear();
			
			winner.money += loser.money;
			
			Informations.Instance.armys.Remove(loser);
		} else {
			
			FindArmyCommander(loser);
			
			int tmp = loser.cityTo;
			loser.cityTo = loser.cityFrom;
			loser.cityFrom = tmp;
			
			loser.state = (int)ArmyController.ArmyState.Escape;
		}
	}
	
	void WarOverResultArmyToCity(ArmyInfo armyInfo, int cIdx, bool isWin) {
		
		CityInfo cInfo = Informations.Instance.GetCityInfo(cIdx);
		
		if (isWin) {
		
			for (int i=0; i<armyInfo.generals.Count; i++) {
				
				GeneralInfo gInfo = Informations.Instance.GetGeneralInfo(armyInfo.generals[i]);
				gInfo.prisonerIdx = -1;
				gInfo.city = cIdx;
				gInfo.escape = 0;
				
				if (gInfo.level > 4) {
					//gInfo.experience += 137 * (gInfo.level - 4) / 2;
					gInfo.experience += (Misc.GetLevelExperience(gInfo.level + 1) - Misc.GetLevelExperience(gInfo.level)) / 2;
				} else {
					gInfo.experience += 50;
				}
				CheckLevelUp(gInfo);
			}
			
			int count = cInfo.generals.Count;
			for (int i=count-1; i>=0; i--) {
				
				if (Informations.Instance.GetGeneralInfo(cInfo.generals[i]).prisonerIdx == armyInfo.king) {
					cInfo.prisons.Add(cInfo.generals[i]);
					Informations.Instance.GetKingInfo(cInfo.king).generals.Remove(cInfo.generals[i]);
					
					if (cInfo.generals[i] == Informations.Instance.GetKingInfo(cInfo.king).generalIdx) {
						SetKingOver(cInfo.king);
					}
					
					cInfo.generals.RemoveAt(i);
				}
			}
			
			count = cInfo.generals.Count;
			if (count > 0) {
				SetEscapeFromCity(cIdx);
			}
			
			//cInfo.generals.Clear();
			cInfo.generals.AddRange(armyInfo.generals);
			cInfo.prefect = armyInfo.commander;
			
			for (int i=0; i<cInfo.prisons.Count; i++) {
				Informations.Instance.GetGeneralInfo(cInfo.prisons[i]).prisonerIdx = armyInfo.king;
			}
			
			for (int i=0; i<armyInfo.prisons.Count; i++) {
				Informations.Instance.GetGeneralInfo(armyInfo.prisons[i]).city = cIdx;
			}
			
			cInfo.prisons.AddRange(armyInfo.prisons);
			cInfo.money += armyInfo.money;
			
			Informations.Instance.armys.Remove(armyInfo);
			
			if (cInfo.king != -1) {
				KingInfo kInfo = Informations.Instance.GetKingInfo(cInfo.king);
				kInfo.cities.Remove(cIdx);
			}
			
			cInfo.king = armyInfo.king;
			Informations.Instance.GetKingInfo(cInfo.king).cities.Add(cIdx);
			
		} else {
			
			for (int i=0; i<cInfo.generals.Count; i++) {
				
				GeneralInfo gInfo = Informations.Instance.GetGeneralInfo(cInfo.generals[i]);
				gInfo.prisonerIdx = -1;
				
				if (gInfo.level > 4) {
					//gInfo.experience += 137 * (gInfo.level - 4) / 2;
					gInfo.experience += (Misc.GetLevelExperience(gInfo.level + 1) - Misc.GetLevelExperience(gInfo.level)) / 2;
				} else {
					gInfo.experience += 50;
				}
				CheckLevelUp(gInfo);
			}
			
			int count = armyInfo.generals.Count;
			for (int i=count-1; i>=0; i--) {
				
				int gIdx = armyInfo.generals[i];
				if (Informations.Instance.GetGeneralInfo(gIdx).prisonerIdx == cInfo.king) {
					
					if (gIdx == Informations.Instance.GetKingInfo(armyInfo.king).generalIdx) {
						SetKingOver(armyInfo.king);
					}
					
					cInfo.prisons.Add(gIdx);
					Informations.Instance.GetGeneralInfo(gIdx).city = cIdx;
					
					Informations.Instance.GetKingInfo(armyInfo.king).generals.Remove(gIdx);
					armyInfo.generals.RemoveAt(i);
				}
			}
			
			if (armyInfo.generals.Count == 0) {
			
				for (int i=0; i<armyInfo.prisons.Count; i++) {
					Informations.Instance.GetGeneralInfo(armyInfo.prisons[i]).prisonerIdx = cInfo.king;
					Informations.Instance.GetGeneralInfo(armyInfo.prisons[i]).city = cIdx;
				}
				
				cInfo.prisons.AddRange(armyInfo.prisons);
				armyInfo.prisons.Clear();
				
				cInfo.money += armyInfo.money;
				Informations.Instance.armys.Remove(armyInfo);
			} else {
				
				FindArmyCommander(armyInfo);

				armyInfo.cityTo = armyInfo.cityFrom;
				armyInfo.cityFrom = cIdx;
				
				armyInfo.state = (int)ArmyController.ArmyState.Escape;
			}
		}
	}
	
	void FindArmyCommander(ArmyInfo armyInfo) {
		
		int strengthMax = 0;
		int commander = -1;
		
		for (int i=0; i<armyInfo.generals.Count; i++) {
			
			if (armyInfo.commander == armyInfo.generals[i] || Informations.Instance.GetKingInfo(armyInfo.king).generalIdx == armyInfo.generals[i]) {
				commander = armyInfo.generals[i];
				break;
			}
			
			int strengthCur = Informations.Instance.GetGeneralInfo(armyInfo.generals[i]).strength;
			if (strengthCur > strengthMax) {
				commander = armyInfo.generals[i]; 
				strengthMax = strengthCur;
			}
		}
		
		armyInfo.commander = commander;
	}
	
	void SetEscapeFromCity(int cIdx) {
		
		CityInfo cInfo = Informations.Instance.GetCityInfo(cIdx);
		
		do {
			ArmyInfo ai 		= new ArmyInfo();
			ai.king 			= cInfo.king;
			ai.money			= 0;
			
			Informations.Instance.armys.Add(ai);
			
			int count = cInfo.generals.Count;
			int min = count - 5;
			min = Mathf.Clamp(min, 0, count);
			for (int i=count-1; i>=min; i--) {
				int g = cInfo.generals[i];
				ai.generals.Add(g);
				cInfo.generals.RemoveAt(i);
				
				Informations.Instance.GetGeneralInfo(g).city = -1;
			}
			
			ai.commander = -1;
			FindArmyCommander(ai);
			
			int cityEscTo = -1;
			List<int> clist = MyPathfinding.GetCityNearbyIdx(cIdx);
			List<int> canGoList = new List<int>();
			
			if (clist.Count == 1) {
				cityEscTo = clist[0];
			} else {
				
				for (int i=0; i<clist.Count; i++) {
					if (Informations.Instance.GetCityInfo(clist[i]).king == ai.king) {
						canGoList.Add(clist[i]);
					}
				}
				
				if (canGoList.Count > 0) {
					cityEscTo = canGoList[Random.Range(0, canGoList.Count)];
				}
				
				if (cityEscTo == -1) {
					for (int i=0; i<clist.Count; i++) {
						if (Informations.Instance.GetCityInfo(clist[i]).king == -1) {
							canGoList.Add(clist[i]);
						}
					}
					
					if (canGoList.Count > 0) {
						cityEscTo = canGoList[Random.Range(0, canGoList.Count)];
					}
				}
				
				if (cityEscTo == -1) {
					cityEscTo = clist[Random.Range(0, clist.Count)];
				}
			}
			
			ai.cityFrom = cityAttacked;
			ai.cityTo 	= cityEscTo;
			
			ai.state = (int)ArmyController.ArmyState.Escape;
			ai.pos = Vector3.zero;
		} while(cInfo.generals.Count > 0);
	}
	
	void SetKingOver(int kIdx) {
		
		for (int g=0; g<Informations.Instance.generalNum; g++) {
			GeneralInfo gInfo = Informations.Instance.GetGeneralInfo(g);
			
			if (gInfo.king == kIdx) {

				gInfo.king = Informations.Instance.kingNum;
			}
		}
		
		for (int c=0; c<Informations.Instance.cityNum; c++) {
			CityInfo cInfo = Informations.Instance.GetCityInfo(c);
			
			if (cInfo.king == kIdx) {
				cInfo.king = Informations.Instance.kingNum;
				for (int i=0; i<cInfo.prisons.Count; i++) {
					Informations.Instance.GetGeneralInfo(cInfo.prisons[i]).prisonerIdx = Informations.Instance.kingNum;
				}
			}
		}
		
		for (int i=0; i<Informations.Instance.armys.Count; i++) {
			ArmyInfo ai = Informations.Instance.armys[i];
			
			if (ai.king == kIdx) {
				ai.king = Informations.Instance.kingNum;
			}
			for (int j=0; j<ai.prisons.Count; j++) {
				Informations.Instance.GetGeneralInfo(ai.prisons[j]).prisonerIdx = Informations.Instance.kingNum;
			}
		}
		
		KingInfo kInfo = Informations.Instance.GetKingInfo(kIdx);
		
		kInfo.generals.Clear();
		kInfo.cities.Clear();
		kInfo.active = 0;
	}
	
	void OnToWar() {
		
		menuSelectIdx = 0;
		OnSubMenu();
		
		WarSceneController.leftGeneralIdx = leftGenerals[leftSelectIdx];
		WarSceneController.rightGeneralIdx = rightGenerals[rightSelectIdx];
		WarSceneController.leftDefense = leftDefense;
		WarSceneController.rightDefense = rightDefense;
		WarSceneController.rightGeneralPosition = gPos[rightSelectIdx];
		
		GeneralInfo left = Informations.Instance.GetGeneralInfo(leftGenerals[leftSelectIdx]);
		GeneralInfo right = Informations.Instance.GetGeneralInfo(rightGenerals[rightSelectIdx]);
		
		leftExperience = left.level * 50 + left.knightCur * 6 + left.soldierCur * 3 + left.strength * 2 + left.healthCur + left.manaCur / 2 + leftDefense / 10 + Random.Range(0, 100);
		rightExperience = right.level * 50 + right.knightCur * 6 + right.soldierCur * 3 + right.strength * 2 + right.healthCur + right.manaCur / 2 + rightDefense / 10 + Random.Range(0, 100);
		
		Misc.LoadLevel("WarScene");
	}
	
	void OnSelectFormation() {
		
		menuSelectIdx = 1;
		OnSubMenu();
		selectFormation.SetGeneral(rightGenerals[rightSelectIdx]);
	}
	
	void OnGeneralPosition() {
		
		menuSelectIdx = 2;
		OnSubMenu();
		generalPos.SetActive(true);
	}
	
	void OnGeneralInformation() {
		
		menuSelectIdx = 3;
		OnSubMenu();
		
		generalsInfo.AddGeneralsList(rightGenerals);
		generalsInfo.AddGeneralsList(leftGenerals);
	}
	
	void OnEscape() {
		
		menuSelectIdx = 4;
		OnSubMenu();
		retreatConfirm.SetActive(true);
	}
	
	void OnSubMenu() {
		
		foreach (Button m in menus) {
			m.enabled = false;
		}
		
		generalListCtrl.SetClickEable(false);
	}
	
	public void OnReturnMain() {
		
		state = 0;
		
		int i = 0;
		if (rightFailFlag[rightSelectIdx]) {
			i = 3;
		}
		
		for (; i<menus.Length; i++) {
			menus[i].enabled = true;
		}
		
		if (menuSelectIdx != -1) {
			
			menus[menuSelectIdx].GetComponent<exSpriteFont>().topColor = new Color(1, 1, 1, 1);
			menus[menuSelectIdx].GetComponent<exSpriteFont>().botColor = new Color(1, 1, 1, 1);
			
			menuSelectIdx = -1;
		}
		
		generalListCtrl.SetClickEable(true);
	}
	
	public void UpdateGeneralInfo() {
		
		rightGeneralInfo.SetGeneralInformation(rightGenerals[rightSelectIdx], rightDefense, gPos[rightSelectIdx]);
	}
	
	public void OnSelectGeneralPosition(int pos) {
		
		gPos[rightSelectIdx] = pos;
		UpdateGeneralInfo();
	}
	
	public void OnRetreat() {
		
		warResult = 1;
		state = 3;
		dialogCtrl.SetDialogue(mineCommander, ZhongWen.Instance.qingkuangbumiao, MenuDisplayAnim.AnimType.InsertFromBottom);
	}
	
	public void OnRightGeneralSelected(int idx) {
		
		rightSelectIdx = idx;
		rightGeneralInfo.SetGeneralInformation(rightGenerals[rightSelectIdx], rightDefense, gPos[rightSelectIdx]);
		
		if (rightFailFlag[rightSelectIdx]) {
			
			for (int i=0; i<3; i++) {
				menus[i].SetButtonEnable(false);
			}
		} else {
			for (int i=0; i<3; i++) {
				menus[i].SetButtonEnable(true);
			}
			
			if (state != 0) {
				for (int i=0; i<3; i++) {
					menus[i].enabled = false;
				}
			}
		}
	}
}
