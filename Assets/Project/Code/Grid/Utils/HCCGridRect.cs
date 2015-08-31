public struct HCCGridRect {
	public int XMin;
	public int XMax;
	public int ZMin;
	public int ZMax;
	//public int XMin { get; set; }
	//public int XMax { get; set; }
	//public int ZMin { get; set; }
	//public int ZMax { get; set; }

	public HCCGridPoint Min {
		get { return new HCCGridPoint(XMin, ZMin); }
	}

	public HCCGridPoint Max {
		get { return new HCCGridPoint(XMax, ZMax); }
	}

	public int XSize {
		get { return XMax - XMin; }
	}
	public int ZSize {
		get { return ZMax - ZMin; }
	}

	public HCCGridRect(int xMin, int xMax, int zMin, int zMax) {
		XMin = xMin;
		XMax = xMax;
		ZMin = zMin;
		ZMax = zMax;
	}

	public bool Contains(HCCGridPoint point) {
		return Contains(point.X, point.Z);
	}

	public bool Contains(int x, int z) {
		return x >= XMin && x <= XMax && z >= ZMin && z <= ZMax;
	}

	public bool Interacts(HCCGridRect rect) {
		return this.Contains(rect.XMin, rect.ZMin) || this.Contains(rect.XMin, rect.ZMax) || this.Contains(rect.XMax, rect.ZMax) || this.Contains(rect.XMax, rect.ZMin);
	}

	public bool Equals(HCCGridRect gridRect) {
		return XMin == gridRect.XMin && ZMin == gridRect.ZMin && XMax == gridRect.XMax && ZMax == gridRect.ZMax;
	}

	#region static
	public HCCGridRect Zero {
		get { return new HCCGridRect(0, 0, 0, 0); }
	}
	#endregion
}
