using System;
using System.Reflection.Emit;

namespace CodeGen
{
    public static class ILHelper
    {
        // Load integer constant (optimized)
        public static void EmitLoadInt(ILGenerator il, int value) =>
			il.Emit(OpCodes.Ldc_I4, value);
        
        // Load boolean constant
        public static void EmitLoadBool(ILGenerator il, bool value) =>
            il.Emit(value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
        
        // Load real constant
        public static void EmitLoadReal(ILGenerator il, double value) =>
            il.Emit(OpCodes.Ldc_R8, value);
    }
}
