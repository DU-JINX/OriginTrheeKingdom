using UnityEngine;
using System.Collections;

public class FlagsController : MonoBehaviour {

	public GameObject cityNamePerfab;
	public exSpriteAnimation[] flags;

	/// <summary>
	/// 方法说明：初始化战略地图旗帜和城池名。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	void Start () {
		for (int i=0; i<Informations.Instance.cityNum; i++) {
			exSpriteAnimation flag = GetOrCreateFlag(i);
			if (flag == null) continue;

			SetFlag(i);
			
			if (Informations.Instance.HasCityPosition(i)) {
				flag.transform.position = Informations.Instance.GetCityFlagWorldPosition(i);
			}

			Vector3 pos = flag.transform.position;
			pos.z = (pos.y - 400f) / 800f;
			flag.transform.position = pos;

			GameObject go = (GameObject)Instantiate(cityNamePerfab);
			go.transform.parent = flag.transform;
			go.transform.localPosition = new Vector3(8, -8, 0);
			go.GetComponent<exSpriteFont>().text = ZhongWen.Instance.GetCityName(i);
		}
	}
	
	/// <summary>
	/// 方法说明：按城池势力刷新旗帜动画。
	/// 参数说明：cityIdx 为城池索引。
	/// 返回说明：无返回值。
	/// </summary>
	public void SetFlag(int cityIdx) {
		exSpriteAnimation flag = GetOrCreateFlag(cityIdx);
		if (flag == null) return;
		
		CityInfo cityInfo = Informations.Instance.GetCityInfo(cityIdx);
		if (cityInfo == null) return;

		int kingIdx = cityInfo.king;
		if (kingIdx == -1) {
			flag.GetComponent<Renderer>().enabled = false;
			return;
		}

        string kingName = ZhongWen.Instance.GetKingName(kingIdx);
		string animName = "";
        for (int i = 0; i < ZhongWen.Instance.kingNames.Length; i++ )
        {
            if (ZhongWen.Instance.kingNames[i] == kingName)
            {
                animName = "Flag" + (i + 1);
                break;
            }
        }

		if (animName == "") {
			int flagIndex = kingIdx % ZhongWen.Instance.kingNames.Length;
			animName = "Flag" + (flagIndex + 1);
			Debug.LogError("缺少势力旗帜资源，使用当前旗帜动画池: " + kingName);
		}

		flag.Play(animName);
		flag.GetComponent<Renderer>().enabled = true;
	}
	
	/// <summary>
	/// 方法说明：检测当前点击到的城池旗帜。
	/// 参数说明：无参数。
	/// 返回说明：命中返回城池索引，否则返回 -1。
	/// </summary>
	public int GetTouchCityIdx() {
		Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
		RaycastHit hit;
		
		for (int i=0; i<Informations.Instance.cityNum; i++) {
			exSpriteAnimation flag = GetOrCreateFlag(i);
			if (flag != null && flag.GetComponent<Collider>() != null && flag.GetComponent<Collider>().Raycast (ray, out hit, 1000.0f)) {
				return i;
			}
		}
		
		return -1;
	}
	
	/// <summary>
	/// 方法说明：暂停所有可见旗帜动画。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	public void SetFlagsAnimPause() {
		for (int i=0; i<Informations.Instance.cityNum; i++) {
			exSpriteAnimation flag = GetOrCreateFlag(i);
			if (flag != null && flag.GetComponent<Renderer>().enabled) {
				flag.Pause();
			}
		}
	}
	
	/// <summary>
	/// 方法说明：恢复所有可见旗帜动画。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	public void SetFlagsAnimResume() {
		for (int i=0; i<Informations.Instance.cityNum; i++) {
			exSpriteAnimation flag = GetOrCreateFlag(i);
			if (flag != null && flag.GetComponent<Renderer>().enabled) {
				flag.Resume();
			}
		}
	}

	/// <summary>
	/// 方法说明：取得或克隆指定城池的旗帜对象。
	/// 参数说明：cityIdx 为城池索引。
	/// 返回说明：返回旗帜动画组件，缺少模板时返回 null。
	/// </summary>
	private exSpriteAnimation GetOrCreateFlag(int cityIdx) {
		if (cityIdx < 0 || cityIdx >= Informations.Instance.cityNum) return null;

		if (flags == null || flags.Length == 0 || flags[0] == null) {
			Debug.LogError("旗帜模板缺失，无法创建城池旗帜。");
			return null;
		}

		if (cityIdx >= flags.Length) {
			System.Array.Resize(ref flags, cityIdx + 1);
		}

		if (flags[cityIdx] == null) {
			GameObject go = (GameObject)Instantiate(flags[0].gameObject);
			go.name = "Flag" + (cityIdx + 1);
			go.transform.parent = transform;
			go.transform.localScale = flags[0].transform.localScale;
			go.transform.localRotation = flags[0].transform.localRotation;
			flags[cityIdx] = go.GetComponent<exSpriteAnimation>();
		}

		return flags[cityIdx];
	}
}
