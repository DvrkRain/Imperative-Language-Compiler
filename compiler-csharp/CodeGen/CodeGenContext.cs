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
	
	// Current method context
	public ILGenerator CurrentIL { get; set; }
	public MethodBuilder CurrentMethod { get; set; }
	
	// Symbol mappings
	public Dictionary<string, LocalBuilder> LocalVariables { get; private set; }
	public Dictionary<string, FieldBuilder> GlobalFields { get; private set; }
	public Dictionary<string, MethodBuilder> Methods { get; private set; }
	public Dictionary<string, TypeBuilder> UserTypes { get; private set; }
	public Dictionary<string, System.Type> ArrayTypes { get; private set; }
	
	// Loop context for break/continue
	public Stack<LoopContext> LoopStack { get; private set; }
	
	// Type mapping helper
	private Dictionary<string, Type> typeMap;
	
	// Parameter mappings: parameter name -> argument index
	public Dictionary<string, int> ParameterIndices { get; private set; }

	// Parameter types for validation
	public Dictionary<string, Type> ParameterTypes { get; private set; }
	
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
		ArrayTypes = new Dictionary<string, Type>();
		
		// Type mapping for built-in types
		typeMap = new Dictionary<string, Type>
		{
			{ "integer", typeof(int) },
			{ "real", typeof(float) },
			{ "boolean", typeof(bool) },
			{ "void", typeof(void) },
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

	public void RegisterUserType(string typeName, TypeBuilder typeBuilder)
	{
		UserTypes[typeName] = typeBuilder;
		typeMap[typeName] = typeBuilder;
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
