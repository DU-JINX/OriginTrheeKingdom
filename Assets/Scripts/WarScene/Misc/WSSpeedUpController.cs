using UnityEngine;
using System.Collections;

public class WSSpeedUpController : MonoBehaviour {

	private const float NormalTimeScale = 1f;
	private const float SpeedUpTimeScale = 2f;

	private bool isSpeedUp = true;
	private exSpriteFont speedButtonText;

	// 方法说明：初始化战斗场景加速按钮，并默认进入加速状态。
	// 参数说明：无。
	// 返回说明：无返回值。
	void Start () {
		GetComponent<Button>().SetButtonClickHandler(OnClick);
		speedButtonText = GetComponent<exSpriteFont>();
		SetSpeedUp(true);
	}

	// 方法说明：处理战斗场景加速按钮点击，切换普通速度和加速状态。
	// 参数说明：无。
	// 返回说明：无返回值。
	void OnClick() {

		SetSpeedUp(!isSpeedUp);
	}

	// 方法说明：设置战斗场景是否处于加速状态，并同步按钮文字。
	// 参数说明：speedUp 为 true 时使用 2 倍速，为 false 时恢复普通速度。
	// 返回说明：无返回值。
	void SetSpeedUp(bool speedUp) {

		isSpeedUp = speedUp;
		Time.timeScale = isSpeedUp ? SpeedUpTimeScale : NormalTimeScale;
		SetSpeedButtonText();
	}

	// 方法说明：根据当前速度状态刷新战斗加速按钮文字。
	// 参数说明：无。
	// 返回说明：无返回值。
	void SetSpeedButtonText() {

		speedButtonText.text = isSpeedUp ? ZhongWen.Instance.normalSpeed : ZhongWen.Instance.speedUp;
	}
}
