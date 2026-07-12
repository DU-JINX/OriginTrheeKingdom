using UnityEngine;
using System.Collections;

public class FlagsController : MonoBehaviour {

	public GameObject cityNamePerfab;
	public exSpriteAnimation[] flags;

	private const string RecoveredCityMarkerResourcePath = "Sango2Recovered/Map/Sango2CityMarker";
	private const float RecoveredCityMarkerWidth = 24f;
	private const float RecoveredCityMarkerHeight = 18f;
	private const float RecoveredCityNameOffsetY = -22f;
	private const float RecoveredCoincidentFlagOffsetY = 20f;
	private static Material recoveredCityMarkerMaterial;
	private static Mesh recoveredCityMarkerMesh;
	private static readonly Vector3[] RecoveredCityNameShadowOffsets = new Vector3[] {
		new Vector3(-2f, 0f, 0.02f),
		new Vector3(2f, 0f, 0.02f),
		new Vector3(0f, -2f, 0.02f),
		new Vector3(0f, 2f, 0.02f),
		new Vector3(-2f, -2f, 0.02f),
		new Vector3(2f, -2f, 0.02f),
		new Vector3(-2f, 2f, 0.02f),
		new Vector3(2f, 2f, 0.02f)
	};

	/// <summary>
	/// 方法说明：初始化战略地图旗帜和城池名。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	void Start () {
		bool isRestoredSango2 = MODLoadController.IsRestoredSango2Index(Controller.MODSelect);

		for (int i=0; i<Informations.Instance.cityNum; i++) {
			exSpriteAnimation flag = GetOrCreateFlag(i);
			if (flag == null) continue;

			SetFlag(i);
			SetFlagPosition(flag, i, isRestoredSango2);

			if (isRestoredSango2) {
				CreateRecoveredCityMarker(i);
				CreateRecoveredCityName(i);
			} else {
				CreateDefaultCityName(flag.transform, i);
			}
		}
	}

	/// <summary>
	/// 方法说明：按原版规则创建城池名称。
	/// 参数说明：flagTransform 为城池旗帜 Transform，cityIdx 为城池索引。
	/// 返回说明：返回创建出的城池名称对象。
	/// </summary>
	private GameObject CreateDefaultCityName(Transform flagTransform, int cityIdx) {
		GameObject go = (GameObject)Instantiate(cityNamePerfab);
		go.transform.parent = flagTransform;
		go.transform.localPosition = new Vector3(8, -8, 0);
		exSpriteFont cityNameFont = go.GetComponent<exSpriteFont>();
		cityNameFont.text = ZhongWen.Instance.GetCityName(cityIdx);
		return go;
	}

	/// <summary>
	/// 方法说明：为 MOD06 创建以城池坐标为锚点的名称和阴影。
	/// 参数说明：cityIdx 为城池索引。
	/// 返回说明：无返回值。
	/// </summary>
	private void CreateRecoveredCityName(int cityIdx) {
		if (!Informations.Instance.HasCityPosition(cityIdx)) return;

		// 1. 先把名称锚定到城池本体坐标，避免跟旗帜坐标混用。
		string cityName = ZhongWen.Instance.GetCityName(cityIdx);
		Vector3 cityPosition = Informations.Instance.GetCityWorldPosition(cityIdx);
		Vector3 labelPosition = cityPosition + new Vector3(0f, RecoveredCityNameOffsetY, 0f);
		labelPosition.z = GetCityLayerZ(cityPosition) - 0.01f;

		// 2. 再创建多方向黑色描边，让名称在山地、道路、水面上都能读清。
		CreateRecoveredCityNameOutline(cityIdx, cityName, labelPosition);

		// 3. 最后创建高亮正文，避免原来的暗金色被地图底色吞掉。
		GameObject nameObject = CreateCityNameObject("CityName" + cityIdx, transform, cityName, labelPosition);
		exSpriteFont cityNameFont = nameObject.GetComponent<exSpriteFont>();
		SetCityNameColors(cityNameFont, Color.white, Color.white);
	}

	/// <summary>
	/// 方法说明：为 MOD06 城池名称创建多方向黑色描边。
	/// 参数说明：cityIdx 为城池索引，cityName 为显示文本，labelPosition 为正文世界坐标。
	/// 返回说明：无返回值。
	/// </summary>
	private void CreateRecoveredCityNameOutline(int cityIdx, string cityName, Vector3 labelPosition) {
		for (int i = 0; i < RecoveredCityNameShadowOffsets.Length; i++) {
			GameObject shadow = CreateCityNameObject("CityNameOutline" + cityIdx + "_" + i, transform, cityName, labelPosition + RecoveredCityNameShadowOffsets[i]);
			exSpriteFont shadowFont = shadow.GetComponent<exSpriteFont>();
			SetCityNameColors(shadowFont, new Color(0f, 0f, 0f, 0.92f), new Color(0f, 0f, 0f, 0.92f));
		}
	}

	/// <summary>
	/// 方法说明：创建指定文本内容的城池名称对象。
	/// 参数说明：objectName 为对象名，parent 为父节点，cityName 为显示文本，position 为世界坐标。
	/// 返回说明：返回创建出的名称对象。
	/// </summary>
	private GameObject CreateCityNameObject(string objectName, Transform parent, string cityName, Vector3 position) {
		GameObject go = (GameObject)Instantiate(cityNamePerfab);
		go.name = objectName;
		go.transform.parent = parent;
		go.transform.position = position;
		go.transform.localRotation = cityNamePerfab.transform.localRotation;
		go.transform.localScale = cityNamePerfab.transform.localScale;
		exSpriteFont cityNameFont = go.GetComponent<exSpriteFont>();
		cityNameFont.text = cityName;
		return go;
	}

	/// <summary>
	/// 方法说明：设置城池名称字体上下颜色。
	/// 参数说明：cityNameFont 为字体组件，topColor 为上半部颜色，botColor 为下半部颜色。
	/// 返回说明：无返回值。
	/// </summary>
	private void SetCityNameColors(exSpriteFont cityNameFont, Color topColor, Color botColor) {
		cityNameFont.topColor = topColor;
		cityNameFont.botColor = botColor;
	}

	/// <summary>
	/// 方法说明：按城池纵坐标计算与旗帜一致的渲染层级。
	/// 参数说明：position 为城池世界坐标。
	/// 返回说明：返回用于排序的 z 坐标。
	/// </summary>
	private float GetCityLayerZ(Vector3 position) {
		return (position.y - 400f) / 800f;
	}

	/// <summary>
	/// 方法说明：设置旗帜世界坐标，MOD06 会把 prefab 中心偏移修正到 FlagX/FlagY。
	/// 参数说明：flag 为旗帜动画组件，cityIdx 为城池索引，isRestoredSango2 表示是否为 MOD06。
	/// 返回说明：无返回值。
	/// </summary>
	private void SetFlagPosition(exSpriteAnimation flag, int cityIdx, bool isRestoredSango2) {
		Vector3 flagPosition = flag.transform.position;
		if (Informations.Instance.HasCityPosition(cityIdx)) {
			flagPosition = Informations.Instance.GetCityFlagWorldPosition(cityIdx);
		}

		Vector3 objectPosition = flagPosition;
		if (isRestoredSango2) {
			flagPosition = GetRecoveredFlagDisplayPosition(cityIdx, flagPosition);
			objectPosition = flagPosition;
			objectPosition -= GetRendererCenterOffset(flag.transform);
		}

		objectPosition.z = GetCityLayerZ(flagPosition);
		flag.transform.position = objectPosition;
	}

	/// <summary>
	/// 方法说明：读取 MOD06 旗帜显示坐标，旗帜和城池重合时上移旗帜以露出城池标记。
	/// 参数说明：cityIdx 为城池索引，flagPosition 为原始旗帜世界坐标。
	/// 返回说明：返回用于显示的旗帜世界坐标。
	/// </summary>
	private Vector3 GetRecoveredFlagDisplayPosition(int cityIdx, Vector3 flagPosition) {
		if (!Informations.Instance.HasCityPosition(cityIdx)) return flagPosition;

		Vector3 cityPosition = Informations.Instance.GetCityWorldPosition(cityIdx);
		float distance = Vector2.Distance(new Vector2(cityPosition.x, cityPosition.y), new Vector2(flagPosition.x, flagPosition.y));
		if (distance > RecoveredCityMarkerHeight * 0.5f) return flagPosition;

		return flagPosition + new Vector3(0f, RecoveredCoincidentFlagOffsetY, 0f);
	}

	/// <summary>
	/// 方法说明：创建 MOD06 城池本体标记。
	/// 参数说明：cityIdx 为城池索引。
	/// 返回说明：无返回值。
	/// </summary>
	private void CreateRecoveredCityMarker(int cityIdx) {
		if (!Informations.Instance.HasCityPosition(cityIdx)) return;

		Material markerMaterial = GetRecoveredCityMarkerMaterial();
		Mesh markerMesh = GetRecoveredCityMarkerMesh();
		if (markerMaterial == null || markerMesh == null) return;

		Vector3 cityPosition = Informations.Instance.GetCityWorldPosition(cityIdx);
		cityPosition.z = GetCityLayerZ(cityPosition) + 0.02f;

		GameObject go = new GameObject("CityMarker" + cityIdx);
		go.transform.parent = transform;
		go.transform.position = cityPosition;
		go.transform.localRotation = Quaternion.identity;
		go.transform.localScale = Vector3.one;

		MeshFilter meshFilter = go.AddComponent<MeshFilter>();
		meshFilter.sharedMesh = markerMesh;
		MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();
		meshRenderer.sharedMaterial = markerMaterial;
	}

	/// <summary>
	/// 方法说明：读取或创建 MOD06 城池标记材质。
	/// 参数说明：无参数。
	/// 返回说明：成功返回城池标记材质，资源缺失时返回 null。
	/// </summary>
	private Material GetRecoveredCityMarkerMaterial() {
		if (recoveredCityMarkerMaterial != null) return recoveredCityMarkerMaterial;

		Texture2D texture = Resources.Load<Texture2D>(RecoveredCityMarkerResourcePath);
		if (texture == null) {
			Debug.LogError("MOD06 城池标记资源不存在: " + RecoveredCityMarkerResourcePath);
			return null;
		}

		Shader shader = Shader.Find("Unlit/Transparent");
		if (shader == null) {
			Debug.LogError("缺少 Unlit/Transparent Shader，无法显示 MOD06 城池标记。");
			return null;
		}

		recoveredCityMarkerMaterial = new Material(shader);
		recoveredCityMarkerMaterial.mainTexture = texture;
		return recoveredCityMarkerMaterial;
	}

	/// <summary>
	/// 方法说明：读取或创建 MOD06 城池标记网格。
	/// 参数说明：无参数。
	/// 返回说明：返回城池标记四边形网格。
	/// </summary>
	private Mesh GetRecoveredCityMarkerMesh() {
		if (recoveredCityMarkerMesh != null) return recoveredCityMarkerMesh;

		float halfWidth = RecoveredCityMarkerWidth * 0.5f;
		float halfHeight = RecoveredCityMarkerHeight * 0.5f;
		Mesh mesh = new Mesh();
		mesh.name = "RecoveredCityMarkerMesh";
		mesh.vertices = new Vector3[] {
			new Vector3(-halfWidth, -halfHeight, 0f),
			new Vector3(halfWidth, -halfHeight, 0f),
			new Vector3(-halfWidth, halfHeight, 0f),
			new Vector3(halfWidth, halfHeight, 0f)
		};
		mesh.uv = new Vector2[] {
			new Vector2(0f, 0f),
			new Vector2(1f, 0f),
			new Vector2(0f, 1f),
			new Vector2(1f, 1f)
		};
		mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
		mesh.RecalculateBounds();
		recoveredCityMarkerMesh = mesh;
		return recoveredCityMarkerMesh;
	}

	/// <summary>
	/// 方法说明：读取渲染中心相对 Transform 坐标的偏移。
	/// 参数说明：target 为需要测量的 Transform。
	/// 返回说明：有 Renderer 时返回中心偏移，否则返回 Vector3.zero。
	/// </summary>
	private Vector3 GetRendererCenterOffset(Transform target) {
		Renderer renderer = target.GetComponent<Renderer>();
		if (renderer == null) return Vector3.zero;

		return renderer.bounds.center - target.position;
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
