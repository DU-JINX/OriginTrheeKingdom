using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Text;

public class MODLoadController {

    private const int RestoredSango2Index = 5;
    private const float RecoveredMapCenterX = 1500f;
    private const float RecoveredMapCenterY = 950f;
    private const float RecoveredMapScale = 0.18f;

    private static MODLoadController mInstance;
    public static MODLoadController Instance
    {
        get
        {
            if (mInstance == null)
            {
                mInstance = new MODLoadController();
            }
            return mInstance;
        }
    }

    /// <summary>
    /// 方法说明：读取当前支持的 MOD 数量。
    /// 参数说明：无参数。
    /// 返回说明：返回可选择 MOD 数量。
    /// </summary>
    public int GetMODCount()
    {
        return RestoredSango2Index + 1;
    }

    /// <summary>
    /// 方法说明：读取 MOD 选择界面显示名称。
    /// 参数说明：index 为 MOD 索引。
    /// 返回说明：返回显示名称。
    /// </summary>
    public string GetMODDisplayName(int index)
    {
        if (index == RestoredSango2Index)
        {
            return "威力加强版";
        }

        return "MOD0" + (index + 1);
    }

    /// <summary>
    /// 方法说明：加载指定 MOD XML，并初始化势力、城池、武将和运行时元数据。
    /// 参数说明：index 为 MOD 索引，MOD06 对应二代 APK 恢复版。
    /// 返回说明：加载成功返回 true，资源缺失或结构错误返回 false。
    /// </summary>
	public bool LoadMOD(int index)
    {
        Controller.MODSelect = index;

        string modName = "MOD" + (index + 1).ToString("D2");
        TextAsset textAsset = (TextAsset)Resources.Load(modName);
        if (textAsset == null)
        {
            Debug.LogError("MOD资源不存在: " + modName);
            return false;
        }

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(textAsset.text.ToString().Trim());
        XmlElement root = xmlDoc.DocumentElement;
        if (root == null)
        {
            Debug.LogError("MOD XML根节点为空: " + modName);
            return false;
        }

        XmlElement kingRoot = GetRequiredElement(root, "King", modName);
        XmlElement cityRoot = GetRequiredElement(root, "City", modName);
        XmlElement generalRoot = GetRequiredElement(root, "General", modName);
        if (kingRoot == null || cityRoot == null || generalRoot == null)
        {
            return false;
        }

        XmlNodeList kingNodes = kingRoot.ChildNodes;
        XmlNodeList cityNodes = cityRoot.ChildNodes;
        XmlNodeList generalNodes = generalRoot.ChildNodes;
        Informations.Instance.ConfigureRuntimeCounts(kingNodes.Count, cityNodes.Count, generalNodes.Count);

        int historyTime = GetOptionalInt(root, "HistoryTime", Controller.historyTime);
        Controller.historyTime = historyTime;

        LoadKings(kingNodes);
        LoadCities(cityNodes);
        LoadGenerals(generalNodes);

        KingInfo k = new KingInfo();
        k.generalIdx = 0;
        Informations.Instance.SetKingInfo(Informations.Instance.kingNum, k);

        Informations.Instance.InitKingInfo();
        Informations.Instance.InitCityPrefect();
        Informations.Instance.InitCityInfo();
        Informations.Instance.InitGeneralInfo();

        return true;
    }

    /// <summary>
    /// 方法说明：加载势力节点。
    /// 参数说明：nodeList 为 King 节点下的 Item 列表。
    /// 返回说明：无返回值。
    /// </summary>
    private void LoadKings(XmlNodeList nodeList)
    {
        int i = 0;
        foreach (XmlElement kingNode in nodeList)
        {
            int idx = GetOptionalInt(kingNode, "Index", i);
            KingInfo kInfo = new KingInfo();
            kInfo.active = GetRequiredInt(kingNode, "Active");
            kInfo.generalIdx = GetRequiredInt(kingNode, "GeneralIdx");

            Informations.Instance.SetKingInfo(idx, kInfo);
            Informations.Instance.SetKingMeta(idx, GetOptionalString(kingNode, "Name", ""));
            i++;
        }
    }

    /// <summary>
    /// 方法说明：加载城池节点和 APK 恢复坐标。
    /// 参数说明：nodeList 为 City 节点下的 Item 列表。
    /// 返回说明：无返回值。
    /// </summary>
    private void LoadCities(XmlNodeList nodeList)
    {
        int i = 0;
        foreach (XmlElement cityNode in nodeList)
        {
            int idx = GetOptionalInt(cityNode, "Index", i);
            CityInfo cInfo = new CityInfo();
            cInfo.king = GetRequiredInt(cityNode, "King");
            cInfo.population = GetRequiredInt(cityNode, "Population");
            cInfo.money = GetRequiredInt(cityNode, "Money");
            cInfo.reservist = GetRequiredInt(cityNode, "Reservist");
            cInfo.reservistMax = GetRequiredInt(cityNode, "ReservistMax");
            cInfo.defense = GetRequiredInt(cityNode, "Defense");

            Informations.Instance.SetCityInfo(idx, cInfo);

            int x = GetOptionalInt(cityNode, "X", 0);
            int y = GetOptionalInt(cityNode, "Y", 0);
            int flagX = GetOptionalInt(cityNode, "FlagX", x);
            int flagY = GetOptionalInt(cityNode, "FlagY", y);
            if (cityNode.HasAttribute("X") && cityNode.HasAttribute("Y"))
            {
                Informations.Instance.SetCityMeta(
                    idx,
                    GetOptionalString(cityNode, "Name", ""),
                    ConvertRecoveredMapPosition(x, y),
                    ConvertRecoveredMapPosition(flagX, flagY));
            }
            else
            {
                Informations.Instance.SetCityMeta(idx, GetOptionalString(cityNode, "Name", ""), Vector3.zero, Vector3.zero);
            }

            i++;
        }
    }

    /// <summary>
    /// 方法说明：加载武将节点和二代 Face 编号。
    /// 参数说明：nodeList 为 General 节点下的 Item 列表。
    /// 返回说明：无返回值。
    /// </summary>
    private void LoadGenerals(XmlNodeList nodeList)
    {
        int i = 0;
        foreach (XmlElement generalNode in nodeList)
        {
            int idx = GetOptionalInt(generalNode, "Index", i);
            GeneralInfo gInfo = new GeneralInfo();
            gInfo.king = GetRequiredInt(generalNode, "King");
            gInfo.city = GetRequiredInt(generalNode, "City");
            gInfo.magic[0] = GetRequiredInt(generalNode, "Magic0");
            gInfo.magic[1] = GetRequiredInt(generalNode, "Magic1");
            gInfo.magic[2] = GetRequiredInt(generalNode, "Magic2");
            gInfo.magic[3] = GetRequiredInt(generalNode, "Magic3");
            gInfo.equipment = GetRequiredInt(generalNode, "Equipment");
            gInfo.strength = GetRequiredInt(generalNode, "Strength");
            gInfo.intellect = GetRequiredInt(generalNode, "Intellect");
            gInfo.experience = GetRequiredInt(generalNode, "Experience");
            gInfo.level = GetRequiredInt(generalNode, "Level");
            gInfo.healthMax = GetRequiredInt(generalNode, "HealthMax");
            gInfo.healthCur = GetRequiredInt(generalNode, "HealthCur");
            gInfo.manaMax = GetRequiredInt(generalNode, "ManaMax");
            gInfo.manaCur = GetRequiredInt(generalNode, "ManaCur");
            gInfo.soldierMax = GetRequiredInt(generalNode, "SoldierMax");
            gInfo.soldierCur = GetRequiredInt(generalNode, "SoldierCur");
            gInfo.knightMax = GetRequiredInt(generalNode, "KnightMax");
            gInfo.knightCur = GetRequiredInt(generalNode, "KnightCur");
            gInfo.arms = GetRequiredInt(generalNode, "Arms");
            gInfo.armsCur = GetOptionalInt(generalNode, "ArmsCur", 0);
            gInfo.formation = GetRequiredInt(generalNode, "Formation");
            gInfo.formationCur = GetOptionalInt(generalNode, "FormationCur", 0);

            Informations.Instance.SetGeneralInfo(idx, gInfo);
            Informations.Instance.SetGeneralMeta(
                idx,
                GetOptionalString(generalNode, "Name", ""),
                GetOptionalInt(generalNode, "Face", idx + 1));

            i++;
        }
    }

    /// <summary>
    /// 方法说明：把 APK 地图像素坐标转换成当前 Unity 场景世界坐标。
    /// 参数说明：x 和 y 为 APK 中恢复出的地图坐标。
    /// 返回说明：返回可用于旗帜和城池标记的世界坐标。
    /// </summary>
    private Vector3 ConvertRecoveredMapPosition(int x, int y)
    {
        float worldX = (x - RecoveredMapCenterX) * RecoveredMapScale;
        float worldY = (RecoveredMapCenterY - y) * RecoveredMapScale;
        return new Vector3(worldX, worldY, 0);
    }

    /// <summary>
    /// 方法说明：读取必需子节点。
    /// 参数说明：root 为根节点，name 为子节点名称，modName 为日志使用的 MOD 名称。
    /// 返回说明：找到返回 XmlElement，否则返回 null。
    /// </summary>
    private XmlElement GetRequiredElement(XmlElement root, string name, string modName)
    {
        XmlElement node = (XmlElement)root.SelectSingleNode(name);
        if (node == null)
        {
            Debug.LogError("MOD XML缺少节点: " + modName + "/" + name);
        }

        return node;
    }

    /// <summary>
    /// 方法说明：读取必需整数字段。
    /// 参数说明：node 为 XML 节点，attribute 为属性名。
    /// 返回说明：返回解析后的整数，字段缺失时抛出错误。
    /// </summary>
    private int GetRequiredInt(XmlElement node, string attribute)
    {
        if (!node.HasAttribute(attribute))
        {
            throw new XmlException("MOD XML缺少必需属性: " + node.Name + "/" + attribute);
        }

        return int.Parse(node.GetAttribute(attribute));
    }

    /// <summary>
    /// 方法说明：读取可选整数字段。
    /// 参数说明：node 为 XML 节点，attribute 为属性名，defaultValue 为缺省值。
    /// 返回说明：字段存在时返回解析值，否则返回缺省值。
    /// </summary>
    private int GetOptionalInt(XmlElement node, string attribute, int defaultValue)
    {
        if (!node.HasAttribute(attribute) || node.GetAttribute(attribute) == "")
        {
            return defaultValue;
        }

        return int.Parse(node.GetAttribute(attribute));
    }

    /// <summary>
    /// 方法说明：读取可选字符串字段。
    /// 参数说明：node 为 XML 节点，attribute 为属性名，defaultValue 为缺省值。
    /// 返回说明：字段存在时返回字符串，否则返回缺省值。
    /// </summary>
    private string GetOptionalString(XmlElement node, string attribute, string defaultValue)
    {
        if (!node.HasAttribute(attribute))
        {
            return defaultValue;
        }

        return node.GetAttribute(attribute);
    }
}
