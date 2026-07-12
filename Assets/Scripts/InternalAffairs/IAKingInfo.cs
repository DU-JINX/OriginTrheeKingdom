using UnityEngine;
using System.Collections;

public class IAKingInfo : MonoBehaviour {
	
	public exSpriteFont historyTime;
	public GameObject[] kingsHead;
	public exSpriteFont kingName;
	public exSpriteFont cityNum;
	public exSpriteFont money;
	public exSpriteFont population;
	public exSpriteFont generalNum;
	public exSpriteFont soldierNum;
	
	public GameObject[] background;
	
	private Vector3 headPos = new Vector3(97.5f, 70, 0);
	private GeneralsHeadSelect kingHeadSelect;
	private TextMesh kingNameLabel;
	private Font kingNameDynamicFont;
	private int kingNameFontSize = 64;
	private float kingNameCharacterSize = 3.4f;
	
	/// <summary>
	/// 方法说明：初始化内政主界面的君主头像、时间、名称和城池统计。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	void Start () {
		// 1. 按当前 MOD 数据读取玩家君主，避免继续使用一代固定头像数组。
		KingInfo kInfo = Informations.Instance.GetKingInfo(Controller.kingIndex);
		if (kInfo == null) {
			Debug.LogError("内政君主数据不存在，索引: " + Controller.kingIndex);
			return;
		}

		// 2. 通过君主武将索引加载头像，支持 MOD06 的 Face 编号。
		SetKingHead(kInfo.generalIdx);

		// 3. 用运行时势力名称刷新内政顶部信息。
		historyTime.text = Controller.historyTime + ZhongWen.Instance.nian;
		SetKingNameText(ZhongWen.Instance.GetKingName(Controller.kingIndex));
		
		// 4. 统计当前君主城池、金钱和人口。
		GetCityInfo();
	}
	
	/// <summary>
	/// 方法说明：界面启用时刷新武将和兵力统计。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	void OnEnable() {
		GetGeneralInfo();
	}
	
	/// <summary>
	/// 方法说明：内政君主信息界面每帧更新。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	void Update () {
	
	}
	
	/// <summary>
	/// 方法说明：刷新君主头像。
	/// 参数说明：generalIdx 为君主对应武将索引。
	/// 返回说明：无返回值。
	/// </summary>
	void SetKingHead(int generalIdx) {
		if (kingHeadSelect == null) {
			GameObject go = new GameObject("KingHeadRuntime");
			go.transform.parent = transform;
			go.transform.position = new Vector3(transform.position.x + headPos.x, transform.position.y + headPos.y, transform.position.z + headPos.z);
			go.transform.localScale = Vector3.one;
			go.transform.localRotation = Quaternion.identity;
			kingHeadSelect = go.AddComponent<GeneralsHeadSelect>();
		}

		kingHeadSelect.SetGeneralHead(generalIdx);
	}

	/// <summary>
	/// 方法说明：设置内政君主名称，并用动态字体补足 MOD06 字库。
	/// 参数说明：name 为君主名称。
	/// 返回说明：无返回值。
	/// </summary>
	void SetKingNameText(string name) {
		if (kingName == null) return;

		kingName.text = name;
		HideKingNameBitmapFont();
		if (kingNameLabel == null) {
			kingNameLabel = CreateKingNameLabel();
		}

		kingNameLabel.text = name;
	}

	/// <summary>
	/// 方法说明：创建内政君主名称动态字体标签。
	/// 参数说明：无参数。
	/// 返回说明：返回创建出的 TextMesh 标签。
	/// </summary>
	TextMesh CreateKingNameLabel() {
		GameObject go = new GameObject("IAKingNameLabel");
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
	/// 方法说明：读取内政君主名称动态字体。
	/// 参数说明：无参数。
	/// 返回说明：返回动态字体。
	/// </summary>
	Font GetKingNameDynamicFont() {
		if (kingNameDynamicFont == null) {
			kingNameDynamicFont = Font.CreateDynamicFontFromOSFont(new string[] { "PingFang SC", "Heiti SC", "Arial Unicode MS", "sans-serif" }, kingNameFontSize);
		}

		return kingNameDynamicFont;
	}

	/// <summary>
	/// 方法说明：隐藏旧 bitmap 字体，避免 MOD06 缺字或显示错误。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	void HideKingNameBitmapFont() {
		Color transparent = new Color(1f, 1f, 1f, 0f);
		kingName.topColor = transparent;
		kingName.botColor = transparent;
	}

	/// <summary>
	/// 方法说明：统计当前君主拥有的城池、金钱和人口。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	void GetCityInfo() {
		
		long m = 0;
		long p = 0;
		
		KingInfo kInfo = Informations.Instance.GetKingInfo(Controller.kingIndex);
		
		for (int i=0; i<kInfo.cities.Count; i++) {
			CityInfo cInfo = Informations.Instance.GetCityInfo((int)kInfo.cities[i]);
			
			m += cInfo.money;
			p += cInfo.population;
		}
		
		cityNum.text = "" + kInfo.cities.Count;
		money.text = "" + m;
		population.text = p + ZhongWen.Instance.ren;
		
		if (kInfo.cities.Count < 16) {
			Instantiate(background[0]);
		} else if (kInfo.cities.Count < 32) {
			Instantiate(background[1]);
		} else {
			Instantiate(background[2]);
		}
	}
	
	/// <summary>
	/// 方法说明：统计当前君主拥有的武将和兵力。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	void GetGeneralInfo() {
		
		int s = 0;
		
		KingInfo kInfo = Informations.Instance.GetKingInfo(Controller.kingIndex);
		
		for (int i=0; i<kInfo.generals.Count; i++) {
			s += Informations.Instance.GetGeneralInfo((int)kInfo.generals[i]).soldierCur;
			s += Informations.Instance.GetGeneralInfo((int)kInfo.generals[i]).knightCur;
		}
		
		generalNum.text = kInfo.generals.Count + ZhongWen.Instance.ren;
		soldierNum.text = s + ZhongWen.Instance.ren;
	}
}
