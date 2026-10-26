using System.Collections.Generic;
using UnityEngine;

public static class Dico
{
	public static string[] Slum_A = new string[2] { "Residential/SlumA_1", "Residential/SlumA_2" };

	public static string[] Slum_B = new string[2] { "Residential/SlumB_1", "Residential/SlumB_2" };

	public static string[] R1_lot_4x4 = new string[1] { "Lots/R1A_4x4" };

	public static string[] R1_lot_3x4 = new string[1] { "Lots/R1A_3x4" };

	public static string[] R2_lot_4x4 = new string[1] { "Lots/R2A_4x4" };

	public static string[] R2_lot_3x4 = new string[1] { "Lots/R2A_3x4" };

	public static string[] R4_lot_4x4 = new string[1] { "Lots/R4A_4x4" };

	public static string[] R4_lot_3x4 = new string[1] { "Lots/R4A_3x4" };

	public static string[] R1_house = new string[6] { "Residential/R1_Aa", "Residential/R1_Ba", "Residential/R1_C", "Residential/R1_D", "Residential/R1_E", "Residential/R1_F" };

	public static string[] R2_house = new string[4] { "Residential/R2_Aa", "Residential/R2_Ba", "Residential/R2_C", "Residential/R2_D" };

	public static string[] R3_house = new string[4] { "Residential/R3_Aa", "Residential/R3_Ba", "Residential/R3_C", "Residential/R3_D" };

	public static string[] R3S_house = new string[2] { "Residential/R3_BS", "Residential/R3_DS" };

	public static string[] R4_house = new string[5] { "Residential/R4_Aa", "Residential/R4_Ba", "Residential/R4_C", "Residential/R4_D", "Residential/R4_E" };

	public static string[] R4S_house = new string[3] { "Residential/R4_AS", "Residential/R4_BS", "Residential/R4_FS" };

	public static string[] R5_house = new string[3] { "Residential/R5_A", "Residential/R5_B", "Residential/R5_C" };

	public static string[] R6_house = new string[3] { "Residential/R6_A", "Residential/R6_B", "Residential/R6_C" };

	public static string[] R7_house = new string[3] { "Residential/R7_A", "Residential/R7_B", "Residential/R7_C" };

	public static string[] R_Props = new string[16]
	{
		"Props/Prop_BigTrash", "Props/Prop_BigTrash2", "Props/Prop_Container", "Props/Prop_PicNicTable", "Props/Prop_OldCabin", "Props/Prop_Letter", "Props/Prop_Bushes", "Props/Prop_PlasticPool", "Props/Prop_SimpleParasol", "Props/Prop_SmallTrash",
		"Props/Prop_Ben", "Props/Prop_Debris", "Props/Prop_Washing", "Props/Prop_Den", "Props/Prop_HangerMax", "Props/Prop_Hanger"
	};

	public static string[] R_Front = new string[12]
	{
		"Props/Prop_FrontBushes", "Props/Prop_BushAndTrash", "Props/Prop_Letter", "Props/Prop_Bushes", "Props/Prop_FrontTrashLot", "Props/Prop_HangerMax", "Props/Prop_Hanger", "Props/Prop_Ben", "Props/Prop_Debris", "Props/Prop_Washing",
		"Props/Prop_BigTrash", "Props/Prop_BigTrash2"
	};

	public static string[] R_Street = new string[18]
	{
		"Props/Road/Prop_Bin", "Props/Road/Prop_Boxes", "Props/Road/Prop_Bricks", "Props/Road/Prop_Debris", "Props/Road/Prop_Hydrant", "Props/Road/SignSpeed", "Props/Road/SignPitch", "Props/Road/SignNo", "Props/Road/SignSlow", "Props/Road/Prop_Benne",
		"Props/Road/Prop_Basket", "Props/Road/Prop_OldPole", "Props/Road/Prop_Planks", "Props/Road/Prop_RoadDebris", "Props/Road/Prop_RoadDuo", "Props/Road/Prop_Trash", "Props/Road/Prop_Egout", "Props/Road/Prop_Egtwo"
	};

	public static string[] R_Dense = new string[10] { "Props/Prop_FrontBushes", "Props/Prop_BushAndTrash", "Props/Prop_Letter", "Props/Prop_Bushes", "Props/Prop_FrontTrashLot", "Props/Prop_Ben", "Props/Prop_Debris", "Props/Prop_Washing", "Props/Prop_BigTrash", "Props/Prop_BigTrash2" };

	public static string[] RR1_lot_4x4 = new string[1] { "Lots/RR1A_4x4" };

	public static string[] RR1_lot_3x4 = new string[1] { "Lots/RR1A_3x4" };

	public static string[] RR2_lot_4x4 = new string[1] { "Lots/RR2A_4x4" };

	public static string[] RR2_lot_3x4 = new string[1] { "Lots/RR2A_3x4" };

	public static string[] RR4_lot_4x4 = new string[1] { "Lots/RR4A_4x4" };

	public static string[] RR4_lot_3x4 = new string[1] { "Lots/RR4A_3x4" };

	public static string[] RR1_house = new string[5] { "Residential/RR1_Aa", "Residential/RR1_Ba", "Residential/RR1_C", "Residential/RR1_D", "Residential/RR1_E" };

	public static string[] RR1S_house = new string[4] { "Residential/RR1_Aa", "Residential/RR1_Ba", "Residential/RR1_C", "Residential/RR1_D" };

	public static string[] RR2_house = new string[3] { "Residential/RR2_Aa", "Residential/RR2_Ba", "Residential/RR2_C" };

	public static string[] RR3_house = new string[3] { "Residential/RR3_Aa", "Residential/RR3_Ba", "Residential/RR3_C" };

	public static string[] RR3S_house = new string[2] { "Residential/RR3_AS", "Residential/RR3_CS" };

	public static string[] RR4_house = new string[4] { "Residential/RR4_Aa", "Residential/RR4_Ba", "Residential/RR4_C", "Residential/RR4_D" };

	public static string[] RR4S_house = new string[3] { "Residential/RR4_AS", "Residential/RR4_CS", "Residential/RR4_DS" };

	public static string[] RR5_house = new string[3] { "Residential/RR5_A", "Residential/RR5_B", "Residential/RR5_C" };

	public static string[] RR6_house = new string[3] { "Residential/RR6_A", "Residential/RR6_B", "Residential/RR6_C" };

	public static string[] RR7_house = new string[4] { "Residential/RR7_A", "Residential/RR7_B", "Residential/RR7_C", "Residential/RR7_D" };

	public static string[] RR_Props = new string[14]
	{
		"Props/Prop_PurpleFlowers", "Props/Prop_YellowFlowers", "Props/Prop_Gazebo", "Props/Prop_WhitePots", "Props/Prop_RedPots", "Props/Prop_Garden", "Props/Prop_BasicPool", "Props/Prop_BasicPool", "Props/Prop_OldDine", "Props/Prop_Swing",
		"Props/Prop_BackBench", "Props/Prop_Circle", "Props/Prop_Roses", "Props/Prop_Dog"
	};

	public static string[] RR_Front = new string[8] { "Props/Prop_FrontBushes", "Props/Prop_FrontPots", "Props/Prop_YellowFlowers", "Props/Prop_Blues", "Props/Prop_OneRoses", "Props/Prop_WhitePots", "Props/Prop_RedPots", "Props/Prop_Swing" };

	public static string[] RR_Street = new string[16]
	{
		"Props/Road/Prop_Bin", "Props/Road/Prop_Hydrant", "Props/Road/Prop_Ad", "Props/Road/Prop_BinBenches", "Props/Road/Prop_Mail", "Props/Road/Prop_MyTree", "Props/Road/SignSpeed", "Props/Road/SignPitch", "Props/Road/SignNo", "Props/Road/SignSlow",
		"Props/Road/Prop_Trash", "Props/Road/Prop_Egout", "Props/Road/Prop_Egtwo", "Props/Road/Prop_City", "Props/Road/Prop_Grass", "Props/Road/Prop_Welcome"
	};

	public static string[] RR_Dense = new string[12]
	{
		"Props/Prop_Benches", "Props/Prop_FrontBushes", "Props/Prop_FrontPots", "Props/Prop_FrontTrashLot", "Props/Prop_YellowFlowers", "Props/Prop_Blues", "Props/Prop_OneRoses", "Props/Prop_TBench", "Props/Prop_FourBenches", "Props/Prop_WhitePots",
		"Props/Prop_RedPots", "Props/Prop_BackBench"
	};

	public static string[] RRR1_lot_4x4 = new string[1] { "Lots/RRR1A_4x4" };

	public static string[] RRR1_lot_3x4 = new string[1] { "Lots/RRR1A_3x4" };

	public static string[] RRR2_lot_4x4 = new string[1] { "Lots/RRR2A_4x4" };

	public static string[] RRR2_lot_3x4 = new string[1] { "Lots/RRR2A_3x4" };

	public static string[] RRR4_lot_4x4 = new string[1] { "Lots/RRR4A_4x4" };

	public static string[] RRR4_lot_3x4 = new string[1] { "Lots/RRR4A_3x4" };

	public static string[] RRR1_house = new string[5] { "Residential/RRR1_A", "Residential/RRR1_B", "Residential/RRR1_C", "Residential/RRR1_D", "Residential/RRR1_E" };

	public static string[] RRR1S_house = new string[2] { "Residential/RRR1_CS", "Residential/RRR1_DS" };

	public static string[] RRR2_house = new string[3] { "Residential/RRR2_A", "Residential/RRR2_B", "Residential/RRR2_C" };

	public static string[] RRR2S_house = new string[2] { "Residential/RRR2_A", "Residential/RRR2_C" };

	public static string[] RRR3_house = new string[3] { "Residential/RRR3_A", "Residential/RRR3_B", "Residential/RRR3_C" };

	public static string[] RRR3S_house = new string[2] { "Residential/RRR3_AS", "Residential/RRR3_BS" };

	public static string[] RRR4_house = new string[4] { "Residential/RRR4_A", "Residential/RRR4_B", "Residential/RRR4_C", "Residential/RRR4_D" };

	public static string[] RRR4S_house = new string[3] { "Residential/RRR4_AS", "Residential/RRR4_BS", "Residential/RRR4_CS" };

	public static string[] RRR5_house = new string[3] { "Residential/RRR5_A", "Residential/RRR5_B", "Residential/RRR5_C" };

	public static string[] RRR6_house = new string[4] { "Residential/RRR6_A", "Residential/RRR6_B", "Residential/RRR6_C", "Residential/RRR6_D" };

	public static string[] RRR7_house = new string[5] { "Residential/RRR7_A", "Residential/RRR7_B", "Residential/RRR7_C", "Residential/RRR7_D", "Residential/RRR7_E" };

	public static string[] RMC1_house = new string[3] { "Mixed/RMC1_A", "Mixed/RMC1_B", "Mixed/RMC1_C" };

	public static string[] RMC1S_house = new string[2] { "Mixed/RMC1_AS", "Mixed/RMC1_BS" };

	public static string[] RMC2_house = new string[2] { "Mixed/RMC2_A", "Mixed/RMC2_B" };

	public static string[] RMC2S_house = new string[2] { "Mixed/RMC2_AS", "Mixed/RMC2_BS" };

	public static string[] RMC3_house = new string[2] { "Mixed/RMC3_A", "Mixed/RMC3_B" };

	public static string[] RMC4_house = new string[2] { "Mixed/RMC4_A", "Mixed/RMC4_B" };

	public static string[] OUC1_house = new string[3] { "Mixed/OUC1_A", "Mixed/OUC1_B", "Mixed/OUC1_C" };

	public static string[] OUC1S_house = new string[2] { "Mixed/OUC1_AS", "Mixed/OUC1_BS" };

	public static string[] OUC2_house = new string[2] { "Mixed/OUC2_A", "Mixed/OUC2_B" };

	public static string[] OUC2S_house = new string[2] { "Mixed/OUC2_AS", "Mixed/OUC2_BS" };

	public static string[] OUC3_house = new string[2] { "Mixed/OUC3_A", "Mixed/OUC3_B" };

	public static string[] OUC4_house = new string[2] { "Mixed/OUC4_A", "Mixed/OUC4_B" };

	public static string[] RRR_Props = new string[17]
	{
		"Props/Prop_FancyPool", "Props/Prop_Jacuzzi", "Props/Prop_InfinityPool", "Props/Prop_BlackCouch", "Props/Prop_Fountain", "Props/Prop_CubeStatue", "Props/Prop_FlowersSquare", "Props/Prop_FlowersTrees", "Props/Prop_PurpleFlowers", "Props/Prop_YellowFlowers",
		"Props/Prop_Gazebo", "Props/Prop_Alleys", "Props/Prop_Lush", "Props/Prop_BBQ", "Props/Prop_Losange", "Props/Prop_Greek", "Props/Prop_FancyDine"
	};

	public static string[] RRR_Front = new string[11]
	{
		"Props/Prop_FrontBushes", "Props/Prop_FrontPots", "Props/Prop_Fountain", "Props/Prop_YellowFlowers", "Props/Prop_Losange", "Props/Prop_Greek", "Props/Prop_Lush", "Props/Prop_Alleys", "Props/Prop_CubeStatue", "Props/Prop_FlowersSquare",
		"Props/Prop_FlowersTrees"
	};

	public static string[] RRR_Street = new string[22]
	{
		"Props/Road/Prop_BinBenches", "Props/Road/Prop_Mail", "Props/Road/Prop_FourPlants", "Props/Road/Prop_Hedges", "Props/Road/Prop_LongHedge", "Props/Road/Prop_TwoPlants", "Props/Road/Prop_ThreePlants", "Props/Road/SignSpeed", "Props/Road/SignPitch", "Props/Road/SignNo",
		"Props/Road/SignSlow", "Props/Road/Prop_Fancy", "Props/Road/Prop_Vases", "Props/Road/SignSpeed", "Props/Road/SignPitch", "Props/Road/SignNo", "Props/Road/SignSlow", "Props/Road/Prop_Fancy", "Props/Road/Prop_Vases", "Props/Road/Prop_City",
		"Props/Road/Prop_Grass", "Props/Road/Prop_Welcome"
	};

	public static string[] RRR_Dense = new string[10] { "Props/Prop_FrontBushes", "Props/Prop_FrontPots", "Props/Prop_Fountain", "Props/Prop_Losange", "Props/Prop_Greek", "Props/Prop_Lush", "Props/Prop_Alleys", "Props/Prop_CubeStatue", "Props/Prop_FlowersSquare", "Props/Prop_FlowersTrees" };

	public static string[] Farm_house = new string[3] { "Industrial/Farm_A", "Industrial/Farm_B", "Industrial/Farm_C" };

	public static string[] I1_lot_4x4 = new string[1] { "Lots/M1A_4x4" };

	public static string[] I1_lot_3x4 = new string[1] { "Lots/M1A_3x4" };

	public static string[] I2_lot_4x4 = new string[1] { "Lots/M2A_4x4" };

	public static string[] I2_lot_3x4 = new string[1] { "Lots/M2A_3x4" };

	public static string[] I1_house = new string[3] { "Industrial/I1_A", "Industrial/I1_B", "Industrial/I1_C" };

	public static string[] I2_house = new string[3] { "Industrial/I2_A", "Industrial/I2_B", "Industrial/I2_C" };

	public static string[] I3_house = new string[3] { "Industrial/I3_A", "Industrial/I3_B", "Industrial/I3_C" };

	public static string[] I4_house = new string[3] { "Industrial/I4_A", "Industrial/I4_B", "Industrial/I4_C" };

	public static string[] HT1_house = new string[3] { "Industrial/HT1_A", "Industrial/HT1_B", "Industrial/HT1_C" };

	public static string[] HT1S_house = new string[2] { "Industrial/HT1_B", "Industrial/HT1_C" };

	public static string[] HT2_house = new string[3] { "Industrial/HT2_A", "Industrial/HT2_B", "Industrial/HT2_C" };

	public static string[] HT3_house = new string[3] { "Industrial/HT3_A", "Industrial/HT3_B", "Industrial/HT3_C" };

	public static string[] HT4_house = new string[3] { "Industrial/HT4_A", "Industrial/HT4_B", "Industrial/HT4_C" };

	public static string[] C1_lot_4x4 = new string[1] { "Lots/C1A_4x4" };

	public static string[] C1_lot_3x4 = new string[1] { "Lots/C1A_3x4" };

	public static string[] C2_lot_4x4 = new string[1] { "Lots/C2A_4x4" };

	public static string[] C2_lot_3x4 = new string[1] { "Lots/C2A_3x4" };

	public static string[] C3_lot_3x4 = new string[1] { "Lots/C3A_3x4" };

	public static string[] C3_lot_4x4 = new string[1] { "Lots/C3A_4x4" };

	public static string[] C1_house = new string[5] { "Commercial/C1_A", "Commercial/C1_B", "Commercial/C1_C", "Commercial/C1_D", "Commercial/C1_E" };

	public static string[] C1S_house = new string[4] { "Commercial/C1_A", "Commercial/C1_B", "Commercial/C1_C", "Commercial/C1_E" };

	public static string[] C2_house = new string[3] { "Commercial/C2_A", "Commercial/C2_B", "Commercial/C2_C" };

	public static string[] C2S_house = new string[3] { "Commercial/C3_AS", "Commercial/C2_CS", "Commercial/C2_DS" };

	public static string[] C3_house = new string[3] { "Commercial/C3_A", "Commercial/C3_B", "Commercial/C3_C" };

	public static string[] C4_house = new string[3] { "Commercial/C4_A", "Commercial/C4_B", "Commercial/C4_C" };

	public static string[] C5_house = new string[3] { "Commercial/C5_A", "Commercial/C5_B", "Commercial/C5_C" };

	public static string[] C_Sign = new string[17]
	{
		"Props/Prop_BigSignA", "Props/Prop_BigSignB", "Props/Prop_BigSignC", "Props/Prop_BigSignD", "Props/Prop_BigSignE", "Props/Prop_BigSignF", "Props/Prop_MediumSignA", "Props/Prop_MediumSignB", "Props/Prop_MediumSignC", "Props/Prop_MediumSignD",
		"Props/Prop_SmallSignA", "Props/Prop_SmallSignB", "Props/Prop_SmallSignC", "Props/Prop_SmallSignD", "Props/Prop_HighSignA", "Props/Prop_HighSignB", "Props/Prop_HighSignC"
	};

	public static string[] O2_lot_4x4 = new string[1] { "Lots/O2A_4x4" };

	public static string[] O2_lot_3x4 = new string[1] { "Lots/O2A_3x4" };

	public static string[] O3_lot_4x4 = new string[1] { "Lots/O3A_4x4" };

	public static string[] O3_lot_3x4 = new string[1] { "Lots/O3A_3x4" };

	public static string[] O1_house = new string[3] { "Commercial/O1_A", "Commercial/O1_B", "Commercial/O1_C" };

	public static string[] O2_house = new string[3] { "Commercial/O2_A", "Commercial/O2_B", "Commercial/O2_C" };

	public static string[] O3_house = new string[4] { "Commercial/O3_A", "Commercial/O3_B", "Commercial/O3_C", "Commercial/O3_D" };

	public static string[] O4_house = new string[4] { "Commercial/O4_A", "Commercial/O4_B", "Commercial/O4_C", "Commercial/O4_D" };

	public static string[] O5_house = new string[4] { "Commercial/O5_A", "Commercial/O5_B", "Commercial/O5_C", "Commercial/O5_D" };

	public static string[] O6_house = new string[3] { "Commercial/O6_A", "Commercial/O6_B", "Commercial/O6_C" };

	public static Vector2[] Cars_R1_4x4 = new Vector2[8]
	{
		new Vector2(-1.5f, -1.75f),
		new Vector2(-1.5f, -1.25f),
		new Vector2(-1.5f, -0.75f),
		new Vector2(-1.5f, -0.25f),
		new Vector2(-1.5f, 0.25f),
		new Vector2(-1.5f, 0.75f),
		new Vector2(-1.5f, 1.25f),
		new Vector2(-1.5f, 1.75f)
	};

	public static Vector2[] Cars_R1_3x4 = new Vector2[6]
	{
		new Vector2(-1.5f, -1.25f),
		new Vector2(-1.5f, -0.75f),
		new Vector2(-1.5f, -0.25f),
		new Vector2(-1.5f, 0.25f),
		new Vector2(-1.5f, 0.75f),
		new Vector2(-1.5f, 1.25f)
	};

	public static Vector2[] PropsFront_R1_4x4 = new Vector2[2]
	{
		new Vector2(-1.5f, -1.5f),
		new Vector2(-1.5f, 1.5f)
	};

	public static Vector2[] PropsFront_R1_3x4 = new Vector2[2]
	{
		new Vector2(-1.5f, -1f),
		new Vector2(-1.5f, 1f)
	};

	public static Vector2[] TreeTiles_4x4 = new Vector2[2]
	{
		new Vector2(-1.5f, -1f),
		new Vector2(-1.5f, 1f)
	};

	public static Vector2[] Cars_S_4x4 = new Vector2[6]
	{
		new Vector2(1.75f, 1.7f),
		new Vector2(1.25f, 1.7f),
		new Vector2(0.75f, 1.7f),
		new Vector2(0.25f, 1.7f),
		new Vector2(-0.25f, 1.7f),
		new Vector2(-0.75f, 1.7f)
	};

	public static Vector2[] Props_R1_4x4 = new Vector2[4]
	{
		new Vector2(1.5f, -1.5f),
		new Vector2(1.5f, -0.5f),
		new Vector2(1.5f, 0.5f),
		new Vector2(1.5f, 1.5f)
	};

	public static Vector2[] Props_R1_3x4 = new Vector2[3]
	{
		new Vector2(1.5f, -1f),
		new Vector2(1.5f, 0f),
		new Vector2(1.5f, 1f)
	};

	public static Vector2[] Props_Side_4x4 = new Vector2[4]
	{
		new Vector2(0.5f, 1.7f),
		new Vector2(-0.5f, 1.7f),
		new Vector2(0.5f, -1.7f),
		new Vector2(-0.5f, -1.7f)
	};

	public static Vector2[] Parking_4x = new Vector2[12]
	{
		new Vector2(-1.35f, 1.3f),
		new Vector2(-0.85f, 1.3f),
		new Vector2(-0.37f, 1.3f),
		new Vector2(0.12f, 1.3f),
		new Vector2(0.62f, 1.3f),
		new Vector2(1.1f, 1.3f),
		new Vector2(1.6f, 1.3f),
		new Vector2(-0.37f, 1.3f),
		new Vector2(0.12f, 1.3f),
		new Vector2(0.62f, 1.3f),
		new Vector2(1.1f, 1.3f),
		new Vector2(1.6f, 1.3f)
	};

	public static Vector2[] Parking_3x = new Vector2[7]
	{
		new Vector2(-1.35f, 0.8f),
		new Vector2(-0.85f, 0.8f),
		new Vector2(-0.37f, 0.8f),
		new Vector2(0.12f, 0.8f),
		new Vector2(0.62f, 0.8f),
		new Vector2(1.1f, 0.8f),
		new Vector2(1.6f, 0.8f)
	};

	public static string[] Cars_LC = new string[10] { "Vehicles/Hatch_Green", "Vehicles/Hatch_Brown", "Vehicles/Hatch_Grey", "Vehicles/Cross_Blue", "Vehicles/Cross_Green", "Vehicles/Cross_Grey", "Vehicles/Sedan_Black", "Vehicles/Sedan_Red", "Vehicles/Sedan_White", "Vehicles/Truck_A" };

	public static string[] Cars_MC = new string[7] { "Vehicles/Pickup_Black", "Vehicles/Pickup_Grey", "Vehicles/Pickup_White", "Vehicles/Cross_Blue", "Vehicles/Cross_Green", "Vehicles/Cross_Grey", "Vehicles/Truck_B" };

	public static string[] Cars_UC = new string[9] { "Vehicles/Hyper_Red", "Vehicles/Hyper_Lime", "Vehicles/Hyper_Cyan", "Vehicles/SUV_Blue", "Vehicles/SUV_Brown", "Vehicles/SUV_White", "Vehicles/Sports_Red", "Vehicles/Sports_Stripes", "Vehicles/Sports_White" };

	public static string[] Cars_M = new string[9] { "Vehicles/Hatch_Green", "Vehicles/Hatch_Brown", "Vehicles/Hatch_Grey", "Vehicles/Cross_Blue", "Vehicles/Cross_Green", "Vehicles/Cross_Grey", "Vehicles/Truck_A", "Vehicles/Truck_B", "Vehicles/Truck_C" };

	public static string[] Cars_HT = new string[9] { "Vehicles/Pickup_Black", "Vehicles/Pickup_Grey", "Vehicles/Pickup_White", "Vehicles/SUV_Blue", "Vehicles/SUV_Brown", "Vehicles/SUV_White", "Vehicles/Truck_A", "Vehicles/Truck_B", "Vehicles/Truck_C" };

	public static string[] Cars_O = new string[9] { "Vehicles/Sedan_Black", "Vehicles/Sedan_Red", "Vehicles/Sedan_White", "Vehicles/Cross_Blue", "Vehicles/Cross_Green", "Vehicles/Cross_Grey", "Vehicles/SUV_Blue", "Vehicles/SUV_Brown", "Vehicles/SUV_White" };

	public static string[] Cars_R = new string[18]
	{
		"Vehicles/Hatch_Green", "Vehicles/Hatch_Brown", "Vehicles/Hatch_Grey", "Vehicles/Cross_Blue", "Vehicles/Cross_Green", "Vehicles/Cross_Grey", "Vehicles/Sedan_Black", "Vehicles/Sedan_Red", "Vehicles/Sedan_White", "Vehicles/SUV_Blue",
		"Vehicles/SUV_Brown", "Vehicles/SUV_White", "Vehicles/Pickup_Black", "Vehicles/Pickup_Grey", "Vehicles/Pickup_White", "Vehicles/Truck_A", "Vehicles/Truck_B", "Vehicles/Truck_C"
	};

	public static string[] P_Cars_LC = new string[9] { "Vehicles/Parked/Hatch_Green", "Vehicles/Parked/Hatch_Brown", "Vehicles/Parked/Hatch_Grey", "Vehicles/Parked/Cross_Blue", "Vehicles/Parked/Cross_Green", "Vehicles/Parked/Cross_Grey", "Vehicles/Parked/Sedan_Black", "Vehicles/Parked/Sedan_Red", "Vehicles/Parked/Sedan_White" };

	public static string[] P_Cars_MC = new string[9] { "Vehicles/Parked/Pickup_Black", "Vehicles/Parked/Pickup_Grey", "Vehicles/Parked/Pickup_White", "Vehicles/Parked/Sedan_Black", "Vehicles/Parked/Sedan_Red", "Vehicles/Parked/Sedan_White", "Vehicles/Parked/Cross_Blue", "Vehicles/Parked/Cross_Green", "Vehicles/Parked/Cross_Grey" };

	public static string[] P_Cars_UC = new string[9] { "Vehicles/Parked/Hyper_Red", "Vehicles/Parked/Hyper_Lime", "Vehicles/Parked/Hyper_Cyan", "Vehicles/Parked/SUV_Blue", "Vehicles/Parked/SUV_Brown", "Vehicles/Parked/SUV_White", "Vehicles/Parked/Sports_Red", "Vehicles/Parked/Sports_Stripes", "Vehicles/Parked/Sports_White" };

	public static string[] P_Cars_M = new string[9] { "Vehicles/Parked/Hatch_Green", "Vehicles/Parked/Hatch_Brown", "Vehicles/Parked/Hatch_Grey", "Vehicles/Parked/Cross_Blue", "Vehicles/Parked/Cross_Green", "Vehicles/Parked/Cross_Grey", "Vehicles/Parked/Truck_A", "Vehicles/Parked/Truck_B", "Vehicles/Parked/Truck_C" };

	public static string[] P_Cars_HT = new string[9] { "Vehicles/Parked/Pickup_Black", "Vehicles/Parked/Pickup_Grey", "Vehicles/Parked/Pickup_White", "Vehicles/Parked/SUV_Blue", "Vehicles/Parked/SUV_Brown", "Vehicles/Parked/SUV_White", "Vehicles/Parked/Truck_A", "Vehicles/Parked/Truck_B", "Vehicles/Parked/Truck_C" };

	public static string[] P_Boats = new string[3] { "Vehicles/Parked/BoatBlue", "Vehicles/Parked/BoatRed", "Vehicles/Parked/BoatWhite" };

	public static string[] Drones_RES = new string[7] { "Vehicles/HoverS_White", "Vehicles/HoverS_DarkGrey", "Vehicles/HoverS_White", "Vehicles/HoverS_DarkGrey", "Vehicles/HoverS_Cyan", "Vehicles/HoverS_Lime", "Vehicles/HoverS_Yellow" };

	public static string[] Drones_IND = new string[6] { "Vehicles/HoverL_Ship", "Vehicles/HoverL_Ship", "Vehicles/HoverL_Cluses", "Vehicles/HoverL_Blue", "Vehicles/HoverL_Gpol", "Vehicles/HoverL_Red" };

	public static string[] Peds_Low = new string[9] { "man_casual", "man_farm", "man_mechanic", "man_plumber", "man_skate", "woman_lumberjack", "woman_basketball_player", "woman_carpenter", "woman_explorer" };

	public static string[] Peds_Middle = new string[10] { "man_casual", "man_metalhead", "man_reporter", "man_skate", "man_post", "man_tennis", "woman_metalhead", "woman_reporter", "woman_tennis", "woman_explorer" };

	public static string[] Peds_Rich = new string[11]
	{
		"man_business", "woman_business", "man_butler", "man_reporter", "man_post", "man_tennis", "woman_doctor", "woman_eskimo", "woman_judge", "woman_basketball_player",
		"woman_maid"
	};

	public static string[] Peds_All = new string[23]
	{
		"man_business", "woman_business", "man_butler", "man_casual", "man_farm", "man_mechanic", "man_metalhead", "man_reporter", "man_skate", "man_plumber",
		"man_post", "man_tennis", "woman_doctor", "woman_eskimo", "woman_judge", "woman_lumberjack", "woman_metalhead", "woman_reporter", "woman_tennis", "woman_basketball_player",
		"woman_carpenter", "woman_explorer", "woman_maid"
	};

	public static string[] Rioter = new string[8] { "man_actionhero", "man_lumberjack", "man_ninja", "man_paramedic", "man_weightlifter", "woman_actionhero", "woman_construction_worker", "woman_paramedic" };

	public static string[] Prop_Riot = new string[5] { "Pedestrian/Prop_RiotA", "Pedestrian/Prop_RiotB", "Pedestrian/Prop_RiotC", "Pedestrian/Prop_RiotD", "Pedestrian/Prop_RiotE" };

	public static List<Vector2> RioterPos = new List<Vector2>
	{
		new Vector2(-0.4f, 0.3f),
		new Vector2(-0.2f, 0.3f),
		new Vector2(0f, 0.3f),
		new Vector2(0.2f, 0.3f),
		new Vector2(0.4f, 0.3f),
		new Vector2(-0.4f, 0.1f),
		new Vector2(-0.2f, 0.1f),
		new Vector2(0f, 0.1f),
		new Vector2(0.2f, 0.1f),
		new Vector2(0.4f, 0.1f),
		new Vector2(-0.4f, -0.1f),
		new Vector2(-0.2f, -0.1f),
		new Vector2(0f, -0.1f),
		new Vector2(0.2f, -0.1f),
		new Vector2(0.4f, -0.1f),
		new Vector2(-0.4f, -0.3f),
		new Vector2(-0.2f, -0.3f),
		new Vector2(0f, -0.3f),
		new Vector2(0.2f, -0.3f),
		new Vector2(0.4f, -0.3f)
	};

	public static string[] Leaves_Types = new string[5] { "Trees/LeavesPalm", "Trees/LeavesSilk", "Trees/LeavesJava", "Trees/LeavesWalnut", "Trees/LeavesFan" };

	public static string[] Trunk_Types = new string[5] { "Trees/TrunkPalm", "Trees/TrunkSilk", "Trees/TrunkJava", "Trees/TrunkWalnut", "Trees/TrunkFan" };

	public static string[] Leaves_Temp = new string[5] { "Trees/LeavesSilk", "Trees/LeavesPara", "Trees/LeavesWalnut", "Trees/LeavesOr", "Trees/LeavesSpruce" };

	public static string[] Trunk_Temp = new string[5] { "Trees/TrunkSilk", "Trees/TrunkPara", "Trees/TrunkWalnut", "Trees/TrunkOr", "Trees/TrunkSpruce" };

	public static string[] Leaves_Arid = new string[5] { "Trees/LeavesThumb", "Trees/LeavesCactus", "Trees/LeavesPara", "Trees/LeavesFan", "Trees/LeavesThumb" };

	public static string[] Trunk_Arid = new string[5] { "Trees/TrunkThumb", "Trees/TrunkCactus", "Trees/TrunkPara", "Trees/TrunkFan", "Trees/TrunkThumb" };

	public static string[] Leaves_Road = new string[5] { "Trees/LeavesPalm", "Trees/LeavesSilk", "Trees/LeavesWalnut", "Trees/LeavesFan", "Trees/LeavesFan" };

	public static string[] Trunk_Road = new string[5] { "Trees/TrunkPalm", "Trees/TrunkSilk", "Trees/TrunkWalnut", "Trees/TrunkFan", "Trees/TrunkFan" };

	public static string[] Leaves_Low = new string[5] { "Trees/LeavesThumb", "Trees/LeavesSilk", "Trees/LeavesWalnut", "Trees/LeavesWalnut", "Trees/LeavesThumb" };

	public static string[] Trunk_Low = new string[5] { "Trees/TrunkThumb", "Trees/TrunkSilk", "Trees/TrunkWalnut", "Trees/TrunkWalnut", "Trees/TrunkThumb" };

	public static string[] Leaves_Hi = new string[5] { "Trees/LeavesPalm", "Trees/LeavesPara", "Trees/LeavesJava", "Trees/LeavesFan", "Trees/LeavesFan" };

	public static string[] Trunk_Hi = new string[5] { "Trees/TrunkPalm", "Trees/TrunkPara", "Trees/TrunkJava", "Trees/TrunkFan", "Trees/TrunkFan" };

	public static string[] Map_Geography = new string[9] { "Peninsula", "Archipel", "Plains", "Valleys", "Coastal", "Exotic", "Newplains", "Newvalley", "Map" };

	public static string[] Map_Variation = new string[10] { "Isa", "Dalawa", "Tatlo", "Apat", "Lima", "Anim", "Pito", "Walo", "Siyam", "Sampu" };

	public static string[] Tips = new string[48]
	{
		"LT0", "LT1", "LT2", "LT3", "LT4", "LT5", "LT6", "LT7", "LT8", "LT9",
		"LT10", "LT11", "LT12", "LT13", "LT14", "LT15", "LT16", "LT17", "LT18", "LT19",
		"LT20", "LT21", "LT22", "LT23", "LT24", "LT25", "LT26", "LT27", "LT28", "LT29",
		"LT30", "LT31", "LT32", "LT33", "LT34", "LT35", "LT36", "LT37", "LT38", "LT39",
		"LT40", "LT41", "LT42", "LT43", "LT44", "LT45", "LT46", "LT47"
	};

	public static List<Vector2> Res_S1_TreeOnly_x4 = new List<Vector2>
	{
		new Vector2(0.5f, -1.5f),
		new Vector2(-0.5f, -1.5f),
		new Vector2(0.5f, 1.5f),
		new Vector2(-0.5f, 1.5f)
	};

	public static List<Vector2> Res_S1_TreeOrProp_x4 = new List<Vector2>
	{
		new Vector2(1.5f, 1.5f),
		new Vector2(1.5f, -1.5f),
		new Vector2(-1.5f, 1.5f),
		new Vector2(-1.5f, -1.5f),
		new Vector2(1.5f, 0.5f),
		new Vector2(1.5f, -0.5f)
	};

	public static Vector2[] Res_S1_Cars_x4 = new Vector2[2]
	{
		new Vector2(-1.5f, 0.25f),
		new Vector2(-1.5f, -0.25f)
	};

	public static List<Vector2> Res_S1_TreeOrProp_x3 = new List<Vector2>
	{
		new Vector2(1.5f, 1f),
		new Vector2(1.5f, 0f),
		new Vector2(1.5f, -1f),
		new Vector2(-1.5f, 1f),
		new Vector2(-1.5f, -1f)
	};

	public static List<Vector2> Res_S1_TreeOnly_x3 = new List<Vector2>
	{
		new Vector2(-0.5f, 1f),
		new Vector2(-0.5f, -1f),
		new Vector2(0.5f, 1f),
		new Vector2(0.5f, -1f)
	};

	public static List<Vector2> Res_S2and3_TreeOnly_x4 = new List<Vector2>
	{
		new Vector2(0.5f, -1.5f),
		new Vector2(-0.5f, -1.5f),
		new Vector2(0.5f, 1.5f),
		new Vector2(-0.5f, 1.5f)
	};

	public static List<Vector2> Res_S2and3_TreeOrProp_x4 = new List<Vector2>
	{
		new Vector2(1.5f, 1.5f),
		new Vector2(1.5f, -1.5f),
		new Vector2(1.5f, 0.5f),
		new Vector2(1.5f, -0.5f)
	};

	public static List<Vector2> Res_S2and3_Cars_x4 = new List<Vector2>
	{
		new Vector2(-1.5f, 0.25f),
		new Vector2(-1.5f, -0.25f),
		new Vector2(-1.5f, 0.75f),
		new Vector2(-1.5f, -0.75f)
	};

	public static List<Vector2> Res_S2and3_TreeOrProp_x3 = new List<Vector2>
	{
		new Vector2(1.5f, 1f),
		new Vector2(1.5f, 0f),
		new Vector2(1.5f, -1f)
	};

	public static List<Vector2> Res_S2and3_Cars_x3 = new List<Vector2>
	{
		new Vector2(-1.5f, 1.25f),
		new Vector2(-1.5f, 0.75f),
		new Vector2(-1.5f, 0.25f),
		new Vector2(-1.5f, -0.25f)
	};

	public static List<Vector2> Res_S4and5_TreeOnly_x4 = new List<Vector2>
	{
		new Vector2(0.5f, -1.5f),
		new Vector2(-0.5f, -1.5f),
		new Vector2(0.5f, 1.5f),
		new Vector2(-0.5f, 1.5f)
	};

	public static List<Vector2> Res_S4and5_TreeOrProp_x4 = new List<Vector2>
	{
		new Vector2(1.5f, 1.5f),
		new Vector2(1.5f, -1.5f),
		new Vector2(1.5f, 0.5f),
		new Vector2(1.5f, -0.5f)
	};

	public static List<Vector2> Res_S4and5_Cars_x4 = new List<Vector2>
	{
		new Vector2(-1.5f, 1.75f),
		new Vector2(-1.5f, 1.25f),
		new Vector2(-1.5f, 0.25f),
		new Vector2(-1.5f, -0.25f),
		new Vector2(-1.5f, 0.75f),
		new Vector2(-1.5f, -0.75f)
	};

	public static List<Vector2> Res_S4and5_TreeOrProp_x3 = new List<Vector2>
	{
		new Vector2(1.5f, 1f),
		new Vector2(1.5f, 0f),
		new Vector2(1.5f, -1f)
	};

	public static List<Vector2> Res_S4and5_Cars_x3 = new List<Vector2>
	{
		new Vector2(-1.5f, 1.25f),
		new Vector2(-1.5f, 0.75f),
		new Vector2(-1.5f, 0.25f),
		new Vector2(-1.5f, -0.25f),
		new Vector2(-1.5f, -0.75f),
		new Vector2(-1.5f, -1.25f)
	};

	public static List<Vector2> ResUpp_S2_TreeOnly_x4 = new List<Vector2>
	{
		new Vector2(0.5f, -1.5f),
		new Vector2(-0.5f, -1.5f),
		new Vector2(0.5f, 1.5f),
		new Vector2(-0.5f, 1.5f)
	};

	public static List<Vector2> ResUpp_S2_TreeOrProp_x4 = new List<Vector2>
	{
		new Vector2(1.5f, 1.5f),
		new Vector2(1.5f, -1.5f),
		new Vector2(1.5f, 0.5f),
		new Vector2(1.5f, -0.5f),
		new Vector2(-1.5f, 0f)
	};

	public static List<Vector2> ResUpp_S2_Cars_x4 = new List<Vector2>
	{
		new Vector2(-1.5f, 1.75f),
		new Vector2(-1.5f, 1.25f),
		new Vector2(-1.5f, 0.75f),
		new Vector2(-1.5f, -0.75f),
		new Vector2(-1.5f, -1.25f),
		new Vector2(-1.5f, -1.75f)
	};

	public static List<Vector2> ResUpp_S2_TreeOrProp_x3 = new List<Vector2>
	{
		new Vector2(1.5f, 1f),
		new Vector2(1.5f, -1f),
		new Vector2(-1.5f, 0f)
	};

	public static List<Vector2> ResUpp_S2_Cars_x3 = new List<Vector2>
	{
		new Vector2(-1.5f, 1.25f),
		new Vector2(-1.5f, 0.75f),
		new Vector2(-1.5f, -0.75f),
		new Vector2(-1.5f, -1.25f)
	};

	public static List<Vector2> M1_Props_x4 = new List<Vector2>
	{
		new Vector2(1.5f, 1.5f),
		new Vector2(1.5f, -1.5f),
		new Vector2(-1.5f, 1.5f),
		new Vector2(-1.5f, -1.5f)
	};

	public static List<Vector2> M1_Props_x3 = new List<Vector2>
	{
		new Vector2(1.5f, 1f),
		new Vector2(1.5f, -1f),
		new Vector2(-1.5f, -1f),
		new Vector2(-1.5f, 1f)
	};

	public static List<Vector2> M1_Trees_x4 = new List<Vector2>
	{
		new Vector2(0.5f, 1.5f),
		new Vector2(0.5f, -1.5f),
		new Vector2(-0.5f, 1.5f),
		new Vector2(-0.5f, -1.5f),
		new Vector2(1.5f, -0.5f),
		new Vector2(1.5f, 0.5f)
	};

	public static List<Vector2> M1_Trees_x3 = new List<Vector2>
	{
		new Vector2(-0.5f, -1f),
		new Vector2(-0.5f, 1f),
		new Vector2(0.5f, -1f),
		new Vector2(0.5f, 1f),
		new Vector2(1.5f, 0f)
	};

	public static List<Vector2> M2_Props_x4 = new List<Vector2>
	{
		new Vector2(1.5f, 1.5f),
		new Vector2(1.5f, -1.5f)
	};

	public static List<Vector2> M2_Props_x3 = new List<Vector2>
	{
		new Vector2(1.5f, 1f),
		new Vector2(1.5f, -1f)
	};

	public static List<Vector2> C1_Trees_x4 = new List<Vector2>
	{
		new Vector2(1.5f, 1.5f),
		new Vector2(1.5f, -1.5f),
		new Vector2(-1.5f, 1.5f),
		new Vector2(-1.5f, -1.5f),
		new Vector2(1.5f, 0.5f),
		new Vector2(1.5f, -0.5f),
		new Vector2(0.5f, -1.5f),
		new Vector2(-0.5f, -1.5f),
		new Vector2(0.5f, 1.5f),
		new Vector2(-0.5f, 1.5f)
	};

	public static List<Vector2> C1_Trees_x3 = new List<Vector2>
	{
		new Vector2(1.5f, 1f),
		new Vector2(1.5f, 0f),
		new Vector2(1.5f, -1f),
		new Vector2(-1.5f, 1f),
		new Vector2(-1.5f, -1f),
		new Vector2(-0.5f, 1f),
		new Vector2(-0.5f, -1f),
		new Vector2(0.5f, 1f),
		new Vector2(0.5f, -1f)
	};

	public static List<Vector2> C2_Cars_x4 = new List<Vector2>
	{
		new Vector2(-0.5f, -1.75f),
		new Vector2(-0.5f, -1.25f),
		new Vector2(-0.5f, -0.75f),
		new Vector2(-0.5f, -0.25f),
		new Vector2(-0.5f, 0.25f),
		new Vector2(-0.5f, 0.75f),
		new Vector2(-0.5f, 1.25f),
		new Vector2(-0.5f, 1.75f)
	};

	public static List<Vector2> C2_Trees_x4 = new List<Vector2>
	{
		new Vector2(0.5f, -1.7f),
		new Vector2(0.5f, 1.7f),
		new Vector2(1.7f, -1.7f),
		new Vector2(1.7f, 1.7f)
	};

	public static List<Vector2> C2_Cars_x3 = new List<Vector2>
	{
		new Vector2(-0.5f, -1.25f),
		new Vector2(-0.5f, -0.75f),
		new Vector2(-0.5f, -0.25f),
		new Vector2(-0.5f, 0.25f),
		new Vector2(-0.5f, 0.75f),
		new Vector2(-0.5f, 1.25f)
	};

	public static List<Vector2> C2_Trees_x3 = new List<Vector2>
	{
		new Vector2(1.7f, 1f),
		new Vector2(1.7f, -1f),
		new Vector2(0.5f, 1f),
		new Vector2(0.5f, -1f),
		new Vector2(-0.5f, 1f),
		new Vector2(-0.5f, -1f)
	};

	public static List<Vector2> C3_Trees_x4 = new List<Vector2>
	{
		new Vector2(1.6f, -1.6f),
		new Vector2(1.6f, 1.6f),
		new Vector2(-1.6f, 1.6f)
	};

	public static List<Vector2> C3_Trees_x3 = new List<Vector2>
	{
		new Vector2(-1.6f, 1.1f),
		new Vector2(1.6f, -1.1f),
		new Vector2(1.6f, 1.1f)
	};

	public static Vector2[] Comm_Signs = new Vector2[2]
	{
		new Vector2(-1.8f, -0.75f),
		new Vector2(-1.8f, 0.75f)
	};

	public static List<string> cityName = new List<string>
	{
		"Sumeria", "Babylonia", "Salanca", "Shangrila", "Warens", "Enneci", "Camberia", "Sao Bernardino", "Amadeus", "Athena",
		"Vittor", "Charles-Albert", "Doran", "Daluyong", "Helix City", "Palisade", "Grand Horizon", "New Evian", "Siena", "Valoria",
		"Koh Laman", "Kol", "Uruk", "Assur", "Akkadian", "Assyria", "Zabalam", "Kahat", "Shibaniba", "Kwel At Lawet",
		"Emar", "Jericho", "Bethlehem", "Sodom", "Petra", "Eisenhuttenstadt", "Mannheim", "Spaichingen", "Altepetl", "Amaseia",
		"Pythopolis", "Apollonia", "Byzantium", "Cleonae", "Edessa", "Emporion", "Edelweiss", "Heliopolis", "Ithaca", "Kimmerikon",
		"Arcadia", "Lilaea", "Lucania", "Nymphaion", "Odessos", "Olympia", "Elis", "Pagasae", "Pharae", "Lumbai",
		"Poseidonia", "Roche Perfia", "Nhaka", "Stratos", "Sparta", "Rima", "Troy", "Baalbek", "Kowloon", "Leptis Magna",
		"Sierra", "Cahokia", "Novellino", "Mayapan", "Nim Li", "Coba Coba", "Monte Alban", "Newfoundland", "Harbour Grace", "Lukong",
		"New Atlantis", "Plymouth", "Salem", "Socorro", "Basseterre", "Greenfield", "Beauport", "Concord", "Haverhill", "Andover",
		"San Eldoro", "Owel Ijan", "Sulucen", "Libreville", "Guanacoville", "Hirohima", "Yokotakama", "Utsusasaki", "Otaru", "San Donato",
		"Averara", "Wagadoodoo", "Toteng", "Lamon", "Zyktar Sand", "Easterlies", "Domenico", "Kangclave", "Shooki", "Oleopolis",
		"Andaerium", "Suwon", "Dualta", "Constance", "Willville", "New Wright City", "Palatial", "Ostent", "Thomas", "Val Eden",
		"Tosashimizu", "Nakati", "Nao Lam", "Tsuha Masha", "Magsasa", "Mya", "Yanyan", "Hitomi", "Ebe", "Onaan",
		"Soleah", "Elea", "Saint Andrea", "Darantasia", "Omahasee", "Odetta", "Laustaca", "Zasawante", "Calliope", "Asteria",
		"Taalim", "Nipolis", "Oraclion", "Meridional", "Shiro", "Bellecombette", "Vittorio", "Santa Nim", "Roccioso", "Sulla Baia",
		"Yama", "Suprapolis", "Praz Amadeo", "Jesus Pike", "Lapu-Lapu", "Mesa", "Columbus", "Lombard", "Lafayette", "Zion",
		"Busanjin", "Coruscant", "Mulholland", "Vinewood", "Pointe Percée", "Risa", "Antares", "Qottawa", "Caesarea", "Abu Dammam",
		"Ayutthaya", "Lagoon Azul", "Philomene", "Villanova", "Kuala Katiagan", "Kota Alto", "Johnlocke", "Sao Fierro", "Rio De Febreiro", "El Nido",
		"Timbuktu", "Bamboo City", "Alpha Leno", "Floreale", "Estoleen", "Twin Oaks", "Donona", "Palm Coast", "Yumaltalia", "Pompadori",
		"Hiyakulto", "Carbon Falls", "Gaia", "Possession", "Typhoon City", "Lake Prime", "Accaju", "El Dorado", "Orange", "Volance",
		"Phonen", "Anatulip", "Jonastena", "Anatidae", "Sfax", "Ventidi", "Dongsam", "Delivrance", "Cape Abraham", "Mesa Rachel",
		"Elijah Valley", "Santa Olivia", "Alessia", "Hazel", "Ezekiel", "Yeouinaru", "Ellis", "Prince Oshawa", "Queen Astra", "Novadensue",
		"Galileo Bay", "Royal Beach", "Oppo Cove", "Stradabank", "Yadiel Hills", "Banana Hills", "San Ambrosio", "Aldebaran", "Jupiter", "Replica",
		"Callen", "Echosha", "Eureka", "Lavalette", "Taigacota", "Qolcago", "Azedarach", "Lions", "Yeonmi Park", "Grytviken",
		"Albatros", "Nordenskjöld", "Bocote", "Panorama", "Sidi Yahiya", "Galilee", "Tuvok", "Arab Al Naim", "Eshhar", "Kenaan",
		"Medusa"
	};

	public static List<string> nationName = new List<string>
	{
		"Afiliastan", "Alkania", "Aljazeria", "Angorra", "Andola", "Antibar and Zarzuela", "Alentina", "Arkenia", "Astra Republic", "Assyr",
		"Azoraijan", "Bahalahas", "Badrain", "Labangdesh", "Barbar", "Belacius", "Belandorium", "Beliss", "Beninski Republic", "Butuathan",
		"Boliveria", "Bolgovi and Herzonia", "Borealis", "Bornei", "Bagulria", "Yvoria", "Jungundi", "Cape Green", "Camdia", "Comwaloon",
		"Conodor", "Terra Central Republic", "Chilad", "Zhonghua", "Catambia", "Coloccos", "Koonga", "Costa Pobre", "Cornitia", "Cubentu",
		"Pycrus", "United Ceska", "Republic of the Koonga", "Enmar", "Djibi Libidji", "Ezador", "Ejik", "Sao Bahia Salvador", "Equatorial Emerica", "Erabicam",
		"Etnocia", "Watiland", "Seetopia", "Edelweiss Confederation", "Endlands", "West Galls", "Kabon", "Kambia", "Manyland", "Ghataga",
		"Hellas Republic", "Fireland", "Guamaltena", "Genie-Hissau", "Guayana", "Tayati", "Junlary", "Clearland", "Bharat Hindi", "Javalia",
		"Elan", "Aran", "Sarahael", "New Tuscany", "Tomaica", "Tsukihon", "Azarian", "Nya Nya", "Kibabiti", "Ouarzazate",
		"Lyrzystan", "Laotag", "Latopia", "Latanon", "Freetopia", "Bylibilia", "Hovermenstein", "Luthiana", "Taxembourg", "Wagalamare",
		"Marawi", "Alaysia", "Valdives", "Napiri", "Moritzania", "Helixo", "Netanesia", "Rococo", "Montolia", "Montelargo",
		"Maluacco", "Zanzovique", "Dyanmar", "Namilia", "Ahoru", "Napat", "Enderlands", "New Poland", "Nipenlagua", "Nukorium",
		"North Tsahala", "North Saladetia", "Norlands", "Upam", "Gajikistan", "Pawalau", "Hermanama", "Palawan New Koonga", "Paralands", "Eru",
		"Harmonies", "Prussia", "Port Regal", "Kotar", "Rusmania", "Arwanda", "Saint Lys", "Saint Elena", "Saint Arthur and Strawberries", "Soonlaboa",
		"Sao Bernardino", "Mahomethia", "Saul Goodmandia", "Sayhells", "Sierra Impressi", "Kualanpore", "Sloveria", "Salmon Islands", "Somaliland", "South Borneo",
		"South Tsahala", "South Pacifica", "Espon", "Sri Lemons", "Goldrigos", "Sapaudia", "Hissya", "Miseristan", "Zankania", "Koh Lailand",
		"Moker Lestor", "Jonga", "Teneridad and Bobos", "Ubanlaga", "Bidenheim", "United Mahometia", "United Nations of Stultitia", "Nohamnot", "Uzikistan", "Valoriatu",
		"Valenzuela", "West Stultitia", "Yevir", "Zanzib Republic", "Zimmam Mahometia", "Darkysztan", "Kyrzadstan"
	};

	public static int TreeType(float biome, float spec, float alt)
	{
		float num = (0.5f - biome) * 15f + (alt - 22f) / 10f + (spec - 25f) / 30f;
		if (num < 0f)
		{
			num = 0f;
		}
		else if (num > 4f)
		{
			num = 4f;
		}
		return Mathf.RoundToInt(num);
	}
}
