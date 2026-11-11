using System.Reflection;
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
}
