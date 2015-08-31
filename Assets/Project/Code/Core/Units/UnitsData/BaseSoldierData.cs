using UnityEngine;

[System.Serializable]
public class BaseSoldierData : BaseUnitData {
	[SerializeField]
	protected int _leadershipCost = 0;	//how much leadership hero must have to hire the soldier
	public int LeadershipCost {
		get { return _leadershipCost; }
	}

	//TODO: maybe credits cost will be recalculated on unit upgrade
	[SerializeField]
	protected int _creditsCost = 0;	//price in credits
	public int CreditsCost {
		get { return _creditsCost; }
	}
}
