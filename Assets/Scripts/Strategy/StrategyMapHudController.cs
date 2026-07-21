using UnityEngine;

public class StrategyMapHudController : MonoBehaviour {

	private const float BaseScreenWidth = 1280f;
	private const float BaseScreenHeight = 720f;
	private const float RefreshInterval = 0.2f;
	private const float CityMarkerWorldPadding = 0f;

	private static readonly Color GlassColor = new Color(0.035f, 0.055f, 0.070f, 0.82f);
	private static readonly Color GlassStrongColor = new Color(0.020f, 0.030f, 0.040f, 0.92f);
	private static readonly Color AccentColor = new Color(0.20f, 0.78f, 0.86f, 1f);
	private static readonly Color AccentGoldColor = new Color(0.95f, 0.70f, 0.28f, 1f);
	private static readonly Color PlayerCityColor = new Color(0.20f, 0.78f, 0.86f, 0.96f);
	private static readonly Color EnemyCityColor = new Color(1.00f, 0.38f, 0.28f, 0.96f);
	private static readonly Color NeutralCityColor = new Color(0.82f, 0.86f, 0.78f, 0.90f);
	private static readonly Color LabelTextColor = new Color(0.96f, 0.98f, 1f, 1f);
	private static readonly Color MutedTextColor = new Color(0.74f, 0.84f, 0.88f, 1f);
	private static readonly Color ButtonColor = new Color(0.075f, 0.12f, 0.145f, 0.92f);
	private static readonly Color ButtonHoverColor = new Color(0.10f, 0.22f, 0.26f, 0.96f);
	private static readonly Color ButtonActiveColor = new Color(0.15f, 0.38f, 0.43f, 0.98f);
	private static readonly Color DangerButtonColor = new Color(0.30f, 0.12f, 0.10f, 0.94f);

	private static StrategyMapHudController activeController;

	private StrategyController strategyController;
	private float nextRefreshTime = 0f;
	private string historyText = "";
	private string playerOverviewText = "";
	private bool historyErrorLogged = false;

	private Font hudFont;
	private Texture2D glassTexture;
	private Texture2D glassStrongTexture;
	private Texture2D accentTexture;
	private Texture2D goldTexture;
	private Texture2D buttonTexture;
	private Texture2D buttonHoverTexture;
	private Texture2D buttonActiveTexture;
	private Texture2D dangerButtonTexture;
	private Texture2D playerCityTexture;
	private Texture2D enemyCityTexture;
	private Texture2D neutralCityTexture;
	private Texture2D labelTexture;
	private GUIStyle titleStyle;
	private GUIStyle primaryTextStyle;
	private GUIStyle mutedTextStyle;
	private GUIStyle buttonStyle;
	private GUIStyle dangerButtonStyle;
	private GUIStyle cityLabelStyle;
	private GameObject worldOverlayRoot;
	private SpriteRenderer worldTopPanelRenderer;
	private SpriteRenderer worldTopAccentRenderer;
	private SpriteRenderer worldZoomPanelRenderer;
	private SpriteRenderer worldBackPanelRenderer;
	private TextMesh worldTitleText;
	private TextMesh worldDateText;
	private TextMesh worldOverviewText;
	private TextMesh[] worldTopButtonTexts;
	private TextMesh worldZoomInText;
	private TextMesh worldZoomOutText;
	private TextMesh worldBackText;
	private SpriteRenderer[] worldCityMarkerRenderers;
	private SpriteRenderer[] worldCityPillRenderers;
	private TextMesh[] worldCityTexts;
	private Material glassMaterial;
	private Material glassStrongMaterial;
	private Material accentMaterial;
	private Material goldMaterial;
	private Material playerCityMaterial;
	private Material enemyCityMaterial;
	private Material neutralCityMaterial;
	private Material cityPillMaterial;
	private Material textMaterial;
	private Sprite worldWhiteSprite;

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

	// 方法说明：刷新战略 HUD 数据并持续压制旧 UI 渲染。
	// 参数说明：无。
	// 返回说明：无返回值。
	void Update() {
		EnsureControllerBinding();
		HideLegacyMapChrome();
		EnsureWorldOverlay();
		if (strategyController == null || Time.unscaledTime < nextRefreshTime) return;

		nextRefreshTime = Time.unscaledTime + RefreshInterval;
		RefreshCachedData();
		UpdateWorldOverlay();
	}

	// 方法说明：绘制现代战略地图 HUD、城市标记和返回按钮。
	// 参数说明：无。
	// 返回说明：无返回值。
	void OnGUI() {
		EnsureControllerBinding();
		HideLegacyMapChrome();
		if (!ShouldUseModernStrategyMapUi() || strategyController == null || !ShouldDrawStrategyHud()) return;

		EnsureGuiResources();
		UpdateWorldOverlay();
		HandleHudGuiClick();
	}

	// 方法说明：销毁 HUD 运行时创建的纯色贴图。
	// 参数说明：无。
	// 返回说明：无返回值。
	void OnDestroy() {
		DestroyWorldOverlay();
		DestroyRuntimeTexture(glassTexture);
		DestroyRuntimeTexture(glassStrongTexture);
		DestroyRuntimeTexture(accentTexture);
		DestroyRuntimeTexture(goldTexture);
		DestroyRuntimeTexture(buttonTexture);
		DestroyRuntimeTexture(buttonHoverTexture);
		DestroyRuntimeTexture(buttonActiveTexture);
		DestroyRuntimeTexture(dangerButtonTexture);
		DestroyRuntimeTexture(playerCityTexture);
		DestroyRuntimeTexture(enemyCityTexture);
		DestroyRuntimeTexture(neutralCityTexture);
		DestroyRuntimeTexture(labelTexture);
	}

	// 方法说明：绑定战略地图控制器并初始化顶部 HUD 数据。
	// 参数说明：controller 为当前战略地图控制器。
	// 返回说明：无返回值。
	public void Initialize(StrategyController controller) {
		strategyController = controller;
		HideLegacyMapChrome();
		RefreshCachedData();
	}

	// 方法说明：供编辑器截图或场景强制初始化后立即刷新世界空间 HUD。
	// 参数说明：无。
	// 返回说明：无返回值。
	public void RefreshForCameraCapture() {
		EnsureControllerBinding();
		HideLegacyMapChrome();
		RefreshCachedData();
		EnsureWorldOverlay();
		UpdateWorldOverlay();
	}

	// 方法说明：判断当前 MOD 是否使用现代战略地图 UI。
	// 参数说明：无。
	// 返回说明：恢复版三国二 MOD 启用时返回 true，否则返回 false。
	public static bool ShouldUseModernStrategyMapUi() {
		return MODLoadController.IsRestoredSango2Index(Controller.MODSelect);
	}

	// 方法说明：判断新版战略地图 HUD 是否已启用。
	// 参数说明：无。
	// 返回说明：存在可用 HUD 时返回 true，否则返回 false。
	public static bool IsActive() {
		return activeController != null && activeController.enabled && activeController.gameObject.activeInHierarchy;
	}

	// 方法说明：判断当前鼠标或触点是否位于新版 HUD 的交互区域。
	// 参数说明：无。
	// 返回说明：位于顶部状态栏、缩放按钮或返回按钮区域时返回 true，否则返回 false。
	public static bool IsPointerOverHud() {
		if (!ShouldUseModernStrategyMapUi() || !IsActive() || !activeController.ShouldDrawStrategyHud()) return false;

		Vector2 guiPosition = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
		return activeController.ContainsHudPoint(guiPosition);
	}

	// 方法说明：在执行顺序不稳定时重新查找并绑定战略地图控制器。
	// 参数说明：无。
	// 返回说明：成功绑定控制器时返回 true，否则返回 false。
	private bool EnsureControllerBinding() {
		if (strategyController != null) return true;

		// 1. 优先从同一对象获取，保持 StrategyController 主挂载路径不变。
		StrategyController controller = GetComponent<StrategyController>();
		if (controller == null) {
			// 2. 截图工具或场景恢复时可能先创建 HUD，兜底从当前场景查找控制器。
			controller = FindAnyObjectByType<StrategyController>();
		}

		// 3. 找到控制器后立即绑定并刷新缓存，避免第一帧仍露出旧 UI。
		if (controller != null) {
			Initialize(controller);
			return true;
		}

		return false;
	}

	// 方法说明：判断一个 GUI 坐标是否命中新版 HUD 区域。
	// 参数说明：guiPosition 为左上角原点的 GUI 坐标。
	// 返回说明：命中任一 HUD 区域时返回 true，否则返回 false。
	private bool ContainsHudPoint(Vector2 guiPosition) {
		return GetTopBarRect().Contains(guiPosition)
			|| GetZoomControlsRect().Contains(guiPosition)
			|| GetBackButtonRect().Contains(guiPosition);
	}

	// 方法说明：判断当前状态是否需要绘制战略地图现代 HUD。
	// 参数说明：无。
	// 返回说明：处于战略地图普通、暂停或时间流转状态时返回 true，否则返回 false。
	private bool ShouldDrawStrategyHud() {
		return StrategyController.state == StrategyController.State.Normal
			|| StrategyController.state == StrategyController.State.Pause
			|| StrategyController.state == StrategyController.State.TimePass;
	}

	// 方法说明：刷新顶部君主概况数据缓存。
	// 参数说明：无。
	// 返回说明：无返回值。
	private void RefreshCachedData() {
		RefreshPlayerOverviewData();
		UpdateWorldOverlay();
	}

	// 方法说明：创建相机可见的世界空间 HUD，保证截图和真机看到同一套新版 UI。
	// 参数说明：无。
	// 返回说明：无返回值。
	private void EnsureWorldOverlay() {
		if (!ShouldUseModernStrategyMapUi() || Camera.main == null) return;
		if (worldOverlayRoot != null) return;

		// 1. 创建挂在主相机下的覆盖层根节点，避开旧 UI 预制体。
		worldOverlayRoot = new GameObject("ModernStrategyMapWorldHud");
		worldOverlayRoot.transform.SetParent(Camera.main.transform, false);
		worldOverlayRoot.transform.localPosition = Vector3.zero;
		worldOverlayRoot.transform.localRotation = Quaternion.identity;

		// 2. 准备材质、顶部状态栏、缩放区和返回按钮。
		EnsureWorldMaterials();
		worldTopPanelRenderer = CreateWorldQuad("TopBarPanel", glassMaterial);
		worldTopAccentRenderer = CreateWorldQuad("TopBarAccent", accentMaterial);
		worldZoomPanelRenderer = CreateWorldQuad("ZoomPanel", glassMaterial);
		worldBackPanelRenderer = CreateWorldQuad("BackPanel", glassMaterial);
		worldTitleText = CreateWorldText("TitleText", "战略地图", 22, LabelTextColor, TextAnchor.MiddleLeft);
		worldDateText = CreateWorldText("DateText", historyText, 15, MutedTextColor, TextAnchor.MiddleLeft);
		worldOverviewText = CreateWorldText("OverviewText", playerOverviewText, 17, LabelTextColor, TextAnchor.MiddleLeft);
		worldTopButtonTexts = new TextMesh[] {
			CreateWorldText("CapitalButtonText", "主城", 16, LabelTextColor, TextAnchor.MiddleCenter),
			CreateWorldText("PowerButtonText", "势力", 16, LabelTextColor, TextAnchor.MiddleCenter),
			CreateWorldText("MenuButtonText", "菜单", 16, LabelTextColor, TextAnchor.MiddleCenter),
			CreateWorldText("SpeedButtonText", "×1", 16, LabelTextColor, TextAnchor.MiddleCenter),
			CreateWorldText("PauseButtonText", "暂停", 16, LabelTextColor, TextAnchor.MiddleCenter)
		};
		worldZoomInText = CreateWorldText("ZoomInText", "+", 20, LabelTextColor, TextAnchor.MiddleCenter);
		worldZoomOutText = CreateWorldText("ZoomOutText", "−", 20, LabelTextColor, TextAnchor.MiddleCenter);
		worldBackText = CreateWorldText("BackText", "返回", 20, LabelTextColor, TextAnchor.MiddleCenter);

		// 3. 为城池创建独立标记和标签，坐标仍读取原始城池坐标。
		worldCityMarkerRenderers = new SpriteRenderer[Informations.Instance.cityNum];
		worldCityPillRenderers = new SpriteRenderer[Informations.Instance.cityNum];
		worldCityTexts = new TextMesh[Informations.Instance.cityNum];
		for (int i = 0; i < Informations.Instance.cityNum; i++) {
			worldCityMarkerRenderers[i] = CreateWorldQuad("CityMarker_" + i, neutralCityMaterial);
			worldCityPillRenderers[i] = CreateWorldQuad("CityPill_" + i, cityPillMaterial);
			worldCityTexts[i] = CreateWorldText("CityText_" + i, ZhongWen.Instance.GetCityName(i), 15, LabelTextColor, TextAnchor.MiddleLeft);
		}

		// 4. 首帧立即排版，避免截图捕捉到空 HUD。
		UpdateWorldOverlay();
	}

	// 方法说明：创建世界空间 HUD 使用的 Unlit 材质。
	// 参数说明：无。
	// 返回说明：无返回值。
	private void EnsureWorldMaterials() {
		if (glassMaterial != null) return;

		glassMaterial = CreateWorldMaterial(GlassColor);
		glassStrongMaterial = CreateWorldMaterial(GlassStrongColor);
		accentMaterial = CreateWorldMaterial(AccentColor);
		goldMaterial = CreateWorldMaterial(AccentGoldColor);
		playerCityMaterial = CreateWorldMaterial(PlayerCityColor);
		enemyCityMaterial = CreateWorldMaterial(EnemyCityColor);
		neutralCityMaterial = CreateWorldMaterial(NeutralCityColor);
		cityPillMaterial = CreateWorldMaterial(new Color(0.02f, 0.035f, 0.045f, 0.72f));
		textMaterial = CreateWorldMaterial(Color.white);
	}

	// 方法说明：创建一个纯色世界空间材质。
	// 参数说明：color 为材质颜色。
	// 返回说明：返回配置为透明渲染队列的材质。
	private Material CreateWorldMaterial(Color color) {
		Shader shader = Shader.Find("Unlit/Transparent");
		if (shader == null) shader = Shader.Find("Sprites/Default");
		Material material = new Material(shader);
		material.hideFlags = HideFlags.HideAndDontSave;
		material.color = color;
		if (material.HasProperty("_MainTex")) {
			material.mainTexture = Texture2D.whiteTexture;
		}
		if (material.HasProperty("_SrcBlend")) {
			material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
		}
		if (material.HasProperty("_DstBlend")) {
			material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
		}
		if (material.HasProperty("_ZWrite")) {
			material.SetInt("_ZWrite", 0);
		}
		if (material.HasProperty("_Cull")) {
			material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
		}
		material.renderQueue = 5000;
		return material;
	}

	// 方法说明：创建世界空间 HUD 矩形块。
	// 参数说明：name 为对象名，material 为渲染材质。
	// 返回说明：返回矩形块的 MeshRenderer。
	private SpriteRenderer CreateWorldQuad(string name, Material material) {
		GameObject quad = new GameObject(name);
		quad.transform.SetParent(worldOverlayRoot.transform, false);
		quad.transform.localRotation = Quaternion.identity;
		SpriteRenderer renderer = quad.AddComponent<SpriteRenderer>();
		renderer.sprite = GetWorldWhiteSprite();
		renderer.color = material == null ? Color.white : material.color;
		renderer.sortingOrder = 30000;
		return renderer;
	}

	// 方法说明：获取世界空间 HUD 使用的 1x1 白色 Sprite。
	// 参数说明：无。
	// 返回说明：返回可被 SpriteRenderer 染色缩放的白色 Sprite。
	private Sprite GetWorldWhiteSprite() {
		if (worldWhiteSprite != null) return worldWhiteSprite;

		Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
		texture.hideFlags = HideFlags.HideAndDontSave;
		texture.SetPixel(0, 0, Color.white);
		texture.Apply();
		worldWhiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
		worldWhiteSprite.hideFlags = HideFlags.HideAndDontSave;
		return worldWhiteSprite;
	}

	// 方法说明：创建世界空间 HUD 文字。
	// 参数说明：name 为对象名，text 为文字内容，fontSize 为目标字号，color 为颜色，anchor 为对齐方式。
	// 返回说明：返回创建完成的 TextMesh。
	private TextMesh CreateWorldText(string name, string text, int fontSize, Color color, TextAnchor anchor) {
		GameObject textObject = new GameObject(name);
		textObject.transform.SetParent(worldOverlayRoot.transform, false);
		textObject.transform.localRotation = Quaternion.identity;
		TextMesh textMesh = textObject.AddComponent<TextMesh>();
		textMesh.text = text;
		textMesh.anchor = anchor;
		textMesh.alignment = TextAlignment.Left;
		textMesh.fontSize = 64;
		textMesh.richText = false;
		Font font = UnifiedGameFontController.CreateChineseDynamicFont(fontSize);
		if (font != null) {
			textMesh.font = font;
			MeshRenderer renderer = textObject.GetComponent<MeshRenderer>();
			renderer.sharedMaterial = font.material;
			renderer.sortingOrder = 30001;
			if (renderer.sharedMaterial != null) {
				if (renderer.sharedMaterial.HasProperty("_Cull")) {
					renderer.sharedMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
				}
				renderer.sharedMaterial.renderQueue = 5001;
			}
		}
		textMesh.color = color;
		return textMesh;
	}

	// 方法说明：刷新世界空间 HUD 的文本、位置和显隐状态。
	// 参数说明：无。
	// 返回说明：无返回值。
	private void UpdateWorldOverlay() {
		if (worldOverlayRoot == null || Camera.main == null) return;
		bool visible = ShouldUseModernStrategyMapUi() && ShouldDrawStrategyHud();
		worldOverlayRoot.SetActive(visible);
		if (!visible) return;

		UpdateTopBarWorldOverlay();
		UpdateZoomWorldOverlay();
		UpdateBackWorldOverlay();
		UpdateCityWorldOverlay();
	}

	// 方法说明：刷新世界空间顶部状态栏。
	// 参数说明：无。
	// 返回说明：无返回值。
	private void UpdateTopBarWorldOverlay() {
		Rect topRect = GetTopBarRect();
		PlaceWorldQuad(worldTopPanelRenderer, topRect, 5f);
		PlaceWorldQuad(worldTopAccentRenderer, new Rect(topRect.x, topRect.y, topRect.width, 3f * GetUiScale()), 4.99f);

		float scale = GetUiScale();
		PlaceWorldText(worldTitleText, new Vector2(topRect.x + 16f * scale, topRect.y + 20f * scale), 22, 4.98f);
		worldDateText.text = historyText;
		PlaceWorldText(worldDateText, new Vector2(topRect.x + 16f * scale, topRect.y + 47f * scale), 15, 4.98f);
		worldOverviewText.text = playerOverviewText;
		PlaceWorldText(worldOverviewText, new Vector2(topRect.x + 210f * scale, topRect.y + 34f * scale), 17, 4.98f);

		for (int i = 0; i < worldTopButtonTexts.Length; i++) {
			Rect buttonRect = GetTopBarButtonRect(i);
			worldTopButtonTexts[i].text = GetTopBarButtonText(i);
			PlaceWorldText(worldTopButtonTexts[i], buttonRect.center, 16, 4.97f);
		}
	}

	// 方法说明：刷新世界空间缩放控件。
	// 参数说明：无。
	// 返回说明：无返回值。
	private void UpdateZoomWorldOverlay() {
		Rect zoomRect = GetZoomControlsRect();
		worldZoomPanelRenderer.gameObject.SetActive(StrategyController.state == StrategyController.State.Normal);
		worldZoomInText.gameObject.SetActive(StrategyController.state == StrategyController.State.Normal);
		worldZoomOutText.gameObject.SetActive(StrategyController.state == StrategyController.State.Normal);
		if (StrategyController.state != StrategyController.State.Normal) return;

		float scale = GetUiScale();
		PlaceWorldQuad(worldZoomPanelRenderer, zoomRect, 5f);
		Rect zoomInRect = new Rect(zoomRect.x + 5f * scale, zoomRect.y + 5f * scale, zoomRect.width - 10f * scale, 42f * scale);
		Rect zoomOutRect = new Rect(zoomInRect.x, zoomInRect.yMax + 6f * scale, zoomInRect.width, 42f * scale);
		PlaceWorldText(worldZoomInText, zoomInRect.center, 20, 4.97f);
		PlaceWorldText(worldZoomOutText, zoomOutRect.center, 20, 4.97f);
	}

	// 方法说明：刷新世界空间返回按钮。
	// 参数说明：无。
	// 返回说明：无返回值。
	private void UpdateBackWorldOverlay() {
		bool showBack = ShouldDrawBackButton();
		worldBackPanelRenderer.gameObject.SetActive(showBack);
		worldBackText.gameObject.SetActive(showBack);
		if (!showBack) return;

		Rect backRect = GetBackButtonRect();
		PlaceWorldQuad(worldBackPanelRenderer, backRect, 5f);
		PlaceWorldText(worldBackText, backRect.center, 20, 4.97f);
	}

	// 方法说明：刷新世界空间城池标记和标签。
	// 参数说明：无。
	// 返回说明：无返回值。
	private void UpdateCityWorldOverlay() {
		for (int i = 0; i < worldCityMarkerRenderers.Length; i++) {
			UpdateCityWorldMarker(i);
		}
	}

	// 方法说明：刷新单个城池的现代标记，坐标严格来自原城池坐标。
	// 参数说明：cityIdx 为城池索引。
	// 返回说明：无返回值。
	private void UpdateCityWorldMarker(int cityIdx) {
		bool visible = Informations.Instance.HasCityPosition(cityIdx) && Camera.main != null;
		Vector3 screenPosition = Vector3.zero;
		if (visible) {
			Vector3 worldPosition = Informations.Instance.GetCityFlagWorldPosition(cityIdx);
			visible = IsWorldPositionInsideMap(worldPosition);
			screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
			visible = visible
				&& screenPosition.x > -80f && screenPosition.x < Screen.width + 80f
				&& screenPosition.y > -80f && screenPosition.y < Screen.height + 80f;
		}

		worldCityMarkerRenderers[cityIdx].gameObject.SetActive(visible);
		worldCityPillRenderers[cityIdx].gameObject.SetActive(visible);
		worldCityTexts[cityIdx].gameObject.SetActive(visible);
		if (!visible) return;

		float scale = GetUiScale();
		Vector2 guiPosition = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
		worldCityMarkerRenderers[cityIdx].color = GetCityMarkerWorldColor(cityIdx);
		PlaceWorldQuad(worldCityMarkerRenderers[cityIdx], new Rect(guiPosition.x - 8f * scale, guiPosition.y - 8f * scale, 16f * scale, 16f * scale), 4.95f);

		string cityName = ZhongWen.Instance.GetCityName(cityIdx);
		worldCityTexts[cityIdx].text = cityName;
		float pillWidth = Mathf.Max(48f * scale, cityName.Length * 20f * scale + 20f * scale);
		Rect pillRect = new Rect(guiPosition.x + 11f * scale, guiPosition.y - 14f * scale, pillWidth, 28f * scale);
		PlaceWorldQuad(worldCityPillRenderers[cityIdx], pillRect, 4.96f);
		PlaceWorldText(worldCityTexts[cityIdx], new Vector2(pillRect.x + 8f * scale, pillRect.center.y), 17, 4.94f);
	}

	// 方法说明：按照城池归属返回世界空间标记材质。
	// 参数说明：cityIdx 为城池索引。
	// 返回说明：返回玩家、敌方或中立城池材质。
	private Color GetCityMarkerWorldColor(int cityIdx) {
		CityInfo cityInfo = Informations.Instance.GetCityInfo(cityIdx);
		if (cityInfo == null || cityInfo.king < 0) return NeutralCityColor;
		return cityInfo.king == Controller.kingIndex ? PlayerCityColor : EnemyCityColor;
	}

	// 方法说明：把 GUI 矩形换算为相机子对象本地矩形块。
	// 参数说明：renderer 为目标渲染器，rect 为 GUI 像素矩形，localZ 为相机本地深度。
	// 返回说明：无返回值。
	private void PlaceWorldQuad(SpriteRenderer renderer, Rect rect, float localZ) {
		if (renderer == null || Camera.main == null) return;

		Vector3 center = GuiPointToCameraLocal(rect.center, localZ);
		Vector2 size = GuiSizeToCameraLocal(rect.size);
		renderer.transform.localPosition = center;
		renderer.transform.localScale = new Vector3(size.x, size.y, 1f);
	}

	// 方法说明：把 GUI 点位换算为相机子对象本地文字位置。
	// 参数说明：textMesh 为目标文字，guiPoint 为 GUI 像素点，fontSize 为目标字号，localZ 为相机本地深度。
	// 返回说明：无返回值。
	private void PlaceWorldText(TextMesh textMesh, Vector2 guiPoint, int fontSize, float localZ) {
		if (textMesh == null || Camera.main == null) return;

		textMesh.transform.localPosition = GuiPointToCameraLocal(guiPoint, localZ);
		textMesh.characterSize = Mathf.Max(0.01f, fontSize * GetCameraWorldHeight() / Screen.height / 12f);
		textMesh.transform.localRotation = Quaternion.identity;
	}

	// 方法说明：把 GUI 像素点换算为相机本地坐标。
	// 参数说明：guiPoint 为左上角原点像素点，localZ 为相机本地深度。
	// 返回说明：返回相机本地坐标。
	private Vector3 GuiPointToCameraLocal(Vector2 guiPoint, float localZ) {
		float width = GetCameraWorldWidth();
		float height = GetCameraWorldHeight();
		float x = (guiPoint.x / Screen.width - 0.5f) * width;
		float y = (0.5f - guiPoint.y / Screen.height) * height;
		return new Vector3(x, y, localZ);
	}

	// 方法说明：把 GUI 像素尺寸换算为相机本地尺寸。
	// 参数说明：guiSize 为 GUI 像素尺寸。
	// 返回说明：返回相机本地尺寸。
	private Vector2 GuiSizeToCameraLocal(Vector2 guiSize) {
		return new Vector2(guiSize.x / Screen.width * GetCameraWorldWidth(), guiSize.y / Screen.height * GetCameraWorldHeight());
	}

	// 方法说明：获取当前相机正交世界高度。
	// 参数说明：无。
	// 返回说明：返回相机视口世界高度。
	private float GetCameraWorldHeight() {
		return Camera.main.orthographicSize * 2f;
	}

	// 方法说明：获取当前相机正交世界宽度。
	// 参数说明：无。
	// 返回说明：返回相机视口世界宽度。
	private float GetCameraWorldWidth() {
		return GetCameraWorldHeight() * Camera.main.aspect;
	}

	// 方法说明：处理新版 HUD 的点击热区，不再依赖旧按钮缩放变色动画。
	// 参数说明：无。
	// 返回说明：无返回值。
	private void HandleHudGuiClick() {
		Event currentEvent = Event.current;
		if (currentEvent == null || currentEvent.type != EventType.MouseUp || currentEvent.button != 0) return;
		if (StrategyController.state != StrategyController.State.Normal && !GetBackButtonRect().Contains(currentEvent.mousePosition)) return;

		for (int i = 0; i < 5; i++) {
			if (GetTopBarButtonRect(i).Contains(currentEvent.mousePosition)) {
				ExecuteTopBarButton(i);
				currentEvent.Use();
				return;
			}
		}

		float scale = GetUiScale();
		Rect zoomRect = GetZoomControlsRect();
		Rect zoomInRect = new Rect(zoomRect.x + 5f * scale, zoomRect.y + 5f * scale, zoomRect.width - 10f * scale, 42f * scale);
		Rect zoomOutRect = new Rect(zoomInRect.x, zoomInRect.yMax + 6f * scale, zoomInRect.width, 42f * scale);
		if (zoomInRect.Contains(currentEvent.mousePosition)) {
			strategyController.ZoomStrategyMapFromHud(0.82f);
			currentEvent.Use();
		} else if (zoomOutRect.Contains(currentEvent.mousePosition)) {
			strategyController.ZoomStrategyMapFromHud(1.22f);
			currentEvent.Use();
		} else if (GetBackButtonRect().Contains(currentEvent.mousePosition) && ShouldDrawBackButton()) {
			Misc.isBack = true;
			currentEvent.Use();
		}
	}

	// 方法说明：执行顶部按钮命令。
	// 参数说明：buttonIndex 为按钮序号，0 主城、1 势力、2 菜单、3 速度、4 暂停。
	// 返回说明：无返回值。
	private void ExecuteTopBarButton(int buttonIndex) {
		if (strategyController == null) return;

		switch (buttonIndex) {
		case 0:
			strategyController.FocusPlayerCapitalFromHud();
			break;
		case 1:
			strategyController.OpenPowerMapFromHud();
			break;
		case 2:
			strategyController.ShowMainMenuFromHud();
			break;
		case 3:
			StrategySpeedState.ToggleSpeedUp();
			break;
		case 4:
			StrategySpeedState.TogglePaused();
			break;
		}
	}

	// 方法说明：销毁世界空间 HUD 对象和材质。
	// 参数说明：无。
	// 返回说明：无返回值。
	private void DestroyWorldOverlay() {
		if (worldOverlayRoot != null) Destroy(worldOverlayRoot);
		DestroyRuntimeMaterial(glassMaterial);
		DestroyRuntimeMaterial(glassStrongMaterial);
		DestroyRuntimeMaterial(accentMaterial);
		DestroyRuntimeMaterial(goldMaterial);
		DestroyRuntimeMaterial(playerCityMaterial);
		DestroyRuntimeMaterial(enemyCityMaterial);
		DestroyRuntimeMaterial(neutralCityMaterial);
		DestroyRuntimeMaterial(cityPillMaterial);
		DestroyRuntimeMaterial(textMaterial);
		if (worldWhiteSprite != null) {
			if (Application.isPlaying) {
				Destroy(worldWhiteSprite);
			} else {
				DestroyImmediate(worldWhiteSprite);
			}
		}
	}

	// 方法说明：销毁单个运行时材质。
	// 参数说明：material 为需要销毁的材质。
	// 返回说明：无返回值。
	private void DestroyRuntimeMaterial(Material material) {
		if (material == null) return;

		if (Application.isPlaying) {
			Destroy(material);
		} else {
			DestroyImmediate(material);
		}
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
			"{0}  城池 {1}  武将 {2}  部队 {3}  金 {4}",
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
		if (strategyController != null && strategyController.hTimeCtrl != null) {
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

	// 方法说明：隐藏旧战略地图时间框和旧返回按钮渲染，保留脚本逻辑继续运行。
	// 参数说明：无。
	// 返回说明：无返回值。
	private void HideLegacyMapChrome() {
		HideLegacyHistoryDisplay();
		HideLegacyBackButtonDisplay();
	}

	// 方法说明：隐藏旧战略地图时间框的渲染对象，时间推进脚本继续运行。
	// 参数说明：无。
	// 返回说明：无返回值。
	private void HideLegacyHistoryDisplay() {
		if (!ShouldUseModernStrategyMapUi()) return;

		if (strategyController != null && strategyController.hTimeCtrl != null) {
			HideRenderers(strategyController.hTimeCtrl.gameObject);
		}

		HistoryTimeController[] historyControllers = Resources.FindObjectsOfTypeAll<HistoryTimeController>();
		for (int i = 0; i < historyControllers.Length; i++) {
			if (historyControllers[i] != null) {
				HideRenderers(historyControllers[i].gameObject);
			}
		}
	}

	// 方法说明：隐藏旧返回按钮所有 Renderer，只保留 BackController 逻辑。
	// 参数说明：无。
	// 返回说明：无返回值。
	private void HideLegacyBackButtonDisplay() {
		if (!ShouldUseModernStrategyMapUi()) return;

		if (Misc.backButton != null) {
			HideRenderers(Misc.backButton);
		}
		BackController[] backControllers = Resources.FindObjectsOfTypeAll<BackController>();
		for (int i = 0; i < backControllers.Length; i++) {
			if (backControllers[i] != null) {
				HideRenderers(backControllers[i].gameObject);
			}
		}
	}

	// 方法说明：隐藏指定对象及其子对象的所有 Renderer。
	// 参数说明：target 为需要隐藏渲染层的对象。
	// 返回说明：无返回值。
	private void HideRenderers(GameObject target) {
		if (target == null) return;

		Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
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

		if (glassTexture == null) glassTexture = CreateColorTexture(GlassColor);
		if (glassStrongTexture == null) glassStrongTexture = CreateColorTexture(GlassStrongColor);
		if (accentTexture == null) accentTexture = CreateColorTexture(AccentColor);
		if (goldTexture == null) goldTexture = CreateColorTexture(AccentGoldColor);
		if (buttonTexture == null) buttonTexture = CreateColorTexture(ButtonColor);
		if (buttonHoverTexture == null) buttonHoverTexture = CreateColorTexture(ButtonHoverColor);
		if (buttonActiveTexture == null) buttonActiveTexture = CreateColorTexture(ButtonActiveColor);
		if (dangerButtonTexture == null) dangerButtonTexture = CreateColorTexture(DangerButtonColor);
		if (playerCityTexture == null) playerCityTexture = CreateColorTexture(PlayerCityColor);
		if (enemyCityTexture == null) enemyCityTexture = CreateColorTexture(EnemyCityColor);
		if (neutralCityTexture == null) neutralCityTexture = CreateColorTexture(NeutralCityColor);
		if (labelTexture == null) labelTexture = CreateColorTexture(new Color(0.02f, 0.035f, 0.045f, 0.76f));

		float scale = GetUiScale();
		titleStyle = CreateLabelStyle(Mathf.RoundToInt(22f * scale), LabelTextColor, FontStyle.Bold, TextAnchor.MiddleLeft);
		primaryTextStyle = CreateLabelStyle(Mathf.RoundToInt(17f * scale), LabelTextColor, FontStyle.Normal, TextAnchor.MiddleLeft);
		mutedTextStyle = CreateLabelStyle(Mathf.RoundToInt(13f * scale), MutedTextColor, FontStyle.Normal, TextAnchor.MiddleLeft);
		buttonStyle = CreateButtonStyle(Mathf.RoundToInt(16f * scale), buttonTexture);
		dangerButtonStyle = CreateButtonStyle(Mathf.RoundToInt(18f * scale), dangerButtonTexture);
		cityLabelStyle = CreateLabelStyle(Mathf.RoundToInt(15f * scale), LabelTextColor, FontStyle.Bold, TextAnchor.MiddleLeft);
	}

	// 方法说明：绘制顶部年月、君主概况和主操作按钮。
	// 参数说明：无。
	// 返回说明：无返回值。
	private void DrawTopBar() {
		Rect rect = GetTopBarRect();
		float scale = GetUiScale();
		DrawModernPanel(rect, AccentColor);

		// 1. 左侧显示战略模块标题和真实年月。
		float padding = 16f * scale;
		Rect titleRect = new Rect(rect.x + padding, rect.y + 6f * scale, 100f * scale, 22f * scale);
		Rect dateRect = new Rect(titleRect.x, titleRect.yMax + 4f * scale, 180f * scale, 20f * scale);
		GUI.Label(titleRect, "战略地图", titleStyle);
		GUI.Label(dateRect, historyText, mutedTextStyle);

		// 2. 中间显示当前势力概要，避免旧式横向杂乱文字。
		Rect overviewRect = new Rect(rect.x + 210f * scale, rect.y + 12f * scale, rect.width - 690f * scale, 36f * scale);
		if (overviewRect.width > 160f * scale) {
			GUI.Label(overviewRect, playerOverviewText, primaryTextStyle);
		}

		// 3. 右侧绘制现代操作按钮，替代原顶部老按钮。
		float buttonWidth = 70f * scale;
		float buttonHeight = 38f * scale;
		float gap = 8f * scale;
		Rect pauseRect = new Rect(rect.xMax - padding - buttonWidth, rect.y + 11f * scale, buttonWidth, buttonHeight);
		Rect speedRect = new Rect(pauseRect.x - gap - buttonWidth, pauseRect.y, buttonWidth, buttonHeight);
		Rect menuRect = new Rect(speedRect.x - gap - buttonWidth, pauseRect.y, buttonWidth, buttonHeight);
		Rect powerRect = new Rect(menuRect.x - gap - buttonWidth, pauseRect.y, buttonWidth, buttonHeight);
		Rect capitalRect = new Rect(powerRect.x - gap - buttonWidth, pauseRect.y, buttonWidth, buttonHeight);

		DrawTopBarButton(capitalRect, "主城", delegate { strategyController.FocusPlayerCapitalFromHud(); });
		DrawTopBarButton(powerRect, "势力", delegate { strategyController.OpenPowerMapFromHud(); });
		DrawTopBarButton(menuRect, "菜单", delegate { strategyController.ShowMainMenuFromHud(); });
		DrawTopBarButton(speedRect, StrategySpeedState.IsSpeedUp() ? "×2" : "×1", delegate { StrategySpeedState.ToggleSpeedUp(); });
		DrawTopBarButton(pauseRect, StrategySpeedState.IsPaused() ? "继续" : "暂停", delegate { StrategySpeedState.TogglePaused(); });
	}

	// 方法说明：绘制顶部操作按钮并执行点击回调。
	// 参数说明：rect 为按钮区域，text 为按钮文本，handler 为点击后执行的回调。
	// 返回说明：无返回值。
	private void DrawTopBarButton(Rect rect, string text, System.Action handler) {
		if (GUI.Button(rect, text, buttonStyle) && handler != null && StrategyController.state == StrategyController.State.Normal) {
			handler();
		}
	}

	// 方法说明：绘制地图放大和缩小按钮。
	// 参数说明：无。
	// 返回说明：无返回值。
	private void DrawZoomControls() {
		if (StrategyController.state != StrategyController.State.Normal) return;

		Rect rect = GetZoomControlsRect();
		float scale = GetUiScale();
		DrawModernPanel(rect, AccentColor);

		Rect zoomInRect = new Rect(rect.x + 5f * scale, rect.y + 5f * scale, rect.width - 10f * scale, 42f * scale);
		Rect zoomOutRect = new Rect(zoomInRect.x, zoomInRect.yMax + 6f * scale, zoomInRect.width, 42f * scale);
		if (GUI.Button(zoomInRect, "+", buttonStyle)) {
			strategyController.ZoomStrategyMapFromHud(0.82f);
		}

		if (GUI.Button(zoomOutRect, "−", buttonStyle)) {
			strategyController.ZoomStrategyMapFromHud(1.22f);
		}
	}

	// 方法说明：绘制现代返回按钮并触发原返回逻辑。
	// 参数说明：无。
	// 返回说明：无返回值。
	private void DrawBackButton() {
		if (!ShouldDrawBackButton()) return;

		Rect rect = GetBackButtonRect();
		DrawModernPanel(rect, AccentGoldColor);
		if (GUI.Button(new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, rect.height - 8f), "返回", dangerButtonStyle)) {
			Misc.isBack = true;
		}
	}

	// 方法说明：判断当前是否需要显示现代返回按钮。
	// 参数说明：无。
	// 返回说明：旧返回按钮激活或战略地图处于暂停态时返回 true，否则返回 false。
	private bool ShouldDrawBackButton() {
		return (Misc.backButton != null && Misc.backButton.activeSelf) || StrategyController.state == StrategyController.State.Pause;
	}

	// 方法说明：绘制屏幕空间现代城市标记和标签。
	// 参数说明：无。
	// 返回说明：无返回值。
	private void DrawCityOverlay() {
		if (!ShouldUseModernStrategyMapUi() || Camera.main == null) return;

		for (int i = 0; i < Informations.Instance.cityNum; i++) {
			DrawCityMarker(i);
		}
	}

	// 方法说明：绘制单个城市的现代标记和名称标签。
	// 参数说明：cityIdx 为城池索引。
	// 返回说明：无返回值。
	private void DrawCityMarker(int cityIdx) {
		if (!Informations.Instance.HasCityPosition(cityIdx)) return;

		Vector3 worldPosition = Informations.Instance.GetCityFlagWorldPosition(cityIdx);
		if (!IsWorldPositionInsideMap(worldPosition)) return;

		Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
		if (screenPosition.z < 0f) return;

		Vector2 guiPosition = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
		float scale = GetUiScale();
		float markerSize = 11f * scale;
		Texture2D markerTexture = GetCityMarkerTexture(cityIdx);
		Rect markerRect = new Rect(guiPosition.x - markerSize * 0.5f, guiPosition.y - markerSize * 0.5f, markerSize, markerSize);
		GUI.DrawTexture(new Rect(markerRect.x - 3f, markerRect.y - 3f, markerRect.width + 6f, markerRect.height + 6f), glassStrongTexture);
		GUI.DrawTexture(markerRect, markerTexture);

		string cityName = ZhongWen.Instance.GetCityName(cityIdx);
		Vector2 textSize = cityLabelStyle.CalcSize(new GUIContent(cityName));
		Rect labelRect = new Rect(guiPosition.x + 9f * scale, guiPosition.y - 12f * scale, textSize.x + 18f * scale, 24f * scale);
		GUI.DrawTexture(labelRect, labelTexture);
		GUI.Label(new Rect(labelRect.x + 8f * scale, labelRect.y, labelRect.width - 12f * scale, labelRect.height), cityName, cityLabelStyle);
	}

	// 方法说明：判断世界坐标是否位于恢复地图尺寸范围内。
	// 参数说明：worldPosition 为待判断世界坐标。
	// 返回说明：在地图范围内返回 true，否则返回 false。
	private bool IsWorldPositionInsideMap(Vector3 worldPosition) {
		float halfWidth = MODLoadController.RecoveredMapWorldWidth * 0.5f + CityMarkerWorldPadding;
		float halfHeight = MODLoadController.RecoveredMapWorldHeight * 0.5f + CityMarkerWorldPadding;
		return worldPosition.x >= -halfWidth && worldPosition.x <= halfWidth
			&& worldPosition.y >= -halfHeight && worldPosition.y <= halfHeight;
	}

	// 方法说明：按城池归属选择现代城市标记颜色贴图。
	// 参数说明：cityIdx 为城池索引。
	// 返回说明：返回玩家、敌方或中立城市标记贴图。
	private Texture2D GetCityMarkerTexture(int cityIdx) {
		CityInfo cityInfo = Informations.Instance.GetCityInfo(cityIdx);
		if (cityInfo == null || cityInfo.king < 0) return neutralCityTexture;
		return cityInfo.king == Controller.kingIndex ? playerCityTexture : enemyCityTexture;
	}

	// 方法说明：绘制现代玻璃感面板。
	// 参数说明：rect 为面板矩形，accentColor 为顶部强调线颜色。
	// 返回说明：无返回值。
	private void DrawModernPanel(Rect rect, Color accentColor) {
		GUI.DrawTexture(rect, glassTexture);
		GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 2f * GetUiScale()), GetAccentTexture(accentColor));
		GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1f * GetUiScale(), rect.width, 1f * GetUiScale()), glassStrongTexture);
	}

	// 方法说明：根据强调色返回对应的纯色贴图。
	// 参数说明：accentColor 为强调色。
	// 返回说明：青色返回 accentTexture，金色返回 goldTexture。
	private Texture2D GetAccentTexture(Color accentColor) {
		return Mathf.Approximately(accentColor.r, AccentGoldColor.r) ? goldTexture : accentTexture;
	}

	// 方法说明：创建 HUD 文本样式。
	// 参数说明：fontSize 为字号，color 为文字颜色，fontStyle 为字形，alignment 为文本对齐方式。
	// 返回说明：返回配置完成的 GUIStyle。
	private GUIStyle CreateLabelStyle(int fontSize, Color color, FontStyle fontStyle, TextAnchor alignment) {
		GUIStyle style = new GUIStyle(GUI.skin.label);
		style.font = hudFont;
		style.fontSize = fontSize;
		style.fontStyle = fontStyle;
		style.alignment = alignment;
		style.normal.textColor = color;
		style.padding = new RectOffset(0, 0, 0, 0);
		style.wordWrap = false;
		return style;
	}

	// 方法说明：创建 HUD 命令按钮样式。
	// 参数说明：fontSize 为按钮字号，normalTexture 为普通状态背景贴图。
	// 返回说明：返回配置完成的 GUIStyle。
	private GUIStyle CreateButtonStyle(int fontSize, Texture2D normalTexture) {
		GUIStyle style = new GUIStyle(GUI.skin.button);
		style.font = hudFont;
		style.fontSize = fontSize;
		style.alignment = TextAnchor.MiddleCenter;
		style.normal.background = normalTexture;
		style.hover.background = buttonHoverTexture;
		style.active.background = buttonActiveTexture;
		style.focused.background = buttonHoverTexture;
		style.normal.textColor = LabelTextColor;
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
		float margin = 14f * scale;
		return new Rect(margin, margin, Screen.width - margin * 2f, 64f * scale);
	}

	// 方法说明：计算地图缩放按钮区域矩形。
	// 参数说明：无。
	// 返回说明：返回顶部状态栏下方的缩放控制矩形。
	private static Rect GetZoomControlsRect() {
		float scale = GetUiScale();
		float margin = 14f * scale;
		Rect anchorRect = GetTopBarRect();
		float width = 54f * scale;
		return new Rect(Screen.width - margin - width, anchorRect.yMax + 12f * scale, width, 100f * scale);
	}

	// 方法说明：计算顶部状态栏按钮矩形。
	// 参数说明：buttonIndex 为按钮序号，0 主城、1 势力、2 菜单、3 速度、4 暂停。
	// 返回说明：返回对应按钮的 GUI 矩形。
	private static Rect GetTopBarButtonRect(int buttonIndex) {
		float scale = GetUiScale();
		Rect barRect = GetTopBarRect();
		float buttonWidth = 70f * scale;
		float buttonHeight = 38f * scale;
		float gap = 8f * scale;
		float padding = 16f * scale;
		float pauseX = barRect.xMax - padding - buttonWidth;
		float targetX = pauseX - (4 - buttonIndex) * (buttonWidth + gap);
		return new Rect(targetX, barRect.y + 11f * scale, buttonWidth, buttonHeight);
	}

	// 方法说明：获取顶部状态栏按钮文字。
	// 参数说明：buttonIndex 为按钮序号，0 主城、1 势力、2 菜单、3 速度、4 暂停。
	// 返回说明：返回当前按钮显示文字。
	private static string GetTopBarButtonText(int buttonIndex) {
		switch (buttonIndex) {
		case 0:
			return "主城";
		case 1:
			return "势力";
		case 2:
			return "菜单";
		case 3:
			return StrategySpeedState.IsSpeedUp() ? "×2" : "×1";
		case 4:
			return StrategySpeedState.IsPaused() ? "继续" : "暂停";
		default:
			return "";
		}
	}

	// 方法说明：计算现代返回按钮矩形。
	// 参数说明：无。
	// 返回说明：返回屏幕右下方返回按钮矩形。
	private static Rect GetBackButtonRect() {
		float scale = GetUiScale();
		float width = 116f * scale;
		float height = 52f * scale;
		float margin = 24f * scale;
		return new Rect(Screen.width - margin - width, Screen.height - margin - height, width, height);
	}
}
