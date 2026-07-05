using UnityEngine;

public class StrategySpeedUpController : MonoBehaviour {

	private const float NormalTimeScale = 1f;
	private const float SpeedUpTimeScale = 2f;
	private const float ButtonBaseWidth = 96f;
	private const float ButtonBaseHeight = 34f;
	private const float ButtonBaseTop = 10f;
	private const float ButtonBaseFontSize = 18f;

	private bool isSpeedUp = false;
	private GUIStyle speedButtonStyle;

	// 方法说明：初始化主地图加速状态，进入战略地图时默认恢复正常速度。
	// 参数说明：无。
	// 返回说明：无返回值。
	void Start () {
		SetSpeedUp(false);
	}

	// 方法说明：绘制主地图顶部居中的加速按钮，并处理点击切换。
	// 参数说明：无。
	// 返回说明：无返回值。
	void OnGUI () {
		if (StrategyController.state != StrategyController.State.Normal) {
			return;
		}

		Rect buttonRect = GetSpeedButtonRect();
		GUIStyle buttonStyle = GetSpeedButtonStyle();

		if (GUI.Button(buttonRect, GetSpeedButtonText(), buttonStyle)) {
			ToggleSpeedUp();
		}
	}

	// 方法说明：组件停用时恢复正常速度，避免倍速影响其他场景。
	// 参数说明：无。
	// 返回说明：无返回值。
	void OnDisable () {
		SetSpeedUp(false);
	}

	// 方法说明：组件销毁时恢复正常速度，避免切换场景后残留 Time.timeScale。
	// 参数说明：无。
	// 返回说明：无返回值。
	void OnDestroy () {
		SetSpeedUp(false);
	}

	// 方法说明：判断当前鼠标位置是否位于主地图加速按钮范围内。
	// 参数说明：无。
	// 返回说明：鼠标在按钮范围内返回 true，否则返回 false。
	public static bool IsPointerOverSpeedButton () {
		Rect buttonRect = GetSpeedButtonRect();
		Vector2 mousePosition = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

		return buttonRect.Contains(mousePosition);
	}

	// 方法说明：根据当前屏幕尺寸计算顶部居中按钮的屏幕坐标。
	// 参数说明：无。
	// 返回说明：返回可供 IMGUI 绘制和点击检测使用的按钮矩形。
	private static Rect GetSpeedButtonRect () {
		float scaleX = Screen.width / 640f;
		float scaleY = Screen.height / 480f;
		float width = ButtonBaseWidth * scaleX;
		float height = ButtonBaseHeight * scaleY;
		float left = (Screen.width - width) / 2f;
		float top = ButtonBaseTop * scaleY;

		return new Rect(left, top, width, height);
	}

	// 方法说明：取得主地图加速按钮的当前显示文本。
	// 参数说明：无。
	// 返回说明：倍速中返回“正常”，普通速度返回“加速”。
	private string GetSpeedButtonText () {
		if (isSpeedUp) {
			return ZhongWen.Instance.normalSpeed;
		}

		return ZhongWen.Instance.speedUp;
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
		SetSpeedUp(!isSpeedUp);
	}

	// 方法说明：设置主地图是否使用 2 倍速。
	// 参数说明：speedUp 为 true 时设置 2 倍速，为 false 时恢复正常速度。
	// 返回说明：无返回值。
	private void SetSpeedUp (bool speedUp) {
		isSpeedUp = speedUp;
		Time.timeScale = isSpeedUp ? SpeedUpTimeScale : NormalTimeScale;
	}
}
