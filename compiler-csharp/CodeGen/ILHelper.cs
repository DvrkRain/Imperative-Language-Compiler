using System;
using System.Reflection.Emit;

namespace CodeGen
{
    public static class ILHelper
    {
        public static void EmitLoadInt(ILGenerator il, int value) =>
			il.Emit(OpCodes.Ldc_I4, value);
        
        public static void EmitLoadInt(ILGenerator il, float value) =>
			il.Emit(OpCodes.Ldc_I4, (int)Math.Round(value, MidpointRounding.AwayFromZero));
        
        public static void EmitLoadInt(ILGenerator il, bool value) =>
			il.Emit(OpCodes.Ldc_I4, value?1:0);
        
        public static void EmitLoadReal(ILGenerator il, int value) =>
            il.Emit(OpCodes.Ldc_R8, (float)value);
        
        public static void EmitLoadReal(ILGenerator il, float value) =>
            il.Emit(OpCodes.Ldc_R8, value);
        
        public static void EmitLoadReal(ILGenerator il, bool value) =>
            il.Emit(OpCodes.Ldc_R8, value?1f:0f);
        
        public static void EmitLoadBool(ILGenerator il, int value) =>
            il.Emit(value != 0 ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
        
        public static void EmitLoadBool(ILGenerator il, float value) {
			return; }
        
        public static void EmitLoadBool(ILGenerator il, bool value) =>
            il.Emit(value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
    }
}
