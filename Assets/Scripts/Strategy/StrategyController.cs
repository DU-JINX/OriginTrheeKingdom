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
	private StrategyMapHudController mapHudController;

	private bool isMouseMove = false;
	private bool isInterfacePointerDown = false;
	private bool isPointerDownTracked = false;
	private Vector3 mouseDownPos = Vector3.zero;

	private const string RecoveredSango2StrategyMapResourcePath = "Sango2Recovered/Map/Sango2WorldMapGenerated";
	private const float DefaultStrategyMapWidth = 1280f;
	private const float DefaultStrategyMapHeight = 960f;
	private const float RestoredStrategyDefaultOrthographicSize = 360f;
	private static bool isStrategyMapBoundsCached = false;
	private static Bounds strategyMapBounds = new Bounds(Vector3.zero, new Vector3(DefaultStrategyMapWidth, DefaultStrategyMapHeight, 0));
	
	// 方法说明：初始化战略地图控制器、军队、胜负状态和顶部加速按钮。
	// 参数说明：无。
	// 返回说明：无返回值。
	void Start () {
		
		// 1. 初始化战略地图运行状态和寻路依赖。
		state = State.Normal;
		pathfinding = GameObject.FindWithTag("Pathfinding").GetComponent<MyPathfinding>();
		ResetStrategyMapBoundsCache();
		ApplyRestoredSango2StrategyMap();
		Camera.main.transform.position = ClampCameraPosition(strategyCamPos);
		
		// 2. 挂载主地图速度控制和新版战略地图界面。
		EnsureSpeedUpController();
		EnsureStrategyMapHudController();
		
		// 3. 修正城市军队数据并生成地图军队对象。
		CheckCorrection();
		InitArmys();

		// 4. 检查胜负状态并播放战略地图背景音乐。
		CheckGameState();

		SoundController.Instance.PlayBackgroundMusic("Music03");
	}
		
	// 方法说明：按当前战略地图状态分发每帧逻辑。
	// 参数说明：无。
	// 返回说明：无返回值。
	void Update () {

		KeepMapAnnotationsHiddenWhenPaused();

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

	// 方法说明：确保战略地图对象挂载新版常驻 HUD，并绑定当前控制器。
	// 参数说明：无。
	// 返回说明：无返回值。
	void EnsureStrategyMapHudController() {
		mapHudController = GetComponent<StrategyMapHudController>();
		if (mapHudController == null) {
			mapHudController = gameObject.AddComponent<StrategyMapHudController>();
		}

		mapHudController.Initialize(this);
	}

	// 方法说明：在战略场景主背景上应用 MOD06 无字恢复地图和显示尺寸。
	// 参数说明：无。
	// 返回说明：无返回值。
	void ApplyRestoredSango2StrategyMap() {
		if (!MODLoadController.IsRestoredSango2Index(Controller.MODSelect)) {
			return;
		}

		// 1. 定位战略主背景对象，避免误用菜单里的小地图对象。
		GameObject mapObject = FindStrategyMapObject();
		if (mapObject == null) {
			Debug.LogError("战略场景缺少 Background 地图对象，无法应用二代恢复地图。");
			return;
		}

		// 2. 加载 MOD06 无字恢复地图贴图。
		Texture2D mapTexture = Resources.Load<Texture2D>(RecoveredSango2StrategyMapResourcePath);
		Renderer mapRenderer = mapObject.GetComponent<Renderer>();
		if (mapTexture == null) {
			Debug.LogError("二代恢复地图资源不存在: " + RecoveredSango2StrategyMapResourcePath);
			if (mapRenderer != null) {
				mapRenderer.enabled = false;
			}
			return;
		}

		// 3. 替换主背景材质和 exSprite 尺寸。
		if (mapRenderer == null || mapRenderer.material == null) {
			Debug.LogError("战略地图主背景 Renderer 缺失，无法切换二代恢复地图。");
			return;
		}
		mapRenderer.material.mainTexture = mapTexture;
		ApplyRecoveredStrategyMapMesh(mapObject);

		exSprite sprite = mapObject.GetComponent<exSprite>();
		if (sprite != null) {
			sprite.trimTexture = true;
			sprite.trimUV = new Rect(0f, 0f, 1f, 1f);
			sprite.customSize = true;
			sprite.width = MODLoadController.RecoveredMapWorldWidth;
			sprite.height = MODLoadController.RecoveredMapWorldHeight;
		}

		// 4. 刷新战略相机边界缓存，保证拖动范围读取主背景实际边界。
		ResetStrategyMapBoundsCache();
		ApplyRestoredStrategyDefaultCameraZoom();
	}

	// 方法说明：MOD06 战略地图默认使用更近的高清视野，给超宽屏保留横向拖动空间。
	// 参数说明：无。
	// 返回说明：无返回值。
	void ApplyRestoredStrategyDefaultCameraZoom() {
		Camera camera = Camera.main;
		if (camera == null || !camera.orthographic) return;

		camera.orthographicSize = Mathf.Min(camera.orthographicSize, RestoredStrategyDefaultOrthographicSize);
	}

	// 方法说明：暂停态持续隐藏地图旗帜和城名，避免半透明菜单下出现层级穿透。
	// 参数说明：无。
	// 返回说明：无返回值。
	void KeepMapAnnotationsHiddenWhenPaused() {
		if (state != State.Pause || flagsCtrl == null) return;

		flagsCtrl.KeepPausedMapAnnotationsHidden();
	}

	// 方法说明：把战略主背景重建为和 MOD06 坐标系一致的地图网格。
	// 参数说明：mapObject 为战略场景主地图对象。
	// 返回说明：无返回值。
	void ApplyRecoveredStrategyMapMesh(GameObject mapObject) {
		MeshFilter meshFilter = mapObject.GetComponent<MeshFilter>();
		if (meshFilter == null) {
			Debug.LogError("战略地图主背景 MeshFilter 缺失，无法重建二代恢复地图网格。");
			return;
		}

		float halfWidth = MODLoadController.RecoveredMapWorldWidth * 0.5f;
		float halfHeight = MODLoadController.RecoveredMapWorldHeight * 0.5f;
		Mesh mesh = new Mesh();
		mesh.name = "RecoveredSango2StrategyMapMesh";
		mesh.vertices = new Vector3[] {
			new Vector3(-halfWidth, -halfHeight, 0f),
			new Vector3(halfWidth, -halfHeight, 0f),
			new Vector3(-halfWidth, halfHeight, 0f),
			new Vector3(halfWidth, halfHeight, 0f)
		};
		mesh.uv = new Vector2[] {
			new Vector2(0f, 0f),
			new Vector2(1f, 0f),
			new Vector2(0f, 1f),
			new Vector2(1f, 1f)
		};
		mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
		mesh.RecalculateBounds();
		meshFilter.mesh = mesh;

		Vector3 position = mapObject.transform.position;
		position.x = 0f;
		position.y = 0f;
		mapObject.transform.position = position;
		mapObject.transform.localScale = Vector3.one;
	}

	// 方法说明：查找战略场景主地图对象。
	// 参数说明：无。
	// 返回说明：优先返回激活根节点 Background，缺失时返回激活对象 Background 或 Map，均缺失返回 null。
	static GameObject FindStrategyMapObject() {
		GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
		foreach (GameObject rootObject in rootObjects) {
			if (rootObject.name == "Background" && rootObject.activeInHierarchy) {
				return rootObject;
			}
		}

		GameObject background = GameObject.Find("Background");
		if (background != null) {
			return background;
		}

		return GameObject.Find("Map");
	}

	// 方法说明：重置战略地图边界缓存，地图贴图或尺寸改变后调用。
	// 参数说明：无。
	// 返回说明：无返回值。
	public static void ResetStrategyMapBoundsCache() {
		isStrategyMapBoundsCached = false;
	}

	// 方法说明：根据两次屏幕坐标计算相机需要移动的世界坐标偏移。
	// 参数说明：previousScreenPos 为上一帧屏幕坐标，currentScreenPos 为当前屏幕坐标。
	// 返回说明：返回世界坐标偏移，缺少主相机时返回 Vector3.zero。
	public static Vector3 GetCameraDragOffset(Vector3 previousScreenPos, Vector3 currentScreenPos) {
		Camera camera = Camera.main;
		if (camera == null) {
			return Vector3.zero;
		}

		Vector3 previousWorldPos = camera.ScreenToWorldPoint(previousScreenPos);
		Vector3 currentWorldPos = camera.ScreenToWorldPoint(currentScreenPos);
		Vector3 offset = previousWorldPos - currentWorldPos;
		offset.z = 0;
		return offset;
	}

	// 方法说明：把战略地图相机位置限制在当前地图边界内。
	// 参数说明：position 为待限制的相机世界坐标。
	// 返回说明：返回限制后的相机世界坐标。
	public static Vector3 ClampCameraPosition(Vector3 position) {
		Camera camera = Camera.main;
		if (camera == null) {
			return position;
		}

		// 1. 读取当前地图边界，并确保宽屏视口不会大于地图可覆盖范围。
		Bounds mapBounds = GetStrategyMapBounds();
		ClampCameraZoomToMapBounds(camera, mapBounds);
		float halfHeight = camera.orthographic ? camera.orthographicSize : DefaultStrategyMapHeight * 0.25f;
		float halfWidth = halfHeight * camera.aspect;

		// 2. 根据地图边界减去视口半径，得到相机中心可移动范围。
		float minX = mapBounds.min.x + halfWidth;
		float maxX = mapBounds.max.x - halfWidth;
		float minY = mapBounds.min.y + halfHeight;
		float maxY = mapBounds.max.y - halfHeight;

		// 3. 当视口比地图还大时，固定在地图中心，避免露出单侧底色。
		position.x = ClampCameraAxis(position.x, minX, maxX, mapBounds.center.x);
		position.y = ClampCameraAxis(position.y, minY, maxY, mapBounds.center.y);
		return position;
	}

	// 方法说明：限制正交相机最大视野，保证当前屏幕比例下地图始终覆盖完整视口。
	// 参数说明：camera 为战略地图相机，mapBounds 为主地图世界坐标边界。
	// 返回说明：无返回值。
	private static void ClampCameraZoomToMapBounds(Camera camera, Bounds mapBounds) {
		if (camera == null || !camera.orthographic || camera.aspect <= 0f) return;

		float maxOrthographicSize = Mathf.Min(mapBounds.extents.y, mapBounds.extents.x / camera.aspect);
		if (maxOrthographicSize <= 0f) {
			Debug.LogError("战略地图边界无效，无法限制相机视野。");
			return;
		}

		camera.orthographicSize = Mathf.Min(camera.orthographicSize, maxOrthographicSize);
	}

	// 方法说明：读取战略地图渲染边界，Renderer 不可用时使用当前 MOD 对应的默认地图尺寸。
	// 参数说明：无。
	// 返回说明：返回地图世界坐标边界。
	private static Bounds GetStrategyMapBounds() {
		if (isStrategyMapBoundsCached) {
			return strategyMapBounds;
		}

		// 1. MOD06 使用恢复地图坐标系尺寸，避免旧 exSprite Renderer bounds 返回菜单小图尺寸。
		if (MODLoadController.IsRestoredSango2Index(Controller.MODSelect)) {
			strategyMapBounds = new Bounds(
				Vector3.zero,
				new Vector3(MODLoadController.RecoveredMapWorldWidth, MODLoadController.RecoveredMapWorldHeight, 0));
			isStrategyMapBoundsCached = true;
			return strategyMapBounds;
		}

		// 2. 旧剧本优先读取场景主地图对象的 Renderer 边界。
		GameObject mapObject = FindStrategyMapObject();
		if (mapObject != null) {
			Renderer mapRenderer = mapObject.GetComponent<Renderer>();
			if (mapRenderer != null && mapRenderer.bounds.size.x > 0 && mapRenderer.bounds.size.y > 0) {
				strategyMapBounds = mapRenderer.bounds;
				isStrategyMapBoundsCached = true;
				return strategyMapBounds;
			}
		}

		// 3. Renderer 不可用时按旧地图尺寸生成默认边界，不生成任何内容数据。
		Vector3 mapSize = new Vector3(DefaultStrategyMapWidth, DefaultStrategyMapHeight, 0);

		strategyMapBounds = new Bounds(Vector3.zero, mapSize);
		isStrategyMapBoundsCached = true;
		return strategyMapBounds;
	}

	// 方法说明：限制单轴相机位置，视口大于地图时回到地图中心。
	// 参数说明：value 为当前轴坐标，min 为最小值，max 为最大值，center 为地图中心坐标。
	// 返回说明：返回限制后的单轴坐标。
	private static float ClampCameraAxis(float value, float min, float max, float center) {
		if (min > max) {
			return center;
		}

		return Mathf.Clamp(value, min, max);
	}
	
	// 方法说明：处理战略地图普通状态下的点击、拖动和菜单入口。
	// 参数说明：无。
	// 返回说明：无返回值。
	void OnNormalMode() {
		
		// 1. 新版 HUD 和旧速度按钮区域由界面控制器处理，避免触发地图菜单。
		bool isPointerOverInterface = StrategyMapHudController.IsPointerOverHud()
			|| StrategySpeedUpController.IsPointerOverSpeedButton();

		if (Input.GetMouseButtonDown(0) && isPointerOverInterface) {
			isInterfacePointerDown = true;
			return;
		}

		if (isInterfacePointerDown) {
			if (Input.GetMouseButtonUp(0)) {
				isInterfacePointerDown = false;
				isPointerDownTracked = false;
			}
			return;
		}

		if (!Input.GetMouseButton(0) && isPointerOverInterface) {
			return;
		}

		// 2. 鼠标按下时记录起点，用于区分点击和拖动。
		if (Input.GetMouseButtonDown(0)) {
			
			isMouseMove = false;
			isPointerDownTracked = true;
			mouseDownPos = Input.mousePosition;
			
		} else if (isPointerDownTracked && !isMouseMove && Input.GetMouseButtonUp(0)) {
			Vector3 releaseOffset = GetCameraDragOffset(mouseDownPos, Input.mousePosition);
			if (IsCameraDragOffset(releaseOffset)) {
				isPointerDownTracked = false;
				isMouseMove = true;
				ApplyCameraDragOffset(releaseOffset);
				Input.ResetInputAxes();
				return;
			}

			// 3. 鼠标松开且没有拖动时，按点击目标打开城市、军队或主菜单。
			isPointerDownTracked = false;
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
		} else if (isPointerDownTracked && Input.GetMouseButtonUp(0)) {
			isPointerDownTracked = false;
			Input.ResetInputAxes();
		} else if (isPointerDownTracked && Input.GetMouseButton(0)) {

			// 4. 鼠标拖动时移动战略地图相机。
			if (!isMouseMove) {
				
				Vector3 offset = GetCameraDragOffset(mouseDownPos, Input.mousePosition);
				
				if (IsCameraDragOffset(offset)) {
					isMouseMove = true;
					ApplyCameraDragOffset(offset);
					mouseDownPos = Input.mousePosition;
				}
			} else {
				
				Vector3 offset = GetCameraDragOffset(mouseDownPos, Input.mousePosition);
				mouseDownPos = Input.mousePosition;
				ApplyCameraDragOffset(offset);
			}
		}
	}

	// 方法说明：判断屏幕移动换算出的世界偏移是否达到地图拖动阈值。
	// 参数说明：offset 为待判断的世界坐标偏移。
	// 返回说明：超过任一轴拖动阈值返回 true，否则返回 false。
	bool IsCameraDragOffset(Vector3 offset) {
		return Mathf.Abs(offset.x) > 5f || Mathf.Abs(offset.y) > 5f;
	}

	// 方法说明：立即应用一段地图拖动偏移并记录战略相机位置，保证单次拖动事件也能移动视野。
	// 参数说明：offset 为屏幕拖动换算后的世界坐标偏移。
	// 返回说明：无返回值。
	void ApplyCameraDragOffset(Vector3 offset) {
		if (Camera.main == null) return;

		Vector3 position = Camera.main.transform.position + offset;
		position = ClampCameraPosition(position);
		Camera.main.transform.position = position;
		strategyCamPos = position;
	}
	
	// 方法说明：处理战略时间流逝结束后的点击确认和进入内政。
	// 参数说明：无。
	// 返回说明：无返回值。
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
	
	// 方法说明：处理战争触发后的镜头移动和战斗武将选择场景切换。
	// 参数说明：无。
	// 返回说明：无返回值。
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

	// 方法说明：处理游戏失败对话结束后的失败场景切换。
	// 参数说明：无。
	// 返回说明：无返回值。
	void GameOverHandler() {
		if (dialog.IsShowingText()) {
			return;
		}

		if (Input.GetMouseButtonDown(0)) {
			Misc.LoadLevel("GameOver");
			state = State.ChangeScene;
		}
	}

	// 方法说明：处理游戏胜利对话结束后的胜利结算窗口。
	// 参数说明：无。
	// 返回说明：无返回值。
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

	// 方法说明：检查玩家君主存活和统一胜利状态。
	// 参数说明：无。
	// 返回说明：无返回值。
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

	// 方法说明：暂停战略地图交互、顶部时间条、旗帜和部队动画。
	// 参数说明：无。
	// 返回说明：无返回值。
	void SetGamePause() {
		
		StrategySpeedState.ApplyNormalTimeScale();
		state = State.Pause;

		hTimeCtrl.SetAnim(MenuDisplayAnim.AnimType.OutToLeft);
		
		flagsCtrl.SetFlagsAnimPause();
		SetArmyPause();
	}
	
	// 方法说明：在点击位置显示主菜单，并把菜单位置限制在相机视口内。
	// 参数说明：无。
	// 返回说明：无返回值。
	void ShowMainMenu() {
		ShowMainMenuAtScreenPosition(Input.mousePosition);
	}

	// 方法说明：在指定屏幕坐标显示原有战略主菜单，并把菜单限制在相机视口内。
	// 参数说明：screenPosition 为左下角原点的屏幕坐标。
	// 返回说明：无返回值。
	void ShowMainMenuAtScreenPosition(Vector3 screenPosition) {

		HideMainMenuChildPanels(null);
		BringStrategyMenuUiToFront();

		Vector3 camPos = Camera.main.transform.position;
		Vector3 pos = Camera.main.ScreenToWorldPoint(screenPosition);
		
		pos.x = Mathf.Clamp(pos.x, camPos.x - (640 - 182) / 2, camPos.x + (640 - 182) / 2);
		pos.y = Mathf.Clamp(pos.y, camPos.y - (480 - 213) / 2, camPos.y + (480 - 213) / 2);
		pos.z = mainMenuCtrl.transform.position.z;
		
		mainMenuCtrl.SetActive(true);
		mainMenuCtrl.transform.position = pos;
		BringStrategyMenuUiToFront();
	}

	// 方法说明：响应新版 HUD 菜单按钮，在屏幕中心打开原有战略主菜单。
	// 参数说明：无。
	// 返回说明：无返回值。
	public void ShowMainMenuFromHud() {
		if (state != State.Normal) return;

		SetGamePause();
		ShowMainMenuAtScreenPosition(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
	}

	// 方法说明：响应新版 HUD 势力按钮，直接打开原有势力地图界面。
	// 参数说明：无。
	// 返回说明：无返回值。
	public void OpenPowerMapFromHud() {
		if (state != State.Normal) return;

		MainMenu mainMenu = mainMenuCtrl == null ? null : mainMenuCtrl.GetComponent<MainMenu>();
		if (mainMenu == null || mainMenu.commandAct == null || mainMenu.commandAct.Length == 0 || mainMenu.commandAct[0] == null) {
			Debug.LogError("战略主菜单缺少势力地图入口，无法从新版 HUD 打开。");
			return;
		}

		SetGamePause();
		HideMainMenuChildPanels(mainMenu.commandAct[0]);
		mainMenuCtrl.SetActive(false);
		mainMenu.commandAct[0].SetActive(true);
		BringStrategyMenuPanelToFront(mainMenu.commandAct[0]);
	}

	// 方法说明：关闭战略主菜单的所有二级面板，只保留指定面板，避免菜单和势力地图底栏同时残留。
	// 参数说明：keepActive 为需要保留激活的面板；传 null 时全部关闭。
	// 返回说明：无返回值。
	void HideMainMenuChildPanels(GameObject keepActive) {
		if (mainMenuCtrl == null) return;

		MainMenu mainMenu = mainMenuCtrl.GetComponent<MainMenu>();
		if (mainMenu != null) {
			mainMenu.ResetMenuState();
			if (mainMenu.commandAct != null) {
				for (int i = 0; i < mainMenu.commandAct.Length; i++) {
					GameObject panel = mainMenu.commandAct[i];
					if (panel != null && panel != keepActive) {
						panel.SetActive(false);
						SetPowerMapSiblingMapActive(panel, false);
					}
				}
			}
		}
	}

	// 方法说明：同步隐藏或显示战略势力图挂在兄弟节点上的地图，避免二级地图残留覆盖主战略地图。
	// 参数说明：panel 为待检查的二级面板，active 为地图目标显示状态。
	// 返回说明：无返回值。
	void SetPowerMapSiblingMapActive(GameObject panel, bool active) {
		if (panel == null) return;

		SyPowerMap powerMap = panel.GetComponent<SyPowerMap>();
		if (powerMap == null || powerMap.map == null) return;

		powerMap.map.gameObject.SetActive(active);
	}

	// 方法说明：把战略主菜单和二级菜单固定到地图前景层。
	// 参数说明：无。
	// 返回说明：无返回值。
	void BringStrategyMenuUiToFront() {
		if (mainMenuCtrl == null) return;

		BringStrategyMenuPanelToFront(mainMenuCtrl);
		MainMenu mainMenu = mainMenuCtrl.GetComponent<MainMenu>();
		if (mainMenu == null || mainMenu.commandAct == null) return;

		for (int i = 0; i < mainMenu.commandAct.Length; i++) {
			BringStrategyMenuPanelToFront(mainMenu.commandAct[i]);
		}
	}

	// 方法说明：给单个战略菜单面板挂载前景层保持器，并立即应用前景 z 值。
	// 参数说明：panel 为目标菜单面板。
	// 返回说明：无返回值。
	void BringStrategyMenuPanelToFront(GameObject panel) {
		if (panel == null) return;

		StrategyCommandForegroundZKeeper keeper = panel.GetComponent<StrategyCommandForegroundZKeeper>();
		if (keeper == null) {
			keeper = panel.AddComponent<StrategyCommandForegroundZKeeper>();
		}
		keeper.SetTargetZ(-8f);
		keeper.ApplyNow();
	}

	// 方法说明：响应新版 HUD 主城按钮，把相机移到当前君主所在城池。
	// 参数说明：无。
	// 返回说明：无返回值。
	public void FocusPlayerCapitalFromHud() {
		if (state != State.Normal || Camera.main == null || pathfinding == null) return;

		int cityIdx = GetPlayerCapitalCityIndex();
		if (cityIdx < 0) {
			Debug.LogError("当前君主没有有效城池，无法定位主城。");
			return;
		}

		Vector3 cityPosition = pathfinding.GetCityPos(cityIdx);
		cityPosition.z = Camera.main.transform.position.z;
		Camera.main.transform.position = ClampCameraPosition(cityPosition);
		strategyCamPos = Camera.main.transform.position;
	}

	// 方法说明：响应新版 HUD 缩放按钮，调整正交相机尺寸并保持地图边界约束。
	// 参数说明：zoomMultiplier 小于 1 时放大地图，大于 1 时缩小地图。
	// 返回说明：无返回值。
	public void ZoomStrategyMapFromHud(float zoomMultiplier) {
		Camera camera = Camera.main;
		if (state != State.Normal || camera == null || !camera.orthographic) return;
		if (zoomMultiplier <= 0f) {
			Debug.LogError("战略地图缩放倍率必须大于 0。");
			return;
		}

		Bounds mapBounds = GetStrategyMapBounds();
		float maxZoom = Mathf.Min(mapBounds.extents.y, mapBounds.extents.x / camera.aspect);
		float minZoom = Mathf.Max(110f, maxZoom * 0.36f);
		camera.orthographicSize = Mathf.Clamp(camera.orthographicSize * zoomMultiplier, minZoom, maxZoom);
		camera.transform.position = ClampCameraPosition(camera.transform.position);
		strategyCamPos = camera.transform.position;
	}

	// 方法说明：读取当前君主所在城池，君主在外时使用其首座所属城池。
	// 参数说明：无。
	// 返回说明：返回有效城池索引，当前君主无城时返回 -1。
	int GetPlayerCapitalCityIndex() {
		if (Controller.kingIndex < 0 || Controller.kingIndex >= Informations.Instance.kingNum) return -1;

		KingInfo kingInfo = Informations.Instance.GetKingInfo(Controller.kingIndex);
		if (kingInfo == null) return -1;

		GeneralInfo rulerInfo = Informations.Instance.GetGeneralInfo(kingInfo.generalIdx);
		if (rulerInfo != null && rulerInfo.city >= 0 && rulerInfo.city < Informations.Instance.cityNum) {
			return rulerInfo.city;
		}

		return kingInfo.cities.Count > 0 ? kingInfo.cities[0] : -1;
	}
	
	// 方法说明：从暂停、菜单或选择状态恢复到战略地图普通状态。
	// 参数说明：无。
	// 返回说明：无返回值。
	public void ReturnMainMode() {
		
		state = State.Normal;
		StrategySpeedState.ApplyCurrentTimeScale();
		HideMainMenuChildPanels(null);
		if (mainMenuCtrl != null) {
			mainMenuCtrl.SetActive(false);
		}
		
		hTimeCtrl.SetAnim(MenuDisplayAnim.AnimType.InsertFromLeft);
		flagsCtrl.SetFlagsAnimResume();
		SetArmyResume();

		Misc.isBack = false;
		Misc.backButton.SetActive(false);
	}
	
	// 方法说明：进入每月时间结束状态并暂停地图对象。
	// 参数说明：无。
	// 返回说明：无返回值。
	public void OnTimeOver() {
		
		state = State.TimePass;
		
		flagsCtrl.SetFlagsAnimPause();
		SetArmyPause();
		
		dialog.SetDialogue(Informations.Instance.GetKingInfo(Controller.kingIndex).generalIdx, ZhongWen.Instance.niezhengzhongle, MenuDisplayAnim.AnimType.InsertFromBottom);
	}
	
	// 方法说明：暂停所有战略地图部队。
	// 参数说明：无。
	// 返回说明：无返回值。
	void SetArmyPause() {
		
		for (int i=0; i<Informations.Instance.armys.Count; i++) {
			
			Informations.Instance.armys[i].armyCtrl.Pause();
		}
	}
	
	// 方法说明：恢复所有战略地图部队。
	// 参数说明：无。
	// 返回说明：无返回值。
	void SetArmyResume() {
		
		for (int i=0; i<Informations.Instance.armys.Count; i++) {
			
			Informations.Instance.armys[i].armyCtrl.Resume();
		}
	}
	
	// 方法说明：修正城内武将过多的城池，把超额武将拆分成驻军部队。
	// 参数说明：无。
	// 返回说明：无返回值。
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
	
	// 方法说明：从武将列表中选出君主或武力最高者作为统帅。
	// 参数说明：generalList 为候选武将编号列表，king 为所属君主编号。
	// 返回说明：返回统帅武将编号。
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
	
	// 方法说明：初始化并生成战略地图上的所有部队对象。
	// 参数说明：无。
	// 返回说明：无返回值。
	void InitArmys() {
		
		root = GameObject.Find("ArmiesRoot");
		
		for (int i=0; i<Informations.Instance.armys.Count; i++) {
			
			SetArmy(Informations.Instance.armys[i]);
		}
	}
	
	// 方法说明：根据部队数据生成战略地图部队对象并绑定控制器。
	// 参数说明：armyInfo 为需要生成的部队数据。
	// 返回说明：无返回值。
	void SetArmy(ArmyInfo armyInfo) {
		
		if (armyInfo.pos == Vector3.zero) {
			armyInfo.pos = pathfinding.GetCityPos(armyInfo.cityFrom);
		}
		
		GameObject go = (GameObject)Instantiate(armyPrefab, armyInfo.pos, transform.rotation);
		go.transform.parent = root.transform;
		ArmyController armyCtrl = go.GetComponent<ArmyController>();
		
		armyCtrl.InitArmyInfo(armyInfo);
	}
	
	// 方法说明：玩家部队与敌军相遇时进入开战对话。
	// 参数说明：mine 为玩家部队，enemy 为敌军部队。
	// 返回说明：无返回值。
	public void SetWarDialogue(ArmyInfo mine, ArmyInfo enemy) {
		
		state = State.OnWar;
		
		Vector3 pos = mine.armyCtrl.transform.position;
		pos = ClampCameraPosition(pos);
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
	
	// 方法说明：玩家城池被敌军攻击时进入开战对话。
	// 参数说明：cityAttacked 为被攻击城池索引，enemy 为敌军部队。
	// 返回说明：无返回值。
	public void SetWarDialogue(int cityAttacked, ArmyInfo enemy) {
		
		state = State.OnWar;
		
		Vector3 pos = enemy.armyCtrl.transform.position;
		pos = ClampCameraPosition(pos);
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
	
	// 方法说明：玩家部队攻击城池时进入开战对话。
	// 参数说明：army 为玩家部队，cityAttacked 为被攻击城池索引。
	// 返回说明：无返回值。
	public void SetWarDialogue(ArmyInfo army, int cityAttacked) {
		
		state = State.OnWar;
		
		Vector3 pos = army.armyCtrl.transform.position;
		pos = ClampCameraPosition(pos);
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
	
	// 方法说明：为电脑势力城池按资金和上限增加预备兵。
	// 参数说明：无。
	// 返回说明：无返回值。
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
	
	// 方法说明：处理每月战略地图上的武将恢复、城池行动和部队行动。
	// 参数说明：无。
	// 返回说明：无返回值。
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
	
	// 方法说明：按武将属性恢复指定武将的体力和技力。
	// 参数说明：gIdx 为需要恢复的武将编号。
	// 返回说明：无返回值。
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
	
	// 方法说明：处理单座城池的月度行动，包括自动征兵和电脑势力调兵。
	// 参数说明：cIdx 为需要处理的城池编号。
	// 返回说明：无返回值。
	void CityAct(int cIdx) {

		CityInfo cInfo = Informations.Instance.GetCityInfo(cIdx);

		if (cInfo.king == -1 || cInfo.king == Informations.Instance.kingNum) {
			return;
		}

		// 1. 所有有主城池先按武力从高到低给城内武将自动征兵。
		AutoConscribeCityGenerals(cInfo);

		// 2. 玩家城池只自动补兵，不执行电脑势力的自动调兵和出征逻辑。
		if (cInfo.king == Controller.kingIndex) {
			return;
		}

		// 3. 电脑势力根据邻城状态决定是否支援或出征。
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

	// 方法说明：按武力从高到低为城内武将自动分配城池预备兵。
	// 参数说明：cInfo 为需要执行自动征兵的城池数据。
	// 返回说明：无返回值。
	void AutoConscribeCityGenerals(CityInfo cInfo) {

		if (cInfo.reservist <= 0 || cInfo.generals.Count == 0) {
			return;
		}

		List<int> sortedGenerals = new List<int>(cInfo.generals);
		sortedGenerals.Sort(CompareGeneralStrengthDescending);

		for (int i=0; i<sortedGenerals.Count; i++) {
			if (cInfo.reservist <= 0) {
				break;
			}

			FillGeneralSoldiers(Informations.Instance.GetGeneralInfo(sortedGenerals[i]), cInfo);
		}
	}

	// 方法说明：比较两个武将的武力值，用于自动征兵优先级排序。
	// 参数说明：leftIdx 为左侧武将编号，rightIdx 为右侧武将编号。
	// 返回说明：右侧武力更高时返回正数，左侧武力更高时返回负数，武力相同则按编号升序返回。
	int CompareGeneralStrengthDescending(int leftIdx, int rightIdx) {

		GeneralInfo leftInfo = Informations.Instance.GetGeneralInfo(leftIdx);
		GeneralInfo rightInfo = Informations.Instance.GetGeneralInfo(rightIdx);
		int strengthCompare = rightInfo.strength.CompareTo(leftInfo.strength);
		if (strengthCompare != 0) {
			return strengthCompare;
		}

		return leftIdx.CompareTo(rightIdx);
	}

	// 方法说明：使用城池预备兵为单个武将补满骑兵和步兵。
	// 参数说明：gInfo 为需要补兵的武将数据，cInfo 为提供预备兵的城池数据。
	// 返回说明：无返回值。
	void FillGeneralSoldiers(GeneralInfo gInfo, CityInfo cInfo) {

		FillGeneralKnight(gInfo, cInfo);
		if (cInfo.reservist <= 0) {
			return;
		}

		FillGeneralFootSoldier(gInfo, cInfo);
	}

	// 方法说明：使用城池预备兵为单个武将补充骑兵。
	// 参数说明：gInfo 为需要补骑兵的武将数据，cInfo 为提供预备兵的城池数据。
	// 返回说明：无返回值。
	void FillGeneralKnight(GeneralInfo gInfo, CityInfo cInfo) {

		if (gInfo.knightCur >= gInfo.knightMax) {
			return;
		}

		int soldierAdd = gInfo.knightMax - gInfo.knightCur;
		soldierAdd = Mathf.Clamp(soldierAdd, 0, cInfo.reservist);

		gInfo.knightCur += soldierAdd;
		cInfo.reservist -= soldierAdd;
	}

	// 方法说明：使用城池预备兵为单个武将补充步兵。
	// 参数说明：gInfo 为需要补步兵的武将数据，cInfo 为提供预备兵的城池数据。
	// 返回说明：无返回值。
	void FillGeneralFootSoldier(GeneralInfo gInfo, CityInfo cInfo) {

		if (gInfo.soldierCur >= gInfo.soldierMax) {
			return;
		}

		int soldierAdd = gInfo.soldierMax - gInfo.soldierCur;
		soldierAdd = Mathf.Clamp(soldierAdd, 0, cInfo.reservist);

		gInfo.soldierCur += soldierAdd;
		cInfo.reservist -= soldierAdd;
	}
	
	// 方法说明：按最大单支部队人数拆分并派出电脑势力部队。
	// 参数说明：fo 为出发城池，to 为目标城池，num 为计划出征武将数量，maxNum 为单支部队最大武将数量。
	// 返回说明：无返回值。
	void SetArmyMove(int fo, int to, int num, int maxNum) {
		
		while (num > 0) {
			SetArmyMove(fo, to, Mathf.Clamp(num, 1, maxNum));
			num -= maxNum;
		}
	}
	
	// 方法说明：从指定城池抽调武将、资金和俘虏，生成一支电脑势力移动部队。
	// 参数说明：fo 为出发城池，to 为目标城池，num 为本支部队出征武将数量。
	// 返回说明：无返回值。
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
	
	// 方法说明：处理单支部队的月度自动入城逻辑。
	// 参数说明：i 为 Informations.Instance.armys 中的部队索引。
	// 返回说明：无返回值。
	void ArmyAct(int i) {
		
		ArmyInfo armyInfo = Informations.Instance.armys[i];
		
		if (armyInfo.king != Controller.kingIndex && armyInfo.armyCtrl.GetState() == ArmyController.ArmyState.Garrison) {
			
			int cityCanIntoIdx = pathfinding.GetCityIndex(armyInfo.armyCtrl.transform.position, 30);
			if (cityCanIntoIdx != -1) {
				armyInfo.armyCtrl.IntoTheCity(cityCanIntoIdx);
			}
		}
	}

	// 方法说明：重置战略地图控制器的静态运行状态。
	// 参数说明：无。
	// 返回说明：无返回值。
	public static void Reset() {
		state = State.Normal;
		isFirstEnter = true;
		strategyCamPos = Vector3.zero;
	}
}
