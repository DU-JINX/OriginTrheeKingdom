using UnityEngine;
using System.Collections;

public class GeneralsHeadSelect : MonoBehaviour {
	
	private GameObject go;
	private int idxCur = -1;
	
	/// <summary>
	/// 方法说明：按武将索引显示头像，支持 MOD06 的二代 Face 编号。
	/// 参数说明：idx 为武将索引。
	/// 返回说明：无返回值。
	/// </summary>
	public void SetGeneralHead(int idx) {
		if (idx < 0 || idx >= Informations.Instance.generalNum) {
			
			if (go != null) {
				Destroy(go);
				Resources.UnloadUnusedAssets();
			}
			return;
		}
		
		if (idx == idxCur)	return;
		
		if (go != null) {
			Destroy(go);
			Resources.UnloadUnusedAssets();
		}
		
		int faceIndex = Informations.Instance.GetGeneralFaceIndex(idx);
		if (faceIndex <= 0) {
			faceIndex = idx + 1;
		}

		string headName = GetHeadResourceName(faceIndex);
		UnityEngine.Object headPrefab = Resources.Load(headName);
		if (headPrefab == null) {
			Debug.LogError("武将头像资源尚未恢复: " + headName + " 武将索引: " + idx);
			idxCur = idx;
			return;
		}
		
		go = (GameObject)Instantiate(headPrefab, transform.position, transform.rotation);
		go.transform.parent = transform;
		idxCur = idx;
	}

	/// <summary>
	/// 方法说明：按当前剧本选择头像资源目录，避免二代恢复头像覆盖一代率土头像。
	/// 参数说明：faceIndex 为武将头像编号。
	/// 返回说明：返回 Resources.Load 可读取的头像路径。
	/// </summary>
	private string GetHeadResourceName(int faceIndex) {
		string headDirectory = MODLoadController.IsRestoredSango2Index(Controller.MODSelect) ? "Head" : "StzbHead";
		return headDirectory + "/Head" + faceIndex.ToString("D3");
	}
}
