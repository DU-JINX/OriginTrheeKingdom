using UnityEngine;

public class StrategySpeedUpController : MonoBehaviour {

	private const float ButtonBaseWidth = 96f;
	private const float ButtonBaseHeight = 34f;
	private const float ButtonBaseTop = 10f;
	private const float ButtonBaseFontSize = 18f;
	private const float ButtonBaseGap = 8f;

	private GUIStyle speedButtonStyle;

	// 方法说明：组件启用时恢复已记录的战略地图加速状态。
	// 参数说明：无。
	// 返回说明：无返回值。
	void OnEnable () {
		StrategySpeedState.ApplyCurrentTimeScale();
	}

	// 方法说明：初始化主地图加速状态，进入战略地图时恢复上次记录的加速状态。
	// 参数说明：无。
	// 返回说明：无返回值。
	void Start () {
		StrategySpeedState.ApplyCurrentTimeScale();
	}

	// 方法说明：绘制主地图顶部居中的加速和暂停按钮，并处理点击切换。
	// 参数说明：无。
	// 返回说明：无返回值。
	void OnGUI () {
		if (StrategyMapHudController.IsActive()) {
			return;
		}

		if (StrategyController.state != StrategyController.State.Normal) {
			return;
		}

		Rect speedButtonRect = GetSpeedButtonRect();
		Rect pauseButtonRect = GetPauseButtonRect();
		GUIStyle buttonStyle = GetSpeedButtonStyle();

		if (GUI.Button(speedButtonRect, GetSpeedButtonText(), buttonStyle)) {
			ToggleSpeedUp();
		}

		if (GUI.Button(pauseButtonRect, GetPauseButtonText(), buttonStyle)) {
			TogglePaused();
		}
	}

	// 方法说明：组件停用时临时恢复正常速度，但保留战略地图加速状态。
	// 参数说明：无。
	// 返回说明：无返回值。
	void OnDisable () {
		StrategySpeedState.ApplyNormalTimeScale();
	}

	// 方法说明：组件销毁时临时恢复正常速度，但保留战略地图加速状态。
	// 参数说明：无。
	// 返回说明：无返回值。
	void OnDestroy () {
		StrategySpeedState.ApplyNormalTimeScale();
	}

	// 方法说明：判断当前鼠标位置是否位于主地图加速或暂停按钮范围内。
	// 参数说明：无。
	// 返回说明：鼠标在任一按钮范围内返回 true，否则返回 false。
	public static bool IsPointerOverSpeedButton () {
		if (StrategyMapHudController.IsActive()) {
			return false;
		}

		Vector2 mousePosition = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

		return GetSpeedButtonRect().Contains(mousePosition) || GetPauseButtonRect().Contains(mousePosition);
	}

	// 方法说明：根据当前屏幕尺寸计算顶部加速按钮的屏幕坐标。
	// 参数说明：无。
	// 返回说明：返回可供 IMGUI 绘制和点击检测使用的按钮矩形。
	private static Rect GetSpeedButtonRect () {
		float scaleX = Screen.width / 640f;
		float scaleY = Screen.height / 480f;
		float width = ButtonBaseWidth * scaleX;
		float height = ButtonBaseHeight * scaleY;
		float gap = ButtonBaseGap * scaleX;
		float left = (Screen.width - width * 2f - gap) / 2f;
		float top = ButtonBaseTop * scaleY;

		return new Rect(left, top, width, height);
	}

	// 方法说明：根据当前屏幕尺寸计算顶部暂停按钮的屏幕坐标。
	// 参数说明：无。
	// 返回说明：返回可供 IMGUI 绘制和点击检测使用的按钮矩形。
	private static Rect GetPauseButtonRect () {
		Rect speedButtonRect = GetSpeedButtonRect();
		float gap = ButtonBaseGap * (Screen.width / 640f);

		return new Rect(speedButtonRect.xMax + gap,
		                speedButtonRect.y,
		                speedButtonRect.width,
		                speedButtonRect.height);
	}

	// 方法说明：取得主地图加速按钮的当前显示文本。
	// 参数说明：无。
	// 返回说明：倍速中返回“正常”，普通速度返回“加速”。
	private string GetSpeedButtonText () {
		return StrategySpeedState.GetSpeedButtonText();
	}

	// 方法说明：取得主地图暂停按钮的当前显示文本。
	// 参数说明：无。
	// 返回说明：暂停中返回“继续”，未暂停返回“暂停”。
	private string GetPauseButtonText () {
		return StrategySpeedState.GetPauseButtonText();
	}

	// 方法说明：取得并刷新 IMGUI 按钮样式，使按钮在不同分辨率下保持可读。
	// 参数说明：无。
	// 返回说明：返回当前帧使用的按钮样式。
	private GUIStyle GetSpeedButtonStyle () {
		if (speedButtonStyle == null) {
			speedButtonStyle = new GUIStyle(GUI.skin.button);
			speedButtonStyle.alignment = TextAnchor.MiddleCenter;
			speedButtonStyle.normal.textColor = Color.white;
			speedButtonStyle.hover.textColor = Color.white;
			speedButtonStyle.active.textColor = Color.red;
		}

		speedButtonStyle.fontSize = Mathf.RoundToInt(ButtonBaseFontSize * Mathf.Min(Screen.width / 640f, Screen.height / 480f));

		return speedButtonStyle;
	}

	// 方法说明：切换主地图加速状态。
	// 参数说明：无。
	// 返回说明：无返回值。
	private void ToggleSpeedUp () {
		StrategySpeedState.ToggleSpeedUp();
	}

	// 方法说明：切换主地图暂停状态。
	// 参数说明：无。
	// 返回说明：无返回值。
	private void TogglePaused () {
		StrategySpeedState.TogglePaused();
	}
}
