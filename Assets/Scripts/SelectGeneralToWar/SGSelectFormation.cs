using UnityEngine;
using System.Collections;

public class SGSelectFormation : MonoBehaviour {

	private const float DynamicTextFrontZ = -1.25f;
	private const int DynamicTextSortingOrder = 1750;
	private const float ReadablePanelX = -140f;
	
	public SelectGeneralToWarController sgCtrl;
	
	public Transform token;
	public Button[] formations;
	
	private int generalIdx;
	private MenuDisplayAnim menuAnim;
	
	// Use this for initialization
	void Start () {
		
		if (menuAnim == null)
			menuAnim = GetComponent<MenuDisplayAnim>();
		
		for (int i=0; i<formations.Length; i++) {
			
			formations[i].SetButtonData(i);
			formations[i].SetButtonClickHandler(OnButtonClick);
		}
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

	// 方法说明：持续把阵形选项的动态字体提升到右侧面板前方。
	// 参数说明：无。
	// 返回说明：无返回值。
	void LateUpdate() {
		UnifiedGameFontController.SetDynamicTextLayer(gameObject, DynamicTextFrontZ, DynamicTextSortingOrder);
	}
	
	void OnButtonClick(object idx) {
		
		int i = (int)idx;
		
		Informations.Instance.GetGeneralInfo(generalIdx).formationCur = 1 << i;
		token.position = new Vector3(token.position.x, formations[i].transform.position.y, token.position.z);
		
		menuAnim.SetAnim(MenuDisplayAnim.AnimType.OutToRight);
		Invoke("ReturnMain", 0.2f);
		
		sgCtrl.UpdateGeneralInfo();
	}
	
	void ReturnMain() {
		
		gameObject.SetActive(false);
		
		sgCtrl.OnReturnMain();
	}
	
	public void SetGeneral(int gIdx) {
		
		generalIdx = gIdx;
		
		if (menuAnim == null)
			menuAnim = GetComponent<MenuDisplayAnim>();
		ApplyReadablePanelLayout();
		
		menuAnim.SetAnim(MenuDisplayAnim.AnimType.InsertFromRight);
		
		GeneralInfo gInfo = Informations.Instance.GetGeneralInfo(gIdx);
		
		for (int i=0; i<formations.Length; i++) {
			
			if ((gInfo.formation & (1 << i)) == 0) {
				
				formations[i].SetButtonEnable(false);
			} else {
				
				formations[i].SetButtonEnable(true);
				if (gInfo.formationCur == (1 << i)) {
					token.position = new Vector3(token.position.x, formations[i].transform.position.y, token.position.z);
				}
			}
		}
	}

	// 方法说明：把阵形列表从旧版屏幕外坐标移到当前宽屏右侧可见区域，并同步动画终点。
	// 参数说明：无。
	// 返回说明：无返回值。
	void ApplyReadablePanelLayout() {
		Vector3 panelPosition = transform.localPosition;
		panelPosition.x = ReadablePanelX;
		transform.localPosition = panelPosition;
		if (menuAnim == null) return;

		Vector3 originalPosition = menuAnim.GetOriginalPosition();
		originalPosition.x = ReadablePanelX;
		menuAnim.SetOriginalPosition(originalPosition);
	}
}
