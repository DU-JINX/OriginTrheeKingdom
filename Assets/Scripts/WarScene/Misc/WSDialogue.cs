using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WSDialogue : MonoBehaviour {

	private const float SpeakerPortraitOffsetX = 205f;
	private const float SpeakerPortraitOffsetY = -8f;
	private const int SpeakerSortingOrder = 1800;
	
	private exSpriteFont font;
	private GeneralsHeadSelect speakerHead;
	private TextMesh speakerName;
	private Font speakerNameFont;
	private WSInfoPanel battleInfoPanel;
	private Vector3 dialogueFontOriginalLocalPosition;
	private bool hasDialogueFontOriginalLocalPosition;
	private readonly List<Renderer> hiddenBattleInfoRenderers = new List<Renderer>();
	private readonly List<bool> hiddenBattleInfoRendererStates = new List<bool>();
	private bool hasHiddenBattleInfoPanel;
	
	private string text;
	private bool isShowingText;
	private int textIdx;
	private float timeTick;
	
	private float offset = 50;
	
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
		if (isShowingText) {
			if (Input.GetMouseButtonUp(0)) {
				Input.ResetInputAxes();
				
				isShowingText = false;
				font.text = "";
				for (int i=0; i<text.Length; i++) {
					if (text[i] == ' ') continue;
					
					font.text += text[i];
				}
				UnifiedGameFontController.SyncFontNow(font);
				return;
			}
			
			if ((Time.realtimeSinceStartup - timeTick) >= 0.05f) {
				timeTick = Time.realtimeSinceStartup;
				
				AppendNextVisibleCharacter();
				if (textIdx >= text.Length) {
					isShowingText = false;
				}
			}
		}
	}

	// 方法说明：持续把战斗对白正文提升到蓝色对白框和说话人立绘前方。
	// 参数说明：无。
	// 返回说明：无返回值。
	void LateUpdate() {
		if (font == null) return;

		UnifiedGameFontController.SetDynamicTextLayer(font.gameObject, -1.45f, SpeakerSortingOrder + 2);
	}

	// 方法说明：对白框关闭时恢复被临时隐藏的战斗信息面板。
	// 参数说明：无。
	// 返回说明：无返回值。
	void OnDisable() {
		RestoreBattleInfoPanel();
	}
	
	public void SetDialogue(string t, WarSceneController.WhichSide side) {
		
		gameObject.SetActive(true);
		HideBattleInfoPanel();
		isShowingText = true;
		text = t;
		textIdx = 0;
		timeTick = Time.realtimeSinceStartup;
		
		if (font == null) {
			font = transform.Find("Font").GetComponent<exSpriteFont>();
		}
		ApplyDialogueTextLayout(side);
		SyncSpeaker(side);
		font.text = "";
		UnifiedGameFontController.SyncFontNow(font);
		AppendNextVisibleCharacter();
		
		if (side == WarSceneController.WhichSide.Left) {
			transform.localPosition = new Vector3(offset, transform.localPosition.y, transform.localPosition.z);
		} else {
			transform.localPosition = new Vector3(-offset, transform.localPosition.y, transform.localPosition.z);
		}
	}

	// 方法说明：左侧武将说话时把对白正文移到立绘右侧，右侧武将说话时恢复原始正文位置。
	// 参数说明：side 为当前对白所属的一方。
	// 返回说明：无返回值。
	void ApplyDialogueTextLayout(WarSceneController.WhichSide side) {
		if (font == null) return;

		if (!hasDialogueFontOriginalLocalPosition) {
			dialogueFontOriginalLocalPosition = font.transform.localPosition;
			hasDialogueFontOriginalLocalPosition = true;
		}
		float offsetX = side == WarSceneController.WhichSide.Left ? 95f : 0f;
		font.transform.localPosition = new Vector3(dialogueFontOriginalLocalPosition.x + offsetX,
		                                           dialogueFontOriginalLocalPosition.y,
		                                           dialogueFontOriginalLocalPosition.z);
	}

	// 方法说明：对白显示期间隐藏底部战斗信息面板，避免右侧头像和数值从对白框后面露出。
	// 参数说明：无。
	// 返回说明：无返回值。
	void HideBattleInfoPanel() {
		if (hasHiddenBattleInfoPanel) return;

		WSInfoPanel infoPanel = ResolveBattleInfoPanel();
		if (infoPanel == null) return;

		// 1. 记录原始 Renderer 状态，只隐藏信息面板自身，不碰对白框子节点。
		Renderer[] renderers = infoPanel.GetComponentsInChildren<Renderer>(true);
		for (int i = 0; i < renderers.Length; i++) {
			Renderer targetRenderer = renderers[i];
			if (targetRenderer == null || targetRenderer.transform.IsChildOf(transform)) continue;

			hiddenBattleInfoRenderers.Add(targetRenderer);
			hiddenBattleInfoRendererStates.Add(targetRenderer.enabled);
			targetRenderer.enabled = false;
		}

		// 2. 标记已经进入隐藏状态，避免一段对白重复记录同一组 Renderer。
		hasHiddenBattleInfoPanel = true;
	}

	// 方法说明：恢复对白期间隐藏的战斗信息面板 Renderer。
	// 参数说明：无。
	// 返回说明：无返回值。
	void RestoreBattleInfoPanel() {
		if (!hasHiddenBattleInfoPanel) return;

		for (int i = 0; i < hiddenBattleInfoRenderers.Count; i++) {
			Renderer targetRenderer = hiddenBattleInfoRenderers[i];
			if (targetRenderer != null) {
				targetRenderer.enabled = hiddenBattleInfoRendererStates[i];
			}
		}

		hiddenBattleInfoRenderers.Clear();
		hiddenBattleInfoRendererStates.Clear();
		hasHiddenBattleInfoPanel = false;
	}

	// 方法说明：定位战斗信息面板，优先使用 WarSceneController 绑定的面板。
	// 参数说明：无。
	// 返回说明：返回战斗信息面板，缺失时返回 null。
	WSInfoPanel ResolveBattleInfoPanel() {
		if (battleInfoPanel != null) return battleInfoPanel;

		WarSceneController warController = UnityEngine.Object.FindFirstObjectByType<WarSceneController>();
		if (warController != null && warController.infoPanel != null) {
			battleInfoPanel = warController.infoPanel;
			return battleInfoPanel;
		}

		battleInfoPanel = UnityEngine.Object.FindFirstObjectByType<WSInfoPanel>();
		return battleInfoPanel;
	}

	// 方法说明：按对白左右方向同步说话人头像和姓名，避免战斗对白显示默认黑头像或旧姓名。
	// 参数说明：side 为当前对白所属的一方。
	// 返回说明：无返回值。
	void SyncSpeaker(WarSceneController.WhichSide side) {
		int generalIndex = side == WarSceneController.WhichSide.Left
			? WarSceneController.leftGeneralIdx
			: WarSceneController.rightGeneralIdx;

		EnsureSpeakerWidgets();
		float portraitX = side == WarSceneController.WhichSide.Left
			? -SpeakerPortraitOffsetX
			: SpeakerPortraitOffsetX;
		if (speakerHead != null) {
			speakerHead.transform.localPosition = new Vector3(portraitX, SpeakerPortraitOffsetY, -1.2f);
			speakerHead.SetGeneralHead(generalIndex);
			SetSpeakerPortraitSortingOrder();
		}
		if (speakerName != null) {
			speakerName.text = ZhongWen.Instance.GetGeneralName(generalIndex);
			speakerName.transform.localPosition = new Vector3(portraitX, 49f, -1.3f);
		}
	}

	// 方法说明：查找对白框内的头像组件和姓名字体组件。
	// 参数说明：无。
	// 返回说明：无返回值。
	void EnsureSpeakerWidgets() {
		if (speakerHead == null) {
			GeneralsHeadSelect[] heads = GetComponentsInChildren<GeneralsHeadSelect>(true);
			if (heads.Length > 0) {
				speakerHead = heads[0];
			} else {
				GameObject headObject = new GameObject("WarDialogueSpeakerHead");
				headObject.hideFlags = HideFlags.DontSave;
				headObject.layer = gameObject.layer;
				headObject.transform.SetParent(transform, false);
				speakerHead = headObject.AddComponent<GeneralsHeadSelect>();
			}
		}

		if (speakerName == null) {
			Transform existingName = transform.Find("WarDialogueSpeakerName");
			if (existingName == null) {
				GameObject nameObject = new GameObject("WarDialogueSpeakerName");
				nameObject.hideFlags = HideFlags.DontSave;
				nameObject.layer = gameObject.layer;
				nameObject.transform.SetParent(transform, false);
				speakerName = nameObject.AddComponent<TextMesh>();
			} else {
				speakerName = existingName.GetComponent<TextMesh>();
			}

			if (speakerName != null) {
				if (speakerNameFont == null) {
					speakerNameFont = UnifiedGameFontController.CreateChineseDynamicFont(64);
				}
				speakerName.font = speakerNameFont;
				speakerName.fontSize = 64;
				speakerName.characterSize = 1.8f;
				speakerName.anchor = TextAnchor.MiddleCenter;
				speakerName.alignment = TextAlignment.Center;
				speakerName.color = new Color(1f, 0.88f, 0.3f, 1f);
				Renderer nameRenderer = speakerName.GetComponent<Renderer>();
				if (nameRenderer != null) {
					nameRenderer.sharedMaterial = speakerNameFont.material;
					nameRenderer.sortingOrder = SpeakerSortingOrder + 1;
				}
			}
		}
	}

	// 方法说明：把动态创建的对白头像固定到对白框前景层，避免被蓝色面板遮挡。
	// 参数说明：无。
	// 返回说明：无返回值。
	void SetSpeakerPortraitSortingOrder() {
		if (speakerHead == null) return;

		Renderer[] portraitRenderers = speakerHead.GetComponentsInChildren<Renderer>(true);
		for (int i = 0; i < portraitRenderers.Length; i++) {
			if (portraitRenderers[i] != null) {
				portraitRenderers[i].sortingOrder = SpeakerSortingOrder;
			}
		}
	}

	// 方法说明：向战斗对白框追加下一个非空格字符，并同步统一字体，避免蓝色对白框首帧没有文字。
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
	
	public bool IsShowingText() {
		return isShowingText;
	}
}
