using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace Lilly.ZeroPathCost
{
    [StaticConstructorOnStartup]
    internal static class ZeroPathCostBootstrap
    {
        static ZeroPathCostBootstrap()
        {
            new Harmony("lilly.zeropathcost").PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(PathGrid), nameof(PathGrid.CalculatedCostAt))]
    internal static class PathGrid_CalculatedCostAt_Patch
    {
        private static void Postfix(ref int __result)
        {
            if (__result < PathGrid.ImpassableCost)
                __result = 0;
        }
    }

    [HarmonyPatch]
    internal static class Pawn_PathFollower_TerrainMoveSpeed_Patch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(Pawn_PathFollower),
                "CostToMoveIntoCell",
                new[] { typeof(Pawn), typeof(IntVec3) });
        }

        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            MethodInfo getTerrain = AccessTools.Method(
                typeof(GridsUtility),
                nameof(GridsUtility.GetTerrain),
                new[] { typeof(IntVec3), typeof(Map) });

            int terrainCall = -1;
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Calls(getTerrain))
                {
                    terrainCall = i;
                    break;
                }
            }

            int terrainFactorDivision = -1;
            int terrainBlockEnd = -1;
            if (terrainCall >= 0)
            {
                for (int i = terrainCall + 1; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldc_R4 &&
                        codes[i].operand is float value && value == 450f)
                    {
                        terrainBlockEnd = i;
                        break;
                    }

                    if (codes[i].opcode == OpCodes.Div)
                    {
                        if (terrainFactorDivision >= 0)
                        {
                            terrainFactorDivision = -1;
                            break;
                        }
                        terrainFactorDivision = i;
                    }
                }
            }

            if (terrainFactorDivision < 1 ||
                terrainBlockEnd <= terrainFactorDivision ||
                codes[terrainFactorDivision - 1].opcode != OpCodes.Ldloc_S)
            {
                Log.Error("[Zero Path Cost] Could not safely locate the terrain speed factor in " +
                    "Pawn_PathFollower.CostToMoveIntoCell. Terrain speed modifiers were not changed.");
                return codes;
            }

            CodeInstruction factor = codes[terrainFactorDivision - 1];
            factor.opcode = OpCodes.Ldc_R4;
            factor.operand = 1f;
            return codes;
        }
    }
}
