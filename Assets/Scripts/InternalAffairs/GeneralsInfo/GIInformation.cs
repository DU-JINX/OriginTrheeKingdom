using UnityEngine;
using System.Collections;

public class GIInformation : MonoBehaviour {

	private const float LeftValueReadableOffsetX = 35f;
	private bool hasAppliedReadableValueLayout;
	
	public GeneralsHeadSelect headSelect;
	
	public exSpriteFont generalName;
	public exSpriteFont kingName;
	public exSpriteFont job;
	public exSpriteFont level;
	public exSpriteFont force;
	public exSpriteFont health;
	public exSpriteFont intellect;
	public exSpriteFont mana;
	public exSpriteFont soldier;
	public exSpriteFont experience;
	public exSpriteFont equipment;
	
	/// <summary>
	/// 方法说明：刷新武将资料页的头像、归属、职务、属性与装备信息。
	/// 参数说明：idx 为武将索引。
	/// 返回说明：无返回值。
	/// </summary>
	public void SetGeneral(int idx) {
		ApplyReadableValueLayout();
		
		headSelect.SetGeneralHead(idx);
		
		string str = ZhongWen.Instance.GetGeneralName1(idx);
		
		generalName.text = str;
		
		GeneralInfo gInfo = Informations.Instance.GetGeneralInfo(idx);
		
		if (gInfo.prisonerIdx != -1 || gInfo.king == -1 || gInfo.king == Informations.Instance.kingNum) {
			kingName.text = "---";
		} else {
			kingName.text = ZhongWen.Instance.GetKingName(gInfo.king);
		}
		
		if (gInfo.prisonerIdx != -1 || gInfo.king == -1 || gInfo.king == Informations.Instance.kingNum 
			|| Informations.Instance.GetKingInfo(gInfo.king).generalIdx == idx) {
			job.text = "---";
		} else {
			job.text = ZhongWen.Instance.GetJobsName(gInfo.job);
		}
		
		level.text = "" + gInfo.level;
		force.text = "" + gInfo.strength;
		intellect.text = "" + gInfo.intellect;
		health.text = gInfo.healthCur + "/" + gInfo.healthMax;
		mana.text = gInfo.manaCur + "/" + gInfo.manaMax;
		soldier.text = (gInfo.soldierCur + gInfo.knightCur) + "/" + (gInfo.soldierMax + gInfo.knightMax);
		experience.text = gInfo.experience + "/" + Misc.GetLevelExperience(gInfo.level+1);
		equipment.text = ZhongWen.Instance.GetEquipmentName(gInfo.equipment);
	}

	/// <summary>
	/// 方法说明：把资料页左列数值移到标题右侧，避免比例字体与“等级、体力、技力、经验值”等标题重叠。
	/// 参数说明：无。
	/// 返回说明：无。
	/// </summary>
	private void ApplyReadableValueLayout() {
		if (hasAppliedReadableValueLayout) return;

		ShiftFontX(kingName, LeftValueReadableOffsetX);
		ShiftFontX(level, LeftValueReadableOffsetX);
		ShiftFontX(health, LeftValueReadableOffsetX);
		ShiftFontX(mana, LeftValueReadableOffsetX);
		ShiftFontX(experience, LeftValueReadableOffsetX);
		ShiftFontX(experience, 12f);
		ShiftFontY(experience, 8f);
		hasAppliedReadableValueLayout = true;
	}

	/// <summary>
	/// 方法说明：水平移动单个资料数值字体。
	/// 参数说明：font 为目标旧字体组件，offsetX 为本地横向偏移。
	/// 返回说明：无。
	/// </summary>
	private void ShiftFontX(exSpriteFont font, float offsetX) {
		if (font == null) return;

		Vector3 localPosition = font.transform.localPosition;
		font.transform.localPosition = new Vector3(localPosition.x + offsetX, localPosition.y, localPosition.z);
	}

	/// <summary>
	/// 方法说明：垂直移动单个资料数值字体。
	/// 参数说明：font 为目标旧字体组件，offsetY 为本地纵向偏移。
	/// 返回说明：无。
	/// </summary>
	private void ShiftFontY(exSpriteFont font, float offsetY) {
		if (font == null) return;

		Vector3 localPosition = font.transform.localPosition;
		font.transform.localPosition = new Vector3(localPosition.x, localPosition.y + offsetY, localPosition.z);
	}
}
