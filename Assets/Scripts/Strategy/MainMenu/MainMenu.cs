using UnityEngine;
using System.Collections;

public class MainMenu : MonoBehaviour {

	private const int CommandLabelSortingOrder = 1200;
	private const float CommandLabelFrontZ = 1.2f;
	
	public StrategyController strCtrl;
	
	public Button[] commands;
	public GameObject[] commandAct;
	
	public GameObject confirmBox;
	public Button okBtn;
	public Button cancelBtn;
	public exSprite bgSprite;
	
	private bool isQuitConfirmMode = false;
	
	// Use this for initialization
	void Start () {
		
	}

	// 方法说明：菜单显示期间持续把刚创建的统一字体镜像提升到面板前景。
	// 参数说明：无。
	// 返回说明：无返回值。
	void LateUpdate() {
		if (commands == null) return;

		for (int i = 0; i < commands.Length; i++) {
			BringCommandLabelToFront(commands[i]);
		}
	}

	// 方法说明：重置战略主菜单的确认框和按钮状态，避免再次打开菜单时残留退出确认或按钮按下态。
	// 参数说明：无。
	// 返回说明：无返回值。
	public void ResetMenuState() {
		isQuitConfirmMode = false;
		if (confirmBox != null) {
			confirmBox.SetActive(false);
		}
		if (commands == null) return;

		for (int i = 0; i < commands.Length; i++) {
			if (commands[i] != null) {
				commands[i].SetButtonEnable(true);
				commands[i].SetButtonState(Button.ButtonState.Normal);
				UnifiedGameFontController.SyncFontNow(commands[i].GetComponent<exSpriteFont>());
				BringCommandLabelToFront(commands[i]);
			}
		}
	}

	// 方法说明：把战略主菜单按钮的动态字体镜像固定到菜单面板前方，避免半透明蓝底把选项字挡成空白。
	// 参数说明：command 为需要调整的菜单按钮。
	// 返回说明：无返回值。
	void BringCommandLabelToFront(Button command) {
		if (command == null) return;

		TextMesh[] labels = command.GetComponentsInChildren<TextMesh>(true);
		for (int i = 0; i < labels.Length; i++) {
			Transform labelTransform = labels[i].transform;
			labelTransform.localPosition = new Vector3(labelTransform.localPosition.x,
			                                           labelTransform.localPosition.y,
			                                           CommandLabelFrontZ);

			Renderer labelRenderer = labels[i].GetComponent<Renderer>();
			if (labelRenderer != null) {
				labelRenderer.sortingOrder = CommandLabelSortingOrder;
			}
		}
	}

	// 方法说明：处理战略主菜单的返回、面板外点击、命令按钮和退出确认交互。
	// 参数说明：无。
	// 返回说明：无返回值。
	void Update () {
		
		if (!isQuitConfirmMode) {
			if (Misc.GetBack()) {
				
				gameObject.SetActive(false);
				strCtrl.ReturnMainMode();
				return;
			}
			
			if (Input.GetMouseButtonUp(0)) {
				Vector3 mousePoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
				Vector2 point = new Vector2(mousePoint.x, mousePoint.y);
				
				Rect bound = bgSprite.boundingRect;
				bound.x += transform.position.x;
				bound.y += transform.position.y;
				
				if (!bound.Contains(point)) {
					
					gameObject.SetActive(false);
					Input.ResetInputAxes();
					strCtrl.ReturnMainMode();
					return;
				}
			}
			
			ProcessClickedCommandButtons();
		} else {
			if (Misc.GetBack()) {
				
				isQuitConfirmMode = false;
				confirmBox.SetActive(false);
				return;
			}
			
			if (okBtn.GetButtonState() == Button.ButtonState.Clicked) {

				Controller.historyTime = 190;
				HistoryTimeController.Reset();
				StrategyController.Reset();
				Informations.Instance.armys.Clear();

				UnityEngine.SceneManagement.SceneManager.LoadScene(0);
				GameObject.Destroy(GameObject.Find("MouseTrack"));
				
			} else if (cancelBtn.GetButtonState() == Button.ButtonState.Clicked) {
				
				isQuitConfirmMode = false;
				confirmBox.SetActive(false);
			}
		}
	}

	/// <summary>
	/// 方法说明：扫描战略主菜单按钮的 Clicked 状态并执行对应命令，供运行时 Update 与编辑器 QA 共用同一条分发路径。
	/// 参数说明：无。
	/// 返回说明：无返回值。
	/// </summary>
	public void ProcessClickedCommandButtons() {
		if (commands == null) return;

		int commandCount = Mathf.Min(4, commands.Length);
		for (int i = 0; i < commandCount; i++) {
			if (commands[i] == null || commands[i].GetButtonState() != Button.ButtonState.Clicked) continue;

			if (i == 3) {
				isQuitConfirmMode = true;
				confirmBox.SetActive(true);

				Vector3 position = commands[i].transform.position;
				float confirmX = position.x + 100 + 128 < 320
					? position.x + 100 + 64
					: position.x - 100 - 64;
				confirmBox.transform.position = new Vector3(confirmX, position.y + 10, confirmBox.transform.position.z);
			} else {
				commandAct[i].SetActive(true);
				gameObject.SetActive(false);
			}
			return;
		}
	}
}
