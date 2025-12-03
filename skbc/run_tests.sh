#!/bin/bash

echo "=== Running Compiler Tests ==="

dotnet build
pass="Passed in:"
failed="Failed in:"
echo ""
for test in tests/compile/*.skb; do
    # Compile
	echo "Testing: $test"
    bin/Debug/net9.0/skbc build "$test" "-s" $1
    
    if [ $? -eq 0 ]; then
        echo "✓ Compilation successful"
        pass="$pass
        $test"
    else
        echo "✗ Compilation failed in $test"
        failed="$failed
        $test"
    fi
    
    echo "---"
done

rm "main.dll"

echo "$pass"
echo "$failed"
