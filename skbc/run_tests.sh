#!/bin/bash

echo "=== Running Compiler Tests ==="

dotnet build
complete="Completed in:"
failed="Failed in:"
for test in tests/*.skb; do
    echo ""
    echo "Testing: $test"
    
    # Compile
    stage=$1
    bin/Debug/net9.0/compiler-csharp "$test" "-s" $stage > "$test.txt"
    
    if [ $? -eq 0 ]; then
        echo "✓ Compilation successful"
        complete="$complete
        $test"
    else
        echo "✗ Compilation failed in $test"
        failed="$failed
        $test"
    fi
    
    echo "---"
done

rm "main.dll"

echo "$complete"
echo "$failed"
