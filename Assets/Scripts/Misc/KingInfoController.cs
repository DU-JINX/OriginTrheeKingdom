using UnityEngine;

public class KingInfoController : MonoBehaviour {
	
	public GeneralsHeadSelect head;
	
	public exSpriteFont kingName;
	public exSpriteFont cityNum;
	public exSpriteFont money;
	public exSpriteFont population;
	public exSpriteFont generalNum;
	public exSpriteFont soldierNum;

	private TextMesh kingNameLabel;
	private Font kingNameDynamicFont;
	private int kingNameFontSize = 64;
	private float kingNameCharacterSize = 3.4f;
	
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
		
		head.SetGeneralHead(Informations.Instance.GetKingInfo(idx).generalIdx);
		
		SetKingNameText(ZhongWen.Instance.GetKingName(idx));
		
		long _money = 0;
		long _population = 0;
		
		for (int i=0; i<kInfo.cities.Count; i++) {
			int cIdx = (int)kInfo.cities[i];
			CityInfo cInfo = Informations.Instance.GetCityInfo(cIdx);
			
			_money += cInfo.money;
			_population += cInfo.population;
		}
		
		cityNum.text 	= kInfo.cities.Count + "";
		money.text		= _money + "";
		population.text	= _population + ZhongWen.Instance.ren;
		
		int _soldierNum = 0;
		
		for (int i=0; i<kInfo.generals.Count; i++) {
			int gIdx = (int)kInfo.generals[i];
			_soldierNum += Informations.Instance.GetGeneralInfo(gIdx).soldierCur;
			_soldierNum += Informations.Instance.GetGeneralInfo(gIdx).knightCur;
		}
		
		generalNum.text = kInfo.generals.Count + ZhongWen.Instance.ren;
		soldierNum.text = _soldierNum + ZhongWen.Instance.ren;
	}

	/// <summary>
	/// 方法说明：设置底部信息栏君主名称，并避开旧 bitmap 字体缺字问题。
	/// 参数说明：name 为君主名称。
	/// 返回说明：无返回值。
	/// </summary>
	private void SetKingNameText(string name)
	{
		if (kingName == null) return;

		kingName.text = name;
		HideKingNameBitmapFont();
		if (kingNameLabel == null)
		{
			kingNameLabel = CreateKingNameLabel();
		}

		kingNameLabel.text = name;
	}

	/// <summary>
	/// 方法说明：创建底部信息栏君主名称动态字体标签。
	/// 参数说明：无参数。
	/// 返回说明：返回创建出的 TextMesh 标签。
	/// </summary>
	private TextMesh CreateKingNameLabel()
	{
		GameObject go = new GameObject("KingInfoNameLabel");
		go.transform.parent = kingName.transform.parent;
		go.transform.localPosition = new Vector3(kingName.transform.localPosition.x, kingName.transform.localPosition.y, kingName.transform.localPosition.z - 0.2f);
		go.transform.localScale = Vector3.one;
		go.transform.localRotation = Quaternion.identity;
		go.layer = kingName.gameObject.layer;

		TextMesh textMesh = go.AddComponent<TextMesh>();
		textMesh.font = GetKingNameDynamicFont();
		textMesh.GetComponent<Renderer>().sharedMaterial = textMesh.font.material;
		textMesh.fontSize = kingNameFontSize;
		textMesh.characterSize = kingNameCharacterSize;
		textMesh.anchor = TextAnchor.MiddleCenter;
		textMesh.alignment = TextAlignment.Center;
		textMesh.color = Color.white;
		return textMesh;
	}

	/// <summary>
	/// 方法说明：读取底部信息栏君主名称动态字体。
	/// 参数说明：无参数。
	/// 返回说明：返回动态字体对象。
	/// </summary>
	private Font GetKingNameDynamicFont()
	{
		if (kingNameDynamicFont == null)
		{
			kingNameDynamicFont = Font.CreateDynamicFontFromOSFont(new string[] { "PingFang SC", "Heiti SC", "Arial Unicode MS", "sans-serif" }, kingNameFontSize);
		}

		return kingNameDynamicFont;
	}

	/// <summary>
	/// 方法说明：隐藏底部信息栏旧 bitmap 君主名字体。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	private void HideKingNameBitmapFont()
	{
		Color transparent = new Color(1f, 1f, 1f, 0f);
		kingName.topColor = transparent;
		kingName.botColor = transparent;
	}
}
