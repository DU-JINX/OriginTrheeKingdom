using UnityEngine;

public static class StrategySpeedState {

	private const float NormalTimeScale = 1f;
	private const float SpeedUpTimeScale = 2f;
	private static bool isSpeedUp = false;
	private static bool isPaused = false;

	// 方法说明：取得战略地图加速状态。
	// 参数说明：无。
	// 返回说明：当前处于战略加速状态时返回 true，否则返回 false。
	public static bool IsSpeedUp() {
		return isSpeedUp;
	}

	// 方法说明：取得战略地图暂停状态。
	// 参数说明：无。
	// 返回说明：当前处于战略暂停状态时返回 true，否则返回 false。
	public static bool IsPaused() {
		return isPaused;
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

	// 方法说明：设置战略地图暂停状态，并立即应用到当前 Time.timeScale。
	// 参数说明：paused 为 true 时暂停战略地图推进但保留界面动画，为 false 时恢复到记录的正常或加速状态。
	// 返回说明：无返回值。
	public static void SetPaused(bool paused) {
		isPaused = paused;
		ApplyCurrentTimeScale();
	}

	// 方法说明：切换战略地图暂停状态，并立即应用到当前 Time.timeScale。
	// 参数说明：无。
	// 返回说明：无返回值。
	public static void TogglePaused() {
		SetPaused(!isPaused);
	}

	// 方法说明：把记录中的战略地图加速和暂停状态重新应用到当前场景。
	// 参数说明：无。
	// 返回说明：无返回值。
	public static void ApplyCurrentTimeScale() {
		if (isPaused) {
			Time.timeScale = NormalTimeScale;
			return;
		}

		Time.timeScale = isSpeedUp ? SpeedUpTimeScale : NormalTimeScale;
	}

	// 方法说明：临时恢复正常 Time.timeScale，但不清除已记录的战略加速和暂停状态。
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

	// 方法说明：取得战略地图暂停按钮应显示的文本。
	// 参数说明：无。
	// 返回说明：暂停中返回“继续”，未暂停返回“暂停”。
	public static string GetPauseButtonText() {
		if (isPaused) {
			return ZhongWen.Instance.resume;
		}

		return ZhongWen.Instance.pause;
	}
}
