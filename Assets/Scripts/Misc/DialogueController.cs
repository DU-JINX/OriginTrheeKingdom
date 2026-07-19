using UnityEngine;
using System.Collections;

public class DialogueController : MonoBehaviour {
	
	public GeneralsHeadSelect headSelct;
	
	private exSpriteFont font;
	private MenuDisplayAnim menuAnim;
	
	private int state;
	private string text;
	private int textIdx;
	private bool isShowingText;
	
	private float timeTick;
	
	// Use this for initialization
	void Start () {
		
	}
	
	void OnEnable() {
		
		if (menuAnim == null) {
			menuAnim = GetComponent<MenuDisplayAnim>();
		}
		
		if (font == null) {
			font = transform.Find("Font").GetComponent<exSpriteFont>();
		}
	}
	
	// Update is called once per frame
	void Update () {
		switch (state) {
		case 0:
			timeTick += Time.deltaTime;
			if (timeTick >= 0.2f) {
				state = 1;
				timeTick = 0;
				if (font == null || string.IsNullOrEmpty(font.text)) {
					textIdx = 0;
				}
			}
			break;
		case 1:
			
			if (Input.GetMouseButtonUp(0)) {
				Input.ResetInputAxes();
				
				state = 2;
				isShowingText = false;
				font.text = "";
				for (int i=0; i<text.Length; i++) {
					if (text[i] == ' ') continue;
					
					font.text += text[i];
				}
				UnifiedGameFontController.SyncFontNow(font);
				break;
			}
			
			timeTick += Time.deltaTime;
			if (timeTick >= 0.05f) {
				timeTick = 0;
				
				AppendNextVisibleCharacter();
				if (textIdx >= text.Length) {
					state = 2;
					isShowingText = false;
				}
			}
			break;
		case 2:
			
			break;
		case 3:
			if (!menuAnim.IsPlaying()) {
				state = -1;
			}
			break;
		case 1000:
			state = 1;
			break;
		}
	}
	
	public void SetText(string t) {
		isShowingText = true;
		state = 1000;
		textIdx = 0;
		timeTick = 0;
		
		if (font == null) {
			font = transform.Find("Font").GetComponent<exSpriteFont>();
		}
		font.text = "";
		UnifiedGameFontController.SyncFontNow(font);
		
		text = t;
		AppendNextVisibleCharacter();
		//text.Replace("  ", "");
		
		Input.ResetInputAxes();
	}

	// 方法说明：向对白框追加下一个非空格字符，并立即同步动态字体，避免对白框首帧空白。
	// 参数说明：无。
	// 返回说明：无返回值。
	void AppendNextVisibleCharacter() {
		if (font == null || string.IsNullOrEmpty(text) || textIdx >= text.Length) return;

		while (textIdx < text.Length && text[textIdx] == ' ') {
			textIdx++;
		}
		if (textIdx >= text.Length) return;

		font.text += text[textIdx];
		textIdx++;
		UnifiedGameFontController.SyncFontNow(font);
	}
	
	public void SetHeadIndex(int idx) {
		headSelct.SetGeneralHead(idx);
	}
	
	public bool IsShowingText() {
		return isShowingText;
	}
	
	public void SetDialogueInset(MenuDisplayAnim.AnimType type) {
		state = 0;
		timeTick = 0;
		
		if (menuAnim == null) {
			menuAnim = GetComponent<MenuDisplayAnim>();
		}
		
		menuAnim.SetAnim(type);
	}
	
	public void SetDialogueOut(MenuDisplayAnim.AnimType type) {
		state = 3;
		
		if (menuAnim == null) {
			menuAnim = GetComponent<MenuDisplayAnim>();
		}
		
		menuAnim.SetAnim(type);
	}
	
	public void SetDialogue(int gHeadIndex, string t, MenuDisplayAnim.AnimType animType) {
		gameObject.SetActive(true);
		
		SetHeadIndex(gHeadIndex);
		
		SetText(t);
		
		SetDialogueInset(animType);
	}
}
