#!/bin/bash

echo "=== Running Compiler Tests ==="

dotnet build
for test in tests/*.skb; do
    echo ""
    echo "Testing: $test"
    
    # Compile
    stage=$1
    bin/Debug/net9.0/compiler-csharp.exe "$test" $stage "output.dll"
    
    if [ $? -eq 0 ]; then
        echo "✓ Compilation successful"
        
        # Run generated DLL
        if [ $stage -eq 3 ]; then
          echo "Output:"
#          dotnet output.dll
        fi
    else
        echo "✗ Compilation failed in $test"
        break
    fi
    
    echo "---"
done
