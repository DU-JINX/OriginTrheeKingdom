using UnityEngine;
using System.Collections;

public class CCInformationController : MonoBehaviour {

	private const int ReadableInfoFontSize = 64;
	private const float ReadableInfoCharacterSize = 2.0f;
	private const int ReadableInfoSortingOrder = 1700;
	
	public GeneralsHeadSelect head;
	public exSpriteFont historyTime;
	public exSpriteFont prefect;
	public exSpriteFont kingName;
	public exSpriteFont cityName;
	public exSpriteFont defense;
	public exSpriteFont population;
	public exSpriteFont money;
	public exSpriteFont generalNum;
	public exSpriteFont soldierNum;
	public exSpriteFont reservistNum;
	public exSpriteFont prisonNum;

	private GameObject readableInfoRoot;
	private Font readableInfoFont;
	private TextMesh readableHistoryTime;
	private TextMesh readablePrefect;
	private TextMesh readableKingName;
	private TextMesh readableCityName;
	private TextMesh readableDefense;
	private TextMesh readablePopulation;
	private TextMesh readableMoney;
	private TextMesh readableGeneralNum;
	private TextMesh readableSoldierNum;
	private TextMesh readableReservistNum;
	private TextMesh readablePrisonNum;
	
	/// <summary>
	/// 方法说明：刷新城内指令右侧城池详情信息，并使用稳定动态字体布局替代旧点阵字段。
	/// 参数说明：idx 为城池索引。
	/// 返回说明：无返回值。
	/// </summary>
	public void SetCity(int idx) {
		EnsureReadableInformationLayout();
		
		if (idx < 0 || idx >= Informations.Instance.cityNum) {
			return;
		}
		
		CityInfo cInfo = Informations.Instance.GetCityInfo(idx);
		
		SetLegacyAndReadableText(historyTime, readableHistoryTime, Controller.historyTime + ZhongWen.Instance.nian);
		
		if (cInfo.king == -1) {
			
			head.SetGeneralHead(-1);
			
			SetLegacyAndReadableText(prefect, readablePrefect, ZhongWen.Instance.wurenzhanling);
			SetLegacyAndReadableText(kingName, readableKingName, ZhongWen.Instance.wurenzhanling);
		} else {
			
			head.SetGeneralHead(cInfo.prefect);
			
			string pName = ZhongWen.Instance.GetGeneralName(cInfo.prefect);
			
			SetLegacyAndReadableText(prefect, readablePrefect, pName);
			
			
			if (cInfo.king < Informations.Instance.kingNum) {
				SetLegacyAndReadableText(kingName, readableKingName, ZhongWen.Instance.GetKingName(cInfo.king));
			} else {
				SetLegacyAndReadableText(kingName, readableKingName, ZhongWen.Instance.daozei);
			}
		}
		
		SetLegacyAndReadableText(cityName, readableCityName, ZhongWen.Instance.GetCityName(idx));
		
		if (cInfo.king == Controller.kingIndex) {
			SetLegacyAndReadableText(defense, readableDefense, cInfo.defense + "");
			SetLegacyAndReadableText(population, readablePopulation, cInfo.population + "");
			SetLegacyAndReadableText(money, readableMoney, cInfo.money + "");
			
			SetLegacyAndReadableText(reservistNum, readableReservistNum, cInfo.reservist + "/" + cInfo.reservistMax);
		} else {
			if (cInfo.king == -1) {
				SetLegacyAndReadableText(defense, readableDefense, cInfo.defense + "");
				SetLegacyAndReadableText(population, readablePopulation, cInfo.population + "");
			} else {
				SetLegacyAndReadableText(defense, readableDefense, "---");
				SetLegacyAndReadableText(population, readablePopulation, "---");
			}
			SetLegacyAndReadableText(money, readableMoney, "---");
			SetLegacyAndReadableText(reservistNum, readableReservistNum, "---");
		}
		
		if (cInfo.king != -1) {
			
			SetLegacyAndReadableText(generalNum, readableGeneralNum, cInfo.generals.Count + "");
			
			if (cInfo.king == Controller.kingIndex) {
				
				SetLegacyAndReadableText(soldierNum, readableSoldierNum, cInfo.soldiersNum + "");
				SetLegacyAndReadableText(prisonNum, readablePrisonNum, cInfo.prisons.Count + "");
			} else {
				SetLegacyAndReadableText(soldierNum, readableSoldierNum, "---");
				SetLegacyAndReadableText(prisonNum, readablePrisonNum, "---");
			}
		} else {
			SetLegacyAndReadableText(generalNum, readableGeneralNum, "---");
			SetLegacyAndReadableText(soldierNum, readableSoldierNum, "---");
			SetLegacyAndReadableText(prisonNum, readablePrisonNum, "---");
		}
	}

	/// <summary>
	/// 方法说明：创建城池详情的稳定动态字体布局，并隐藏旧点阵字段。
	/// 参数说明：无。
	/// 返回说明：无返回值。
	/// </summary>
	private void EnsureReadableInformationLayout() {
		HideLegacyInformationFonts();
		if (readableInfoRoot != null) return;

		readableInfoFont = UnifiedGameFontController.CreateChineseDynamicFont(ReadableInfoFontSize);
		readableInfoRoot = new GameObject("ReadableCityCommandInformation");
		readableInfoRoot.hideFlags = HideFlags.DontSave;
		readableInfoRoot.layer = gameObject.layer;
		readableInfoRoot.transform.SetParent(transform, false);
		readableInfoRoot.transform.localPosition = new Vector3(0f, 0f, -0.4f);

		CreateReadableLabel("TimeLabel", "时间", new Vector3(-70f, 142f, 0f), TextAnchor.MiddleLeft);
		readableHistoryTime = CreateReadableValue("TimeValue", new Vector3(25f, 142f, 0f), TextAnchor.MiddleLeft);
		CreateReadableLabel("PrefectLabel", "太守", new Vector3(-70f, 112f, 0f), TextAnchor.MiddleLeft);
		readablePrefect = CreateReadableValue("PrefectValue", new Vector3(25f, 112f, 0f), TextAnchor.MiddleLeft);
		CreateReadableLabel("KingLabel", "君主", new Vector3(-70f, 82f, 0f), TextAnchor.MiddleLeft);
		readableKingName = CreateReadableValue("KingValue", new Vector3(25f, 82f, 0f), TextAnchor.MiddleLeft);

		CreateReadableLabel("CityLabel", "城名", new Vector3(-70f, 42f, 0f), TextAnchor.MiddleLeft);
		readableCityName = CreateReadableValue("CityValue", new Vector3(25f, 42f, 0f), TextAnchor.MiddleLeft);
		CreateReadableLabel("DefenseLabel", "防御力", new Vector3(-70f, 12f, 0f), TextAnchor.MiddleLeft);
		readableDefense = CreateReadableValue("DefenseValue", new Vector3(45f, 12f, 0f), TextAnchor.MiddleLeft);
		CreateReadableLabel("PopulationLabel", "人口", new Vector3(-70f, -18f, 0f), TextAnchor.MiddleLeft);
		readablePopulation = CreateReadableValue("PopulationValue", new Vector3(25f, -18f, 0f), TextAnchor.MiddleLeft);
		CreateReadableLabel("MoneyLabel", "金钱", new Vector3(-70f, -48f, 0f), TextAnchor.MiddleLeft);
		readableMoney = CreateReadableValue("MoneyValue", new Vector3(25f, -48f, 0f), TextAnchor.MiddleLeft);

		CreateReadableLabel("GeneralCountLabel", "武将数", new Vector3(90f, 42f, 0f), TextAnchor.MiddleLeft);
		readableGeneralNum = CreateReadableValue("GeneralCountValue", new Vector3(205f, 42f, 0f), TextAnchor.MiddleLeft);
		CreateReadableLabel("SoldierLabel", "兵数", new Vector3(90f, 12f, 0f), TextAnchor.MiddleLeft);
		readableSoldierNum = CreateReadableValue("SoldierValue", new Vector3(205f, 12f, 0f), TextAnchor.MiddleLeft);
		CreateReadableLabel("ReservistLabel", "预备兵", new Vector3(90f, -18f, 0f), TextAnchor.MiddleLeft);
		readableReservistNum = CreateReadableValue("ReservistValue", new Vector3(205f, -18f, 0f), TextAnchor.MiddleLeft);
		CreateReadableLabel("PrisonLabel", "俘虏", new Vector3(90f, -48f, 0f), TextAnchor.MiddleLeft);
		readablePrisonNum = CreateReadableValue("PrisonValue", new Vector3(205f, -48f, 0f), TextAnchor.MiddleLeft);
	}

	/// <summary>
	/// 方法说明：隐藏城池详情面板原有 exSpriteFont 及其动态镜像。
	/// 参数说明：无。
	/// 返回说明：无返回值。
	/// </summary>
	private void HideLegacyInformationFonts() {
		exSpriteFont[] legacyFonts = GetComponentsInChildren<exSpriteFont>(true);
		for (int i = 0; i < legacyFonts.Length; i++) {
			HideLegacyInformationFont(legacyFonts[i]);
		}
	}

	/// <summary>
	/// 方法说明：隐藏单个旧字段，避免它和手动布局的动态文本重叠。
	/// 参数说明：legacyFont 为旧字体组件。
	/// 返回说明：无返回值。
	/// </summary>
	private void HideLegacyInformationFont(exSpriteFont legacyFont) {
		if (legacyFont == null) return;

		if (legacyFont.GetComponent<UnifiedGameFontIgnore>() == null) {
			legacyFont.gameObject.AddComponent<UnifiedGameFontIgnore>();
		}

		UnifiedGameFontMirror mirror = legacyFont.GetComponent<UnifiedGameFontMirror>();
		if (mirror != null) {
			mirror.Dispose();
		}

		Renderer renderer = legacyFont.GetComponent<Renderer>();
		if (renderer != null) {
			renderer.enabled = false;
		}
	}

	/// <summary>
	/// 方法说明：同步旧字段文本数据，同时刷新手动布局的新字段。
	/// 参数说明：legacyFont 为旧字段，readableText 为新字段，text 为要显示的文本。
	/// 返回说明：无返回值。
	/// </summary>
	private void SetLegacyAndReadableText(exSpriteFont legacyFont, TextMesh readableText, string text) {
		if (legacyFont != null) {
			legacyFont.text = text;
		}
		if (readableText != null) {
			readableText.text = text;
		}
	}

	/// <summary>
	/// 方法说明：创建城池详情静态标签。
	/// 参数说明：objectName 为对象名，text 为标签文字，localPosition 为本地坐标，anchor 为锚点。
	/// 返回说明：返回创建出的 TextMesh。
	/// </summary>
	private TextMesh CreateReadableLabel(string objectName, string text, Vector3 localPosition, TextAnchor anchor) {
		TextMesh textMesh = CreateReadableText(objectName, localPosition, anchor);
		textMesh.text = text;
		return textMesh;
	}

	/// <summary>
	/// 方法说明：创建城池详情动态值字段。
	/// 参数说明：objectName 为对象名，localPosition 为本地坐标，anchor 为锚点。
	/// 返回说明：返回创建出的 TextMesh。
	/// </summary>
	private TextMesh CreateReadableValue(string objectName, Vector3 localPosition, TextAnchor anchor) {
		return CreateReadableText(objectName, localPosition, anchor);
	}

	/// <summary>
	/// 方法说明：创建城池详情统一中文 TextMesh。
	/// 参数说明：objectName 为对象名，localPosition 为本地坐标，anchor 为锚点。
	/// 返回说明：返回创建出的 TextMesh。
	/// </summary>
	private TextMesh CreateReadableText(string objectName, Vector3 localPosition, TextAnchor anchor) {
		GameObject textObject = new GameObject(objectName);
		textObject.hideFlags = HideFlags.DontSave;
		textObject.layer = gameObject.layer;
		textObject.transform.SetParent(readableInfoRoot.transform, false);
		textObject.transform.localPosition = localPosition;

		TextMesh textMesh = textObject.AddComponent<TextMesh>();
		textMesh.font = readableInfoFont;
		textMesh.fontSize = ReadableInfoFontSize;
		textMesh.characterSize = ReadableInfoCharacterSize;
		textMesh.anchor = anchor;
		textMesh.alignment = TextAlignment.Left;
		textMesh.color = Color.white;

		Renderer textRenderer = textMesh.GetComponent<Renderer>();
		if (textRenderer != null) {
			textRenderer.sharedMaterial = readableInfoFont.material;
			textRenderer.sortingOrder = ReadableInfoSortingOrder;
		}
		return textMesh;
	}
	
}
