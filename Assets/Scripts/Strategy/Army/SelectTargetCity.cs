using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SelectTargetCity : MonoBehaviour {
	
	public StrategyController strCtrl;
	public MyPathfinding path;
	
	public FlagsController flagsCtrl;
	
	private ArmyInfo armyInfo;
	
	private bool isMouseMove;
	private Vector3 mouseDownPos;
	
	// 方法说明：初始化选城控制器。
	// 参数说明：无。
	// 返回说明：无返回值。
	void Start () {

	}
	
	// 方法说明：处理出征目标城选择、返回和地图拖动。
	// 参数说明：无。
	// 返回说明：无返回值。
	void Update () {
		
		// 1. 返回按钮优先退出目标城选择。
		if (Misc.GetBack()) {
			gameObject.SetActive(false);
			strCtrl.ReturnMainMode();
			return;
		}
		
		// 2. 鼠标按下时记录起点，用于区分点击和拖动。
		if (Input.GetMouseButtonDown(0)) {
				
			isMouseMove = false;
			mouseDownPos = Input.mousePosition;
			
		} else if (!isMouseMove && Input.GetMouseButtonUp(0)) {
			
			// 3. 鼠标松开且没有拖动时，命中城池则改派部队目标。
			int targetCity = flagsCtrl.GetTouchCityIdx();
			if (targetCity != -1) {
				
				armyInfo.armyCtrl.SetRoute(path.GetRoute(armyInfo.armyCtrl.transform.position, targetCity));
				armyInfo.armyCtrl.SetArmyRunning();
				if (targetCity == armyInfo.cityFrom) {
					armyInfo.cityFrom = armyInfo.cityTo;
				}
				armyInfo.cityTo = targetCity;
				
				gameObject.SetActive(false);
				strCtrl.ReturnMainMode();

				Input.ResetInputAxes();
			}
		} else if (Input.GetMouseButton(0)) {
			
			// 4. 鼠标拖动时移动相机，并限制在战略地图边界内。
			if (!isMouseMove) {
				
				Vector3 offset = StrategyController.GetCameraDragOffset(mouseDownPos, Input.mousePosition);
				if (Mathf.Abs(offset.x) > 5 || Mathf.Abs(offset.y) > 5) {
					
					isMouseMove = true;
					mouseDownPos = Input.mousePosition;
				}
			} else {
				
				Vector3 offset = StrategyController.GetCameraDragOffset(mouseDownPos, Input.mousePosition);
				mouseDownPos = Input.mousePosition;
					
				Vector3 pos = Camera.main.transform.position;
				
				pos += offset;
				pos = StrategyController.ClampCameraPosition(pos);
				
				Camera.main.transform.position = pos;
			}
		}
	}
	
	// 方法说明：进入目标城选择状态并记录当前出征部队。
	// 参数说明：a 为当前需要重新选择目标城的部队。
	// 返回说明：无返回值。
	public void SetArmy(ArmyInfo a) {

		armyInfo = a;
		gameObject.SetActive(true);
	}
}
