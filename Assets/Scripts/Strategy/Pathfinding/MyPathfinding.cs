using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class MyPathfinding : MonoBehaviour {
	
	public int maxDistance = 30;
	public Transform[] cityPoints;
	
	private static bool isInit = false;
	private static List<int> cityPointsIdx = new List<int>();
	
	private static List<List<int>> connectList = new List<List<int>>();
	//private List<List<int>> nearbyCities = new List<List<int>>();
	
	private static List<List<int>> nearbyCities = new List<List<int>> {
		new List<int>{1},
		new List<int>{0, 2, 4},
		new List<int>{1, 4, 3},
		new List<int>{2, 10, 6},
		new List<int>{5, 7, 2, 1},
		new List<int>{4, 7, 6, 8},
		new List<int>{9, 18, 3, 10, 5, 8},
		new List<int>{5, 4, 16, 17},
		new List<int>{6, 5, 9, 17, 19},
		new List<int>{8, 17, 19, 6, 18},
		new List<int>{3, 6, 18, 21, 11},
		new List<int>{22, 10, 12},
		new List<int>{11, 13, 37},
		new List<int>{12, 14},
		new List<int>{13, 15, 38},
		new List<int>{14},
		new List<int>{7, 17, 32},
		new List<int>{8, 9, 19, 7, 16, 31},
		new List<int>{6, 9, 19, 10},
		new List<int>{20, 8, 9, 17, 18},
		new List<int>{31, 19, 21},
		new List<int>{10, 20, 22},
		new List<int>{23, 24, 21, 11},
		new List<int>{25, 26, 22, 24},
		new List<int>{23, 22, 37, 41},
		new List<int>{35, 23, 26, 28},
		new List<int>{41, 27, 25, 23},
		new List<int>{41, 26, 28, 30},
		new List<int>{27, 30, 25, 29, 36},
		new List<int>{30, 28},
		new List<int>{27, 28, 29},
		new List<int>{32, 35, 17, 20},
		new List<int>{16, 33, 31, 35},
		new List<int>{32, 34},
		new List<int>{33, 47, 46},
		new List<int>{32, 31, 25, 46},
		new List<int>{28, 45, 46},
		new List<int>{38, 12, 24},
		new List<int>{14, 37, 39},
		new List<int>{38, 40},
		new List<int>{39, 43, 42},
		new List<int>{42, 24, 27, 26},
		new List<int>{40, 41},
		new List<int>{40, 44},
		new List<int>{43},
		new List<int>{36},
		new List<int>{35, 34, 36},
		new List<int>{34}
	};
	
	// Use this for initialization
	void Awake () {
		
		if (!isInit) {
			isInit = true;
			InitConnectList();
			InitNearbyCities();
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	void InitConnectList() {
		
		int count = transform.childCount;
		
		for (int i=0; i<count; i++) {
			
			List<int> nodeConnectList = new List<int>();
			
			for (int j=0; j<count ;j++) {
				
				if (i == j) continue;
				
				bool breakFlag = false;
				Transform child_i = transform.GetChild(i);
				Transform child_j = transform.GetChild(j);
				
				LinkBreak linkBreak_i = child_i.GetComponent<LinkBreak>();
				if (linkBreak_i != null) {
					
					for (int k=0; k<linkBreak_i.node.Length; k++) {
						
						if (linkBreak_i.node[k] == child_j) {
							
							breakFlag = true;
							break;
						}
					}
					
					if (breakFlag) continue;
				} 
				
					
				LinkBreak linkBreak_j = child_j.GetComponent<LinkBreak>();
				if (linkBreak_j != null) {
					
					for (int k=0; k<linkBreak_j.node.Length; k++) {
						
						if (linkBreak_j.node[k] == child_i) {
							
							breakFlag = true;
							break;
						}
					}
					
					if (breakFlag) continue;
				} 
				
				float distance = Vector3.Distance(child_i.position, child_j.position);
				if (distance <= maxDistance) {
					
					nodeConnectList.Add(j);
				}
			}
			
			connectList.Add(nodeConnectList);
		}
	}
	
	private void InitNearbyCities() {
		
		int count = cityPoints.Length;
		
		for (int i=0; i<count; i++) {
			
			cityPointsIdx.Add(GetPointIdx(i));
		}
		
		/*
		for (int i=0; i<count; i++) {
			
			List<int> nbc = new List<int>();
			
			int[] flag = new int[transform.childCount];
			List<int> connect = connectList[cityPointsIdx[i]];
			
			flag[cityPointsIdx[i]] = 1;
			for (int n=0; n<connect.Count; n++) {
				FindNearbyCitiesX(connect[n], nbc, flag);
			}
			
			nearbyCities.Add(nbc);
		}
		
		Debug.Log(Application.dataPath);
		FileStream fs = new FileStream(Application.dataPath+"/1.txt", FileMode.Create, FileAccess.Write);
		StreamWriter sw = new StreamWriter(fs);
		sw.Flush();
		
		for (int i=0; i<count; i++) {
			string str = "new List<int>{";
			for (int j=0; j<nearbyCities[i].Count; j++) {
				str += nearbyCities[i][j];
				if (j != nearbyCities[i].Count-1)
					str += ", ";
			}
			str += "},";
			Debug.Log(str);
			sw.WriteLine(str);
		}
		
		sw.Flush();
		sw.Close();
		*/
	}
	
	private void FindNearbyCitiesX(int idx, List<int> nbc, int[] flag) {
		
		if (flag[idx] == 1) return;
		flag[idx] = 1;
		
		int cityIdx = -1;
		for (int i=0; i<cityPointsIdx.Count; i++) {
			if (cityPointsIdx[i] == idx) {
				
				cityIdx = i;
				break;
			}
		}
		
		if (cityIdx != -1) {
			
			nbc.Add(cityIdx);
			return;
		}
		
		List<int> connect = connectList[idx];
		for (int n=0; n<connect.Count; n++) {
			
			FindNearbyCitiesX(connect[n], nbc, flag);
		}
	}
	
	/// <summary>
	/// 方法说明：读取城池相邻目标，旧 48 城使用原始拓扑，新增城池按恢复坐标选择近邻。
	/// 参数说明：cIdx 为城池索引。
	/// 返回说明：返回可作为目标的城池索引列表。
	/// </summary>
	public static List<int> GetCityNearbyIdx(int cIdx) {
		
		if (cIdx < 0 || cIdx >= Informations.Instance.cityNum) return new List<int>();
		
		if (cIdx >= nearbyCities.Count) {
			return GetRecoveredNearbyCities(cIdx);
		}

		return nearbyCities[cIdx];
	}

	/// <summary>
	/// 方法说明：按恢复坐标为新增城池生成最近邻。
	/// 参数说明：cIdx 为城池索引。
	/// 返回说明：返回最多 4 个最近城池。
	/// </summary>
	private static List<int> GetRecoveredNearbyCities(int cIdx) {
		List<int> result = new List<int>();
		if (!Informations.Instance.HasCityPosition(cIdx)) return result;

		Vector3 basePos = Informations.Instance.GetCityWorldPosition(cIdx);
		List<int> candidates = new List<int>();
		List<float> distances = new List<float>();
		for (int i = 0; i < Informations.Instance.cityNum; i++) {
			if (i == cIdx || !Informations.Instance.HasCityPosition(i)) continue;

			float distance = Vector3.Distance(basePos, Informations.Instance.GetCityWorldPosition(i));
			int insertIndex = 0;
			while (insertIndex < distances.Count && distances[insertIndex] < distance) {
				insertIndex++;
			}

			distances.Insert(insertIndex, distance);
			candidates.Insert(insertIndex, i);
		}

		int count = Mathf.Min(4, candidates.Count);
		for (int i = 0; i < count; i++) {
			result.Add(candidates[i]);
		}

		return result;
	}
	
	private int GetPointIdx(Vector3 pos) {
		
		float minDistance = float.MaxValue;
		int closest = -1;
		int count = transform.childCount;
		
		for (int i=0; i<count; i++) {
			
			Transform child = transform.GetChild(i);
			
			float distance = Vector3.Distance(pos, child.position);
			
			if (distance < 1) return i;
			if (distance < minDistance) {
				
				minDistance = distance;
				closest = i;
			}
		}
		
		return closest;
	}
	
	private int GetPointIdx(Transform node) {
		
		int count = transform.childCount;
		
		for (int i=0; i<count; i++) {
			
			if (transform.GetChild(i) == node) {
				
				return i;
			}
		}
		
		return -1;
	}
	
	/// <summary>
	/// 方法说明：读取城池对应路径点索引。
	/// 参数说明：city 为城池索引。
	/// 返回说明：旧城池返回场景路径点，新增城池按恢复坐标寻找最近路径点。
	/// </summary>
	private int GetPointIdx(int city) {
		if (city < 0) return -1;

		if (city >= cityPoints.Length || cityPoints[city] == null) {
			if (Informations.Instance.HasCityPosition(city)) {
				return GetPointIdx(Informations.Instance.GetCityWorldPosition(city));
			}

			return -1;
		}
		
		return GetPointIdx(cityPoints[city]);
	}
	
	/// <summary>
	/// 方法说明：根据世界坐标反查城池索引。
	/// 参数说明：pos 为世界坐标，distance 为命中距离。
	/// 返回说明：命中返回城池索引，否则返回 -1。
	/// </summary>
	public int GetCityIndex(Vector3 pos, int distance) {
		
		int ret = -1;
		
		for (int i=0; i<Informations.Instance.cityNum; i++) {
			
			if (Vector3.Distance(pos, GetCityPos(i)) < distance) {
				ret = i;
				break;
			}
		}
		
		return ret;
	}
	
	/// <summary>
	/// 方法说明：读取城池世界坐标。
	/// 参数说明：city 为城池索引。
	/// 返回说明：旧城池返回场景路径点坐标，新增城池返回恢复坐标。
	/// </summary>
	public Vector3 GetCityPos(int city) {
		
		if (city < 0 || city >= Informations.Instance.cityNum) return Vector3.zero;

		if (city < cityPoints.Length && cityPoints[city] != null) {
			return cityPoints[city].position;
		}

		if (Informations.Instance.HasCityPosition(city)) {
			return Informations.Instance.GetCityWorldPosition(city);
		}
		
		return Vector3.zero;
	}
	
	/// <summary>
	/// 方法说明：获取任意坐标到任意坐标的路径。
	/// 参数说明：startPos 为起点坐标，endPos 为终点坐标。
	/// 返回说明：返回路径点列表。
	/// </summary>
	public List<Vector3> GetRoute(Vector3 startPos, Vector3 endPos) {
		
		return GetRoutePoints(GetPointIdx(startPos), GetPointIdx(endPos));
	}
	
	/// <summary>
	/// 方法说明：获取任意坐标到目标城池的路径。
	/// 参数说明：startPos 为起点坐标，endCity 为目标城池索引。
	/// 返回说明：返回路径点列表。
	/// </summary>
	public List<Vector3> GetRoute(Vector3 startPos, int endCity) {
		int start = GetPointIdx(startPos);
		int end = GetPointIdx(endCity);
		if (start < 0 || end < 0 || endCity >= cityPoints.Length) {
			return GetDirectRoute(GetCityPos(endCity));
		}

		return GetRoutePoints(start, end);
	}
	
	/// <summary>
	/// 方法说明：获取城池到城池的路径。
	/// 参数说明：startCity 为起点城池，endCity 为目标城池。
	/// 返回说明：返回路径点列表，新增城池无法走旧拓扑时返回直线路径。
	/// </summary>
	public List<Vector3> GetRoute(int startCity, int endCity) {
		if (startCity >= cityPoints.Length || endCity >= cityPoints.Length) {
			return GetDirectRoute(GetCityPos(endCity));
		}

		return GetRoutePoints(GetPointIdx(startCity), GetPointIdx(endCity));
	}
	
	/// <summary>
	/// 方法说明：获取 Transform 到 Transform 的路径。
	/// 参数说明：start 为起点 Transform，end 为终点 Transform。
	/// 返回说明：返回路径点列表。
	/// </summary>
	public List<Vector3> GetRoute(Transform start, Transform end) {
		
		return GetRoutePoints(GetPointIdx(start), GetPointIdx(end));
	}
	
	/// <summary>
	/// 方法说明：从路径点索引生成路径。
	/// 参数说明：start 为起点路径点索引，end 为终点路径点索引。
	/// 返回说明：返回路径点列表，无法连通时返回终点直达。
	/// </summary>
	private List<Vector3> GetRoutePoints(int start, int end) {
		
		List<Vector3> list = new List<Vector3>();
		
		if (!isInit) {
			
			InitConnectList();
		}
		
		if (start < 0 || end < 0 || start >= transform.childCount || end >= transform.childCount) {
			return list;
		}

		if (start == end) {
			
			list.Add(transform.GetChild(end).position);
			return list;
		}
		
		bool isFinished = false;
		int[] flag = new int[transform.childCount];
		List<int> nodes = new List<int>();
		int index = 1;
		
		flag[end] = index;
		nodes.AddRange(connectList[end]);
		
		while (!isFinished && nodes.Count > 0) {
			
			index++;
			
			int count = nodes.Count;
			for (int i=count-1; i>=0; i--) {
				
				int checking = nodes[i];
				
				if (checking == start) {
					
					isFinished = true;
					nodes.Clear();
					break;
				}
				
				if (flag[checking] == 0) {
					flag[checking] = index;
					nodes.AddRange(connectList[checking]);
				}
				
				nodes.RemoveAt(i);
			}
		}

		if (!isFinished) {
			list.Add(transform.GetChild(end).position);
			return list;
		}
		
		nodes = connectList[start];
		
		while (index > 1) {
			
			index--;
			
			for (int i=0; i<nodes.Count; i++) {
				
				int checking = nodes[i];
				if (flag[checking] == index) {
					
					nodes = connectList[checking];
					list.Add(transform.GetChild(checking).position);
					
					break;
				}
			}
		}
		
		return list;
	}

	/// <summary>
	/// 方法说明：生成直达路径。
	/// 参数说明：endPos 为终点坐标。
	/// 返回说明：返回只包含终点的路径列表。
	/// </summary>
	private List<Vector3> GetDirectRoute(Vector3 endPos) {
		List<Vector3> list = new List<Vector3>();
		list.Add(endPos);
		return list;
	}
	
	
	
	
	void OnDrawGizmos () {
		
		if (!isInit) return;
		
		Gizmos.color = new Color (1,0,0,1F);
			
		for (int i=0; i<connectList.Count; i++) {
			
			List<int> connect = connectList[i];
			for (int j=0; j<connect.Count; j++) {
				
				Gizmos.DrawLine (transform.GetChild(i).position, transform.GetChild(connect[j]).position);
			}
		}
	}
	
}
