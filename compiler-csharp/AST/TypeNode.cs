using System.Drawing;
using System.Reflection;
using Data.ErrorHandling;
using Data.Objects;
using SemanticAnalyzer.SymbolTable;
using System.Reflection.Emit;
using CodeGen;
using SystemType = System.Type; 


namespace AST;
public class TypeNode : Node {
	public TypeNode(Position pos) : base(pos) { }

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "TypeNode") Console.WriteLine($"TypeNode(childs={this.childs.Count}, pos=({this.position.Row()}, {this.position.Col()}))");
		base.PrintInfo(indent);
	}


	public override void Parse(ref Queue<Token> tokenQueue) {
		// Identifier
		Token token = tokenQueue.Peek();
		if(token.Code() != TokenCode.identifier) {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}
		this.childs.Add(new PrimaryNode(token.Position(), token.Value()));
		tokenQueue.Dequeue();

		// 'is' keyword
		token = tokenQueue.Peek();
		if(token.Code() != TokenCode.is_assignment) {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}
		tokenQueue.Dequeue();

		// Type
		token = tokenQueue.Peek();
		switch(token.Code()) {
			case TokenCode.identifier:
				tokenQueue.Dequeue();
				this.childs.Add(new PrimaryNode(token.Position(), token.Value()));
				break;

			case TokenCode.builtin_type:
				tokenQueue.Dequeue();
				this.childs.Add(new PrimaryNode(token.Position(), token.Value()));
				break;

			case TokenCode.record_declaration:
				tokenQueue.Dequeue();
				RecordNode rec = new RecordNode(token.Position());
				rec.Parse(ref tokenQueue);
				this.childs.Add(rec);
				break;

			case TokenCode.array_declaration:
				tokenQueue.Dequeue();
				ArrayNode arr = new ArrayNode(this.position);
				arr.Parse(ref tokenQueue);
				this.childs.Add(arr);
				break;

			default:
				HandleUnexpectedToken(ref tokenQueue, token.Position());
				return;
		}

		// Check if declaration ends with ';'
		token = tokenQueue.Peek();
		if(token.Code() != TokenCode.semicolon) {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
		}
		tokenQueue.Dequeue();
	}


    public override void Verify() {
        this.childs[0].Verify(); // check identifier
        // Type declaration looks as follows:
        // type `Identifier` is `Type`
        
        // `Identifier` ->  PrimaryNode
        
        // Type can be
        // - PrimitiveType (integer, real, boolean) -> PrimaryNode
        // - UserType (ArrayType, RecordType)-> ArrayNode, RecordNode
        // - Identifier -> PrimaryNode
        
        // Thus, we expect childs to be
        // PrimaryNode + (PrimaryNode/ArrayNode/RecordNode)

        if (this.childs.Count() != 2) {
            ErrorHandling.Add("TypeNode", this.position, $"Expected to have 2 childs, got {this.childs.Count()}");
            return;
        }

        if (!VerifyIdentifier() || !VerifyType()) return;
        
        PrimaryNode identifier = (PrimaryNode)this.childs[0];
        string baseType;
        Scope? typeScope = null;

        switch (this.childs[1]) {
            case PrimaryNode primaryNode:
                primaryNode.Verify();
                baseType = primaryNode.Name();

                while (!DedicatedWords.BuiltIn(baseType))
                    baseType = ((SemanticAnalyzer.SymbolTable.Type)SymbolTable.FindEntry(baseType)).BaseType;

                break;
            
            case ArrayNode arr:
                baseType = "array";
                arr.Verify();
                typeScope = new Scope();
                typeScope.AddEntry(new Variable("size", "integer"));
                typeScope.AddEntry(new Variable("type", "void", arr.Type()));
                
                break;
            
            case RecordNode:
                baseType = "record";
                SymbolTable.EnterScope(ScopeType.Record);
                this.childs[1].Verify();
                typeScope = SymbolTable.GetCurrentScope();
                SymbolTable.ExitScope();
                typeScope.Parent = null;
                break;
            
            default:
                baseType = (string)identifier.value;
                this.childs[1].Verify();
                break;
        }

        SemanticAnalyzer.SymbolTable.Type newType =
            new SemanticAnalyzer.SymbolTable.Type((string)identifier.value, baseType, typeScope);

        if (baseType == "record") newType.TypeScope = typeScope;

        SymbolTable.DeclareEntry(newType);
    }

    private bool VerifyIdentifier() {
        // Check identifier node type
        if (this.childs[0] is not PrimaryNode) {
            ErrorHandling.Add("TypeNode", this.position, "Expected PrimaryNode");
            return false;
        }
        
        PrimaryNode identifier = (PrimaryNode)this.childs[0];
        
        // Check for redeclaration
        if (SymbolTable.FindEntry((string)identifier.value, true) != null) {
            ErrorHandling.Add("TypeNode", this.position, "Type redeclaration");
            return false;
        }

        return true;
    }

    private bool VerifyType() {
        // Type can be
        // - PrimitiveType (integer, real, boolean) -> PrimaryNode
        // - UserType (ArrayType, RecordType)-> ArrayNode, RecordNode
        // - Identifier -> PrimaryNode
        
        // Check child type
        switch (this.childs[1]) {
            case PrimaryNode primaryNode:
                if (SymbolTable.FindEntry((string)primaryNode.value) is not SemanticAnalyzer.SymbolTable.Type) {
                    ErrorHandling.Add("TypeNode", this.position, $"Type '{(string)primaryNode.value}' is not declared");
                    return false;
                }
                break;
            
            case ArrayNode arrayNode:
                int arrayChilds = arrayNode.GetChilds().Count();
                PrimaryNode arrayType = (PrimaryNode)arrayNode.GetChilds()[arrayChilds - 1];

                if (SymbolTable.FindEntry((string)arrayType.value) is not SemanticAnalyzer.SymbolTable.Type) {
                    ErrorHandling.Add("TypeNode", this.position, $"Array type '{(string)arrayType.value}' is not declared");
                    return false;
                }
                break;
            
            case RecordNode recordNode:
                // TODO: Create a way to check if record is alright
                break;

            default:
                ErrorHandling.Add("TypeNode", this.position, $"Unexpected type '{this.childs[1].GetType().Name}'");
                return false;
        }
        
        return true;
    }
    
    public override void Generate(CodeGen.CodeGenContext ctx)
    {
        string typeName = (string)((PrimaryNode)this.childs[0]).value;
        Node typeDefinition = this.childs[1];
        
        if (typeDefinition is PrimaryNode aliasNode) {
            // Type alias: type MyInt is integer
            string baseTypeName = aliasNode.Name();
            SystemType baseType = ctx.ResolveType(baseTypeName);
            
            // Register alias in context (add to type mapping)
            ctx.RegisterTypeAlias(typeName, baseType);

        } else if (typeDefinition is ArrayNode arrayNode)
            // Array type: type intarr is array [5] integer
            GenerateArrayType(ctx, typeName);

        else if (typeDefinition is RecordNode recordNode)
            // Record type: type Person is record ... end
            GenerateRecordType(ctx, typeName);
    }

    private void GenerateArrayType(CodeGen.CodeGenContext ctx, string typeName) {
        // Get array size (childs[0] is size expression)
		ArrayNode arr = (ArrayNode)this.childs[1];
        
        // Get element type (childs[1] is type)
        string elementTypeName = (string)((PrimaryNode)arr.GetChilds()[1]).value;
        SystemType elementType = ctx.ResolveType(elementTypeName);

        var typeBuilder = ctx.ModuleBuilder.DefineType(
			typeName,
			System.Reflection.TypeAttributes.Public |
			System.Reflection.TypeAttributes.Class);
		
        // Specify custom index property
		var defaultMemberCtor = typeof(System.Reflection.DefaultMemberAttribute)
			.GetConstructor(new System.Type[] { typeof(string) });
		var defaultMemberAttr = new CustomAttributeBuilder(
			defaultMemberCtor,
			new object[] { "Item" });
		typeBuilder.SetCustomAttribute(defaultMemberAttr);
		
		// Size field
		var sizeStatField = typeBuilder.DefineField(
			"Size",
			typeof(int),
			FieldAttributes.Private | FieldAttributes.Static);
		
		var sizeField = typeBuilder.DefineField(
			"size",
			typeof(int),
			FieldAttributes.Public);

		// Data field
		var dataField = typeBuilder.DefineField(
			"data",
			elementType.MakeArrayType(),
			FieldAttributes.Public);

		// Constructor
		var constructor = typeBuilder.DefineConstructor(
			MethodAttributes.Public | MethodAttributes.HideBySig |
			MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
			CallingConventions.Standard,
			System.Type.EmptyTypes);

		var ctorIL = constructor.GetILGenerator();
		var oldIL = ctx.CurrentIL;
		ctx.CurrentIL = ctorIL;

		// base()
		ctorIL.Emit(OpCodes.Ldarg_0);
		ctorIL.Emit(OpCodes.Call, typeof(object).GetConstructor(System.Type.EmptyTypes));

		// Init Size
		arr.Generate(ctx);
		ctorIL.Emit(OpCodes.Stsfld, sizeStatField);

		// Init size
		ctorIL.Emit(OpCodes.Ldarg_0);
		ctorIL.Emit(OpCodes.Ldsfld, sizeStatField);
		ctorIL.Emit(OpCodes.Stfld, sizeField);

		// data = new <type>[size]
		ctorIL.Emit(OpCodes.Ldarg_0);
		ctorIL.Emit(OpCodes.Ldarg_0);
		ctorIL.Emit(OpCodes.Ldfld, sizeField);
		ctorIL.Emit(OpCodes.Newarr, elementType);
		ctorIL.Emit(OpCodes.Stfld, dataField);
		ctorIL.Emit(OpCodes.Ret);
		
		ctx.CurrentIL = oldIL;

		// Indexer get item
		var getItem = typeBuilder.DefineMethod(
			"get_Item",
			MethodAttributes.Public | MethodAttributes.HideBySig |
			MethodAttributes.SpecialName,
			elementType,
			new System.Type[]{typeof(int)});

		var getIL = getItem.GetILGenerator();
		var throw_label = getIL.DefineLabel();

		// index < 0
		getIL.Emit(OpCodes.Ldarg_1);
		getIL.Emit(OpCodes.Ldc_I4_0);
		getIL.Emit(OpCodes.Blt, throw_label);

		// index >= len
		getIL.Emit(OpCodes.Ldarg_1);
		getIL.Emit(OpCodes.Ldarg_0);
		getIL.Emit(OpCodes.Ldfld, sizeField);
		getIL.Emit(OpCodes.Bge, throw_label);

		// return data[index]
		getIL.Emit(OpCodes.Ldarg_0);
		getIL.Emit(OpCodes.Ldfld, dataField);
		getIL.Emit(OpCodes.Ldarg_1);
		getIL.Emit(OpCodes.Ldelem, elementType);
		getIL.Emit(OpCodes.Ret);

		// Index out of range
		getIL.MarkLabel(throw_label);
		var exceptionCtor = typeof(IndexOutOfRangeException).GetConstructor(System.Type.EmptyTypes);
		getIL.Emit(OpCodes.Newobj, exceptionCtor);
		getIL.Emit(OpCodes.Throw);

		// Indexer set item
		var setItem = typeBuilder.DefineMethod(
			"set_Item",
			MethodAttributes.Public | MethodAttributes.HideBySig |
			MethodAttributes.SpecialName,
			typeof(void),
			new System.Type[]{typeof(int), elementType});

		var setIL = setItem.GetILGenerator();
		throw_label = setIL.DefineLabel();

		// index < 0
		setIL.Emit(OpCodes.Ldarg_1);
		setIL.Emit(OpCodes.Ldc_I4_0);
		setIL.Emit(OpCodes.Blt, throw_label);

		// index >= len
		setIL.Emit(OpCodes.Ldarg_1);
		setIL.Emit(OpCodes.Ldarg_0);
		setIL.Emit(OpCodes.Ldfld, dataField);
		setIL.Emit(OpCodes.Ldlen);
		setIL.Emit(OpCodes.Bge, throw_label);

		// data[index] = value
		setIL.Emit(OpCodes.Ldarg_0);
		setIL.Emit(OpCodes.Ldfld, dataField);
		setIL.Emit(OpCodes.Ldarg_1);
		setIL.Emit(OpCodes.Ldarg_2);
		setIL.Emit(OpCodes.Stelem, elementType);
		setIL.Emit(OpCodes.Ret);

		// Index out of range
		setIL.MarkLabel(throw_label);
		setIL.Emit(OpCodes.Newobj, exceptionCtor);
		setIL.Emit(OpCodes.Throw);

		// Indexer
		var itemProperty = typeBuilder.DefineProperty(
			"Item",
			PropertyAttributes.None,
			elementType,
			new System.Type[]{typeof(int)});
		itemProperty.SetGetMethod(getItem);
		itemProperty.SetSetMethod(setItem);

		typeBuilder.CreateType();
        
        // Register array type with metadata about size
        ctx.RegisterUserType(typeName, typeBuilder);
    }

    private void GenerateRecordType(CodeGen.CodeGenContext ctx, string typeName) {
        // Create a new class type for the record
        var recordType = ctx.ModuleBuilder.DefineType(
            typeName,
            System.Reflection.TypeAttributes.Public | 
            System.Reflection.TypeAttributes.Class |
            System.Reflection.TypeAttributes.Sealed);
        
        // Create default constructor
        var ctor = recordType.DefineConstructor(
            System.Reflection.MethodAttributes.Public,
            System.Reflection.CallingConventions.Standard,
            SystemType.EmptyTypes);
        
        var ctorIL = ctor.GetILGenerator();
		ctorIL.Emit(OpCodes.Ldarg_0);
        ctorIL.Emit(OpCodes.Call, 
            typeof(object).GetConstructor(SystemType.EmptyTypes));
        // Add fields from record definition
        foreach (var child in this.childs[1].GetChilds()) {
            VarNode fieldNode = (VarNode)child;
            var fieldInfo = recordType.DefineField(
                ((PrimaryNode)fieldNode.GetChilds()[0]).Name(),
                ctx.ResolveType(fieldNode.Type()),
                System.Reflection.FieldAttributes.Public);

            if (ctx.UserTypes.ContainsKey(fieldNode.Type())) {
                var fieldType = ctx.UserTypes[fieldNode.Type()];
                ctorIL.Emit(OpCodes.Ldarg_0);
                ctorIL.Emit(OpCodes.Newobj, fieldType.GetConstructor(System.Type.EmptyTypes));
                ctorIL.Emit(OpCodes.Stfld, fieldInfo);
            }
        }

        ctorIL.Emit(OpCodes.Ret);
        
        // Register the type
        ctx.RegisterUserType(typeName, recordType);
		recordType.CreateType();
    }
}
