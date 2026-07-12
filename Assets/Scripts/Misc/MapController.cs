using UnityEngine;
using System.Collections;

public class MapController : MonoBehaviour {

	private Color gray = new Color(0.5f, 0, 0, 1);
	private exSprite[] cities;
	private TextMesh[] cityNameLabels;
	private Font cityNameFont;
	private int cityNameFontSize = 42;
	private float cityNameCharacterSize = 1.35f;
	private Vector3 cityNameOffset = new Vector3(0f, -10f, -0.35f);
	private bool hasMapLocalBounds = false;
	private Vector2 mapLocalMin = Vector2.zero;
	private Vector2 mapLocalMax = Vector2.zero;

	/// <summary>
	/// 方法说明：初始化地图城池标记。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	void Awake()
	{
		InitCities();
	}

	/// <summary>
	/// 方法说明：初始化选君主地图城池标记，支持 MOD06 的动态城池数量。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	void InitCities() {
		if (cities == null || cities.Length != Informations.Instance.cityNum) {
			CaptureExistingCityLocalBounds();
			cities = new exSprite[Informations.Instance.cityNum];
			exSprite template = null;
			
			for (int i=0; i<Informations.Instance.cityNum; i++) {
				string cityName = "City" + (i+1);
				Transform cityTransform = transform.FindChild(cityName);
				if (cityTransform != null) {
					cities[i] = cityTransform.GetComponent<exSprite>();
					if (template == null) template = cities[i];
				} else if (template != null) {
					GameObject go = (GameObject)Instantiate(template.gameObject);
					go.name = cityName;
					go.transform.parent = transform;
					go.transform.localScale = template.transform.localScale;
					go.transform.localRotation = template.transform.localRotation;
					cities[i] = go.GetComponent<exSprite>();
				}

				if (cities[i] != null && Informations.Instance.HasCityPosition(i)) {
					cities[i].transform.localPosition = GetCityMapLocalPosition(i);
				}
			}

			RebuildCityNameLabels();
		}
	}

	/// <summary>
	/// 方法说明：读取场景内已有城池点的本地坐标范围，作为小地图坐标映射边界。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	private void CaptureExistingCityLocalBounds() {
		if (hasMapLocalBounds) return;

		for (int i = 0; i < transform.childCount; i++) {
			Transform child = transform.GetChild(i);
			if (!IsCityTransform(child)) continue;

			Vector3 position = child.localPosition;
			if (!hasMapLocalBounds) {
				mapLocalMin = new Vector2(position.x, position.y);
				mapLocalMax = mapLocalMin;
				hasMapLocalBounds = true;
			} else {
				mapLocalMin.x = Mathf.Min(mapLocalMin.x, position.x);
				mapLocalMin.y = Mathf.Min(mapLocalMin.y, position.y);
				mapLocalMax.x = Mathf.Max(mapLocalMax.x, position.x);
				mapLocalMax.y = Mathf.Max(mapLocalMax.y, position.y);
			}
		}

		if (!hasMapLocalBounds) {
			Debug.LogError("小地图缺少原始城池点，无法映射 MOD06 城池坐标。");
		}
	}

	/// <summary>
	/// 方法说明：判断节点是否为小地图城池点。
	/// 参数说明：target 为待判断节点。
	/// 返回说明：是城池点返回 true，否则返回 false。
	/// </summary>
	private bool IsCityTransform(Transform target) {
		if (target == null || !target.name.StartsWith("City")) return false;

		string suffix = target.name.Substring(4);
		int cityNumber;
		return int.TryParse(suffix, out cityNumber) && cityNumber > 0;
	}

	/// <summary>
	/// 方法说明：读取城池在当前小地图内的本地坐标。
	/// 参数说明：cityIdx 为城池索引。
	/// 返回说明：返回映射到小地图范围内的本地坐标。
	/// </summary>
	private Vector3 GetCityMapLocalPosition(int cityIdx) {
		if (!MODLoadController.IsRestoredSango2Index(Controller.MODSelect)) {
			return Informations.Instance.GetCityWorldPosition(cityIdx);
		}

		if (!hasMapLocalBounds) {
			return Informations.Instance.GetCityWorldPosition(cityIdx);
		}

		Vector3 worldPosition = Informations.Instance.GetCityWorldPosition(cityIdx);
		float halfWidth = MODLoadController.RecoveredMapWorldWidth * 0.5f;
		float halfHeight = MODLoadController.RecoveredMapWorldHeight * 0.5f;
		float x = Mathf.InverseLerp(-halfWidth, halfWidth, worldPosition.x);
		float y = Mathf.InverseLerp(-halfHeight, halfHeight, worldPosition.y);
		return new Vector3(Mathf.Lerp(mapLocalMin.x, mapLocalMax.x, x), Mathf.Lerp(mapLocalMin.y, mapLocalMax.y, y), -1f);
	}

	/// <summary>
	/// 方法说明：重建 MOD06 小地图城池名称标签。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	private void RebuildCityNameLabels() {
		ClearCityNameLabels();
		if (!MODLoadController.IsRestoredSango2Index(Controller.MODSelect)) return;

		cityNameLabels = new TextMesh[Informations.Instance.cityNum];
		for (int i = 0; i < Informations.Instance.cityNum; i++) {
			if (cities[i] == null || !Informations.Instance.HasCityPosition(i)) continue;

			cityNameLabels[i] = CreateCityNameLabel(i);
		}
	}

	/// <summary>
	/// 方法说明：清理当前小地图城池名称标签。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	private void ClearCityNameLabels() {
		if (cityNameLabels == null) return;

		for (int i = 0; i < cityNameLabels.Length; i++) {
			if (cityNameLabels[i] != null) {
				DestroyMapObject(cityNameLabels[i].gameObject);
			}
		}

		cityNameLabels = null;
	}

	/// <summary>
	/// 方法说明：创建单个小地图城池名称标签。
	/// 参数说明：cityIdx 为城池索引。
	/// 返回说明：返回创建出的 TextMesh 标签。
	/// </summary>
	private TextMesh CreateCityNameLabel(int cityIdx) {
		GameObject go = new GameObject("CityNameLabel" + cityIdx);
		go.transform.parent = transform;
		go.transform.localPosition = cities[cityIdx].transform.localPosition + cityNameOffset;
		go.transform.localScale = Vector3.one;
		go.transform.localRotation = Quaternion.identity;
		go.layer = gameObject.layer;

		TextMesh textMesh = go.AddComponent<TextMesh>();
		textMesh.font = GetCityNameFont();
		textMesh.GetComponent<Renderer>().sharedMaterial = textMesh.font.material;
		textMesh.text = ZhongWen.Instance.GetCityName(cityIdx);
		textMesh.fontSize = cityNameFontSize;
		textMesh.characterSize = cityNameCharacterSize;
		textMesh.anchor = TextAnchor.MiddleCenter;
		textMesh.alignment = TextAlignment.Center;
		textMesh.color = Color.white;
		go.SetActive(false);
		return textMesh;
	}

	/// <summary>
	/// 方法说明：销毁小地图运行时创建的对象。
	/// 参数说明：target 为目标对象。
	/// 返回说明：无返回值。
	/// </summary>
	private void DestroyMapObject(GameObject target) {
		if (target == null) return;

		if (Application.isPlaying) {
			Destroy(target);
		} else {
			DestroyImmediate(target);
		}
	}

	/// <summary>
	/// 方法说明：读取小地图城池名称动态字体。
	/// 参数说明：无参数。
	/// 返回说明：返回动态字体。
	/// </summary>
	private Font GetCityNameFont() {
		if (cityNameFont == null) {
			cityNameFont = Font.CreateDynamicFontFromOSFont(new string[] { "PingFang SC", "Heiti SC", "Arial Unicode MS", "sans-serif" }, cityNameFontSize);
		}

		return cityNameFont;
	}
	
	/// <summary>
	/// 方法说明：清空所有城池高亮。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	public void ClearSelect() {
		InitCities();
		
		for (int i=0; i<Informations.Instance.cityNum; i++) {
			if (cities[i] != null) {
				cities[i].color = gray;
			}

			SetCityNameVisible(i, false);
		}
	}
	
	/// <summary>
	/// 方法说明：高亮指定城池。
	/// 参数说明：idx 为城池索引。
	/// 返回说明：无返回值。
	/// </summary>
	public void SelectCity(int idx) {
		InitCities();
		
		if (idx < 0 || idx >= Informations.Instance.cityNum) return;
		
		if (cities[idx] != null) {
			cities[idx].color = new Color(1, 1, 1, 1);
		}

		SetCityNameVisible(idx, true);
	}

	/// <summary>
	/// 方法说明：设置指定城池名称标签是否显示。
	/// 参数说明：idx 为城池索引，visible 为是否显示。
	/// 返回说明：无返回值。
	/// </summary>
	private void SetCityNameVisible(int idx, bool visible) {
		if (cityNameLabels == null || idx < 0 || idx >= cityNameLabels.Length) return;
		if (cityNameLabels[idx] == null) return;

		cityNameLabels[idx].gameObject.SetActive(visible);
	}
}
