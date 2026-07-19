using UnityEngine;
using System.Collections;

public class SGRetreat : MonoBehaviour {

	private const float DynamicTextFrontZ = -1.25f;
	private const int DynamicTextSortingOrder = 1750;
	
	public SelectGeneralToWarController sgCtrl;
	
	public Button okButton;
	public Button cancelButton;
	
	// Use this for initialization
	void Start () {
		
		okButton.SetButtonClickHandler(OnOKButton);
		cancelButton.SetButtonClickHandler(OnCancelButton);
	}
	
	// Update is called once per frame
	void Update () {
		
		if (Misc.GetBack()) {
			OnCancelButton();
		}
	}

	// 方法说明：持续把退兵确认文字和按钮文字提升到确认框前方。
	// 参数说明：无。
	// 返回说明：无返回值。
	void LateUpdate() {
		UnifiedGameFontController.SetDynamicTextLayer(gameObject, DynamicTextFrontZ, DynamicTextSortingOrder);
	}
	
	void OnOKButton() {
		
		gameObject.SetActive(false);
		sgCtrl.OnRetreat();
	}
	
	void OnCancelButton() {
		
		gameObject.SetActive(false);
		sgCtrl.OnReturnMain();
	}
}
