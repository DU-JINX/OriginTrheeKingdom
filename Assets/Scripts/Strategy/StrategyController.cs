using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StrategyController : MonoBehaviour {
	
	public enum State {
		Normal,
		Pause,
		TimePass,
		OnWar,
		GameOver,
		GameVictory,
		ChangeScene = -1
	}
	
	public static State state = State.Normal;
	public static bool isFirstEnter = true;
	public static Vector3 strategyCamPos = Vector3.zero;
	
	public GameObject mainMenuCtrl;
	public FlagsController flagsCtrl;
	public MenuDisplayAnim hTimeCtrl;
	public DialogueController dialog;
	public ChoiceTargetController choiceTarget;
	public GameObject armyPrefab;
	public GameObject victoryDialog;
	
	private GameObject root;
	private MyPathfinding pathfinding;
	
	private bool isMouseMove = false;
	private bool isSpeedButtonPointerDown = false;
	private Vector3 mouseDownPos = Vector3.zero;
	
	private Vector3 scale = new Vector3 (640f/Screen.width, 480f/Screen.height, 0);
	
	// 方法说明：初始化战略地图控制器、军队、胜负状态和顶部加速按钮。
	// 参数说明：无。
	// 返回说明：无返回值。
	void Start () {
		
		// 1. 初始化战略地图运行状态和寻路依赖。
		state = State.Normal;
		pathfinding = GameObject.FindWithTag("Pathfinding").GetComponent<MyPathfinding>();
		Camera.main.transform.position = strategyCamPos;
		
		// 2. 挂载主地图加速按钮控制器。
		EnsureSpeedUpController();
		
		// 3. 修正城市军队数据并生成地图军队对象。
		CheckCorrection();
		InitArmys();

		// 4. 检查胜负状态并播放战略地图背景音乐。
		CheckGameState();

		SoundController.Instance.PlayBackgroundMusic("Music03");
	}
		
	// Update is called once per frame
	void Update () {

		if (isFirstEnter) {
			isFirstEnter = false;
			
			SetGamePause();
			choiceTarget.AddCityTarget(Informations.Instance.GetGeneralInfo(
				Informations.Instance.GetKingInfo(Controller.kingIndex).generalIdx).city);
			choiceTarget.Show();
		}
		
		switch (state) {
		case State.Normal:
			OnNormalMode();
			break;
		case State.TimePass:
			OnTimeOverMode();
			break;
		case State.OnWar:
			OnWarHappenMode();
			break;
		case State.GameOver:
			GameOverHandler();
			break;
		case State.GameVictory:
			GameVictoryHandler();
			break;
		}
	}
	
	// 方法说明：确保战略地图对象挂载主地图加速按钮控制器。
	// 参数说明：无。
	// 返回说明：无返回值。
	void EnsureSpeedUpController() {
		
		if (GetComponent<StrategySpeedUpController>() == null) {
			gameObject.AddComponent<StrategySpeedUpController>();
		}
	}
	
	// 方法说明：处理战略地图普通状态下的点击、拖动和菜单入口。
	// 参数说明：无。
	// 返回说明：无返回值。
	void OnNormalMode() {
		
		// 1. 顶部加速和暂停按钮区域交给 StrategySpeedUpController 处理，避免触发地图菜单。
		bool isPointerOverSpeedButton = StrategySpeedUpController.IsPointerOverSpeedButton();

		if (Input.GetMouseButtonDown(0) && isPointerOverSpeedButton) {
			isSpeedButtonPointerDown = true;
			return;
		}

		if (isSpeedButtonPointerDown) {
			if (Input.GetMouseButtonUp(0)) {
				isSpeedButtonPointerDown = false;
			}
			return;
		}

		if (!Input.GetMouseButton(0) && isPointerOverSpeedButton) {
			return;
		}

		if (StrategySpeedState.IsPaused()) {
			return;
		}
		
		// 2. 鼠标按下时记录起点，用于区分点击和拖动。
		if (Input.GetMouseButtonDown(0)) {
			
			isMouseMove = false;
			mouseDownPos = Input.mousePosition;
			
		} else if (!isMouseMove && Input.GetMouseButtonUp(0)) {
				
			// 3. 鼠标松开且没有拖动时，按点击目标打开城市、军队或主菜单。
			SetGamePause();
			
			int cityIdx = flagsCtrl.GetTouchCityIdx();
			if (cityIdx != -1) {
				
				choiceTarget.AddCityTarget(cityIdx);
				SoundController.Instance.PlaySound("00038");
			}
			
			for (int i=0; i<Informations.Instance.armys.Count; i++) {
				
				if (Informations.Instance.armys[i].armyCtrl.GetTouchedFlag()) {
					choiceTarget.AddArmyTarget(Informations.Instance.armys[i]);

					SoundController.Instance.PlaySound("00038");
				}
			}
			
			if (choiceTarget.targetList.GetCount() == 0) {
				ShowMainMenu();
			} else {
				choiceTarget.Show();
			}

			Input.ResetInputAxes();
		} else if (Input.GetMouseButton(0)) {
			
			// 4. 鼠标拖动时移动战略地图相机。
			if (!isMouseMove) {
				
				Vector3 offset = mouseDownPos - Input.mousePosition;
				offset.Scale(scale);
				
				if (Mathf.Abs(offset.x) > 5 || Mathf.Abs(offset.y) > 5) {
					
					isMouseMove = true;
					mouseDownPos = Input.mousePosition;
				}
			} else {
				
				Vector3 offset = mouseDownPos - Input.mousePosition;
				mouseDownPos = Input.mousePosition;
				offset.Scale(scale);
					
				Vector3 pos = Camera.main.transform.position;
				
				pos += offset;
				pos.x = Mathf.Clamp(pos.x, -320, 320);
				pos.y = Mathf.Clamp(pos.y, -240, 240);
				
				Camera.main.transform.position = pos;
				
			}
		}
	}
	
	void OnTimeOverMode() {
		
		if (dialog.IsShowingText()) 
			return;
		
		if (Input.GetMouseButtonDown(0)) {
			
			for (int i=0; i<Informations.Instance.cityNum; i++) {
			
				CityInfo cInfo = Informations.Instance.GetCityInfo(i);
				
				if (cInfo.king != -1 && cInfo.king != Informations.Instance.kingNum) {
					
					int moneyAdd = (int) (2250 + cInfo.population / 133.34f);
					
					cInfo.money += moneyAdd;
					cInfo.money = Mathf.Clamp(cInfo.money, cInfo.money, 999999);
				}
			}
			
			for (int i=0; i<Informations.Instance.generalNum; i++) {
				
				GeneralInfo gInfo = Informations.Instance.GetGeneralInfo(i);
				
				gInfo.active = 1;
				if (gInfo.king == -1 || gInfo.king == Controller.kingIndex || gInfo.king == Informations.Instance.kingNum) continue;
				
				gInfo.experience += (Misc.GetLevelExperience(gInfo.level+1) - Misc.GetLevelExperience(gInfo.level)) / 2;
				Misc.CheckIsLevelUp(gInfo);
			}
			
			strategyCamPos = Camera.main.transform.position;
			
			Misc.LoadLevel("InternalAffairs");
			state = State.ChangeScene;
		}
	}
	
	void OnWarHappenMode() {
		
		Camera.main.transform.position = Vector3.MoveTowards(Camera.main.transform.position, strategyCamPos, 500*Time.deltaTime);
		
		if (dialog.IsShowingText()) {
			return;
		}
		
		if (Input.GetMouseButtonDown(0)) {
			
			strategyCamPos = Camera.main.transform.position;
			
			Misc.LoadLevel("SelectGeneralToWar");
			state = State.ChangeScene;
		}
	}

	void GameOverHandler() {
		if (dialog.IsShowingText()) {
			return;
		}

		if (Input.GetMouseButtonDown(0)) {
			Misc.LoadLevel("GameOver");
			state = State.ChangeScene;
		}
	}

	void GameVictoryHandler() {
		if (dialog.IsShowingText()) {
			return;
		}

		if (Input.GetMouseButtonDown(0)) {
			dialog.SetDialogueOut(MenuDisplayAnim.AnimType.OutToBottom);
			GameObject go = (GameObject)Instantiate(victoryDialog);
			go.transform.parent = Camera.main.transform;
			go.transform.localPosition = Vector3.zero;
		}
	}

	void CheckGameState() {

		if (Informations.Instance.GetKingInfo(Controller.kingIndex).active == 0) {
			SetGamePause();
			state = State.GameOver;
			dialog.SetDialogue(Informations.Instance.GetKingInfo(Controller.kingIndex).generalIdx,
			                   ZhongWen.Instance.henyihantongyi,
			                   MenuDisplayAnim.AnimType.InsertFromBottom);
		} else {
			bool flag = true;

			for (int i=0; i<Informations.Instance.cityNum; i++) {
				CityInfo cInfo = Informations.Instance.GetCityInfo(i);

				if (cInfo.king != Controller.kingIndex) {
					flag = false;
				}
			}
			if (flag) {
                PlayerPrefs.SetInt("GamePass", 1);
                PlayerPrefs.Save();

				SetGamePause();
				state = State.GameVictory;
				dialog.SetDialogue(Informations.Instance.GetKingInfo(Controller.kingIndex).generalIdx,
				                   ZhongWen.Instance.jieshuyu,
				                   MenuDisplayAnim.AnimType.InsertFromBottom);
			}
		}
	}

	void SetGamePause() {
		
		state = State.Pause;

		hTimeCtrl.SetAnim(MenuDisplayAnim.AnimType.OutToLeft);
		
		flagsCtrl.SetFlagsAnimPause();
		SetArmyPause();
	}
	
	void ShowMainMenu() {

		Vector3 camPos = Camera.main.transform.position;
		Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		
		pos.x = Mathf.Clamp(pos.x, camPos.x - (640 - 182) / 2, camPos.x + (640 - 182) / 2);
		pos.y = Mathf.Clamp(pos.y, camPos.y - (480 - 213) / 2, camPos.y + (480 - 213) / 2);
		pos.z = mainMenuCtrl.transform.position.z;
		
		mainMenuCtrl.SetActive(true);
		mainMenuCtrl.transform.position = pos;
	}
	
	public void ReturnMainMode() {
		
		state = State.Normal;
		
		hTimeCtrl.SetAnim(MenuDisplayAnim.AnimType.InsertFromLeft);
		flagsCtrl.SetFlagsAnimResume();
		SetArmyResume();

		Misc.isBack = false;
		Misc.backButton.SetActive(false);
	}
	
	public void OnTimeOver() {
		
		state = State.TimePass;
		
		flagsCtrl.SetFlagsAnimPause();
		SetArmyPause();
		
		dialog.SetDialogue(Informations.Instance.GetKingInfo(Controller.kingIndex).generalIdx, ZhongWen.Instance.niezhengzhongle, MenuDisplayAnim.AnimType.InsertFromBottom);
	}
	
	void SetArmyPause() {
		
		for (int i=0; i<Informations.Instance.armys.Count; i++) {
			
			Informations.Instance.armys[i].armyCtrl.Pause();
		}
	}
	
	void SetArmyResume() {
		
		for (int i=0; i<Informations.Instance.armys.Count; i++) {
			
			Informations.Instance.armys[i].armyCtrl.Resume();
		}
	}
	
	void CheckCorrection() {
		
		for (int i=0; i<Informations.Instance.cityNum; i++) {
			
			CityInfo cInfo = Informations.Instance.GetCityInfo(i);
			
			if (cInfo.generals.Count <= 10) {
				continue;
			}
			
			do {
				ArmyInfo ai	= new ArmyInfo();
				ai.king = cInfo.king;
				ai.state = (int)ArmyController.ArmyState.Garrison;
				
				if (pathfinding == null)
					pathfinding = GameObject.FindWithTag("Pathfinding").GetComponent<MyPathfinding>();
				ai.pos = pathfinding.GetCityPos(i);
				
				Informations.Instance.armys.Add(ai);
				
				int count = cInfo.generals.Count;
				int min = count - 5;
				min = Mathf.Clamp(min, 10, count);
				for (int j=count-1; j>=min; j--) {
					int g = cInfo.generals[j];
					ai.generals.Add(g);
					cInfo.generals.RemoveAt(j);
					
					Informations.Instance.GetGeneralInfo(g).city = -1;
					
					if (cInfo.prefect == g)
						cInfo.prefect = FindCommander(cInfo.generals, cInfo.king);
				}
				
				ai.commander = FindCommander(ai.generals, ai.king);
				
			} while(cInfo.generals.Count > 10);
		}
	}
	
	int FindCommander(List<int> generalList, int king) {
		
		int strengthMax = 0;
		int commander = -1;
		
		for (int i=0; i<generalList.Count; i++) {
			
			if (Informations.Instance.GetKingInfo(king).generalIdx == generalList[i]) {
				commander = generalList[i];
				break;
			}
			
			int strengthCur = Informations.Instance.GetGeneralInfo(generalList[i]).strength;
			if (strengthCur > strengthMax) {
				commander = generalList[i]; 
				strengthMax = strengthCur;
			}
		}
		
		return commander;
	}
	
	void InitArmys() {
		
		root = GameObject.Find("ArmiesRoot");
		
		for (int i=0; i<Informations.Instance.armys.Count; i++) {
			
			SetArmy(Informations.Instance.armys[i]);
		}
	}
	
	void SetArmy(ArmyInfo armyInfo) {
		
		if (armyInfo.pos == Vector3.zero) {
			armyInfo.pos = pathfinding.GetCityPos(armyInfo.cityFrom);
		}
		
		GameObject go = (GameObject)Instantiate(armyPrefab, armyInfo.pos, transform.rotation);
		go.transform.parent = root.transform;
		ArmyController armyCtrl = go.GetComponent<ArmyController>();
		
		armyCtrl.InitArmyInfo(armyInfo);
	}
	
	public void SetWarDialogue(ArmyInfo mine, ArmyInfo enemy) {
		
		state = State.OnWar;
		
		Vector3 pos = mine.armyCtrl.transform.position;
		pos.x = Mathf.Clamp(pos.x, -320, 320);
		pos.y = Mathf.Clamp(pos.y, -240, 240);
		strategyCamPos = pos;
		
		hTimeCtrl.SetAnim(MenuDisplayAnim.AnimType.OutToLeft);
		flagsCtrl.SetFlagsAnimPause();
		SetArmyPause();
		
		string msg = ZhongWen.Instance.wojun + ZhongWen.Instance.GetGeneralName(mine.commander) + ZhongWen.Instance.budui 
			+ ZhongWen.Instance.zaoyu + ZhongWen.Instance.GetGeneralName(enemy.commander) + ZhongWen.Instance.jun + ZhongWen.Instance.fashengzhandou;
		
		dialog.SetDialogue(Informations.Instance.GetKingInfo(Controller.kingIndex).generalIdx, msg, MenuDisplayAnim.AnimType.InsertFromBottom);
		
		SelectGeneralToWarController.isWarBegin = true;
		SelectGeneralToWarController.mode = 0;
		SelectGeneralToWarController.mine = mine;
		SelectGeneralToWarController.enemy = enemy;
	}
	
	public void SetWarDialogue(int cityAttacked, ArmyInfo enemy) {
		
		state = State.OnWar;
		
		Vector3 pos = enemy.armyCtrl.transform.position;
		pos.x = Mathf.Clamp(pos.x, -320, 320);
		pos.y = Mathf.Clamp(pos.y, -240, 240);
		strategyCamPos = pos;
		
		hTimeCtrl.SetAnim(MenuDisplayAnim.AnimType.OutToLeft);
		flagsCtrl.SetFlagsAnimPause();
		SetArmyPause();
		
		string msg = ZhongWen.Instance.GetCityName(cityAttacked) + ZhongWen.Instance.chengTarget + ZhongWen.Instance.zaodao
			 + ZhongWen.Instance.GetGeneralName(enemy.commander) + ZhongWen.Instance.jun + ZhongWen.Instance.degongji;
		
		dialog.SetDialogue(Informations.Instance.GetKingInfo(Controller.kingIndex).generalIdx, msg, MenuDisplayAnim.AnimType.InsertFromBottom);
		
		SelectGeneralToWarController.isWarBegin = true;
		SelectGeneralToWarController.mode = 1;
		SelectGeneralToWarController.cityAttacked = cityAttacked;
		SelectGeneralToWarController.enemy = enemy;
	}
	
	public void SetWarDialogue(ArmyInfo army, int cityAttacked) {
		
		state = State.OnWar;
		
		Vector3 pos = army.armyCtrl.transform.position;
		pos.x = Mathf.Clamp(pos.x, -320, 320);
		pos.y = Mathf.Clamp(pos.y, -240, 240);
		strategyCamPos = pos;
		
		hTimeCtrl.SetAnim(MenuDisplayAnim.AnimType.OutToLeft);
		flagsCtrl.SetFlagsAnimPause();
		SetArmyPause();
		
		string msg = ZhongWen.Instance.wojun + ZhongWen.Instance.GetGeneralName(army.commander) + ZhongWen.Instance.budui 
			+ ZhongWen.Instance.gongji + ZhongWen.Instance.GetCityName(cityAttacked) + ZhongWen.Instance.chengTarget + ZhongWen.Instance.tanhao;
		
		dialog.SetDialogue(Informations.Instance.GetKingInfo(Controller.kingIndex).generalIdx, msg, MenuDisplayAnim.AnimType.InsertFromBottom);
		
		SelectGeneralToWarController.isWarBegin = true;
		SelectGeneralToWarController.mode = 2;
		SelectGeneralToWarController.cityAttacked = cityAttacked;
		SelectGeneralToWarController.mine = army;
	}
	
	public void AddReservist() {
		
		for (int i=0; i<Informations.Instance.cityNum; i++) {
			
			CityInfo cInfo = Informations.Instance.GetCityInfo(i);
			
			if (cInfo.king != -1 && cInfo.king != Informations.Instance.kingNum) {
				
				if (cInfo.money > 100 && cInfo.reservist < cInfo.reservistMax) {
					
					cInfo.reservist++;
					cInfo.money -= 100;
				}
			}
		}
	}
	
	public void MonthAct() {
		
		for (int i=0; i<Informations.Instance.generalNum; i++) {
			GeneralResume(i);
		}
		
		for (int i=0; i<Informations.Instance.cityNum; i++) {
			CityAct(i);
		}
		
		for (int i=0; i<Informations.Instance.armys.Count; i++) {
			ArmyAct(i);
		}
	}
	
	void GeneralResume(int gIdx) {
		
		GeneralInfo gInfo = Informations.Instance.GetGeneralInfo(gIdx);
		
		if (gInfo.healthCur < gInfo.healthMax) {
			
			gInfo.healthCur += gInfo.strength / 20;
			gInfo.healthCur = Mathf.Clamp(gInfo.healthCur, 0, gInfo.healthMax);
		}
		
		if (gInfo.manaCur < gInfo.manaMax) {
			
			gInfo.manaCur += gInfo.intellect / 20;
			gInfo.manaCur = Mathf.Clamp(gInfo.manaCur, 0, gInfo.manaMax);
		}
	}
	
	void CityAct(int cIdx) {
		
		CityInfo cInfo = Informations.Instance.GetCityInfo(cIdx);
		
		if (cInfo.king == -1 
			|| cInfo.king == Controller.kingIndex
			|| cInfo.king == Informations.Instance.kingNum ) {
			return;
		}
		
		foreach (int gIdx in cInfo.generals) {
			
			if (cInfo.reservist == 0)	break;
			
			GeneralInfo gInfo = Informations.Instance.GetGeneralInfo(gIdx);

			if (gInfo.knightCur < gInfo.knightMax) {
				
				int soldierAdd = gInfo.knightMax - gInfo.knightCur;
				soldierAdd = Mathf.Clamp(soldierAdd, 0, cInfo.reservist);
				
				gInfo.knightCur += soldierAdd;
				cInfo.reservist -= soldierAdd;
			}

			if (cInfo.reservist == 0)	break;

			if (gInfo.soldierCur < gInfo.soldierMax) {
				
				int soldierAdd = gInfo.soldierMax - gInfo.soldierCur;
				soldierAdd = Mathf.Clamp(soldierAdd, 0, cInfo.reservist);
				
				gInfo.soldierCur += soldierAdd;
				cInfo.reservist -= soldierAdd;
			}
		}
		
		List<int> cities = MyPathfinding.GetCityNearbyIdx(cIdx);
		
		for (int i=0; i<cities.Count; i++) {
			
			CityInfo cityNeighbor = Informations.Instance.GetCityInfo(cities[i]);

			int count = 0;
			for (int j=0; j<cInfo.generals.Count; j++) {
				GeneralInfo gInfo = Informations.Instance.GetGeneralInfo(cInfo.generals[j]);
				if (gInfo.healthCur > 50 && (gInfo.soldierCur + gInfo.knightCur) > (gInfo.soldierMax + gInfo.knightMax) / 2) {
					count++;
				}
			}

			if (cityNeighbor.king == cInfo.king) {
				if (count < 8 && cityNeighbor.generals.Count > 2 && Random.Range(0, 100) > 88) {
					
					SetArmyMove(cities[i], cIdx, Random.Range(1, cityNeighbor.generals.Count), 5);
					break;
				}
			} else {
				
				if (cityNeighbor.king == -1) {
					if (count > 3 && Random.Range(0, 100) > 80) {
						SetArmyMove(cIdx, cities[i], Random.Range(1, cInfo.generals.Count), 5);
						break;
					}
				} else {
					if (count > 2 && Random.Range(0, 100) > 88) {
						if (count - cityNeighbor.generals.Count >= 2) {
							
							SetArmyMove(cIdx, cities[i], Random.Range(cityNeighbor.generals.Count + 1, cInfo.generals.Count), 5);
							break;
						} else if (count - cityNeighbor.generals.Count > 0 && Random.Range(0, 100) > 50) {
							
							SetArmyMove(cIdx, cities[i], cityNeighbor.generals.Count, 5);
							break;
						} else if (count - cityNeighbor.generals.Count == 0 && Random.Range(0, 100) > 88) {
							
							SetArmyMove(cIdx, cities[i], cityNeighbor.generals.Count - 1, 5);
							break;
						}
					}
				}
			}
		}
	}
	
	void SetArmyMove(int fo, int to, int num, int maxNum) {
		
		while (num > 0) {
			SetArmyMove(fo, to, Mathf.Clamp(num, 1, maxNum));
			num -= maxNum;
		}
	}
	
	void SetArmyMove(int fo, int to, int num) {
		
		CityInfo cInfo = Informations.Instance.GetCityInfo(fo);
		
		ArmyInfo armyInfo 	= new ArmyInfo();
		armyInfo.king 		= cInfo.king;
		armyInfo.cityFrom	= fo;
		armyInfo.cityTo 	= to;
		armyInfo.money 		= Random.Range(0, cInfo.money);
		armyInfo.pos 		= pathfinding.GetCityPos(fo);
		
		bool isPerfect = false;
		int count = cInfo.generals.Count;

		int numLeft = num;
		int j = count - 1;
		while (numLeft > 0 && j >= 0) {

			GeneralInfo gInfo = Informations.Instance.GetGeneralInfo(cInfo.generals[j]);
			if (gInfo.healthCur > 50) {

				if (cInfo.generals[j] == cInfo.prefect) {
					if (Random.Range(0, 100) > 50) {
						isPerfect = true;
						armyInfo.commander = cInfo.generals[j];
					} else {
						j--;
						continue;
					}
				}
				
				gInfo.city = -1;
				armyInfo.generals.Add(cInfo.generals[j]);
				cInfo.generals.RemoveAt(j);

				numLeft--;
			}
			j--;
		}

		if (armyInfo.generals.Count == 0) {
			return;
		}
		
		if (cInfo.prisons.Count > 0 && Random.Range(0, 100) > 60) {
			for (int i=0; i<cInfo.prisons.Count; i++) {
				Informations.Instance.GetGeneralInfo(cInfo.prisons[i]).city = -1;
			}
			armyInfo.prisons.AddRange(cInfo.prisons);
			cInfo.prisons.Clear();
		}

		if (isPerfect) {
			
			cInfo.prefect = cInfo.generals[0];
			for (int k=0; k<cInfo.generals.Count; k++) {
				
				if (cInfo.generals[k] == Informations.Instance.GetKingInfo(cInfo.king).generalIdx) {
					cInfo.prefect = cInfo.generals[k];
					break;
				}
				
				if (Informations.Instance.GetGeneralInfo(cInfo.prefect).strength
					< Informations.Instance.GetGeneralInfo(cInfo.generals[k]).strength ) {
					cInfo.prefect = cInfo.generals[k];
				}
			}
		} else {
			
			armyInfo.commander = armyInfo.generals[0];
			for (int k=0; k<armyInfo.generals.Count; k++) {
				
				if (armyInfo.generals[k] == Informations.Instance.GetKingInfo(armyInfo.king).generalIdx) {
					armyInfo.commander = armyInfo.generals[k];
					break;
				}
				
				if (Informations.Instance.GetGeneralInfo(armyInfo.commander).strength
					< Informations.Instance.GetGeneralInfo(armyInfo.generals[k]).strength ) {
					armyInfo.commander = armyInfo.generals[k];
				}
			}
		}
		
		SetArmy(armyInfo);
		Informations.Instance.armys.Add(armyInfo);
	}
	
	void ArmyAct(int i) {
		
		ArmyInfo armyInfo = Informations.Instance.armys[i];
		
		if (armyInfo.king != Controller.kingIndex && armyInfo.armyCtrl.GetState() == ArmyController.ArmyState.Garrison) {
			
			int cityCanIntoIdx = pathfinding.GetCityIndex(armyInfo.armyCtrl.transform.position, 30);
			if (cityCanIntoIdx != -1) {
				armyInfo.armyCtrl.IntoTheCity(cityCanIntoIdx);
			}
		}
	}

	public static void Reset() {
		state = State.Normal;
		isFirstEnter = true;
		strategyCamPos = Vector3.zero;
	}
}
