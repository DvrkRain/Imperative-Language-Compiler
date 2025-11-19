using System;
using System.Reflection.Emit;

namespace CodeGen
{
    public static class ILHelper
    {
        // Load integer constant (optimized)
        public static void EmitLoadInt(ILGenerator il, int value)
        {
            switch (value)
            {
                case -1: il.Emit(OpCodes.Ldc_I4_M1); break;
                case 0: il.Emit(OpCodes.Ldc_I4_0); break;
                case 1: il.Emit(OpCodes.Ldc_I4_1); break;
                case 2: il.Emit(OpCodes.Ldc_I4_2); break;
                case 3: il.Emit(OpCodes.Ldc_I4_3); break;
                case 4: il.Emit(OpCodes.Ldc_I4_4); break;
                case 5: il.Emit(OpCodes.Ldc_I4_5); break;
                case 6: il.Emit(OpCodes.Ldc_I4_6); break;
                case 7: il.Emit(OpCodes.Ldc_I4_7); break;
                case 8: il.Emit(OpCodes.Ldc_I4_8); break;
                default:
                    if (value >= sbyte.MinValue && value <= sbyte.MaxValue)
                        il.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
                    else
                        il.Emit(OpCodes.Ldc_I4, value);
                    break;
            }
        }
        
        // Load boolean constant
        public static void EmitLoadBool(ILGenerator il, bool value)
        {
            il.Emit(value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
        }
        
        // Load real constant
        public static void EmitLoadReal(ILGenerator il, double value)
        {
            il.Emit(OpCodes.Ldc_R8, value);
        }
    }
}