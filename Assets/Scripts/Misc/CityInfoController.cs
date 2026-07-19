using UnityEngine;
using System.Collections;

public class CityInfoController : MonoBehaviour {

	private const int ReadableLabelFontSize = 64;
	private const int ReadableLabelSortingOrder = 1600;
	private GameObject readableLabelRoot;
	private Font readableLabelFont;
	
	public GameObject label;
	public exSpriteFont cityName;
	public exSpriteFont generalNum;
	public exSpriteFont reservist;
	public exSpriteFont defense;
	public exSpriteFont population;
	public exSpriteFont money;
	
	
	/// <summary>
	/// 方法说明：刷新当前选中城池的名称、武将、兵力、防御、人口和金钱信息。
	/// 参数说明：idx 为城池索引；索引无效时隐藏资料面板并清空数值。
	/// 返回说明：无返回值。
	/// </summary>
	public void SetCity(int idx) {
		EnsureReadableLabels();
		
		if (idx < 0 || idx >= Informations.Instance.cityNum) {
			label.SetActive(false);
			readableLabelRoot.SetActive(false);
			
			cityName.text = "";
			generalNum.text = "";
			reservist.text = "";
			defense.text = "";
			population.text = "";
			money.text = "";
			
			return;
		}
		
		label.SetActive(true);
		readableLabelRoot.SetActive(true);
		
		CityInfo cInfo = Informations.Instance.GetCityInfo(idx);
		
		cityName.text = ZhongWen.Instance.GetCityName(idx);
		
		generalNum.text = "" + cInfo.generals.Count;
		
		reservist.text = cInfo.reservist + "/" + cInfo.reservistMax;
		defense.text = cInfo.defense + "";
		population.text = cInfo.population + ZhongWen.Instance.ren;
		money.text = cInfo.money + "";
	}

	/// <summary>
	/// 方法说明：把依赖大量空格分列的旧城池标题替换成六个独立标签，避免动态字体列宽重叠。
	/// 参数说明：无。
	/// 返回说明：无。
	/// </summary>
	private void EnsureReadableLabels() {
		HideLegacyLabelRenderer();
		if (readableLabelRoot != null) return;

		Transform existing = transform.Find("ReadableCityInfoLabels");
		if (existing != null) {
			readableLabelRoot = existing.gameObject;
			return;
		}

		readableLabelFont = UnifiedGameFontController.CreateChineseDynamicFont(ReadableLabelFontSize);
		readableLabelRoot = new GameObject("ReadableCityInfoLabels");
		readableLabelRoot.hideFlags = HideFlags.DontSave;
		readableLabelRoot.layer = gameObject.layer;
		readableLabelRoot.transform.SetParent(transform, false);

		CreateReadableLabel("CityNameLabel", "城名", new Vector3(-270f, -140f, -0.4f));
		CreateReadableLabel("GeneralCountLabel", "武将", new Vector3(-270f, -172f, -0.4f));
		CreateReadableLabel("ReservistLabel", "预备兵", new Vector3(-270f, -204f, -0.4f));
		CreateReadableLabel("DefenseLabel", "防御力", new Vector3(-45f, -140f, -0.4f));
		CreateReadableLabel("PopulationLabel", "人口", new Vector3(-45f, -172f, -0.4f));
		CreateReadableLabel("MoneyLabel", "金钱", new Vector3(-45f, -204f, -0.4f));
	}

	/// <summary>
	/// 方法说明：隐藏旧版多行静态标题及其动态镜像。
	/// 参数说明：无。
	/// 返回说明：无。
	/// </summary>
	private void HideLegacyLabelRenderer() {
		if (label == null) return;

		exSpriteFont legacyFont = label.GetComponent<exSpriteFont>();
		if (legacyFont != null && legacyFont.GetComponent<UnifiedGameFontIgnore>() == null) {
			legacyFont.gameObject.AddComponent<UnifiedGameFontIgnore>();
		}
		if (legacyFont != null) {
			UnifiedGameFontMirror mirror = legacyFont.GetComponent<UnifiedGameFontMirror>();
			if (mirror != null) {
				mirror.Dispose();
			}
		}
		Renderer legacyRenderer = label.GetComponent<Renderer>();
		if (legacyRenderer != null) {
			legacyRenderer.enabled = false;
		}
	}

	/// <summary>
	/// 方法说明：创建单个城池资料静态标签。
	/// 参数说明：objectName 为对象名，text 为显示文字，localPosition 为标签本地坐标。
	/// 返回说明：返回创建出的 TextMesh。
	/// </summary>
	private TextMesh CreateReadableLabel(string objectName, string text, Vector3 localPosition) {
		GameObject labelObject = new GameObject(objectName);
		labelObject.hideFlags = HideFlags.DontSave;
		labelObject.layer = gameObject.layer;
		labelObject.transform.SetParent(readableLabelRoot.transform, false);
		labelObject.transform.localPosition = localPosition;

		TextMesh textMesh = labelObject.AddComponent<TextMesh>();
		textMesh.font = readableLabelFont;
		textMesh.fontSize = ReadableLabelFontSize;
		textMesh.characterSize = 1.8f;
		textMesh.anchor = TextAnchor.MiddleLeft;
		textMesh.alignment = TextAlignment.Left;
		textMesh.color = Color.white;
		textMesh.text = text;

		Renderer textRenderer = textMesh.GetComponent<Renderer>();
		if (textRenderer != null) {
			textRenderer.sharedMaterial = readableLabelFont.material;
			textRenderer.sortingOrder = ReadableLabelSortingOrder;
		}
		return textMesh;
	}
}
