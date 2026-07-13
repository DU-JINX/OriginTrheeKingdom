using UnityEngine;

public class KingInfoController : MonoBehaviour {
	
	public GeneralsHeadSelect head;
	
	public exSpriteFont kingName;
	public exSpriteFont cityNum;
	public exSpriteFont money;
	public exSpriteFont population;
	public exSpriteFont generalNum;
	public exSpriteFont soldierNum;

	private const int InfoItemCount = 6;
	private const int InfoKingNameIndex = 0;
	private const int InfoMoneyIndex = 1;
	private const int InfoPopulationIndex = 2;
	private const int InfoCityNumIndex = 3;
	private const int InfoGeneralNumIndex = 4;
	private const int InfoSoldierNumIndex = 5;
	private TextMesh[] infoTitleLabels;
	private TextMesh[] infoValueLabels;
	private Font infoDynamicFont;
	private int infoFontSize = 64;
	private float infoCharacterSize = 3.1f;
	private Vector3[] infoTitlePositions = new Vector3[] {
		new Vector3(-155f, -138f, 1.8f),
		new Vector3(-155f, -170f, 1.8f),
		new Vector3(-155f, -202f, 1.8f),
		new Vector3(45f, -138f, 1.8f),
		new Vector3(45f, -170f, 1.8f),
		new Vector3(45f, -202f, 1.8f)
	};
	private Vector3[] infoValuePositions = new Vector3[] {
		new Vector3(-105f, -138f, 1.8f),
		new Vector3(-105f, -170f, 1.8f),
		new Vector3(-105f, -202f, 1.8f),
		new Vector3(175f, -138f, 1.8f),
		new Vector3(175f, -170f, 1.8f),
		new Vector3(210f, -202f, 1.8f)
	};
	private string[] infoTitleTexts = new string[] { "君主", "金钱", "人口", "城数", "武将", "总兵数" };

	/// <summary>
	/// 方法说明：界面启用时先隐藏旧底栏字体，避免全局字体镜像抢先创建错位文字。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	void OnEnable()
	{
		HideLegacyInfoFonts();
	}
	
	/// <summary>
	/// 方法说明：刷新指定君主的头像、名称、城池、金钱、人口、武将数和总兵数。
	/// 参数说明：idx 为君主索引。
	/// 返回说明：无返回值。
	/// </summary>
	public void SetKing(int idx) {
		if (idx < 0 || idx >= Informations.Instance.kingNum) {
			return;
		}

		KingInfo kInfo = Informations.Instance.GetKingInfo(idx);
		head.SetGeneralHead(kInfo.generalIdx);

		string kingNameText = ZhongWen.Instance.GetKingName(idx);
		long _money = 0;
		long _population = 0;

		for (int i=0; i<kInfo.cities.Count; i++) {
			int cIdx = (int)kInfo.cities[i];
			CityInfo cInfo = Informations.Instance.GetCityInfo(cIdx);

			_money += cInfo.money;
			_population += cInfo.population;
		}

		string cityNumText = kInfo.cities.Count + "";
		string moneyText = _money + "";
		string populationText = _population + ZhongWen.Instance.ren;
		cityNum.text = cityNumText;
		money.text = moneyText;
		population.text = populationText;

		int _soldierNum = 0;

		for (int i=0; i<kInfo.generals.Count; i++) {
			int gIdx = (int)kInfo.generals[i];
			_soldierNum += Informations.Instance.GetGeneralInfo(gIdx).soldierCur;
			_soldierNum += Informations.Instance.GetGeneralInfo(gIdx).knightCur;
		}

		string generalNumText = kInfo.generals.Count + ZhongWen.Instance.ren;
		string soldierNumText = _soldierNum + ZhongWen.Instance.ren;
		generalNum.text = generalNumText;
		soldierNum.text = soldierNumText;
		SetInfoPanelTexts(kingNameText, moneyText, populationText, cityNumText, generalNumText, soldierNumText);
	}

	/// <summary>
	/// 方法说明：设置底部信息栏全部动态字体内容。
	/// 参数说明：kingNameText 为君主名，moneyText 为金钱，populationText 为人口，cityNumText 为城池数，generalNumText 为武将数，soldierNumText 为总兵数。
	/// 返回说明：无返回值。
	/// </summary>
	private void SetInfoPanelTexts(string kingNameText, string moneyText, string populationText, string cityNumText, string generalNumText, string soldierNumText)
	{
		EnsureInfoPanelLabels();
		HideLegacyInfoFonts();

		infoValueLabels[InfoKingNameIndex].text = kingNameText;
		infoValueLabels[InfoMoneyIndex].text = moneyText;
		infoValueLabels[InfoPopulationIndex].text = populationText;
		infoValueLabels[InfoCityNumIndex].text = cityNumText;
		infoValueLabels[InfoGeneralNumIndex].text = generalNumText;
		infoValueLabels[InfoSoldierNumIndex].text = soldierNumText;
	}

	/// <summary>
	/// 方法说明：确保底部信息栏动态字体对象已经创建。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	private void EnsureInfoPanelLabels()
	{
		if (infoTitleLabels != null && infoValueLabels != null) return;

		infoTitleLabels = new TextMesh[InfoItemCount];
		infoValueLabels = new TextMesh[InfoItemCount];
		for (int i = 0; i < InfoItemCount; i++)
		{
			infoTitleLabels[i] = CreateInfoTextMesh("KingInfoTitle" + i, infoTitleTexts[i], infoTitlePositions[i], TextAnchor.MiddleLeft);
			infoValueLabels[i] = CreateInfoTextMesh("KingInfoValue" + i, "", infoValuePositions[i], TextAnchor.MiddleLeft);
		}
	}

	/// <summary>
	/// 方法说明：创建底部信息栏单个动态字体标签。
	/// 参数说明：objectName 为对象名，text 为初始文字，localPosition 为本地坐标，anchor 为文字锚点。
	/// 返回说明：返回创建出的 TextMesh 标签。
	/// </summary>
	private TextMesh CreateInfoTextMesh(string objectName, string text, Vector3 localPosition, TextAnchor anchor)
	{
		GameObject go = new GameObject(objectName);
		go.transform.parent = transform;
		go.transform.localPosition = localPosition;
		go.transform.localScale = Vector3.one;
		go.transform.localRotation = Quaternion.identity;
		go.layer = gameObject.layer;

		TextMesh textMesh = go.AddComponent<TextMesh>();
		textMesh.font = GetInfoDynamicFont();
		textMesh.GetComponent<Renderer>().sharedMaterial = textMesh.font.material;
		textMesh.text = text;
		textMesh.fontSize = infoFontSize;
		textMesh.characterSize = infoCharacterSize;
		textMesh.anchor = anchor;
		textMesh.alignment = TextAlignment.Left;
		textMesh.color = Color.white;
		return textMesh;
	}

	/// <summary>
	/// 方法说明：隐藏底部信息栏旧 bitmap 字体和空格排版标签。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	private void HideLegacyInfoFonts()
	{
		HideLegacyInfoFont(kingName);
		HideLegacyInfoFont(cityNum);
		HideLegacyInfoFont(money);
		HideLegacyInfoFont(population);
		HideLegacyInfoFont(generalNum);
		HideLegacyInfoFont(soldierNum);

		exSpriteFont[] fonts = GetComponentsInChildren<exSpriteFont>(true);
		for (int i = 0; i < fonts.Length; i++)
		{
			if (IsLegacyInfoTitleFont(fonts[i]))
			{
				HideLegacyInfoFont(fonts[i]);
			}
		}
	}

	/// <summary>
	/// 方法说明：判断旧字体是否为底部信息栏整段标签。
	/// 参数说明：font 为待判断字体。
	/// 返回说明：是整段信息标签返回 true，否则返回 false。
	/// </summary>
	private bool IsLegacyInfoTitleFont(exSpriteFont font)
	{
		if (font == null || string.IsNullOrEmpty(font.text)) return false;

		return font.text.IndexOf("君主") >= 0
			&& font.text.IndexOf("城数") >= 0
			&& font.text.IndexOf("总兵数") >= 0;
	}

	/// <summary>
	/// 方法说明：隐藏单个底部信息旧字体并阻止全局镜像重复显示。
	/// 参数说明：font 为旧字体组件。
	/// 返回说明：无返回值。
	/// </summary>
	private void HideLegacyInfoFont(exSpriteFont font)
	{
		if (font == null) return;

		MarkFontAsHandledByManualLabel(font);
		Color transparent = new Color(1f, 1f, 1f, 0f);
		font.topColor = transparent;
		font.botColor = transparent;
	}

	/// <summary>
	/// 方法说明：标记旧字体已经由当前脚本手工创建动态字体，避免全局字体镜像重复覆盖。
	/// 参数说明：font 为需要跳过全局镜像的旧字体组件。
	/// 返回说明：无返回值。
	/// </summary>
	private void MarkFontAsHandledByManualLabel(exSpriteFont font)
	{
		if (font == null || font.GetComponent<UnifiedGameFontIgnore>() != null) return;

		font.gameObject.AddComponent<UnifiedGameFontIgnore>();
	}

	/// <summary>
	/// 方法说明：读取底部信息栏动态字体。
	/// 参数说明：无参数。
	/// 返回说明：返回动态字体对象。
	/// </summary>
	private Font GetInfoDynamicFont()
	{
		if (infoDynamicFont == null)
		{
			infoDynamicFont = UnifiedGameFontController.CreateChineseDynamicFont(infoFontSize);
		}

		return infoDynamicFont;
	}
}
