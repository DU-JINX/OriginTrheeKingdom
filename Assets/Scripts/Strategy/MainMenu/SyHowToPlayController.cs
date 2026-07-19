using UnityEngine;
using System.Collections;

public class SyHowToPlayController : MonoBehaviour {

	private const int HelpFontSize = 64;
	private const int HelpSortingOrder = 1500;
	private Font helpFont;
	
	public StrategyController strCtrl;
	
	// 方法说明：初始化战略帮助页内容。
	// 参数说明：无。
	// 返回说明：无返回值。
	void Start () {
		EnsureHelpContent();
	}

	// 方法说明：帮助页每次启用时确保完整操作说明存在。
	// 参数说明：无。
	// 返回说明：无返回值。
	void OnEnable() {
		EnsureHelpContent();
	}
	
	// 方法说明：处理帮助页返回操作。
	// 参数说明：无。
	// 返回说明：无返回值。
	void Update () {
		if (Misc.GetBack()) {
			gameObject.SetActive(false);
			
			strCtrl.ReturnMainMode();
		}
	}

	// 方法说明：创建战略帮助页标题与完整操作说明，已存在时只刷新内容。
	// 参数说明：无。
	// 返回说明：无返回值。
	void EnsureHelpContent() {
		HideLegacyHelpHint();

		if (helpFont == null) {
			helpFont = UnifiedGameFontController.CreateChineseDynamicFont(HelpFontSize);
		}

		TextMesh title = EnsureHelpLabel("StrategyHelpTitle", new Vector3(0f, 112f, 2.5f));
		title.text = "战略地图操作";
		title.characterSize = 2.35f;
		title.color = new Color(1f, 0.88f, 0.32f, 1f);
		title.anchor = TextAnchor.MiddleCenter;
		title.alignment = TextAlignment.Center;

		TextMesh content = EnsureHelpLabel("StrategyHelpContent", new Vector3(0f, 76f, 2.5f));
		content.text = "单击城池或部队：打开目标指令\n"
			+ "按住并拖动地图：移动战略视野\n"
			+ "＋／－：放大或缩小地图\n"
			+ "主城：定位当前君主主城\n"
			+ "势力：查看各君主势力\n"
			+ "菜单：寻找武将、帮助、返回主选单\n"
			+ "×1／×2、暂停：控制战略时间\n"
			+ "Esc 或系统返回键：返回上一页";
		content.characterSize = 1.55f;
		content.lineSpacing = 1.25f;
		content.color = Color.white;
		content.anchor = TextAnchor.UpperCenter;
		content.alignment = TextAlignment.Center;
	}

	// 方法说明：隐藏场景里旧版“返回: 多点触摸”提示，避免它压住新版帮助正文。
	// 参数说明：无。
	// 返回说明：无返回值。
	void HideLegacyHelpHint() {
		exSpriteFont[] legacyFonts = GetComponentsInChildren<exSpriteFont>(true);
		for (int i = 0; i < legacyFonts.Length; i++) {
			exSpriteFont legacyFont = legacyFonts[i];
			if (legacyFont == null) continue;

			if (legacyFont.GetComponent<UnifiedGameFontIgnore>() == null) {
				legacyFont.gameObject.AddComponent<UnifiedGameFontIgnore>();
			}
			UnifiedGameFontMirror mirror = legacyFont.GetComponent<UnifiedGameFontMirror>();
			if (mirror != null) {
				mirror.Dispose();
			}
			Renderer legacyRenderer = legacyFont.GetComponent<Renderer>();
			if (legacyRenderer != null) {
				legacyRenderer.enabled = false;
			}
		}
	}

	// 方法说明：查找或创建帮助页 TextMesh 标签并设置统一字体层级。
	// 参数说明：objectName 为标签对象名，localPosition 为标签本地坐标。
	// 返回说明：返回可直接写入内容的 TextMesh。
	TextMesh EnsureHelpLabel(string objectName, Vector3 localPosition) {
		Transform existing = transform.Find(objectName);
		TextMesh label;
		if (existing == null) {
			GameObject labelObject = new GameObject(objectName);
			labelObject.hideFlags = HideFlags.DontSave;
			labelObject.layer = gameObject.layer;
			labelObject.transform.SetParent(transform, false);
			label = labelObject.AddComponent<TextMesh>();
		} else {
			label = existing.GetComponent<TextMesh>();
			if (label == null) {
				label = existing.gameObject.AddComponent<TextMesh>();
			}
		}

		label.transform.localPosition = localPosition;
		label.transform.localRotation = Quaternion.identity;
		label.transform.localScale = Vector3.one;
		label.font = helpFont;
		label.fontSize = HelpFontSize;
		label.fontStyle = FontStyle.Normal;
		label.richText = false;
		Renderer labelRenderer = label.GetComponent<Renderer>();
		if (labelRenderer != null) {
			labelRenderer.sharedMaterial = helpFont.material;
			labelRenderer.sortingOrder = HelpSortingOrder;
		}
		return label;
	}
}
