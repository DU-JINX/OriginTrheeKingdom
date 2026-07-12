using UnityEngine;

public class UnifiedGameFontController : MonoBehaviour {

	private const int DynamicFontSize = 64;
	private const float ScanInterval = 0.2f;

	private static UnifiedGameFontController instance;
	private readonly System.Collections.Generic.List<UnifiedGameFontMirror> mirrors = new System.Collections.Generic.List<UnifiedGameFontMirror>();
	private readonly System.Collections.Generic.List<UnifiedBakedTextMirror> bakedTextMirrors = new System.Collections.Generic.List<UnifiedBakedTextMirror>();
	private Font gameFont;
	private float scanTimer;

	/// <summary>
	/// 方法说明：场景加载后创建全局字体控制器。
	/// 参数说明：无。
	/// 返回说明：无。
	/// </summary>
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void CreateRuntimeInstance() {
		EnsureInstance();
	}

	/// <summary>
	/// 方法说明：给编辑器截图流程主动刷新当前场景字体。
	/// 参数说明：无。
	/// 返回说明：无。
	/// </summary>
	public static void RefreshSceneFontsForPreview() {
		UnifiedGameFontController controller = EnsureInstance();
		controller.EnsureGameFontLoaded();
		controller.RefreshMirrors();
		controller.SyncMirrors();
	}

	/// <summary>
	/// 方法说明：创建或返回唯一的全局字体控制器。
	/// 参数说明：无。
	/// 返回说明：返回全局字体控制器实例。
	/// </summary>
	private static UnifiedGameFontController EnsureInstance() {
		if (instance != null) {
			return instance;
		}

		GameObject controllerObject = new GameObject("UnifiedGameFontController");
		instance = controllerObject.AddComponent<UnifiedGameFontController>();
		if (Application.isPlaying) {
			DontDestroyOnLoad(controllerObject);
		} else {
			controllerObject.hideFlags = HideFlags.DontSave;
		}
		return instance;
	}

	/// <summary>
	/// 方法说明：初始化单例和统一字体。
	/// 参数说明：无。
	/// 返回说明：无。
	/// </summary>
	private void Awake() {
		if (instance != null && instance != this) {
			Destroy(gameObject);
			return;
		}

		instance = this;
		gameFont = LoadGameFont();
	}

	/// <summary>
	/// 方法说明：定期扫描旧字体，并同步所有镜像字体状态。
	/// 参数说明：无。
	/// 返回说明：无。
	/// </summary>
	private void Update() {
		EnsureGameFontLoaded();

		scanTimer += Time.unscaledDeltaTime;
		if (scanTimer >= ScanInterval) {
			scanTimer = 0f;
			RefreshMirrors();
		}

		SyncMirrors();
	}

	/// <summary>
	/// 方法说明：确保统一字体已经加载。
	/// 参数说明：无。
	/// 返回说明：无。
	/// </summary>
	private void EnsureGameFontLoaded() {
		if (gameFont == null) {
			gameFont = LoadGameFont();
		}
	}

	/// <summary>
	/// 方法说明：加载全局统一中文动态字体。
	/// 参数说明：无。
	/// 返回说明：返回系统动态字体对象。
	/// </summary>
	private Font LoadGameFont() {
		return Font.CreateDynamicFontFromOSFont(new string[] { "PingFang SC", "Heiti SC", "Arial Unicode MS", "sans-serif" }, DynamicFontSize);
	}

	/// <summary>
	/// 方法说明：扫描当前场景所有旧字体并补齐动态字体镜像。
	/// 参数说明：无。
	/// 返回说明：无。
	/// </summary>
	private void RefreshMirrors() {
		EnsureGameFontLoaded();
		if (gameFont == null) {
			return;
		}

		exSpriteFont[] sceneFonts = Resources.FindObjectsOfTypeAll<exSpriteFont>();
		for (int i = 0; i < sceneFonts.Length; i++) {
			exSpriteFont sourceFont = sceneFonts[i];
			if (!ShouldMirrorFont(sourceFont)) {
				continue;
			}

			UnifiedGameFontMirror mirror = sourceFont.GetComponent<UnifiedGameFontMirror>();
			if (mirror == null) {
				mirror = sourceFont.gameObject.AddComponent<UnifiedGameFontMirror>();
				mirror.Initialize(sourceFont, gameFont);
				mirrors.Add(mirror);
			}
		}

		RefreshBakedTextMirrors();
	}

	/// <summary>
	/// 方法说明：判断指定旧字体是否需要接入统一字体镜像。
	/// 参数说明：sourceFont 为待检查的旧字体组件。
	/// 返回说明：需要镜像返回 true，否则返回 false。
	/// </summary>
	private bool ShouldMirrorFont(exSpriteFont sourceFont) {
		if (sourceFont == null || sourceFont.gameObject == null) {
			return false;
		}

		if (!sourceFont.gameObject.scene.IsValid()) {
			return false;
		}

		if (sourceFont.GetComponent<UnifiedGameFontMirror>() != null) {
			return true;
		}

		return !IsEffectivelyHidden(sourceFont);
	}

	/// <summary>
	/// 方法说明：判断旧字体当前是否已经被业务代码隐藏。
	/// 参数说明：sourceFont 为待检查的旧字体组件。
	/// 返回说明：隐藏返回 true，否则返回 false。
	/// </summary>
	private bool IsEffectivelyHidden(exSpriteFont sourceFont) {
		return sourceFont.topColor.a <= 0.05f && sourceFont.botColor.a <= 0.05f;
	}

	/// <summary>
	/// 方法说明：同步并清理所有统一字体镜像。
	/// 参数说明：无。
	/// 返回说明：无。
	/// </summary>
	private void SyncMirrors() {
		for (int i = mirrors.Count - 1; i >= 0; i--) {
			UnifiedGameFontMirror mirror = mirrors[i];
			if (mirror == null || !mirror.IsAlive()) {
				mirrors.RemoveAt(i);
				continue;
			}

			mirror.SyncNow();
		}

		for (int i = bakedTextMirrors.Count - 1; i >= 0; i--) {
			UnifiedBakedTextMirror mirror = bakedTextMirrors[i];
			if (mirror == null || !mirror.IsAlive()) {
				bakedTextMirrors.RemoveAt(i);
				continue;
			}

			mirror.SyncNow();
		}
	}

	/// <summary>
	/// 方法说明：扫描图片里烘入的固定标题字，并用动态字体覆盖。
	/// 参数说明：无。
	/// 返回说明：无。
	/// </summary>
	private void RefreshBakedTextMirrors() {
		if (gameFont == null) {
			return;
		}

		exSprite[] sprites = Resources.FindObjectsOfTypeAll<exSprite>();
		for (int i = 0; i < sprites.Length; i++) {
			exSprite sprite = sprites[i];
			UnifiedBakedTextDescriptor descriptor;
			if (!ShouldMirrorBakedText(sprite, out descriptor)) {
				continue;
			}

			UnifiedBakedTextMirror mirror = sprite.GetComponent<UnifiedBakedTextMirror>();
			if (mirror == null) {
				mirror = sprite.gameObject.AddComponent<UnifiedBakedTextMirror>();
				mirror.Initialize(sprite, gameFont, descriptor);
				bakedTextMirrors.Add(mirror);
			}
		}
	}

	/// <summary>
	/// 方法说明：判断图片对象是否包含需要替换的固定标题字。
	/// 参数说明：sprite 为待检查图片组件，descriptor 输出标题覆盖信息。
	/// 返回说明：需要覆盖返回 true，否则返回 false。
	/// </summary>
	private bool ShouldMirrorBakedText(exSprite sprite, out UnifiedBakedTextDescriptor descriptor) {
		descriptor = UnifiedBakedTextDescriptor.Empty;
		if (sprite == null || sprite.gameObject == null) {
			return false;
		}

		if (!sprite.gameObject.scene.IsValid()) {
			return false;
		}

		if (sprite.GetComponent<UnifiedBakedTextMirror>() != null) {
			return false;
		}

		Renderer spriteRenderer = sprite.GetComponent<Renderer>();
		if (spriteRenderer == null || spriteRenderer.sharedMaterial == null || spriteRenderer.sharedMaterial.mainTexture == null) {
			return false;
		}

		descriptor = UnifiedBakedTextDescriptor.FromTexture(spriteRenderer.sharedMaterial.mainTexture);
		return descriptor.valid;
	}
}

public struct UnifiedBakedTextDescriptor {
	public static readonly UnifiedBakedTextDescriptor Empty = new UnifiedBakedTextDescriptor("", false);

	public readonly string text;
	public readonly bool valid;

	/// <summary>
	/// 方法说明：创建图片标题覆盖描述。
	/// 参数说明：text 为要显示的新标题，valid 为描述是否有效。
	/// 返回说明：无。
	/// </summary>
	private UnifiedBakedTextDescriptor(string text, bool valid) {
		this.text = text;
		this.valid = valid;
	}

	/// <summary>
	/// 方法说明：根据图片纹理读取需要覆盖的新标题。
	/// 参数说明：texture 为图片纹理。
	/// 返回说明：返回匹配到的标题覆盖描述。
	/// </summary>
	public static UnifiedBakedTextDescriptor FromTexture(Texture texture) {
		if (texture == null) {
			return Empty;
		}

		switch (texture.name) {
			case "264-1":
				return new UnifiedBakedTextDescriptor("内政指令", true);
			case "269-1":
				return new UnifiedBakedTextDescriptor("物品使用", true);
			case "270-1":
				return new UnifiedBakedTextDescriptor("搜   索", true);
			case "272-1":
				return new UnifiedBakedTextDescriptor("筑   城", true);
			case "273-1":
				return new UnifiedBakedTextDescriptor("开   发", true);
			case "318-1":
				return new UnifiedBakedTextDescriptor("选择君主", true);
			case "340-1":
				return new UnifiedBakedTextDescriptor("选择武将", true);
			case "344-1":
				return new UnifiedBakedTextDescriptor("储存进度", true);
			case "359-1":
				return new UnifiedBakedTextDescriptor("选择俘虏", true);
			case "373-1":
				return new UnifiedBakedTextDescriptor("武将升迁", true);
			default:
				return Empty;
		}
	}
}

public class UnifiedBakedTextMirror : MonoBehaviour {

	private const int TitleFontSize = 64;
	private const float TitleFrontZ = -0.3f;
	private static readonly Color TitleColor = new Color(1f, 0.92f, 0.08f, 1f);
	private static readonly Color CoverColor = new Color(0f, 0f, 0f, 0.96f);

	private exSprite sourceSprite;
	private TextMesh titleText;
	private Renderer coverRenderer;
	private Font dynamicFont;
	private UnifiedBakedTextDescriptor descriptor;

	/// <summary>
	/// 方法说明：初始化图片固定文字的动态字体覆盖层。
	/// 参数说明：source 为原图片组件，font 为统一动态字体，textDescriptor 为标题覆盖描述。
	/// 返回说明：无。
	/// </summary>
	public void Initialize(exSprite source, Font font, UnifiedBakedTextDescriptor textDescriptor) {
		sourceSprite = source;
		dynamicFont = font;
		descriptor = textDescriptor;
		CreateCover();
		CreateTitle();
		SyncNow();
	}

	/// <summary>
	/// 方法说明：判断绑定的图片对象是否仍然存在。
	/// 参数说明：无。
	/// 返回说明：图片对象存在返回 true，否则返回 false。
	/// </summary>
	public bool IsAlive() {
		return sourceSprite != null;
	}

	/// <summary>
	/// 方法说明：同步覆盖层位置、尺寸和标题文字。
	/// 参数说明：无。
	/// 返回说明：无。
	/// </summary>
	public void SyncNow() {
		if (sourceSprite == null || !descriptor.valid) {
			return;
		}

		Texture texture = GetSourceTexture();
		if (texture == null) {
			return;
		}

		Vector2 titleCenter = GetTitleCenter(texture);
		Vector2 coverSize = GetCoverSize(texture);

		if (coverRenderer != null) {
			coverRenderer.transform.localPosition = new Vector3(titleCenter.x, titleCenter.y, TitleFrontZ);
			coverRenderer.transform.localScale = new Vector3(coverSize.x, coverSize.y, 1f);
		}

		if (titleText != null) {
			titleText.transform.localPosition = new Vector3(titleCenter.x, titleCenter.y, TitleFrontZ - 0.1f);
			titleText.text = descriptor.text;
			titleText.characterSize = GetTitleCharacterSize(texture, descriptor.text);
			if (dynamicFont != null) {
				dynamicFont.RequestCharactersInTexture(titleText.text, titleText.fontSize, titleText.fontStyle);
			}
		}
	}

	/// <summary>
	/// 方法说明：创建遮盖原图片标题字的深色底。
	/// 参数说明：无。
	/// 返回说明：无。
	/// </summary>
	private void CreateCover() {
		GameObject coverObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
		coverObject.name = "UnifiedBakedTextCover";
		coverObject.layer = gameObject.layer;
		coverObject.hideFlags = HideFlags.DontSave;
		coverObject.transform.SetParent(transform, false);

		Collider coverCollider = coverObject.GetComponent<Collider>();
		if (coverCollider != null) {
			if (Application.isPlaying) {
				Destroy(coverCollider);
			} else {
				DestroyImmediate(coverCollider);
			}
		}

		coverRenderer = coverObject.GetComponent<Renderer>();
		if (coverRenderer != null) {
			coverRenderer.sharedMaterial = CreateCoverMaterial();
		}
	}

	/// <summary>
	/// 方法说明：创建动态字体标题。
	/// 参数说明：无。
	/// 返回说明：无。
	/// </summary>
	private void CreateTitle() {
		GameObject titleObject = new GameObject("UnifiedBakedTextLabel");
		titleObject.layer = gameObject.layer;
		titleObject.hideFlags = HideFlags.DontSave;
		titleObject.transform.SetParent(transform, false);
		titleObject.transform.localRotation = Quaternion.identity;
		titleObject.transform.localScale = Vector3.one;

		titleText = titleObject.AddComponent<TextMesh>();
		titleText.font = dynamicFont;
		titleText.fontSize = TitleFontSize;
		titleText.fontStyle = FontStyle.Normal;
		titleText.anchor = TextAnchor.MiddleCenter;
		titleText.alignment = TextAlignment.Center;
		titleText.color = TitleColor;
		Renderer textRenderer = titleText.GetComponent<Renderer>();
		if (textRenderer != null && dynamicFont != null) {
			textRenderer.sharedMaterial = dynamicFont.material;
		}
	}

	/// <summary>
	/// 方法说明：创建标题遮盖底色材质。
	/// 参数说明：无。
	/// 返回说明：返回遮盖层材质。
	/// </summary>
	private Material CreateCoverMaterial() {
		Shader shader = Shader.Find("Unlit/Color");
		if (shader == null) {
			shader = Shader.Find("Sprites/Default");
		}

		Material material = new Material(shader);
		material.hideFlags = HideFlags.DontSave;
		material.color = CoverColor;
		return material;
	}

	/// <summary>
	/// 方法说明：读取原图片纹理。
	/// 参数说明：无。
	/// 返回说明：找到返回纹理，否则返回 null。
	/// </summary>
	private Texture GetSourceTexture() {
		Renderer spriteRenderer = sourceSprite == null ? null : sourceSprite.GetComponent<Renderer>();
		if (spriteRenderer == null || spriteRenderer.sharedMaterial == null) {
			return null;
		}

		return spriteRenderer.sharedMaterial.mainTexture;
	}

	/// <summary>
	/// 方法说明：计算标题覆盖中心点。
	/// 参数说明：texture 为原图片纹理。
	/// 返回说明：返回标题区域中心点。
	/// </summary>
	private Vector2 GetTitleCenter(Texture texture) {
		if (texture.height > 90) {
			return new Vector2(0f, texture.height * 0.5f - 29f);
		}

		return Vector2.zero;
	}

	/// <summary>
	/// 方法说明：计算遮盖原标题字的矩形尺寸。
	/// 参数说明：texture 为原图片纹理。
	/// 返回说明：返回遮盖矩形宽高。
	/// </summary>
	private Vector2 GetCoverSize(Texture texture) {
		float width = Mathf.Max(80f, texture.width - 34f);
		return new Vector2(width, 36f);
	}

	/// <summary>
	/// 方法说明：根据标题长度和图片宽度估算动态标题字号。
	/// 参数说明：texture 为原图片纹理，text 为标题文字。
	/// 返回说明：返回 TextMesh characterSize。
	/// </summary>
	private float GetTitleCharacterSize(Texture texture, string text) {
		float baseSize = texture.height > 90 ? 3.6f : 3.4f;
		if (!string.IsNullOrEmpty(text) && text.Length >= 5) {
			baseSize -= 0.25f;
		}

		return Mathf.Clamp(baseSize, 2.8f, 3.8f);
	}
}

public class UnifiedGameFontMirror : MonoBehaviour {

	private const float FrontOffsetZ = -0.2f;
	private const float ColorTolerance = 0.01f;
	private static readonly Color HiddenColor = new Color(1f, 1f, 1f, 0f);

	private exSpriteFont sourceFont;
	private TextMesh mirrorText;
	private Font dynamicFont;
	private string lastText;
	private string lastAnchorName;
	private Rect lastBounds;
	private Color displayColor = Color.white;

	/// <summary>
	/// 方法说明：初始化旧字体和新字体之间的显示镜像。
	/// 参数说明：source 为旧字体组件，font 为统一动态字体。
	/// 返回说明：无。
	/// </summary>
	public void Initialize(exSpriteFont source, Font font) {
		sourceFont = source;
		dynamicFont = font;
		displayColor = ResolveDisplayColor(sourceFont.topColor, sourceFont.botColor);
		CreateMirrorText();
		SyncNow();
	}

	/// <summary>
	/// 方法说明：判断镜像绑定的旧字体是否仍然存在。
	/// 参数说明：无。
	/// 返回说明：旧字体存在返回 true，否则返回 false。
	/// </summary>
	public bool IsAlive() {
		return sourceFont != null;
	}

	/// <summary>
	/// 方法说明：同步旧字体文字、颜色、锚点和尺寸到动态字体。
	/// 参数说明：无。
	/// 返回说明：无。
	/// </summary>
	public void SyncNow() {
		if (sourceFont == null) {
			return;
		}

		if (mirrorText == null) {
			CreateMirrorText();
		}

		// 1. 读取业务代码对旧字体做出的文字和颜色变化。
		CaptureSourceState();

		// 2. 把旧字体当前排版同步到新字体。
		ApplyMirrorState();

		// 3. 隐藏旧字体的像素字形，只保留旧组件的点击和状态能力。
		HideSourceGlyphs();
	}

	/// <summary>
	/// 方法说明：创建承载动态字体的 TextMesh。
	/// 参数说明：无。
	/// 返回说明：无。
	/// </summary>
	private void CreateMirrorText() {
		GameObject labelObject = new GameObject("UnifiedGameFontLabel");
		labelObject.layer = gameObject.layer;
		labelObject.hideFlags = HideFlags.DontSave;
		labelObject.transform.SetParent(transform, false);
		labelObject.transform.localPosition = new Vector3(0f, 0f, FrontOffsetZ);
		labelObject.transform.localRotation = Quaternion.identity;
		labelObject.transform.localScale = Vector3.one;

		mirrorText = labelObject.AddComponent<TextMesh>();
		mirrorText.font = dynamicFont;
		mirrorText.fontSize = 64;
		mirrorText.fontStyle = FontStyle.Normal;
		mirrorText.richText = false;
		mirrorText.lineSpacing = 1f;
		mirrorText.offsetZ = 0f;
		mirrorText.tabSize = 4f;
		Renderer textRenderer = mirrorText.GetComponent<Renderer>();
		if (textRenderer != null) {
			textRenderer.sharedMaterial = dynamicFont.material;
		}
	}

	/// <summary>
	/// 方法说明：捕获旧字体上由业务脚本写入的新文字和新颜色。
	/// 参数说明：无。
	/// 返回说明：无。
	/// </summary>
	private void CaptureSourceState() {
		if (!ColorsClose(sourceFont.topColor, HiddenColor) || !ColorsClose(sourceFont.botColor, HiddenColor)) {
			displayColor = ResolveDisplayColor(sourceFont.topColor, sourceFont.botColor);
		}

		lastText = sourceFont.text;
	}

	/// <summary>
	/// 方法说明：把缓存的文字、颜色、锚点和字号应用到动态字体。
	/// 参数说明：无。
	/// 返回说明：无。
	/// </summary>
	private void ApplyMirrorState() {
		if (mirrorText == null) {
			return;
		}

		string anchorName = sourceFont.anchor.ToString();
		Rect bounds = sourceFont.boundingRect;
		if (mirrorText.text != lastText) {
			mirrorText.text = lastText;
		}

		if (!string.IsNullOrEmpty(lastText) && dynamicFont != null) {
			dynamicFont.RequestCharactersInTexture(lastText, mirrorText.fontSize, mirrorText.fontStyle);
		}

		if (lastAnchorName != anchorName) {
			lastAnchorName = anchorName;
			mirrorText.anchor = ConvertAnchor(anchorName);
			mirrorText.alignment = ConvertAlignment(anchorName);
		}

		if (lastBounds != bounds) {
			lastBounds = bounds;
			mirrorText.characterSize = CalculateCharacterSize(bounds);
		}

		mirrorText.color = displayColor;
	}

	/// <summary>
	/// 方法说明：隐藏旧字体原始字形。
	/// 参数说明：无。
	/// 返回说明：无。
	/// </summary>
	private void HideSourceGlyphs() {
		sourceFont.topColor = HiddenColor;
		sourceFont.botColor = HiddenColor;
	}

	/// <summary>
	/// 方法说明：根据旧字体的包围盒估算动态字体字号。
	/// 参数说明：bounds 为旧字体当前包围盒。
	/// 返回说明：返回动态字体 characterSize。
	/// </summary>
	private float CalculateCharacterSize(Rect bounds) {
		float height = Mathf.Abs(bounds.height);
		if (height <= 0.01f) {
			height = 28f;
		}

		return Mathf.Clamp(height / 10f, 1.1f, 3.8f);
	}

	/// <summary>
	/// 方法说明：把旧字体锚点转换成 TextMesh 锚点。
	/// 参数说明：anchorName 为旧字体锚点名称。
	/// 返回说明：返回 TextMesh 使用的锚点。
	/// </summary>
	private TextAnchor ConvertAnchor(string anchorName) {
		bool left = anchorName.Contains("Left");
		bool right = anchorName.Contains("Right");
		bool top = anchorName.Contains("Top");
		bool bottom = anchorName.Contains("Bot") || anchorName.Contains("Bottom");

		if (top && left) return TextAnchor.UpperLeft;
		if (top && right) return TextAnchor.UpperRight;
		if (top) return TextAnchor.UpperCenter;
		if (bottom && left) return TextAnchor.LowerLeft;
		if (bottom && right) return TextAnchor.LowerRight;
		if (bottom) return TextAnchor.LowerCenter;
		if (left) return TextAnchor.MiddleLeft;
		if (right) return TextAnchor.MiddleRight;
		return TextAnchor.MiddleCenter;
	}

	/// <summary>
	/// 方法说明：把旧字体锚点转换成 TextMesh 横向对齐方式。
	/// 参数说明：anchorName 为旧字体锚点名称。
	/// 返回说明：返回 TextMesh 使用的横向对齐方式。
	/// </summary>
	private TextAlignment ConvertAlignment(string anchorName) {
		if (anchorName.Contains("Left")) {
			return TextAlignment.Left;
		}

		if (anchorName.Contains("Right")) {
			return TextAlignment.Right;
		}

		return TextAlignment.Center;
	}

	/// <summary>
	/// 方法说明：把旧字体上下渐变色合成为动态字体单色。
	/// 参数说明：topColor 为旧字体上半部分颜色，botColor 为旧字体下半部分颜色。
	/// 返回说明：返回动态字体显示颜色。
	/// </summary>
	private Color ResolveDisplayColor(Color topColor, Color botColor) {
		Color color = Color.Lerp(topColor, botColor, 0.5f);
		color.a = Mathf.Max(topColor.a, botColor.a);
		return color;
	}

	/// <summary>
	/// 方法说明：判断两个颜色是否足够接近。
	/// 参数说明：left 为第一个颜色，right 为第二个颜色。
	/// 返回说明：足够接近返回 true，否则返回 false。
	/// </summary>
	private bool ColorsClose(Color left, Color right) {
		return Mathf.Abs(left.r - right.r) <= ColorTolerance
			&& Mathf.Abs(left.g - right.g) <= ColorTolerance
			&& Mathf.Abs(left.b - right.b) <= ColorTolerance
			&& Mathf.Abs(left.a - right.a) <= ColorTolerance;
	}
}
