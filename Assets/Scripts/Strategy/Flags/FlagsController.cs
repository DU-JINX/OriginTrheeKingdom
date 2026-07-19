using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class FlagsController : MonoBehaviour {

	public GameObject cityNamePerfab;
	public exSpriteAnimation[] flags;

	private const float RecoveredCityNameOffsetY = -22f;
	private const float RecoveredFlagEdgePadding = 18f;
	private const float RecoveredCityNameEdgePaddingX = 46f;
	private const float RecoveredCityNameEdgePaddingY = 26f;
	private readonly List<GameObject> cityAnnotationObjects = new List<GameObject>();
	private readonly List<Renderer> pausedFlagRenderers = new List<Renderer>();
	private readonly List<bool> pausedFlagRendererStates = new List<bool>();
	private bool hasPausedFlagRenderers;
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
		string cityName = ZhongWen.Instance.GetCityName(cityIdx);
		Vector3 localPosition = new Vector3(8f, -8f, 0f);
		CreateDefaultCityNameOutline(flagTransform, cityIdx, cityName, localPosition);

		GameObject go = (GameObject)Instantiate(cityNamePerfab);
		go.name = "CityName" + cityIdx;
		go.transform.parent = flagTransform;
		go.transform.localPosition = localPosition;
		exSpriteFont cityNameFont = go.GetComponent<exSpriteFont>();
		cityNameFont.text = cityName;
		SetCityNameColors(cityNameFont, Color.white, Color.white);
		RegisterCityAnnotationRenderer(go.GetComponent<Renderer>());
		return go;
	}

	/// <summary>
	/// 方法说明：在原版旗帜本地坐标下为城池名称创建八方向黑色描边。
	/// 参数说明：flagTransform 为旗帜节点，cityIdx 为城池索引，cityName 为名称，localPosition 为原版名称坐标。
	/// 返回说明：无返回值。
	/// </summary>
	private void CreateDefaultCityNameOutline(Transform flagTransform, int cityIdx, string cityName, Vector3 localPosition) {
		for (int i = 0; i < RecoveredCityNameShadowOffsets.Length; i++) {
			GameObject shadow = (GameObject)Instantiate(cityNamePerfab);
			shadow.name = "CityNameOutline" + cityIdx + "_" + i;
			shadow.transform.parent = flagTransform;
			shadow.transform.localPosition = localPosition + RecoveredCityNameShadowOffsets[i];
			exSpriteFont shadowFont = shadow.GetComponent<exSpriteFont>();
			shadowFont.text = cityName;
			SetCityNameColors(shadowFont, new Color(0f, 0f, 0f, 0.92f), new Color(0f, 0f, 0f, 0.92f));
			RegisterCityAnnotationRenderer(shadow.GetComponent<Renderer>());
		}
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
		labelPosition = ClampRecoveredMapAnnotationPosition(labelPosition,
		                                                    RecoveredCityNameEdgePaddingX,
		                                                    RecoveredCityNameEdgePaddingY);
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
		RegisterCityAnnotationRenderer(go.GetComponent<Renderer>());
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
			objectPosition = ClampRecoveredMapAnnotationPosition(flagPosition,
			                                                      RecoveredFlagEdgePadding,
			                                                      RecoveredFlagEdgePadding);
			objectPosition -= GetRendererCenterOffset(flag.transform);
		}

		objectPosition.z = GetCityLayerZ(flagPosition);
		flag.transform.position = objectPosition;
	}

	/// <summary>
	/// 方法说明：登记由战略地图动态创建的城名渲染器。
	/// 参数说明：targetRenderer 为需要随菜单开关显示状态的渲染器。
	/// 返回说明：无返回值。
	/// </summary>
	private void RegisterCityAnnotationRenderer(Renderer targetRenderer) {
		if (targetRenderer == null) return;

		targetRenderer.enabled = true;
		GameObject annotationObject = targetRenderer.gameObject;
		if (!cityAnnotationObjects.Contains(annotationObject)) {
			cityAnnotationObjects.Add(annotationObject);
		}
	}

	/// <summary>
	/// 方法说明：统一切换城名，避免旧菜单打开时地图文字穿透面板。
	/// 参数说明：visible 为 true 时显示地图标注，为 false 时隐藏。
	/// 返回说明：无返回值。
	/// </summary>
	private void SetCityAnnotationsVisible(bool visible) {
		Renderer[] childRenderers = GetComponentsInChildren<Renderer>(true);
		for (int i = 0; i < childRenderers.Length; i++) {
			Renderer childRenderer = childRenderers[i];
			if (childRenderer == null) continue;

			string objectName = childRenderer.gameObject.name;
			if (objectName.StartsWith("CityName", StringComparison.Ordinal)) {
				RegisterCityAnnotationRenderer(childRenderer);
			}
		}

		for (int i = cityAnnotationObjects.Count - 1; i >= 0; i--) {
			GameObject annotationObject = cityAnnotationObjects[i];
			if (annotationObject == null) {
				cityAnnotationObjects.RemoveAt(i);
				continue;
			}

			annotationObject.SetActive(visible);
		}
	}

	/// <summary>
	/// 方法说明：把 MOD06 旗帜或城名中心限制在地图安全边距内，避免全图视野下被屏幕四边裁切。
	/// 参数说明：position 为原世界坐标，paddingX 和 paddingY 为横纵安全边距。
	/// 返回说明：返回限制后的世界坐标，z 保持不变。
	/// </summary>
	private Vector3 ClampRecoveredMapAnnotationPosition(Vector3 position, float paddingX, float paddingY) {
		float halfWidth = MODLoadController.RecoveredMapWorldWidth * 0.5f;
		float halfHeight = MODLoadController.RecoveredMapWorldHeight * 0.5f;
		position.x = Mathf.Clamp(position.x, -halfWidth + paddingX, halfWidth - paddingX);
		position.y = Mathf.Clamp(position.y, -halfHeight + paddingY, halfHeight - paddingY);
		return position;
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
			Debug.LogWarning("缺少势力旗帜资源，使用当前旗帜动画池: " + kingName);
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
	/// 方法说明：暂停所有可见旗帜动画，并隐藏城名和旗帜避免菜单打开时地图元素穿透面板。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	public void SetFlagsAnimPause() {
		SetCityAnnotationsVisible(false);
		HideFlagRenderers();
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
		SetCityAnnotationsVisible(true);
		RestoreFlagRenderers();
		for (int i=0; i<Informations.Instance.cityNum; i++) {
			exSpriteAnimation flag = GetOrCreateFlag(i);
			if (flag != null && flag.GetComponent<Renderer>().enabled) {
				flag.Resume();
			}
		}
	}

	/// <summary>
	/// 方法说明：记录并隐藏所有城池旗帜渲染器，避免半透明菜单下露出旗帜。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	private void HideFlagRenderers() {
		if (hasPausedFlagRenderers) return;

		// 1. 逐个记录当前显示状态，后续恢复时按原状态还原。
		for (int i = 0; i < Informations.Instance.cityNum; i++) {
			exSpriteAnimation flag = GetOrCreateFlag(i);
			if (flag == null) continue;

			Renderer flagRenderer = flag.GetComponent<Renderer>();
			if (flagRenderer == null) continue;

			pausedFlagRenderers.Add(flagRenderer);
			pausedFlagRendererStates.Add(flagRenderer.enabled);
			flagRenderer.enabled = false;
		}

		// 2. 标记已经隐藏，避免重复进入暂停时把 false 状态覆盖成原状态。
		hasPausedFlagRenderers = true;
	}

	/// <summary>
	/// 方法说明：恢复暂停时隐藏的城池旗帜渲染器。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	private void RestoreFlagRenderers() {
		if (!hasPausedFlagRenderers) return;

		for (int i = 0; i < pausedFlagRenderers.Count; i++) {
			Renderer flagRenderer = pausedFlagRenderers[i];
			if (flagRenderer != null) {
				flagRenderer.enabled = pausedFlagRendererStates[i];
			}
		}

		pausedFlagRenderers.Clear();
		pausedFlagRendererStates.Clear();
		hasPausedFlagRenderers = false;
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
