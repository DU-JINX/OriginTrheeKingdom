using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HistoryTimeController : MonoBehaviour {
	
	public StrategyController strCtrl;
	public exSpriteFont font;
	
	private static int month = 0;
	
	private float timeTick = 0;
	private static int timeCount = 0;
	
	// 方法说明：初始化战略地图年月计时器并刷新显示文本。
	// 参数说明：无。
	// 返回说明：无返回值。
	void Start () {
	
		timeTick = 0;
		
		SetText();
	}
	
	// 方法说明：每帧推进战略时间，并在现代地图 UI 中隐藏旧年月框视觉层。
	// 参数说明：无。
	// 返回说明：无返回值。
	void Update () {
		HideLegacyVisualsForModernStrategyMap();
		
		TimePassing();
	}
	
	// 方法说明：按战略地图状态累计月份推进。
	// 参数说明：无。
	// 返回说明：无返回值。
	void TimePassing() {
		
		if (StrategyController.state != StrategyController.State.Normal) return;
		if (StrategySpeedState.IsPaused()) return;
		
		timeTick += Time.deltaTime;
		
		if (timeTick >= 0.5f) {
			timeTick = 0;
			
			strCtrl.AddReservist();
			
			timeCount++;
			if (timeCount >= 10) {
				timeCount = 0;
				
				AddMonth();
			}
		}
	}
	
	// 方法说明：根据当前历史年份和月份刷新旧时间文本，供新版 HUD 读取真实年月。
	// 参数说明：无。
	// 返回说明：无返回值。
	void SetText() {
		
		font.text = ZhongWen.Instance.xiyuan + " " + Controller.historyTime + ZhongWen.Instance.nian;
		
		if (month < 9) {
			
			font.text += " " + (int)(month+1) + ZhongWen.Instance.yue;
		} else {
			
			font.text += "" +  (int)(month+1) + ZhongWen.Instance.yue;
		}
	}
	
	// 方法说明：月份加一并在跨年时触发战略年结流程。
	// 参数说明：无。
	// 返回说明：无返回值。
	void AddMonth() {
		
		month++;
		if (month == 12) {
			month = 0;
			
			Controller.historyTime++;
			
			gameObject.SetActive(false);
			
			strCtrl.OnTimeOver();
			return;
		}
		
		SetText();
		
		strCtrl.MonthAct();
	}

	// 方法说明：重置战略时间内部月份和时间累计。
	// 参数说明：无。
	// 返回说明：无返回值。
	public static void Reset() {
		month = 0;
		timeCount = 0;
	}

	// 方法说明：现代战略地图启用时隐藏旧年月框所有 Renderer，仅保留时间逻辑。
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
