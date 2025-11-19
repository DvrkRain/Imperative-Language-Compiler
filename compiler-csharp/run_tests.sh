#!/bin/bash

echo "=== Running Compiler Tests ==="

for test in tests/*.skb; do
    echo ""
    echo "Testing: $test"
    
    # Compile
    stage=2
    dotnet run -- "$test" $stage output.dll
    
    if [ \( $? -eq 0 \) -a \( $stage -eq 3 \) ]; then
        echo "✓ Compilation successful"
        
        # Run generated DLL
        echo "Output:"
        dotnet output.dll
    else
        echo "✗ Compilation failed in $test"
        return
    fi
    
    echo "---"
done
