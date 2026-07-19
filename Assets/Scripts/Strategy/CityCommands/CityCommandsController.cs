using UnityEngine;
using System.Collections;

public class CityCommandsController : MonoBehaviour {
	
	public StrategyController 			strCtrl;
	
	public MenuDisplayAnim 				cityCommands;
	public CCInformationController 		infoCtrl;
	public DialogueController 			kingDialog;
	
	public ExpeditionController 		expedition;
	public ConscriptionController 		conscription;
	public CCGeneralsInfoController 	generalsInfo;
	public AppointedPrefectController 	prefectAppointed;
	
	public Button[] commands;
	
	private int state = 0;
	private int cityIdx;
	private int commandIdx = -1;
	
	
	private float timeTick;
	private const float CommandForegroundZ = -5f;
	
	// Use this for initialization
	void Start () {
		BringCommandUiToFront();
		for (int i=0; i<commands.Length; i++) {
			
			commands[i].SetButtonData(i);
			commands[i].SetButtonClickHandler(OnCommandsButtonClickHandler);
		}
	}
	
	void OnEnable() {

		BringCommandUiToFront();
		state = 1000;
		commandIdx = -1000;
		timeTick = 0;
		
		kingDialog.gameObject.SetActive(false);
		
		cityCommands.SetAnim(MenuDisplayAnim.AnimType.InsertFromLeft);
		infoCtrl.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.InsertFromRight);
		
		infoCtrl.SetCity(cityIdx);
	}
	
	// Update is called once per frame
	void Update () {

		BringCommandUiToFront();

		if (StrategyController.state != StrategyController.State.Pause) {
			gameObject.SetActive(false);
			return;
		}

		switch (state) {
		case 0:
			OnNormalModeHandler();
			break;
		case 1:
			OnChangingToCommandModeHandler();
			break;
		case 2:
			OnAppointedPrefectModeController();
			break;
		case 1000:
			OnBeginHandler();
			break;
		}
	}
	
	void OnNormalModeHandler() {
		if (Misc.GetBack()) {
			state = 1;
			commandIdx = -1;
			
			cityCommands.SetAnim(MenuDisplayAnim.AnimType.OutToLeft);
			infoCtrl.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.OutToRight);
		}
	}
	
	void OnChangingToCommandModeHandler() {
		timeTick += Time.deltaTime;
		if (timeTick >= 0.2f) {
			timeTick = 0;
			
			gameObject.SetActive(false);
			
			switch (commandIdx) {
			case -1:
				strCtrl.ReturnMainMode();
				break;
			case 0:
				expedition.SetCity(cityIdx);
				break;
			case 1:
				conscription.SetCity(cityIdx);
				break;
			case 2:
				generalsInfo.AddGeneralsList(cityIdx);
				break;
			case 3:
				generalsInfo.AddPrisonsList(cityIdx);
				break;
			case 4:
				prefectAppointed.AddGeneralsList(cityIdx);
				break;
			}
		}
	}
	
	void OnAppointedPrefectModeController() {
		
		if (!kingDialog.IsShowingText() && Input.GetMouseButtonUp(0)) {
			
			state = 0;
			
			for (int i=0; i<commands.Length; i++) {
				
				commands[i].enabled = true;
			}
			
			Input.ResetInputAxes();
			kingDialog.SetDialogueOut(MenuDisplayAnim.AnimType.OutToBottom);
		}
	}
	
	void OnCommandsButtonClickHandler(object data) {
		
		commandIdx = (int)data;
		state = 1;
		
		cityCommands.SetAnim(MenuDisplayAnim.AnimType.OutToLeft);
		infoCtrl.GetComponent<MenuDisplayAnim>().SetAnim(MenuDisplayAnim.AnimType.OutToRight);
	}

	void OnBeginHandler() {
		if (!cityCommands.GetComponent<MenuDisplayAnim>().IsPlaying()) {
			state = 0;
		}
	}

	public void SetCity(int idx) {
		
		BringCommandUiToFront();
		cityIdx = idx;
		
		gameObject.SetActive(true);
		
		if (Informations.Instance.GetCityInfo(idx).king == Controller.kingIndex) {
			for (int i=0; i<commands.Length; i++) {
				commands[i].SetButtonEnable(true);
			}
			
			if (Informations.Instance.GetCityInfo(idx).prisons.Count > 0) {
				commands[3].SetButtonEnable(true);
			} else {
				commands[3].SetButtonEnable(false);
			}
			
			KingInfo kInfo = Informations.Instance.GetKingInfo(Informations.Instance.GetCityInfo(idx).king);
			int kingCityIdx = Informations.Instance.GetGeneralInfo(kInfo.generalIdx).city;
			if (kingCityIdx != idx) {
				commands[4].SetButtonEnable(true);
			} else {
				commands[4].SetButtonEnable(false);
			}
		} else {
			for (int i=0; i<commands.Length; i++) {
				commands[i].SetButtonEnable(false);
			}
		}
	}
	
	public void OnAppointedPrefectMode() {
		
		BringCommandUiToFront();
		state = 2;
		
		gameObject.SetActive(true);
		
		for (int i=0; i<commands.Length; i++) {
			
			commands[i].enabled = false;
		}
		
		string str = ZhongWen.Instance.ming + ZhongWen.Instance.GetGeneralName(Informations.Instance.GetCityInfo(cityIdx).prefect) + 
						ZhongWen.Instance.wei + ZhongWen.Instance.GetCityName(cityIdx) + ZhongWen.Instance.taishou;
			
		kingDialog.SetDialogue(Informations.Instance.GetKingInfo(Controller.kingIndex).generalIdx,
			str, MenuDisplayAnim.AnimType.InsertFromBottom);
	}

	/// <summary>
	/// 方法说明：把城内指令和所有子命令面板固定到战略地图前景层。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	private void BringCommandUiToFront() {
		EnsureForegroundKeeper(cityCommands == null ? null : cityCommands.transform);
		EnsureForegroundKeeper(infoCtrl == null ? null : infoCtrl.transform);
		EnsureForegroundKeeper(kingDialog == null ? null : kingDialog.transform);
		EnsureForegroundKeeper(expedition == null ? null : expedition.transform);
		EnsureForegroundKeeper(conscription == null ? null : conscription.transform);
		EnsureForegroundKeeper(generalsInfo == null ? null : generalsInfo.transform);
		EnsureForegroundKeeper(prefectAppointed == null ? null : prefectAppointed.transform);
	}

	/// <summary>
	/// 方法说明：为目标对象挂载前景层保持器，并立即应用前景 z 值。
	/// 参数说明：target 为目标 Transform。
	/// 返回说明：无返回值。
	/// </summary>
	private void EnsureForegroundKeeper(Transform target) {
		if (target == null) return;

		StrategyCommandForegroundZKeeper keeper = target.GetComponent<StrategyCommandForegroundZKeeper>();
		if (keeper == null) {
			keeper = target.gameObject.AddComponent<StrategyCommandForegroundZKeeper>();
		}

		keeper.SetTargetZ(CommandForegroundZ);
		keeper.ApplyNow();
	}
}

/// <summary>
/// 方法说明：保持城内指令面板位于战略地图和旗帜前景，避免旧 UI 被 MOD06 地图层级遮挡。
/// 参数说明：无。
/// 返回说明：无返回值。
/// </summary>
public class StrategyCommandForegroundZKeeper : MonoBehaviour {
	private float targetZ = -5f;

	/// <summary>
	/// 方法说明：设置需要保持的本地 z 值。
	/// 参数说明：z 为目标本地 z 值。
	/// 返回说明：无返回值。
	/// </summary>
	public void SetTargetZ(float z) {
		targetZ = z;
	}

	/// <summary>
	/// 方法说明：每帧末尾重新应用前景 z 值，覆盖菜单动画的原始 z 回写。
	/// 参数说明：无。
	/// 返回说明：无返回值。
	/// </summary>
	void LateUpdate() {
		ApplyNow();
	}

	/// <summary>
	/// 方法说明：立即把当前 Transform 移到目标前景 z 层。
	/// 参数说明：无。
	/// 返回说明：无返回值。
	/// </summary>
	public void ApplyNow() {
		Vector3 position = transform.localPosition;
		if (Mathf.Approximately(position.z, targetZ)) return;

		transform.localPosition = new Vector3(position.x, position.y, targetZ);
	}
}
