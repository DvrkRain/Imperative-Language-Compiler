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
		ArrayNode arr = (ArrayNode)this.childs[1];
		
		// Get array size
		ExpressionNode arraySize = (ExpressionNode)arr.GetChilds()[0];
        
        // Get element type (childs[1] is type)
        string elementTypeName = (string)((PrimaryNode)arr.GetChilds()[1]).value;
        System.Type elementType = ctx.ResolveType(elementTypeName);
        
        arraySize.Generate(ctx); // Put array size into stack
        
        if (ctx.CurrentMethod.Name == "_Main") { // Define global field
	        ctx.GlobalFields[typeName] = ctx.ProgramTypeBuilder.DefineField(
		        typeName,
		        typeof(int),
		        FieldAttributes.Public | FieldAttributes.Static);
	        
	        ctx.CurrentIL.Emit(OpCodes.Stsfld, ctx.GlobalFields[typeName]);
	        
        } else { // Define local var (type)
	        ctx.LocalVariables[typeName] = ctx.CurrentIL.DeclareLocal(typeof(int));
	        ctx.CurrentIL.Emit(OpCodes.Stloc, ctx.LocalVariables[typeName]);
        }
        
        // Register array type with metadata about size
        ctx.RegisterArrayType(typeName, elementType);
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
