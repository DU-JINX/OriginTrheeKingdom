using UnityEngine;
using System.Collections;

public class BackController : MonoBehaviour {
	
	private Button bottonCtrl;
	
	// 方法说明：初始化返回按钮控制器并注册全局返回按钮对象。
	// 参数说明：无。
	// 返回说明：无返回值。
	void Start () {
		bottonCtrl = GetComponent<Button>();
		Misc.backButton = gameObject;
		gameObject.SetActive(false);
	}
	
	// 方法说明：处理返回按钮点击状态，并在现代战略地图 UI 中隐藏旧按钮视觉层。
	// 参数说明：无。
	// 返回说明：无返回值。
	void Update () {
		HideLegacyVisualsForModernStrategyMap();
		
		if (Misc.isBack) {
			Misc.isBack = false;
			gameObject.SetActive(false);
			HideLegacyVisualsForModernStrategyMap();
		}
		
		if (bottonCtrl != null && bottonCtrl.GetButtonState() == Button.ButtonState.Clicked) {
			Misc.isBack = true;
		}

		if (Misc.isNeedBack) {
			Misc.isNeedBack = false;
		} else {
			Misc.isBack = false;
			gameObject.SetActive(false);
		}

		HideLegacyVisualsForModernStrategyMap();
	}

	// 方法说明：销毁返回按钮时清理全局返回状态。
	// 参数说明：无。
	// 返回说明：无返回值。
	void OnDestroy() {
		Misc.isBack = false;
	}

	// 方法说明：现代战略地图启用时隐藏旧返回按钮所有 Renderer，仅保留返回逻辑。
	// 参数说明：无。
	// 返回说明：无返回值。
	private void HideLegacyVisualsForModernStrategyMap() {
		if (!StrategyMapHudController.ShouldUseModernStrategyMapUi()) return;

		Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
		for (int i = 0; i < renderers.Length; i++) {
			renderers[i].enabled = false;
		}
	}
}
