using UnityEngine;

public static class StrategySpeedState {

	private const float NormalTimeScale = 1f;
	private const float SpeedUpTimeScale = 2f;

	private static bool isSpeedUp = false;

	// 方法说明：取得战略地图加速状态。
	// 参数说明：无。
	// 返回说明：当前处于战略加速状态时返回 true，否则返回 false。
	public static bool IsSpeedUp() {
		return isSpeedUp;
	}

	// 方法说明：设置战略地图加速状态，并立即应用到当前 Time.timeScale。
	// 参数说明：speedUp 为 true 时记录并应用 2 倍速，为 false 时记录并恢复正常速度。
	// 返回说明：无返回值。
	public static void SetSpeedUp(bool speedUp) {
		isSpeedUp = speedUp;
		ApplyCurrentTimeScale();
	}

	// 方法说明：切换战略地图加速状态，并立即应用到当前 Time.timeScale。
	// 参数说明：无。
	// 返回说明：无返回值。
	public static void ToggleSpeedUp() {
		SetSpeedUp(!isSpeedUp);
	}

	// 方法说明：把记录中的战略地图加速状态重新应用到当前场景。
	// 参数说明：无。
	// 返回说明：无返回值。
	public static void ApplyCurrentTimeScale() {
		Time.timeScale = isSpeedUp ? SpeedUpTimeScale : NormalTimeScale;
	}

	// 方法说明：临时恢复正常 Time.timeScale，但不清除已记录的战略加速状态。
	// 参数说明：无。
	// 返回说明：无返回值。
	public static void ApplyNormalTimeScale() {
		Time.timeScale = NormalTimeScale;
	}

	// 方法说明：取得战略地图加速按钮应显示的文本。
	// 参数说明：无。
	// 返回说明：加速中返回“正常”，未加速返回“加速”。
	public static string GetSpeedButtonText() {
		if (isSpeedUp) {
			return ZhongWen.Instance.normalSpeed;
		}

		return ZhongWen.Instance.speedUp;
	}
}
