#!/bin/bash
set -e

# Template Pack Validation Script for Linux/macOS
# This script validates the CoreEx.Template pack by scaffolding and building test scenarios.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
TEMPLATE_PROJECT_PATH="$REPO_ROOT/src/CoreEx.Template"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
TEMPORARY_TEST_ROOT="$REPO_ROOT/artifacts/template-validation-test-$TIMESTAMP"

SKIP_CLEANUP=false
NO_REBUILD=false

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-cleanup)
            SKIP_CLEANUP=true
            shift
            ;;
        --no-rebuild)
            NO_REBUILD=true
            shift
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
NC='\033[0m' # No Color

write_header() {
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "$1"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
}

write_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

write_error() {
    echo -e "${RED}✗ $1${NC}"
}

echo "Repository root: $REPO_ROOT"
echo "Template project: $TEMPLATE_PROJECT_PATH"
echo "Test root: $TEMPORARY_TEST_ROOT"

# Build and pack the template
if [ "$NO_REBUILD" != "true" ]; then
    write_header "Building CoreEx.Template package"
    cd "$TEMPLATE_PROJECT_PATH"
    dotnet pack -c Release
    if [ $? -ne 0 ]; then
        echo "Failed to pack CoreEx.Template"
        exit 1
    fi
    cd - > /dev/null
    write_success "Template packed successfully"
fi

# Get template package path
NUPKG_FILE=$(find "$TEMPLATE_PROJECT_PATH/bin/Release" -name "CoreEx.Template.*.nupkg" -type f | head -1)
if [ -z "$NUPKG_FILE" ] || [ ! -f "$NUPKG_FILE" ]; then
    echo "Template package not found in $TEMPLATE_PROJECT_PATH/bin/Release"
    exit 1
fi
echo "Using template package: $NUPKG_FILE"

# Create test environment
write_header "Creating test environment"
mkdir -p "$TEMPORARY_TEST_ROOT"
write_success "Test root created: $TEMPORARY_TEST_ROOT"

# Define test scenarios
declare -a scenarios
declare -a scenario_names
declare -a scenario_templates
declare -a scenario_params

# Scenario 1: coreex-fullstack
scenarios[0]="test-fullstack"
scenario_names[0]="coreex-fullstack"
scenario_templates[0]="coreex"
scenario_params[0]="--data-provider SqlServer --messaging-provider ServiceBus --refdata-enabled true --rop-enabled false --domain-driven-enabled false --outbox-enabled true"

# Scenario 2: coreex-api-with-refdata
scenarios[1]="test-api-refdata"
scenario_names[1]="coreex-api-with-refdata"
scenario_templates[1]="coreex-api"
scenario_params[1]="--data-provider SqlServer --refdata-enabled true"

# Scenario 3: coreex-relay-with-servicebus
scenarios[2]="test-relay-servicebus"
scenario_names[2]="coreex-relay-with-servicebus"
scenario_templates[2]="coreex-relay"
scenario_params[2]="--data-provider SqlServer --messaging-provider ServiceBus"

# Scenario 4: coreex-subscriber-with-refdata
scenarios[3]="test-subscriber-refdata"
scenario_names[3]="coreex-subscriber-with-refdata"
scenario_templates[3]="coreex-subscriber"
scenario_params[3]="--data-provider SqlServer --messaging-provider ServiceBus --refdata-enabled true"

# Run test scenarios
write_header "Running template validation scenarios"

FAILED_SCENARIOS=()
SCENARIO_COUNT=${#scenarios[@]}

for i in $(seq 0 $((SCENARIO_COUNT - 1))); do
    TEST_DIR="$TEMPORARY_TEST_ROOT/${scenarios[$i]}"
    SCENARIO_NAME="${scenario_names[$i]}"
    TEMPLATE="${scenario_templates[$i]}"
    PARAMS="${scenario_params[$i]}"
    
    echo ""
    echo "Testing: $SCENARIO_NAME"
    echo "  Template: $TEMPLATE"
    echo "  Parameters: $PARAMS"
    
    mkdir -p "$TEST_DIR"
    
    # Scaffold the template
    NEW_CMD="dotnet new $TEMPLATE --output \"$TEST_DIR\" --force $PARAMS"
    eval "$NEW_CMD"
    if [ $? -ne 0 ]; then
        write_error "$SCENARIO_NAME failed: Template scaffolding failed"
        FAILED_SCENARIOS+=("$SCENARIO_NAME")
        continue
    fi
    
    # Validate generated files for specific templates
    if [[ "$TEMPLATE" == "coreex-api" || "$TEMPLATE" == "coreex-relay" || "$TEMPLATE" == "coreex-subscriber" ]]; then
        # Find and validate .csproj file
        PROJ_FILE=$(find "$TEST_DIR" -name "*.csproj" -type f | head -1)
        if [ ! -z "$PROJ_FILE" ]; then
            echo "Validating project: $PROJ_FILE"
            dotnet list "$PROJ_FILE" --format json > /dev/null 2>&1
            if [ $? -ne 0 ]; then
                write_error "$SCENARIO_NAME failed: Project file validation failed"
                FAILED_SCENARIOS+=("$SCENARIO_NAME")
                continue
            fi
        fi
    fi
    
    # Validate solution for full coreex template
    if [[ "$TEMPLATE" == "coreex" ]]; then
        SLN_FILE=$(find "$TEST_DIR" -name "*.sln" -type f | head -1)
        if [ ! -z "$SLN_FILE" ]; then
            echo "Validating solution: $SLN_FILE"
            if ! grep -q "Project" "$SLN_FILE"; then
                write_error "$SCENARIO_NAME failed: Solution file appears invalid"
                FAILED_SCENARIOS+=("$SCENARIO_NAME")
                continue
            fi
        fi
    fi
    
    write_success "$SCENARIO_NAME validated successfully"
done

# Summary
write_header "Validation Summary"

PASSED_COUNT=$((SCENARIO_COUNT - ${#FAILED_SCENARIOS[@]}))
echo "Passed: $PASSED_COUNT / $SCENARIO_COUNT"

if [ ${#FAILED_SCENARIOS[@]} -gt 0 ]; then
    write_error "Failed scenarios:"
    for scenario in "${FAILED_SCENARIOS[@]}"; do
        echo "  - $scenario"
    done
    EXIT_CODE=1
else
    write_success "All validation scenarios passed!"
    EXIT_CODE=0
fi

# Cleanup
if [ "$SKIP_CLEANUP" != "true" ] && [ -d "$TEMPORARY_TEST_ROOT" ]; then
    write_header "Cleaning up test environment"
    rm -rf "$TEMPORARY_TEST_ROOT"
    write_success "Test directory cleaned up: $TEMPORARY_TEST_ROOT"
else
    if [ "$SKIP_CLEANUP" = "true" ]; then
        echo ""
        echo "Test directory preserved (--skip-cleanup): $TEMPORARY_TEST_ROOT"
    fi
fi

exit $EXIT_CODE
