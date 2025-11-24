using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeGen;
public class CodeGenContext {
	// Assembly and module builders for .NET 9
	public PersistedAssemblyBuilder AssemblyBuilder { get; private set; }
	public ModuleBuilder ModuleBuilder { get; private set; }
	public TypeBuilder ProgramTypeBuilder { get; private set; }
	
	public TypeBuilder ArrayBuilder { get; private set; }
	
	// Current method context
	public ILGenerator CurrentIL { get; set; }
	public MethodBuilder CurrentMethod { get; set; }
	
	// Symbol mappings
	public Dictionary<string, LocalBuilder> LocalVariables { get; private set; }
	public Dictionary<string, FieldBuilder> GlobalFields { get; private set; }
	public Dictionary<string, MethodBuilder> Methods { get; private set; }
	public Dictionary<string, TypeBuilder> UserTypes { get; private set; }
	
	// Loop context for break/continue
	public Stack<LoopContext> LoopStack { get; private set; }
	
	// Type mapping helper
	private Dictionary<string, Type> typeMap;
	
	// Parameter mappings: parameter name -> argument index
	public Dictionary<string, int> ParameterIndices { get; private set; }

	// Parameter types for validation
	public Dictionary<string, Type> ParameterTypes { get; private set; }
	
	// Array metadata: type name -> (array type, fixed size)
	private Dictionary<string, (Type arrayType, int size)> arrayTypes;
	
	public CodeGenContext(string assemblyName)
	{
		// Initialize collections
		LocalVariables = new Dictionary<string, LocalBuilder>();
		GlobalFields = new Dictionary<string, FieldBuilder>();
		Methods = new Dictionary<string, MethodBuilder>();
		UserTypes = new Dictionary<string, TypeBuilder>();
		LoopStack = new Stack<LoopContext>();
		ParameterIndices = new Dictionary<string, int>();
		ParameterTypes = new Dictionary<string, Type>();
		arrayTypes = new Dictionary<string, (Type, int)>();
		
		// Type mapping for built-in types
		typeMap = new Dictionary<string, Type>
		{
			{ "integer", typeof(int) },
			{ "real", typeof(float) },
			{ "boolean", typeof(bool) },
			{ "void", typeof(void) }
		};
		
		// Create assembly and module
		var asmName = new AssemblyName(assemblyName);
		var coreAssembly = typeof(object).Assembly;
		AssemblyBuilder = new PersistedAssemblyBuilder(asmName, coreAssembly);
		ModuleBuilder = AssemblyBuilder.DefineDynamicModule("MainModule");
		
		// Create main Program type
		ProgramTypeBuilder = ModuleBuilder.DefineType(
			"Program",
			TypeAttributes.Public | TypeAttributes.Class);
		
		// Create array ...
		ArrayBuilder = GenerateArrayClass();
	}
	
	public Type ResolveType(string typeName)
	{
		if (typeMap.ContainsKey(typeName))
			return typeMap[typeName];
		if (UserTypes.ContainsKey(typeName))
			return UserTypes[typeName];
		return typeof(object);
	}
	
	public void EnterLoop(Label breakLabel, Label continueLabel)
	{
		LoopStack.Push(new LoopContext(breakLabel, continueLabel));
	}
	
	public void ExitLoop()
	{
		LoopStack.Pop();
	}
	
	public LoopContext CurrentLoop => LoopStack.Peek();
	
	public void Save(string outputPath)
	{
		// Finalize the Program type
		ProgramTypeBuilder.CreateType();
		
		// Save to disk
		AssemblyBuilder.Save(outputPath);
	}
	
	public void ClearParameters()
	{
		ParameterIndices.Clear();
		ParameterTypes.Clear();
	}
	
	public void RegisterTypeAlias(string aliasName, Type baseType)
	{
		typeMap[aliasName] = baseType;
	}

	public void RegisterArrayType(string typeName, Type arrayType, int size)
	{
		typeMap[typeName] = arrayType;
		arrayTypes[typeName] = (arrayType, size);
	}

	public void RegisterUserType(string typeName, TypeBuilder typeBuilder)
	{
		UserTypes[typeName] = typeBuilder;
		typeMap[typeName] = typeBuilder;
	}

	public int GetArraySize(string typeName)
	{
		if (arrayTypes.ContainsKey(typeName))
			return arrayTypes[typeName].size;
		return -1;
	}
	
	private TypeBuilder GenerateArrayClass () {
		var typeBuilder = this.ModuleBuilder.DefineType(
			"ArrayClass`1",
			System.Reflection.TypeAttributes.Public |
			System.Reflection.TypeAttributes.Abstract |
			System.Reflection.TypeAttributes.Class);

		var genericParams = typeBuilder.DefineGenericParameters(new string[] {"T"});
		var T = genericParams[0];
		
		var defaultMemberCtor = typeof(System.Reflection.DefaultMemberAttribute)
			.GetConstructor(new Type[] { typeof(string) });
		var defaultMemberAttr = new CustomAttributeBuilder(
			defaultMemberCtor,
			new object[] { "Item" });
		typeBuilder.SetCustomAttribute(defaultMemberAttr);
		
		// Size field
		var sizeField = typeBuilder.DefineField(
			"Size",
			typeof(int),
			FieldAttributes.Private | FieldAttributes.Static);

		// Data field
		var dataField = typeBuilder.DefineField(
			"data",
			T.MakeArrayType(),
			FieldAttributes.Public);

		// Constructor
		var constructor = typeBuilder.DefineConstructor(
			MethodAttributes.Public | MethodAttributes.HideBySig |
			MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
			CallingConventions.Standard,
			new Type[]{typeof(int)});

		var ctorIL = constructor.GetILGenerator();

		ctorIL.Emit(OpCodes.Ldarg_0);
		ctorIL.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));

		ctorIL.Emit(OpCodes.Ldarg_0);
		ctorIL.Emit(OpCodes.Ldarg_1);
		ctorIL.Emit(OpCodes.Newarr, T);
		ctorIL.Emit(OpCodes.Stfld, dataField);
		ctorIL.Emit(OpCodes.Ret);


		// Indexer get item
		var getItem = typeBuilder.DefineMethod(
			"get_Item",
			MethodAttributes.Public | MethodAttributes.HideBySig |
			MethodAttributes.SpecialName,
			T,
			new Type[]{typeof(int)});

		var getIL = getItem.GetILGenerator();
		var throw_label = getIL.DefineLabel();

		// index < 1
		getIL.Emit(OpCodes.Ldarg_1);
		getIL.Emit(OpCodes.Ldc_I4_1);
		getIL.Emit(OpCodes.Blt, throw_label);

		// index > len
		getIL.Emit(OpCodes.Ldarg_1);
		getIL.Emit(OpCodes.Ldarg_0);
		getIL.Emit(OpCodes.Ldfld, dataField);
		getIL.Emit(OpCodes.Ldlen);
		getIL.Emit(OpCodes.Bgt, throw_label);

		// return data[index-1]
		getIL.Emit(OpCodes.Ldarg_0);
		getIL.Emit(OpCodes.Ldfld, dataField);
		getIL.Emit(OpCodes.Ldarg_1);
		getIL.Emit(OpCodes.Ldc_I4_1);
		getIL.Emit(OpCodes.Sub);
		getIL.Emit(OpCodes.Ldelem, T);
		getIL.Emit(OpCodes.Ret);

		// Index out of range
		getIL.MarkLabel(throw_label);
		var exceptionCtor = typeof(IndexOutOfRangeException).GetConstructor(Type.EmptyTypes);
		getIL.Emit(OpCodes.Newobj, exceptionCtor);
		getIL.Emit(OpCodes.Throw);

		// Indexer set item
		var setItem = typeBuilder.DefineMethod(
			"set_Item",
			MethodAttributes.Public | MethodAttributes.HideBySig |
			MethodAttributes.SpecialName,
			typeof(void),
			new Type[]{typeof(int), T});

		var setIL = setItem.GetILGenerator();
		throw_label = setIL.DefineLabel();

		// index < 1
		setIL.Emit(OpCodes.Ldarg_1);
		setIL.Emit(OpCodes.Ldc_I4_1);
		setIL.Emit(OpCodes.Blt, throw_label);

		// index > len
		setIL.Emit(OpCodes.Ldarg_1);
		setIL.Emit(OpCodes.Ldarg_0);
		setIL.Emit(OpCodes.Ldfld, dataField);
		setIL.Emit(OpCodes.Ldlen);
		setIL.Emit(OpCodes.Bgt, throw_label);

		// data[index-1] = value
		setIL.Emit(OpCodes.Ldarg_0);
		setIL.Emit(OpCodes.Ldfld, dataField);
		setIL.Emit(OpCodes.Ldarg_1);
		setIL.Emit(OpCodes.Ldc_I4_1);
		setIL.Emit(OpCodes.Sub);
		setIL.Emit(OpCodes.Ldarg_2);
		setIL.Emit(OpCodes.Stelem, T);
		setIL.Emit(OpCodes.Ret);

		// Index out of range
		setIL.MarkLabel(throw_label);
		setIL.Emit(OpCodes.Newobj, exceptionCtor);
		setIL.Emit(OpCodes.Throw);

		// Indexer
		var itemProperty = typeBuilder.DefineProperty(
			"Item",
			PropertyAttributes.None,
			T,
			new Type[]{typeof(int)});
		itemProperty.SetGetMethod(getItem);
		itemProperty.SetSetMethod(setItem);

		typeBuilder.CreateType();
		return typeBuilder;
	}
}


public class LoopContext
{
	public Label BreakLabel { get; }
	public Label ContinueLabel { get; }
	
	public LoopContext(Label breakLabel, Label continueLabel)
	{
		BreakLabel = breakLabel;
		ContinueLabel = continueLabel;
	}
}
