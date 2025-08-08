#!/usr/bin/env bash
script_dir=$(dirname "$0")

node_output=$(node "$script_dir/js/qs.js")
cs_output=$(dotnet run --project "$script_dir/../QsNet.Comparison" -c Release)

if [ "$node_output" == "$cs_output" ]; then
    echo "The outputs are identical."
    exit 0
else
    echo "The outputs are different."
    diff <(echo "$node_output") <(echo "$cs_output")
    exit 1
fi