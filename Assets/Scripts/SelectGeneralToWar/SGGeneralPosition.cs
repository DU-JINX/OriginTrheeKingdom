using UnityEngine;
using System.Collections;

public class SGGeneralPosition : MonoBehaviour {

	private const float DynamicTextFrontZ = -1.25f;
	private const int DynamicTextSortingOrder = 1750;
	
	public SelectGeneralToWarController sgCtrl;
	
	public Button front;
	public Button back;
	public MenuDisplayAnim menuAnim;
	
	// Use this for initialization
	void Start () {
		
		front.SetButtonClickHandler(OnClickFront);
		back.SetButtonClickHandler(OnClickBack);
	}
	
	void OnEnable() {
		
		menuAnim.SetAnim(MenuDisplayAnim.AnimType.InsertFromRight);
	}
	
	// Update is called once per frame
	void Update () {
	
		if (!menuAnim.IsPlaying()) {
			if (Misc.GetBack()) {
				menuAnim.SetAnim(MenuDisplayAnim.AnimType.OutToRight);
				Invoke("ReturnMain", 0.2f);
			}
		}
	}

	// 方法说明：持续把前列和后列选项的动态字体提升到弹窗前方。
	// 参数说明：无。
	// 返回说明：无返回值。
	void LateUpdate() {
		UnifiedGameFontController.SetDynamicTextLayer(gameObject, DynamicTextFrontZ, DynamicTextSortingOrder);
	}
	
	void OnClickFront() {
		
		sgCtrl.OnSelectGeneralPosition(1);
		
		menuAnim.SetAnim(MenuDisplayAnim.AnimType.OutToRight);
		Invoke("ReturnMain", 0.2f);
	}
	
	void OnClickBack() {
		
		sgCtrl.OnSelectGeneralPosition(0);
		
		menuAnim.SetAnim(MenuDisplayAnim.AnimType.OutToRight);
		Invoke("ReturnMain", 0.2f);
	}
	
	void ReturnMain() {
		
		gameObject.SetActive(false);
		
		sgCtrl.OnReturnMain();
	}
}
