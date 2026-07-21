using UnityEngine;
using System.Collections;

public class Button : MonoBehaviour {
	
	public enum ButtonState {
		Normal,
		Down,
		Pressed,
		Leave,
		Clicked
	};
	
	private ButtonState state = ButtonState.Normal;
	private exSpriteFont fontScript;
	private object data = null;
	private static readonly Color NormalTextColor = new Color(1, 1, 1, 1);
	
	public delegate void MessageDelegate();
	private MessageDelegate buttonDownHandler = null;
	private MessageDelegate buttonPressHandler = null;
	private MessageDelegate buttonClickHandler = null;
	
	public delegate void MessageDelegate1(object d);
	private MessageDelegate1 buttonClickHandler1 = null;
	
	/// <summary>
	/// 方法说明：缓存当前按钮的旧字体组件。
	/// 参数说明：无。
	/// 返回说明：无返回值。
	/// </summary>
	void Start () {
		if (fontScript == null) {
			fontScript = GetComponent<exSpriteFont>();
		}
	}
	
	/// <summary>
	/// 方法说明：按钮隐藏时恢复普通状态，避免下一次显示残留按下态。
	/// 参数说明：无。
	/// 返回说明：无返回值。
	/// </summary>
	void OnDisable() {
		SetButtonState(ButtonState.Normal);
	}
	
	/// <summary>
	/// 方法说明：处理普通文字按钮的按下、松开、离开和点击回调。
	/// 参数说明：无。
	/// 返回说明：无返回值。
	/// </summary>
	void Update () {
		
		if (Input.touchCount > 1) return;
		
		if (state == ButtonState.Clicked)
			state = ButtonState.Normal;
		
		if (Input.GetMouseButtonDown(0)) {
			
			if (CheckIsHit()) {
				
				state = ButtonState.Down;
				ApplyNormalTextColor();
				
				if (buttonDownHandler != null)		buttonDownHandler();
			}
		} else if (Input.GetMouseButtonUp(0)) {
			if (state == ButtonState.Down || state == ButtonState.Pressed) {
				
				if (CheckIsHit()) {
					state = ButtonState.Clicked;
					ApplyNormalTextColor();

					if (buttonClickHandler != null)		buttonClickHandler();
					if (buttonClickHandler1 != null)	buttonClickHandler1(data);

					Input.ResetInputAxes();
					SoundController.Instance.PlaySound("00038");
					return;
				}
			}
			
			state = ButtonState.Normal;
		} else if (Input.GetMouseButton(0)) {
			if (state == ButtonState.Down || state == ButtonState.Pressed) {
				
				if (CheckIsHit()) {
					state = ButtonState.Pressed;
					
					if (buttonPressHandler != null)		buttonPressHandler();
				} else {
					state = ButtonState.Leave;
					
					ApplyNormalTextColor();
				}
			} else if (state == ButtonState.Leave) {
				
				if (CheckIsHit()) {
					state = ButtonState.Pressed;
					
					if (buttonPressHandler != null)		buttonPressHandler();
					
					ApplyNormalTextColor();
				}
			}
		}
		
	}
	
	/// <summary>
	/// 方法说明：检测当前指针是否命中文字按钮包围盒。
	/// 参数说明：无。
	/// 返回说明：命中返回 true，否则返回 false。
	/// </summary>
	bool CheckIsHit() {
		
		Vector3 mousePoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		Vector2 point = new Vector2(mousePoint.x, mousePoint.y);
		
		Rect bound = fontScript.boundingRect;
		bound.x += transform.position.x;
		bound.y += transform.position.y;
		
		return bound.Contains(point);
	}
	
	/// <summary>
	/// 方法说明：设置按钮是否可点击，并同步禁用态文字颜色。
	/// 参数说明：flag 为 true 时启用按钮，为 false 时禁用按钮。
	/// 返回说明：无返回值。
	/// </summary>
	public void SetButtonEnable(bool flag) {
		
		enabled = flag;
		
		state = ButtonState.Normal;
		
		if (fontScript == null) {
			fontScript = GetComponent<exSpriteFont>();
		}
		
		if (enabled) {
			fontScript.botColor = NormalTextColor;
			fontScript.topColor = NormalTextColor;
		} else {
			fontScript.botColor = new Color(0.5f, 0.5f, 0.5f, 1);
			fontScript.topColor = new Color(0.5f, 0.5f, 0.5f, 1);
		}
		UnifiedGameFontController.SyncFontNow(fontScript);
	}
	
	/// <summary>
	/// 方法说明：读取按钮当前交互状态。
	/// 参数说明：无。
	/// 返回说明：返回当前 ButtonState。
	/// </summary>
	public ButtonState GetButtonState() {
		return state;
	}
	
	/// <summary>
	/// 方法说明：外部设置按钮交互状态并刷新文字颜色。
	/// 参数说明：s 为目标按钮状态。
	/// 返回说明：无返回值。
	/// </summary>
	public void SetButtonState(ButtonState s) {
		
		state = s;
		
		if (fontScript == null) {
			fontScript = GetComponent<exSpriteFont>();
		}
		
		if (!enabled) return;
		
		ApplyNormalTextColor();
	}

	/// <summary>
	/// 方法说明：普通按钮点击时保持白字，不再做红字放大式反馈。
	/// 参数说明：无。
	/// 返回说明：无返回值。
	/// </summary>
	private void ApplyNormalTextColor() {
		if (fontScript == null) {
			fontScript = GetComponent<exSpriteFont>();
		}
		if (fontScript == null) return;

		fontScript.botColor = NormalTextColor;
		fontScript.topColor = NormalTextColor;
		UnifiedGameFontController.SyncFontNow(fontScript);
	}
	
	/// <summary>
	/// 方法说明：设置按钮携带的数据。
	/// 参数说明：d 为点击回调需要携带的数据。
	/// 返回说明：无返回值。
	/// </summary>
	public void SetButtonData(object d) {
		data = d;
	}
	
	/// <summary>
	/// 方法说明：读取按钮携带的数据。
	/// 参数说明：无。
	/// 返回说明：返回按钮当前携带的数据。
	/// </summary>
	public object GetButtonData() {
		return data;
	}
	
	/// <summary>
	/// 方法说明：设置按钮按下回调。
	/// 参数说明：func 为按下时执行的方法。
	/// 返回说明：无返回值。
	/// </summary>
	public void SetButtonDownHandler(MessageDelegate func) {
		buttonDownHandler = new MessageDelegate(func);
	}
	
	/// <summary>
	/// 方法说明：设置按钮持续按住回调。
	/// 参数说明：func 为按住时执行的方法。
	/// 返回说明：无返回值。
	/// </summary>
	public void SetButtonPressHandler(MessageDelegate func) {
		buttonPressHandler = new MessageDelegate(func);
	}
	
	/// <summary>
	/// 方法说明：设置无参数点击回调。
	/// 参数说明：func 为点击时执行的方法。
	/// 返回说明：无返回值。
	/// </summary>
	public void SetButtonClickHandler(MessageDelegate func) {
		buttonClickHandler = new MessageDelegate(func);
	}
	
	/// <summary>
	/// 方法说明：设置携带数据的点击回调。
	/// 参数说明：func 为点击时执行且接收按钮数据的方法。
	/// 返回说明：无返回值。
	/// </summary>
	public void SetButtonClickHandler(MessageDelegate1 func) {
		buttonClickHandler1 = new MessageDelegate1(func);
	}
}
