public struct HCCGridPoint {
	public int X;
	public int Z;
	//public int X { get; set; }
	//public int Z { get; set; }

	public HCCGridPoint(int x, int z) {
		X = x;
		Z = z;
	}

	#region static
	public static HCCGridPoint Zero {
		get { return new HCCGridPoint(0, 0); }
	}
	#endregion

	#region operators
	public static HCCGridPoint operator +(HCCGridPoint o1, HCCGridPoint o2) {
		return new HCCGridPoint(o1.X + o2.X, o1.Z + o2.Z);
	}

	public static HCCGridPoint operator -(HCCGridPoint o1, HCCGridPoint o2) {
		return new HCCGridPoint(o1.X - o2.X, o1.Z - o2.Z);
	}

	public static HCCGridPoint operator *(HCCGridPoint o1, HCCGridPoint o2) {
		return new HCCGridPoint(o1.X * o2.X, o1.Z * o2.Z);
	}

	public static HCCGridPoint operator /(HCCGridPoint o1, HCCGridPoint o2) {
		return new HCCGridPoint(o1.X / o2.X, o1.Z / o2.Z);
	}
	#endregion
}
