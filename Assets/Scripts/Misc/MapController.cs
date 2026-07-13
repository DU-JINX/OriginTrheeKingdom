using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MapController : MonoBehaviour {

	private Color gray = new Color(0.5f, 0, 0, 1);
	private exSprite[] cities;
	private bool hasMapLocalBounds = false;
	private Vector2 mapLocalMin = Vector2.zero;
	private Vector2 mapLocalMax = Vector2.zero;
	private bool hasBaseLocalPosition = false;
	private bool mapDraggingEnabled = false;
	private bool mapDragActive = false;
	private Vector3 baseMapLocalPosition = Vector3.zero;
	private Vector3 dragStartPointerLocalPosition = Vector3.zero;
	private Vector3 dragStartMapLocalPosition = Vector3.zero;
	private float dragViewportMinX = 0.36f;
	private float dragViewportMaxX = 1f;
	private float dragViewportMinY = 0.27f;
	private float dragViewportMaxY = 1f;

	/// <summary>
	/// 方法说明：初始化地图城池标记。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	void Awake()
	{
		CaptureBaseMapLocalPosition();
		InitCities();
	}

	/// <summary>
	/// 方法说明：处理选君主地图拖动。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	void Update() {
		HandleMapDrag();
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
		}
	}

	/// <summary>
	/// 方法说明：记录地图初始本地坐标，作为聚焦回正的基准。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	private void CaptureBaseMapLocalPosition() {
		if (hasBaseLocalPosition) return;

		baseMapLocalPosition = transform.localPosition;
		hasBaseLocalPosition = true;
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

	/// <summary>
	/// 方法说明：设置当前地图是否允许手势拖动。
	/// 参数说明：enabledFlag 为是否允许拖动。
	/// 返回说明：无返回值。
	/// </summary>
	public void SetMapDraggingEnabled(bool enabledFlag) {
		mapDraggingEnabled = enabledFlag;
		if (!mapDraggingEnabled) {
			mapDragActive = false;
		}
	}

	/// <summary>
	/// 方法说明：把地图位置重置到场景初始位置。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	public void ResetMapPan() {
		CaptureBaseMapLocalPosition();
		transform.localPosition = baseMapLocalPosition;
	}

	/// <summary>
	/// 方法说明：把地图自动聚焦到一组城池中心。
	/// 参数说明：cityIndexes 为需要聚焦的城池索引列表。
	/// 返回说明：无返回值。
	/// </summary>
	public void FocusOnCities(List<int> cityIndexes) {
		InitCities();
		if (cityIndexes == null || cityIndexes.Count == 0) return;

		Vector3 center;
		if (!TryGetCitiesLocalCenter(cityIndexes, out center)) return;

		Rect viewportRect;
		if (!TryGetDragViewportLocalRect(out viewportRect)) return;

		Vector3 viewportCenter = new Vector3(viewportRect.center.x, viewportRect.center.y, baseMapLocalPosition.z);
		Vector3 scaledCenter = Vector3.Scale(center, transform.localScale);
		Vector3 desiredPosition = viewportCenter - scaledCenter;
		desiredPosition.z = baseMapLocalPosition.z;
		SetMapLocalPosition(desiredPosition);
	}

	/// <summary>
	/// 方法说明：处理地图拖动输入。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	private void HandleMapDrag() {
		if (!mapDraggingEnabled) return;

		if (Input.GetMouseButtonDown(0) && IsPointerInMapDragArea()) {
			mapDragActive = true;
			dragStartPointerLocalPosition = GetPointerParentLocalPosition();
			dragStartMapLocalPosition = transform.localPosition;
		} else if (mapDragActive && Input.GetMouseButton(0)) {
			Vector3 delta = GetPointerParentLocalPosition() - dragStartPointerLocalPosition;
			SetMapLocalPosition(dragStartMapLocalPosition + delta);
		} else if (mapDragActive && Input.GetMouseButtonUp(0)) {
			mapDragActive = false;
		}
	}

	/// <summary>
	/// 方法说明：判断当前指针是否在地图拖动区域。
	/// 参数说明：无参数。
	/// 返回说明：在地图区域返回 true，否则返回 false。
	/// </summary>
	private bool IsPointerInMapDragArea() {
		return Input.mousePosition.x >= Screen.width * dragViewportMinX
			&& Input.mousePosition.x <= Screen.width * dragViewportMaxX
			&& Input.mousePosition.y >= Screen.height * dragViewportMinY
			&& Input.mousePosition.y <= Screen.height * dragViewportMaxY;
	}

	/// <summary>
	/// 方法说明：把当前鼠标屏幕坐标转换为地图父节点本地坐标。
	/// 参数说明：无参数。
	/// 返回说明：返回父节点本地坐标。
	/// </summary>
	private Vector3 GetPointerParentLocalPosition() {
		Camera camera = Camera.main;
		if (camera == null) return Vector3.zero;

		float zDistance = Mathf.Abs(camera.transform.position.z - transform.position.z);
		Vector3 worldPosition = camera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, zDistance));
		return GetParentLocalPoint(worldPosition);
	}

	/// <summary>
	/// 方法说明：尝试计算一组城池在地图本地坐标中的中心。
	/// 参数说明：cityIndexes 为城池索引列表，center 输出中心点。
	/// 返回说明：成功计算返回 true，否则返回 false。
	/// </summary>
	private bool TryGetCitiesLocalCenter(List<int> cityIndexes, out Vector3 center) {
		center = Vector3.zero;
		int count = 0;
		for (int i = 0; i < cityIndexes.Count; i++) {
			int cityIdx = (int)cityIndexes[i];
			if (cityIdx < 0 || cityIdx >= Informations.Instance.cityNum) continue;
			if (cities[cityIdx] == null) continue;

			center += cities[cityIdx].transform.localPosition;
			count++;
		}

		if (count <= 0) return false;

		center /= count;
		center.z = 0f;
		return true;
	}

	/// <summary>
	/// 方法说明：设置地图本地坐标并按可视区域约束。
	/// 参数说明：desiredPosition 为期望本地坐标。
	/// 返回说明：无返回值。
	/// </summary>
	private void SetMapLocalPosition(Vector3 desiredPosition) {
		transform.localPosition = ClampMapLocalPosition(desiredPosition);
	}

	/// <summary>
	/// 方法说明：把地图本地坐标限制在可视区域内，减少拖动后露出底色。
	/// 参数说明：desiredPosition 为期望本地坐标。
	/// 返回说明：返回约束后的本地坐标。
	/// </summary>
	private Vector3 ClampMapLocalPosition(Vector3 desiredPosition) {
		Rect viewportRect;
		Rect mapRect;
		if (!TryGetDragViewportLocalRect(out viewportRect) || !TryGetMapParentRect(desiredPosition, out mapRect)) {
			return desiredPosition;
		}

		Vector3 clampedPosition = desiredPosition;
		clampedPosition.x += GetClampDelta(mapRect.xMin, mapRect.xMax, viewportRect.xMin, viewportRect.xMax);
		mapRect.x += clampedPosition.x - desiredPosition.x;
		clampedPosition.y += GetClampDelta(mapRect.yMin, mapRect.yMax, viewportRect.yMin, viewportRect.yMax);
		clampedPosition.z = baseMapLocalPosition.z;
		return clampedPosition;
	}

	/// <summary>
	/// 方法说明：计算一维区间为了覆盖视口需要补偿的位移。
	/// 参数说明：contentMin 为内容最小值，contentMax 为内容最大值，viewMin 为视口最小值，viewMax 为视口最大值。
	/// 返回说明：返回需要补偿的坐标差。
	/// </summary>
	private float GetClampDelta(float contentMin, float contentMax, float viewMin, float viewMax) {
		float contentSize = contentMax - contentMin;
		float viewSize = viewMax - viewMin;
		if (contentSize <= viewSize) {
			return (viewMin + viewMax) * 0.5f - (contentMin + contentMax) * 0.5f;
		}

		if (contentMin > viewMin) {
			return viewMin - contentMin;
		}

		if (contentMax < viewMax) {
			return viewMax - contentMax;
		}

		return 0f;
	}

	/// <summary>
	/// 方法说明：计算地图拖动可视区域在父节点内的本地矩形。
	/// 参数说明：viewportRect 输出本地矩形。
	/// 返回说明：成功返回 true，否则返回 false。
	/// </summary>
	private bool TryGetDragViewportLocalRect(out Rect viewportRect) {
		viewportRect = new Rect();
		Camera camera = Camera.main;
		if (camera == null) return false;

		float zDistance = Mathf.Abs(camera.transform.position.z - transform.position.z);
		Vector3 bottomLeft = camera.ScreenToWorldPoint(new Vector3(Screen.width * dragViewportMinX, Screen.height * dragViewportMinY, zDistance));
		Vector3 topRight = camera.ScreenToWorldPoint(new Vector3(Screen.width * dragViewportMaxX, Screen.height * dragViewportMaxY, zDistance));
		Vector3 localBottomLeft = GetParentLocalPoint(bottomLeft);
		Vector3 localTopRight = GetParentLocalPoint(topRight);
		viewportRect = Rect.MinMaxRect(
			Mathf.Min(localBottomLeft.x, localTopRight.x),
			Mathf.Min(localBottomLeft.y, localTopRight.y),
			Mathf.Max(localBottomLeft.x, localTopRight.x),
			Mathf.Max(localBottomLeft.y, localTopRight.y));
		return true;
	}

	/// <summary>
	/// 方法说明：计算地图背景在父节点中的矩形。
	/// 参数说明：mapLocalPosition 为地图本地坐标，mapRect 输出地图矩形。
	/// 返回说明：成功返回 true，否则返回 false。
	/// </summary>
	private bool TryGetMapParentRect(Vector3 mapLocalPosition, out Rect mapRect) {
		mapRect = new Rect();
		exSprite mapSprite = GetComponent<exSprite>();
		if (mapSprite == null) return false;

		Rect localRect = mapSprite.boundingRect;
		Vector3 scale = transform.localScale;
		float xMin = mapLocalPosition.x + Mathf.Min(localRect.xMin * scale.x, localRect.xMax * scale.x);
		float xMax = mapLocalPosition.x + Mathf.Max(localRect.xMin * scale.x, localRect.xMax * scale.x);
		float yMin = mapLocalPosition.y + Mathf.Min(localRect.yMin * scale.y, localRect.yMax * scale.y);
		float yMax = mapLocalPosition.y + Mathf.Max(localRect.yMin * scale.y, localRect.yMax * scale.y);
		mapRect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
		return true;
	}

	/// <summary>
	/// 方法说明：把世界坐标转换为地图父节点本地坐标。
	/// 参数说明：worldPosition 为世界坐标。
	/// 返回说明：返回父节点本地坐标。
	/// </summary>
	private Vector3 GetParentLocalPoint(Vector3 worldPosition) {
		if (transform.parent == null) {
			return worldPosition;
		}

		return transform.parent.InverseTransformPoint(worldPosition);
	}
}
