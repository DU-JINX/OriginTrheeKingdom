using UnityEngine;
using System.Collections;

public class FadeOut : MonoBehaviour {
	
	private exSprite sprite;
	private float timeTick;
	private string levelName;
	
	/// <summary>
	/// 方法说明：初始化淡出遮罩状态。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	void Start () {
		
		enabled = false;
		timeTick = 0;
		EnsureSprite();
		if (sprite == null) {
			return;
		}

		sprite.color = new Color(0, 0, 0, 0);
		sprite.gameObject.SetActive(false);
	}
	
	/// <summary>
	/// 方法说明：推进淡出动画并在完成后切换场景。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	void Update () {
		if (sprite == null) {
			EnsureSprite();
			if (sprite == null) {
				enabled = false;
				return;
			}
		}
		
		if (timeTick < 0.5f) {
			timeTick += Time.deltaTime;
			timeTick = Mathf.Clamp(timeTick, 0f, 0.5f);
			
			sprite.color = new Color(0, 0, 0, timeTick * 2);
		} else {
			UnityEngine.SceneManagement.SceneManager.LoadScene(levelName);
		}
	}
	
	/// <summary>
	/// 方法说明：设置淡出完成后要进入的场景，并显示遮罩。
	/// 参数说明：n 为目标场景名称。
	/// 返回说明：无返回值。
	/// </summary>
	public void SetLevelName(string n) {
		EnsureSprite();
		if (sprite == null) {
			Debug.LogError("FadeOut 子节点缺少 exSprite，无法切换场景: " + n);
			return;
		}

		enabled = true;
		levelName = n;
		
		sprite.gameObject.SetActive(true);
	}

	/// <summary>
	/// 方法说明：确保淡出遮罩 sprite 已绑定。
	/// 参数说明：无参数。
	/// 返回说明：无返回值。
	/// </summary>
	private void EnsureSprite() {
		if (sprite != null) {
			return;
		}

		if (transform.childCount == 0) {
			Debug.LogError("FadeOut 缺少遮罩子节点。");
			return;
		}

		sprite = transform.GetChild(0).GetComponent<exSprite>();
	}
}
