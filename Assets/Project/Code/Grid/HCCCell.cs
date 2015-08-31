using System.Collections.Generic;
using UnityEngine;

public class HCCCell {
	public bool inOpenSet = false;

	public HCCGridPoint GridPosition { get; private set; }

	public List<HCCGridObject> ObjectData { get; private set; }

	public HCCGridObject ObjectPathReservation { get; set; }

	public bool IsFree {
		get { return ObjectData.Count == 0; }
	}

	public bool IsBusyByStaticObject {
		get { return ObjectData.Count > 0 && ObjectData.FindIndex((HCCGridObject x) => { return x.IsStatic == true; }) != -1; }
	}

	public HCCCell(int x, int z) {
		GridPosition = new HCCGridPoint(x, z);
		ObjectData = new List<HCCGridObject>();
	}
}
