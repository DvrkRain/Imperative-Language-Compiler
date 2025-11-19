#!/bin/bash

echo "=== Running Compiler Tests ==="

for test in tests/*.skb; do
    echo ""
    echo "Testing: $test"
    
    # Compile
    dotnet run -- "$test" 3 output.dll
    
    if [ $? -eq 0 ]; then
        echo "✓ Compilation successful"
        
        # Run generated DLL
        echo "Output:"
        dotnet output.dll
    else
        echo "✗ Compilation failed"
        return
    fi
    
    echo "---"
done
