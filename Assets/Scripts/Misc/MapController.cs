using UnityEngine;
using System.Collections;

public class MapController : MonoBehaviour {
	
	private Color gray = new Color(0.5f, 0, 0, 1);
    private exSprite[] cities;

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
		if (cities == null) {
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
					cities[i].transform.localPosition = Informations.Instance.GetCityWorldPosition(i);
				}
			}
		}
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
	}
}
