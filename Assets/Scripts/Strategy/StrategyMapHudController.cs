using UnityEngine;

public class StrategyMapHudController : MonoBehaviour {

	private const float BaseScreenWidth = 1280f;
	private const float BaseScreenHeight = 720f;
	private const float RefreshInterval = 0.2f;

	private static readonly Color BorderColor = new Color(0.82f, 0.66f, 0.28f, 1f);
	private static readonly Color PanelColor = new Color(0.035f, 0.045f, 0.055f, 0.92f);
	private static readonly Color ButtonColor = new Color(0.11f, 0.15f, 0.18f, 0.96f);
	private static readonly Color ButtonHoverColor = new Color(0.20f, 0.25f, 0.25f, 0.98f);
	private static readonly Color ButtonActiveColor = new Color(0.38f, 0.16f, 0.10f, 1f);
	private static readonly Color TextColor = new Color(0.96f, 0.94f, 0.86f, 1f);

	private static StrategyMapHudController activeController;

	private StrategyController strategyController;
	private float nextRefreshTime = 0f;
	private string historyText = "";
	private string playerOverviewText = "";
	private bool historyErrorLogged = false;

	private Font hudFont;
	private Texture2D panelTexture;
	private Texture2D borderTexture;
	private Texture2D buttonTexture;
	private Texture2D buttonHoverTexture;
	private Texture2D buttonActiveTexture;
	private GUIStyle titleStyle;
	private GUIStyle primaryTextStyle;
	private GUIStyle buttonStyle;

	// 方法说明：组件启用时注册当前战略地图 HUD 实例。
	// 参数说明：无。
	// 返回说明：无返回值。
	void OnEnable() {
		activeController = this;
	}

	// 方法说明：组件停用时注销当前战略地图 HUD 实例。
	// 参数说明：无。
	// 返回说明：无返回值。
	void OnDisable() {
		if (activeController == this) {
			activeController = null;
		}
	}

	// 方法说明：按固定时间间隔刷新君主和选中城池数据。
	// 参数说明：无。
	// 返回说明：无返回值。
	void Update() {
		if (strategyController == null || Time.unscaledTime < nextRefreshTime) return;

		nextRefreshTime = Time.unscaledTime + RefreshInterval;
		RefreshCachedData();
	}

	// 方法说明：绘制战略地图顶部状态栏和缩放操作栏。
	// 参数说明：无。
	// 返回说明：无返回值。
	void OnGUI() {
		if (strategyController == null || StrategyController.state != StrategyController.State.Normal) return;

		GUI.depth = -1000;
		EnsureGuiResources();
		DrawTopBar();
		DrawZoomControls();
	}

	// 方法说明：销毁 HUD 运行时创建的纯色贴图。
	// 参数说明：无。
	// 返回说明：无返回值。
	void OnDestroy() {
		DestroyRuntimeTexture(panelTexture);
		DestroyRuntimeTexture(borderTexture);
		DestroyRuntimeTexture(buttonTexture);
		DestroyRuntimeTexture(buttonHoverTexture);
		DestroyRuntimeTexture(buttonActiveTexture);
	}

	// 方法说明：绑定战略地图控制器并初始化顶部 HUD 数据。
	// 参数说明：controller 为当前战略地图控制器。
	// 返回说明：无返回值。
	public void Initialize(StrategyController controller) {
		strategyController = controller;
		HideLegacyHistoryDisplay();
		RefreshCachedData();
	}

	// 方法说明：判断新版战略地图 HUD 是否已启用。
	// 参数说明：无。
	// 返回说明：存在可用 HUD 时返回 true，否则返回 false。
	public static bool IsActive() {
		return activeController != null && activeController.enabled && activeController.gameObject.activeInHierarchy;
	}

	// 方法说明：判断当前鼠标或触点是否位于新版 HUD 的交互区域。
	// 参数说明：无。
	// 返回说明：位于顶部状态栏或缩放按钮区域时返回 true，否则返回 false。
	public static bool IsPointerOverHud() {
		if (!IsActive() || StrategyController.state != StrategyController.State.Normal) return false;

		Vector2 guiPosition = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
		return activeController.ContainsHudPoint(guiPosition);
	}

	// 方法说明：判断一个 GUI 坐标是否命中新版 HUD 区域。
	// 参数说明：guiPosition 为左上角原点的 GUI 坐标。
	// 返回说明：命中任一 HUD 区域时返回 true，否则返回 false。
	private bool ContainsHudPoint(Vector2 guiPosition) {
		return GetTopBarRect().Contains(guiPosition)
			|| GetZoomControlsRect().Contains(guiPosition);
	}

	// 方法说明：刷新顶部君主概况数据缓存。
	// 参数说明：无。
	// 返回说明：无返回值。
	private void RefreshCachedData() {
		RefreshPlayerOverviewData();
	}

	// 方法说明：统计当前玩家君主的城池、武将、部队和金钱。
	// 参数说明：无。
	// 返回说明：无返回值。
	private void RefreshPlayerOverviewData() {
		if (Controller.kingIndex < 0 || Controller.kingIndex >= Informations.Instance.kingNum) return;

		KingInfo kingInfo = Informations.Instance.GetKingInfo(Controller.kingIndex);
		if (kingInfo == null) return;

		long totalMoney = 0;
		for (int i = 0; i < kingInfo.cities.Count; i++) {
			CityInfo cityInfo = Informations.Instance.GetCityInfo(kingInfo.cities[i]);
			if (cityInfo != null) totalMoney += cityInfo.money;
		}

		int armyCount = 0;
		for (int i = 0; i < Informations.Instance.armys.Count; i++) {
			ArmyInfo armyInfo = Informations.Instance.armys[i];
			if (armyInfo != null && armyInfo.king == Controller.kingIndex) armyCount++;
		}

		historyText = ReadHistoryText();
		playerOverviewText = string.Format(
			"{0}   城 {1}   武 {2}   军 {3}   金 {4}",
			ZhongWen.Instance.GetKingName(Controller.kingIndex),
			kingInfo.cities.Count,
			kingInfo.generals.Count,
			armyCount,
			totalMoney);
	}

	// 方法说明：读取旧时间控制器中的真实年月文本。
	// 参数说明：无。
	// 返回说明：成功返回当前年月文本，组件缺失时返回空字符串。
	private string ReadHistoryText() {
		if (strategyController.hTimeCtrl != null) {
			HistoryTimeController historyController = strategyController.hTimeCtrl.GetComponent<HistoryTimeController>();
			if (historyController != null && historyController.font != null) {
				return historyController.font.text;
			}
		}

		if (!historyErrorLogged) {
			historyErrorLogged = true;
			Debug.LogError("战略地图时间组件缺失，新版状态栏无法读取年月。");
		}
		return "";
	}

	// 方法说明：隐藏旧战略地图时间框的渲染对象，时间推进脚本继续运行。
	// 参数说明：无。
	// 返回说明：无返回值。
	private void HideLegacyHistoryDisplay() {
		if (strategyController == null || strategyController.hTimeCtrl == null) return;

		Renderer[] renderers = strategyController.hTimeCtrl.GetComponentsInChildren<Renderer>(true);
		for (int i = 0; i < renderers.Length; i++) {
			renderers[i].enabled = false;
		}
	}

	// 方法说明：创建或刷新 HUD 使用的字体、样式和纯色贴图。
	// 参数说明：无。
	// 返回说明：无返回值。
	private void EnsureGuiResources() {
		if (hudFont == null) {
			hudFont = UnifiedGameFontController.CreateChineseDynamicFont(36);
		}

		if (panelTexture == null) panelTexture = CreateColorTexture(PanelColor);
		if (borderTexture == null) borderTexture = CreateColorTexture(BorderColor);
		if (buttonTexture == null) buttonTexture = CreateColorTexture(ButtonColor);
		if (buttonHoverTexture == null) buttonHoverTexture = CreateColorTexture(ButtonHoverColor);
		if (buttonActiveTexture == null) buttonActiveTexture = CreateColorTexture(ButtonActiveColor);

		float scale = GetUiScale();
		titleStyle = CreateLabelStyle(Mathf.RoundToInt(22f * scale), TextColor, FontStyle.Bold);
		primaryTextStyle = CreateLabelStyle(Mathf.RoundToInt(18f * scale), TextColor, FontStyle.Normal);
		buttonStyle = CreateButtonStyle(Mathf.RoundToInt(17f * scale));
	}

	// 方法说明：绘制顶部年月、君主概况和速度控制。
	// 参数说明：无。
	// 返回说明：无返回值。
	private void DrawTopBar() {
		Rect rect = GetTopBarRect();
		float scale = GetUiScale();
		DrawPanel(rect);

		float padding = 14f * scale;
		float gap = 7f * scale;
		float buttonWidth = 66f * scale;
		float buttonHeight = rect.height - padding;
		Rect pauseRect = new Rect(rect.xMax - padding * 0.5f - buttonWidth, rect.y + padding * 0.5f, buttonWidth, buttonHeight);
		Rect speedRect = new Rect(pauseRect.x - gap - buttonWidth, pauseRect.y, buttonWidth, buttonHeight);
		Rect menuRect = new Rect(speedRect.x - gap - buttonWidth, pauseRect.y, buttonWidth, buttonHeight);
		Rect powerRect = new Rect(menuRect.x - gap - buttonWidth, pauseRect.y, buttonWidth, buttonHeight);
		Rect capitalRect = new Rect(powerRect.x - gap - buttonWidth, pauseRect.y, buttonWidth, buttonHeight);
		Rect titleRect = new Rect(rect.x + padding, rect.y + 3f * scale, 72f * scale, rect.height - 6f * scale);
		Rect dateRect = new Rect(titleRect.xMax + 10f * scale, titleRect.y, 175f * scale, titleRect.height);
		Rect overviewRect = new Rect(dateRect.xMax + 8f * scale, titleRect.y, capitalRect.x - dateRect.xMax - 16f * scale, titleRect.height);

		GUI.Label(titleRect, "战略", titleStyle);
		GUI.Label(dateRect, historyText, primaryTextStyle);
		if (overviewRect.width >= 170f * scale) {
			GUI.Label(overviewRect, playerOverviewText, primaryTextStyle);
		}

		if (GUI.Button(capitalRect, "主城", buttonStyle)) {
			strategyController.FocusPlayerCapitalFromHud();
		}

		if (GUI.Button(powerRect, "势力", buttonStyle)) {
			strategyController.OpenPowerMapFromHud();
		}

		if (GUI.Button(menuRect, "菜单", buttonStyle)) {
			strategyController.ShowMainMenuFromHud();
		}

		string speedText = StrategySpeedState.IsSpeedUp() ? "×2" : "×1";
		if (GUI.Button(speedRect, speedText, buttonStyle)) {
			StrategySpeedState.ToggleSpeedUp();
		}

		string pauseText = StrategySpeedState.IsPaused() ? "继续" : "暂停";
		if (GUI.Button(pauseRect, pauseText, buttonStyle)) {
			StrategySpeedState.TogglePaused();
		}
	}

	// 方法说明：绘制地图放大和缩小按钮。
	// 参数说明：无。
	// 返回说明：无返回值。
	private void DrawZoomControls() {
		Rect rect = GetZoomControlsRect();
		float scale = GetUiScale();
		float buttonHeight = 43f * scale;
		DrawPanel(rect);

		Rect zoomInRect = new Rect(rect.x + 4f * scale, rect.y + 4f * scale, rect.width - 8f * scale, buttonHeight);
		Rect zoomOutRect = new Rect(zoomInRect.x, zoomInRect.yMax + 4f * scale, zoomInRect.width, buttonHeight);
		if (GUI.Button(zoomInRect, "+", buttonStyle)) {
			strategyController.ZoomStrategyMapFromHud(0.82f);
		}

		if (GUI.Button(zoomOutRect, "−", buttonStyle)) {
			strategyController.ZoomStrategyMapFromHud(1.22f);
		}
	}

	// 方法说明：绘制带金色细边的半透明面板。
	// 参数说明：rect 为面板矩形。
	// 返回说明：无返回值。
	private void DrawPanel(Rect rect) {
		GUI.DrawTexture(rect, borderTexture);
		GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f), panelTexture);
	}

	// 方法说明：创建 HUD 文本样式。
	// 参数说明：fontSize 为字号，color 为文字颜色，fontStyle 为字形。
	// 返回说明：返回配置完成的 GUIStyle。
	private GUIStyle CreateLabelStyle(int fontSize, Color color, FontStyle fontStyle) {
		GUIStyle style = new GUIStyle(GUI.skin.label);
		style.font = hudFont;
		style.fontSize = fontSize;
		style.fontStyle = fontStyle;
		style.alignment = TextAnchor.MiddleLeft;
		style.normal.textColor = color;
		style.padding = new RectOffset(0, 0, 0, 0);
		style.wordWrap = false;
		return style;
	}

	// 方法说明：创建 HUD 命令按钮样式。
	// 参数说明：fontSize 为按钮字号。
	// 返回说明：返回配置完成的 GUIStyle。
	private GUIStyle CreateButtonStyle(int fontSize) {
		GUIStyle style = new GUIStyle(GUI.skin.button);
		style.font = hudFont;
		style.fontSize = fontSize;
		style.alignment = TextAnchor.MiddleCenter;
		style.normal.background = buttonTexture;
		style.hover.background = buttonHoverTexture;
		style.active.background = buttonActiveTexture;
		style.focused.background = buttonHoverTexture;
		style.normal.textColor = TextColor;
		style.hover.textColor = Color.white;
		style.active.textColor = Color.white;
		style.focused.textColor = Color.white;
		style.border = new RectOffset(0, 0, 0, 0);
		return style;
	}

	// 方法说明：创建一张用于 IMGUI 面板或按钮的单色贴图。
	// 参数说明：color 为贴图颜色。
	// 返回说明：返回运行时创建的 1x1 贴图。
	private Texture2D CreateColorTexture(Color color) {
		Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
		texture.hideFlags = HideFlags.HideAndDontSave;
		texture.SetPixel(0, 0, color);
		texture.Apply();
		return texture;
	}

	// 方法说明：销毁单个运行时贴图。
	// 参数说明：texture 为需要销毁的贴图。
	// 返回说明：无返回值。
	private void DestroyRuntimeTexture(Texture2D texture) {
		if (texture == null) return;

		if (Application.isPlaying) {
			Destroy(texture);
		} else {
			DestroyImmediate(texture);
		}
	}

	// 方法说明：根据当前分辨率计算 HUD 统一缩放比例。
	// 参数说明：无。
	// 返回说明：返回限制在 0.72 到 1.8 之间的缩放比例。
	private static float GetUiScale() {
		return Mathf.Clamp(Mathf.Min(Screen.width / BaseScreenWidth, Screen.height / BaseScreenHeight), 0.72f, 1.8f);
	}

	// 方法说明：计算顶部状态栏矩形。
	// 参数说明：无。
	// 返回说明：返回屏幕左上方状态栏矩形。
	private static Rect GetTopBarRect() {
		float scale = GetUiScale();
		float margin = 10f * scale;
		return new Rect(margin, margin, Screen.width - margin * 2f, 60f * scale);
	}

	// 方法说明：计算地图缩放按钮区域矩形。
	// 参数说明：无。
	// 返回说明：返回顶部状态栏下方的缩放控制矩形。
	private static Rect GetZoomControlsRect() {
		float scale = GetUiScale();
		float margin = 10f * scale;
		Rect anchorRect = GetTopBarRect();
		float width = 50f * scale;
		return new Rect(Screen.width - margin - width, anchorRect.yMax + 10f * scale, width, 98f * scale);
	}

}
